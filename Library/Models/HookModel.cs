using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Models
{
    public class HookModel
    {
        public string Name { get; set; }
        public string Parameters { get; set; }

        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string MethodCode { get; set; }
    }
}
