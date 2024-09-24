using Library.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Models
{
    public class ConstructorModel
    {
        public string ConstructorName { get; set; }
        public TypeAccessibilityLevel Accessibility { get; set; }
        public List<ParameterModel> Parameters { get; set; } = new List<ParameterModel>();
        public bool IsStatic { get; set; }

        public override string ToString()
        {
            var staticModifier = IsStatic ? "static " : string.Empty;

            // Формируем строку с параметрами
            var parametersString = Parameters.Any()
                ? string.Join(", ", Parameters)
                : string.Empty;

            // Формируем сигнатуру конструктора
            return $"{Accessibility.ToAccessibilityString()} {staticModifier}{ConstructorName}({parametersString});";
        }
    }
}
