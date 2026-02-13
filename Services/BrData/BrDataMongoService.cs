using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.BrData
{
    /// <summary>
    /// MongoDB document class for BrData POS data
    /// </summary>
    public class BrDataPosDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string client_token { get; set; }
        public string store_number { get; set; }
        public string store_name { get; set; }       
        public DateTime? date { get; set; }
        public decimal? qty_sold { get; set; }
        public decimal? amount_sold { get; set; }
        public string department { get; set; }
        public string brand { get; set; }
        public string upc { get; set; }
        public string central_description { get; set; }
        public DateTime? last_updated { get; set; }
        public int row_count { get; set; }
        public string file_name { get; set; }
        public DateTime importedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Service for storing BrData POS data in MongoDB
    /// </summary>
    public class BrDataMongoService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BrDataPosDocument> _posCollection;

        public BrDataMongoService()
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

            // Get MongoDB database name from App.config
            var databaseName = System.Configuration.ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration_test";

            // Expand environment variables in database name
            databaseName = Environment.ExpandEnvironmentVariables(databaseName);
            databaseName = "integration_test";

            Console.WriteLine($"Using MongoDB database for BrData: {databaseName}");

            Console.WriteLine($"Connecting to MongoDB...");
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);

            _posCollection = _database.GetCollection<BrDataPosDocument>("brdata_pos");
        }

        public BrDataMongoService(string connectionString, string databaseName = null)
        {
            // Expand environment variables in connection string
            connectionString = Environment.ExpandEnvironmentVariables(connectionString);

            var client = new MongoClient(connectionString);

            // Use provided databaseName or fall back to default
            if (string.IsNullOrEmpty(databaseName))
            {
                databaseName = System.Configuration.ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration_test";
            }

            // Expand environment variables in database name
            databaseName = "integration_test";

            _database = client.GetDatabase(databaseName);

            _posCollection = _database.GetCollection<BrDataPosDocument>("brdata_pos");
        }

        /// <summary>
        /// Gets customer data from SQL Server (CustomerNumber and CustomerName mapping)
        /// </summary>
        /// <returns>Dictionary mapping CustomerNumber to CustomerName</returns>
        private async Task<Dictionary<string, string>> GetCustomerDataAsync()
        {
            var customerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var connectionString = ConfigurationManager.ConnectionStrings["AdStudioCertco"]?.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    Console.WriteLine("Warning: AdStudioCertco connection string not found. Store names will not be populated.");
                    return customerMap;
                }

                //string query = @"SELECT c.CustomerNumber, c.CustomerName
                //FROM Certco.[dbo].[Customer] c with (nolock)";

                string query = @"SELECT CASE WHEN TRY_CONVERT(BIGINT, c.CustomerNumber) IS NOT NULL THEN RIGHT(REPLICATE('0', 6) + c.CustomerNumber, 6) ELSE c.CustomerNumber
		        END AS CustomerNumber , c.CustomerName FROM Certco.[dbo].[Customer] c WITH (NOLOCK)";

                Console.WriteLine("Querying SQL Server for customer data...");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Connected to AdStudioCertco database successfully");

                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var customerNumber = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                                var customerName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

                                if (!string.IsNullOrEmpty(customerNumber))
                                {
                                    customerMap[customerNumber] = customerName;
                                }
                            }
                        }
                    }
                }

                Console.WriteLine($"Retrieved {customerMap.Count} customer records from SQL Server");
                return customerMap;
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"SQL Error retrieving customer data: {ex.Message}");
                Console.WriteLine("Continuing without customer name mapping...");
                return customerMap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving customer data: {ex.Message}");
                Console.WriteLine("Continuing without customer name mapping...");
                return customerMap;
            }
        }

        /// <summary>
        /// Stores POS data in MongoDB
        /// </summary>
        /// <param name="posRecords">List of POS data to store</param>
        /// <param name="clientToken">Client token identifier</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StorePosDataAsync(List<BrDataPos> posRecords, string clientToken)
        {
            try
            {
                if (posRecords == null || posRecords.Count == 0)
                {
                    Console.WriteLine("No BrData POS data to store");
                    return 0;
                }

                Console.WriteLine($"Storing {posRecords.Count} BrData POS records in MongoDB...");

                // Get customer data from SQL Server for store name mapping
                var customerMap = await GetCustomerDataAsync();

                var currentDate = DateTime.UtcNow;  

                var documents = new List<BrDataPosDocument>();
                foreach (var pos in posRecords)
                {
                    // Look up customer name by store number (CustomerNumber)
                    string storeName = null;
                    if (!string.IsNullOrEmpty(pos.Store) && customerMap.ContainsKey(pos.Store))
                    {
                        storeName = customerMap[pos.Store];
                    }

                    documents.Add(new BrDataPosDocument
                    {
                        client_token = clientToken,
                        store_number = pos.Store,
                        store_name = storeName,                       
                        date = pos.Date,                      
                        qty_sold = pos.QtySold,
                        amount_sold = pos.AmountSold,                       
                        department = pos.Department,
                        brand = pos.Brand,
                        upc = pos.UPC,
                        central_description = pos.Central_Description,                       
                        last_updated = pos.LastUpdated,  
                        row_count = 1,
                        file_name = pos.SourceFileName,
                        importedAt = currentDate
                    });
                }

                await _posCollection.InsertManyAsync(documents);

                Console.WriteLine($"Successfully stored {posRecords.Count} BrData POS records");
                return posRecords.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing BrData POS data: {ex.Message}");
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
                Console.WriteLine("Testing MongoDB connection for BrData service...");

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
                Console.WriteLine("Ensuring BrData MongoDB collections exist...");

                var collections = await _database.ListCollectionNamesAsync();
                var collectionNames = await collections.ToListAsync();

                // Check if POS collection exists, create if not
                if (!collectionNames.Contains("brdata_pos"))
                {
                    await _database.CreateCollectionAsync("brdata_pos");
                    Console.WriteLine("Created collection: brdata_pos");
                }
                else
                {
                    Console.WriteLine("Collection already exists: brdata_pos");
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
                Console.WriteLine("Creating MongoDB indexes for BrData collections...");

                // Create index on Store for POS collection
                var storeIndex = Builders<BrDataPosDocument>.IndexKeys.Ascending(x => x.store_number);
                await _posCollection.Indexes.CreateOneAsync(new CreateIndexModel<BrDataPosDocument>(storeIndex));
                Console.WriteLine("Created index on Store for brdata_pos collection");

                // Create index on Date for POS collection
                var dateIndex = Builders<BrDataPosDocument>.IndexKeys.Ascending(x => x.date);
                await _posCollection.Indexes.CreateOneAsync(new CreateIndexModel<BrDataPosDocument>(dateIndex));
                Console.WriteLine("Created index on Date for brdata_pos collection");

                // Create index on UPC for POS collection
                var upcIndex = Builders<BrDataPosDocument>.IndexKeys.Ascending(x => x.upc);
                await _posCollection.Indexes.CreateOneAsync(new CreateIndexModel<BrDataPosDocument>(upcIndex));
                Console.WriteLine("Created index on UPC for brdata_pos collection");
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
                var posCount = (int)await _posCollection.CountDocumentsAsync(_ => true);
                return posCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting collection counts: {ex.Message}");
                return 0;
            }
        }
    }
}

