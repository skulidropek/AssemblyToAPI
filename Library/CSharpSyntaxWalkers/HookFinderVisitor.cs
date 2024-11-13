using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Library.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static ICSharpCode.Decompiler.IL.Transforms.Stepper;

namespace Library
{
    public static partial class AssemblyDataSerializer
    {
        public class HookFinderVisitor : CSharpSyntaxWalker
        {
            private readonly SemanticModel semanticModel;
            public Dictionary<string, HookModel> Hooks = new Dictionary<string, HookModel>();

            public HookFinderVisitor(SemanticModel semanticModel)
            {
                this.semanticModel = semanticModel;
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
            {
                var memberAccessExpr = invocation.Expression as MemberAccessExpressionSyntax;

                if (memberAccessExpr != null && Regex.IsMatch(memberAccessExpr?.ToFullString(), @"Interface(.Oxide)?.CallHook"))
                {
                    var hookName = invocation.ArgumentList.Arguments.First().ToString();
                    var parameters = new List<string>();

                    foreach (var arg in invocation.ArgumentList.Arguments.Skip(1))
                    {
                        var typeInfo = semanticModel.GetTypeInfo(arg.Expression);
                        var type = typeInfo.Type?.ToString() ?? "unknown";
                        parameters.Add(type);
                    }

                    var hash = hookName.Replace("\"", "") + "(" + string.Join(',', parameters) + ")";

                    if (!Hooks.ContainsKey(hash))
                    {
                        var method = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                        if (method == null)
                        {
                            // Проверка в родительских узлах
                            var parent = invocation.Parent;
                            while (parent != null)
                            {
                                method = parent.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                    .FirstOrDefault(m => m.DescendantNodes().Contains(invocation));
                                if (method != null)
                                    break;

                                parent = parent.Parent;
                            }
                        }

                        if (method != null)
                        {
                            Hooks.Add(hash, new HookModel()
                            {
                                MethodName = method.Identifier.Text,
                                Name = hookName.Replace("\"", ""),
                                Parameters = "(" + string.Join(',', parameters) + ")",
                                MethodCode = method.ToFullString(),
                            });
                        }
                    }
                }

                base.VisitInvocationExpression(invocation);
            }
        }

    }
}
