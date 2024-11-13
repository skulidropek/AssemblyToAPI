using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Models
{
    public class HookModel
    {
        public string HookName { get; set; }
        public string HookParameters { get; set; }

        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string MethodCode { get; set; }
        public List<ParameterModel> MethodParameters { get; set; } = new List<ParameterModel>();
    }
}
