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

namespace DataStudioDataMgr.Services.ShopToCook
{
    /// <summary>
    /// Custom type converter for integers that handles comma-separated numbers
    /// </summary>
    public class IntegerConverterWithCommas : Int32Converter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            // Remove commas from the text before conversion
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Replace(",", "");
            }

            // Use base converter for the cleaned value
            return base.ConvertFromString(text, row, memberMapData);
        }
    }

    /// <summary>
    /// CSV mapping configurations for ShopToCook data
    /// </summary>
    public class ShopToCookEmailMap : ClassMap<ShopToCookEmail>
    {
        public ShopToCookEmailMap()
        {
            Map(m => m.Email).Name("Email");
            Map(m => m.Date).Name("Date");
            // Use custom converter for Sent and Opened to handle comma-separated numbers
            Map(m => m.Sent).Name("Sent").TypeConverter<IntegerConverterWithCommas>();
            Map(m => m.Opened).Name("Opened").TypeConverter<IntegerConverterWithCommas>();
            // SourceFileName is set programmatically, not from CSV
            Map(m => m.SourceFileName).Ignore();
        }
    }

    public class ShopToCookWebMap : ClassMap<ShopToCookWeb>
    {
        public ShopToCookWebMap()
        {
            Map(m => m.Date).Name("Date");
            Map(m => m.Order).Name("Order");
            Map(m => m.TotalImpressions).Name("Total impressions");
            Map(m => m.TotalClicks).Name("Total clicks");
            // SourceFileName is set programmatically, not from CSV
            Map(m => m.SourceFileName).Ignore();
        }
    }

    public class ShopToCookKioskMap : ClassMap<ShopToCookKiosk>
    {
        public ShopToCookKioskMap()
        {
            Map(m => m.Feature).Name("Feature");
            Map(m => m.Date).Name("Date");
            Map(m => m.FeatureCount).Name("Feature Count");
            // SourceFileName is set programmatically, not from CSV
            Map(m => m.SourceFileName).Ignore();
        }
    }

    /// <summary>
    /// Service for processing ShopToCook CSV files using CsvHelper
    /// </summary>
    public class ShopToCookCsvService
    {
        /// <summary>
        /// Processes email CSV file and returns list of ShopToCookEmail objects
        /// </summary>
        /// <param name="csvFilePath">Path to the email CSV file</param>
        /// <returns>List of ShopToCookEmail objects</returns>
        public async Task<List<ShopToCookEmail>> ProcessEmailCsvAsync(string csvFilePath)
        {
            var emails = new List<ShopToCookEmail>();

            try
            {
                Console.WriteLine($"Processing email CSV files from directory: {csvFilePath}");

                // Filter for email-related CSV files only
                var emailFiles = Directory.EnumerateFiles(csvFilePath, "*.csv")
                    .Where(f => Path.GetFileName(f).ToLower().StartsWith("emaildata_"))
                    .ToList();

                if (!emailFiles.Any())
                {
                    Console.WriteLine("No email CSV files found in directory");
                    return emails;
                }

                Console.WriteLine($"Found {emailFiles.Count} email CSV files to process");

                foreach (string file in emailFiles)
                {

                    //var clientToken = "AWG";
                    var fileName = Path.GetFileName(file);

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    };


                    //List<ShopToCookEmail> records = new List<ShopToCookEmail>();
                    //var records = await Task.Run(() => csv.GetRecords<ShopToCookWeb>().ToList());

                    using (var reader = new StreamReader(file.ToString()))
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Context.RegisterClassMap<ShopToCookEmailMap>();
                        
                        // Configure multiple date formats globally for this CSV reader
                        var dateOptions = new TypeConverterOptions { 
                            Formats = new[] { "M/d/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/dd/yyyy", "MM/d/yyyy" } 
                        };
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dateOptions);
                        
                        var records = await Task.Run(() => csv.GetRecords<ShopToCookEmail>().ToList());
                        foreach (var record in records)
                        {
                            //record.ClientToken = clientToken;
                            record.Email = record.Email?.Replace(",", " ");
                            record.SourceFileName = fileName;
                            emails.Add(record);
                        }

                        Console.WriteLine($"Successfully processed {records.Count} email records from file: {fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing email CSV file: {ex.Message}");
                throw;
            }

            return emails;
        }

        /// <summary>
        /// Processes web CSV file and returns list of ShopToCookWeb objects
        /// </summary>
        /// <param name="csvFilePath">Path to the web CSV file</param>
        /// <returns>List of ShopToCookWeb objects</returns>
        public async Task<List<ShopToCookWeb>> ProcessWebCsvAsync(string csvFilePath)
        {
            var webData = new List<ShopToCookWeb>();

            try
            {
                Console.WriteLine($"Processing web CSV files from directory: {csvFilePath}");

                // Filter for web-related CSV files only
                var webFiles = Directory.EnumerateFiles(csvFilePath, "*.csv")
                    .Where(f => Path.GetFileName(f).ToLower().StartsWith("websitedata_"))
                    .ToList();

                if (!webFiles.Any())
                {
                    Console.WriteLine("No web CSV files found in directory");
                    return webData;
                }

                Console.WriteLine($"Found {webFiles.Count} web CSV files to process");

                foreach (string file in webFiles)
                {
                    //var clientToken = "AWG";
                    var fileName = Path.GetFileName(file);

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    };

                    using (var reader = new StreamReader(file.ToString()))
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Context.RegisterClassMap<ShopToCookWebMap>();
                        
                        // Configure multiple date formats globally for this CSV reader
                        var dateOptions = new TypeConverterOptions { 
                            Formats = new[] { "M/d/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/dd/yyyy", "MM/d/yyyy" } 
                        };
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dateOptions);

                        var records = await Task.Run(() => csv.GetRecords<ShopToCookWeb>().ToList());
                        foreach (var record in records)
                        {
                            //record.ClientToken = clientToken;
                            record.Order = record.Order?.Replace(",", " ");
                            record.SourceFileName = fileName;
                            webData.Add(record);
                        }

                        Console.WriteLine($"Successfully processed {records.Count} web records from file: {fileName}");
                    } 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing web CSV file: {ex.Message}");
                throw;
            }

            return webData;
        }

        /// <summary>
        /// Processes kiosk CSV file and returns list of ShopToCookKiosk objects
        /// </summary>
        /// <param name="csvFilePath">Path to the kiosk CSV file</param>
        /// <returns>List of ShopToCookKiosk objects</returns>
        public async Task<List<ShopToCookKiosk>> ProcessKioskCsvAsync(string csvFilePath)
        {
            var kioskData = new List<ShopToCookKiosk>();

            try
            {
                Console.WriteLine($"Processing kiosk CSV files from directory: {csvFilePath}");

                // Filter for kiosk-related CSV files only
                var kioskFiles = Directory.EnumerateFiles(csvFilePath, "*.csv")
                    .Where(f => Path.GetFileName(f).ToLower().StartsWith("kioskdata_"))
                    .ToList();

                if (!kioskFiles.Any())
                {
                    Console.WriteLine("No kiosk CSV files found in directory");
                    return kioskData;
                }

                Console.WriteLine($"Found {kioskFiles.Count} kiosk CSV files to process");

                foreach (string file in kioskFiles)
                {
                    var clientToken = "AWG";
                    var fileName = Path.GetFileName(file);

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    };

                    using (var reader = new StreamReader(file.ToString()))
                        using (var csv = new CsvReader(reader, config))
                        {
                            csv.Context.RegisterClassMap<ShopToCookKioskMap>();
                            
                            // Configure multiple date formats globally for this CSV reader
                            var dateOptions = new TypeConverterOptions { 
                                Formats = new[] { "M/d/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/dd/yyyy", "MM/d/yyyy" } 
                            };
                            csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dateOptions);
                            
                            var records = await Task.Run(() => csv.GetRecords<ShopToCookKiosk>().ToList());
                            foreach (var record in records)
                            {
                                //record.ClientToken = clientToken;
                                record.Feature = record.Feature?.Replace(",", " ");
                                record.SourceFileName = fileName;
                                kioskData.Add(record);
                            }

                            Console.WriteLine($"Successfully processed {records.Count} kiosk records from file: {fileName}");
                        }
                } 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing kiosk CSV file: {ex.Message}");
                throw;
            }

            return kioskData;
        }

        /// <summary>
        /// Processes any CSV file and returns records based on file name detection
        /// </summary>
        /// <param name="csvFilePath">Path to the CSV file</param>
        /// <returns>Tuple containing the processed data and type</returns>
        public async Task<(object data, string type)> ProcessCsvFileAsync(string csvFilePath)
        {
            var fileName = Path.GetFileName(csvFilePath).ToLower();

            // Handle new naming convention with subdirectory prefixes
            if (fileName.StartsWith("emaildata_") || fileName.Contains("email"))
            {
                var emailData = await ProcessEmailCsvAsync(csvFilePath);
                return (emailData, "email");
            }
            else if (fileName.StartsWith("websitedata_") || fileName.Contains("web"))
            {
                var webData = await ProcessWebCsvAsync(csvFilePath);
                return (webData, "web");
            }
            else if (fileName.StartsWith("kioskdata_") || fileName.Contains("kiosk"))
            {
                var kioskData = await ProcessKioskCsvAsync(csvFilePath);
                return (kioskData, "kiosk");
            }
            else
            {
                throw new ArgumentException($"Unknown file type: {fileName}. Expected file to contain 'email', 'web', or 'kiosk' in the name, or start with 'EmailData_', 'WebsiteData_', or 'KioskData_'.");
            }
        }

        /// <summary>
        /// Validates CSV file structure without processing all records
        /// </summary>
        /// <param name="csvFilePath">Path to the CSV file</param>
        /// <returns>True if file structure is valid</returns>
        public async Task<bool> ValidateCsvFileAsync(string csvFilePath)
        {
            try
            {
                Console.WriteLine($"Validating CSV file: {csvFilePath}");

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null
                };

                using (var reader = new StreamReader(csvFilePath))
                using (var csv = new CsvReader(reader, config))
                {
                    // Try to read header
                    await Task.Run(() => csv.Read());
                    csv.ReadHeader();

                    // Try to read first data row
                    await Task.Run(() => csv.Read());

                    Console.WriteLine($"CSV file validation successful: {Path.GetFileName(csvFilePath)}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CSV file validation failed for {Path.GetFileName(csvFilePath)}: {ex.Message}");
                return false;
            }
        }
    }
}