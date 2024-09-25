using Library;
using System;
using System.Collections.Generic;
using System.IO;

public class Program
{
    public class NugetLibrary
    {
        public string LibraryName { get; set; }   // Полный путь от папки packages до файла
        public string DllPath { get; set; }       // Полный путь к .dll файлу
    }

    public static void Main()
    {
        // Путь к папке NuGet пакетов
        string nugetPath = @"C:\Users\legov\.nuget\packages";

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
        foreach (var library in TraverseDirectory(nugetPath))
        {
            Console.WriteLine($"Библиотека: {library.LibraryName}");
            Console.WriteLine($"Путь к файлу: {library.DllPath}");

            try
            {
                // Читаем содержимое файла .dll и конвертируем в текст
                var text = AssemblyDataSerializer.ConvertToText(library.DllPath);

                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine("Файл пустой " + library.DllPath);
                    continue;
                }

                // Генерация структуры папок для сохранения результатов
                string libraryDirectory = outputDirectory + Path.Combine(outputDirectory, library.LibraryName);
                if (!Directory.Exists(libraryDirectory))
                {
                    Directory.CreateDirectory(libraryDirectory);
                }

                // Путь к текстовому файлу
                string outputFile = Path.Combine(libraryDirectory, Path.GetFileNameWithoutExtension(library.DllPath) + ".dll.txt");

                // Сохраняем текст в файл
                File.WriteAllText(outputFile, text);

                Console.WriteLine($"Текст сохранён в: {outputFile}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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

                // Возвращаем найденную библиотеку
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
            string relativePath = filePath.Substring(index).Replace("packages", "");

            // Убираем имя файла .dll из пути, возвращаем только путь к папке
            return Path.GetDirectoryName(relativePath);
        }
        return directory; // Если "packages" не найден, возвращаем текущую папку
    }
}
