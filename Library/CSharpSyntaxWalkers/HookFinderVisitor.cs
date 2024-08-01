using System.Reflection;
using System.Text.RegularExpressions;
using Library.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

                var code = memberAccessExpr.ToFullString();

                if (memberAccessExpr != null && Regex.IsMatch(code, @"Interface(.Oxide)?.CallHook"))
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
                        Hooks.Add(hash, new HookModel()
                        {
                            Name = hookName.Replace("\"", ""),
                            Parameters = "(" + string.Join(',', parameters) + ")",
                            MethodCode = code,
                        });
                    }
                }

                base.VisitInvocationExpression(invocation);
            }
        }

    }
}
