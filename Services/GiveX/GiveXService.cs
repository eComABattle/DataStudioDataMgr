using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Linq;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.GiveX
{
    /// <summary>
    /// Main service for GiveX data integration
    /// Orchestrates file access, CSV processing, and MongoDB storage
    /// </summary>
    public class GiveXService
    {
        private readonly GiveXFileService _fileService;
        private readonly GiveXCsvService _csvService;
        private readonly GiveXMongoService _mongoService;

        public GiveXService()
        {
            _fileService = new GiveXFileService();
            _csvService = new GiveXCsvService();
            _mongoService = new GiveXMongoService();
        }

        public GiveXService(GiveXFileService fileService, GiveXCsvService csvService, GiveXMongoService mongoService)
        {
            _fileService = fileService;
            _csvService = csvService;
            _mongoService = mongoService;
        }

        /// <summary>
        /// Main method to process all GiveX email data
        /// Copies CSV files from network share, processes them, and stores in MongoDB
        /// </summary>
        public async Task ProcessGiveXEmailDataAsync()
        {
            try
            {
                Console.WriteLine("=== STARTING GIVEX EMAIL DATA PROCESSING ===");

                // Step 1: Copy CSV files from both network shares to local directory
                Console.WriteLine("Step 1: Copying email CSV files from network shares (Certco and AWG)...");
                var fileToClientTokenMap = await _fileService.CopyCsvFilesToLocalAsync("email");

                if (fileToClientTokenMap.Count == 0)
                {
                    Console.WriteLine("No CSV files were copied. Exiting.");
                    return;
                }

                var localDirectory = ConfigurationManager.AppSettings["GiveXLocalPath"] ?? "downloads\\givex";
                Console.WriteLine($"CSV files copied to directory: {localDirectory}");

                // Track original network file paths for archiving
                var networkFileToClientTokenMap = new Dictionary<string, string>();
                var networkFiles = await _fileService.GetCsvFilesAsync("email");
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

                // Step 2: Test MongoDB connection (GiveX email database: ad_campaign)
                Console.WriteLine("Step 2: Testing MongoDB connection...");
                bool connectionSuccessful = await _mongoService.TestConnectionAsync(GiveXMongoService.EmailDatabaseName);

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

                // Step 3: Ensure MongoDB collections exist (ad_campaign)
                Console.WriteLine("Step 3: Ensuring MongoDB collections exist...");
                await _mongoService.EnsureCollectionsExistAsync(GiveXMongoService.EmailDatabaseName);

                // Step 4: Process Email CSV files and store in MongoDB with appropriate client tokens
                int totalEmailRecords = 0;
                var successfullyProcessedFiles = new Dictionary<string, string>(); // Network file path -> client token

                try
                {
                    Console.WriteLine("Processing Email CSV files...");
                    
                    // Process each file with its associated client token
                    foreach (var kvp in fileToClientTokenMap)
                    {
                        var filePath = kvp.Key;
                        var clientToken = kvp.Value;
                        
                        Console.WriteLine($"Processing file: {Path.GetFileName(filePath)} with Client Token: {clientToken}");
                        
                        try
                        {
                            var emailData = await _csvService.ProcessEmailCsvAsync(filePath);
                            var emailStoredCount = await _mongoService.StoreEmailDataAsync(emailData, clientToken);
                            totalEmailRecords += emailStoredCount;
                            Console.WriteLine($"Processed {emailData.Count} email records from {Path.GetFileName(filePath)}, stored {emailStoredCount} with Client Token: {clientToken}");
                            
                            // Track successfully processed files for archiving
                            var networkFile = networkFileToClientTokenMap.Keys.FirstOrDefault(nf => 
                                Path.GetFileName(nf).Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase));

                            //networkFile != null && emailStoredCount > 0  removed condition to archive even if 0 records stored
                            if (networkFile != null)
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
                    Console.WriteLine($"Error processing email CSV files: {ex.Message}");
                }

                // Step 5: Create indexes for better performance
                Console.WriteLine("Step 5: Creating MongoDB indexes...");
                await _mongoService.CreateIndexesAsync();

                // Step 6: Get final counts
                Console.WriteLine("Step 6: Getting collection counts...");
                var emailCount = await _mongoService.GetCollectionCountsAsync();

                // Step 7: Archive all files (always archive, regardless of processing success or record count)
                if (networkFileToClientTokenMap.Count > 0)
                {
                    Console.WriteLine("Step 7: Archiving CSV files...");
                    try
                    {
                        var archivedCount = await _fileService.ArchiveFilesAsync(networkFileToClientTokenMap, "email");
                        Console.WriteLine($"Archived {archivedCount} of {networkFileToClientTokenMap.Count} file(s)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error archiving files: {ex.Message}");
                    }
                }

                // Summary
                Console.WriteLine("=== GIVEX EMAIL DATA PROCESSING COMPLETE ===");
                Console.WriteLine($"Total email records processed: {totalEmailRecords}");
                Console.WriteLine($"Total records in email collection: {emailCount}");

                // Cleanup downloaded files if configured
                bool cleanupFiles = bool.Parse(ConfigurationManager.AppSettings["GiveXCleanupFiles"] ?? "false");
                if (cleanupFiles)
                {
                    Console.WriteLine("Cleaning up copied files...");
                    try
                    {
                        var csvFiles = Directory.GetFiles(localDirectory, "*.csv")
                            .Where(f => Path.GetFileName(f).ToLower().Contains("email") || Path.GetFileName(f).ToLower().Contains("awg"))
                            .ToList();
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
                Console.WriteLine($"Error in GiveX email data processing: {ex.Message}");
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
                Console.WriteLine($"=== PROCESSING GIVEX EMAIL FILE: {fileName} ===");

                // Get file from network shares with client token
                var (filePath, clientToken) = await _fileService.GetCsvFileAsync(fileName);

                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine($"File not found: {fileName}");
                    return;
                }

                // Determine client token from filename if not already determined
                if (string.IsNullOrEmpty(clientToken))
                {
                    clientToken = fileName.ToLower().Contains("awg") ? "AWG" : "Certco";
                }

                Console.WriteLine($"Processing file: {fileName} with Client Token: {clientToken}");

                // Process and store
                var emailData = await _csvService.ProcessEmailCsvAsync(filePath);
                var storedCount = await _mongoService.StoreEmailDataAsync(emailData, clientToken);

                Console.WriteLine($"Successfully processed {storedCount} email records from {fileName} with Client Token: {clientToken}");



                // Archive file if successfully stored
                //storedCount > 0 && !string.IsNullOrEmpty(filePath) removed condition to archive even if 0 records stored
                if (!string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine($"Archiving file: {fileName}...");
                    var archived = await _fileService.ArchiveFileAsync(filePath, clientToken, "email");
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

        /// <summary>
        /// Main method to process all GiveX loyalty data
        /// Copies CSV files from network share, processes them, and stores in MongoDB
        /// </summary>
        public async Task ProcessGiveXLoyaltyDataAsync()
        {
            try
            {
                Console.WriteLine("=== STARTING GIVEX LOYALTY DATA PROCESSING ===");

                // Step 1: Copy CSV files from network share to local directory
                Console.WriteLine("Step 1: Copying loyalty CSV files from network share...");
                var fileToClientTokenMap = await _fileService.CopyCsvFilesToLocalAsync("loyalty");

                if (fileToClientTokenMap.Count == 0)
                {
                    Console.WriteLine("No CSV files were copied. Exiting.");
                    return;
                }

                var localDirectory = ConfigurationManager.AppSettings["GiveXLocalPath"] ?? "downloads\\givex";
                Console.WriteLine($"CSV files copied to directory: {localDirectory}");

                // Track original network file paths for archiving
                var networkFileToClientTokenMap = new Dictionary<string, string>();
                var networkFiles = await _fileService.GetCsvFilesAsync("loyalty");
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

                // Step 2: Test MongoDB connection (GiveX loyalty database: loyalty_analytics)
                Console.WriteLine("Step 2: Testing MongoDB connection...");
                bool connectionSuccessful = await _mongoService.TestConnectionAsync(GiveXMongoService.LoyaltyDatabaseName);

                if (!connectionSuccessful)
                {
                    Console.WriteLine("MongoDB connection failed. Cannot proceed with data processing.");
                    return;
                }

                // Step 3: Ensure MongoDB collections exist (loyalty_analytics)
                Console.WriteLine("Step 3: Ensuring MongoDB collections exist...");
                await _mongoService.EnsureCollectionsExistAsync(GiveXMongoService.LoyaltyDatabaseName);

                // Step 4: Process Loyalty CSV files and store in MongoDB
                int totalLoyaltyRecords = 0;
                var successfullyProcessedFiles = new Dictionary<string, string>(); // Network file path -> client token

                try
                {
                    Console.WriteLine("Processing Loyalty CSV files...");
                    
                    // Process each file with its associated client token
                    foreach (var kvp in fileToClientTokenMap)
                    {
                        var filePath = kvp.Key;
                        var clientToken = kvp.Value;
                        
                        Console.WriteLine($"Processing file: {Path.GetFileName(filePath)} with Client Token: {clientToken}");
                        
                        try
                        {
                            var loyaltyData = await _csvService.ProcessLoyaltyCsvAsync(filePath);
                            var loyaltyStoredCount = await _mongoService.StoreLoyaltyDataAsync(loyaltyData, clientToken);
                            totalLoyaltyRecords += loyaltyStoredCount;
                            Console.WriteLine($"Processed {loyaltyData.Count} loyalty records from {Path.GetFileName(filePath)}, stored {loyaltyStoredCount} with Client Token: {clientToken}");
                            
                            // Track successfully processed files for archiving
                            var networkFile = networkFileToClientTokenMap.Keys.FirstOrDefault(nf => 
                                Path.GetFileName(nf).Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase));
                            
                            // Removed condition to archive even if 0 records stored
                            if (networkFile != null)
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
                    Console.WriteLine($"Error processing loyalty CSV files: {ex.Message}");
                }

                // Step 5: Create indexes for better performance
                Console.WriteLine("Step 5: Creating MongoDB indexes...");
                await _mongoService.CreateIndexesAsync();

                // Step 6: Get final counts
                Console.WriteLine("Step 6: Getting collection counts...");
                var (emailCount, loyaltyCount, couponCount) = await _mongoService.GetAllCollectionCountsAsync();

                // Step 7: Archive all files (always archive, regardless of processing success or record count)
                if (networkFileToClientTokenMap.Count > 0)
                {
                    Console.WriteLine("Step 7: Archiving CSV files...");
                    try
                    {
                        var archivedCount = await _fileService.ArchiveFilesAsync(networkFileToClientTokenMap, "loyalty");
                        Console.WriteLine($"Archived {archivedCount} of {networkFileToClientTokenMap.Count} file(s)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error archiving files: {ex.Message}");
                    }
                }

                // Summary
                Console.WriteLine("=== GIVEX LOYALTY DATA PROCESSING COMPLETE ===");
                Console.WriteLine($"Total loyalty records processed: {totalLoyaltyRecords}");
                Console.WriteLine($"Total records in loyalty collection: {loyaltyCount}");

                // Cleanup downloaded files if configured
                bool cleanupFiles = bool.Parse(ConfigurationManager.AppSettings["GiveXCleanupFiles"] ?? "false");
                if (cleanupFiles)
                {
                    Console.WriteLine("Cleaning up copied files...");
                    try
                    {
                        var csvFiles = Directory.GetFiles(localDirectory, "*.csv")
                            .Where(f => Path.GetFileName(f).ToLower().Contains("enrollment_"))
                            .ToList();
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
                Console.WriteLine($"Error in GiveX loyalty data processing: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Main method to process all GiveX coupon data
        /// Copies CSV files from network share, processes them, and stores in MongoDB
        /// </summary>
        public async Task ProcessGiveXCouponDataAsync()
        {
            try
            {
                Console.WriteLine("=== STARTING GIVEX COUPON DATA PROCESSING ===");

                // Step 1: Copy CSV files from network share to local directory
                Console.WriteLine("Step 1: Copying coupon CSV files from network share...");
                var fileToClientTokenMap = await _fileService.CopyCsvFilesToLocalAsync("coupon");

                if (fileToClientTokenMap.Count == 0)
                {
                    Console.WriteLine("No CSV files were copied. Exiting.");
                    return;
                }

                var localDirectory = ConfigurationManager.AppSettings["GiveXLocalPath"] ?? "downloads\\givex";
                Console.WriteLine($"CSV files copied to directory: {localDirectory}");

                // Track original network file paths for archiving
                var networkFileToClientTokenMap = new Dictionary<string, string>();
                var networkFiles = await _fileService.GetCsvFilesAsync("coupon");
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

                // Step 2: Test MongoDB connection (GiveX coupon database: digital_coupon_analytics)
                Console.WriteLine("Step 2: Testing MongoDB connection...");
                bool connectionSuccessful = await _mongoService.TestConnectionAsync(GiveXMongoService.CouponDatabaseName);

                if (!connectionSuccessful)
                {
                    Console.WriteLine("MongoDB connection failed. Cannot proceed with data processing.");
                    return;
                }

                // Step 3: Ensure MongoDB collections exist (digital_coupon_analytics)
                Console.WriteLine("Step 3: Ensuring MongoDB collections exist...");
                await _mongoService.EnsureCollectionsExistAsync(GiveXMongoService.CouponDatabaseName);

                // Step 4: Process Coupon CSV files and store in MongoDB
                int totalCouponRecords = 0;
                var successfullyProcessedFiles = new Dictionary<string, string>(); // Network file path -> client token

                try
                {
                    Console.WriteLine("Processing Coupon CSV files...");
                    
                    // Process each file with its associated client token
                    foreach (var kvp in fileToClientTokenMap)
                    {
                        var filePath = kvp.Key;
                        var clientToken = kvp.Value;
                        
                        Console.WriteLine($"Processing file: {Path.GetFileName(filePath)} with Client Token: {clientToken}");
                        
                        try
                        {
                            var couponData = await _csvService.ProcessCouponCsvAsync(filePath);
                            var couponStoredCount = await _mongoService.StoreCouponDataAsync(couponData, clientToken);
                            totalCouponRecords += couponStoredCount;
                            Console.WriteLine($"Processed {couponData.Count} coupon records from {Path.GetFileName(filePath)}, stored {couponStoredCount} with Client Token: {clientToken}");
                            
                            // Track successfully processed files for archiving
                            var networkFile = networkFileToClientTokenMap.Keys.FirstOrDefault(nf => 
                                Path.GetFileName(nf).Equals(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase));

                            //if (networkFile != null && couponStoredCount > 0)
                            if (networkFile != null)
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
                    Console.WriteLine($"Error processing coupon CSV files: {ex.Message}");
                }

                // Step 5: Create indexes for better performance
                Console.WriteLine("Step 5: Creating MongoDB indexes...");
                await _mongoService.CreateIndexesAsync();

                // Step 6: Get final counts
                Console.WriteLine("Step 6: Getting collection counts...");
                var (emailCount, loyaltyCount, couponCount) = await _mongoService.GetAllCollectionCountsAsync();

                // Step 7: Archive all files (always archive, regardless of processing success or record count)
                if (networkFileToClientTokenMap.Count > 0)
                {
                    Console.WriteLine("Step 7: Archiving CSV files...");
                    try
                    {
                        var archivedCount = await _fileService.ArchiveFilesAsync(networkFileToClientTokenMap, "coupon");
                        Console.WriteLine($"Archived {archivedCount} of {networkFileToClientTokenMap.Count} file(s)");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error archiving files: {ex.Message}");
                    }
                }

                // Summary
                Console.WriteLine("=== GIVEX COUPON DATA PROCESSING COMPLETE ===");
                Console.WriteLine($"Total coupon records processed: {totalCouponRecords}");
                Console.WriteLine($"Total records in coupon collection: {couponCount}");

                // Cleanup downloaded files if configured
                bool cleanupFiles = bool.Parse(ConfigurationManager.AppSettings["GiveXCleanupFiles"] ?? "false");
                if (cleanupFiles)
                {
                    Console.WriteLine("Cleaning up copied files...");
                    try
                    {
                        var csvFiles = Directory.GetFiles(localDirectory, "*.csv")
                            .Where(f => Path.GetFileName(f).ToLower().Contains("coupons_"))
                            .ToList();
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
                Console.WriteLine($"Error in GiveX coupon data processing: {ex.Message}");
                throw;
            }
        }
    }
}

