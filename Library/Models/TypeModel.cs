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
        public List<FieldModel> Fields { get; set; } = new List<FieldModel>();
        public List<PropertyModel> Properties { get; set; } = new List<PropertyModel>();
        public List<ConstructorModel> Constructors { get; set; } = new List<ConstructorModel>(); // Добавляем список конструкторов
        public List<MethodModel> Methods { get; set; } = new List<MethodModel>();

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{Accessibility.ToAccessibilityString()} {ClassName}{(string.IsNullOrEmpty(InheritsFrom) ? "" : " : " + InheritsFrom)} {{");

            // Добавляем поля
            foreach (var field in Fields)
            {
                sb.AppendLine($"    {field.ToString()}");
            }

            // Добавляем свойства
            foreach (var property in Properties)
            {
                sb.AppendLine($"    {property.ToString()}");
            }

            // Добавляем конструкторы
            foreach (var constructor in Constructors)
            {
                sb.AppendLine($"    {constructor.ToString()}");
            }

            // Добавляем методы
            foreach (var method in Methods)
            {
                sb.AppendLine($"    {method.ToString()}");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}