using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace SmartPulseTask.App
{
    public static class TransactionProcessor
    {
        public static void ProcessTransactions(string json)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<TransactionHistoryResponse>(json, options);

                var items = data?.Items;

                if (items == null || items.Count == 0)
                {
                    Console.WriteLine("No transaction data found.");
                    return;
                }

                var grouped = items
                    .Where(i => !string.IsNullOrEmpty(i.ContractName))
                    .GroupBy(i => i.ContractName!)
                    .Select(g =>
                    {
                        decimal totalQuantity = g.Sum(x => x.Quantity / 10);
                        decimal totalValue = g.Sum(x => (x.Price * x.Quantity) / 10);
                        decimal weightedAverage = totalQuantity > 0 ? totalValue / totalQuantity : 0;

                        string contract = g.Key;
                        string prettyDate = "-";
                        string prettyHour = "-";

                        if (contract.StartsWith("PH") && contract.Length >= 10)
                        {
                            string datePart = contract.Substring(2, 8);
                            if (DateTime.TryParseExact(datePart, "yyMMddHH",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTs))
                            {
                                prettyDate = parsedTs.ToString("dd/MM/yyyy");
                                prettyHour = parsedTs.ToString("HH:00");
                            }
                        }

                        return new
                        {
                            ContractName = contract,
                            Date = prettyDate,
                            Hour = prettyHour,
                            TotalQuantity = totalQuantity,
                            TotalValue = totalValue,
                            WeightedPrice = weightedAverage
                        };
                    })
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.Hour)
                    .ToList();

                Console.WriteLine($"Processed {grouped.Count} contract groups.\n");

                Console.WriteLine(
                    $"{"Contract",-12} {"Date",-12} {"Hour",-8} {"Total Quantity",-15} {"Total Value",-18} {"Weighted Avg",-15}");
                Console.WriteLine(new string('-', 90));

                // Print results
                foreach (var g in grouped)
                {
                    Console.WriteLine(
                        $"{g.ContractName,-12} {g.Date,-12} {g.Hour,-8} {g.TotalQuantity,12:N2} {g.TotalValue,18:N2} {g.WeightedPrice,15:N2}");
                }

                Console.WriteLine();
                Console.WriteLine("Full grouped results were saved to results.csv\n");

                // Export full grouped results to CSV file
                var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "results.csv");
                var sb = new StringBuilder();
                sb.AppendLine("Contract,Date,Hour,TotalQuantity,TotalValue,WeightedAveragePrice");

                foreach (var g in grouped)
                {
                    sb.AppendLine($"{g.ContractName},{g.Date},{g.Hour},{g.TotalQuantity:N2},{g.TotalValue:N2},{g.WeightedPrice:N2}");
                }

                File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing transactions: {ex.Message}");
            }
        }
    }
}
