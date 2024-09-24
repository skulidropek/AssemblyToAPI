﻿using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using Library.Extensions;
using Library.Models;
using Library.Pull;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Library
{
    public static partial class AssemblyDataSerializer
    {
        public static string ConvertToJSON(string path)
        {
            return JsonSerializer.Serialize(ConvertToModel(path), new JsonSerializerOptions { WriteIndented = true });
        }

        public static AssemblyModel ConvertToModel(string path)
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(path);
            var assemblyModel = new AssemblyModel();

            // Обрабатываем типы (классы, интерфейсы и т.д.)
            foreach (var type in assemblyDefinition.MainModule.Types.Where(s => !s.FullName.Contains("<>") && !s.FullName.Contains("<Module>")))
            {
                var typeModel = new TypeModel
                {
                    ClassName = type.FullName,
                    InheritsFrom = type.BaseType != null ? type.BaseType.GetFriendlyTypeName() : null,
                    Accessibility = type.GetAccessibility(),
                    IsAbstract = type.IsAbstract,
                    IsSealed = type.IsSealed,
                    IsStatic = type.IsAbstract && type.IsSealed // В C# статический класс это абстрактный + запечатанный
                };

                // Обрабатываем поля
                foreach (var field in type.Fields)
                {
                    var fieldModel = new FieldModel
                    {
                        FieldType = field.FieldType.GetFriendlyTypeName(),
                        FieldName = field.Name,
                        IsStatic = field.IsStatic,
                        IsReadOnly = field.IsInitOnly, // readonly поле
                        Accessibility = field.GetAccessibility()
                    };
                    typeModel.Fields.Add(fieldModel);
                }

                // Обрабатываем свойства
                // Обрабатываем свойства
                foreach (var property in type.Properties)
                {
                    var propertyModel = new PropertyModel
                    {
                        PropertyType = property.PropertyType.GetFriendlyTypeName(),
                        PropertyName = property.Name
                    };

                    // Получаем уровни доступа для get и set методов, а также информацию о статичности
                    var (getAccessibility, setAccessibility, isStatic) = property.GetAccessibility();

                    propertyModel.GetAccessibility = getAccessibility;
                    propertyModel.SetAccessibility = setAccessibility;
                    propertyModel.IsStatic = isStatic;

                    // Добавляем свойство в модель типа
                    typeModel.Properties.Add(propertyModel);
                }


                // Обрабатываем методы
                foreach (var method in type.Methods)
                {
                    var isConstructor = method.Name == ".ctor" || method.Name == ".cctor";

                    if (isConstructor)
                    {
                        // Обработка конструктора
                        var constructorModel = new ConstructorModel
                        {
                            ConstructorName = type.Name, // Имя конструктора — это имя класса
                            IsStatic = method.IsStatic,
                            Accessibility = method.GetAccessibility()
                        };

                        // Обработка параметров конструктора
                        foreach (var parameter in method.Parameters)
                        {
                            constructorModel.Parameters.Add(new ParameterModel
                            {
                                ParameterType = parameter.ParameterType.GetFriendlyTypeName(),
                                ParameterName = parameter.Name
                            });
                        }

                        // Пропускаем пустые конструкторы
                        if (!constructorModel.Parameters.Any() && !method.IsStatic)
                        {
                            continue; // Пустой нестатический конструктор не добавляем
                        }

                        typeModel.Constructors.Add(constructorModel);
                    }
                    else
                    {
                        // Обработка обычных методов
                        var methodModel = new MethodModel
                        {
                            MethodReturnType = method.ReturnType.GetFriendlyTypeName(),
                            MethodName = method.Name,
                            IsStatic = method.IsStatic,
                            IsVirtual = method.IsVirtual,
                            IsOverride = method.HasOverrides,
                            IsAbstract = method.IsAbstract,
                            IsSealed = method.IsFinal,
                            Accessibility = method.GetAccessibility()
                        };

                        // Обработка параметров метода
                        foreach (var parameter in method.Parameters)
                        {
                            methodModel.Parameters.Add(new ParameterModel
                            {
                                ParameterType = parameter.ParameterType.GetFriendlyTypeName(),
                                ParameterName = parameter.Name
                            });
                        }

                        typeModel.Methods.Add(methodModel);
                    }
                }

                assemblyModel.Types.Add(typeModel);
            }

            return assemblyModel;
        }


        public static string ConvertToText(string path)
        {
            var assemblyModel = ConvertToModel(path);

            if (assemblyModel == null)
            {
                throw new InvalidOperationException("Невозможно десериализовать JSON.");
            }

            return ConvertToText(assemblyModel);
        }

        public static string ConvertToText(AssemblyModel assemblyModel)
        {
            var sb = new StringBuilder();

            foreach (var type in assemblyModel.Types.OrderBy(s => s.ClassName).Where(s => !s.ClassName.StartsWith("<")))
            {
                sb.Append(type.ToString());
            }

            return sb.ToString();
        }

        private static CSharpCompilation CreateAnalyzer(Microsoft.CodeAnalysis.SyntaxTree source, string compilationName, string managedFolder)
        {
            var references = Directory.GetFiles(managedFolder)
                                     .Where(f => !f.Contains("Newtonsoft.Json.dll"))
                                     .Select(path => MetadataReference.CreateFromFile(path.Replace("\n", "").Replace("\r", "")))
                                     .ToList();

            return CSharpCompilation.Create(compilationName,
                                            syntaxTrees: new[] { source },
                                            references: references,
                                            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        public static Dictionary<string, HookModel> FindHooksDictionary(string assemblyPath)
        {
            try
            {
                var hooks = new ConcurrentDictionary<string, HookModel>();
                var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

                var hooksMethodsTuple = new List<string>();

                Parallel.ForEach(assembly.MainModule.Types, type =>
                {
                    foreach (var method in type.Methods)
                    {
                        if (method.HasBody)
                        {
                            for (int i = 0; i < method.Body.Instructions.Count; i++)
                            {
                                var instruction = method.Body.Instructions[i];
                                if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                                {
                                    var methodReference = instruction.Operand as MethodReference;
                                    if (methodReference != null && IsHookMethod(methodReference))
                                    {
                                        string hookName = null;
                                        var parameters = new List<string>();

                                        // Пройти назад по инструкциям и собрать аргументы
                                        int argCount = methodReference.Parameters.Count;
                                        for (int j = i - argCount, argIndex = 0; j < i; j++, argIndex++)
                                        {
                                            if (j < 0 || j >= method.Body.Instructions.Count)
                                                continue; // Защита от выхода за пределы

                                            var argInstruction = method.Body.Instructions[j];
                                            if (argIndex == 0) // Первый аргумент - имя хука
                                            {
                                                hookName = argInstruction.Operand?.ToString();
                                            }
                                            else // Остальные аргументы
                                            {
                                                var paramType = argInstruction.Operand as TypeReference;
                                                parameters.Add(paramType?.Name ?? "unknown");
                                            }
                                        }

                                        if (string.IsNullOrEmpty(hookName))
                                            continue;

                                        hooksMethodsTuple.Add(type.Name + method.Name);
                                    }
                                }
                            }
                        }
                    }
                });

                if (hooksMethodsTuple.Count == 0)
                {
                    return new Dictionary<string, HookModel>();
                }

                var decompiler = new CSharpDecompiler(assemblyPath, new DecompilerSettings());

                var allTypeDefinitions = decompiler
                    .TypeSystem.GetAllTypeDefinitions()
                    .Where(s => s.Methods.Any())
                    .Where(s => hooksMethodsTuple.FirstOrDefault(s1 => s.Methods.Select(m => s.Name + m.Name).Contains(s1)) != null)
                    .ToList();

                foreach (var type in allTypeDefinitions)
                {
                    try
                    {
                        string decompiledCode = decompiler.DecompileTypeAsString(type.FullTypeName);
                        var syntaxTree = CSharpSyntaxTree.ParseText(decompiledCode);
                        var compilation = CreateAnalyzer(syntaxTree, "DecompiledAssembly", Path.GetDirectoryName(assemblyPath));

                        var semanticModel = compilation.GetSemanticModel(syntaxTree, true);
                        var visitor = new HookFinderVisitor(semanticModel);

                        visitor.Visit(syntaxTree.GetRoot());

                        foreach (var hook in visitor.Hooks)
                        {
                            hook.Value.ClassName = type.Name;
                            hooks.TryAdd(hook.Key, hook.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                return hooks.ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new Dictionary<string, HookModel>();
        }

        public static List<string> FindHooks(string assemblyPath) => FindHooksDictionary(assemblyPath).Select(s => s.Key).ToList();

        private static bool IsHookMethod(MethodReference method)
        {
            // Реализуйте логику для определения, является ли метод хуком
            // Например, проверка по имени метода или другим характеристикам
            return method.Name.Contains("CallHook"); // Пример условия
        }
    }
}
