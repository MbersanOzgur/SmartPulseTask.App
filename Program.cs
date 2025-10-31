﻿using System;
using System.IO;
using System.Threading.Tasks;
using SmartPulseTask.App;

class Program
{
    static async Task Main()
    {
        LoadEnvFile(".env");

        string? username = Environment.GetEnvironmentVariable("EPIAS_USERNAME");
        string? password = Environment.GetEnvironmentVariable("EPIAS_PASSWORD");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Please make sure EPIAS_USERNAME and EPIAS_PASSWORD are set in the .env file.");
            return;
        }

        string? tgt = await AuthService.GetTGTAsync(username, password);
        if (string.IsNullOrEmpty(tgt))
        {
            Console.WriteLine("Failed to obtain TGT.");
            return;
        }

        Console.WriteLine($"TGT retrieved: {tgt}\n");

        var json = await EpiasClient.FetchTransactionDataAsync(tgt);

        if (!string.IsNullOrEmpty(json))
        {
            Console.WriteLine("Transaction data fetched successfully!\n");

            // Process transactions 
            TransactionProcessor.ProcessTransactions(json);
        }
        else
        {
            Console.WriteLine("Failed to fetch data.");
        }
    }

    static void LoadEnvFile(string path)
    {
        if (!File.Exists(path)) return;

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            var parts = line.Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
