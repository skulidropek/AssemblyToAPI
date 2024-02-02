namespace Library.Models
{
    public class MethodModel
    {
        public string MethodReturnType { get; set; }
        public string MethodName { get; set; }
        public List<ParameterModel> Parameters { get; set; } = new List<ParameterModel>();
    }
}
