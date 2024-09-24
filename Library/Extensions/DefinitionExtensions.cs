using Library.Models;  
using Mono.Cecil;

namespace Library.Extensions
{
    public static class DefinitionExtensions
    {
        public static TypeAccessibilityLevel GetAccessibility(this TypeDefinition type)
        {
            if (type.IsPublic) return TypeAccessibilityLevel.Public;
            if (type.IsNotPublic) return TypeAccessibilityLevel.Internal;
            return TypeAccessibilityLevel.Unknown;
        }

        public static MemberAccessibilityLevel GetAccessibility(this FieldDefinition field)
        {
            if (field.IsPublic) return MemberAccessibilityLevel.Public;
            if (field.IsPrivate) return MemberAccessibilityLevel.Private;
            if (field.IsFamily) return MemberAccessibilityLevel.Protected;
            if (field.IsAssembly) return MemberAccessibilityLevel.Internal;
            if (field.IsFamilyOrAssembly) return MemberAccessibilityLevel.ProtectedInternal;
            if (field.IsFamilyAndAssembly) return MemberAccessibilityLevel.PrivateProtected;
            return MemberAccessibilityLevel.Unknown;
        }

        public static MemberAccessibilityLevel GetAccessibility(this MethodDefinition method)
        {
            if (method.IsPublic) return MemberAccessibilityLevel.Public;
            if (method.IsPrivate) return MemberAccessibilityLevel.Private;
            if (method.IsFamily) return MemberAccessibilityLevel.Protected;
            if (method.IsAssembly) return MemberAccessibilityLevel.Internal;
            if (method.IsFamilyOrAssembly) return MemberAccessibilityLevel.ProtectedInternal;
            if (method.IsFamilyAndAssembly) return MemberAccessibilityLevel.PrivateProtected;
            return MemberAccessibilityLevel.Unknown;
        }

        public static (MemberAccessibilityLevel GetAccessibility, MemberAccessibilityLevel SetAccessibility, bool IsStatic) GetAccessibility(this PropertyDefinition property)
        {
            var getMethod = property.GetMethod;
            var setMethod = property.SetMethod;

            // Определяем уровень доступа для get метода
            var getAccessibility = getMethod != null
                ? GetAccessibility(getMethod)
                : MemberAccessibilityLevel.Unknown;

            // Определяем уровень доступа для set метода
            var setAccessibility = setMethod != null
                ? GetAccessibility(setMethod)
                : MemberAccessibilityLevel.Unknown;

            // Свойство является статическим, если его метод get или set статический
            var isStatic = (getMethod?.IsStatic ?? false) || (setMethod?.IsStatic ?? false);

            // Возвращаем кортеж с уровнями доступа для get и set методов и значением IsStatic
            return (getAccessibility, setAccessibility, isStatic);
        }
    }
}
