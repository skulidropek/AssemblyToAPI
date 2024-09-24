using Library.Extensions;
using System.Text;

namespace Library.Models
{
    public class PropertyModel
    {
        public string PropertyType { get; set; }
        public string PropertyName { get; set; }
        public bool IsStatic { get; set; }

        public MemberAccessibilityLevel GetAccessibility { get; set; }
        public MemberAccessibilityLevel SetAccessibility { get; set; }

        public bool CanRead => GetAccessibility != MemberAccessibilityLevel.Unknown;
        public bool CanWrite => SetAccessibility != MemberAccessibilityLevel.Unknown;

        public override string ToString()
        {
            var staticModifier = IsStatic ? "static " : string.Empty;

            var propertyDeclaration = new StringBuilder($"{GetAccessibility.ToAccessibilityString()} {staticModifier}{PropertyType} {PropertyName} {{");

            if (CanRead)
            {
                propertyDeclaration.Append(" get; ");
            }

            if (CanWrite)
            {
                propertyDeclaration.Append($"{SetAccessibility.ToAccessibilityString()} set; ");
            }

            propertyDeclaration.Append("}");
            return propertyDeclaration.ToString();
        }
    }
}
