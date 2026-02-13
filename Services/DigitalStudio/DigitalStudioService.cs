using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.DigitalStudio
{
    /// <summary>
    /// Service for handling DigitalStudio SQL Server database operations
    /// </summary>
    public class DigitalStudioService
    {
        private readonly string _connectionString;

        public DigitalStudioService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DigitalStudio"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("DigitalStudio connection string not found in configuration");
            }
        }

        public DigitalStudioService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Retrieves Campaign Store Metrics data from the DigitalStudio database
        /// </summary>
        /// <returns>List of Campaign Store Metrics records</returns>
        public async Task<List<CampaignStoreMetrics>> GetCampaignStoreMetricsAsync()
        {
            var records = new List<CampaignStoreMetrics>();
//            string query = @"

//WITH DailyTotals AS (
//    SELECT
//        pt.[Name] AS Platform,
//        mt.[Name] AS MetricType,
//        mt.Id AS MetricTypeId,
//        p.Id AS PostID,
//        p.[Name] AS PostName,
//        CAST(p.ScheduledDate AS DATETIME) AS PostScheduledDate,
//        CAST(p.PublishedDate AS DATETIME) AS PostPublishedDate,
//        pmet.[Date] AS MetricDate,
//        ISNULL(GroupCode, 'N/A') AS Customer,
//        ap.[Name] AS Store,
//        SUM(CASE WHEN [Count] >= 0 THEN [Count] ELSE 0 END) AS TotalMetricValue
//    FROM Posts.Post p
//    JOIN metrics.PostMetric pmet ON pmet.PostId = p.[Id]
//    JOIN Types.MetricType mt ON mt.Id = pmet.MetricTypeId
//    JOIN Posts.PostTarget ptar ON ptar.PostId = p.[Id]
//    JOIN Posts.PostTargetAccountPlatform ptap ON ptap.PostTargetId = ptar.[Id]
//    JOIN Accounts.AccountPlatform ap ON ap.Id = ptap.AccountPlatformId
//    JOIN Platforms.PlatformTarget pt ON pt.Id = ptar.PlatformTargetId
//    WHERE
//        p.[Name] NOT LIKE '%test%'
//        --AND ap.[Name] = 'Anacortes Health and Nutrition'
//        --AND p.[Name] = 'Perfect Bar Recipe - Scheduled to Post - 05/25/2025'
//    GROUP BY
//        pt.[Name], mt.[Name], mt.Id, p.Id, p.[Name], CAST(p.ScheduledDate AS DATETIME),
//        CAST(p.PublishedDate AS DATETIME), pmet.[Date], ISNULL(GroupCode, 'N/A'), ap.[Name]
//),
//WithDelta AS (
//    SELECT *,
//        TotalMetricValue - LAG(TotalMetricValue) OVER (
//            PARTITION BY Store, MetricTypeId, Platform, PostID
//            ORDER BY MetricDate
//        ) AS MetricDelta
//    FROM DailyTotals
//)
//SELECT
//Platform
//,MetricType
//, convert(varchar,MetricTypeId) as MetricTypeId
//, convert(varchar,PostID) as PostID
//, PostName
//, IsNull(convert(varchar,PostScheduledDate, 110), '01-01-1900') as PostScheduledDate
//, IsNull(convert(varchar,PostPublishedDate, 110), '01-01-1900') as PostPublishedDate
//, IsNull(convert(varchar,MetricDate, 110),'01-01-1900') as MetricDate
//, Customer as ClientToken
//, Store
//, isnull(TotalMetricValue,0) as TotalMetricValue
//, isnull(MetricDelta,0) as MetricDelta
//FROM WithDelta
//ORDER BY MetricDate DESC, PostID, Store, MetricType";

            string query = @"
WITH BaseData AS (
    SELECT
        pt.[Name] AS Platform,
        mt.[Name] AS MetricType,
        mt.Id AS MetricTypeId,
        p.Id AS PostID,
        p.[Name] AS PostName,
		ptar.MetaData,
        pm.[Url],
        CAST(p.ScheduledDate AS DATETIME) AS ScheduledDate,
        CAST(p.PublishedDate AS DATETIME) AS PublishedDate,
        pmet.[Date] AS MetricDate,
        ISNULL(GroupCode, 'N/A') AS Customer,
        ap.[Name] AS Store,
        CASE WHEN [Count] >= 0 THEN [Count] ELSE 0 END AS MetricValue
    FROM Posts.Post p
    INNER JOIN Posts.PostTarget ptar ON ptar.PostId = p.Id
    INNER JOIN Posts.PostTargetAccountPlatform ptap ON ptap.PostTargetId = ptar.Id
    INNER JOIN metrics.PostMetric pmet ON pmet.PostTargetAccountPlatformId = ptap.Id
    INNER JOIN Types.MetricType mt ON mt.Id = pmet.MetricTypeId
    INNER JOIN Accounts.AccountPlatform ap ON ap.Id = ptap.AccountPlatformId
    INNER JOIN Platforms.PlatformTarget pt ON pt.Id = ptar.PlatformTargetId
    INNER JOIN Posts.PostMedia pm ON pm.PostId = p.Id AND pm.PlatformTargetId = pt.Id
    WHERE p.[Name] NOT LIKE '%test%'
),
DailyTotals AS (
    SELECT
        Platform, MetricType, MetricTypeId, PostID, PostName, MetaData, [Url],
        CAST(ScheduledDate AS DATETIME2) AS PostScheduledDate,
        CAST(PublishedDate AS DATETIME2) AS PostPublishedDate,
        MetricDate, Customer, Store,
        SUM(MetricValue) AS TotalMetricValue
    FROM BaseData
    GROUP BY Platform, MetricType, MetricTypeId, PostID, PostName, MetaData, [Url],
             CAST(ScheduledDate AS DATETIME2), CAST(PublishedDate AS DATETIME2),
             MetricDate, Customer, Store
),
WithDelta AS (
    SELECT *,
        ISNULL(
            TotalMetricValue - LAG(TotalMetricValue) OVER (
                PARTITION BY Store, MetricTypeId, Platform, PostID
                ORDER BY MetricDate
            ),
            TotalMetricValue
        ) AS MetricDelta
    FROM DailyTotals
)
SELECT
Platform
,MetricType
, convert(varchar,MetricTypeId) as MetricTypeId
, convert(varchar,PostID) as PostID
, PostName
, MetaData
, Url
, IsNull(convert(varchar,PostScheduledDate, 110), '01-01-1900') as PostScheduledDate
, IsNull(convert(varchar,PostPublishedDate, 110), '01-01-1900') as PostPublishedDate
, IsNull(convert(varchar,MetricDate, 110),'01-01-1900') as MetricDate
, Customer as ClientToken
, Store
, isnull(TotalMetricValue,0) as TotalMetricValue
, isnull(MetricDelta,0) as MetricDelta
FROM WithDelta
ORDER BY MetricDate DESC, PostID, Store, MetricType";
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Connected to DigitalStudio database successfully");

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new CampaignStoreMetrics
                                {
                                    Platform = reader.IsDBNull(0) ? string.Empty : reader.GetString(0), // Platform
                                    MetricType = reader.IsDBNull(1) ? string.Empty : reader.GetString(1), // MetricType
                                    MetricTypeId = reader.IsDBNull(2) ? string.Empty : reader.GetString(2), // MetricTypeId
                                    PostID = reader.IsDBNull(3) ? string.Empty : reader.GetString(3), // PostID
                                    PostName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4), // PostName
                                    MetaData = reader.IsDBNull(5) ? string.Empty : reader.GetString(5), // MetaData
                                    Url = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                    PostScheduledDate = reader.IsDBNull(5) ? string.Empty : reader.GetString(7), // PostScheduledDate
                                    PostPublishedDate = reader.IsDBNull(5) ? string.Empty : reader.GetString(8), // PostPublishedDate
                                    MetricDate = reader.IsDBNull(6) ? string.Empty : reader.GetString(9), // MetricDate
                                    ClientToken = reader.IsDBNull(7) ? string.Empty : reader.GetString(10), // ClientToken
                                    Store = reader.IsDBNull(8) ? string.Empty : reader.GetString(11), // Store
                                    TotalMetricValue = reader.GetInt32(12), // TotalMetricValue
                                    MetricDelta = reader.GetInt32(13) // MetricDelta
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                Console.WriteLine($"Retrieved {records.Count} Campaign Store Metrics records from database");
                return records;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error retrieving Campaign Store Metrics records: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Campaign Store Metrics records: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves Campaign Store Metrics data to JSON file
        /// </summary>
        /// <param name="records">List of records to save</param>
        /// <param name="filePath">Path where to save the JSON file</param>
        public async Task SaveCampaignStoreMetricsToJsonAsync(List<CampaignStoreMetrics> records, string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(records, Formatting.Indented);
                await Task.Run(() => File.WriteAllText(filePath, json));
                Console.WriteLine($"Campaign Store Metrics data saved to JSON file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving Campaign Store Metrics data to JSON: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves Campaign Store Metrics data to CSV file
        /// </summary>
        /// <param name="records">List of records to save</param>
        /// <param name="filePath">Path where to save the CSV file</param>
        public async Task SaveCampaignStoreMetricsToCsvAsync(List<CampaignStoreMetrics> records, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Write CSV header
                    await writer.WriteLineAsync("Platform,MetricType,MetricTypeId,PostID,PostName,PostScheduledDate,MetricDate,ClientToken,Store,TotalMetricValue,MetricDelta");

                    // Write data rows
                    foreach (var record in records)
                    {
                        var line = $"\"{EscapeCsvField(record.Platform)}\",\"{EscapeCsvField(record.MetricType)}\",\"{EscapeCsvField(record.MetricTypeId)}\",\"{EscapeCsvField(record.PostID)}\",\"{EscapeCsvField(record.PostName)}\",\"{EscapeCsvField(record.PostScheduledDate)}\",\"{EscapeCsvField(record.MetricDate)}\",\"{EscapeCsvField(record.ClientToken)}\",\"{EscapeCsvField(record.Store)}\",{record.TotalMetricValue},{record.MetricDelta}";
                        await writer.WriteLineAsync(line);
                    }
                }

                Console.WriteLine($"Campaign Store Metrics data saved to CSV file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving Campaign Store Metrics data to CSV: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves Entry Metrics data from the DigitalStudio database
        /// </summary>
        /// <returns>List of Entry Metrics records</returns>
        public async Task<List<EntryMetrics>> GetEntryMetricsAsync()
        {
            var records = new List<EntryMetrics>();
            string query = @"select 
a.Id AS AccountId 
, a.Name AS AccountName 
, a.GroupCode AS Customer 
, b.Name AS CampaignName 
, em.EntryId AS EntryId 
,  et.Name AS EntryType 
 ,mt.Name AS MetricType 
 , CAST(CAST(em.[Timestamp] AS datetime) AS date) as [Timestamp] 
, e.MetaData  
FROM Metrics.EntryMetric em with (nolock) 
JOIN Accounts.Account a with (nolock) ON a.Id = em.AccountId 
JOIN Entries.Entry e with (nolock) ON e.Id = em.EntryId 
JOIN Entries.Batch b with (nolock) ON b.Id = e.BatchId 
JOIN Types.EntryType et with (nolock) ON et.Id = em.EntryTypeId 
JOIN Types.MetricType mt with (nolock) ON mt.Id = em.MetricTypeId";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Connected to DigitalStudio database successfully for Entry Metrics");

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new EntryMetrics
                                {
                                    AccountId = reader.GetInt32(0), // AccountId
                                    AccountName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1), // AccountName
                                    Customer = reader.IsDBNull(2) ? string.Empty : reader.GetString(2), // Customer
                                    CampaignName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3), // CampaignName
                                    EntryId = reader.GetInt32(4), // EntryId
                                    EntryType = reader.IsDBNull(5) ? string.Empty : reader.GetString(5), // EntryType
                                    MetricType = reader.IsDBNull(6) ? string.Empty : reader.GetString(6), // MetricType
                                    Timestamp = reader.IsDBNull(7) ? string.Empty : reader.GetString(7), // Timestamp
                                    MetaData = reader.IsDBNull(8) ? string.Empty : reader.GetString(8) // MetaData
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                Console.WriteLine($"Retrieved {records.Count} Entry Metrics records from database");
                return records;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error retrieving Entry Metrics records: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Entry Metrics records: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves Entry Metrics data to JSON file
        /// </summary>
        /// <param name="records">List of records to save</param>
        /// <param name="filePath">Path where to save the JSON file</param>
        public async Task SaveEntryMetricsToJsonAsync(List<EntryMetrics> records, string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(records, Formatting.Indented);
                await Task.Run(() => File.WriteAllText(filePath, json));
                Console.WriteLine($"Entry Metrics data saved to JSON file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving Entry Metrics data to JSON: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves Entry Metrics data to CSV file
        /// </summary>
        /// <param name="records">List of records to save</param>
        /// <param name="filePath">Path where to save the CSV file</param>
        public async Task SaveEntryMetricsToCsvAsync(List<EntryMetrics> records, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Write CSV header
                    await writer.WriteLineAsync("AccountId,AccountName,Customer,CampaignName,EntryId,EntryType,MetricType,Timestamp,MetaData");

                    // Write data rows
                    foreach (var record in records)
                    {
                        var line = $"{record.AccountId},\"{EscapeCsvField(record.AccountName)}\",\"{EscapeCsvField(record.Customer)}\",\"{EscapeCsvField(record.CampaignName)}\",{record.EntryId},\"{EscapeCsvField(record.EntryType)}\",\"{EscapeCsvField(record.MetricType)}\",\"{EscapeCsvField(record.Timestamp)}\",\"{EscapeCsvField(record.MetaData)}\"";
                        await writer.WriteLineAsync(line);
                    }
                }

                Console.WriteLine($"Entry Metrics data saved to CSV file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving Entry Metrics data to CSV: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tests the DigitalStudio database connection
        /// </summary>
        /// <returns>True if connection is successful</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("DigitalStudio database connection test successful");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DigitalStudio database connection test failed: {ex.Message}");
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