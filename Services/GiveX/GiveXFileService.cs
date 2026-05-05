using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Linq;

namespace DataStudioDataMgr.Services.GiveX
{
    /// <summary>
    /// Service for accessing GiveX CSV files from network share
    /// Supports both Certco and AWG file locations
    /// </summary>
    public class GiveXFileService
    {
        private readonly string _certcoNetworkPath;
        private readonly string _awgNetworkPath;
        private readonly string _certcoEmailArchivePath;
        private readonly string _awgEmailArchivePath;
        private readonly string _certcoLoyaltyArchivePath;
        private readonly string _certcoCouponArchivePath;
        private readonly string _localDownloadPath;

        public GiveXFileService()
        {
            // Network paths where GiveX files are stored
            _certcoNetworkPath = ConfigurationManager.AppSettings["GiveXCertcoNetworkPath"] ?? @"\\vm-fileserver\FTP\Certco\GiveX\Coupon_Data\";
            _awgNetworkPath = ConfigurationManager.AppSettings["GiveXAwgNetworkPath"] ?? @"\\vm-fileserver\FTP\AWG\GiveX\Coupon_Data\";
            
            // Archive paths for GiveX files
            _certcoEmailArchivePath = ConfigurationManager.AppSettings["GiveXCertcoEmailArchivePath"] ?? @"\\vm-fileserver\FTP\Certco\GiveX\Email_Data\Archive\";
            _awgEmailArchivePath = ConfigurationManager.AppSettings["GiveXAwgEmailArchivePath"] ?? @"\\vm-fileserver\FTP\AWG\GiveX\Email_Data\Archive\";
            _certcoLoyaltyArchivePath = ConfigurationManager.AppSettings["GiveXCertcoLoyaltyArchivePath"] ?? @"\\vm-fileserver\FTP\Certco\GiveX\Loyalty_Data\Archive\";
            _certcoCouponArchivePath = ConfigurationManager.AppSettings["GiveXCertcoCouponArchivePath"] ?? @"\\vm-fileserver\FTP\Certco\GiveX\Coupon_Data\Archive\";
            
            // Local download path for processing
            _localDownloadPath = ConfigurationManager.AppSettings["GiveXLocalPath"] ?? "downloads\\givex";
        }

        public GiveXFileService(string certcoNetworkPath, string awgNetworkPath, string localPath, string certcoEmailArchivePath = null, string awgEmailArchivePath = null, string certcoLoyaltyArchivePath = null, string certcoCouponArchivePath = null)
        {
            _certcoNetworkPath = certcoNetworkPath;
            _awgNetworkPath = awgNetworkPath;
            _localDownloadPath = localPath;
            _certcoEmailArchivePath = certcoEmailArchivePath ?? @"\\vm-fileserver\FTP\Certco\GiveX\Email_Data\Archive\";
            _awgEmailArchivePath = awgEmailArchivePath ?? @"\\vm-fileserver\FTP\AWG\GiveX\Email_Data\Archive\";
            _certcoLoyaltyArchivePath = certcoLoyaltyArchivePath ?? @"\\vm-fileserver\FTP\Certco\GiveX\Loyalty_Data\Archive\";
            _certcoCouponArchivePath = certcoCouponArchivePath ?? @"\\vm-fileserver\FTP\Certco\GiveX\Coupon_Data\Archive\";
        }

        /// <summary>
        /// Gets all CSV files from both network shares for email data type
        /// Certco files contain "Email" in filename, AWG files contain "AWG" in filename
        /// </summary>
        /// <param name="dataType">Type of data (email, loyalty, coupon)</param>
        /// <returns>List of file paths with their associated client tokens</returns>
        public async Task<List<(string filePath, string clientToken)>> GetCsvFilesAsync(string dataType)
        {
            var filePaths = new List<(string filePath, string clientToken)>();

            try
            {
                    // Process Certco files based on data type
                    if (dataType.ToLower() == "email")
                    {
                        Console.WriteLine($"Accessing GiveX Certco network share: {_certcoNetworkPath}");
                        if (Directory.Exists(_certcoNetworkPath))
                        {
                            var certcoFiles = Directory.EnumerateFiles(_certcoNetworkPath, "*.csv", SearchOption.TopDirectoryOnly)
                                .Where(f => Path.GetFileName(f).ToLower().Contains("email"))
                                .ToList();

                            Console.WriteLine($"Found {certcoFiles.Count} Certco email CSV files");
                            foreach (var file in certcoFiles)
                            {
                                filePaths.Add((file, "Certco"));
                                Console.WriteLine($"  - {Path.GetFileName(file)} (Client Token: Certco)");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Certco network path does not exist: {_certcoNetworkPath}");
                        }

                        // Process AWG files (files containing "AWG" in name)
                        Console.WriteLine($"Accessing GiveX AWG network share: {_awgNetworkPath}");
                        if (Directory.Exists(_awgNetworkPath))
                        {
                            var awgFiles = Directory.EnumerateFiles(_awgNetworkPath, "*.csv", SearchOption.TopDirectoryOnly)
                                .Where(f => Path.GetFileName(f).ToLower().Contains("awg"))
                                .ToList();

                            Console.WriteLine($"Found {awgFiles.Count} AWG email CSV files");
                            foreach (var file in awgFiles)
                            {
                                filePaths.Add((file, "AWG"));
                                Console.WriteLine($"  - {Path.GetFileName(file)} (Client Token: AWG)");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"AWG network path does not exist: {_awgNetworkPath}");
                        }
                    }
                    else if (dataType.ToLower() == "loyalty")
                    {
                        // Process Certco loyalty files (files containing "Enrollment_")
                        Console.WriteLine($"Accessing GiveX Certco network share: {_certcoNetworkPath}");
                        if (Directory.Exists(_certcoNetworkPath))
                        {
                            var loyaltyFiles = Directory.EnumerateFiles(_certcoNetworkPath, "*.csv", SearchOption.TopDirectoryOnly)
                                .Where(f => Path.GetFileName(f).ToLower().Contains("enrollment_"))
                                .ToList();

                            Console.WriteLine($"Found {loyaltyFiles.Count} Certco loyalty CSV files");
                            foreach (var file in loyaltyFiles)
                            {
                                filePaths.Add((file, "CERTCO"));
                                Console.WriteLine($"  - {Path.GetFileName(file)} (Client Token: CERTCO)");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Certco network path does not exist: {_certcoNetworkPath}");
                        }
                    }
                    else if (dataType.ToLower() == "coupon")
                    {
                        // Process Certco coupon files (files containing "Coupons_")
                        Console.WriteLine($"Accessing GiveX Certco network share: {_certcoNetworkPath}");
                        if (Directory.Exists(_certcoNetworkPath))
                        {
                            var couponFiles = Directory.EnumerateFiles(_certcoNetworkPath, "*.csv", SearchOption.TopDirectoryOnly)
                                .Where(f => Path.GetFileName(f).ToLower().Contains("coupons_"))
                                .ToList();

                            Console.WriteLine($"Found {couponFiles.Count} Certco coupon CSV files");
                            foreach (var file in couponFiles)
                            {
                                filePaths.Add((file, "CERTCO"));
                                Console.WriteLine($"  - {Path.GetFileName(file)} (Client Token: CERTCO)");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Certco network path does not exist: {_certcoNetworkPath}");
                        }
                    }
                else
                {
                    // For other data types, check both paths
                    foreach (var networkPath in new[] { _certcoNetworkPath, _awgNetworkPath })
                    {
                        if (Directory.Exists(networkPath))
                        {
                            var csvFiles = Directory.EnumerateFiles(networkPath, "*.csv", SearchOption.TopDirectoryOnly)
                                .Where(f => Path.GetFileName(f).ToLower().Contains(dataType.ToLower()))
                                .ToList();

                            // Determine client token based on path
                            string clientToken = networkPath.Contains("Certco") ? "Certco" : "AWG";
                            
                            foreach (var file in csvFiles)
                            {
                                filePaths.Add((file, clientToken));
                                Console.WriteLine($"  - {Path.GetFileName(file)} (Client Token: {clientToken})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing network shares: {ex.Message}");
                throw;
            }

            return filePaths;
        }

        /// <summary>
        /// Copies CSV files from network shares to local directory for processing
        /// </summary>
        /// <param name="dataType">Type of data (email, loyalty, coupon)</param>
        /// <returns>Dictionary mapping file paths to client tokens</returns>
        public async Task<Dictionary<string, string>> CopyCsvFilesToLocalAsync(string dataType)
        {
            var fileToClientTokenMap = new Dictionary<string, string>();

            try
            {
                // Ensure local directory exists
                if (!Directory.Exists(_localDownloadPath))
                {
                    Directory.CreateDirectory(_localDownloadPath);
                    Console.WriteLine($"Created local directory: {_localDownloadPath}");
                }
                else
                {
                    // Clear existing files of this type from local downloads folder
                    var existingFiles = Directory.GetFiles(_localDownloadPath, $"*{dataType}*.csv");
                    foreach (var file in existingFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            Console.WriteLine($"Deleted existing file: {Path.GetFileName(file)}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Could not delete file {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                    if (existingFiles.Length > 0)
                    {
                        Console.WriteLine($"Cleared {existingFiles.Length} existing {dataType} file(s) from local folder");
                    }
                }

                // Get files from both network shares with their client tokens
                var networkFiles = await GetCsvFilesAsync(dataType);

                if (networkFiles.Count == 0)
                {
                    Console.WriteLine($"No {dataType} CSV files found in network shares");
                    return fileToClientTokenMap;
                }

                int filesCopied = 0;

                foreach (var (networkFile, clientToken) in networkFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(networkFile);
                        var localFilePath = Path.Combine(_localDownloadPath, fileName);

                        // Copy file from network share to local directory
                        File.Copy(networkFile, localFilePath, overwrite: true);
                        fileToClientTokenMap[localFilePath] = clientToken;
                        filesCopied++;
                        Console.WriteLine($"Copied: {fileName} to local directory (Client Token: {clientToken})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error copying file {Path.GetFileName(networkFile)}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Total {dataType} CSV files copied: {filesCopied}");
                return fileToClientTokenMap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying CSV files from network shares: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lists CSV files on the AWG network path whose names start with the given prefix (e.g. Aggregated_Email_Stats_).
        /// </summary>
        public List<string> EnumerateAwgCsvFilesByPrefix(string fileNamePrefix)
        {
            var results = new List<string>();
            if (string.IsNullOrEmpty(fileNamePrefix))
                return results;

            try
            {
                if (!Directory.Exists(_awgNetworkPath))
                {
                    Console.WriteLine($"AWG network path does not exist: {_awgNetworkPath}");
                    return results;
                }

                foreach (var path in Directory.EnumerateFiles(_awgNetworkPath, "*.csv", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(path);
                    if (name.StartsWith(fileNamePrefix, StringComparison.OrdinalIgnoreCase))
                        results.Add(path);
                }

                Console.WriteLine($"Found {results.Count} AWG CSV file(s) with prefix '{fileNamePrefix}' in {_awgNetworkPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing AWG CSV files: {ex.Message}");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Gets a specific CSV file path from network shares
        /// </summary>
        /// <param name="fileName">Name of the file to get</param>
        /// <returns>Tuple containing file path and client token, or (null, null) if not found</returns>
        public async Task<(string filePath, string clientToken)> GetCsvFileAsync(string fileName)
        {
            try
            {
                // Check Certco path first
                if (Directory.Exists(_certcoNetworkPath))
                {
                    var filePath = Path.Combine(_certcoNetworkPath, fileName);
                    if (File.Exists(filePath))
                    {
                        string clientToken = fileName.ToLower().Contains("email") ? "Certco" : "Certco";
                        Console.WriteLine($"Found file in Certco path: {filePath} (Client Token: {clientToken})");
                        return (filePath, clientToken);
                    }
                }

                // Check AWG path
                if (Directory.Exists(_awgNetworkPath))
                {
                    var filePath = Path.Combine(_awgNetworkPath, fileName);
                    if (File.Exists(filePath))
                    {
                        string clientToken = fileName.ToLower().Contains("awg") ? "AWG" : "AWG";
                        Console.WriteLine($"Found file in AWG path: {filePath} (Client Token: {clientToken})");
                        return (filePath, clientToken);
                    }
                }

                Console.WriteLine($"File not found in either network path: {fileName}");
                return (null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting file {fileName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Archives a CSV file from the source network path to the archive path
        /// </summary>
        /// <param name="sourceFilePath">Full path to the source file on the network share</param>
        /// <param name="clientToken">Client token (Certco or AWG) to determine archive path</param>
        /// <param name="dataType">Type of data (email, loyalty, coupon) to determine archive path</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ArchiveFileAsync(string sourceFilePath, string clientToken, string dataType = "email")
        {
            try
            {
                if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                {
                    Console.WriteLine($"Source file does not exist: {sourceFilePath}");
                    return false;
                }

                string archivePath;
                dataType = dataType?.ToLower() ?? "email";

                if (clientToken.Equals("Certco", StringComparison.OrdinalIgnoreCase) || clientToken.Equals("CERTCO", StringComparison.OrdinalIgnoreCase))
                {
                    if (dataType == "loyalty")
                    {
                        archivePath = _certcoLoyaltyArchivePath;
                    }
                    else if (dataType == "coupon")
                    {
                        archivePath = _certcoCouponArchivePath;
                    }
                    else // email (default)
                    {
                        archivePath = _certcoEmailArchivePath;
                    }
                }
                else if (clientToken.Equals("AWG", StringComparison.OrdinalIgnoreCase))
                {
                    // AWG only has email archive path currently
                    archivePath = _awgEmailArchivePath;
                }
                else
                {
                    Console.WriteLine($"Unknown client token: {clientToken}. Cannot determine archive path.");
                    return false;
                }

                // Ensure archive directory exists
                if (!Directory.Exists(archivePath))
                {
                    try
                    {
                        Directory.CreateDirectory(archivePath);
                        Console.WriteLine($"Created archive directory: {archivePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating archive directory {archivePath}: {ex.Message}");
                        return false;
                    }
                }

                var fileName = Path.GetFileName(sourceFilePath);
                var archiveFilePath = Path.Combine(archivePath, fileName);

                // Check if file already exists in archive (add timestamp if needed)
                if (File.Exists(archiveFilePath))
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var extension = Path.GetExtension(fileName);
                    fileName = $"{nameWithoutExt}_{timestamp}{extension}";
                    archiveFilePath = Path.Combine(archivePath, fileName);
                    Console.WriteLine($"File already exists in archive, using timestamped name: {fileName}");
                }

                // Move file from source to archive
                await Task.Run(() => File.Move(sourceFilePath, archiveFilePath));
                Console.WriteLine($"Successfully archived {Path.GetFileName(sourceFilePath)} to {archivePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error archiving file {sourceFilePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Archives multiple CSV files from source network paths to archive paths
        /// </summary>
        /// <param name="sourceFilePaths">Dictionary mapping source file paths to client tokens</param>
        /// <param name="dataType">Type of data (email, loyalty, coupon) to determine archive path</param>
        /// <returns>Number of files successfully archived</returns>
        public async Task<int> ArchiveFilesAsync(Dictionary<string, string> sourceFilePaths, string dataType = "email")
        {
            int successCount = 0;

            if (sourceFilePaths == null || sourceFilePaths.Count == 0)
            {
                return 0;
            }

            try
            {
                Console.WriteLine($"Archiving {sourceFilePaths.Count} file(s)...");

                foreach (var kvp in sourceFilePaths)
                {
                    var sourceFilePath = kvp.Key;
                    var clientToken = kvp.Value;

                    if (await ArchiveFileAsync(sourceFilePath, clientToken, dataType))
                    {
                        successCount++;
                    }
                }

                Console.WriteLine($"Successfully archived {successCount} of {sourceFilePaths.Count} file(s)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error archiving files: {ex.Message}");
            }

            return successCount;
        }
    }
}

