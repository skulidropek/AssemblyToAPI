using Library.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Library.Models
{
    internal class RustMethodModel : MethodModel
    {
        public List<HookModel> Hooks { get; set; }

        public RustMethodModel(MethodModel method, List<HookModel> hooks)
        {
            MethodReturnType = method.MethodReturnType;
            MethodName = method.MethodName;
            Accessibility = method.Accessibility;
            Parameters = method.Parameters != null ? new List<ParameterModel>(method.Parameters) : new List<ParameterModel>();
            IsStatic = method.IsStatic;
            IsVirtual = method.IsVirtual;
            IsOverride = method.IsOverride;
            IsAbstract = method.IsAbstract;
            IsSealed = method.IsSealed;
            Attributes = method.Attributes != null ? new List<AttributeModel>(method.Attributes) : new List<AttributeModel>();

            Hooks = hooks;
        }

        public override string ToString()
        {
            var hook = Hooks.FirstOrDefault();
            if(hook == null)
            {
                return base.ToString();
            }

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

            return hook.MethodCode;
        }
    }
}
