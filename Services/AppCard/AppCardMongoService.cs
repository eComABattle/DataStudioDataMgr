using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.AppCard
{
    /// <summary>
    /// MongoDB document class for AppCard data
    /// </summary>
    public class AppCardDataDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string ClientToken { get; set; }
        public DateTime TransactionDate { get; set; }
        public int TotalClipCount { get; set; }
        public int TotalRedemptionsCount { get; set; }
        public string MerchantName { get; set; }
        public string ContentProviderName { get; set; }
        public string DigitalOfferId { get; set; }
        public decimal DiscountAmount { get; set; }
        public int NumberOfUniqueRedeemers { get; set; }
        public string SourceFileName { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Service for storing AppCard data in MongoDB
    /// </summary>
    public class AppCardMongoService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<AppCardDataDocument> _dataCollection;

        public AppCardMongoService()
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

            // Get MongoDB database name from App.config (use test database for AppCard)
            var databaseName = System.Configuration.ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration";

            // Expand environment variables in database name
            databaseName = Environment.ExpandEnvironmentVariables(databaseName);

            Console.WriteLine($"Using MongoDB database for AppCard: {databaseName}");

            Console.WriteLine($"Connecting to MongoDB...");
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);

            _dataCollection = _database.GetCollection<AppCardDataDocument>("appCard_coupons");
        }

        public AppCardMongoService(string connectionString, string databaseName = null)
        {
            // Expand environment variables in connection string
            connectionString = Environment.ExpandEnvironmentVariables(connectionString);

            var client = new MongoClient(connectionString);

            // Use provided databaseName or fall back to default (use test database for AppCard)
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = System.Configuration.ConfigurationManager.AppSettings["MongoDbTestDatabaseName"] ?? "integration_test";
            }

            // Expand environment variables in database name
            databaseName = Environment.ExpandEnvironmentVariables(databaseName);

            _database = client.GetDatabase(databaseName);

            _dataCollection = _database.GetCollection<AppCardDataDocument>("appCard_coupons");
        }

        /// <summary>
        /// Stores AppCard data in MongoDB
        /// </summary>
        /// <param name="data">List of AppCard data to store</param>
        /// <param name="clientToken">Client token identifier</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreDataAsync(List<AppCardData> data, string clientToken)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    Console.WriteLine("No AppCard data to store");
                    return 0;
                }

                Console.WriteLine($"Storing {data.Count} AppCard records in MongoDB...");

                var documents = new List<AppCardDataDocument>();
                foreach (var item in data)
                {
                    documents.Add(new AppCardDataDocument
                    {
                        ClientToken = clientToken,
                        TransactionDate = item.TransactionDate,
                        TotalClipCount = item.TotalClipCount,
                        TotalRedemptionsCount = item.TotalRedemptionsCount,
                        MerchantName = item.MerchantName,
                        ContentProviderName = item.ContentProviderName,
                        DigitalOfferId = item.DigitalOfferId,
                        DiscountAmount = item.DiscountAmount,
                        NumberOfUniqueRedeemers = item.NumberOfUniqueRedeemers,
                        SourceFileName = item.SourceFileName,
                        ImportedAt = DateTime.UtcNow
                    });
                }

                await _dataCollection.InsertManyAsync(documents);

                Console.WriteLine($"Successfully stored {data.Count} AppCard records");
                return data.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing AppCard data: {ex.Message}");
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
                Console.WriteLine("Testing MongoDB connection for AppCard service...");

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
        /// Ensures MongoDB collections exist
        /// </summary>
        public async Task EnsureCollectionsExistAsync()
        {
            try
            {
                Console.WriteLine("Ensuring AppCard MongoDB collections exist...");

                // Check if data collection exists, create if not
                var collections = await _database.ListCollectionNamesAsync();
                var collectionNames = await collections.ToListAsync();
                if (!collectionNames.Contains("appCard_coupons"))
                {
                    await _database.CreateCollectionAsync("appCard_coupons");
                    Console.WriteLine("Created collection: appCard_coupons");
                }
                else
                {
                    Console.WriteLine("Collection already exists: appCard_coupons");
                }
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
                Console.WriteLine("Creating MongoDB indexes for AppCard collections...");

                // Create index on TransactionDate for better query performance
                var transactionDateIndex = Builders<AppCardDataDocument>.IndexKeys.Ascending(x => x.TransactionDate);
                await _dataCollection.Indexes.CreateOneAsync(new CreateIndexModel<AppCardDataDocument>(transactionDateIndex));
                Console.WriteLine("Created index on TransactionDate for appCard_coupons collection");

                // Create index on DigitalOfferId for lookups
                var digitalOfferIdIndex = Builders<AppCardDataDocument>.IndexKeys.Ascending(x => x.DigitalOfferId);
                await _dataCollection.Indexes.CreateOneAsync(new CreateIndexModel<AppCardDataDocument>(digitalOfferIdIndex));
                Console.WriteLine("Created index on DigitalOfferId for appCard_coupons collection");

                // Create index on MerchantName for filtering
                var merchantNameIndex = Builders<AppCardDataDocument>.IndexKeys.Ascending(x => x.MerchantName);
                await _dataCollection.Indexes.CreateOneAsync(new CreateIndexModel<AppCardDataDocument>(merchantNameIndex));
                Console.WriteLine("Created index on MerchantName for appCard_coupons collection");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating indexes: {ex.Message}");
                // Don't throw - indexes are optional
            }
        }

        /// <summary>
        /// Gets collection counts
        /// </summary>
        public async Task<int> GetCollectionCountsAsync()
        {
            try
            {
                var dataCount = (int)await _dataCollection.CountDocumentsAsync(_ => true);

                return dataCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting collection counts: {ex.Message}");
                return 0;
            }
        }
    }
}

