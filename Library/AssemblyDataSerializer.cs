using ICSharpCode.Decompiler;
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
using System.Text.Json.Serialization.Metadata;
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
            var directory = Path.GetDirectoryName(path);

            var resolver = new DefaultAssemblyResolver();

            if (directory != null)
            {
                resolver.AddSearchDirectory(directory);
            }

            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = resolver
            };

            using var assemblyDefinition = AssemblyDefinition.ReadAssembly(path, readerParameters);
            
            if(assemblyDefinition == null)
            {
                return null;
            }

            var assemblyModel = new AssemblyModel();

            foreach (var type in assemblyDefinition.MainModule.Types.Where(s => !s.FullName.Contains("<>") && !s.FullName.Contains("<Module>")))
            {
                var typeModel = new TypeModel
                {
                    ClassName = type.FullName,
                    InheritsFrom = type.BaseType != null ? type.BaseType.GetFriendlyTypeName() : null,
                    Accessibility = type.GetAccessibility(),
                    IsAbstract = type.IsAbstract && !type.IsInterface && !type.IsEnum,
                    IsStatic = type.IsAbstract && type.IsSealed && !type.IsInterface && !type.IsEnum,
                    IsInterface = type.IsInterface,
                    IsEnum = type.IsEnum
                };

                foreach (var attribute in type.CustomAttributes.OrderBy(s => s.AttributeType.Name))
                {
                    var attributeModel = new AttributeModel
                    {
                        Name = attribute.AttributeType.Name
                    };

                    try
                    {
                        if (attribute?.HasConstructorArguments ?? false)
                        {
                            foreach (var argument in attribute.ConstructorArguments)
                            {
                                attributeModel.Arguments.Add(argument.Value?.ToString() ?? "");
                            }
                        }
                    }
                    catch
                    {

                    }

                    typeModel.Attributes.Add(attributeModel);
                }

                foreach (var field in type.Fields)
                {
                    var fieldModel = new FieldModel
                    {
                        FieldType = field.FieldType.GetFriendlyTypeName(),
                        FieldName = field.Name,
                        IsStatic = field.IsStatic,
                        Accessibility = field.GetAccessibility()
                    };

                    foreach (var attribute in field.CustomAttributes.OrderBy(s => s.AttributeType.Name))
                    {
                        var attributeModel = new AttributeModel
                        {
                            Name = attribute.AttributeType.Name
                        };

                        try
                        {
                            if (attribute?.HasConstructorArguments ?? false)
                            {
                                foreach (var argument in attribute.ConstructorArguments)
                                {
                                    attributeModel.Arguments.Add(argument.Value?.ToString() ?? "");
                                }
                            }
                        }
                        catch
                        {

                        }

                        fieldModel.Attributes.Add(attributeModel);
                    }

                    typeModel.Fields.Add(fieldModel);
                }

                foreach (var property in type.Properties)
                {
                    var propertyModel = new PropertyModel
                    {
                        PropertyType = property.PropertyType.GetFriendlyTypeName(),
                        PropertyName = property.Name
                    };

                    var (getAccessibility, setAccessibility, isStatic) = property.GetAccessibility();

                    propertyModel.GetAccessibility = getAccessibility;
                    propertyModel.SetAccessibility = setAccessibility;
                    propertyModel.IsStatic = isStatic;

                    foreach (var attribute in property.CustomAttributes.OrderBy(s => s.AttributeType.Name))
                    {
                        var attributeModel = new AttributeModel
                        {
                            Name = attribute.AttributeType.Name
                        };

                        try
                        {
                            if (attribute?.HasConstructorArguments ?? false)
                            {
                                foreach (var argument in attribute.ConstructorArguments)
                                {
                                    attributeModel.Arguments.Add(argument.Value?.ToString() ?? "");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                      

                        propertyModel.Attributes.Add(attributeModel);
                    }

                    typeModel.Properties.Add(propertyModel);
                }

                foreach (var method in type.Methods)
                {
                    var isConstructor = method.Name == ".ctor" || method.Name == ".cctor";

                    if (isConstructor)
                    {
                        var constructorModel = new ConstructorModel
                        {
                            ConstructorName = type.Name,
                            IsStatic = method.IsStatic,
                            Accessibility = method.GetAccessibility()
                        };

                        foreach (var parameter in method.Parameters)
                        {
                            constructorModel.Parameters.Add(new ParameterModel
                            {
                                ParameterType = parameter.ParameterType.GetFriendlyTypeName(),
                                ParameterName = parameter.Name
                            });
                        }

                        if (!constructorModel.Parameters.Any() && !method.IsStatic)
                        {
                            continue; 
                        }

                        foreach (var attribute in method.CustomAttributes.OrderBy(s => s.AttributeType.Name))
                        {
                            var attributeModel = new AttributeModel
                            {
                                Name = attribute.AttributeType.Name
                            };

                            try
                            {
                                if (attribute?.HasConstructorArguments ?? false)
                                {
                                    foreach (var argument in attribute.ConstructorArguments)
                                    {
                                        attributeModel.Arguments.Add(argument.Value?.ToString() ?? "");
                                    }
                                }
                            }
                            catch
                            {

                            }
                            
                            constructorModel.Attributes.Add(attributeModel);
                        }

                        typeModel.Constructors.Add(constructorModel);
                    }
                    else
                    {
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

                        foreach (var parameter in method.Parameters)
                        {
                            methodModel.Parameters.Add(new ParameterModel
                            {
                                ParameterType = parameter.ParameterType.GetFriendlyTypeName(),
                                ParameterName = parameter.Name
                            });
                        }

                        foreach (var attribute in method.CustomAttributes.OrderBy(s => s.AttributeType.Name))
                        {
                            var attributeModel = new AttributeModel
                            {
                                Name = attribute.AttributeType.Name
                            };

                            try
                            {
                                if (attribute?.HasConstructorArguments ?? false)
                                {
                                    foreach (var argument in attribute.ConstructorArguments)
                                    {
                                        attributeModel.Arguments.Add(argument.Value?.ToString() ?? "");
                                    }
                                }
                            }
                            catch
                            {

                            }

                            methodModel.Attributes.Add(attributeModel);
                        }

                        typeModel.Methods.Add(methodModel);
                    }
                }

                assemblyModel.Types.Add(typeModel);
            }

            var hooks = FindHooksDictionary(path);

            if (hooks.Count > 0)
            {
                foreach (var type in assemblyModel.Types)
                {
                    for (int i = 0; i < type.Methods.Count; i++)
                    {
                        var method = type.Methods[i];
                        var className = type.ClassName.Split('.').Last();
                        var findHooks = hooks
                            .Where(s => s.Value.MethodName == method.MethodName && s.Value.ClassName == className)
                            .Where(s => s.Value.MethodParameters.Count == method.Parameters.Count &&
                                        s.Value.MethodParameters
                                            .Zip(method.Parameters, (param1, param2) =>
                                                param1.ParameterType == param2.ParameterType &&
                                                param1.ParameterName == param2.ParameterName)
                                            .All(match => match))
                            .Select(s => s.Value)
                            .ToList();

                        if (findHooks.Count > 0)
                        {
                            type.Methods[i] = new RustMethodModel(type.Methods[i], findHooks); 
                        }
                    }
                }
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
                                                if (argInstruction.Operand != null)
                                                {
                                                    hookName = argInstruction.Operand.ToString();
                                                }
                                                else if (argInstruction?.Previous?.Operand != null)
                                                {
                                                    hookName = argInstruction.Previous.Operand.ToString();
                                                }
                                                else if(argInstruction?.Next?.Operand != null)
                                                {
                                                    hookName = argInstruction.Next.Operand.ToString();
                                                }
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
