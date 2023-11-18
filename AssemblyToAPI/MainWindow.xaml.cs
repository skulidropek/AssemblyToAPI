using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Mono.Cecil;
using Ookii.Dialogs.Wpf;

namespace AssemblyToAPI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string path = GetPathOpenFileDialog();

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Need to select a dll");
                return;
            }

            string assemblyPath = path;

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
            var sb = new StringBuilder();

            foreach (var type in assemblyDefinition.MainModule.Types)
            {
                sb.AppendLine($"Class: {type.FullName}");
                if (type.BaseType != null)
                {
                    sb.AppendLine($"Inherits from: {GetFriendlyTypeName(type.BaseType)}");
                }

                sb.AppendLine("Fields:");

                foreach (var field in type.Fields)
                {
                    sb.AppendLine($" {GetFriendlyTypeName(field.FieldType)} {field.Name}");
                }

                sb.AppendLine("Properties:");

                foreach (var property in type.Properties)
                {
                    sb.AppendLine($" {GetFriendlyTypeName(property.PropertyType)} {property.Name}");
                }

                sb.AppendLine("Methods:");

                foreach (var method in type.Methods)
                {
                    string methodSignature = $"{GetFriendlyTypeName(method.ReturnType)} {method.Name}(";
                    for (int i = 0; i < method.Parameters.Count; i++)
                    {
                        var parameter = method.Parameters[i];
                        methodSignature += $"{GetFriendlyTypeName(parameter.ParameterType)} {parameter.Name}";
                        if (i < method.Parameters.Count - 1)
                            methodSignature += ", ";
                    }
                    methodSignature += ")";
                    sb.AppendLine($" {methodSignature}");
                }

                sb.AppendLine(new string('-', 50));
            }

            TextBox.Text = sb.ToString();
        }

        static string GetFriendlyTypeName(TypeReference type)
        {
            switch (type.FullName)
            {
                case "System.Void": return "void";
                case "System.Int32": return "int";
                case "System.Single": return "float";
                case "System.Double": return "double";
                case "System.Decimal": return "decimal";
                case "System.Boolean": return "bool";
                case "System.Char": return "char";
                case "System.Byte": return "byte";
                case "System.SByte": return "sbyte";
                case "System.Int16": return "short";
                case "System.UInt16": return "ushort";
                case "System.Int64": return "long";
                case "System.UInt64": return "ulong";
                case "System.String": return "string";
                case "System.Object": return "object";
                // Add other conversions
                // ...
                default:
                    if (type.IsGenericInstance)
                    {
                        var genericType = (GenericInstanceType)type;
                        string genericTypeName = GetFriendlyTypeName(genericType.ElementType);
                        string genericArgs = string.Join(", ", genericType.GenericArguments.Select(GetFriendlyTypeName));
                        return $"{genericTypeName}<{genericArgs}>";
                    }

                    return type.Name;
            }
        }

        private string GetPathOpenFileDialog()
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();

            openFileDialog.Filter = "Dll (*.dll)|*.dll";

            if (openFileDialog.ShowDialog() == false)
            {
                return "";
            }

            return openFileDialog.FileName;
        }
    }
}