using System;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;

namespace DataStudioDataMgr.Services.Brick
{
    public class BrickService
    {
        private readonly BrickApiService _apiService;
        private readonly BrickMongoService _mongoService;

        public BrickService()
        {
            _apiService = new BrickApiService();
            _mongoService = new BrickMongoService();
        }

        public async Task<bool> TestConnectionAsync()
        {
            return await _apiService.TestConnectionAsync();
        }

        /// <summary>
        /// Processes Brick campaign data by fetching campaign list from API and storing in MongoDB.
        /// Then processes daily statistics for each campaign using dates from the campaign data.
        /// 
        /// Current approach:
        /// 1. Fetches campaign data from Brick Campaign endpoint
        /// 2. Stores campaigns in MongoDB
        /// 3. For each campaign, fetches daily statistics using campaignId, startDate, and endDate from the campaign data
        /// 4. Adjusts endDate by adding BrickDailyStatisticsEndDateAddDays configuration value
        /// 5. Stores daily statistics in MongoDB only if adjustedEndDate >= currentDateTime
        /// </summary>
        public async Task ProcessBrickCampaignDataAsync()
        {
            try
            {
                Console.WriteLine("=== PROCESSING BRICK CAMPAIGN DATA ===");
                
                // Get advertiser ID from configuration (default: 1)
                int advertiserId = int.Parse(ConfigurationManager.AppSettings["BrickDefaultCampaignId"] ?? "1");
                Console.WriteLine($"Using advertiser ID: {advertiserId}");

                // Step 1: Fetch campaign data from Brick API
                Console.WriteLine("Step 1: Fetching campaign data from Brick API...");
                string jsonResponse = await _apiService.GetCampaignListByAdvertiserIdAsync(advertiserId);
                
                if (string.IsNullOrEmpty(jsonResponse))
                {
                    Console.WriteLine("No data received from Brick API");
                    return;
                }

                Console.WriteLine($"Received {jsonResponse.Length} characters of JSON data from Brick API");

                // Step 2: Store campaign data in MongoDB
                Console.WriteLine("Step 2: Storing campaign data in MongoDB...");
                int recordsStored = await _mongoService.StoreCampaignListFromJsonAsync(jsonResponse, advertiserId);
                
                Console.WriteLine($"Successfully processed and stored {recordsStored} campaign records");

                // Step 3: Query MongoDB for campaigns and process daily statistics for each
                Console.WriteLine("Step 3: Processing daily statistics for each campaign from MongoDB...");
                await ProcessDailyStatisticsForAllCampaignsFromMongoAsync();
                
                Console.WriteLine($"=== BRICK CAMPAIGN DATA PROCESSING COMPLETE ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Brick campaign data: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Processes Brick daily statistics by fetching campaign daily statistics from API and storing in MongoDB
        /// DEPRECATED: This method uses configuration values for dates. 
        /// The current use case requires dates to come from the Brick Campaign endpoint.
        /// Use ProcessBrickCampaignDataAsync instead, which automatically processes daily statistics for all campaigns.
        /// </summary>
        [Obsolete("This method uses configuration values for dates. Use ProcessBrickCampaignDataAsync instead, which gets dates from the Brick Campaign endpoint.")]
        public async Task ProcessBrickDailyStatisticsAsync()
        {
            try
            {
                Console.WriteLine("=== PROCESSING BRICK DAILY STATISTICS ===");
                
                // Get configuration values
                int campaignId = int.Parse(ConfigurationManager.AppSettings["BrickDailyStatisticsCampaignId"] ?? "1");
                
                // Try to get dates from BrickDailyStatisticsStartDate/EndDate first, fall back to BrickStartDate/EndDate
                string startDateStr = ConfigurationManager.AppSettings["BrickDailyStatisticsStartDate"] ?? "";
                string endDateStr = ConfigurationManager.AppSettings["BrickDailyStatisticsEndDate"] ?? "";
                
                // If daily statistics dates are empty, try the general Brick date settings
                if (string.IsNullOrEmpty(startDateStr))
                {
                    startDateStr = ConfigurationManager.AppSettings["BrickStartDate"] ?? "";
                }
                if (string.IsNullOrEmpty(endDateStr))
                {
                    endDateStr = ConfigurationManager.AppSettings["BrickEndDate"] ?? "";
                }
                
                // Validate dates
                if (string.IsNullOrEmpty(startDateStr) || string.IsNullOrEmpty(endDateStr))
                {
                    Console.WriteLine("ERROR: Date range must be set in App.config");
                    Console.WriteLine("Set either BrickDailyStatisticsStartDate/BrickDailyStatisticsEndDate or BrickStartDate/BrickEndDate");
                    Console.WriteLine("Date format should be YYYY-MM-DD (e.g., 2025-09-01)");
                    return;
                }
                
                // Validate date format and that endDate is after startDate
                if (!DateTime.TryParse(startDateStr, out DateTime startDate))
                {
                    Console.WriteLine($"ERROR: Invalid start date format: {startDateStr}");
                    Console.WriteLine("Date format should be YYYY-MM-DD (e.g., 2025-09-01)");
                    return;
                }
                
                if (!DateTime.TryParse(endDateStr, out DateTime endDate))
                {
                    Console.WriteLine($"ERROR: Invalid end date format: {endDateStr}");
                    Console.WriteLine("Date format should be YYYY-MM-DD (e.g., 2025-09-01)");
                    return;
                }
                
                if (endDate < startDate)
                {
                    Console.WriteLine($"ERROR: End date ({endDateStr}) must occur after start date ({startDateStr})");
                    return;
                }
                
                Console.WriteLine($"Using campaign ID: {campaignId}");
                Console.WriteLine($"Date range: {startDateStr} to {endDateStr}");

                // Step 1: Fetch daily statistics from Brick API
                Console.WriteLine("Step 1: Fetching campaign daily statistics from Brick API...");
                string jsonResponse = await _apiService.GetCampaignDailyStatisticsAsync(campaignId, startDateStr, endDateStr);
                
                if (string.IsNullOrEmpty(jsonResponse))
                {
                    Console.WriteLine("No data received from Brick API");
                    return;
                }

                Console.WriteLine($"Received {jsonResponse.Length} characters of JSON data from Brick API");

                // Get campaign name (we'll need to fetch it from MongoDB or API)
                // For now, we'll use a placeholder or fetch from campaign collection
                string campaignName = await GetCampaignNameAsync(campaignId);

                // Get the number of days to add to endDate from configuration (default: 7)
                int endDateAddDays = int.Parse(ConfigurationManager.AppSettings["BrickDailyStatisticsEndDateAddDays"] ?? "7");
                DateTime adjustedEndDate = endDate.AddDays(endDateAddDays);
                DateTime currentDateTime = DateTime.UtcNow;

                Console.WriteLine($"Original end date: {endDateStr}, Adjusted end date (with {endDateAddDays} days): {adjustedEndDate:yyyy-MM-dd}");
                Console.WriteLine($"Current date time: {currentDateTime:yyyy-MM-dd HH:mm:ss} UTC");

                // Only store daily statistics if adjustedEndDate is greater than or equal to currentDateTime
                if (adjustedEndDate >= currentDateTime)
                {
                    // Step 2: Store daily statistics in MongoDB
                    Console.WriteLine("Step 2: Storing daily statistics in MongoDB...");
                    int recordsStored = await _mongoService.StoreCampaignDailyStatisticsFromJsonAsync(jsonResponse, campaignId, campaignName, startDate, adjustedEndDate);
                    
                    Console.WriteLine($"=== BRICK DAILY STATISTICS PROCESSING COMPLETE ===");
                    Console.WriteLine($"Successfully processed and stored {recordsStored} daily statistics records");
                }
                else
                {
                    Console.WriteLine($"Skipping storage: Adjusted end date ({adjustedEndDate:yyyy-MM-dd}) is before current date time ({currentDateTime:yyyy-MM-dd HH:mm:ss} UTC)");
                    Console.WriteLine($"=== BRICK DAILY STATISTICS PROCESSING COMPLETE ===");
                    Console.WriteLine("No records stored - adjusted end date is in the past");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing Brick daily statistics: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Processes daily statistics for all campaigns from MongoDB.
        /// Gets campaignId, startDate, and endDate from each campaign document in MongoDB,
        /// then calls ProcessBrickDailyStatisticsForCampaignAsync for each campaign.
        /// This is the primary approach - dates come from the Brick Campaign endpoint response.
        /// </summary>
        private async Task ProcessDailyStatisticsForAllCampaignsFromMongoAsync()
        {
            try
            {
                // Get all campaigns from MongoDB
                var campaigns = await _mongoService.GetAllCampaignsAsync();
                
                if (campaigns == null || campaigns.Count == 0)
                {
                    Console.WriteLine("No campaigns found in MongoDB to process daily statistics");
                    return;
                }

                Console.WriteLine($"Found {campaigns.Count} campaigns in MongoDB. Processing daily statistics for each...");

                int processedCount = 0;
                int skippedCount = 0;

                foreach (var campaignDoc in campaigns)
                {
                    try
                    {
                        // Extract campaign ID (check multiple field name variations)
                        int? campaignId = GetIntValueFromBson(campaignDoc, "id", "Id", "campaignId", "CampaignId");
                        
                        if (!campaignId.HasValue)
                        {
                            Console.WriteLine($"Skipping campaign: No valid campaign ID found");
                            skippedCount++;
                            continue;
                        }

                        // Extract campaign name (check multiple field name variations)
                        string campaignName = GetStringValueFromBson(campaignDoc, "name", "Name", "campaignName", "CampaignName") ?? string.Empty;

                        // Extract start date (check multiple field name variations)
                        DateTime? startDate = GetDateTimeValueFromBson(campaignDoc, "startDate", "StartDate", "start_date", "Start_Date");
                        
                        // Extract end date (check multiple field name variations)
                        DateTime? endDate = GetDateTimeValueFromBson(campaignDoc, "endDate", "EndDate", "end_date", "End_Date");

                        // Validate dates
                        if (!startDate.HasValue || !endDate.HasValue)
                        {
                            Console.WriteLine($"Skipping campaign ID {campaignId}: Missing start date or end date");
                            Console.WriteLine($"  StartDate found: {startDate.HasValue}, EndDate found: {endDate.HasValue}");
                            Console.WriteLine($"  Available fields: {string.Join(", ", campaignDoc.Names)}");
                            skippedCount++;
                            continue;
                        }

                        if (endDate.Value < startDate.Value)
                        {
                            Console.WriteLine($"Skipping campaign ID {campaignId}: End date is before start date");
                            skippedCount++;
                            continue;
                        }

                        // Adjust endDate by adding BrickDailyStatisticsEndDateAddDays configuration value
                        int endDateAddDays = int.Parse(ConfigurationManager.AppSettings["BrickDailyStatisticsEndDateAddDays"] ?? "7");
                        DateTime adjustedEndDate = endDate.Value.AddDays(endDateAddDays);

                        Console.WriteLine($"Processing daily statistics for campaign ID {campaignId} ({campaignName})");
                        Console.WriteLine($"  Date range: {startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}");
                        Console.WriteLine($"  Adjusted end date (with {endDateAddDays} days): {adjustedEndDate:yyyy-MM-dd}");

                        // Check if adjustedEndDate is greater than or equal to current date
                        DateTime currentDateTime = DateTime.UtcNow;
                        if (adjustedEndDate >= currentDateTime)
                        {
                            // Process daily statistics for this campaign using values from MongoDB
                            await ProcessBrickDailyStatisticsForCampaignAsync(
                                campaignId.Value,
                                startDate.Value,
                                adjustedEndDate,
                                campaignName
                            );

                            processedCount++;
                        }
                        else
                        {
                            Console.WriteLine($"  Skipping campaign ID {campaignId}: Adjusted end date ({adjustedEndDate:yyyy-MM-dd}) is before current date time ({currentDateTime:yyyy-MM-dd HH:mm:ss} UTC)");
                            skippedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing campaign: {ex.Message}");
                        skippedCount++;
                    }
                }

                Console.WriteLine($"Daily statistics processing complete: {processedCount} processed, {skippedCount} skipped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing daily statistics for all campaigns: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Processes daily statistics for all campaigns in the JSON response (legacy method - no longer used)
        /// DEPRECATED: This method is no longer used. The current approach uses ProcessDailyStatisticsForAllCampaignsFromMongoAsync
        /// which gets campaigns from MongoDB instead of parsing JSON directly.
        /// </summary>
        /// <param name="jsonResponse">JSON response string from Brick API containing campaign list</param>
        [Obsolete("This method is no longer used. Use ProcessDailyStatisticsForAllCampaignsFromMongoAsync instead.")]
        private async Task ProcessDailyStatisticsForAllCampaignsAsync(string jsonResponse)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonResponse))
                {
                    Console.WriteLine("No JSON data to process");
                    return;
                }

                // Parse JSON - handle different possible response structures
                JToken jsonData = JToken.Parse(jsonResponse);
                
                // Try to find the campaigns array - could be at root, in "data", "campaigns", etc.
                JArray campaignsArray = null;
                
                if (jsonData is JArray)
                {
                    campaignsArray = (JArray)jsonData;
                }
                else if (jsonData["data"] != null)
                {
                    if (jsonData["data"] is JArray)
                    {
                        campaignsArray = (JArray)jsonData["data"];
                    }
                    else if (jsonData["data"]["campaigns"] != null)
                    {
                        campaignsArray = (JArray)jsonData["data"]["campaigns"];
                    }
                }
                else if (jsonData["campaigns"] != null)
                {
                    campaignsArray = (JArray)jsonData["campaigns"];
                }
                else if (jsonData["Data"] != null)
                {
                    if (jsonData["Data"] is JArray)
                    {
                        campaignsArray = (JArray)jsonData["Data"];
                    }
                    else if (jsonData["Data"]["Records"] != null)
                    {
                        campaignsArray = (JArray)jsonData["Data"]["Records"];
                    }
                }

                if (campaignsArray == null || campaignsArray.Count == 0)
                {
                    Console.WriteLine("No campaigns found in JSON response to process daily statistics");
                    return;
                }

                Console.WriteLine($"Found {campaignsArray.Count} campaigns. Processing daily statistics for each...");

                int processedCount = 0;
                int skippedCount = 0;

                foreach (var campaignToken in campaignsArray)
                {
                    if (campaignToken is JObject campaignObj)
                    {
                        try
                        {
                            // Extract campaign ID (check multiple field name variations)
                            int? campaignId = GetIntValue(campaignObj, "id", "Id", "campaignId", "CampaignId");
                            
                            if (!campaignId.HasValue)
                            {
                                Console.WriteLine($"Skipping campaign: No valid campaign ID found");
                                skippedCount++;
                                continue;
                            }

                            // Extract campaign name (check multiple field name variations)
                            string campaignName = GetStringValue(campaignObj, "name", "Name", "campaignName", "CampaignName") ?? string.Empty;

                            // Extract start date (check multiple field name variations)
                            DateTime? startDate = GetDateTimeValue(campaignObj, "startDate", "StartDate", "start_date", "Start_Date", "start", "Start", "dateStart", "DateStart");
                            
                            // Extract end date (check multiple field name variations)
                            DateTime? endDate = GetDateTimeValue(campaignObj, "endDate", "EndDate", "end_date", "End_Date", "end", "End", "dateEnd", "DateEnd");

                            // Log what we found for debugging
                            if (!startDate.HasValue)
                            {
                                Console.WriteLine($"  Start date not found. Checking available fields...");
                                Console.WriteLine($"  Available fields: {string.Join(", ", campaignObj.Properties().Select(p => $"{p.Name} ({p.Value.Type})"))}");
                                
                                // Try to find any date-like fields
                                foreach (var prop in campaignObj.Properties())
                                {
                                    if (prop.Name.ToLower().Contains("start") || prop.Name.ToLower().Contains("date"))
                                    {
                                        Console.WriteLine($"  Found potential start date field: {prop.Name} = {prop.Value}");
                                    }
                                }
                            }
                            
                            if (!endDate.HasValue)
                            {
                                Console.WriteLine($"  End date not found. Checking available fields...");
                                
                                // Try to find any date-like fields
                                foreach (var prop in campaignObj.Properties())
                                {
                                    if (prop.Name.ToLower().Contains("end") || prop.Name.ToLower().Contains("date"))
                                    {
                                        Console.WriteLine($"  Found potential end date field: {prop.Name} = {prop.Value}");
                                    }
                                }
                            }

                            // Validate dates
                            if (!startDate.HasValue || !endDate.HasValue)
                            {
                                Console.WriteLine($"Skipping campaign ID {campaignId}: Missing start date or end date");
                                Console.WriteLine($"  StartDate found: {startDate.HasValue}, EndDate found: {endDate.HasValue}");
                                Console.WriteLine($"  Available fields in campaign object: {string.Join(", ", campaignObj.Properties().Select(p => p.Name))}");
                                skippedCount++;
                                continue;
                            }

                            if (endDate.Value < startDate.Value)
                            {
                                Console.WriteLine($"Skipping campaign ID {campaignId}: End date is before start date");
                                skippedCount++;
                                continue;
                            }

                            // Adjust endDate by adding BrickDailyStatisticsEndDateAddDays configuration value
                            int endDateAddDays = int.Parse(ConfigurationManager.AppSettings["BrickDailyStatisticsEndDateAddDays"] ?? "7");
                            DateTime adjustedEndDate = endDate.Value.AddDays(endDateAddDays);

                            Console.WriteLine($"Processing daily statistics for campaign ID {campaignId} ({campaignName})");
                            Console.WriteLine($"  Date range: {startDate.Value:yyyy-MM-dd} to {endDate.Value:yyyy-MM-dd}");
                            Console.WriteLine($"  Adjusted end date (with {endDateAddDays} days): {adjustedEndDate:yyyy-MM-dd}");

                            // Process daily statistics for this campaign using values from BrickCampaignData response
                            // This is the primary approach - campaignId, startDate, and endDate come from the campaign data
                            await ProcessBrickDailyStatisticsForCampaignAsync(
                                campaignId.Value,
                                startDate.Value,
                                adjustedEndDate,
                                campaignName
                            );

                            processedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing campaign: {ex.Message}");
                            skippedCount++;
                        }
                    }
                }

                Console.WriteLine($"Daily statistics processing complete: {processedCount} processed, {skippedCount} skipped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing daily statistics for all campaigns: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Processes Brick daily statistics for a single campaign.
        /// This is the primary method used for processing daily statistics.
        /// 
        /// Process:
        /// 1. Uses campaignId, startDate, and endDate from the Brick Campaign endpoint response
        /// 2. Calls Brick Daily Statistics API with adjustedEndDate
        /// 3. Receives adjustedEndDate (already adjusted by BrickDailyStatisticsEndDateAddDays)
        /// 4. Stores in MongoDB (date validation is performed by the caller)
        /// </summary>
        /// <param name="campaignId">The campaign ID from Brick Campaign endpoint response</param>
        /// <param name="startDate">The start date from Brick Campaign endpoint response</param>
        /// <param name="adjustedEndDate">The end date from Brick Campaign endpoint response, already adjusted by BrickDailyStatisticsEndDateAddDays</param>
        /// <param name="campaignName">The campaign name from Brick Campaign endpoint response</param>
        private async Task ProcessBrickDailyStatisticsForCampaignAsync(int campaignId, DateTime startDate, DateTime adjustedEndDate, string campaignName)
        {
            try
            {               
                string startDateStr = startDate.ToString("yyyy-MM-dd");
                string originalEndDateStr = adjustedEndDate.ToString("yyyy-MM-dd");

                // Log the values being passed to the API
                Console.WriteLine($"  Calling GetCampaignDailyStatisticsAsync with:");
                Console.WriteLine($"    Campaign ID: {campaignId}");
                Console.WriteLine($"    Start Date: {startDateStr} (from campaign data)");
                Console.WriteLine($"    End Date: {adjustedEndDate} (adjusted end date from campaign data)");

                // Fetch daily statistics from Brick API using adjustedEndDate
                // This is the primary approach - campaignId, startDate, and endDate come from the campaign data
                string jsonResponse = await _apiService.GetCampaignDailyStatisticsAsync(campaignId, startDateStr, originalEndDateStr);
                
                if (string.IsNullOrEmpty(jsonResponse))
                {
                    Console.WriteLine($"  No data received from Brick API for campaign ID {campaignId}");
                    return;
                }

                // Store daily statistics in MongoDB with campaign name and adjusted end date
                int recordsStored = await _mongoService.StoreCampaignDailyStatisticsFromJsonAsync(
                    jsonResponse,
                    campaignId,
                    campaignName,
                    startDate,
                    adjustedEndDate
                );

                Console.WriteLine($"  Successfully stored {recordsStored} daily statistics records for campaign ID {campaignId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error processing daily statistics for campaign ID {campaignId}: {ex.Message}");
                // Don't throw - continue processing other campaigns
            }
        }

        /// <summary>
        /// Helper method to extract integer value from BsonDocument checking multiple field names
        /// </summary>
        private int? GetIntValueFromBson(BsonDocument doc, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (doc.Contains(fieldName))
                {
                    var value = doc[fieldName];
                    if (value.BsonType == BsonType.Int32)
                    {
                        return value.AsInt32;
                    }
                    else if (value.BsonType == BsonType.Int64)
                    {
                        return (int)value.AsInt64;
                    }
                    else if (value.BsonType == BsonType.Double)
                    {
                        return (int)value.AsDouble;
                    }
                    else if (value.BsonType == BsonType.String)
                    {
                        if (int.TryParse(value.AsString, out int result))
                        {
                            return result;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Helper method to extract string value from BsonDocument checking multiple field names
        /// </summary>
        private string GetStringValueFromBson(BsonDocument doc, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (doc.Contains(fieldName))
                {
                    var value = doc[fieldName];
                    if (value.BsonType == BsonType.String)
                    {
                        return value.AsString;
                    }
                    else
                    {
                        return value.ToString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Helper method to extract DateTime value from BsonDocument checking multiple field names
        /// </summary>
        private DateTime? GetDateTimeValueFromBson(BsonDocument doc, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                if (doc.Contains(fieldName))
                {
                    var value = doc[fieldName];
                    try
                    {
                        if (value.BsonType == BsonType.DateTime)
                        {
                            return value.ToUniversalTime();
                        }
                        else if (value.BsonType == BsonType.String)
                        {
                            string dateString = value.AsString;
                            if (DateTime.TryParse(dateString, out DateTime result))
                            {
                                return result;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Warning: Could not parse date from field '{fieldName}': {ex.Message}");
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Helper method to extract integer value from JObject checking multiple field names
        /// </summary>
        private int? GetIntValue(JObject obj, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                var token = obj[fieldName];
                if (token != null && token.Type != JTokenType.Null)
                {
                    if (token.Type == JTokenType.Integer)
                    {
                        return token.ToObject<int>();
                    }
                    else if (token.Type == JTokenType.String)
                    {
                        if (int.TryParse(token.ToString(), out int result))
                        {
                            return result;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Helper method to extract string value from JObject checking multiple field names
        /// </summary>
        private string GetStringValue(JObject obj, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                var token = obj[fieldName];
                if (token != null && token.Type != JTokenType.Null)
                {
                    return token.ToString();
                }
            }
            return null;
        }

        /// <summary>
        /// Helper method to extract DateTime value from JObject checking multiple field names
        /// Handles dates as strings, DateTime objects, Unix timestamps, and nested date objects
        /// </summary>
        private DateTime? GetDateTimeValue(JObject obj, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                var token = obj[fieldName];
                if (token != null && token.Type != JTokenType.Null)
                {
                    try
                    {
                        if (token.Type == JTokenType.Date)
                        {
                            var dateValue = token.ToObject<DateTime>();
                            Console.WriteLine($"  Found date field '{fieldName}' as DateTime: {dateValue:yyyy-MM-dd}");
                            return dateValue;
                        }
                        else if (token.Type == JTokenType.String)
                        {
                            string dateString = token.ToString().Trim();
                            if (string.IsNullOrEmpty(dateString))
                                continue;
                                
                            // Try parsing with common date formats
                            if (DateTime.TryParse(dateString, out DateTime result))
                            {
                                Console.WriteLine($"  Found date field '{fieldName}' as string (parsed): {result:yyyy-MM-dd}");
                                return result;
                            }
                            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result2))
                            {
                                Console.WriteLine($"  Found date field '{fieldName}' as string (yyyy-MM-dd): {result2:yyyy-MM-dd}");
                                return result2;
                            }
                            if (DateTime.TryParseExact(dateString, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result3))
                            {
                                Console.WriteLine($"  Found date field '{fieldName}' as string (MM/dd/yyyy): {result3:yyyy-MM-dd}");
                                return result3;
                            }
                            if (DateTime.TryParseExact(dateString, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime result4))
                            {
                                Console.WriteLine($"  Found date field '{fieldName}' as string (ISO): {result4:yyyy-MM-dd}");
                                return result4;
                            }
                            
                            Console.WriteLine($"  Warning: Could not parse date string from field '{fieldName}': '{dateString}'");
                        }
                        else if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                        {
                            // Handle Unix timestamp (seconds or milliseconds)
                            long timestamp = token.ToObject<long>();
                            DateTime dateValue;
                            if (timestamp > 1000000000000) // milliseconds
                            {
                                dateValue = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                            }
                            else // seconds
                            {
                                dateValue = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                            }
                            Console.WriteLine($"  Found date field '{fieldName}' as Unix timestamp: {dateValue:yyyy-MM-dd}");
                            return dateValue;
                        }
                        else if (token.Type == JTokenType.Object)
                        {
                            // Handle nested date objects (e.g., {year: 2025, month: 1, day: 15})
                            var dateObj = token as JObject;
                            if (dateObj != null)
                            {
                                int? year = GetIntValue(dateObj, "year", "Year", "YEAR");
                                int? month = GetIntValue(dateObj, "month", "Month", "MONTH");
                                int? day = GetIntValue(dateObj, "day", "Day", "DAY");
                                
                                if (year.HasValue && month.HasValue && day.HasValue)
                                {
                                    try
                                    {
                                        var dateValue = new DateTime(year.Value, month.Value, day.Value);
                                        Console.WriteLine($"  Found date field '{fieldName}' as nested object: {dateValue:yyyy-MM-dd}");
                                        return dateValue;
                                    }
                                    catch (ArgumentOutOfRangeException)
                                    {
                                        Console.WriteLine($"  Warning: Invalid date values in nested object '{fieldName}': year={year}, month={month}, day={day}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error for debugging but continue to next field name
                        Console.WriteLine($"  Warning: Could not parse date from field '{fieldName}': {ex.Message}");
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the campaign name from MongoDB by campaign ID
        /// </summary>
        /// <param name="campaignId">The campaign ID</param>
        /// <returns>The campaign name, or empty string if not found</returns>
        private async Task<string> GetCampaignNameAsync(int campaignId)
        {
            try
            {
                var campaignName = await _mongoService.GetCampaignNameByIdAsync(campaignId);
                if (string.IsNullOrEmpty(campaignName))
                {
                    Console.WriteLine($"Warning: Campaign name not found for campaign ID {campaignId}, using empty string");
                    return string.Empty;
                }
                return campaignName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving campaign name for ID {campaignId}: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Backs up both collections, deletes all data, and reprocesses both collections
        /// </summary>
        /// <param name="outputDirectory">Directory to save the backup files</param>
        public async Task BackupDeleteAndReprocessAsync(string outputDirectory)
        {
            try
            {
                Console.WriteLine("=== BRICK COLLECTIONS: BACKUP, DELETE, AND REPROCESS ===");
                
                // Step 1: Backup and delete collections
                Console.WriteLine("Step 1: Backing up and deleting collections...");
                var (campaignBackupPath, dailyStatisticsBackupPath) = await _mongoService.BackupAndDeleteCollectionsAsync(outputDirectory);
                
                if (!string.IsNullOrEmpty(campaignBackupPath))
                {
                    Console.WriteLine($"Campaign backup saved to: {campaignBackupPath}");
                }
                if (!string.IsNullOrEmpty(dailyStatisticsBackupPath))
                {
                    Console.WriteLine($"Daily statistics backup saved to: {dailyStatisticsBackupPath}");
                }
                
                // Step 2: Process campaign data and daily statistics
                // ProcessBrickCampaignDataAsync now automatically processes daily statistics 
                // for all campaigns using values (campaignId, startDate, endDate) from the BrickCampaignData response
                Console.WriteLine("Step 2: Processing campaign data and daily statistics...");
                await ProcessBrickCampaignDataAsync();
                
                Console.WriteLine("=== BACKUP, DELETE, AND REPROCESS COMPLETE ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during backup, delete, and reprocess operation: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public void Dispose()
        {
            _apiService?.Dispose();
        }
    }
}