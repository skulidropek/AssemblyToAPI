using Library.Extensions;

namespace Library.Models
{
    public class FieldModel
    {
        public string FieldType { get; set; }
        public string FieldName { get; set; }
        public MemberAccessibilityLevel Accessibility { get; set; }
        public bool IsStatic { get; set; }
        public bool IsReadOnly { get; set; }

        public List<AttributeModel> Attributes { get; set; } = new List<AttributeModel>();

        public override string ToString()
        {
            var staticModifier = IsStatic ? "static " : string.Empty;
            var readOnlyModifier = IsReadOnly ? "readonly " : string.Empty;

            // Формируем строку для атрибутов
            var attributesString = Attributes.Any() ? string.Join(Environment.NewLine, Attributes.Select(attr => attr.ToString())) + Environment.NewLine : string.Empty;

            return $"{attributesString}{Accessibility.ToAccessibilityString()} {staticModifier}{readOnlyModifier}{FieldType} {FieldName};";
        }
    }
}
