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
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

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

            foreach (var type in assemblyDefinition.MainModule.Types)
            {
                var typeModel = new TypeModel
                {
                    ClassName = type.FullName,
                    InheritsFrom = type.BaseType != null ? type.BaseType.GetFriendlyTypeName() : null
                };

                foreach (var field in type.Fields)
                {
                    typeModel.Fields.Add(new FieldModel { FieldType = field.FieldType.GetFriendlyTypeName(), FieldName = field.Name });
                }

                foreach (var property in type.Properties)
                {
                    typeModel.Properties.Add(new PropertyModel { PropertyType = property.PropertyType.GetFriendlyTypeName(), PropertyName = property.Name });
                }

                foreach (var method in type.Methods)
                {
                    var methodModel = new MethodModel
                    {
                        MethodReturnType = method.ReturnType.GetFriendlyTypeName(),
                        MethodName = method.Name
                    };

                    foreach (var parameter in method.Parameters)
                    {
                        methodModel.Parameters.Add(new ParameterModel { ParameterType = parameter.ParameterType.GetFriendlyTypeName(), ParameterName = parameter.Name });
                    }

                    typeModel.Methods.Add(methodModel);
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

            foreach (var type in assemblyModel.Types)
            {
                sb.Append(ConvertToText(type));
            }

            return sb.ToString();
        }

        public static string ConvertToText(TypeModel type)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"class: {type.ClassName}");
            if (!string.IsNullOrEmpty(type.InheritsFrom))
            {
                sb.AppendLine($"inherits from: {type.InheritsFrom}");
            }

            sb.AppendLine("fields:");
            foreach (var field in type.Fields)
            {
                sb.AppendLine($"{field.FieldType} {field.FieldName}");
            }

            sb.AppendLine("properties:");
            foreach (var property in type.Properties)
            {
                sb.AppendLine($"{property.PropertyType} {property.PropertyName}");
            }

            sb.AppendLine("methods:");
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
                sb.AppendLine($"{methodSignature}");
            }

            sb.AppendLine(new string('-', 50));

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

        public static List<string> FindHooks(string assemblyPath)
        {
            var hooks = new ConcurrentDictionary<string, bool>();
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
                                    var parameters = new System.Collections.Generic.List<string>();

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
                                            //Console.WriteLine(paramType);
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

            if(hooksMethodsTuple.Count == 0)
            {
                return new List<string>();
            }

            var decompiler = new CSharpDecompiler(assemblyPath, new DecompilerSettings());

            var allTypeDefinitions = decompiler
                .TypeSystem.GetAllTypeDefinitions()
                    .Where(s => s.Methods.Any())
                    .Where(s => hooksMethodsTuple.FirstOrDefault(s1 => s.Methods.Select(m => s.Name + m.Name).Contains(s1)) != null)
                    .ToList();

            foreach(var type in allTypeDefinitions)
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
                        if (hooks.TryAdd(hook, true))
                        {
                          //  Console.WriteLine(hook);
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
               
            }

            return hooks.Select(s => s.Key).ToList();
        }

        private static bool IsHookMethod(MethodReference method)
        {
            // Реализуйте логику для определения, является ли метод хуком
            // Например, проверка по имени метода или другим характеристикам
            return method.Name.Contains("CallHook"); // Пример условия
        }
    }
}
