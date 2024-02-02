using System.IO;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Windows;
using Library;
using Microsoft.Win32;
using Mono.Cecil;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using static System.Net.Mime.MediaTypeNames;

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
            string selectedItem = ComboBox.SelectedItem.ToString();

            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Need to select a combobox");
                return;
            }

            string path = GetPathOpenFileDialog();

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Need to select a dll");
                return;
            }

            string assemblyPath = path;

            if(selectedItem.Contains("API"))
            {
                var text = AssemblyDataSerializer.ConvertToText(assemblyPath);
                TextBox.Text = text;
                return;
            }

            if(selectedItem.Contains("HOOKS"))
            {
                var json = AssemblyDataSerializer.FindHooks(assemblyPath);
                TextBox.Text = JsonConvert.SerializeObject(json);
                return;
            }
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