using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Linq;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.AppCard
{
    /// <summary>
    /// Main service for AppCard data integration
    /// Orchestrates SFTP download, CSV processing, and MongoDB storage
    /// </summary>
    public class AppCardService
    {
        private readonly AppCardSftpService _sftpService;
        private readonly AppCardCsvService _csvService;
        private readonly AppCardMongoService _mongoService;

        public AppCardService()
        {
            _sftpService = new AppCardSftpService();
            _csvService = new AppCardCsvService();
            _mongoService = new AppCardMongoService();
        }

        public AppCardService(AppCardSftpService sftpService, AppCardCsvService csvService, AppCardMongoService mongoService)
        {
            _sftpService = sftpService;
            _csvService = csvService;
            _mongoService = mongoService;
        }

        /// <summary>
        /// Main method to process all AppCard data
        /// Downloads CSV files from SFTP, processes them, and stores in MongoDB
        /// </summary>
        public async Task ProcessAppCardDataAsync()
        {
            try
            {
                Console.WriteLine("=== STARTING APPCARD DATA PROCESSING ===");

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
                
                if (!connectionSuccessful)
                {
                    Console.WriteLine("MongoDB connection failed. Cannot proceed with data processing.");
                    Console.WriteLine("Please check:");
                    Console.WriteLine("  1. MongoDB server is running");
                    Console.WriteLine("  2. Connection string in App.config is correct");
                    Console.WriteLine("  3. Network connectivity to MongoDB server");
                    Console.WriteLine("  4. MongoDB credentials are valid");
                    return;
                }

                // Step 3: Ensure MongoDB collections exist
                Console.WriteLine("Step 3: Ensuring MongoDB collections exist...");
                await _mongoService.EnsureCollectionsExistAsync();

                // Step 4: Process CSV files and store in MongoDB
                int totalRecords = 0;
                List<string> processedFiles = new List<string>();

                try
                {
                    Console.WriteLine("Processing AppCard CSV files...");
                    
                    // Get list of CSV files before processing
                    var csvFiles = Directory.GetFiles(downloadDirectory, "*.csv")
                        .Select(f => Path.GetFileName(f))
                        .ToList();
                    
                    var data = await _csvService.ProcessCsvAsync(downloadDirectory);
                    var storedCount = await _mongoService.StoreDataAsync(data, "AWG");
                    totalRecords += storedCount;
                    Console.WriteLine($"Processed {data.Count} records, stored {storedCount}");
                    
                    // Track successfully processed files
                    if (storedCount > 0)
                    {
                        processedFiles.AddRange(csvFiles);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing AppCard CSV files: {ex.Message}");
                }

                // Step 5: Create indexes for better performance
                Console.WriteLine("Step 5: Creating MongoDB indexes...");
                await _mongoService.CreateIndexesAsync();

                // Step 6: Get final counts
                Console.WriteLine("Step 6: Getting collection counts...");
                var dataCount = await _mongoService.GetCollectionCountsAsync();

                // Step 7: Move processed files to Archive folder on SFTP
                if (processedFiles.Count > 0)
                {
                    Console.WriteLine("Step 7: Moving processed files to Archive folder...");
                    try
                    {
                        var archivedCount = await _sftpService.MoveFilesToArchiveAsync(processedFiles);
                        Console.WriteLine($"Successfully moved {archivedCount} of {processedFiles.Count} file(s) to Archive folder");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error moving files to archive: {ex.Message}");
                    }
                }

                // Summary
                Console.WriteLine("=== APPCARD DATA PROCESSING COMPLETE ===");
                Console.WriteLine($"Total records processed: {totalRecords}");
                Console.WriteLine($"Total records in data collection: {dataCount}");

                // Cleanup downloaded files if configured
                bool cleanupFiles = bool.Parse(ConfigurationManager.AppSettings["AppCardCleanupFiles"] ?? "false");
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
                Console.WriteLine($"Error in AppCard data processing: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Processes a specific CSV file
        /// </summary>
        /// <param name="fileName">Name of the file to process</param>
        public async Task ProcessSpecificFileAsync(string fileName)
        {
            try
            {
                Console.WriteLine($"=== PROCESSING APPCARD FILE: {fileName} ===");

                // Download specific file
                var filePath = await _sftpService.DownloadCsvFileAsync(fileName);

                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine($"File not found: {fileName}");
                    return;
                }

                Console.WriteLine($"Processing file: {fileName}");

                // Process and store
                var data = await _csvService.ProcessCsvAsync(filePath);
                var storedCount = await _mongoService.StoreDataAsync(data, "AWG");

                Console.WriteLine($"Successfully processed {storedCount} records from {fileName}");

                // Move file to Archive folder after successful processing
                if (storedCount > 0)
                {
                    Console.WriteLine($"Moving {fileName} to Archive folder...");
                    var archived = await _sftpService.MoveFileToArchiveAsync(fileName);
                    if (archived)
                    {
                        Console.WriteLine($"Successfully moved {fileName} to Archive folder");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Failed to move {fileName} to Archive folder");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {fileName}: {ex.Message}");
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
                Console.WriteLine("=== LISTING FILES ON APPCARD SFTP SERVER ===");
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


