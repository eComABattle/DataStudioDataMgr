using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Linq;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.ShopToCook
{
    /// <summary>
    /// Main service for ShopToCook data integration
    /// Orchestrates SFTP download, CSV processing, and MongoDB storage
    /// </summary>
    public class ShopToCookService
    {
        private readonly ShopToCookSftpService _sftpService;
        private readonly ShopToCookCsvService _csvService;
        private readonly ShopToCookMongoService _mongoService;

        public ShopToCookService()
        {
            _sftpService = new ShopToCookSftpService();
            _csvService = new ShopToCookCsvService();
            _mongoService = new ShopToCookMongoService();
        }

        public ShopToCookService(ShopToCookSftpService sftpService, ShopToCookCsvService csvService, ShopToCookMongoService mongoService)
        {
            _sftpService = sftpService;
            _csvService = csvService;
            _mongoService = mongoService;
        }

        /// <summary>
        /// Main method to process all ShopToCook data
        /// Downloads CSV files from SFTP, processes them, and stores in MongoDB
        /// </summary>
        public async Task ProcessShopToCookDataAsync()
        {
            try
            {
                Console.WriteLine("=== STARTING SHOPTOCOOK DATA PROCESSING ===");

                // Step 1: Download CSV files from SFTP
                Console.WriteLine("Step 1: Downloading CSV files from SFTP...");
                var downloadDirectory = await _sftpService.DownloadCsvFilesAsync();
                
                if (string.IsNullOrEmpty(downloadDirectory) || !Directory.Exists(downloadDirectory))
                {
                    Console.WriteLine("No CSV files were downloaded or download directory not found. Exiting.");
                    return;
                }

                Console.WriteLine($"CSV files downloaded to directory: {downloadDirectory}");

                // Step 2: Test MongoDB connection
                Console.WriteLine("Step 2: Testing MongoDB connection...");
                bool connectionSuccessful = await _mongoService.TestConnectionAsync();
                
                // If the first test fails, try the alternative method
                if (!connectionSuccessful)
                {
                    Console.WriteLine("Primary connection test failed, trying alternative method...");
                    connectionSuccessful = await _mongoService.TestConnectionSimpleAsync();
                }
                
                if (!connectionSuccessful)
                {
                    Console.WriteLine("MongoDB connection failed. Cannot proceed with data processing.");
                    Console.WriteLine("Please check:");
                    Console.WriteLine("  1. MongoDB server is running");
                    Console.WriteLine("  2. Connection string in App.config is correct");
                    Console.WriteLine("  3. Network connectivity to MongoDB server");
                    Console.WriteLine("  4. MongoDB credentials are valid");
                    Console.WriteLine("  5. MongoDB driver version compatibility");
                    return;
                }

                // Step 3: Ensure MongoDB collections exist
                Console.WriteLine("Step 3: Ensuring MongoDB collections exist...");
                await _mongoService.EnsureCollectionsExistAsync();

                // Step 4: Process CSV files by type and store in MongoDB
                int totalEmailRecords = 0;
                int totalWebRecords = 0;
                int totalKioskRecords = 0;

                try
                {
                    // Process Email CSV files
                    Console.WriteLine("Processing Email CSV files...");
                    var emailData = await _csvService.ProcessEmailCsvAsync(downloadDirectory);
                    var emailStoredCount = await _mongoService.StoreEmailDataAsync(emailData, "AWG");
                    totalEmailRecords += emailStoredCount;
                    Console.WriteLine($"Processed {emailData.Count} email records, stored {emailStoredCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing email CSV files: {ex.Message}");
                }

                try
                {
                    // Process Web CSV files
                     Console.WriteLine("Processing Web CSV files...");
                    var webData = await _csvService.ProcessWebCsvAsync(downloadDirectory);
                    var webStoredCount = await _mongoService.StoreWebDataAsync(webData, "AWG");
                    totalWebRecords += webStoredCount;
                    Console.WriteLine($"Processed {webData.Count} web records, stored {webStoredCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing web CSV files: {ex.Message}");
                }

                try
                {
                    // Process Kiosk CSV files
                    Console.WriteLine("Processing Kiosk CSV files...");
                    var kioskData = await _csvService.ProcessKioskCsvAsync(downloadDirectory);
                    var kioskStoredCount = await _mongoService.StoreKioskDataAsync(kioskData, "AWG");
                    totalKioskRecords += kioskStoredCount;
                    Console.WriteLine($"Processed {kioskData.Count} kiosk records, stored {kioskStoredCount}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing kiosk CSV files: {ex.Message}");
                }

                // Step 5: Create indexes for better performance
                Console.WriteLine("Step 5: Creating MongoDB indexes...");
                await _mongoService.CreateIndexesAsync();

                // Step 6: Get final counts
                Console.WriteLine("Step 6: Getting collection counts...");
                var counts = await _mongoService.GetCollectionCountsAsync();

                // Summary
                Console.WriteLine("=== SHOPTOCOOK DATA PROCESSING COMPLETE ===");
                Console.WriteLine($"Total email records processed: {totalEmailRecords}");
                Console.WriteLine($"Total web records processed: {totalWebRecords}");
                Console.WriteLine($"Total kiosk records processed: {totalKioskRecords}");
                Console.WriteLine($"Total records in email collection: {counts.emailCount}");
                   Console.WriteLine($"Total records in web collection: {counts.webCount}");
                Console.WriteLine($"Total records in kiosk collection: {counts.kioskCount}");

                // Cleanup downloaded files if configured
                bool cleanupFiles = bool.Parse(ConfigurationManager.AppSettings["ShopToCookCleanupFiles"] ?? "false");
                if (cleanupFiles)
                {
                    Console.WriteLine("Cleaning up downloaded files...");
                    try
                    {
                        var csvFiles = Directory.GetFiles(downloadDirectory, "*.csv");
                        foreach (var filePath in csvFiles)
                        {
                            File.Delete(filePath);
                            Console.WriteLine($"Deleted: {Path.GetFileName(filePath)}");
                        }
                        Console.WriteLine($"Cleaned up {csvFiles.Length} CSV files from {downloadDirectory}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error cleaning up files in {downloadDirectory}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShopToCook data processing: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Processes a specific CSV file type
        /// </summary>
        /// <param name="fileType">Type of file to process (email, web, kiosk)</param>
        /// <param name="fileName">Specific file name to download (optional)</param>
        public async Task ProcessSpecificFileTypeAsync(string fileType, string fileName = null)
        {
            try
            {
                Console.WriteLine($"=== PROCESSING SHOPTOCOOK {fileType.ToUpper()} DATA ===");

                string filePath;
                
                if (!string.IsNullOrEmpty(fileName))
                {
                    // Download specific file
                    filePath = await _sftpService.DownloadCsvFileAsync(fileName);
                }
                else
                {
                    // Download all files and find the one we need
                    var downloadDirectory = await _sftpService.DownloadCsvFilesAsync();
                    var csvFiles = Directory.GetFiles(downloadDirectory, "*.csv");
                    filePath = csvFiles.FirstOrDefault(f => Path.GetFileName(f).ToLower().Contains(fileType.ToLower()));
                    
                    if (string.IsNullOrEmpty(filePath))
                    {
                        Console.WriteLine($"No {fileType} file found on SFTP server");
                        return;
                    }
                }

                Console.WriteLine($"Processing {fileType} file: {Path.GetFileName(filePath)}");

                int storedCount = 0;
                
                switch (fileType.ToLower())
                {
                    case "email":
                        var emailData = await _csvService.ProcessEmailCsvAsync(filePath);
                        storedCount = await _mongoService.StoreEmailDataAsync(emailData, "AWG");
                        break;
                        
                    case "web":
                        var webData = await _csvService.ProcessWebCsvAsync(filePath);
                        storedCount = await _mongoService.StoreWebDataAsync(webData, "AWG");
                        break;
                        
                    case "kiosk":
                        var kioskData = await _csvService.ProcessKioskCsvAsync(filePath);
                        storedCount = await _mongoService.StoreKioskDataAsync(kioskData, "AWG");
                        break;
                        
                    default:
                        Console.WriteLine($"Unknown file type: {fileType}");
                        return;
                }

                Console.WriteLine($"Successfully processed {storedCount} {fileType} records");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {fileType} data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lists files available on SFTP server
        /// </summary>
        public async Task ListAvailableFilesAsync()
        {
            try
            {
                Console.WriteLine("=== LISTING FILES ON SFTP SERVER ===");
                var files = await _sftpService.ListFilesAsync();
                
                if (files.Count == 0)
                {
                    Console.WriteLine("No files found on SFTP server");
                }
                else
                {
                    Console.WriteLine($"Found {files.Count} files:");
                    foreach (var file in files)
                    {
                        Console.WriteLine($"  - {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing files: {ex.Message}");
                throw;
            }
        }
    }
}
