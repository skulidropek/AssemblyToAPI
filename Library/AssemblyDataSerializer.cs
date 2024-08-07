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

            foreach (var type in assemblyDefinition.MainModule.Types.Where(s => !s.FullName.Contains("<>") && !s.FullName.Contains("<Module>")))
            {
                var typeModel = new TypeModel
                {
                    ClassName = type.FullName,
                    InheritsFrom = type.BaseType != null ? type.BaseType.GetFriendlyTypeName() : null,
                    Accessibility = GetAccessibility(type)
                };

                foreach (var field in type.Fields)
                {
                    typeModel.Fields.Add(new FieldModel
                    {
                        FieldType = field.FieldType.GetFriendlyTypeName(),
                        FieldName = field.Name,
                        Accessibility = GetAccessibility(field)
                    });
                }

                foreach (var property in type.Properties)
                {
                    typeModel.Properties.Add(new PropertyModel
                    {
                        PropertyType = property.PropertyType.GetFriendlyTypeName(),
                        PropertyName = property.Name,
                        Accessibility = GetAccessibility(property)
                    });
                }

                foreach (var method in type.Methods)
                {
                    var methodModel = new MethodModel
                    {
                        MethodReturnType = method.ReturnType.GetFriendlyTypeName(),
                        MethodName = method.Name,
                        Accessibility = GetAccessibility(method)
                    };

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

                assemblyModel.Types.Add(typeModel);
            }


            return assemblyModel;
        }

        static string GetAccessibility(TypeDefinition type)
        {
            if (type.IsPublic) return "public";
            if (type.IsNotPublic) return "internal";
            return "unknown";
        }

        static string GetAccessibility(FieldDefinition field)
        {
            if (field.IsPublic) return "public";
            if (field.IsPrivate) return "private";
            if (field.IsFamily) return "protected";
            if (field.IsAssembly) return "internal";
            if (field.IsFamilyOrAssembly) return "protected internal";
            if (field.IsFamilyAndAssembly) return "private protected";
            return "unknown";
        }

        static string GetAccessibility(MethodDefinition method)
        {
            if (method.IsPublic) return "public";
            if (method.IsPrivate) return "private";
            if (method.IsFamily) return "protected";
            if (method.IsAssembly) return "internal";
            if (method.IsFamilyOrAssembly) return "protected internal";
            if (method.IsFamilyAndAssembly) return "private protected";
            return "unknown";
        }

        static string GetAccessibility(PropertyDefinition property)
        {
            // Определение уровня доступа по методам get и set
            var getMethod = property.GetMethod;
            var setMethod = property.SetMethod;

            var getAccessibility = getMethod != null ? GetAccessibility(getMethod) : null;
            var setAccessibility = setMethod != null ? GetAccessibility(setMethod) : null;

            // Возвращаем более открытый уровень доступа (если один из методов публичный, то свойство публичное)
            if (getAccessibility == "public" || setAccessibility == "public") return "public";
            if (getAccessibility == "protected internal" || setAccessibility == "protected internal") return "protected internal";
            if (getAccessibility == "internal" || setAccessibility == "internal") return "internal";
            if (getAccessibility == "protected" || setAccessibility == "protected") return "protected";
            if (getAccessibility == "private protected" || setAccessibility == "private protected") return "private protected";
            return "private";
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
                sb.Append(ConvertToText(type));
            }

            return sb.ToString();
        }

        public static string ConvertToText(TypeModel type)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{type.Accessibility} {type.ClassName}{(string.IsNullOrEmpty(type.InheritsFrom) ? "" : " : " + type.InheritsFrom)} {{");

            if (type.Fields.Count == 0 && type.Properties.Count == 0 && type.Methods.Count == 0)
            {
                return string.Empty;
            }

            var body = new StringBuilder();

            foreach (var field in type.Fields)
            {
                body.AppendLine($"{field.Accessibility} {field.FieldType} {field.FieldName}");
            }

            foreach (var property in type.Properties)
            {
                body.AppendLine($"{property.Accessibility} {property.PropertyType} {property.PropertyName}");
            }

            foreach (var method in type.Methods)
            {
                var methodSignature = $"{method.Accessibility} {method.MethodReturnType} {method.MethodName}({string.Join(", ", method.Parameters.Select(p => $"{p.ParameterType} {p.ParameterName}"))})";
                
                if(Regex.IsMatch(methodSignature, @"void .c?ctor\(\)"))
                    continue;

                body.AppendLine(methodSignature);
            }

            if (body.Length == 0)
            {
                return string.Empty;
            }

            sb.Append(body);
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static string ConvertAssemblyToMarkdownTable(string path)
        {
            var assemblyModel = ConvertToModel(path);

            if (assemblyModel == null)
            {
                throw new InvalidOperationException("Невозможно десериализовать JSON.");
            }

            return ConvertAssemblyToMarkdownTable(assemblyModel);
        }

        public static string ConvertAssemblyToMarkdownTable(AssemblyModel assemblyModel)
        {
            var sb = new StringBuilder();

            // Введение разделителя между типами, если это необходимо
            string typeSeparator = "---"; // Разделитель для лучшей читаемости и разделения типов

            bool isFirstType = true; // Флаг для проверки, является ли обрабатываемый тип первым в списке

            foreach (var type in assemblyModel.Types)
            {
                // Добавляем разделитель между типами, если это не первый тип
                if (!isFirstType)
                {
                    sb.Append(typeSeparator);
                }
                else
                {
                    isFirstType = false; // Сбрасываем флаг для всех следующих типов
                }

                // Конвертируем информацию о типе в формат Markdown таблицы и добавляем в общий StringBuilder
                sb.Append(ConvertToMarkdownTable(type));
            }

            return sb.ToString();
        }

        public static string ConvertToMarkdownTable(TypeModel type)
        {
            var sb = new StringBuilder();

            // Добавление заголовка класса и наследования, если есть
            sb.AppendLine($"| Class | Inherits From |");
            sb.AppendLine($"| --- | --- |");
            sb.AppendLine($"| {type.ClassName} | {(string.IsNullOrEmpty(type.InheritsFrom) ? "N/A" : type.InheritsFrom)} |");

            // Добавление полей
            if (type.Fields.Any())
            {
                sb.AppendLine("| Field Type | Field Name |");
                sb.AppendLine("| --- | --- |");
                foreach (var field in type.Fields)
                {
                    sb.AppendLine($"| {field.FieldType} | {field.FieldName} |");
                }
            }

            // Добавление свойств
            if (type.Properties.Any())
            {
                sb.AppendLine("| Property Type | Property Name |");
                sb.AppendLine("| --- | --- |");
                foreach (var property in type.Properties)
                {
                    sb.AppendLine($"| {property.PropertyType} | {property.PropertyName} |");
                }
            }

            // Добавление методов
            if (type.Methods.Any())
            {
                sb.AppendLine("| Method Signature |");
                sb.AppendLine("| --- |");
                foreach (var method in type.Methods)
                {
                    var methodSignature = new StringBuilder();
                    methodSignature.Append($"{method.MethodReturnType} {method.MethodName}(");
                    for (int i = 0; i < method.Parameters.Count; i++)
                    {
                        var parameter = method.Parameters[i];
                        methodSignature.Append($"{parameter.ParameterType} {parameter.ParameterName}");
                        if (i < method.Parameters.Count - 1)
                        {
                            methodSignature.Append(", ");
                        }
                    }
                    methodSignature.Append(")");
                    sb.AppendLine($"| {methodSignature} |");
                }
            }

            return sb.ToString();
        }

        //public static List<string> GetAllHooks(string path)
        //{
        //    var decompiler = new CSharpDecompiler(path, new DecompilerSettings());

        //    List<string> strings = new List<string>();

        //    foreach (var type in decompiler.TypeSystem.GetAllTypeDefinitions())
        //    {
        //        try
        //        {
        //            var code = decompiler.DecompileTypeAsString(type.FullTypeName);
        //            var code1 = decompiler.DecompileType(type.FullTypeName);
        //            if (Regex.IsMatch(code, @"Interface(.Oxide)?.CallHook\("".+"",.+\)"))
        //            {
        //                var matches = Regex.Matches(code, @"Interface(.Oxide)?.CallHook\(("".+""),.+\)");
        //                foreach (Match match in matches)
        //                {
        //                    var str = match.Groups[2].ToString();
        //                    if (!strings.Contains(str))
        //                    {
        //                        strings.Add(str);
        //                        Console.WriteLine(str);
        //                    }
        //                }
        //            }
        //        }
        //        catch
        //        {

        //        }
        //    }

        //    return strings;
        //}

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
                            hooks.TryAdd(hook.Key, hook.Value);
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
