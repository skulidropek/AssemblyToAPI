using Library.Extensions;

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

            // Обычный метод
            var methodSignature = $"{Accessibility.ToAccessibilityString()} {staticModifier}{abstractModifier}{sealedModifier}{virtualModifier}{overrideModifier}{MethodReturnType} {MethodName}({parametersString})";

            return methodSignature.Trim() + ";";
        }
    }
}
