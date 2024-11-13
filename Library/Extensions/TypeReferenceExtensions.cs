using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;

namespace Library.Extensions
{
    internal static class TypeReferenceExtensions
    {
        public static string GetFriendlyTypeName(this TypeReference type)
        {
            switch (type.FullName)
            {
                case "System.Void": return "void";
                case "System.Int32": return "int";
                case "System.Single": return "float";
                case "System.Double": return "double";
                case "System.Decimal": return "decimal";
                case "System.Boolean": return "bool";
                case "System.Char": return "char";
                case "System.Byte": return "byte";
                case "System.SByte": return "sbyte";
                case "System.Int16": return "short";
                case "System.UInt16": return "ushort";
                case "System.Int64": return "long";
                case "System.UInt64": return "ulong";
                case "System.String": return "string";
                case "System.Object": return "object";
                default:
                    if (type.IsGenericInstance)
                    {
                        var genericType = (GenericInstanceType)type;
                        string genericTypeName = GetFriendlyTypeName(genericType.ElementType);
                        string genericArgs = string.Join(", ", genericType.GenericArguments.Select(GetFriendlyTypeName));
                        return $"{genericTypeName}<{genericArgs}>";
                    }

                    return type.Name;
            }
        }

        public static string GetFriendlyTypeName(this TypeSyntax type)
        {
            string typeName = type.ToString();

            switch (typeName)
            {
                case "void": return "void";
                case "int": return "int";
                case "float": return "float";
                case "double": return "double";
                case "decimal": return "decimal";
                case "bool": return "bool";
                case "char": return "char";
                case "byte": return "byte";
                case "sbyte": return "sbyte";
                case "short": return "short";
                case "ushort": return "ushort";
                case "long": return "long";
                case "ulong": return "ulong";
                case "string": return "string";
                case "object": return "object";
                default:
                    // Здесь можно добавить логику для обработки обобщенных типов, если нужно.
                    return typeName;
            }
        }
    }
}
