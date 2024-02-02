using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Models
{
    public class AssemblyModel
    {
        public List<TypeModel> Types { get; set; } = new List<TypeModel>();
    }
}
