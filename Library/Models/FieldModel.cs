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

        public override string ToString()
        {
            var staticModifier = IsStatic ? "static " : string.Empty;
            var readOnlyModifier = IsReadOnly ? "readonly " : string.Empty;
            return $"{Accessibility.ToAccessibilityString()} {staticModifier}{readOnlyModifier}{FieldType} {FieldName};";
        }
    }
}
