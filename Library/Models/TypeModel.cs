using Library.Extensions;
using Library.Models;
using System.Text;

namespace Library.Models
{
    public class TypeModel
    {
        public string ClassName { get; set; }
        public string InheritsFrom { get; set; }
        public TypeAccessibilityLevel Accessibility { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }
        public bool IsStatic { get; set; }      // Поддержка статического класса
        public List<FieldModel> Fields { get; set; } = new List<FieldModel>();
        public List<PropertyModel> Properties { get; set; } = new List<PropertyModel>();
        public List<MethodModel> Methods { get; set; } = new List<MethodModel>();

        public override string ToString()
        {
            var sb = new StringBuilder();

            // Добавляем static, abstract и sealed модификаторы
            var staticModifier = IsStatic ? "static " : string.Empty;
            var abstractModifier = IsAbstract ? "abstract " : string.Empty;
            var sealedModifier = IsSealed ? "sealed " : string.Empty;

            sb.AppendLine($"{Accessibility.ToAccessibilityString()} {staticModifier}{abstractModifier}{sealedModifier}{ClassName}{(string.IsNullOrEmpty(InheritsFrom) ? "" : " : " + InheritsFrom)} {{");

            foreach (var field in Fields)
            {
                sb.AppendLine($"    {field.ToString()}");
            }

            foreach (var property in Properties)
            {
                sb.AppendLine($"    {property.ToString()}");
            }

            foreach (var method in Methods)
            {
                sb.AppendLine($"    {method.ToString()}");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}