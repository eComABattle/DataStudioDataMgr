using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.SqlServer
{
    /// <summary>
    /// Service for reading UNFI Customer data from the AdStudioUnfi SQL Server database
    /// </summary>
    public class UnfiCustomerService
    {
        private readonly string _connectionString;

        public UnfiCustomerService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["AdStudioUnfi"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("AdStudioUnfi connection string not found in configuration");
            }
        }

        public UnfiCustomerService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Retrieves customer records from UNFI.dbo.Customer
        /// </summary>
        public async Task<List<UnfiCustomer>> GetCustomersAsync()
        {
            var records = new List<UnfiCustomer>();
            const string query = @"
SELECT 'UNFI' AS ClientToken
    ,[CustomerID]
    ,[ActiveThroughDate]
    ,isnull([AdminLevel],0) as Adminlevel
    ,[CreatedBy]
    ,convert(varchar,[CreatedDate], 101) as CreatedDate
    ,[CustomerName]
    ,[CustomerNumber]
    ,[CustomerServiceRepID]
    ,isnull([Email], '') as Email
    ,isnull([Fax], '') as Fax
    ,[IsActive]
    ,[IsInternal]
    ,isnull(convert(varchar,[LastLoginDate], 101), '01/01/1900') as LastLoginDate
    ,isnull([ModifiedBy],0) as ModifiedBy
    ,isnull(convert(varchar, [ModifiedDate], 101), '01/01/1900') as ModifiedDate
    ,isnull([Phone], '') as Phone
    ,isnull([PreferredLanguageToken], '') as PreferredLanguageToken
    ,[SalesRepID]
    ,isnull([WebURL], '') as WebURL
    ,convert(varchar(256),[CustomerGUID]) as CustomerGUID
    ,[CanPreMerchandise]
    ,isnull([Notes], '') as Notes
FROM [UNFI].[dbo].[Customer] c WITH (NOLOCK)
WHERE c.CustomerID > 0";

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Connected to AdStudioUnfi database for customer data");

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var record = new UnfiCustomer
                                {
                                    ClientToken = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                                    CustomerId = reader.GetInt32(1),
                                    ActiveThroughDate = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                                    AdminLevel = reader.GetInt32(3),
                                    CreatedBy = reader.IsDBNull(4) ? string.Empty : reader.GetValue(4).ToString(),
                                    CreatedDate = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                    CustomerName = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                    CustomerNumber = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                                    CustomerServiceRepId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                                    Email = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                    Fax = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                    IsActive = !reader.IsDBNull(11) && reader.GetBoolean(11),
                                    IsInternal = !reader.IsDBNull(12) && reader.GetBoolean(12),
                                    LastLoginDate = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                                    ModifiedBy = reader.IsDBNull(14) ? 0 : reader.GetInt32(14),
                                    ModifiedDate = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                                    Phone = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                                    PreferredLanguageToken = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                                    SalesRepId = reader.IsDBNull(18) ? (int?)null : reader.GetInt32(18),
                                    WebUrl = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
                                    CustomerGuid = reader.IsDBNull(20) ? string.Empty : reader.GetString(20),
                                    CanPreMerchandise = !reader.IsDBNull(21) && reader.GetBoolean(21),
                                    Notes = reader.IsDBNull(22) ? string.Empty : reader.GetString(22)
                                };

                                records.Add(record);
                            }
                        }
                    }
                }

                Console.WriteLine($"Retrieved {records.Count} UNFI customer records from database");
                return records;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error retrieving UNFI customer records: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving UNFI customer records: {ex.Message}");
                throw;
            }
        }

        public async Task SaveUnfiCustomersToJsonAsync(List<UnfiCustomer> records, string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(records, Formatting.Indented);
                await Task.Run(() => File.WriteAllText(filePath, json));
                Console.WriteLine($"UNFI customer data saved to JSON file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving UNFI customer data to JSON: {ex.Message}");
                throw;
            }
        }

        public async Task SaveUnfiCustomersToCsvAsync(List<UnfiCustomer> records, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    await writer.WriteLineAsync(
                        "ClientToken,CustomerId,ActiveThroughDate,AdminLevel,CreatedBy,CreatedDate,CustomerName,CustomerNumber,CustomerServiceRepId,Email,Fax,IsActive,IsInternal,LastLoginDate,ModifiedBy,ModifiedDate,Phone,PreferredLanguageToken,SalesRepId,WebUrl,CustomerGuid,CanPreMerchandise,Notes");

                    foreach (var r in records)
                    {
                        var line =
                            $"\"{EscapeCsvField(r.ClientToken)}\",{r.CustomerId},\"{r.ActiveThroughDate:O}\",{r.AdminLevel},\"{EscapeCsvField(r.CreatedBy)}\",\"{EscapeCsvField(r.CreatedDate)}\",\"{EscapeCsvField(r.CustomerName)}\",\"{EscapeCsvField(r.CustomerNumber)}\",{r.CustomerServiceRepId?.ToString() ?? ""},\"{EscapeCsvField(r.Email)}\",\"{EscapeCsvField(r.Fax)}\",{r.IsActive.ToString().ToLowerInvariant()},{r.IsInternal.ToString().ToLowerInvariant()},\"{EscapeCsvField(r.LastLoginDate)}\",{r.ModifiedBy},\"{EscapeCsvField(r.ModifiedDate)}\",\"{EscapeCsvField(r.Phone)}\",\"{EscapeCsvField(r.PreferredLanguageToken)}\",{r.SalesRepId?.ToString() ?? ""},\"{EscapeCsvField(r.WebUrl)}\",\"{EscapeCsvField(r.CustomerGuid)}\",{r.CanPreMerchandise.ToString().ToLowerInvariant()},\"{EscapeCsvField(r.Notes)}\"";
                        await writer.WriteLineAsync(line);
                    }
                }

                Console.WriteLine($"UNFI customer data saved to CSV file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving UNFI customer data to CSV: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("AdStudioUnfi database connection test successful (customer service)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdStudioUnfi database connection test failed: {ex.Message}");
                return false;
            }
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;
            return field.Replace("\"", "\"\"");
        }
    }
}
