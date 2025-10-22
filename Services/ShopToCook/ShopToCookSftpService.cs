using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;
using System.Configuration;
using System.Linq;

namespace DataStudioDataMgr.Services.ShopToCook
{
    /// <summary>
    /// Service for downloading CSV files from ShopToCook SFTP server
    /// </summary>
    public class ShopToCookSftpService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly string _localDownloadPath;

        public ShopToCookSftpService()
        {
            _host = ConfigurationManager.AppSettings["ShopToCookSftpHost"] ?? "sftp.ecomsystems.com";
            _port = int.Parse(ConfigurationManager.AppSettings["ShopToCookSftpPort"] ?? "22");
            _username = ConfigurationManager.AppSettings["ShopToCookSftpUsername"] ?? "ShopToCookAdmin";
            
            // Expand environment variables in SFTP password
            var password = ConfigurationManager.AppSettings["ShopToCookSftpPassword"] ?? "GFGf4l3pv8ZIluhnPFD6u6NRMRuPgEBa";
            _password = Environment.ExpandEnvironmentVariables(password);
            
            _localDownloadPath = ConfigurationManager.AppSettings["ShopToCookLocalPath"] ?? "downloads";
        }

        public ShopToCookSftpService(string host, int port, string username, string password, string localPath)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _localDownloadPath = localPath;
        }

        /// <summary>
        /// Downloads all ShopToCook CSV files from SFTP server subdirectories
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

                Console.WriteLine($"Connecting to SFTP server: {_host}:{_port}");
                
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    await Task.Run(() => client.Connect());
                    
                    if (client.IsConnected)
                    {
                        Console.WriteLine("Successfully connected to SFTP server");
                        
                        // Define the subdirectories to search
                        var subdirectories = new[] { "EmailData", "KioskData", "WebsiteData" };
                        int totalFilesDownloaded = 0;
                        
                        foreach (var subdir in subdirectories)
                        {
                            Console.WriteLine($"Searching in subdirectory: {subdir}");
                            
                            try
                            {
                                // List files in the subdirectory
                                var files = await Task.Run(() => client.ListDirectory($"./{subdir}"));
                                
                                Console.WriteLine($"Found {files.Count()} files in {subdir}");
                                
                                // Download CSV files from this subdirectory
                                foreach (var file in files)
                                {
                                    if (file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Console.WriteLine($"Downloading file: {subdir}/{file.Name}");
                                        
                                        // Create subdirectory-specific filename to avoid conflicts
                                        var localFileName = $"{subdir}_{file.Name}";
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
                        throw new Exception("Failed to connect to SFTP server");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading files from SFTP: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Downloads a specific CSV file from SFTP server subdirectories
        /// </summary>
        /// <param name="remoteFileName">Name of the file to download (without subdirectory)</param>
        /// <param name="subdirectory">Subdirectory to search in (EmailData, KioskData, WebsiteData)</param>
        /// <returns>Local file path of downloaded file</returns>
        public async Task<string> DownloadCsvFileAsync(string remoteFileName, string subdirectory = null)
        {
            try
            {
                // Ensure local directory exists
                if (!Directory.Exists(_localDownloadPath))
                {
                    Directory.CreateDirectory(_localDownloadPath);
                }

                Console.WriteLine($"Connecting to SFTP server to download: {remoteFileName}");
                
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    await Task.Run(() => client.Connect());
                    
                    if (client.IsConnected)
                    {
                        Console.WriteLine("Successfully connected to SFTP server");
                        
                        string remotePath = null;
                        string localFileName = null;
                        
                        if (!string.IsNullOrEmpty(subdirectory))
                        {
                            // Download from specific subdirectory
                            remotePath = $"./{subdirectory}/{remoteFileName}";
                            localFileName = $"{subdirectory}_{remoteFileName}";
                        }
                        else
                        {
                            // Search in all subdirectories
                            var subdirectories = new[] { "EmailData", "KioskData", "WebsiteData" };
                            
                            foreach (var subdir in subdirectories)
                            {
                                try
                                {
                                    var files = await Task.Run(() => client.ListDirectory($"./{subdir}"));
                                    var file = files.FirstOrDefault(f => f.Name.Equals(remoteFileName, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (file != null)
                                    {
                                        remotePath = $"./{subdir}/{remoteFileName}";
                                        localFileName = $"{subdir}_{remoteFileName}";
                                        Console.WriteLine($"Found file in subdirectory: {subdir}");
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error searching in {subdir}: {ex.Message}");
                                }
                            }
                            
                            if (string.IsNullOrEmpty(remotePath))
                            {
                                throw new FileNotFoundException($"File {remoteFileName} not found in any subdirectory");
                            }
                        }
                        
                        var localFilePath = Path.Combine(_localDownloadPath, localFileName);
                        
                        using (var localFileStream = File.Create(localFilePath))
                        {
                            await Task.Run(() => client.DownloadFile(remotePath, localFileStream));
                        }
                        
                        Console.WriteLine($"Successfully downloaded: {remotePath} as {localFileName}");
                        return localFilePath;
                    }
                    else
                    {
                        throw new Exception("Failed to connect to SFTP server");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file {remoteFileName} from SFTP: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lists all files in SFTP server subdirectories
        /// </summary>
        /// <returns>List of file names with their subdirectory paths</returns>
        public async Task<List<string>> ListFilesAsync()
        {
            var fileNames = new List<string>();
            
            try
            {
                Console.WriteLine($"Connecting to SFTP server to list files: {_host}:{_port}");
                
                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    await Task.Run(() => client.Connect());
                    
                    if (client.IsConnected)
                    {
                        Console.WriteLine("Successfully connected to SFTP server");
                        
                        // Define the subdirectories to search
                        var subdirectories = new[] { "EmailData", "KioskData", "WebsiteData" };
                        
                        foreach (var subdir in subdirectories)
                        {
                            try
                            {
                                Console.WriteLine($"Listing files in subdirectory: {subdir}");
                                var files = await Task.Run(() => client.ListDirectory($"./{subdir}"));
                                
                                foreach (var file in files)
                                {
                                    if (!file.Name.StartsWith(".")) // Skip hidden files
                                    {
                                        var filePath = $"{subdir}/{file.Name}";
                                        fileNames.Add(filePath);
                                        Console.WriteLine($"Found file: {filePath}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error listing files in {subdir}: {ex.Message}");
                                // Continue with other subdirectories
                            }
                        }
                        
                        Console.WriteLine($"Total files found: {fileNames.Count}");
                    }
                    else
                    {
                        throw new Exception("Failed to connect to SFTP server");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error listing files from SFTP: {ex.Message}");
                throw;
            }

            return fileNames;
        }
    }
}
