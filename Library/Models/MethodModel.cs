using Library.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Library.Models
{
    public class MethodModel
    {
        public string MethodReturnType { get; set; }
        public string MethodName { get; set; }
        public MemberAccessibilityLevel Accessibility { get; set; }
        public List<ParameterModel> Parameters { get; set; } = new List<ParameterModel>();

        public bool IsStatic { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public bool IsAbstract { get; set; }
        public bool IsSealed { get; set; }

        // Поддержка атрибутов
        public List<AttributeModel> Attributes { get; set; } = new List<AttributeModel>();

        public override string ToString()
        {
            var staticModifier = IsStatic ? "static " : string.Empty;
            var virtualModifier = IsVirtual ? "virtual " : string.Empty;
            var overrideModifier = IsOverride ? "override " : string.Empty;
            var abstractModifier = IsAbstract ? "abstract " : string.Empty;
            var sealedModifier = IsSealed ? "sealed " : string.Empty;

            // Формируем строку с параметрами
            var parametersString = Parameters.Any()
                ? string.Join(", ", Parameters)
                : string.Empty;

            // Формируем строку для атрибутов
            var attributesString = Attributes.Any()
                ? string.Join(System.Environment.NewLine, Attributes.Select(attr => attr.ToString())) + System.Environment.NewLine
                : string.Empty;

            // Формируем сигнатуру метода
            var methodSignature = $"{Accessibility.ToAccessibilityString()} {staticModifier}{abstractModifier}{sealedModifier}{virtualModifier}{overrideModifier}{MethodReturnType} {MethodName}({parametersString})";

            return $"{attributesString}{methodSignature.Trim()};";
        }
    }
}
