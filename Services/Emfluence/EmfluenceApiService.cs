using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using DataStudioDataMgr.Configuration;
using System.Linq;

namespace DataStudioDataMgr.Services.Emfluence
{
    public class EmfluenceApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _accessToken;
        private readonly string _storeId;
        private readonly string _storeName;

        public EmfluenceApiService(string accessToken)
        {
            _httpClient = new HttpClient();
            _baseUrl = "https://api.emailer.emfluence.com/v1/emails/search";
            _accessToken = accessToken;
            _storeId = "default";
            _storeName = "Default Store";
            
            // Set up the authorization header
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public EmfluenceApiService(string accessToken, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = "https://api.emailer.emfluence.com/v1/emails/search";
            _accessToken = accessToken;
            _storeId = "default";
            _storeName = "Default Store";
            
            // Set up the authorization header
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public EmfluenceApiService(StoreConfiguration storeConfig)
        {
            _httpClient = new HttpClient();
            _baseUrl = "https://api.emailer.emfluence.com/v1/emails/search";
            _accessToken = storeConfig.AccessToken;
            _storeId = storeConfig.StoreId;
            _storeName = storeConfig.StoreName;
            
            // Set up the authorization header
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public EmfluenceApiService(StoreConfiguration storeConfig, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = "https://api.emailer.emfluence.com/v1/emails/search";
            _accessToken = storeConfig.AccessToken;
            _storeId = storeConfig.StoreId;
            _storeName = storeConfig.StoreName;
            
            // Set up the authorization header
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Gets the store ID for this service instance
        /// </summary>
        public string StoreId => _storeId;

        /// <summary>
        /// Gets the store name for this service instance
        /// </summary>
        public string StoreName => _storeName;

        /// <summary>
        /// Fetches email records from the Emfluence API
        /// </summary>
        /// <param name="deliveryType">Type of delivery (e.g., "manual")</param>
        /// <param name="status">Email status (e.g., "sent")</param>
        /// <param name="dateSentStart">Start date for filtering emails (YYYY-MM-DD format)</param>
        /// <param name="dateSentEnd">End date for filtering emails (YYYY-MM-DD format)</param>
        /// <param name="automatedResults">Whether to include automated results ("summary" or "detail")</param>
        /// <param name="page">Page number for pagination</param>
        /// <param name="rpp">Records per page</param>
        /// <returns>API response containing email records</returns>
        public async Task<EmfluenceAPI.RootResponse> GetEmailRecordsAsync(
            string deliveryType = "manual",
            string status = "sent", 
            string dateSentStart = null,
            string dateSentEnd = null,
            string automatedResults = "summary",
            int page = 1,
            int rpp = 50)
        {
            try
            {
                // Build query parameters
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(deliveryType))
                    queryParams.Add($"deliveryType={Uri.EscapeDataString(deliveryType)}");
                
                if (!string.IsNullOrEmpty(status))
                    queryParams.Add($"status={Uri.EscapeDataString(status)}");

                if (string.IsNullOrEmpty(dateSentStart))
                    dateSentStart = DateTime.Now.AddMonths(-17).ToString("yyyy-MM-dd");

                if (!string.IsNullOrEmpty(dateSentStart))
                    queryParams.Add($"dateSentStart={Uri.EscapeDataString(dateSentStart)}");
                
                //if (!string.IsNullOrEmpty(dateSentEnd))
                //    queryParams.Add($"dateSentEnd={Uri.EscapeDataString(dateSentEnd)}");
                
                if (!string.IsNullOrEmpty(automatedResults))
                    queryParams.Add($"automatedResults={Uri.EscapeDataString(automatedResults)}");
                
                queryParams.Add($"page={page}");
                queryParams.Add($"rpp={rpp}");

                var queryString = string.Join("&", queryParams);
                var fullUrl = $"{_baseUrl}?{queryString}";

                Console.WriteLine($"Making API request to: {fullUrl} (Store: {_storeName})");

                // Make the API call
                var response = await _httpClient.GetAsync(fullUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"API request failed with status {response.StatusCode}: {errorContent}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response received: {jsonContent.Length} characters");

                // Deserialize the response
                var result = JsonConvert.DeserializeObject<EmfluenceAPI.RootResponse>(jsonContent);
                
                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize API response");
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error: {ex.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON Parsing Error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Fetches email records with detailed automated results
        /// </summary>
        /// <param name="deliveryType">Type of delivery</param>
        /// <param name="status">Email status</param>
        /// <param name="dateSentStart">Start date for filtering</param>
        /// <param name="dateSentEnd">End date for filtering</param>
        /// <param name="page">Page number for pagination</param>
        /// <param name="rpp">Records per page</param>
        /// <returns>API response containing detailed email records</returns>
        public async Task<EmfluenceAPI.RootResponse> GetDetailedEmailRecordsAsync(
            string deliveryType = "manual",
            string status = "sent",
            string dateSentStart = null,
            string dateSentEnd = null,
            int page = 1,
            int rpp = 50)
        {
            return await GetEmailRecordsAsync(deliveryType, status, dateSentStart, dateSentEnd, "detail", page, rpp);
        }

        /// <summary>
        /// Fetches all email records by handling pagination automatically
        /// </summary>
        /// <param name="deliveryType">Type of delivery</param>
        /// <param name="status">Email status</param>
        /// <param name="dateSentStart">Start date for filtering</param>
        /// <param name="automatedResults">Automated results setting</param>
        /// <param name="maxRecords">Maximum number of records to fetch (0 = no limit)</param>
        /// <returns>Combined list of all email records</returns>
        public async Task<List<EmfluenceAPI.Record>> GetAllEmailRecordsAsync(
            string deliveryType = "manual",
            string status = "sent",
            string dateSentStart = null,
            string automatedResults = "summary",
            int maxRecords = 0)
        {
            var allRecords = new List<EmfluenceAPI.Record>();
            int currentPage = 1;
            int recordsPerPage = 250;
            bool hasMorePages = true;

            Console.WriteLine($"Starting to fetch all email records with pagination for store: {_storeName}...");

            while (hasMorePages)
            {
                try
                {
                    Console.WriteLine($"Fetching page {currentPage} for store: {_storeName}...");
                    
                    var response = await GetEmailRecordsAsync(
                        deliveryType, 
                        status, 
                        dateSentStart, 
                        null, // dateSentEnd - not used in GetAllEmailRecordsAsync
                        automatedResults, 
                        currentPage, 
                        recordsPerPage);

                    if (response?.Data?.Records != null && response.Data.Records.Count > 0)
                    {
                        allRecords.AddRange(response.Data.Records);
                        Console.WriteLine($"Added {response.Data.Records.Count} records from page {currentPage}");
                        
                        // Check if we've reached the maximum records limit
                        if (maxRecords > 0 && allRecords.Count >= maxRecords)
                        {
                            Console.WriteLine($"Reached maximum records limit of {maxRecords}");
                            break;
                        }
                        
                        // Check if there are more pages
                        if (response.Data.Paging != null)
                        {
                            hasMorePages = currentPage < response.Data.Paging.TotalPages;
                            Console.WriteLine($"Page {currentPage} of {response.Data.Paging.TotalPages} (Total records: {response.Data.Paging.TotalRecords})");
                        }
                        else
                        {
                            hasMorePages = false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No records found on page {currentPage}");
                        hasMorePages = false;
                    }

                    currentPage++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching page {currentPage}: {ex.Message}");
                    break;
                }
            }

            Console.WriteLine($"Completed fetching records for store: {_storeName}. Total records collected: {allRecords.Count}");
            return allRecords;
        }

        /// <summary>
        /// String fields are quoted and escaped so commas and emojis don�t break CSV
        /// </summary>


        /// <summary>
        /// String fields are quoted and escaped so commas and emojis don�t break CSV
        /// </summary>
        static string Quote(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return "\"\"";

            string value = token.ToString().Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        /// <summary>
        /// Test method that fetches email records for every day over a 90-day period
        /// </summary>
        /// <param name="deliveryType">Type of delivery</param>
        /// <param name="status">Email status</param>
        /// <param name="startDate">Start date for the 90-day period (YYYY-MM-DD format)</param>
        /// <param name="outputFilePath">Path where the CSV file should be saved</param>
        /// <returns>Total number of records processed</returns>
        //public async Task<int> TestDailyEmailRecordsAsync(
        //    string deliveryType = "manual",
        //    string status = "sent",
        //    string startDate = null,
        //    string outputFilePath = "test_daily_emails.csv")
        //{
        //    try
        //    {
        //        // Use current date minus 90 days if no start date provided
        //        DateTime startDateTime;
        //        if (string.IsNullOrEmpty(startDate))
        //        {
        //            startDateTime = DateTime.Now.AddDays(-90);
        //        }
        //        else
        //        {
        //            startDateTime = DateTime.Parse(startDate);
        //        }

        //        Console.WriteLine($"Starting daily email test from {startDateTime:yyyy-MM-dd} for 90 days...");

        //        var allRecords = new List<EmfluenceAPI.Record>();
        //        int totalDaysProcessed = 0;
        //        int totalRecordsFound = 0;

        //        // Process each day for 90 days
        //        for (int i = 0; i < 90; i++)
        //        {
        //            DateTime currentDate = startDateTime.AddDays(i);
        //            string dateStr = currentDate.ToString("yyyy-MM-dd");

        //            Console.WriteLine($"Processing day {i + 1}/90: {dateStr}");

        //            try
        //            {
        //                // Fetch records for this specific day
        //                var response = await GetEmailRecordsAsync(
        //                    deliveryType: deliveryType,
        //                    status: status,
        //                    dateSentStart: dateStr,
        //                    dateSentEnd: dateStr,
        //                    automatedResults: "detail",
        //                    page: 1,
        //                    rpp: 250 // Get more records per page for efficiency
        //                );

        //                if (response?.Data?.Records != null && response.Data.Records.Count > 0)
        //                {
        //                    allRecords.AddRange(response.Data.Records);
        //                    totalRecordsFound += response.Data.Records.Count;
        //                    Console.WriteLine($"  Found {response.Data.Records.Count} records for {dateStr}");
        //                }
        //                else
        //                {
        //                    Console.WriteLine($"  No records found for {dateStr}");
        //                }

        //                totalDaysProcessed++;

        //                // Add a small delay to avoid overwhelming the API
        //                await Task.Delay(100);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"  Error processing {dateStr}: {ex.Message}");
        //                // Continue with next day
        //            }
        //        }

        //        Console.WriteLine($"\nDaily test completed:");
        //        Console.WriteLine($"  Days processed: {totalDaysProcessed}/90");
        //        Console.WriteLine($"  Total records found: {totalRecordsFound}");

        //        // Create CSV file with all records
        //        if (allRecords.Count > 0)
        //        {
        //            using (var writer = new StreamWriter(outputFilePath))
        //            {
        //                // Write CSV header with dateSentStart and dateSentEnd columns
        //                writer.WriteLine("emailId,templateId,subject,fromAddress,fromName,replyToAddress,replyToName,deliveryType,status,title,userId,dateAdded,dateModified,dateSent,dateSentStart,dateSentEnd,parentEmailId,abSplitId,campaignIds,confirmed,clicks,uniqueViews,bounces,forwards,recipients,uniqueForwards,clickToViewRate,sendingIssues,webViews,complaints,shares,uniqueClicks,unsubscribes,uniqueWebViews,views,uniqueShares");

        //                // Write records
        //                foreach (var record in allRecords)
        //                {
        //                    var campaignIDs = record.CampaignIDs != null ? string.Join(";", record.CampaignIDs) : "";

        //                    writer.WriteLine(string.Join(",",
        //                        record.EmailID,
        //                        Quote(record.TemplateID.ToString()),
        //                        Quote(record.Subject),
        //                        Quote(record.FromAddress),
        //                        Quote(record.FromName),
        //                        Quote(record.ReplyToAddress),
        //                        Quote(record.ReplyToName),
        //                        Quote(record.DeliveryType),
        //                        Quote(record.Status),
        //                        Quote(record.Title),
        //                        record.UserID,
        //                        Quote(record.DateAdded.ToString("yyyy-MM-ddTHH:mm:ss")),
        //                        Quote(record.DateModified.ToString("yyyy-MM-ddTHH:mm:ss")),
        //                        Quote(record.DateSent.ToString("yyyy-MM-ddTHH:mm:ss")),
        //                        Quote(record.DateSent.ToString("yyyy-MM-dd")), // dateSentStart
        //                        Quote(record.DateSent.ToString("yyyy-MM-dd")), // dateSentEnd
        //                        Quote(record.ParentEmailID),
        //                        Quote(record.AbSplitID),
        //                        Quote(campaignIDs),
        //                        record.Schedule?.Confirmed ?? 0,
        //                        record.Metrics?.Clicks ?? 0,
        //                        record.Metrics?.UniqueViews ?? 0,
        //                        record.Metrics?.Bounces ?? 0,
        //                        record.Metrics?.Forwards ?? 0,
        //                        record.Metrics?.Recipients ?? 0,
        //                        record.Metrics?.UniqueForwards ?? 0,
        //                        record.Metrics?.ClickToViewRate ?? 0,
        //                        record.Metrics?.SendingIssues ?? 0,
        //                        record.Metrics?.WebViews ?? 0,
        //                        record.Metrics?.Complaints ?? 0,
        //                        record.Metrics?.Shares ?? 0,
        //                        record.Metrics?.UniqueClicks ?? 0,
        //                        record.Metrics?.Unsubscribes ?? 0,
        //                        record.Metrics?.UniqueWebViews ?? 0,
        //                        record.Metrics?.Views ?? 0,
        //                        record.Metrics?.UniqueShares ?? 0
        //                    ));
        //                }
        //            }

        //            Console.WriteLine($"CSV file saved to: {outputFilePath}");
        //        }
        //        else
        //        {
        //            Console.WriteLine("No records found to save to CSV.");
        //        }

        //        return allRecords.Count;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error in daily email test: {ex.Message}");
        //        throw;
        //    }
        //}

        public async Task<int> TestDailyEmailRecordsAsync(
    string deliveryType = "manual",
    string status = "sent",
    string startDate = null,
    string outputFilePath = "test_daily_emails.csv")
        {
            try
            {
                // Use current date minus 90 days if no start date provided
                DateTime startDateTime;
                if (string.IsNullOrEmpty(startDate))
                {
                    startDateTime = DateTime.Now.AddDays(-90);
                }
                else
                {
                    startDateTime = DateTime.Parse(startDate);
                }

                Console.WriteLine($"Starting daily email test from {startDateTime:yyyy-MM-dd} for 90 days...");

                var allRecords = new List<EmfluenceAPI.Record>();
                int totalDaysProcessed = 0;
                int totalRecordsFound = 0;

                // Process each day for 90 days
                for (int i = 0; i < 90; i++)
                {
                    DateTime currentDate = startDateTime.AddDays(i);
                    string dateStr = currentDate.ToString("yyyy-MM-dd");

                    Console.WriteLine($"Processing day {i + 1}/90: {dateStr}");

                    try
                    {
                        // Fetch records for this specific day
                        var response = await GetEmailRecordsAsync(
                            deliveryType: deliveryType,
                            status: status,
                            dateSentStart: dateStr,
                            dateSentEnd: null, // Explicitly exclude dateSentEnd from query
                            automatedResults: "detail",
                            page: 1,
                            rpp: 250
                        );

                        if (response?.Data?.Records != null && response.Data.Records.Count > 0)
                        {
                            allRecords.AddRange(response.Data.Records);
                            totalRecordsFound += response.Data.Records.Count;
                            Console.WriteLine($"  Found {response.Data.Records.Count} records for {dateStr}");
                        }
                        else
                        {
                            Console.WriteLine($"  No records found for {dateStr}");
                        }

                        totalDaysProcessed++;

                        // Add a small delay to avoid overwhelming the API
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ERROR processing {dateStr}: {ex.Message}");
                        Console.WriteLine($"  Exception type: {ex.GetType().Name}");
                        // Continue with next day
                    }
                }

                Console.WriteLine($"\nDaily test completed:");
                Console.WriteLine($"  Days processed: {totalDaysProcessed}/90");
                Console.WriteLine($"  Total records found: {totalRecordsFound}");

                // Create CSV file with all records
                if (allRecords.Count > 0)
                {
                    Console.WriteLine($"Creating CSV file with {allRecords.Count} records...");
                    Console.WriteLine($"Output file path: {outputFilePath}");

                    try
                    {
                        Console.WriteLine($"About to create file: {outputFilePath}");
                        Console.WriteLine($"Directory exists: {Directory.Exists(Path.GetDirectoryName(outputFilePath))}");
                        
                        // Check if file already exists
                        if (File.Exists(outputFilePath))
                        {
                            Console.WriteLine($"File already exists, deleting it first...");
                            File.Delete(outputFilePath);
                        }

                        using (var writer = new StreamWriter(outputFilePath))
                        {
                            Console.WriteLine($"StreamWriter created successfully");
                            
                            // Write CSV header (same pattern as ConvertResponseToCsv)
                            writer.WriteLine("templateID,replyToAddress,userID,dateModified,fromAddress,deliveryType,subject,dateAdded,campaignIDs,clicks,uniqueViews,bounces,forwards,recipients,uniqueForwards,clickToViewRate,sendingIssues,webViews,complaints,shares,uniqueClicks,unsubscribes,uniqueWebViews,views,uniqueShares,parentEmailID,status,confirmed,abSplitID,fromName,emailID,replyToName,title,dateSent,dateSentStart");
                            Console.WriteLine($"CSV header written");

                            // Write records
                            int recordCount = 0;
                            foreach (var record in allRecords)
                            {
                                var campaignIDs = record.CampaignIDs != null ? string.Join(";", record.CampaignIDs) : "";

                                writer.WriteLine(string.Join(",",
                                    record.TemplateID,
                                    Quote(record.ReplyToAddress),
                                    record.UserID,
                                    Quote(record.DateModified.ToString("yyyy-MM-ddTHH:mm:ss")),
                                    Quote(record.FromAddress),
                                    Quote(record.DeliveryType),
                                    Quote(record.Subject),
                                    Quote(record.DateAdded.ToString("yyyy-MM-ddTHH:mm:ss")),
                                    Quote(campaignIDs),
                                    record.Metrics?.Clicks ?? 0,
                                    record.Metrics?.UniqueViews ?? 0,
                                    record.Metrics?.Bounces ?? 0,
                                    record.Metrics?.Forwards ?? 0,
                                    record.Metrics?.Recipients ?? 0,
                                    record.Metrics?.UniqueForwards ?? 0,
                                    record.Metrics?.ClickToViewRate ?? 0,
                                    record.Metrics?.SendingIssues ?? 0,
                                    record.Metrics?.WebViews ?? 0,
                                    record.Metrics?.Complaints ?? 0,
                                    record.Metrics?.Shares ?? 0,
                                    record.Metrics?.UniqueClicks ?? 0,
                                    record.Metrics?.Unsubscribes ?? 0,
                                    record.Metrics?.UniqueWebViews ?? 0,
                                    record.Metrics?.Views ?? 0,
                                    record.Metrics?.UniqueShares ?? 0,
                                    Quote(record.ParentEmailID),
                                    Quote(record.Status),
                                    //Quote(record.DateScheduled),
                                    record.Schedule?.Confirmed ?? 0,
                                    Quote(record.AbSplitID),
                                    Quote(record.FromName),
                                    record.EmailID,
                                    Quote(record.ReplyToName),
                                    Quote(record.Title),
                                    Quote(record.DateSent.ToString("yyyy-MM-ddTHH:mm:ss")),
                                    Quote(record.DateSent.ToString("yyyy-MM-dd"))  // dateSentStart
                                ));
                                recordCount++;
                                
                                if (recordCount % 100 == 0)
                                {
                                    Console.WriteLine($"Written {recordCount} records so far...");
                                }
                            }
                            
                            Console.WriteLine($"Finished writing {recordCount} records");
                        }

                        Console.WriteLine($"StreamWriter disposed, file should be closed");
                        Console.WriteLine($"CSV file saved to: {outputFilePath}");
                        
                        // Wait a moment for file system to catch up
                        System.Threading.Thread.Sleep(1000);
                        
                        // Verify file was created
                        if (File.Exists(outputFilePath))
                        {
                            var fileInfo = new FileInfo(outputFilePath);
                            Console.WriteLine($"File verification: SUCCESS - File exists at {outputFilePath}");
                            Console.WriteLine($"File size: {fileInfo.Length} bytes");
                            Console.WriteLine($"File created: {fileInfo.CreationTime}");
                        }
                        else
                        {
                            Console.WriteLine($"File verification: FAILED - File does not exist at {outputFilePath}");
                        }
                    }
                    catch (Exception csvEx)
                    {
                        Console.WriteLine($"ERROR creating CSV file: {csvEx.Message}");
                        Console.WriteLine($"Exception type: {csvEx.GetType().Name}");
                        Console.WriteLine($"Stack trace: {csvEx.StackTrace}");
                        throw;
                    }
                }
                else
                {
                    Console.WriteLine("No records found to save to CSV.");
                }

                return allRecords.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR in daily email test: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Disposes of the HTTP client
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        /// <summary>
        /// Converts a JSON response to CSV format and saves to file
        /// </summary>
        /// <param name="jsonResponse">The JSON response from the API</param>
        /// <param name="outputFilePath">Path where the CSV file should be saved</param>
        public void ConvertJsonToCsv(string jsonResponse, string outputFilePath)
        {
            try
            {
                // Parse JSON
                JObject obj = JObject.Parse(jsonResponse);
                JArray records = (JArray)obj["data"]["records"];

                using (var writer = new StreamWriter(outputFilePath))
                {
                    // Write CSV header
                    writer.WriteLine("templateID,replyToAddress,userID,dateModified,fromAddress,deliveryType,subject,dateAdded,campaignIDs,clicks,uniqueViews,bounces,forwards,recipients,uniqueForwards,clickToViewRate,sendingIssues,webViews,complaints,shares,uniqueClicks,unsubscribes,uniqueWebViews,views,uniqueShares,parentEmailID,status,dateScheduled,confirmed,abSplitID,fromName,emailID,replyToName,title,dateSent");

                    // Loop through records and flatten
                    foreach (var record in records)
                    {
                        var metrics = record["metrics"];
                        var schedule = record["schedule"];
                        var campaignIDs = string.Join(";", record["campaignIDs"]);

                        writer.WriteLine(string.Join(",",
                            record["templateID"],
                            Quote(record["replyToAddress"]),
                            record["userID"],
                            Quote(record["dateModified"]),
                            Quote(record["fromAddress"]),
                            Quote(record["deliveryType"]),
                            Quote(record["subject"]),
                            Quote(record["dateAdded"]),
                            Quote(campaignIDs),
                            metrics["clicks"],
                            metrics["uniqueViews"],
                            metrics["bounces"],
                            metrics["forwards"],
                            metrics["recipients"],
                            metrics["uniqueForwards"],
                            metrics["clickToViewRate"],
                            metrics["sendingIssues"],
                            metrics["webViews"],
                            metrics["complaints"],
                            metrics["shares"],
                            metrics["uniqueClicks"],
                            metrics["unsubscribes"],
                            metrics["uniqueWebViews"],
                            metrics["views"],
                            metrics["uniqueShares"],
                            Quote(record["parentEmailID"]),
                            Quote(record["status"]),
                            Quote(schedule["dateScheduled"]),
                            schedule["confirmed"],
                            Quote(record["abSplitID"]),
                            Quote(record["fromName"]),
                            record["emailID"],
                            Quote(record["replyToName"]),
                            Quote(record["title"]),
                            Quote(record["dateSent"])
                        ));
                    }
                }

                Console.WriteLine($"CSV file saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting JSON to CSV: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Converts a JSON response object to CSV format and saves to file
        /// </summary>
        /// <param name="response">The RootResponse object from the API</param>
        /// <param name="outputFilePath">Path where the CSV file should be saved</param>
        public void ConvertResponseToCsv(EmfluenceAPI.RootResponse response, string outputFilePath)
        {
            try
            {
                if (response?.Data?.Records == null)
                {
                    Console.WriteLine("No records found in response to convert to CSV.");
                    return;
                }

                using (var writer = new StreamWriter(outputFilePath))
                {
                    // Write CSV header
                    writer.WriteLine("templateID,replyToAddress,userID,dateModified,fromAddress,deliveryType,subject,dateAdded,campaignIDs,clicks,uniqueViews,bounces,forwards,recipients,uniqueForwards,clickToViewRate,sendingIssues,webViews,complaints,shares,uniqueClicks,unsubscribes,uniqueWebViews,views,uniqueShares,parentEmailID,status,dateScheduled,confirmed,abSplitID,fromName,emailID,replyToName,title,dateSent");

                    // Loop through records and flatten
                    foreach (var record in response.Data.Records)
                    {
                        var campaignIDs = record.CampaignIDs != null ? string.Join(";", record.CampaignIDs) : "";

                        writer.WriteLine(string.Join(",",
                            record.TemplateID,
                            Quote(record.ReplyToAddress),
                            record.UserID,
                            Quote(record.DateModified.ToString("yyyy-MM-ddTHH:mm:ss")),
                            Quote(record.FromAddress),
                            Quote(record.DeliveryType),
                            Quote(record.Subject),
                            Quote(record.DateAdded.ToString("yyyy-MM-ddTHH:mm:ss")),
                            Quote(campaignIDs),
                            record.Metrics?.Clicks ?? 0,
                            record.Metrics?.UniqueViews ?? 0,
                            record.Metrics?.Bounces ?? 0,
                            record.Metrics?.Forwards ?? 0,
                            record.Metrics?.Recipients ?? 0,
                            record.Metrics?.UniqueForwards ?? 0,
                            record.Metrics?.ClickToViewRate ?? 0,
                            record.Metrics?.SendingIssues ?? 0,
                            record.Metrics?.WebViews ?? 0,
                            record.Metrics?.Complaints ?? 0,
                            record.Metrics?.Shares ?? 0,
                            record.Metrics?.UniqueClicks ?? 0,
                            record.Metrics?.Unsubscribes ?? 0,
                            record.Metrics?.UniqueWebViews ?? 0,
                            record.Metrics?.Views ?? 0,
                            record.Metrics?.UniqueShares ?? 0,
                            Quote(record.ParentEmailID),
                            Quote(record.Status),
                            Quote(record.Schedule?.DateScheduled),
                            record.Schedule?.Confirmed ?? 0,
                            Quote(record.AbSplitID),
                            Quote(record.FromName),
                            record.EmailID,
                            Quote(record.ReplyToName),
                            Quote(record.Title),
                            Quote(record.DateSent.ToString("yyyy-MM-ddTHH:mm:ss"))
                        ));
                    }
                }

                Console.WriteLine($"CSV file saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting response to CSV: {ex.Message}");
                throw;
            }
        }


    }
}
