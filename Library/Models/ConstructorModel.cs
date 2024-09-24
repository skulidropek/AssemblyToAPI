using Library.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Library.Models
{
    public class ConstructorModel
    {
        public string ConstructorName { get; set; }
        public MemberAccessibilityLevel Accessibility { get; set; }
        public List<ParameterModel> Parameters { get; set; } = new List<ParameterModel>();
        public bool IsStatic { get; set; }

        // Поддержка атрибутов
        public List<AttributeModel> Attributes { get; set; } = new List<AttributeModel>();

        public override string ToString()
        {
            var staticModifier = IsStatic ? "static " : string.Empty;

            // Формируем строку с параметрами
            var parametersString = Parameters.Any()
                ? string.Join(", ", Parameters)
                : string.Empty;

            // Формируем строку для атрибутов
            var attributesString = Attributes.Any()
                ? string.Join(System.Environment.NewLine, Attributes.Select(attr => attr.ToString())) + System.Environment.NewLine
                : string.Empty;

            // Формируем сигнатуру конструктора
            return $"{attributesString}{Accessibility.ToAccessibilityString()} {staticModifier}{ConstructorName}({parametersString});";
        }
    }
}
