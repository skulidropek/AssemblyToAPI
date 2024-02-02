using Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Extensions
{
    public static class AssemblyModelExtensions
    {
        public static TypeModel Search(this AssemblyModel assemblyModel, string name)
        {
            foreach(var type in assemblyModel.Types)
            {
                if(type.ClassName == name)
                {
                    return type;
                }

                foreach(var propertyModel in type.Properties)
                {
                    if(propertyModel.PropertyName == name)
                    {
                        return type;
                    }
                } 
                
                foreach(var propertyModel in type.Fields)
                {
                    if(propertyModel.FieldName == name)
                    {
                        return type;
                    }
                } 
                
                foreach(var propertyModel in type.Methods)
                {
                    if(propertyModel.MethodName == name)
                    {
                        return type;
                    }
                }
            }

            return null;
        }
    }
}
