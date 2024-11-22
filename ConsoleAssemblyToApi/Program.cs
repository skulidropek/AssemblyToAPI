using Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class Program
{
    public class NugetLibrary
    {
        public string LibraryName { get; set; }   // Путь до папки, где лежит .dll
        public string DllPath { get; set; }       // Полный путь к .dll файлу
    }

    public static async Task Main()
    {
        // Путь к папке NuGet пакетов
        string nugetPath = @"C:\home\bfday\.nuget\packages";

        // Путь для сохранения результатов в GitHub\LibrariesTxt
        string outputDirectory = @"C:\Users\legov\OneDrive\Documents\GitHub\LibrariesTxt";

        // Проверка существования папки с NuGet пакетами
        if (!Directory.Exists(nugetPath))
        {
            Console.WriteLine($"Папка {nugetPath} не существует.");
            return;
        }

        // Проверка или создание папки для сохранения текстовых файлов
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Получаем все библиотеки с их .dll файлами
        foreach (var library in TraverseDirectory(@"C:\YourPath\packages"))
        {
            // Генерация структуры папок для сохранения результатов
            string libraryDirectory = Path.Combine(outputDirectory, library.LibraryName);
            if (!Directory.Exists(libraryDirectory))
            {
                Directory.CreateDirectory(libraryDirectory);
            }

            string outputFile = Path.Combine(libraryDirectory, Path.GetFileNameWithoutExtension(library.DllPath) + ".dll.txt");

            // Если файл уже существует, пропускаем
            if (File.Exists(outputFile))
            {
                Console.WriteLine("Скипаю потому что файл уже существует " + outputFile);
                continue;
            }

            // Запуск обработки в отдельном потоке с использованием Task.Run
            try
            {
                var text = await Task.Run(() =>
                {
                    try
                    {
                        Console.WriteLine("Обрабатываю " + outputFile + " из " + library.DllPath);
                        return AssemblyDataSerializer.ConvertToText(library.DllPath);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("Ошибка " + ex.Message);   
                    }

                    return "";
                });

                // Если текст пустой, пропускаем
                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine("Файл пустой " + library.DllPath);
                    continue;
                }

                // Сохраняем текст в файл
                File.WriteAllText(outputFile, text);
                Console.WriteLine($"Текст сохранён в: {outputFile}");
            }
            catch (Exception ex)
            {
                // Обработка исключений, которые произошли во время выполнения задачи
                Console.WriteLine($"Ошибка при обработке {library.DllPath}: {ex.Message}");
            }
        }
    }

    // Метод, который возвращает IEnumerable для ленивого перечисления
    public static IEnumerable<NugetLibrary> TraverseDirectory(string rootDirectory)
    {
        // Проход по всем подкаталогам
        foreach (var directory in Directory.GetDirectories(rootDirectory))
        {
            // Ищем файлы .dll во всех подкаталогах
            foreach (var file in Directory.GetFiles(directory, "*.dll", SearchOption.AllDirectories))
            {
                // Формируем полный путь от папки packages до файла
                string libraryName = GetRelativePathFromPackages(directory, file);

                // Возвращаем найденную библиотеку с путём до папки
                yield return new NugetLibrary
                {
                    LibraryName = libraryName,
                    DllPath = file
                };
            }

            // Рекурсивно обрабатываем подкаталоги
            foreach (var lib in TraverseDirectory(directory))
            {
                yield return lib;
            }
        }
    }

    // Метод для получения относительного пути от папки packages до файла
    public static string GetRelativePathFromPackages(string directory, string filePath)
    {
        // Находим индекс папки "packages" в пути
        int index = filePath.IndexOf("packages");
        if (index >= 0)
        {
            // Извлекаем часть пути, начиная с "packages"
            string relativePath = filePath.Substring(index + "packages".Length + 1);

            // Разделяем путь на части
            string[] parts = relativePath.Split(Path.DirectorySeparatorChar);

            // Первая часть — это имя пакета и версия
            if (parts.Length >= 2)
            {
                // Имя пакета и версия, например: Aardvark.Base.5.3.4
                string fullName = parts[0];
                var parsedPath = ParseLibraryName(fullName);

                // Склеиваем оставшуюся часть пути, начиная с позиции 1 (чтобы исключить имя пакета и версию)
                string remainingPath = string.Join(Path.DirectorySeparatorChar.ToString(), parts, 1, parts.Length - 2); // -2 для удаления имени файла .dll

                // Строим новый путь: имя пакета (в нижнем регистре) + версия + оставшаяся часть пути
                string directoryPath = Path.Combine(parsedPath.LibraryName, parsedPath.Version, remainingPath);
                return directoryPath;
            }
        }
        return directory; // Если "packages" не найден, возвращаем текущую папку
    }

    // Метод для парсинга имени пакета и версии
    public static (string LibraryName, string Version) ParseLibraryName(string fullName)
    {
        // Используем регулярное выражение для поиска последней версии (пример: Aardvark.Base.5.3.4)
        var match = Regex.Match(fullName, @"^(.*)\.(\d+\.\d+\.\d+)$");
        if (match.Success)
        {
            string libraryName = match.Groups[1].Value.ToLower().Replace('.', '-'); // Приводим имя пакета к нижнему регистру и заменяем точки на дефисы
            string version = match.Groups[2].Value;
            return (libraryName, version);
        }

        // Если не удалось найти версию, возвращаем исходное имя без изменений
        return (fullName.ToLower(), "");
    }
}
