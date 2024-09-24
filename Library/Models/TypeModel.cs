using Library.Extensions;
using System.Text;

namespace Library.Models
{
    public class TypeModel
    {
        public string ClassName { get; set; }
        public string InheritsFrom { get; set; }
        public TypeAccessibilityLevel Accessibility { get; set; }

        public bool IsAbstract { get; set; }
        public bool IsStatic { get; set; }
        public bool IsInterface { get; set; }
        public bool IsEnum { get; set; }

        public List<AttributeModel> Attributes { get; set; } = new List<AttributeModel>();

        public List<FieldModel> Fields { get; set; } = new List<FieldModel>();
        public List<PropertyModel> Properties { get; set; } = new List<PropertyModel>();
        public List<ConstructorModel> Constructors { get; set; } = new List<ConstructorModel>();
        public List<MethodModel> Methods { get; set; } = new List<MethodModel>();

        public override string ToString()
        {
            var sb = new StringBuilder();

            // Формируем строку для атрибутов
            var attributesString = Attributes.Any() ? string.Join(Environment.NewLine, Attributes.Select(attr => attr.ToString())) + Environment.NewLine : string.Empty;

            var typeModifier = IsInterface ? "interface " :
                               IsEnum ? "enum " :
                               IsStatic ? "static class " :
                               IsAbstract ? "abstract class " : "class ";

            sb.AppendLine($"{attributesString}{Accessibility.ToAccessibilityString()} {typeModifier}{ClassName}{(string.IsNullOrEmpty(InheritsFrom) ? "" : " : " + InheritsFrom)} {{");

            foreach (var field in Fields)
            {
                sb.AppendLine($"    {field.ToString()}");
            }

            foreach (var property in Properties)
            {
                sb.AppendLine($"    {property.ToString()}");
            }

            foreach (var constructor in Constructors)
            {
                sb.AppendLine($"    {constructor.ToString()}");
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
