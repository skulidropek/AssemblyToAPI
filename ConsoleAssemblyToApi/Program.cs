using Library;
using Library.Models;
using System.Text.Json;
using ConsoleAssemblyToApi.Models;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class Program
{
    public static async Task Main()
    {
        ProcessHookFiles("/home/user/rust_server/RustDedicated_Data/Managed/");    
    }

    private static void ProcessHookFiles(string path)
    {
        bool isDirectory = Directory.Exists(path);

        string[] files = isDirectory
            ? Directory.GetFiles(path, "*.dll").Where(file => !file.EndsWith(".txt")).ToArray()
            : new[] { path };

        var allHooks = new List<HookImplementationModel>();

        foreach (var file in files)
        {
            {
                var hooksDictionary = AssemblyDataSerializer.FindHooksDictionary(file);

                if (hooksDictionary.Count == 0) continue;

                var implementationModels = hooksDictionary.Values.Select(hook => new HookImplementationModel
                {
                    HookSignature = hook.HookSignature,
                    MethodSignature = hook.MethodSignature,
                    MethodSourceCode = hook.MethodCode,
                    MethodClassName = hook.ClassName,
                    HookLineInvoke = hook.LineNumber
                });

                allHooks.AddRange(implementationModels);
            }
        }

        if (isDirectory)
        {
            var distinctHooks = allHooks.Distinct().ToList();
            var allHooksJson = JsonSerializer.Serialize(distinctHooks, new JsonSerializerOptions { WriteIndented = true });
            var folderPath = Path.GetDirectoryName(files.First());
            if (folderPath != null)
            {
                File.WriteAllText(Path.Combine(folderPath, "allhooks.json"), allHooksJson);
            }
        }
    }
}