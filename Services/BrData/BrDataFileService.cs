using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Linq;

namespace DataStudioDataMgr.Services.BrData
{
    /// <summary>
    /// Service for accessing BrData POS CSV files from network share
    /// </summary>
    public class BrDataFileService
    {
        private readonly string _sourceNetworkPath;
        private readonly string _archivePath;
        private readonly string _localDownloadPath;

        public BrDataFileService()
        {
            // Network paths where BrData POS files are stored
            _sourceNetworkPath = ConfigurationManager.AppSettings["BrDataSourceNetworkPath"] ?? @"\\vm-fileserver\FTP\Certco\BRdata\IncomingPOS\";
            _archivePath = ConfigurationManager.AppSettings["BrDataArchivePath"] ?? @"\\vm-fileserver\FTP\Certco\BRdata\IncomingPOS\Archive\";
            
            // Local download path for processing
            _localDownloadPath = ConfigurationManager.AppSettings["BrDataLocalPath"] ?? "downloads\\brdata";
        }

        public BrDataFileService(string sourceNetworkPath, string archivePath, string localPath)
        {
            _sourceNetworkPath = sourceNetworkPath;
            _archivePath = archivePath;
            _localDownloadPath = localPath;
        }

        /// <summary>
        /// Gets all CSV files from the network share
        /// </summary>
        /// <returns>List of file paths with their associated client tokens</returns>
        public async Task<List<(string filePath, string clientToken)>> GetCsvFilesAsync()
        {
            var filePaths = new List<(string filePath, string clientToken)>();

            try
            {
                Console.WriteLine($"Accessing BrData network share: {_sourceNetworkPath}");
                if (Directory.Exists(_sourceNetworkPath))
                {
                    var csvFiles = Directory.EnumerateFiles(_sourceNetworkPath, "*.csv", SearchOption.TopDirectoryOnly)
                        .ToList();

                    Console.WriteLine($"Found {csvFiles.Count} BrData POS CSV files");
                    foreach (var file in csvFiles)
                    {
                        filePaths.Add((file, "CERTCO"));
                        Console.WriteLine($"  - {Path.GetFileName(file)} (Client Token: CERTCO)");
                    }
                }
                else
                {
                    Console.WriteLine($"BrData network path does not exist: {_sourceNetworkPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing network share: {ex.Message}");
                throw;
            }

            return filePaths;
        }

        /// <summary>
        /// Copies CSV files from network share to local directory for processing
        /// </summary>
        /// <returns>Dictionary mapping file paths to client tokens</returns>
        public async Task<Dictionary<string, string>> CopyCsvFilesToLocalAsync()
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
                    // Clear existing files from local downloads folder
                    var existingFiles = Directory.GetFiles(_localDownloadPath, "*.csv");
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
                        Console.WriteLine($"Cleared {existingFiles.Length} existing file(s) from local folder");
                    }
                }

                // Get files from network share with their client tokens
                var networkFiles = await GetCsvFilesAsync();

                if (networkFiles.Count == 0)
                {
                    Console.WriteLine("No POS CSV files found in network share");
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

                Console.WriteLine($"Total POS CSV files copied: {filesCopied}");
                return fileToClientTokenMap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying CSV files from network share: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a specific CSV file path from network share
        /// </summary>
        /// <param name="fileName">Name of the file to get</param>
        /// <returns>Tuple containing file path and client token, or (null, null) if not found</returns>
        public async Task<(string filePath, string clientToken)> GetCsvFileAsync(string fileName)
        {
            try
            {
                if (Directory.Exists(_sourceNetworkPath))
                {
                    var filePath = Path.Combine(_sourceNetworkPath, fileName);
                    if (File.Exists(filePath))
                    {
                        Console.WriteLine($"Found file in BrData path: {filePath} (Client Token: CERTCO)");
                        return (filePath, "CERTCO");
                    }
                }

                Console.WriteLine($"File not found in network path: {fileName}");
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
        /// <param name="clientToken">Client token (CERTCO)</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ArchiveFileAsync(string sourceFilePath, string clientToken)
        {
            try
            {
                if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                {
                    Console.WriteLine($"Source file does not exist: {sourceFilePath}");
                    return false;
                }

                // Ensure archive directory exists
                if (!Directory.Exists(_archivePath))
                {
                    try
                    {
                        Directory.CreateDirectory(_archivePath);
                        Console.WriteLine($"Created archive directory: {_archivePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating archive directory {_archivePath}: {ex.Message}");
                        return false;
                    }
                }

                var fileName = Path.GetFileName(sourceFilePath);
                var archiveFilePath = Path.Combine(_archivePath, fileName);

                // Check if file already exists in archive (add timestamp if needed)
                if (File.Exists(archiveFilePath))
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var extension = Path.GetExtension(fileName);
                    fileName = $"{nameWithoutExt}_{timestamp}{extension}";
                    archiveFilePath = Path.Combine(_archivePath, fileName);
                    Console.WriteLine($"File already exists in archive, using timestamped name: {fileName}");
                }

                // Move file from source to archive
                await Task.Run(() => File.Move(sourceFilePath, archiveFilePath));
                Console.WriteLine($"Successfully archived {Path.GetFileName(sourceFilePath)} to {_archivePath}");
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
        /// <returns>Number of files successfully archived</returns>
        public async Task<int> ArchiveFilesAsync(Dictionary<string, string> sourceFilePaths)
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

                    if (await ArchiveFileAsync(sourceFilePath, clientToken))
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


