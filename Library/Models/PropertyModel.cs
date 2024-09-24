using Library.Extensions;
using System.Collections.Generic;
using System.Linq;
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

        // Поддержка атрибутов
        public List<AttributeModel> Attributes { get; set; } = new List<AttributeModel>();

        public override string ToString()
        {
            var staticModifier = IsStatic ? "static " : string.Empty;

            // Формируем строку для атрибутов
            var attributesString = Attributes.Any()
                ? string.Join(System.Environment.NewLine, Attributes.Select(attr => attr.ToString())) + System.Environment.NewLine
                : string.Empty;

            var propertyDeclaration = new StringBuilder($"{attributesString}{GetAccessibility.ToAccessibilityString()} {staticModifier}{PropertyType} {PropertyName} {{");

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
