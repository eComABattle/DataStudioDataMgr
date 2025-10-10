using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Configuration;

namespace DataStudioDataMgr
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<EmfluenceEmailDocument> _emfluenceCollection;

        public MongoDbService()
        {
            //string connectionString = ConfigurationManager.AppSettings["MongoDbConnectionString"] ?? "mongodb://localhost:27017";
            string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["MongoDbConnectionString"].ConnectionString;
            string databaseName = ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration_test";
            string collectionName = ConfigurationManager.AppSettings["MongoDbEmfluenceCollection"] ?? "emfluence_email";

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
            _emfluenceCollection = _database.GetCollection<EmfluenceEmailDocument>(collectionName);
        }

        /// <summary>
        /// Stores Emfluence email records in MongoDB
        /// </summary>
        /// <param name="records">List of email records to store</param>
        /// <returns>Number of documents inserted</returns>
        public async Task<int> StoreEmfluenceEmailsAsync(List<EmfluenceAPI.Record> records)
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
                        
                        
                        // Metadata
                        CreatedAt = DateTime.UtcNow,
                        Source = "EmfluenceAPI"
                    };
                    
                    documents.Add(document);
                }

                if (documents.Count > 0)
                {
                    await _emfluenceCollection.InsertManyAsync(documents);
                    Console.WriteLine($"Successfully stored {documents.Count} Emfluence email records in MongoDB");
                }

                return documents.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing Emfluence emails in MongoDB: {ex.Message}");
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
    }
}
