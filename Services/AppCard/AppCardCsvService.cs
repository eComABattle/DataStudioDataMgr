using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.AppCard
{
    /// <summary>
    /// CSV mapping configurations for AppCard data
    /// </summary>
    public class AppCardDataMap : ClassMap<AppCardData>
    {
        public AppCardDataMap()
        {
            Map(m => m.TransactionDate).Name("Transaction Date");
            Map(m => m.TotalClipCount).Name("Total Clip Count");
            Map(m => m.TotalRedemptionsCount).Name("Total Redemptions Count");
            Map(m => m.MerchantName).Name("Merchant Name");
            Map(m => m.ContentProviderName).Name("Content Provider Name");
            Map(m => m.DigitalOfferId).Name("Digital Offer ID");
            Map(m => m.DiscountAmount).Name("Discount Amount");
            Map(m => m.NumberOfUniqueRedeemers).Name("Number of Unique Redeemers");
            // SourceFileName is set programmatically, not from CSV
            Map(m => m.SourceFileName).Ignore();
        }
    }

    /// <summary>
    /// Service for processing AppCard CSV files using CsvHelper
    /// </summary>
    public class AppCardCsvService
    {
        /// <summary>
        /// Processes AppCard CSV file and returns list of AppCardData objects
        /// </summary>
        /// <param name="csvFilePath">Path to the CSV file or directory</param>
        /// <returns>List of AppCardData objects</returns>
        public async Task<List<AppCardData>> ProcessCsvAsync(string csvFilePath)
        {
            var data = new List<AppCardData>();

            try
            {
                Console.WriteLine($"Processing AppCard CSV files from: {csvFilePath}");

                List<string> csvFiles;

                // Check if path is a directory or a file
                if (Directory.Exists(csvFilePath))
                {
                    // Get all CSV files
                    csvFiles = Directory.EnumerateFiles(csvFilePath, "*.csv").ToList();
                }
                else if (File.Exists(csvFilePath))
                {
                    // Single file
                    csvFiles = new List<string> { csvFilePath };
                }
                else
                {
                    Console.WriteLine($"Path does not exist: {csvFilePath}");
                    return data;
                }

                if (!csvFiles.Any())
                {
                    Console.WriteLine("No CSV files found");
                    return data;
                }

                Console.WriteLine($"Found {csvFiles.Count} CSV files to process");

                foreach (string file in csvFiles)
                {
                    var fileName = Path.GetFileName(file);

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    };

                    using (var reader = new StreamReader(file))
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Context.RegisterClassMap<AppCardDataMap>();

                        // Configure multiple date formats globally for this CSV reader
                        var dateOptions = new TypeConverterOptions
                        {
                            Formats = new[] { "M/d/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/dd/yyyy", "MM/d/yyyy", "yyyy/MM/dd", "M/d/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss" }
                        };
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dateOptions);

                        var records = await Task.Run(() => csv.GetRecords<AppCardData>().ToList());
                        foreach (var record in records)
                        {
                            record.MerchantName = record.MerchantName?.Replace(",", " ");
                            record.ContentProviderName = record.ContentProviderName?.Replace(",", " ");
                            record.DigitalOfferId = record.DigitalOfferId?.Replace(",", " ");
                            record.SourceFileName = fileName;
                            data.Add(record);
                        }

                        Console.WriteLine($"Successfully processed {records.Count} records from file: {fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing AppCard CSV file: {ex.Message}");
                throw;
            }

            return data;
        }
    }
}

