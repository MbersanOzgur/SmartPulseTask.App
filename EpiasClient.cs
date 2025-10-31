using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SmartPulseTask.App
{
    public static class EpiasClient
    {
        private const string BaseUrl =
            "https://seffaflik.epias.com.tr/electricity-service/v1/markets/idm/data/transaction-history";

        public static async Task<string?> FetchTransactionDataAsync(string tgt)
        {
            try
            {
                // Load .env file manually (so START_DATE and END_DATE are available in env file for adjustments)
                string envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
                if (File.Exists(envPath))
                {
                    foreach (var line in File.ReadAllLines(envPath))
                    {
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                            continue;

                        var parts = line.Split('=', 2);
                        if (parts.Length == 2)
                            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                    }
                }

                // Read dates from env file
                string? startDateEnv = Environment.GetEnvironmentVariable("START_DATE");
                string? endDateEnv = Environment.GetEnvironmentVariable("END_DATE");

                string startDate, endDate;

                if (!string.IsNullOrEmpty(startDateEnv) && !string.IsNullOrEmpty(endDateEnv))
                {
                    startDate = $"{startDateEnv}+03:00";
                    endDate = $"{endDateEnv}+03:00";
                }
                else
                {
                    var today = DateTime.Now;
                    var yesterday = today.AddDays(-1);

                    startDate = $"{yesterday:yyyy-MM-dd}T00:00:00+03:00";
                    endDate = $"{today:yyyy-MM-dd}T00:00:00+03:00";
                }

                Console.WriteLine($"Start: {startDate}");
                Console.WriteLine($"End:   {endDate}\n");

                // Prepare JSON body 
                var jsonBody = $"{{\"startDate\":\"{startDate}\",\"endDate\":\"{endDate}\"}}";
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("TGT", tgt);
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                // Perform POST request
                var response = await client.PostAsync(BaseUrl, content);

                Console.WriteLine($"EPİAŞ response: {(int)response.StatusCode} {response.ReasonPhrase}");
                Console.WriteLine($"Queried range: {startDate} → {endDate}\n");

                if (!response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"HTTP Error: {response.StatusCode}");
                    Console.ResetColor();

                    string errBody = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errBody))
                        Console.WriteLine($"Response body:\n{errBody}\n");

                    return null;
                }

                string json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Received empty response.");
                    Console.ResetColor();
                    return null;
                }

                Console.WriteLine("Successfully fetched transaction data.\n");

                // Save response to files
                string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "data.json");
                await File.WriteAllTextAsync(jsonPath, json);
                Console.WriteLine($"Saved JSON response to: {jsonPath}");

                string csvPath = Path.Combine(Directory.GetCurrentDirectory(), "data.csv");
                await File.WriteAllTextAsync(csvPath, "RawData\n\"" + json.Replace("\"", "\"\"") + "\"");
                Console.WriteLine($"Saved CSV file to: {csvPath}");

                return json;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data: {ex.Message}");
                return null;
            }
        }
    }
}
