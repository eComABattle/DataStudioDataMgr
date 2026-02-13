using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Linq;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.BrData
{
    /// <summary>
    /// Main service for BrData POS data integration
    /// Orchestrates file access, CSV processing, and MongoDB storage
    /// </summary>
    public class BrDataService
    {
        private readonly BrDataFileService _fileService;
        private readonly BrDataCsvService _csvService;
        private readonly BrDataMongoService _mongoService;

        public BrDataService()
        {
            _fileService = new BrDataFileService();
            _csvService = new BrDataCsvService();
            _mongoService = new BrDataMongoService();
        }

        public BrDataService(BrDataFileService fileService, BrDataCsvService csvService, BrDataMongoService mongoService)
        {
            _fileService = fileService;
            _csvService = csvService;
            _mongoService = mongoService;
        }

        /// <summary>
        /// Main method to process all BrData POS data
        /// Copies CSV files from network share, processes them, and stores in MongoDB
        /// </summary>
        public async Task ProcessBrDataPosDataAsync()
        {
            try
            {
                Console.WriteLine("=== STARTING BRDATA POS DATA PROCESSING ===");

                // Step 1: Copy CSV files from network share to local directory
                Console.WriteLine("Step 1: Copying POS CSV files from network share...");
                var fileToClientTokenMap = await _fileService.CopyCsvFilesToLocalAsync();

                if (fileToClientTokenMap.Count == 0)
                {
                    Console.WriteLine("No CSV files were copied. Exiting.");
                    return;
                }

                var localDirectory = ConfigurationManager.AppSettings["BrDataLocalPath"] ?? "downloads\\brdata";
                Console.WriteLine($"CSV files copied to directory: {localDirectory}");

                // Track original network file paths for archiving
                var networkFileToClientTokenMap = new Dictionary<string, string>();
                var networkFiles = await _fileService.GetCsvFilesAsync();
                foreach (var (networkFile, clientToken) in networkFiles)
                {
                    // Match network files to local files by filename
                    var networkFileName = Path.GetFileName(networkFile);
                    var matchingLocalFile = fileToClientTokenMap.Keys.FirstOrDefault(localFile => 
                        Path.GetFileName(localFile).Equals(networkFileName, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingLocalFile != null)
                    {
                        networkFileToClientTokenMap[networkFile] = clientToken;
                    }
                }

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

                // Step 4: Process POS CSV files and store in MongoDB
                int totalPosRecords = 0;
                var successfullyProcessedFiles = new Dictionary<string, string>(); // Network file path -> client token

                try
                {
                    Console.WriteLine("Processing POS CSV files...");
                    
                    // Process each file with its associated client token
                    foreach (var kvp in fileToClientTokenMap)
                    {
                        var filePath = kvp.Key;
                        var clientToken = kvp.Value;
                        
                        Console.WriteLine($"Processing file: {Path.GetFileName(filePath)} with Client Token: {clientToken}");
                        
                        try
                        {
                            var posData = await _csvService.ProcessPosCsvAsync(filePath);
                            var posStoredCount = await _mongoService.StorePosDataAsync(posData, clientToken);
                            totalPosRecords += posStoredCount;
                            Console.WriteLine($"Processed {posData.Count} POS records from {Path.GetFileName(filePath)}, stored {posStoredCount} with Client Token: {clientToken}");
                            
                            // Track successfully processed files for archiving
                            var networkFile = networkFileToClientTokenMap.Keys.FirstOrDefault(nf => 
                                Path.GetFileName(nf).Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase));
                            
                            if (networkFile != null && posStoredCount > 0)
                            {
                                successfullyProcessedFiles[networkFile] = clientToken;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing file {Path.GetFileName(filePath)}: {ex.Message}");
                            // Continue with next file
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing POS CSV files: {ex.Message}");
                }

                // Step 5: Create indexes for better performance
                Console.WriteLine("Step 5: Creating MongoDB indexes...");
                await _mongoService.CreateIndexesAsync();

                // Step 6: Get final counts
                Console.WriteLine("Step 6: Getting collection counts...");
                var posCount = await _mongoService.GetCollectionCountsAsync();

                // Step 7: Archive successfully processed files
                if (successfullyProcessedFiles.Count > 0)
                {
                    Console.WriteLine("Step 7: Archiving successfully processed CSV files...");
                    try
                    {
                        var archivedCount = await _fileService.ArchiveFilesAsync(successfullyProcessedFiles);
                        Console.WriteLine($"Archived {archivedCount} of {successfullyProcessedFiles.Count} file(s)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error archiving files: {ex.Message}");
                    }
                }

                // Summary
                Console.WriteLine("=== BRDATA POS DATA PROCESSING COMPLETE ===");
                Console.WriteLine($"Total POS records processed: {totalPosRecords}");
                Console.WriteLine($"Total records in POS collection: {posCount}");

                // Cleanup downloaded files if configured
                bool cleanupFiles = bool.Parse(ConfigurationManager.AppSettings["BrDataCleanupFiles"] ?? "false");
                if (cleanupFiles)
                {
                    Console.WriteLine("Cleaning up copied files...");
                    try
                    {
                        var csvFiles = Directory.GetFiles(localDirectory, "*.csv").ToList();
                        foreach (var filePath in csvFiles)
                        {
                            File.Delete(filePath);
                            Console.WriteLine($"Deleted: {Path.GetFileName(filePath)}");
                        }
                        Console.WriteLine($"Cleaned up {csvFiles.Count} CSV files from {localDirectory}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error cleaning up files in {localDirectory}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BrData POS data processing: {ex.Message}");
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
                Console.WriteLine($"=== PROCESSING BRDATA POS FILE: {fileName} ===");

                // Get file from network share with client token
                var (filePath, clientToken) = await _fileService.GetCsvFileAsync(fileName);

                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine($"File not found: {fileName}");
                    return;
                }

                // Client token should be CERTCO
                if (string.IsNullOrEmpty(clientToken))
                {
                    clientToken = "CERTCO";
                }

                Console.WriteLine($"Processing file: {fileName} with Client Token: {clientToken}");

                // Process and store
                var posData = await _csvService.ProcessPosCsvAsync(filePath);
                var storedCount = await _mongoService.StorePosDataAsync(posData, clientToken);

                Console.WriteLine($"Successfully processed {storedCount} POS records from {fileName} with Client Token: {clientToken}");

                // Archive file if successfully stored
                if (storedCount > 0 && !string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine($"Archiving file: {fileName}...");
                    var archived = await _fileService.ArchiveFileAsync(filePath, clientToken);
                    if (archived)
                    {
                        Console.WriteLine($"Successfully archived {fileName}");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Failed to archive {fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {fileName}: {ex.Message}");
                throw;
            }
        }
    }
}


