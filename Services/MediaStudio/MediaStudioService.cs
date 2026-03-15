using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.MediaStudio
{
    /// <summary>
    /// Service for handling MediaStudio SQL Server database operations
    /// </summary>
    public class MediaStudioService
    {
        private readonly string _connectionString;

        public MediaStudioService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MediaStudio"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("MediaStudio connection string not found in configuration");
            }
        }

        public MediaStudioService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Retrieves UNFI Post Campaign Store data from the MediaStudio database
        /// </summary>
        /// <returns>List of UNFI Post Campaign Store records</returns>
        public async Task<List<UnfiPostCampaignStore>> GetUnfiPostCampaignStoreAsync()
        {
            var records = new List<UnfiPostCampaignStore>();
            string query = @"select 
            'UNFI' as ClientToken
            ,pcs.Id
            , isnull(pcs.GroupCampaignId,0) as GroupCampaignId
            , pcs.CustomerGuid
            , pcs.ExternalId
            , isnull(pcs.SecondaryExternalId, '') as SecondaryExternalId
            , pcs.Url
            from Apollo.[MediaStudio].[Posts].[UNFIPostCampaignStore] pcs with (nolock)";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Connected to MediaStudio database successfully");

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new UnfiPostCampaignStore
                                {
                                    ClientToken = reader.IsDBNull(0) ? string.Empty : reader.GetString(0), // ClientToken
                                    Id = reader.GetInt32(1), // Id
                                    GroupCampaignId = reader.GetInt32(2), // GroupCampaignId
                                    CustomerGuid = reader.IsDBNull(3) ? string.Empty : reader.GetString(3), // CustomerGuid
                                    ExternalId = reader.IsDBNull(4) ? string.Empty : reader.GetString(4), // ExternalId
                                    SecondaryExternalId = reader.IsDBNull(5) ? string.Empty : reader.GetString(5), // SecondaryExternalId
                                    Url = reader.IsDBNull(6) ? string.Empty : reader.GetString(6) // Url
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                Console.WriteLine($"Retrieved {records.Count} UNFI Post Campaign Store records from database");
                return records;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error retrieving UNFI Post Campaign Store records: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving UNFI Post Campaign Store records: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves UNFI Post Campaign Store data to JSON file
        /// </summary>
        /// <param name="records">List of records to save</param>
        /// <param name="filePath">Path where to save the JSON file</param>
        public async Task SaveUnfiPostCampaignStoreToJsonAsync(List<UnfiPostCampaignStore> records, string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(records, Formatting.Indented);
                await Task.Run(() => File.WriteAllText(filePath, json));
                Console.WriteLine($"UNFI Post Campaign Store data saved to JSON file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving UNFI Post Campaign Store data to JSON: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves UNFI Post Campaign Store data to CSV file
        /// </summary>
        /// <param name="records">List of records to save</param>
        /// <param name="filePath">Path where to save the CSV file</param>
        public async Task SaveUnfiPostCampaignStoreToCsvAsync(List<UnfiPostCampaignStore> records, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Write CSV header
                    await writer.WriteLineAsync("ClientToken,Id,GroupCampaignId,CustomerGuid,ExternalId,SecondaryExternalId,Url");

                    // Write data rows
                    foreach (var record in records)
                    {
                        var line = $"\"{EscapeCsvField(record.ClientToken)}\",{record.Id},{record.GroupCampaignId},\"{EscapeCsvField(record.CustomerGuid)}\",\"{EscapeCsvField(record.ExternalId)}\",\"{EscapeCsvField(record.SecondaryExternalId)}\",\"{EscapeCsvField(record.Url)}\"";
                        await writer.WriteLineAsync(line);
                    }
                }

                Console.WriteLine($"UNFI Post Campaign Store data saved to CSV file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving UNFI Post Campaign Store data to CSV: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tests the MediaStudio database connection
        /// </summary>
        /// <returns>True if connection is successful</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("MediaStudio database connection test successful");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MediaStudio database connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Escapes CSV field values to handle commas and quotes
        /// </summary>
        /// <param name="field">Field value to escape</param>
        /// <returns>Escaped field value</returns>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // Replace quotes with double quotes and handle commas
            return field.Replace("\"", "\"\"");
        }
    }
}
