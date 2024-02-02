using System.Reflection;
using System.Text.RegularExpressions;
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
            public List<string> Hooks = new List<string>();

            public HookFinderVisitor(SemanticModel semanticModel)
            {
                this.semanticModel = semanticModel;
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
            {
                var memberAccessExpr = invocation.Expression as MemberAccessExpressionSyntax;

                if (memberAccessExpr != null && Regex.IsMatch(memberAccessExpr.ToFullString(), @"Interface(.Oxide)?.CallHook"))
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

                    if (!Hooks.Contains(hash))
                    {
                        Hooks.Add(hash);
                    }
                }

                base.VisitInvocationExpression(invocation);
            }
        }

    }
}
