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

namespace DataStudioDataMgr.Services.BrData
{
    /// <summary>
    /// Custom type converter for nullable decimals that handles "NULL" strings
    /// </summary>
    public class NullableDecimalConverter : ITypeConverter
    {
        private readonly DecimalConverter _decimalConverter = new DecimalConverter();

        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            // Handle NULL strings and empty values
            if (string.IsNullOrWhiteSpace(text) || 
                text.Equals("NULL", StringComparison.OrdinalIgnoreCase) || 
                text.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Use decimal converter for valid decimal values
            try
            {
                return _decimalConverter.ConvertFromString(text, row, memberMapData);
            }
            catch
            {
                // If conversion fails, return null for nullable type
                return null;
            }
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return _decimalConverter.ConvertToString(value, row, memberMapData);
        }
    }

    /// <summary>
    /// CSV mapping configuration for BrData POS data
    /// Note: Column names ignore "[" and "]" brackets, and UPC appears twice (second occurrence is ignored)
    /// </summary>
    public class BrDataPosMap : ClassMap<BrDataPos>
    {
        public BrDataPosMap()
        {
            Map(m => m.Store).Name("Store");
            Map(m => m.UPC).Name("UPC");
            Map(m => m.Date).Name("Date");
            Map(m => m.Priority).Name("Priority");
            
            // Configure nullable decimal fields with custom converter to handle NULL strings
            Map(m => m.QtySold).Name("QtySold").TypeConverter<NullableDecimalConverter>();
            Map(m => m.AmountSold).Name("AmountSold").TypeConverter<NullableDecimalConverter>();
            Map(m => m.WgtSold).Name("WgtSold").TypeConverter<NullableDecimalConverter>();
            Map(m => m.UnitCost).Name("UnitCost").TypeConverter<NullableDecimalConverter>();
            Map(m => m.NetUnitCost).Name("NetUnitCost").TypeConverter<NullableDecimalConverter>();
            Map(m => m.PrcQty).Name("PrcQty").TypeConverter<NullableDecimalConverter>();
            Map(m => m.Price).Name("Price").TypeConverter<NullableDecimalConverter>();
            Map(m => m.CouponBValue).Name("CouponBValue").TypeConverter<NullableDecimalConverter>();
            
            Map(m => m.PriceType).Name("PriceType");
            Map(m => m.CouponType).Name("CouponType");
            Map(m => m.Department).Name("Department");
            Map(m => m.Brand).Name("Brand");
            // Skip the duplicate UPC column (second occurrence)
            // Note: CsvHelper will automatically skip unmapped columns
            Map(m => m.Central_Description).Name("Central_Description");
            // Map CERTCO_Description (header may have brackets, but we ignore them per user requirement)
            // Try both with and without brackets - CsvHelper will use the first match
            Map(m => m.CERTCO_Description).Name("[CERTCO_Description]");
            Map(m => m.LastUpdated).Name("LastUpdated");
            Map(m => m.UnitofMeasure).Name("UnitofMeasure");
            // SourceFileName is set programmatically, not from CSV
            Map(m => m.SourceFileName).Ignore();
        }
    }

    /// <summary>
    /// Service for processing BrData POS CSV files using CsvHelper
    /// Files are pipe-delimited (|)
    /// </summary>
    public class BrDataCsvService
    {
        /// <summary>
        /// Processes POS CSV file and returns list of BrDataPos objects
        /// </summary>
        /// <param name="csvFilePath">Path to the POS CSV file or directory</param>
        /// <returns>List of BrDataPos objects</returns>
        public async Task<List<BrDataPos>> ProcessPosCsvAsync(string csvFilePath)
        {
            var posRecords = new List<BrDataPos>();

            try
            {
                Console.WriteLine($"Processing BrData POS CSV files from: {csvFilePath}");

                List<string> posFiles;

                // Check if path is a directory or a file
                if (Directory.Exists(csvFilePath))
                {
                    // Get all CSV files from the directory
                    posFiles = Directory.EnumerateFiles(csvFilePath, "*.csv", SearchOption.TopDirectoryOnly)
                        .ToList();
                }
                else if (File.Exists(csvFilePath))
                {
                    // Single file
                    posFiles = new List<string> { csvFilePath };
                }
                else
                {
                    Console.WriteLine($"Path does not exist: {csvFilePath}");
                    return posRecords;
                }

                if (!posFiles.Any())
                {
                    Console.WriteLine("No POS CSV files found");
                    return posRecords;
                }

                Console.WriteLine($"Found {posFiles.Count} POS CSV files to process");

                foreach (string file in posFiles)
                {
                    var fileName = Path.GetFileName(file);

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = "|", // Pipe delimiter
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    };

                    using (var reader = new StreamReader(file))
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Context.RegisterClassMap<BrDataPosMap>();

                        // Configure multiple date formats globally for this CSV reader
                        var dateOptions = new TypeConverterOptions
                        {
                            Formats = new[] { "M/d/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "M/dd/yyyy", "MM/d/yyyy", "yyyy/MM/dd", "M/d/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss" }
                        };
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dateOptions);
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(dateOptions);

                        // Note: Nullable decimal fields use NullableDecimalConverter which handles "NULL" strings
                        // No additional TypeConverterOptions configuration needed

                        // Process records one at a time to handle null/invalid values gracefully
                        int recordCount = 0;
                        int errorCount = 0;
                        
                        // Read header first
                        await csv.ReadAsync();
                        csv.ReadHeader();
                        
                        // Process each record individually to catch conversion errors
                        while (await csv.ReadAsync())
                        {
                            try
                            {
                                var record = csv.GetRecord<BrDataPos>();
                                if (record != null)
                                {
                                    record.SourceFileName = fileName;
                                    posRecords.Add(record);
                                    recordCount++;
                                }
                            }
                            catch (CsvHelper.TypeConversion.TypeConverterException ex)
                            {
                                errorCount++;
                                int rowNumber = csv.Context.Parser.Row;
                                Console.WriteLine($"Warning: Skipped record at row {rowNumber} in file {fileName} due to conversion error: {ex.Message}");
                                // Continue processing next record
                            }
                            catch (Exception ex)
                            {
                                errorCount++;
                                int rowNumber = csv.Context.Parser.Row;
                                Console.WriteLine($"Warning: Skipped record at row {rowNumber} in file {fileName} due to error: {ex.Message}");
                                // Continue processing next record
                            }
                        }

                        Console.WriteLine($"Successfully processed {recordCount} POS records from file: {fileName}");
                        if (errorCount > 0)
                        {
                            Console.WriteLine($"Warning: Skipped {errorCount} record(s) due to conversion errors");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing BrData POS CSV file: {ex.Message}");
                throw;
            }

            return posRecords;
        }
    }
}

