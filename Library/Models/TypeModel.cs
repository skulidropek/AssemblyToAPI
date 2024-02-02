using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Models
{
    public class TypeModel
    {
        public string ClassName { get; set; }
        public string InheritsFrom { get; set; }
        public List<FieldModel> Fields { get; set; } = new List<FieldModel>();
        public List<PropertyModel> Properties { get; set; } = new List<PropertyModel>();
        public List<MethodModel> Methods { get; set; } = new List<MethodModel>();
    }
}
