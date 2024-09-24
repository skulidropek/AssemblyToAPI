namespace Library.Models
{
    public class ParameterModel
    {
        public string ParameterType { get; set; }
        public string ParameterName { get; set; }

        public override string ToString()
        {
            return $"{ParameterType} {ParameterName}";
        }
    }
}
