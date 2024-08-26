using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Extensions
{
    public static class DefinitionExtensions
    {
        public static string GetAccessibility(this TypeDefinition type)
        {
            if (type.IsPublic) return "public";
            if (type.IsNotPublic) return "internal";
            return "unknown";
        }

        public static string GetAccessibility(this FieldDefinition field)
        {
            if (field.IsPublic) return "public";
            if (field.IsPrivate) return "private";
            if (field.IsFamily) return "protected";
            if (field.IsAssembly) return "internal";
            if (field.IsFamilyOrAssembly) return "protected internal";
            if (field.IsFamilyAndAssembly) return "private protected";
            return "unknown";
        }

        public static string GetAccessibility(this MethodDefinition method)
        {
            if (method.IsPublic) return "public";
            if (method.IsPrivate) return "private";
            if (method.IsFamily) return "protected";
            if (method.IsAssembly) return "internal";
            if (method.IsFamilyOrAssembly) return "protected internal";
            if (method.IsFamilyAndAssembly) return "private protected";
            return "unknown";
        }

        public static string GetAccessibility(this PropertyDefinition property)
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

    }
}
