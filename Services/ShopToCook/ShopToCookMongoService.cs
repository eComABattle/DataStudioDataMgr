using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.ShopToCook
{
    /// <summary>
    /// MongoDB document classes for ShopToCook data
    /// </summary>
    public class ShopToCookEmailDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string ClientToken { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
        public int Sent { get; set; }
        public int Opened { get; set; }
        public string SourceFileName { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    public class ShopToCookWebDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string ClientToken { get; set; }
        public DateTime Date { get; set; }
        public string Order { get; set; }
        public int TotalImpressions { get; set; }
        public int TotalClicks { get; set; }
        public string SourceFileName { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    public class ShopToCookKioskDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string ClientToken { get; set; }
        public string Feature { get; set; }
        public DateTime Date { get; set; }
        public int FeatureCount { get; set; }
        public string SourceFileName { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Service for storing ShopToCook data in MongoDB integration_test database
    /// </summary>
    public class ShopToCookMongoService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<ShopToCookEmailDocument> _emailCollection;
        private readonly IMongoCollection<ShopToCookWebDocument> _webCollection;
        private readonly IMongoCollection<ShopToCookKioskDocument> _kioskCollection;

        public ShopToCookMongoService()
        {
            var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["MongoDbConnectionString"]?.ConnectionString;
            
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

            // Get MongoDB database name from App.config - ShopToCook uses production database
            var databaseName = System.Configuration.ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration";
            
            // Expand environment variables in database name
            databaseName = Environment.ExpandEnvironmentVariables(databaseName);
            
            Console.WriteLine($"Using MongoDB database for ShopToCook: {databaseName}");

            Console.WriteLine($"Connecting to MongoDB...");
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
            
            _emailCollection = _database.GetCollection<ShopToCookEmailDocument>("shoptocook_email");
            _webCollection = _database.GetCollection<ShopToCookWebDocument>("shoptocook_website");
            _kioskCollection = _database.GetCollection<ShopToCookKioskDocument>("shoptocook_kiosk");
        }

        public ShopToCookMongoService(string connectionString, string databaseName = null)
        {
            // Expand environment variables in connection string
            connectionString = Environment.ExpandEnvironmentVariables(connectionString);
            
            var client = new MongoClient(connectionString);
            
            // Use provided databaseName or fall back to production database for ShopToCook
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = System.Configuration.ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration";
            }
            
            // Expand environment variables in database name
            databaseName = Environment.ExpandEnvironmentVariables(databaseName);
            
            _database = client.GetDatabase(databaseName);
            
            _emailCollection = _database.GetCollection<ShopToCookEmailDocument>("shoptocook_email");
            _webCollection = _database.GetCollection<ShopToCookWebDocument>("shoptocook_website");
            _kioskCollection = _database.GetCollection<ShopToCookKioskDocument>("shoptocook_kiosk");
        }

        /// <summary>
        /// Stores email data in MongoDB
        /// </summary>
        /// <param name="emails">List of email data to store</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreEmailDataAsync(List<ShopToCookEmail> emails, string clientToken)
        {
            try
            {
                if (emails == null || emails.Count == 0)
                {
                    Console.WriteLine("No email data to store");
                    return 0;
                }

                Console.WriteLine($"Storing {emails.Count} email records in MongoDB...");

                var documents = new List<ShopToCookEmailDocument>();
                foreach (var email in emails)
                {
                    documents.Add(new ShopToCookEmailDocument
                    {
                        ClientToken = clientToken,
                        Email = email.Email,
                        Date = email.Date,
                        Sent = email.Sent,
                        Opened = email.Opened,
                        SourceFileName = email.SourceFileName,
                        ImportedAt = DateTime.UtcNow
                    });
                }

                await _emailCollection.InsertManyAsync(documents);
                
                Console.WriteLine($"Successfully stored {emails.Count} email records");
                return emails.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing email data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stores web data in MongoDB
        /// </summary>
        /// <param name="webData">List of web data to store</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreWebDataAsync(List<ShopToCookWeb> webData, string clientToken)
        {
            try
            {
                if (webData == null || webData.Count == 0)
                {
                    Console.WriteLine("No web data to store");
                    return 0;
                }

                Console.WriteLine($"Storing {webData.Count} web records in MongoDB...");

                var documents = new List<ShopToCookWebDocument>();
                foreach (var web in webData)
                {
                    documents.Add(new ShopToCookWebDocument
                    {
                        ClientToken = clientToken,
                        Date = web.Date,
                        Order = web.Order,
                        TotalImpressions = web.TotalImpressions,
                        TotalClicks = web.TotalClicks,
                        SourceFileName = web.SourceFileName,
                        ImportedAt = DateTime.UtcNow
                    });
                }

                await _webCollection.InsertManyAsync(documents);
                
                Console.WriteLine($"Successfully stored {webData.Count} web records");
                return webData.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing web data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stores kiosk data in MongoDB
        /// </summary>
        /// <param name="kioskData">List of kiosk data to store</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreKioskDataAsync(List<ShopToCookKiosk> kioskData, string clientToken)
        {
            try
            {
                if (kioskData == null || kioskData.Count == 0)
                {
                    Console.WriteLine("No kiosk data to store");
                    return 0;
                }

                Console.WriteLine($"Storing {kioskData.Count} kiosk records in MongoDB...");

                var documents = new List<ShopToCookKioskDocument>();
                foreach (var kiosk in kioskData)
                {
                    documents.Add(new ShopToCookKioskDocument
                    {
                        ClientToken = clientToken,
                        Feature = kiosk.Feature,
                        Date = kiosk.Date,
                        FeatureCount = kiosk.FeatureCount,
                        SourceFileName = kiosk.SourceFileName,
                        ImportedAt = DateTime.UtcNow
                    });
                }

                await _kioskCollection.InsertManyAsync(documents);
                
                Console.WriteLine($"Successfully stored {kioskData.Count} kiosk records");
                return kioskData.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing kiosk data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tests the MongoDB connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Console.WriteLine("Testing MongoDB connection...");
                
                // Try to list collections to test connection
                var collections = await _database.ListCollectionNamesAsync();
                var collectionNames = await collections.ToListAsync();
                
                Console.WriteLine($"MongoDB connection successful! Found {collectionNames.Count} collections in database '{_database.DatabaseNamespace.DatabaseName}'");
                
                if (collectionNames.Count > 0)
                {
                    Console.WriteLine("Existing collections:");
                    foreach (var collectionName in collectionNames)
                    {
                        Console.WriteLine($"  - {collectionName}");
                    }
                }
                
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
        /// Alternative MongoDB connection test using a simple operation
        /// </summary>
        public async Task<bool> TestConnectionSimpleAsync()
        {
            try
            {
                Console.WriteLine("Testing MongoDB connection (simple method)...");
                
                // Try to get database stats as a simple connection test
                var stats = await _database.RunCommandAsync<BsonDocument>("{dbStats: 1}");
                
                Console.WriteLine($"MongoDB connection successful! Database stats retrieved.");
                Console.WriteLine($"  Database: {stats.GetValue("db", "unknown")}");
                Console.WriteLine($"  Collections: {stats.GetValue("collections", 0)}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MongoDB connection failed (simple method): {ex.Message}");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }
        public async Task EnsureCollectionsExistAsync()
        {
            try
            {
                Console.WriteLine("Ensuring MongoDB collections exist...");
                
                // Check if collections exist, create if they don't
                var collections = await _database.ListCollectionNamesAsync();
                var collectionNames = await collections.ToListAsync();
                
                var requiredCollections = new[] { "shoptocook_email", "shoptocook_website", "shoptocook_kiosk" };
                
                foreach (var collectionName in requiredCollections)
                {
                    if (!collectionNames.Contains(collectionName))
                    {
                        Console.WriteLine($"Creating collection: {collectionName}");
                        await _database.CreateCollectionAsync(collectionName);
                        Console.WriteLine($"Successfully created collection: {collectionName}");
                    }
                    else
                    {
                        Console.WriteLine($"Collection already exists: {collectionName}");
                    }
                }
                
                Console.WriteLine("All required collections verified/created");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ensuring collections exist: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates indexes for better query performance
        /// </summary>
        public async Task CreateIndexesAsync()
        {
            try
            {
                Console.WriteLine("Creating indexes for ShopToCook collections...");

                // Email collection indexes
                await _emailCollection.Indexes.CreateOneAsync(
                    new CreateIndexModel<ShopToCookEmailDocument>(
                        Builders<ShopToCookEmailDocument>.IndexKeys.Ascending(x => x.Email)));
                
                await _emailCollection.Indexes.CreateOneAsync(
                    new CreateIndexModel<ShopToCookEmailDocument>(
                        Builders<ShopToCookEmailDocument>.IndexKeys.Ascending(x => x.Date)));

                // Web collection indexes
                await _webCollection.Indexes.CreateOneAsync(
                    new CreateIndexModel<ShopToCookWebDocument>(
                        Builders<ShopToCookWebDocument>.IndexKeys.Ascending(x => x.Date)));

                // Kiosk collection indexes
                await _kioskCollection.Indexes.CreateOneAsync(
                    new CreateIndexModel<ShopToCookKioskDocument>(
                        Builders<ShopToCookKioskDocument>.IndexKeys.Ascending(x => x.Feature)));
                
                await _kioskCollection.Indexes.CreateOneAsync(
                    new CreateIndexModel<ShopToCookKioskDocument>(
                        Builders<ShopToCookKioskDocument>.IndexKeys.Ascending(x => x.Date)));

                Console.WriteLine("Successfully created indexes for ShopToCook collections");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating indexes: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the count of records in each collection
        /// </summary>
        public async Task<(long emailCount, long webCount, long kioskCount)> GetCollectionCountsAsync()
        {
            try
            {
                var emailCount = await _emailCollection.CountDocumentsAsync(FilterDefinition<ShopToCookEmailDocument>.Empty);
                var webCount = await _webCollection.CountDocumentsAsync(FilterDefinition<ShopToCookWebDocument>.Empty);
                var kioskCount = await _kioskCollection.CountDocumentsAsync(FilterDefinition<ShopToCookKioskDocument>.Empty);

                return (emailCount, webCount, kioskCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting collection counts: {ex.Message}");
                throw;
            }
        }
    }
}
