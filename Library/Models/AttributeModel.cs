using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Models
{
    public class AttributeModel
    {
        public string Name { get; set; }
        public List<string> Arguments { get; set; } = new List<string>();

        public override string ToString()
        {
            // Формируем строку атрибута с аргументами, если они есть
            var argsString = Arguments.Any() ? $"(\"{string.Join("\", \"", Arguments)}\")" : string.Empty;
            return $"[{Name}{argsString}]";
        }
    }
}
