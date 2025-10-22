using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Configuration;
using DataStudioDataMgr.Services.Emfluence;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.MongoDb
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<EmfluenceEmailDocument> _emfluenceCollection;
        private readonly IMongoCollection<CampaignDocument> _campaignCollection;
        private readonly IMongoCollection<UnfiPostCampaignStoreDocument> _unfiPostCampaignStoreCollection;
        private readonly IMongoCollection<CampaignStoreMetricsDocument> _campaignStoreMetricsCollection;
        private readonly IMongoCollection<EntryMetricsDocument> _entryMetricsCollection;

        public MongoDbService()
        {
            //string connectionString = ConfigurationManager.AppSettings["MongoDbConnectionString"] ?? "mongodb://localhost:27017";
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["MongoDbConnectionString"].ConnectionString;
            
            // Expand environment variables in connection string
            connectionString = Environment.ExpandEnvironmentVariables(connectionString);
            
            string databaseName = ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration_test";
            string collectionName = ConfigurationManager.AppSettings["MongoDbEmfluenceCollection"] ?? "emfluence_email";
            
            // Expand environment variables in database and collection names
            databaseName = Environment.ExpandEnvironmentVariables(databaseName);
            collectionName = Environment.ExpandEnvironmentVariables(collectionName);

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
            _emfluenceCollection = _database.GetCollection<EmfluenceEmailDocument>(collectionName);
            _campaignCollection = _database.GetCollection<CampaignDocument>("ad_campaign");
            _unfiPostCampaignStoreCollection = _database.GetCollection<UnfiPostCampaignStoreDocument>("unfi_post_campaign_store");
            _campaignStoreMetricsCollection = _database.GetCollection<CampaignStoreMetricsDocument>("campaign_store_metrics");
            _entryMetricsCollection = _database.GetCollection<EntryMetricsDocument>("entry_metrics");
        }

        /// <summary>
        /// Stores Emfluence email records in MongoDB
        /// </summary>
        /// <param name="records">List of email records to store</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="storeName">Store name</param>
        /// <returns>Number of documents inserted</returns>
        public async Task<int> StoreEmfluenceEmailsAsync(List<EmfluenceAPI.Record> records, string storeId = "default", string storeName = "Default Store")
        {
            try
            {
                var documents = new List<EmfluenceEmailDocument>();
                
                foreach (var record in records)
                {
                    var document = new EmfluenceEmailDocument
                    {
                        ClientToken = "AWG",
                        EmailID = record.EmailID,                        
                        Subject = record.Subject,                      
                        FromName = record.FromName,                      
                        Status = record.Status,
                        Title = record.Title,
                      
                        DateAdded = record.DateAdded,
                        DateModified = record.DateModified,
                        DateSent = record.DateSent,
                                              
                        // Metrics
                        Clicks = record.Metrics?.Clicks ?? 0,
                        UniqueViews = record.Metrics?.UniqueViews ?? 0,
                        Bounces = record.Metrics?.Bounces ?? 0,
                        Forwards = record.Metrics?.Forwards ?? 0,
                        Recipients = record.Metrics?.Recipients ?? 0,
                        UniqueForwards = record.Metrics?.UniqueForwards ?? 0,
                        ClickToViewRate = record.Metrics?.ClickToViewRate ?? 0,
                        SendingIssues = record.Metrics?.SendingIssues ?? 0,
                        WebViews = record.Metrics?.WebViews ?? 0,
                        Complaints = record.Metrics?.Complaints ?? 0,
                        Shares = record.Metrics?.Shares ?? 0,
                        UniqueClicks = record.Metrics?.UniqueClicks ?? 0,
                        Unsubscribes = record.Metrics?.Unsubscribes ?? 0,
                        UniqueWebViews = record.Metrics?.UniqueWebViews ?? 0,
                        Views = record.Metrics?.Views ?? 0,
                        UniqueShares = record.Metrics?.UniqueShares ?? 0,
                        
                        // Store information
                        StoreId = storeId,
                        StoreName = storeName,
                        
                        // Metadata
                        CreatedAt = DateTime.UtcNow,
                        Source = "EmfluenceAPI"
                    };
                    
                    documents.Add(document);
                }

                if (documents.Count > 0)
                {
                    await _emfluenceCollection.InsertManyAsync(documents);
                    Console.WriteLine($"Successfully stored {documents.Count} Emfluence email records in MongoDB for store: {storeName}");
                }

                return documents.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing Emfluence emails in MongoDB for store {storeName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stores Campaign records in MongoDB
        /// </summary>
        /// <param name="campaigns">List of campaigns to store</param>
        /// <returns>Number of documents inserted</returns>
        public async Task<int> StoreCampaignsAsync(List<Campaign> campaigns, string csvFilePath)
        {
            try
            {
                var documents = new List<CampaignDocument>();
                
                foreach (var campaign in campaigns)
                {
                    var document = new CampaignDocument
                    {
                        CampaignId = campaign.Id,
                        BrandId = campaign.BrandId,
                        Name = campaign.Name,
                        Description = campaign.Description,
                        StartDate = campaign.StartDate,
                        EndDate = campaign.EndDate,
                        CreatedAt = DateTime.UtcNow,
                        FileName = csvFilePath,
                        Source = "SqlServer"
                    };
                    
                    documents.Add(document);
                }

                if (documents.Count > 0)
                {
                    await _campaignCollection.InsertManyAsync(documents);
                    Console.WriteLine($"Successfully stored {documents.Count} campaign records in MongoDB");
                }

                return documents.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing campaigns in MongoDB: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stores UNFI Post Campaign Store records in MongoDB
        /// </summary>
        /// <param name="records">List of records to store</param>
        /// <returns>Number of documents inserted</returns>
        public async Task<int> StoreUnfiPostCampaignStoreAsync(List<UnfiPostCampaignStore> records)
        {
            try
            {
                var documents = new List<UnfiPostCampaignStoreDocument>();
                
                foreach (var record in records)
                {
                    var document = new UnfiPostCampaignStoreDocument
                    {
                        ClientToken = record.ClientToken,
                        RecordId = record.Id,
                        GroupCampaignId = record.GroupCampaignId,
                        CustomerGuid = record.CustomerGuid,
                        ExternalId = record.ExternalId,
                        SecondaryExternalId = record.SecondaryExternalId,
                        Url = record.Url,
                        CreatedAt = DateTime.UtcNow,
                        Source = "MediaStudio"
                    };
                    
                    documents.Add(document);
                }

                if (documents.Count > 0)
                {
                    await _unfiPostCampaignStoreCollection.InsertManyAsync(documents);
                    Console.WriteLine($"Successfully stored {documents.Count} UNFI Post Campaign Store records in MongoDB");
                }

                return documents.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing UNFI Post Campaign Store records in MongoDB: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stores Campaign Store Metrics records in MongoDB
        /// </summary>
        /// <param name="records">List of records to store</param>
        /// <returns>Number of documents inserted</returns>
        public async Task<int> StoreCampaignStoreMetricsAsync(List<CampaignStoreMetrics> records)
        {
            try
            {
                var documents = new List<CampaignStoreMetricsDocument>();
                
                foreach (var record in records)
                {
                    var document = new CampaignStoreMetricsDocument
                    {
                        Platform = record.Platform,
                        MetricType = record.MetricType,
                        MetricTypeId = record.MetricTypeId,
                        PostId = record.PostID,
                        PostName = record.PostName,
                        PostScheduledDate = record.PostScheduledDate,
                        MetricDate = record.MetricDate,
                        ClientToken = record.ClientToken,
                        Store = record.Store,
                        TotalMetricValue = record.TotalMetricValue,
                        MetricDelta = record.MetricDelta,
                        CreatedAt = DateTime.UtcNow,
                        Source = "DigitalStudio"
                    };
                    
                    documents.Add(document);
                }

                if (documents.Count > 0)
                {
                    await _campaignStoreMetricsCollection.InsertManyAsync(documents);
                    Console.WriteLine($"Successfully stored {documents.Count} Campaign Store Metrics records in MongoDB");
                }

                return documents.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing Campaign Store Metrics records in MongoDB: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stores Entry Metrics records in MongoDB
        /// </summary>
        /// <param name="records">List of records to store</param>
        /// <returns>Number of documents inserted</returns>
        public async Task<int> StoreEntryMetricsAsync(List<EntryMetrics> records)
        {
            try
            {
                var documents = new List<EntryMetricsDocument>();
                
                foreach (var record in records)
                {
                    var document = new EntryMetricsDocument
                    {
                        AccountId = record.AccountId,
                        AccountName = record.AccountName,
                        Customer = record.Customer,
                        CampaignName = record.CampaignName,
                        EntryId = record.EntryId,
                        EntryType = record.EntryType,
                        MetricType = record.MetricType,
                        //Timestamp = record.Timestamp,
                        MetaData = record.MetaData,
                        CreatedAt = DateTime.UtcNow,
                        Source = "DigitalStudio"
                    };
                    
                    documents.Add(document);
                }

                if (documents.Count > 0)
                {
                    await _entryMetricsCollection.InsertManyAsync(documents);
                    Console.WriteLine($"Successfully stored {documents.Count} Entry Metrics records in MongoDB");
                }

                return documents.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing Entry Metrics records in MongoDB: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the count of documents in the Emfluence collection
        /// </summary>
        /// <returns>Number of documents in the collection</returns>
        public async Task<long> GetEmfluenceEmailCountAsync()
        {
            try
            {
                return await _emfluenceCollection.CountDocumentsAsync(FilterDefinition<EmfluenceEmailDocument>.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Emfluence email count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the count of documents in the Campaign collection
        /// </summary>
        /// <returns>Number of documents in the collection</returns>
        public async Task<long> GetCampaignCountAsync()
        {
            try
            {
                return await _campaignCollection.CountDocumentsAsync(FilterDefinition<CampaignDocument>.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting campaign count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the count of documents in the UNFI Post Campaign Store collection
        /// </summary>
        /// <returns>Number of documents in the collection</returns>
        public async Task<long> GetUnfiPostCampaignStoreCountAsync()
        {
            try
            {
                return await _unfiPostCampaignStoreCollection.CountDocumentsAsync(FilterDefinition<UnfiPostCampaignStoreDocument>.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting UNFI Post Campaign Store count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the count of documents in the Campaign Store Metrics collection
        /// </summary>
        /// <returns>Number of documents in the collection</returns>
        public async Task<long> GetCampaignStoreMetricsCountAsync()
        {
            try
            {
                return await _campaignStoreMetricsCollection.CountDocumentsAsync(FilterDefinition<CampaignStoreMetricsDocument>.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Campaign Store Metrics count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the count of documents in the Entry Metrics collection
        /// </summary>
        /// <returns>Number of documents in the collection</returns>
        public async Task<long> GetEntryMetricsCountAsync()
        {
            try
            {
                return await _entryMetricsCollection.CountDocumentsAsync(FilterDefinition<EntryMetricsDocument>.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Entry Metrics count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Creates indexes for better query performance
        /// </summary>
        public async Task CreateIndexesAsync()
        {
            try
            {
                // Create index on EmailID for fast lookups
                var emailIdIndex = Builders<EmfluenceEmailDocument>.IndexKeys.Ascending(x => x.EmailID);
                await _emfluenceCollection.Indexes.CreateOneAsync(new CreateIndexModel<EmfluenceEmailDocument>(emailIdIndex));

                // Create index on DateSent for date range queries
                var dateSentIndex = Builders<EmfluenceEmailDocument>.IndexKeys.Ascending(x => x.DateSent);
                await _emfluenceCollection.Indexes.CreateOneAsync(new CreateIndexModel<EmfluenceEmailDocument>(dateSentIndex));

                // Create index on Status for filtering
                var statusIndex = Builders<EmfluenceEmailDocument>.IndexKeys.Ascending(x => x.Status);
                await _emfluenceCollection.Indexes.CreateOneAsync(new CreateIndexModel<EmfluenceEmailDocument>(statusIndex));

                // Create compound index on UserID and DateSent
                var compoundIndex = Builders<EmfluenceEmailDocument>.IndexKeys
                    .Ascending(x => x.UserID)
                    .Ascending(x => x.DateSent);
                await _emfluenceCollection.Indexes.CreateOneAsync(new CreateIndexModel<EmfluenceEmailDocument>(compoundIndex));

                // Create index on StoreId for store-specific queries
                var storeIdIndex = Builders<EmfluenceEmailDocument>.IndexKeys.Ascending(x => x.StoreId);
                await _emfluenceCollection.Indexes.CreateOneAsync(new CreateIndexModel<EmfluenceEmailDocument>(storeIdIndex));

                // Create compound index on StoreId and DateSent for store-specific date queries
                var storeDateIndex = Builders<EmfluenceEmailDocument>.IndexKeys
                    .Ascending(x => x.StoreId)
                    .Ascending(x => x.DateSent);
                await _emfluenceCollection.Indexes.CreateOneAsync(new CreateIndexModel<EmfluenceEmailDocument>(storeDateIndex));

                // Create indexes for Campaign collection
                // Create index on CampaignId for fast lookups
                var campaignIdIndex = Builders<CampaignDocument>.IndexKeys.Ascending(x => x.CampaignId);
                await _campaignCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignDocument>(campaignIdIndex));

                // Create index on BrandId for brand-specific queries
                var brandIdIndex = Builders<CampaignDocument>.IndexKeys.Ascending(x => x.BrandId);
                await _campaignCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignDocument>(brandIdIndex));

                // Create index on Name for text searches
                var nameIndex = Builders<CampaignDocument>.IndexKeys.Ascending(x => x.Name);
                await _campaignCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignDocument>(nameIndex));

                // Create compound index on BrandId and CampaignId
                var brandCampaignIndex = Builders<CampaignDocument>.IndexKeys
                    .Ascending(x => x.BrandId)
                    .Ascending(x => x.CampaignId);
                await _campaignCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignDocument>(brandCampaignIndex));

                // Create indexes for UNFI Post Campaign Store collection
                // Create index on RecordId for fast lookups
                var recordIdIndex = Builders<UnfiPostCampaignStoreDocument>.IndexKeys.Ascending(x => x.RecordId);
                await _unfiPostCampaignStoreCollection.Indexes.CreateOneAsync(new CreateIndexModel<UnfiPostCampaignStoreDocument>(recordIdIndex));

                // Create index on GroupCampaignId for group-specific queries
                var groupCampaignIdIndex = Builders<UnfiPostCampaignStoreDocument>.IndexKeys.Ascending(x => x.GroupCampaignId);
                await _unfiPostCampaignStoreCollection.Indexes.CreateOneAsync(new CreateIndexModel<UnfiPostCampaignStoreDocument>(groupCampaignIdIndex));

                // Create index on ClientToken for client-specific queries
                var clientTokenIndex = Builders<UnfiPostCampaignStoreDocument>.IndexKeys.Ascending(x => x.ClientToken);
                await _unfiPostCampaignStoreCollection.Indexes.CreateOneAsync(new CreateIndexModel<UnfiPostCampaignStoreDocument>(clientTokenIndex));

                // Create index on ExternalId for external ID lookups
                var externalIdIndex = Builders<UnfiPostCampaignStoreDocument>.IndexKeys.Ascending(x => x.ExternalId);
                await _unfiPostCampaignStoreCollection.Indexes.CreateOneAsync(new CreateIndexModel<UnfiPostCampaignStoreDocument>(externalIdIndex));

                // Create compound index on ClientToken and GroupCampaignId
                var clientGroupIndex = Builders<UnfiPostCampaignStoreDocument>.IndexKeys
                    .Ascending(x => x.ClientToken)
                    .Ascending(x => x.GroupCampaignId);
                await _unfiPostCampaignStoreCollection.Indexes.CreateOneAsync(new CreateIndexModel<UnfiPostCampaignStoreDocument>(clientGroupIndex));

                // Create indexes for Campaign Store Metrics collection
                // Create index on PostId for fast lookups
                var postIdIndex = Builders<CampaignStoreMetricsDocument>.IndexKeys.Ascending(x => x.PostId);
                await _campaignStoreMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignStoreMetricsDocument>(postIdIndex));

                // Create index on Store for store-specific queries
                var storeIndex = Builders<CampaignStoreMetricsDocument>.IndexKeys.Ascending(x => x.Store);
                await _campaignStoreMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignStoreMetricsDocument>(storeIndex));

                // Create index on Platform for platform-specific queries
                var platformIndex = Builders<CampaignStoreMetricsDocument>.IndexKeys.Ascending(x => x.Platform);
                await _campaignStoreMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignStoreMetricsDocument>(platformIndex));

                // Create index on MetricType for metric type queries
                var metricTypeIndex = Builders<CampaignStoreMetricsDocument>.IndexKeys.Ascending(x => x.MetricType);
                await _campaignStoreMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignStoreMetricsDocument>(metricTypeIndex));

                // Create index on MetricDate for date-based queries
                var metricDateIndex = Builders<CampaignStoreMetricsDocument>.IndexKeys.Ascending(x => x.MetricDate);
                await _campaignStoreMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignStoreMetricsDocument>(metricDateIndex));

                // Create compound index on Store, Platform, and MetricDate
                var storePlatformDateIndex = Builders<CampaignStoreMetricsDocument>.IndexKeys
                    .Ascending(x => x.Store)
                    .Ascending(x => x.Platform)
                    .Ascending(x => x.MetricDate);
                await _campaignStoreMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<CampaignStoreMetricsDocument>(storePlatformDateIndex));

                // Create indexes for Entry Metrics collection
                // Create index on AccountId for fast lookups
                var accountIdIndex = Builders<EntryMetricsDocument>.IndexKeys.Ascending(x => x.AccountId);
                await _entryMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<EntryMetricsDocument>(accountIdIndex));

                // Create index on EntryId for entry-specific queries
                var entryIdIndex = Builders<EntryMetricsDocument>.IndexKeys.Ascending(x => x.EntryId);
                await _entryMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<EntryMetricsDocument>(entryIdIndex));

                // Create index on Customer for customer-specific queries
                var customerIndex = Builders<EntryMetricsDocument>.IndexKeys.Ascending(x => x.Customer);
                await _entryMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<EntryMetricsDocument>(customerIndex));

                // Create index on CampaignName for campaign-specific queries
                var campaignNameIndex = Builders<EntryMetricsDocument>.IndexKeys.Ascending(x => x.CampaignName);
                await _entryMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<EntryMetricsDocument>(campaignNameIndex));

                // Create index on MetricType for metric type queries
                var entryMetricTypeIndex = Builders<EntryMetricsDocument>.IndexKeys.Ascending(x => x.MetricType);
                await _entryMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<EntryMetricsDocument>(entryMetricTypeIndex));

                // Create index on Timestamp for date-based queries
                //var timestampIndex = Builders<EntryMetricsDocument>.IndexKeys.Ascending(x => x.Timestamp);
                //await _entryMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<EntryMetricsDocument>(timestampIndex));

                // Create compound index on AccountId, EntryId, and Timestamp
                var accountEntryTimestampIndex = Builders<EntryMetricsDocument>.IndexKeys
                    .Ascending(x => x.AccountId)
                    .Ascending(x => x.EntryId);
                    //.Ascending(x => x.Timestamp);
                await _entryMetricsCollection.Indexes.CreateOneAsync(new CreateIndexModel<EntryMetricsDocument>(accountEntryTimestampIndex));

                Console.WriteLine("MongoDB indexes created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating MongoDB indexes: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Document model for storing Emfluence email data in MongoDB
    /// </summary>
    public class EmfluenceEmailDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Core email properties
        [BsonElement("emailId")]
        public long EmailID { get; set; }

        //[BsonElement("templateId")]
        //public int TemplateID { get; set; }

        [BsonElement("subject")]
        public string Subject { get; set; }

        //[BsonElement("fromAddress")]
        //public string FromAddress { get; set; }

        [BsonElement("fromName")]
        public string FromName { get; set; }

        //[BsonElement("replyToAddress")]
        //public string ReplyToAddress { get; set; }

        //[BsonElement("replyToName")]
        //public string ReplyToName { get; set; }

        //[BsonElement("deliveryType")]
        //public string DeliveryType { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("title")]
        public string Title { get; set; }

        [BsonElement("userId")]
        public int UserID { get; set; }

        // Date fields
        [BsonElement("dateAdded")]
        public DateTime DateAdded { get; set; }

        [BsonElement("dateModified")]
        public DateTime DateModified { get; set; }

        [BsonElement("dateSent")]
        public DateTime DateSent { get; set; }

        //[BsonElement("dateScheduled")]
        //public string DateScheduled { get; set; }

        //// Additional fields
        //[BsonElement("parentEmailId")]
        //public string ParentEmailID { get; set; }

        //[BsonElement("abSplitId")]
        //public string AbSplitID { get; set; }

        //[BsonElement("campaignIds")]
        //public List<int> CampaignIDs { get; set; }

        //[BsonElement("confirmed")]
        //public int Confirmed { get; set; }

        // Metrics
        [BsonElement("clicks")]
        public int Clicks { get; set; }

        [BsonElement("uniqueViews")]
        public int UniqueViews { get; set; }

        [BsonElement("bounces")]
        public int Bounces { get; set; }

        [BsonElement("forwards")]
        public int Forwards { get; set; }

        [BsonElement("recipients")]
        public int Recipients { get; set; }

        [BsonElement("uniqueForwards")]
        public int UniqueForwards { get; set; }

        [BsonElement("clickToViewRate")]
        public double ClickToViewRate { get; set; }

        [BsonElement("sendingIssues")]
        public int SendingIssues { get; set; }

        [BsonElement("webViews")]
        public int WebViews { get; set; }

        [BsonElement("complaints")]
        public int Complaints { get; set; }

        [BsonElement("shares")]
        public int Shares { get; set; }

        [BsonElement("uniqueClicks")]
        public int UniqueClicks { get; set; }

        [BsonElement("unsubscribes")]
        public int Unsubscribes { get; set; }

        [BsonElement("uniqueWebViews")]
        public int UniqueWebViews { get; set; }

        [BsonElement("views")]
        public int Views { get; set; }

        [BsonElement("uniqueShares")]
        public int UniqueShares { get; set; }

        // Metadata
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("source")]
        public string Source { get; set; }

        [BsonElement("clientToken")]    
        public string ClientToken { get; set; }

        // Store information
        [BsonElement("storeId")]
        public string StoreId { get; set; }

        [BsonElement("storeName")]
        public string StoreName { get; set; }
    }

    /// <summary>
    /// Document model for storing Campaign data in MongoDB
    /// </summary>
    public class CampaignDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Campaign properties
        [BsonElement("campaignId")]
        public int CampaignId { get; set; }

        [BsonElement("brandId")]
        public int BrandId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("startDate")]
        public string StartDate { get; set; }

        [BsonElement("endDate")]
        public string EndDate { get; set; }

        // Metadata
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("fileName")]
        public string FileName { get; set; }

        [BsonElement("source")]
        public string Source { get; set; }
    }

    /// <summary>
    /// Document model for storing UNFI Post Campaign Store data in MongoDB
    /// </summary>
    public class UnfiPostCampaignStoreDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // UNFI Post Campaign Store properties
        [BsonElement("clientToken")]
        public string ClientToken { get; set; }

        [BsonElement("recordId")]
        public int RecordId { get; set; }

        [BsonElement("groupCampaignId")]
        public int GroupCampaignId { get; set; }

        [BsonElement("customerGuid")]
        public string CustomerGuid { get; set; }

        [BsonElement("externalId")]
        public string ExternalId { get; set; }

        [BsonElement("secondaryExternalId")]
        public string SecondaryExternalId { get; set; }

        [BsonElement("url")]
        public string Url { get; set; }

        // Metadata
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("source")]
        public string Source { get; set; }
    }

    /// <summary>
    /// Document model for storing Campaign Store Metrics data in MongoDB
    /// </summary>
    public class CampaignStoreMetricsDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Campaign Store Metrics properties
        [BsonElement("platform")]
        public string Platform { get; set; }

        [BsonElement("metricType")]
        public string MetricType { get; set; }

        [BsonElement("metricTypeId")]
        public string MetricTypeId { get; set; }

        [BsonElement("postId")]
        public string PostId { get; set; }

        [BsonElement("postName")]
        public string PostName { get; set; }

        [BsonElement("postScheduledDate")]
        public string PostScheduledDate { get; set; }

        [BsonElement("metricDate")]
        public string MetricDate { get; set; }

        [BsonElement("clientToken")]
        public string ClientToken { get; set; }

        [BsonElement("store")]
        public string Store { get; set; }

        [BsonElement("totalMetricValue")]
        public int TotalMetricValue { get; set; }

        [BsonElement("metricDelta")]
        public int MetricDelta { get; set; }

        // Metadata
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("source")]
        public string Source { get; set; }
    }

    /// <summary>
    /// Document model for storing Entry Metrics data in MongoDB
    /// </summary>
    public class EntryMetricsDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Entry Metrics properties
        [BsonElement("accountId")]
        public int AccountId { get; set; }

        [BsonElement("accountName")]
        public string AccountName { get; set; }

        [BsonElement("customer")]
        public string Customer { get; set; }

        [BsonElement("campaignName")]
        public string CampaignName { get; set; }

        [BsonElement("entryId")]
        public int EntryId { get; set; }

        [BsonElement("entryType")]
        public string EntryType { get; set; }

        [BsonElement("metricType")]
        public string MetricType { get; set; }

        //[BsonElement("timestamp")]
        //public string Timestamp { get; set; }

        [BsonElement("metaData")]
        public string MetaData { get; set; }

        // Metadata
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("source")]
        public string Source { get; set; }
    }
}
