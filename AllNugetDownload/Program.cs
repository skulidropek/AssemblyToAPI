using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class NuGetPackage
{
    public string Id { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
}

public class NuGetResponse
{
    public int TotalHits { get; set; }
    public List<NuGetPackage> Data { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        // Укажи путь к папке, куда нужно сохранить файлы
        string outputFolder = @"C:\YourPath\NuGetPackages"; // Укажи свой путь для сохранения .nupkg
        string extractFolder = @"C:\YourPath\ExtractedPackages"; // Укажи свой путь для разархивирования

        // Создаем папки для сохранения и распаковки, если они не существуют
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        if (!Directory.Exists(extractFolder))
        {
            Directory.CreateDirectory(extractFolder);
        }

        // Максимум 20 пакетов за один запрос, так как API может быть ограничено
        int pageSize = 20;
        string baseUrl = $"https://api-v2v3search-0.nuget.org/query?q=&frameworks=net&prerelease=true&sortBy=relevance&pageSize={pageSize}";

        using (HttpClient client = new HttpClient())
        {
            bool hasMoreResults = true;
            int skip = 0;
            int totalHits = 0;
            int downloadedCount = 0;

            while (hasMoreResults)
            {
                // Каждый раз смещаем запрос на pageSize, чтобы загружать новые пакеты
                string requestUrl = $"{baseUrl}&skip={skip}";
                HttpResponseMessage response = await client.GetAsync(requestUrl);
                Console.WriteLine(requestUrl);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    NuGetResponse nugetResponse = JsonConvert.DeserializeObject<NuGetResponse>(jsonResponse);

                    // Устанавливаем количество найденных пакетов
                    if (totalHits == 0)
                    {
                        totalHits = nugetResponse.TotalHits;
                        Console.WriteLine($"Всего пакетов найдено: {totalHits}");
                    }

                    // Сохраняем и распаковываем каждый пакет
                    foreach (var package in nugetResponse.Data)
                    {
                        await DownloadAndExtractNugetPackage(package, outputFolder, extractFolder);
                        downloadedCount++;
                    }

                    // Если пакетов меньше, чем pageSize, значит, больше страниц нет
                    if (nugetResponse.Data.Count < pageSize)
                    {
                        hasMoreResults = false;
                    }
                    else
                    {
                        skip += pageSize; // Переходим к следующей странице
                    }
                }
                else
                {
                    Console.WriteLine($"Ошибка при получении данных: {response.StatusCode}");
                    break;
                }
            }

            Console.WriteLine($"Всего скачано пакетов: {downloadedCount} из {totalHits}");
        }
    }

    // Метод для скачивания и распаковки NuGet пакета
    static async Task DownloadAndExtractNugetPackage(NuGetPackage package, string outputFolder, string extractFolder)
    {
        string downloadUrl = $"https://www.nuget.org/api/v2/package/{package.Id}/{package.Version}";
        string fileName = $"{package.Id}.{package.Version}.nupkg"; // Имя файла для сохранения пакета
        string filePath = Path.Combine(outputFolder, fileName); // Полный путь к файлу

        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(downloadUrl);
                if (response.IsSuccessStatusCode)
                {
                    // Скачиваем пакет и сохраняем его в файл
                    byte[] packageData = await response.Content.ReadAsByteArrayAsync();
                    await File.WriteAllBytesAsync(filePath, packageData);
                    Console.WriteLine($"Скачан пакет: {package.Id} версия {package.Version} в файл {filePath}");

                    // Разархивируем скачанный файл .nupkg
                    string packageExtractFolder = Path.Combine(extractFolder, $"{package.Id}.{package.Version}");
                    if (!Directory.Exists(packageExtractFolder))
                    {
                        Directory.CreateDirectory(packageExtractFolder);
                    }

                    ZipFile.ExtractToDirectory(filePath, packageExtractFolder);
                    Console.WriteLine($"Пакет {package.Id} версия {package.Version} успешно разархивирован в {packageExtractFolder}");
                }
                else
                {
                    Console.WriteLine($"Ошибка скачивания пакета {package.Id} версии {package.Version}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }
}
