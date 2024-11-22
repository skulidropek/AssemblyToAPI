using Library;
using Library.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        // Пути к старым девблогам
        var oldDevBlogPaths = new List<string>
        {
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\133 v1806\\133 v1806\\Managed",
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\177 v2013\\177 v2013\\Managed",
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\196 v2054\\196 v2054\\Managed",
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\199 v2081\\199 v2081\\Managed",
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\203\\203\\Managed",
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\207\\207\\Managed",
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\210\\210\\Managed",
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\220\\220\\Managed",
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\236\\236\\Managed",
            "C:\\Users\\legov\\Downloads\\Telegram Desktop\\266\\266\\Managed",
            "C:\\Users\\legov\\Downloads\\Managed (1)\\Managed",
            "C:\\Users\\legov\\Downloads\\Managed",
        };

        // Путь к последнему девблогу
        string lastDevBlogPath = "C:\\RustServer 2.0\\rustserver\\RustDedicated_Data\\Managed";

        // Кэшированные пути
        string cachePathOld = "old_hooks_cache.json";
        string cachePathNew = "new_hooks_cache.json";

        // Получение старых хуков из всех указанных папок
        var oldDevHooks = GetHooksFromMultipleFoldersWithCache(cachePathOld, oldDevBlogPaths);

        // Получение хуков последнего девблога
        var lastDevHooks = GetHooksFromFolderWithCache(cachePathNew, lastDevBlogPath);

        // Логи изменений
        LogHookDifferences(oldDevHooks, lastDevHooks);

        // Генерация файла анализа
        var outputFilePath = "analyze_configuration.json";
        GenerateCompactAnalyzeFile(oldDevHooks, lastDevHooks, outputFilePath);

        Console.WriteLine($"Файл анализа успешно создан: {outputFilePath}");
    }

    static Dictionary<string, HookModel> GetHooksFromMultipleFoldersWithCache(string cachePath, List<string> folderPaths)
    {
        if (File.Exists(cachePath))
        {
            // Загрузка данных из кеша
            var json = File.ReadAllText(cachePath);
            return JsonSerializer.Deserialize<Dictionary<string, HookModel>>(json);
        }
        else
        {
            var hooks = new Dictionary<string, HookModel>();

            foreach (var folderPath in folderPaths)
            {
                // Сбор всех DLL в каждой папке
                var dllFiles = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories);

                // Проходим по всем DLL и собираем хуки
                foreach (var dllFile in dllFiles)
                {
                    var fileHooks = AssemblyDataSerializer.FindHooksDictionary(dllFile);
                    foreach (var hook in fileHooks)
                    {
                        if (!hooks.ContainsKey(hook.Key))
                        {
                            hooks.Add(hook.Key, hook.Value);
                        }
                    }
                }
            }

            // Сохранение данных в кеш
            var json = JsonSerializer.Serialize(hooks);
            File.WriteAllText(cachePath, json);

            return hooks;
        }
    }

    static Dictionary<string, HookModel> GetHooksFromFolderWithCache(string cachePath, string folderPath)
    {
        if (File.Exists(cachePath))
        {
            // Загрузка данных из кеша
            var json = File.ReadAllText(cachePath);
            return JsonSerializer.Deserialize<Dictionary<string, HookModel>>(json);
        }
        else
        {
            // Сбор всех DLL в папке
            var dllFiles = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories);
            var hooks = new Dictionary<string, HookModel>();

            // Проходим по всем DLL и собираем хуки
            foreach (var dllFile in dllFiles)
            {
                var fileHooks = AssemblyDataSerializer.FindHooksDictionary(dllFile);
                foreach (var hook in fileHooks)
                {
                    if (!hooks.ContainsKey(hook.Key))
                    {
                        hooks.Add(hook.Key, hook.Value);
                    }
                }
            }

            // Сохранение данных в кеш
            var json = JsonSerializer.Serialize(hooks);
            File.WriteAllText(cachePath, json);

            return hooks;
        }
    }

    static void LogHookDifferences(Dictionary<string, HookModel> oldDevHooks, Dictionary<string, HookModel> lastDevHooks)
    {
        // Найти новые хуки (есть в новой сборке, но нет в старой)
        var addedHooks = lastDevHooks
            .Where(newHook => !oldDevHooks.Any(oldHook => oldHook.Value.HookName == newHook.Value.HookName))
            .ToList();

        // Найти удалённые хуки (были в старой сборке, но нет в новой)
        var removedHooks = oldDevHooks
            .Where(oldHook => !lastDevHooks.Any(newHook => newHook.Value.HookName == oldHook.Value.HookName))
            .ToList();

        // Найти изменённые хуки (с одинаковыми именами, но разными параметрами)
        var modifiedHooks = oldDevHooks
            .Where(oldHook => lastDevHooks.Any(newHook =>
                newHook.Value.HookName == oldHook.Value.HookName &&
                newHook.Value.HookParameters != oldHook.Value.HookParameters))
            .ToList();

        // Логи изменений
        Console.WriteLine("=== Added Hooks ===");
        foreach (var hook in addedHooks)
        {
            Console.WriteLine($"Hook: {hook.Value.HookName}, Class: {hook.Value.ClassName}, Method: {hook.Value.MethodName}");
        }

        Console.WriteLine("\n=== Removed Hooks ===");
        foreach (var hook in removedHooks)
        {
            Console.WriteLine($"Hook: {hook.Value.HookName}, Class: {hook.Value.ClassName}, Method: {hook.Value.MethodName}");
        }

        Console.WriteLine("\n=== Modified Hooks ===");
        foreach (var hook in modifiedHooks)
        {
            var newHook = lastDevHooks.First(h => h.Value.HookName == hook.Value.HookName);
            Console.WriteLine($"Hook: {hook.Value.HookName}");
            Console.WriteLine($"  Old Parameters: {hook.Value.HookParameters}");
            Console.WriteLine($"  New Parameters: {newHook.Value.HookParameters}");
        }
    }

    static void GenerateCompactAnalyzeFile(Dictionary<string, HookModel> oldDevHooks, Dictionary<string, HookModel> lastDevHooks, string outputPath)
    {
        // Словарь для хранения результата
        var diagnostics = new Dictionary<string, string>();

        // Обработка старых хуков (удалённые или изменённые)
        foreach (var oldHook in oldDevHooks)
        {
            var matchingHook = lastDevHooks.Values.FirstOrDefault(newHook =>
                newHook.HookName == oldHook.Value.HookName);

            if (matchingHook != null)
            {
                if ($"{oldHook.Value.HookName}{oldHook.Value.HookParameters}" != $"{matchingHook.HookName}{matchingHook.HookParameters}")
                {
                    // Проверяем, изменились ли параметры
                    if (matchingHook.HookParameters != oldHook.Value.HookParameters)
                    {
                        diagnostics[$"{oldHook.Value.HookName}{oldHook.Value.HookParameters}"] = $"{matchingHook.HookName}{matchingHook.HookParameters}";
                    }
                    else
                    {
                        diagnostics[$"{oldHook.Value.HookName}{oldHook.Value.HookParameters}"] = $"{matchingHook.HookName}{matchingHook.HookParameters}";
                    }
                }
                
            }
            else
            {
                // Хук был удалён
                diagnostics[$"{oldHook.Value.HookName}{oldHook.Value.HookParameters}"] = string.Empty;
            }
        }

        //// Обработка новых хуков, которые не были в старой версии
        //foreach (var newHook in lastDevHooks)
        //{
        //    if (!oldDevHooks.Values.Any(oldHook => oldHook.HookName == newHook.Value.HookName))
        //    {
        //        diagnostics[newHook.Value.HookName] = newHook.Value.HookName;
        //    }
        //}

        // Формирование результата
        var result = new
        {
            ConfigurationName = "AnalyzeConfigurationServiceLastDevBlog",
            Diagnostics = diagnostics
        };

        // Сериализация в JSON
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(result, jsonOptions);

        // Сохранение файла
        File.WriteAllText(outputPath, json);
    }
}
