using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Configuration;
using DataStudioDataMgr.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataStudioDataMgr.Services.Brick
{
    // Note: Using BsonDocument directly instead of a class to avoid serialization issues
    // All properties from JSON will be stored as stand-alone fields
    // startDate and endDate will be converted to DateTime

    /// <summary>
    /// Service for storing Brick data in MongoDB
    /// </summary>
    public class BrickMongoService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _campaignCollection;
        private readonly IMongoCollection<BsonDocument> _dailyStatisticsCollection;

        public BrickMongoService()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDbConnectionString"]?.ConnectionString;
            
            // Expand environment variables in connection string
            if (!string.IsNullOrEmpty(connectionString))
            {
                connectionString = Environment.ExpandEnvironmentVariables(connectionString);
            }
            
            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback to default connection string
                connectionString = "mongodb://localhost:27017";
                Console.WriteLine("Using fallback MongoDB connection string: mongodb://localhost:27017");
            }
            else
            {
                Console.WriteLine($"Using MongoDB connection string from config (with environment variables expanded)");
            }

            // Get MongoDB database name from App.config
            var databaseName = ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration";
            //var databaseName = "integration";

            // Expand environment variables in database name
            databaseName = Environment.ExpandEnvironmentVariables(databaseName);
            
            Console.WriteLine($"Using MongoDB database for Brick: {databaseName}");

            Console.WriteLine($"Connecting to MongoDB...");
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
            
            _campaignCollection = _database.GetCollection<BsonDocument>("brick_campaign");
            _dailyStatisticsCollection = _database.GetCollection<BsonDocument>("brick_campaign_daily_statistics");
        }

        public BrickMongoService(string connectionString, string databaseName = null)
        {
            // Expand environment variables in connection string
            connectionString = Environment.ExpandEnvironmentVariables(connectionString);
            
            var client = new MongoClient(connectionString);
            
            // Use provided databaseName or fall back to default
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration";
            }
            
            // Expand environment variables in database name
            databaseName = Environment.ExpandEnvironmentVariables(databaseName);
            
            _database = client.GetDatabase(databaseName);
            
            _campaignCollection = _database.GetCollection<BsonDocument>("brick_campaign");
            _dailyStatisticsCollection = _database.GetCollection<BsonDocument>("brick_campaign_daily_statistics");
        }

        /// <summary>
        /// Stores Brick campaign daily statistics from JSON response in MongoDB
        /// </summary>
        /// <param name="jsonResponse">JSON response string from Brick API daily statistics endpoint</param>
        /// <param name="campaignId">Campaign ID to store with the statistics</param>
        /// <param name="campaignName">Campaign name to store with the statistics</param>
        /// <param name="startDate">Start date for the statistics period</param>
        /// <param name="endDate">End date for the statistics period (7 days will be added to ensure ample time for data)</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreCampaignDailyStatisticsFromJsonAsync(string jsonResponse, int campaignId, string campaignName, DateTime startDate, DateTime endDate)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonResponse))
                {
                    Console.WriteLine("No JSON data to store");
                    return 0;
                }

                Console.WriteLine($"Parsing Brick campaign daily statistics JSON response...");
                
                // Parse JSON
                JToken jsonData = JToken.Parse(jsonResponse);
                
                if (jsonData is JObject rootObj)
                {
                    
                    // Get days.data array
                    var daysObj = rootObj["days"] ?? rootObj["Days"];
                    JArray daysDataArray = null;
                    
                    if (daysObj != null)
                    {
                        if (daysObj["data"] != null && daysObj["data"] is JArray)
                        {
                            daysDataArray = (JArray)daysObj["data"];
                        }
                        else if (daysObj["Data"] != null && daysObj["Data"] is JArray)
                        {
                            daysDataArray = (JArray)daysObj["Data"];
                        }
                        else if (daysObj is JArray)
                        {
                            daysDataArray = (JArray)daysObj;
                        }
                    }
                    
                    if (daysDataArray == null || daysDataArray.Count == 0)
                    {
                        Console.WriteLine("No daily statistics data found in JSON response");
                        return 0;
                    }
                    
                    Console.WriteLine($"Found {daysDataArray.Count} daily statistics records");
                    
                    // Delete existing daily statistics for this campaignId before inserting new ones
                    string campaignIdString = campaignId.ToString();
                    Console.WriteLine($"Deleting existing daily statistics for campaign ID {campaignId} (as string: '{campaignIdString}')...");
                    
                    // Build filter to match campaign ID with multiple field name and type variations
                    var deleteFilter = Builders<BsonDocument>.Filter.Or(
                        // Match "id" field as String (primary - MongoDB stores as string)
                        Builders<BsonDocument>.Filter.Eq("id", new BsonString(campaignIdString)),
                        // Match "id" field as Int32
                        Builders<BsonDocument>.Filter.Eq("id", new BsonInt32(campaignId)),
                        // Match "id" field as Int64
                        Builders<BsonDocument>.Filter.Eq("id", new BsonInt64(campaignId)),
                        // Match "Id" field as String
                        Builders<BsonDocument>.Filter.Eq("Id", new BsonString(campaignIdString)),
                        // Match "Id" field as Int32
                        Builders<BsonDocument>.Filter.Eq("Id", new BsonInt32(campaignId)),
                        // Match "Id" field as Int64
                        Builders<BsonDocument>.Filter.Eq("Id", new BsonInt64(campaignId)),
                        // Match "campaignId" field as String
                        Builders<BsonDocument>.Filter.Eq("campaignId", new BsonString(campaignIdString)),
                        // Match "campaignId" field as Int32
                        Builders<BsonDocument>.Filter.Eq("campaignId", new BsonInt32(campaignId)),
                        // Match "campaignId" field as Int64
                        Builders<BsonDocument>.Filter.Eq("campaignId", new BsonInt64(campaignId)),
                        // Match "CampaignId" field as String
                        Builders<BsonDocument>.Filter.Eq("CampaignId", new BsonString(campaignIdString)),
                        // Match "CampaignId" field as Int32
                        Builders<BsonDocument>.Filter.Eq("CampaignId", new BsonInt32(campaignId)),
                        // Match "CampaignId" field as Int64
                        Builders<BsonDocument>.Filter.Eq("CampaignId", new BsonInt64(campaignId))
                    );
                    
                    var deleteResult = await _dailyStatisticsCollection.DeleteManyAsync(deleteFilter);
                    Console.WriteLine($"Deleted {deleteResult.DeletedCount} existing daily statistics document(s) for campaign ID {campaignId}");
                    
                    var documents = new List<BsonDocument>();
                    
                    foreach (var dayDataToken in daysDataArray)
                    {
                        if (dayDataToken is JObject dayDataObj)
                        {
                            var document = new BsonDocument();
                            
                            // Add campaign ID
                            document["id"] = campaignId;
                            
                            // Add campaign name
                            if (!string.IsNullOrEmpty(campaignName))
                            {
                                document["campaignName"] = campaignName;
                            }
                            
                            // Add start_date and end_date (endDate is already adjusted in BrickService)
                            document["start_date"] = startDate;
                            document["end_date"] = endDate;
                            
                            // Extract day data properties: requests, impressions, clicks, revenue, day
                            if (dayDataObj["requests"] != null)
                            {
                                document["requests"] = ConvertJTokenToBsonValue(dayDataObj["requests"]);
                            }
                            else if (dayDataObj["Requests"] != null)
                            {
                                document["requests"] = ConvertJTokenToBsonValue(dayDataObj["Requests"]);
                            }
                            
                            if (dayDataObj["impressions"] != null)
                            {
                                document["impressions"] = ConvertJTokenToBsonValue(dayDataObj["impressions"]);
                            }
                            else if (dayDataObj["Impressions"] != null)
                            {
                                document["impressions"] = ConvertJTokenToBsonValue(dayDataObj["Impressions"]);
                            }
                            
                            if (dayDataObj["clicks"] != null)
                            {
                                document["clicks"] = ConvertJTokenToBsonValue(dayDataObj["clicks"]);
                            }
                            else if (dayDataObj["Clicks"] != null)
                            {
                                document["clicks"] = ConvertJTokenToBsonValue(dayDataObj["Clicks"]);
                            }
                            
                            if (dayDataObj["revenue"] != null)
                            {
                                document["revenue"] = ConvertJTokenToBsonValue(dayDataObj["revenue"]);
                            }
                            else if (dayDataObj["Revenue"] != null)
                            {
                                document["revenue"] = ConvertJTokenToBsonValue(dayDataObj["Revenue"]);
                            }
                            
                            // Convert day field to DateTime
                            var dayToken = dayDataObj["day"] ?? dayDataObj["Day"];
                            if (dayToken != null)
                            {
                                var dayDateTime = ParseDateTimeFromToken(dayToken);
                                if (dayDateTime.HasValue)
                                {
                                    document["day"] = dayDateTime.Value;
                                }
                                else
                                {
                                    document["day"] = ConvertJTokenToBsonValue(dayToken);
                                }
                            }
                            
                            // Add metadata
                            document["importedAt"] = DateTime.UtcNow;
                            document["source"] = "BrickAPI";
                            
                            documents.Add(document);
                        }
                    }
                    
                    if (documents.Count > 0)
                    {
                        await _dailyStatisticsCollection.InsertManyAsync(documents);
                        Console.WriteLine($"Successfully stored {documents.Count} Brick campaign daily statistics records in MongoDB");
                    }
                    
                    return documents.Count;
                }
                else
                {
                    Console.WriteLine("JSON response is not an object");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing Brick campaign daily statistics: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Helper method to parse DateTime from JToken
        /// </summary>
        private DateTime? ParseDateTimeFromToken(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            try
            {
                if (token.Type == JTokenType.Date)
                {
                    return token.ToObject<DateTime>();
                }
                
                string dateString = token.ToString();
                if (string.IsNullOrEmpty(dateString))
                    return null;

                if (DateTime.TryParse(dateString, out DateTime result))
                {
                    return result;
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        /// <summary>
        /// Stores Brick campaign data from JSON response in MongoDB
        /// </summary>
        /// <param name="jsonResponse">JSON response string from Brick API</param>
        /// <param name="advertiserId">The advertiser ID used to fetch the data</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreCampaignListFromJsonAsync(string jsonResponse, int advertiserId = 1)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonResponse))
                {
                    Console.WriteLine("No JSON data to store");
                    return 0;
                }

                Console.WriteLine($"Parsing Brick campaign list JSON response...");
                
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
                    Console.WriteLine("No campaigns found in JSON response");
                    // Store the raw JSON anyway for debugging
                    await StoreRawJsonAsync(jsonResponse, advertiserId);
                    return 0;
                }

                Console.WriteLine($"Found {campaignsArray.Count} campaigns in response");

                var documents = new List<BsonDocument>();
                var importedAtDateTime = DateTime.UtcNow;
                var campaignIdsToDelete = new List<int>();
                
                foreach (var campaignToken in campaignsArray)
                {
                    var campaignObj = (JObject)campaignToken;
                    
                    // Extract campaign ID (check multiple field name variations and types)
                    int? campaignId = null;
                    
                    // Try to extract ID using ParseInt helper which handles multiple types
                    campaignId = ParseInt(campaignObj["id"]) ?? 
                                 ParseInt(campaignObj["Id"]) ?? 
                                 ParseInt(campaignObj["campaignId"]) ?? 
                                 ParseInt(campaignObj["CampaignId"]);
                    
                    // If still not found, log available fields for debugging (only for first campaign)
                    if (!campaignId.HasValue && documents.Count == 0)
                    {
                        var availableFields = string.Join(", ", campaignObj.Properties().Select(p => $"{p.Name} ({p.Type})"));
                        Console.WriteLine($"Warning: Campaign ID not found in first campaign. Available fields: {availableFields}");
                    }
                    
                    // Convert JObject to BsonDocument - all properties will be stored as stand-alone
                    var document = new BsonDocument();
                    
                    // Copy all properties from the JSON object to the document
                    foreach (var property in campaignObj.Properties())
                    {
                        var propertyName = property.Name;
                        var propertyValue = property.Value;
                        
                        // Convert the JToken to BsonValue
                        BsonValue bsonValue = ConvertJTokenToBsonValue(propertyValue);
                        document[propertyName] = bsonValue;
                    }
                    
                    // If campaign ID wasn't found in JSON, try to extract it from the document after conversion
                    if (!campaignId.HasValue)
                    {
                        campaignId = GetIntValue(document, "id", "Id", "campaignId", "CampaignId");
                    }
                    
                    // If campaign ID found, add to list for deletion
                    if (campaignId.HasValue)
                    {
                        campaignIdsToDelete.Add(campaignId.Value);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Could not extract campaign ID from campaign. Skipping deletion for this campaign.");
                    }
                    
                    // Convert startDate and endDate to DateTime (check multiple naming variations)
                    ConvertDateFieldToDateTime(document, "startDate");
                    ConvertDateFieldToDateTime(document, "endDate");
                    ConvertDateFieldToDateTime(document, "StartDate");
                    ConvertDateFieldToDateTime(document, "EndDate");
                    ConvertDateFieldToDateTime(document, "start_date");
                    ConvertDateFieldToDateTime(document, "end_date");

                    // Add metadata
                    document["clientToken"] = "AWG";
                    document["importedAt"] = importedAtDateTime;
                    document["source"] = "BrickAPI";
                    
                    documents.Add(document);
                }

                // Delete existing campaigns by CampaignId before inserting new ones
                if (campaignIdsToDelete.Count > 0)
                {
                    Console.WriteLine($"Deleting {campaignIdsToDelete.Count} existing campaign(s) by CampaignId before inserting new records...");
                    Console.WriteLine($"Campaign IDs to delete: {string.Join(", ", campaignIdsToDelete)}");
                    
                    long totalDeleted = 0;
                    
                    // Delete each campaign individually to handle type variations (Int32, Int64, String, etc.)
                    foreach (var campaignId in campaignIdsToDelete)
                    {
                        string campaignIdString = campaignId.ToString();
                        
                        // Build filter to match campaign ID with multiple field name and type variations
                        var filter = Builders<BsonDocument>.Filter.Or(
                            // Match "id" field as String (primary - MongoDB stores as string)
                            Builders<BsonDocument>.Filter.Eq("id", new BsonString(campaignIdString)),
                            // Match "id" field as Int32
                            Builders<BsonDocument>.Filter.Eq("id", new BsonInt32(campaignId)),
                            // Match "id" field as Int64
                            Builders<BsonDocument>.Filter.Eq("id", new BsonInt64(campaignId)),
                            // Match "Id" field as String
                            Builders<BsonDocument>.Filter.Eq("Id", new BsonString(campaignIdString)),
                            // Match "Id" field as Int32
                            Builders<BsonDocument>.Filter.Eq("Id", new BsonInt32(campaignId)),
                            // Match "Id" field as Int64
                            Builders<BsonDocument>.Filter.Eq("Id", new BsonInt64(campaignId)),
                            // Match "campaignId" field as String
                            Builders<BsonDocument>.Filter.Eq("campaignId", new BsonString(campaignIdString)),
                            // Match "campaignId" field as Int32
                            Builders<BsonDocument>.Filter.Eq("campaignId", new BsonInt32(campaignId)),
                            // Match "campaignId" field as Int64
                            Builders<BsonDocument>.Filter.Eq("campaignId", new BsonInt64(campaignId)),
                            // Match "CampaignId" field as String
                            Builders<BsonDocument>.Filter.Eq("CampaignId", new BsonString(campaignIdString)),
                            // Match "CampaignId" field as Int32
                            Builders<BsonDocument>.Filter.Eq("CampaignId", new BsonInt32(campaignId)),
                            // Match "CampaignId" field as Int64
                            Builders<BsonDocument>.Filter.Eq("CampaignId", new BsonInt64(campaignId))
                        );
                        
                        var deleteResult = await _campaignCollection.DeleteManyAsync(filter);
                        if (deleteResult.DeletedCount > 0)
                        {
                            Console.WriteLine($"  Deleted {deleteResult.DeletedCount} document(s) for campaign ID {campaignId} (searched as string: '{campaignIdString}')");
                            totalDeleted += deleteResult.DeletedCount;
                        }
                    }
                    
                    Console.WriteLine($"Total deleted: {totalDeleted} existing campaign document(s)");
                }
                else
                {
                    Console.WriteLine($"Warning: No campaign IDs were extracted from the JSON response. Cannot delete existing documents.");
                    Console.WriteLine($"This may result in duplicate campaigns if they already exist in MongoDB.");
                    if (documents.Count > 0)
                    {
                        Console.WriteLine($"First campaign document fields: {string.Join(", ", documents[0].Names)}");
                    }
                }

                if (documents.Count > 0)
                {
                    await _campaignCollection.InsertManyAsync(documents);
                    Console.WriteLine($"Successfully stored {documents.Count} Brick campaign records in MongoDB");
                }

                return documents.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing Brick campaign data: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Stores raw JSON response in MongoDB for debugging/analysis
        /// </summary>
        private async Task StoreRawJsonAsync(string jsonResponse, int advertiserId)
        {
            try
            {
                var jsonData = JToken.Parse(jsonResponse);
                var document = new BsonDocument();
                
                // If it's an object, copy all properties
                if (jsonData is JObject jsonObj)
                {
                    foreach (var property in jsonObj.Properties())
                    {
                        var propertyName = property.Name;
                        var propertyValue = property.Value;
                        BsonValue bsonValue = ConvertJTokenToBsonValue(propertyValue);
                        document[propertyName] = bsonValue;
                    }
                }
                else
                {
                    // If it's not an object, store it as a raw value
                    document["rawResponse"] = ConvertJTokenToBsonValue(jsonData);
                }
                
                // Convert startDate and endDate to DateTime
                ConvertDateFieldToDateTime(document, "startDate");
                ConvertDateFieldToDateTime(document, "endDate");
                ConvertDateFieldToDateTime(document, "StartDate");
                ConvertDateFieldToDateTime(document, "EndDate");
                ConvertDateFieldToDateTime(document, "start_date");
                ConvertDateFieldToDateTime(document, "end_date");
                
                // Add metadata
                document["importedAt"] = DateTime.UtcNow;
                document["source"] = "BrickAPI_Raw";
                
                await _campaignCollection.InsertOneAsync(document);
                Console.WriteLine("Stored raw JSON response in MongoDB for analysis");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing raw JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to parse DateTime from various formats
        /// </summary>
        private DateTime? ParseDateTime(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            try
            {
                if (token.Type == JTokenType.Date)
                {
                    return token.ToObject<DateTime>();
                }
                
                string dateString = token.ToString();
                if (string.IsNullOrEmpty(dateString))
                    return null;

                if (DateTime.TryParse(dateString, out DateTime result))
                {
                    return result;
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        /// <summary>
        /// Helper method to parse integer from various formats
        /// </summary>
        private int? ParseInt(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            try
            {
                if (token.Type == JTokenType.Integer)
                {
                    return token.ToObject<int>();
                }
                
                if (token.Type == JTokenType.Float)
                {
                    return (int)token.ToObject<double>();
                }
                
                string intString = token.ToString();
                if (string.IsNullOrEmpty(intString))
                    return null;

                if (int.TryParse(intString, out int result))
                {
                    return result;
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return null;
        }

        /// <summary>
        /// Converts a JToken to BsonValue
        /// </summary>
        private BsonValue ConvertJTokenToBsonValue(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return BsonNull.Value;

            switch (token.Type)
            {
                case JTokenType.String:
                    return new BsonString(token.ToString());
                
                case JTokenType.Integer:
                    return new BsonInt64(token.ToObject<long>());
                
                case JTokenType.Float:
                    return new BsonDouble(token.ToObject<double>());
                
                case JTokenType.Boolean:
                    return new BsonBoolean(token.ToObject<bool>());
                
                case JTokenType.Date:
                    return new BsonDateTime(token.ToObject<DateTime>());
                
                case JTokenType.Array:
                    var array = new BsonArray();
                    foreach (var item in (JArray)token)
                    {
                        array.Add(ConvertJTokenToBsonValue(item));
                    }
                    return array;
                
                case JTokenType.Object:
                    var obj = new BsonDocument();
                    foreach (var property in ((JObject)token).Properties())
                    {
                        obj[property.Name] = ConvertJTokenToBsonValue(property.Value);
                    }
                    return obj;
                
                default:
                    // For other types, convert to string
                    return new BsonString(token.ToString());
            }
        }

        /// <summary>
        /// Converts a date field in a BsonDocument to DateTime type
        /// Handles date objects with month, day, year properties
        /// </summary>
        private void ConvertDateFieldToDateTime(BsonDocument document, string fieldName)
        {
            if (!document.Contains(fieldName))
                return;

            try
            {
                var fieldValue = document[fieldName];
                
                // If already a DateTime, no conversion needed
                if (fieldValue.BsonType == BsonType.DateTime)
                {
                    return;
                }

                // Handle date objects with month, day, year properties
                if (fieldValue.BsonType == BsonType.Document)
                {
                    var dateObj = fieldValue.AsBsonDocument;
                    
                    // Try to extract month, day, year (check multiple naming variations)
                    int? year = GetIntValue(dateObj, "year", "Year", "YEAR");
                    int? month = GetIntValue(dateObj, "month", "Month", "MONTH");
                    int? day = GetIntValue(dateObj, "day", "Day", "DAY");
                    
                    // Also check for hour, minute, second if they exist
                    int hour = GetIntValue(dateObj, "hour", "Hour", "HOUR") ?? 0;
                    int minute = GetIntValue(dateObj, "minute", "Minute", "MINUTE") ?? 0;
                    int second = GetIntValue(dateObj, "second", "Second", "SECOND") ?? 0;
                    
                    if (year.HasValue && month.HasValue && day.HasValue)
                    {
                        try
                        {
                            var dateTime = new DateTime(year.Value, month.Value, day.Value, hour, minute, second);
                            document[fieldName] = dateTime;
                            return;
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Invalid date values, try to continue with other conversion methods
                        }
                    }
                }

                // Try to parse as DateTime string
                if (fieldValue.BsonType == BsonType.String)
                {
                    string dateString = fieldValue.AsString;
                    if (!string.IsNullOrEmpty(dateString))
                    {
                        if (DateTime.TryParse(dateString, out DateTime dateTime))
                        {
                            document[fieldName] = dateTime;
                            return;
                        }
                    }
                }
                else if (fieldValue.BsonType == BsonType.Int64)
                {
                    // Handle Unix timestamp (seconds or milliseconds)
                    long timestamp = fieldValue.AsInt64;
                    // Check if it's in milliseconds (13 digits) or seconds (10 digits)
                    if (timestamp > 1000000000000) // milliseconds
                    {
                        document[fieldName] = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                        return;
                    }
                    else // seconds
                    {
                        document[fieldName] = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                        return;
                    }
                }
            }
            catch
            {
                // Ignore conversion errors - leave field as is
            }
        }

        /// <summary>
        /// Helper method to get an integer value from a BsonDocument, checking multiple property name variations
        /// </summary>
        private int? GetIntValue(BsonDocument document, params string[] propertyNames)
        {
            foreach (var propName in propertyNames)
            {
                if (document.Contains(propName))
                {
                    var value = document[propName];
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
        /// Stores Brick campaign objects in MongoDB
        /// </summary>
        /// <param name="campaigns">List of Brick campaigns to store</param>
        /// <param name="advertiserId">The advertiser ID</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreCampaignsAsync(List<BrickCampaign> campaigns, int advertiserId = 1)
        {
            try
            {
                if (campaigns == null || campaigns.Count == 0)
                {
                    Console.WriteLine("No campaign data to store");
                    return 0;
                }

                Console.WriteLine($"Storing {campaigns.Count} Brick campaign records in MongoDB...");

                var documents = new List<BsonDocument>();
                foreach (var campaign in campaigns)
                {
                    // Convert campaign to JSON, then parse to JObject to extract all properties
                    string json = JsonConvert.SerializeObject(campaign);
                    var campaignObj = JObject.Parse(json);
                    var document = new BsonDocument();
                    
                    // Copy all properties from the campaign object to the document
                    foreach (var property in campaignObj.Properties())
                    {
                        var propertyName = property.Name;
                        var propertyValue = property.Value;
                        BsonValue bsonValue = ConvertJTokenToBsonValue(propertyValue);
                        document[propertyName] = bsonValue;
                    }
                    
                    // Convert startDate and endDate to DateTime (check multiple naming variations)
                    ConvertDateFieldToDateTime(document, "startDate");
                    ConvertDateFieldToDateTime(document, "endDate");
                    ConvertDateFieldToDateTime(document, "StartDate");
                    ConvertDateFieldToDateTime(document, "EndDate");
                    ConvertDateFieldToDateTime(document, "start_date");
                    ConvertDateFieldToDateTime(document, "end_date");
                    
                    // Add metadata
                    document["importedAt"] = DateTime.UtcNow;
                    document["source"] = "BrickAPI";
                    
                    documents.Add(document);
                }

                await _campaignCollection.InsertManyAsync(documents);
                
                Console.WriteLine($"Successfully stored {campaigns.Count} Brick campaign records");
                return campaigns.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing Brick campaigns: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all campaigns from MongoDB
        /// </summary>
        /// <returns>List of campaign documents</returns>
        public async Task<List<BsonDocument>> GetAllCampaignsAsync()
        {
            try
            {
                var campaigns = await _campaignCollection.Find(new BsonDocument()).ToListAsync();
                Console.WriteLine($"Retrieved {campaigns.Count} campaigns from MongoDB");
                return campaigns;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving campaigns from MongoDB: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the campaign name from MongoDB by campaign ID
        /// </summary>
        /// <param name="campaignId">The campaign ID</param>
        /// <returns>The campaign name, or empty string if not found</returns>
        public async Task<string> GetCampaignNameByIdAsync(int campaignId)
        {
            try
            {
                // Try to find campaign by id (check multiple field name variations)
                var filter = Builders<BsonDocument>.Filter.Or(
                    Builders<BsonDocument>.Filter.Eq("id", campaignId),
                    Builders<BsonDocument>.Filter.Eq("Id", campaignId),
                    Builders<BsonDocument>.Filter.Eq("campaignId", campaignId),
                    Builders<BsonDocument>.Filter.Eq("CampaignId", campaignId)
                );
                
                var campaign = await _campaignCollection.Find(filter).FirstOrDefaultAsync();
                
                if (campaign == null)
                {
                    Console.WriteLine($"Campaign with ID {campaignId} not found in MongoDB");
                    return string.Empty;
                }
                
                // Try to get name from various field name variations
                string campaignName = campaign.GetValue("name", string.Empty)?.AsString ??
                                     campaign.GetValue("Name", string.Empty)?.AsString ??
                                     campaign.GetValue("campaignName", string.Empty)?.AsString ??
                                     campaign.GetValue("CampaignName", string.Empty)?.AsString ??
                                     string.Empty;
                
                if (!string.IsNullOrEmpty(campaignName))
                {
                    Console.WriteLine($"Found campaign name '{campaignName}' for campaign ID {campaignId}");
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
        /// Tests the MongoDB connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Console.WriteLine("Testing MongoDB connection for Brick service...");
                
                // Try to list collections to test connection
                var collections = await _database.ListCollectionNamesAsync();
                var collectionNames = await collections.ToListAsync();
                
                Console.WriteLine($"MongoDB connection successful! Found {collectionNames.Count} collections in database '{_database.DatabaseNamespace.DatabaseName}'");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MongoDB connection failed: {ex.Message}");
                Console.WriteLine($"Connection details:");
                Console.WriteLine($"  Database: {_database.DatabaseNamespace.DatabaseName}");
                Console.WriteLine($"  Error Type: {ex.GetType().Name}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"  Inner Exception: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Backs up campaign collection data to a JSON file
        /// </summary>
        /// <param name="outputDirectory">Directory to save the backup file</param>
        /// <returns>Path to the backup file</returns>
        public async Task<string> BackupCampaignCollectionAsync(string outputDirectory)
        {
            try
            {
                Console.WriteLine("Backing up brick_campaign collection...");
                
                var documents = await _campaignCollection.Find(new BsonDocument()).ToListAsync();
                var count = documents.Count;
                Console.WriteLine($"Found {count} documents in brick_campaign collection");
                
                if (count == 0)
                {
                    Console.WriteLine("No documents to backup in brick_campaign collection");
                    return null;
                }
                
                // Ensure output directory exists
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                
                // Create backup filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"brick_campaign_backup_{timestamp}.json";
                string backupFilePath = Path.Combine(outputDirectory, backupFileName);
                
                // Convert BsonDocuments to JSON
                var jsonArray = new JArray();
                foreach (var doc in documents)
                {
                    jsonArray.Add(JObject.Parse(doc.ToJson()));
                }
                
                // Write to file (using synchronous method wrapped in Task.Run for .NET Framework compatibility)
                await Task.Run(() => File.WriteAllText(backupFilePath, jsonArray.ToString(Formatting.Indented)));
                
                Console.WriteLine($"Successfully backed up {count} documents to: {backupFilePath}");
                return backupFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error backing up campaign collection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Backs up daily statistics collection data to a JSON file
        /// </summary>
        /// <param name="outputDirectory">Directory to save the backup file</param>
        /// <returns>Path to the backup file</returns>
        public async Task<string> BackupDailyStatisticsCollectionAsync(string outputDirectory)
        {
            try
            {
                Console.WriteLine("Backing up brick_campaign_daily_statistics collection...");
                
                var documents = await _dailyStatisticsCollection.Find(new BsonDocument()).ToListAsync();
                var count = documents.Count;
                Console.WriteLine($"Found {count} documents in brick_campaign_daily_statistics collection");
                
                if (count == 0)
                {
                    Console.WriteLine("No documents to backup in brick_campaign_daily_statistics collection");
                    return null;
                }
                
                // Ensure output directory exists
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                
                // Create backup filename with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"brick_campaign_daily_statistics_backup_{timestamp}.json";
                string backupFilePath = Path.Combine(outputDirectory, backupFileName);
                
                // Convert BsonDocuments to JSON
                var jsonArray = new JArray();
                foreach (var doc in documents)
                {
                    jsonArray.Add(JObject.Parse(doc.ToJson()));
                }
                
                // Write to file (using synchronous method wrapped in Task.Run for .NET Framework compatibility)
                await Task.Run(() => File.WriteAllText(backupFilePath, jsonArray.ToString(Formatting.Indented)));
                
                Console.WriteLine($"Successfully backed up {count} documents to: {backupFilePath}");
                return backupFilePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error backing up daily statistics collection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes all documents from the campaign collection
        /// </summary>
        /// <returns>Number of documents deleted</returns>
        public async Task<long> DeleteCampaignCollectionAsync()
        {
            try
            {
                Console.WriteLine("Deleting all documents from brick_campaign collection...");
                
                var result = await _campaignCollection.DeleteManyAsync(new BsonDocument());
                var deletedCount = result.DeletedCount;
                
                Console.WriteLine($"Successfully deleted {deletedCount} documents from brick_campaign collection");
                return deletedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting campaign collection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes all documents from the daily statistics collection
        /// </summary>
        /// <returns>Number of documents deleted</returns>
        public async Task<long> DeleteDailyStatisticsCollectionAsync()
        {
            try
            {
                Console.WriteLine("Deleting all documents from brick_campaign_daily_statistics collection...");
                
                var result = await _dailyStatisticsCollection.DeleteManyAsync(new BsonDocument());
                var deletedCount = result.DeletedCount;
                
                Console.WriteLine($"Successfully deleted {deletedCount} documents from brick_campaign_daily_statistics collection");
                return deletedCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting daily statistics collection: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Backs up both collections, deletes all data, and returns backup file paths
        /// </summary>
        /// <param name="outputDirectory">Directory to save the backup files</param>
        /// <returns>Tuple containing campaign backup path and daily statistics backup path</returns>
        public async Task<(string campaignBackupPath, string dailyStatisticsBackupPath)> BackupAndDeleteCollectionsAsync(string outputDirectory)
        {
            try
            {
                Console.WriteLine("=== BACKING UP AND DELETING BRICK COLLECTIONS ===");
                
                // Backup both collections
                string campaignBackupPath = await BackupCampaignCollectionAsync(outputDirectory);
                string dailyStatisticsBackupPath = await BackupDailyStatisticsCollectionAsync(outputDirectory);
                
                // Delete both collections
                long campaignDeleted = await DeleteCampaignCollectionAsync();
                long dailyStatisticsDeleted = await DeleteDailyStatisticsCollectionAsync();
                
                Console.WriteLine($"=== BACKUP AND DELETE COMPLETE ===");
                Console.WriteLine($"Campaign collection: {campaignDeleted} documents deleted");
                Console.WriteLine($"Daily statistics collection: {dailyStatisticsDeleted} documents deleted");
                
                return (campaignBackupPath, dailyStatisticsBackupPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during backup and delete operation: {ex.Message}");
                throw;
            }
        }
    }
}

