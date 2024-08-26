using System.IO;
using System.Linq;
using System.Windows;
using Library;
using Library.Models;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;
using System.Collections.Generic;

namespace AssemblyToAPI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string selectedItem = ComboBox.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Please select an option from the combobox.");
                return;
            }

            string path = selectedItem.Contains("ALL") ? GetPathOpenFolderDialog() : GetPathOpenFileDialog();

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please select a valid file or folder.");
                return;
            }

            bool isDirectory = Directory.Exists(path);

            if (selectedItem.Contains("API"))
            {
                ProcessApiFiles(path, isDirectory);
            }
            else if (selectedItem.Contains("HOOKS"))
            {
                ProcessHookFiles(path, isDirectory);
            }
        }

        private void ProcessApiFiles(string path, bool isDirectory)
        {
            string[] files = isDirectory
                ? Directory.GetFiles(path, "*.dll").Where(file => !file.EndsWith(".txt")).ToArray()
                : new[] { path };

            foreach (var file in files)
            {
                try
                {
                    var text = AssemblyDataSerializer.ConvertToText(file);
                    TextBox.Text = text;
                    SaveToFile(file, text, ".txt");
                }
                catch
                {
                    MessageBox.Show($"Failed to process file: {file}");
                }
            }
        }

        private void ProcessHookFiles(string path, bool isDirectory)
        {
            string[] files = isDirectory
                ? Directory.GetFiles(path, "*.dll").Where(file => !file.EndsWith(".txt")).ToArray()
                : new[] { path };

            var allHooks = new List<HookModel>();

            foreach (var file in files)
            {
                var hooksDictionary = AssemblyDataSerializer.FindHooksDictionary(file);

                if (hooksDictionary.Count == 0) continue;

                allHooks.AddRange(hooksDictionary.Values);
                var jsonString = JsonConvert.SerializeObject(hooksDictionary);
                TextBox.Text = jsonString;
                SaveToFile(file, jsonString, "_hooks.json");
            }

            if (isDirectory)
            {
                var distinctHooks = allHooks.Distinct().ToList();
                var allHooksJson = JsonConvert.SerializeObject(distinctHooks);
                var folderPath = Path.GetDirectoryName(files.First());
                File.WriteAllText(Path.Combine(folderPath, "allhooks.json"), allHooksJson);
            }
        }

        private void SaveToFile(string filePath, string content, string extension)
        {
            var outputFileName = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath) + extension);
            File.WriteAllText(outputFileName, content);
        }

        private string GetPathOpenFileDialog()
        {
            var openFileDialog = new VistaOpenFileDialog { Filter = "Dll (*.dll)|*.dll" };
            return openFileDialog.ShowDialog() == true ? openFileDialog.FileName : string.Empty;
        }

        private string GetPathOpenFolderDialog()
        {
            var folderDialog = new VistaFolderBrowserDialog();
            return folderDialog.ShowDialog() == true ? folderDialog.SelectedPath : string.Empty;
        }
    }
}
