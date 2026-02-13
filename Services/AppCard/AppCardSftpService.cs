using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataStudioDataMgr.Services.AppCard
{
    /// <summary>
    /// Service for downloading CSV files from AppCard SFTP server
    /// </summary>
    public class AppCardSftpService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _localDownloadPath;
        private readonly string _remotePath;

        public AppCardSftpService()
        {
            // Read SFTP host from environment variable first, then App.config, then default
            var hostEnv = System.Environment.GetEnvironmentVariable("APPCARD_SFTP_HOST");
            //var host = hostEnv ?? ConfigurationManager.AppSettings["AppCardSftpHost"] ?? "us-east-1.sftpcloud.io";
            var host = "sftp.ecomsystems.com";

            // Expand environment variables in case App.config contains %VAR% format
            _host = System.Environment.ExpandEnvironmentVariables(host);
            
            _port = int.Parse(ConfigurationManager.AppSettings["AppCardSftpPort"] ?? "22");
            _username = ConfigurationManager.AppSettings["AppCardSftpUsername"] ?? "awgAppCard";
            
            // Read SFTP password from environment variable first, then App.config, then default
            var passwordEnv = System.Environment.GetEnvironmentVariable("APPCARD_SFTP_PASSWORD");
            var password = passwordEnv ?? ConfigurationManager.AppSettings["AppCardSftpPassword"] ?? "";
            
            // Expand environment variables in case App.config contains %VAR% format
            //_password = System.Environment.ExpandEnvironmentVariables(password);
            _password = "mwQ96H0qBPO1eKKwbRpTSnpQIlcR2yDE";

            _localDownloadPath = ConfigurationManager.AppSettings["AppCardLocalPath"] ?? "downloads\\appcard";
            _remotePath = ConfigurationManager.AppSettings["AppCardSftpRemotePath"] ?? "awg_appcard/Incoming";
        }

        public AppCardSftpService(string host, int port, string username, string password, string localPath, string remotePath)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _localDownloadPath = localPath;
            _remotePath = remotePath;
        }

        /// <summary>
        /// Downloads all AppCard CSV files from SFTP server
        /// </summary>
        /// <returns>Local download directory path</returns>
        public async Task<string> DownloadCsvFilesAsync()
        {
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
                    // Clear existing files from downloads folder
                    var existingFiles = Directory.GetFiles(_localDownloadPath);
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
                        Console.WriteLine($"Cleared {existingFiles.Length} existing file(s) from downloads folder");
                    }
                }

                Console.WriteLine($"Connecting to AppCard SFTP server: {_host}:{_port}");
                
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    await Task.Run(() => client.Connect());
                    
                    if (client.IsConnected)
                    {
                        Console.WriteLine("Successfully connected to SFTP server");

                        // Define the subdirectories to search (exclude Archive directory)
                        var subdirectories = new[] { "Incoming" };
                        int totalFilesDownloaded = 0;

                        foreach (var subdir in subdirectories)
                        {
                            // Skip Archive directory explicitly
                            if (subdir.Equals("Archive", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            Console.WriteLine($"Searching in subdirectory: {subdir}");

                            try
                            {
                                // List files in the subdirectory
                                var files = await Task.Run(() => client.ListDirectory($"./{subdir}"));

                                Console.WriteLine($"Found {files.Count()} files in {subdir}");

                                // Download CSV files from this subdirectory
                                // Skip files in Archive subdirectories
                                foreach (var file in files)
                                {
                                    // Skip Archive directory and files within Archive subdirectories
                                    if (file.IsDirectory && file.Name.Equals("Archive", StringComparison.OrdinalIgnoreCase))
                                    {
                                        continue;
                                    }

                                    if (file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Console.WriteLine($"Downloading file: {subdir}/{file.Name}");

                                        // Use original filename without prefix
                                        var localFileName = file.Name;
                                        var localFilePath = Path.Combine(_localDownloadPath, localFileName);

                                        using (var localFileStream = File.Create(localFilePath))
                                        {
                                            await Task.Run(() => client.DownloadFile($"./{subdir}/{file.Name}", localFileStream));
                                        }

                                        totalFilesDownloaded++;
                                        Console.WriteLine($"Successfully downloaded: {subdir}/{file.Name} as {localFileName}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error accessing subdirectory {subdir}: {ex.Message}");
                                // Continue with other subdirectories
                            }
                        }

                        if (totalFilesDownloaded == 0)
                        {
                            Console.WriteLine("No CSV files found in any subdirectories");
                        }
                        else
                        {
                            Console.WriteLine($"Total CSV files downloaded: {totalFilesDownloaded}");
                        }

                        return _localDownloadPath;
                    }
                    else
                    {
                        throw new Exception("Failed to connect to AppCard SFTP server");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading files from AppCard SFTP: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Downloads a specific CSV file from SFTP server
        /// </summary>
        /// <param name="remoteFileName">Name of the file to download</param>
        /// <returns>Local file path of downloaded file</returns>
        public async Task<string> DownloadCsvFileAsync(string remoteFileName)
        {
            try
            {
                // Ensure local directory exists
                if (!Directory.Exists(_localDownloadPath))
                {
                    Directory.CreateDirectory(_localDownloadPath);
                }

                Console.WriteLine($"Connecting to AppCard SFTP server to download: {remoteFileName}");
                
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    await Task.Run(() => client.Connect());
                    
                    if (client.IsConnected)
                    {
                        Console.WriteLine("Successfully connected to AppCard SFTP server");
                        
                        string remotePath = $"./{_remotePath}/{remoteFileName}";
                        var localFilePath = Path.Combine(_localDownloadPath, remoteFileName);
                        
                        using (var localFileStream = File.Create(localFilePath))
                        {
                            await Task.Run(() => client.DownloadFile(remotePath, localFileStream));
                        }
                        
                        Console.WriteLine($"Successfully downloaded: {remotePath} as {remoteFileName}");
                        return localFilePath;
                    }
                    else
                    {
                        throw new Exception("Failed to connect to AppCard SFTP server");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file {remoteFileName} from AppCard SFTP: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lists all files in SFTP server directory
        /// </summary>
        /// <returns>List of file names</returns>
        public async Task<List<string>> ListFilesAsync()
        {
            var fileNames = new List<string>();
            
            try
            {
                Console.WriteLine($"Connecting to AppCard SFTP server to list files: {_host}:{_port}");
                
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    await Task.Run(() => client.Connect());
                    
                    if (client.IsConnected)
                    {
                        Console.WriteLine("Successfully connected to AppCard SFTP server");
                        Console.WriteLine($"Listing files in: {_remotePath}");
                        
                        var files = await Task.Run(() => client.ListDirectory($"./{_remotePath}"));
                        
                        foreach (var file in files)
                        {
                            if (!file.Name.StartsWith(".") && !file.IsDirectory) // Skip hidden files and directories
                            {
                                var filePath = $"{_remotePath}/{file.Name}";
                                fileNames.Add(filePath);
                                Console.WriteLine($"Found file: {filePath}");
                            }
                        }
                        
                        Console.WriteLine($"Total files found: {fileNames.Count}");
                    }
                    else
                    {
                        throw new Exception("Failed to connect to AppCard SFTP server");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing files from AppCard SFTP: {ex.Message}");
                throw;
            }

            return fileNames;
        }

        /// <summary>
        /// Moves a file from the Incoming folder to the Archive folder on SFTP server
        /// </summary>
        /// <param name="fileName">Name of the file to move</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> MoveFileToArchiveAsync(string fileName)
        {
            try
            {
                Console.WriteLine($"Moving file to archive: {fileName}");
                
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    await Task.Run(() => client.Connect());
                    
                    if (client.IsConnected)
                    {
                        // Use the same path format as download: ./Incoming (download doesn't change directory)
                        string sourcePath = $"./Incoming/{fileName}";
                        string archivePath = $"./awg_appcard/Archive/{fileName}";
                        
                        Console.WriteLine($"Copying file from {sourcePath} to {archivePath}");
                        
                        // Step 1: Copy file from Incoming to Archive
                        using (var sourceStream = new System.IO.MemoryStream())
                        {
                            await Task.Run(() => client.DownloadFile(sourcePath, sourceStream));
                            sourceStream.Position = 0;
                            
                            await Task.Run(() => client.UploadFile(sourceStream, archivePath));
                            Console.WriteLine($"Successfully copied {fileName} to Archive folder");
                        }
                        
                        // Step 2: Delete file from Incoming (only if copy was successful)
                        await Task.Run(() => client.DeleteFile(sourcePath));
                        Console.WriteLine($"Successfully deleted {fileName} from Incoming folder");
                        
                        Console.WriteLine($"Successfully moved {fileName} to Archive folder");
                        return true;
                    }
                    else
                    {
                        throw new Exception("Failed to connect to AppCard SFTP server");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving file {fileName} to archive: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Moves multiple files from the Incoming folder to the Archive folder on SFTP server
        /// </summary>
        /// <param name="fileNames">List of file names to move</param>
        /// <returns>Number of files successfully moved</returns>
        public async Task<int> MoveFilesToArchiveAsync(List<string> fileNames)
        {
            int successCount = 0;
            
            if (fileNames == null || fileNames.Count == 0)
            {
                return 0;
            }
            
            try
            {
                Console.WriteLine($"Moving {fileNames.Count} file(s) to archive...");
                
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    await Task.Run(() => client.Connect());
                    
                    if (client.IsConnected)
                    {
                        // Use the same path format as download: ./Incoming (download doesn't change directory)
                        foreach (var fileName in fileNames)
                        {
                            try
                            {
                                string sourcePath = $"./Incoming/{fileName}";
                                string archivePath = $"./Archive/{fileName}";
                                
                                Console.WriteLine($"Copying file from {sourcePath} to {archivePath}");
                                
                                // Step 1: Copy file from Incoming to Archive
                                using (var sourceStream = new System.IO.MemoryStream())
                                {
                                    await Task.Run(() => client.DownloadFile(sourcePath, sourceStream));
                                    sourceStream.Position = 0;
                                    
                                    await Task.Run(() => client.UploadFile(sourceStream, archivePath));
                                    Console.WriteLine($"Successfully copied {fileName} to Archive folder");
                                }
                                
                                // Step 2: Delete file from Incoming (only if copy was successful)
                                await Task.Run(() => client.DeleteFile(sourcePath));
                                Console.WriteLine($"Successfully deleted {fileName} from Incoming folder");
                                
                                Console.WriteLine($"Successfully moved {fileName} to Archive folder");
                                successCount++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error moving file {fileName} to archive: {ex.Message}");
                                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                                if (ex.InnerException != null)
                                {
                                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Failed to connect to AppCard SFTP server");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving files to archive: {ex.Message}");
            }
            
            return successCount;
        }
    }
}

