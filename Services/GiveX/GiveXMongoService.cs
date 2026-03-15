using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.GiveX
{
    /// <summary>
    /// MongoDB document classes for GiveX data
    /// </summary>
    public class GiveXEmailDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string client_token { get; set; }
        public string tenant_id { get; set; }
        public string email_subject { get; set; }
        public string campaign_id { get; set; }
        public DateTime date_sent { get; set; }
        public string campaign_name { get; set; }
        public int sent { get; set; }
        public int delivered { get; set; }
        public int opened { get; set; }
        public int total_clicks { get; set; }
        public int unique_user_clicks { get; set; }
        public int hard_bounces { get; set; }
        public int soft_bounces { get; set; }
        public int spam_complaint { get; set; }
        public int unsubscribed { get; set; }
        public string file_name { get; set; }
        public DateTime createdAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// MongoDB document class for GiveX Loyalty data
    /// </summary>
    public class GiveXLoyaltyDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string client_token { get; set; }
        public string contact { get; set; }
        public string account { get; set; }
        public string shopper_id { get; set; }
        public string store { get; set; }
        public decimal? current_balance { get; set; }
        public DateTime? enrollment_date { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string street_address1 { get; set; }
        public string street_address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip_code { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string user { get; set; }
        public string store_number { get; set; }
        public DateTime? birth_date { get; set; }
        public string shopper_level { get; set; }
        public DateTime? last_shop_date { get; set; }
        public string file_name { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// MongoDB document class for GiveX Coupon data
    /// </summary>
    public class GiveXCouponDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string client_token { get; set; }
        public string tenant_id { get; set; }
        public string card_number { get; set; }
        public string account_id { get; set; }
        public string customer_name { get; set; }
        public string offer_id { get; set; }
        public string source_id { get; set; }
        public string coupon_source { get; set; }
        public string brand { get; set; }
        public string coupon_detail { get; set; }
        public int? clipped { get; set; }
        public int? redeemed { get; set; }
        public decimal? total_spent { get; set; }
        public decimal? coupon_value { get; set; }
        public DateTime? used_date { get; set; }
        public DateTime? date_clipped { get; set; }
        public string home_store_number { get; set; }
        public string home_store_name { get; set; }
        public string store_number { get; set; }
        public string store_name { get; set; }
        public string upc { get; set; }
        public decimal? point_cost { get; set; }
        public decimal? entered_coupon_value { get; set; }
        public string pivot_store { get; set; }
        public string global_id { get; set; }
        public string file_name { get; set; }
        public DateTime importedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Service for storing GiveX data in MongoDB
    /// </summary>
    public class GiveXMongoService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<GiveXEmailDocument> _emailCollection;
        private readonly IMongoCollection<GiveXLoyaltyDocument> _loyaltyCollection;
        private readonly IMongoCollection<GiveXCouponDocument> _couponCollection;

        public GiveXMongoService()
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
            //databaseName = "integration_test";

            var emailDatabaseName = "ad_campaign";
            var loyaltyDatabaseName = "loyalty_analytics";
            var couponDatabaseName = "digital_coupon_analytics";

            // Expand environment variables in database name
            //databaseName = Environment.ExpandEnvironmentVariables(databaseName);

            Console.WriteLine($"Using MongoDB database for GiveX: {databaseName}");

            Console.WriteLine($"Connecting to MongoDB...");
            var client = new MongoClient(connectionString);
            var _emailDatabase = client.GetDatabase(emailDatabaseName);
            var _loyaltyDatabase = client.GetDatabase(loyaltyDatabaseName);
            var _couponDatabase = client.GetDatabase(couponDatabaseName);

            _emailCollection = _emailDatabase.GetCollection<GiveXEmailDocument>("email");
            _loyaltyCollection = _loyaltyDatabase.GetCollection<GiveXLoyaltyDocument>("events");
            _couponCollection = _couponDatabase.GetCollection<GiveXCouponDocument>("events");
        }

        //public GiveXMongoService(string connectionString, string databaseName = null)
        //{
        //    // Expand environment variables in connection string
        //    connectionString = Environment.ExpandEnvironmentVariables(connectionString);

        //    var client = new MongoClient(connectionString);

        //    // Use provided databaseName or fall back to default
        //    if (string.IsNullOrEmpty(databaseName))
        //    {
        //        databaseName = System.Configuration.ConfigurationManager.AppSettings["MongoDbDatabaseName"] ?? "integration_test";
        //    }

        //    // Expand environment variables in database name
        //    //databaseName = Environment.ExpandEnvironmentVariables(databaseName);
        //    databaseName = "integration_test";

        //    _database = client.GetDatabase(databaseName);

        //    _emailCollection = _database.GetCollection<GiveXEmailDocument>("givex_email");
        //    _loyaltyCollection = _database.GetCollection<GiveXLoyaltyDocument>("givex_loyalty");
        //    _couponCollection = _database.GetCollection<GiveXCouponDocument>("givex_coupon");
        //}

        /// <summary>
        /// Stores email data in MongoDB
        /// </summary>
        /// <param name="emails">List of email data to store</param>
        /// <param name="clientToken">Client token identifier</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreEmailDataAsync(List<GiveXEmail> emails, string clientToken)
        {
            try
            {
                if (emails == null || emails.Count == 0)
                {
                    Console.WriteLine("No GiveX email data to store");
                    return 0;
                }

                Console.WriteLine($"Storing {emails.Count} GiveX email records in MongoDB...");

                var currentDateTime = DateTime.UtcNow;
                var documents = new List<GiveXEmailDocument>();
                foreach (var email in emails)
                {
                    documents.Add(new GiveXEmailDocument
                    {
                        client_token = clientToken,
                        tenant_id = email.TenantId.ToString(),
                        email_subject = email.EmailSubject,
                        campaign_id = email.CampaignId,
                        date_sent = email.DateSent,
                        campaign_name = email.CampaignName,
                        sent = email.Sent,
                        delivered = email.Delivered,
                        opened = email.Opened,
                        total_clicks = email.TotalClicks,
                        unique_user_clicks = email.UniqueUserClicks,
                        hard_bounces = email.HardBounces,
                        soft_bounces = email.SoftBounces,
                        spam_complaint = email.SpamComplaint,
                        unsubscribed = email.Unsubscribed,
                        file_name = email.SourceFileName,
                        createdAt = currentDateTime
                    });
                }

                await _emailCollection.InsertManyAsync(documents);

                Console.WriteLine($"Successfully stored {emails.Count} GiveX email records");
                return emails.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing GiveX email data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stores loyalty data in MongoDB
        /// </summary>
        /// <param name="loyaltyRecords">List of loyalty data to store</param>
        /// <param name="clientToken">Client token identifier</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreLoyaltyDataAsync(List<GiveXLoyalty> loyaltyRecords, string clientToken)
        {
            try
            {
                if (loyaltyRecords == null || loyaltyRecords.Count == 0)
                {
                    Console.WriteLine("No GiveX loyalty data to store");
                    return 0;
                }

                Console.WriteLine($"Storing {loyaltyRecords.Count} GiveX loyalty records in MongoDB...");

                var documents = new List<GiveXLoyaltyDocument>();
                foreach (var loyalty in loyaltyRecords)
                {
                    documents.Add(new GiveXLoyaltyDocument
                    {
                        client_token = clientToken,
                        contact = loyalty.Contact,
                        account = loyalty.Account,
                        shopper_id = loyalty.ShopperID,
                        store = loyalty.Store,
                        current_balance = loyalty.CurrentBalance,
                        enrollment_date = loyalty.EnrollmentDate,
                        first_name = loyalty.FirstName,
                        last_name = loyalty.LastName,
                        street_address1 = loyalty.StreetAddress1,
                        street_address2 = loyalty.StreetAddress2,
                        city = loyalty.City,
                        state = loyalty.State,
                        zip_code = loyalty.ZipCode,
                        phone = loyalty.Phone,
                        email = loyalty.Email,
                        user = loyalty.User,
                        store_number = loyalty.StoreNumber,
                        birth_date = loyalty.BirthDate,
                        shopper_level = loyalty.ShopperLevel,
                        last_shop_date = loyalty.LastShopDate,
                        file_name = loyalty.SourceFileName,
                        ImportedAt = DateTime.UtcNow
                    });
                }

                await _loyaltyCollection.InsertManyAsync(documents);

                Console.WriteLine($"Successfully stored {loyaltyRecords.Count} GiveX loyalty records");
                return loyaltyRecords.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing GiveX loyalty data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stores coupon data in MongoDB
        /// </summary>
        /// <param name="couponRecords">List of coupon data to store</param>
        /// <param name="clientToken">Client token identifier</param>
        /// <returns>Number of records stored</returns>
        public async Task<int> StoreCouponDataAsync(List<GiveXCoupon> couponRecords, string clientToken)
        {
            try
            {
                if (couponRecords == null || couponRecords.Count == 0)
                {
                    Console.WriteLine("No GiveX coupon data to store");
                    return 0;
                }

                Console.WriteLine($"Storing {couponRecords.Count} GiveX coupon records in MongoDB...");

                var documents = new List<GiveXCouponDocument>();
                foreach (var coupon in couponRecords)
                {
                    documents.Add(new GiveXCouponDocument
                    {
                        client_token = clientToken,
                        tenant_id = coupon.TenantId,
                        card_number = coupon.CardNumber,
                        account_id = coupon.AccountId,
                        customer_name = coupon.customerName,
                        offer_id = coupon.OfferID,
                        source_id = coupon.SourceID,
                        coupon_source = coupon.CouponSource,
                        brand = coupon.Brand,
                        coupon_detail = coupon.CouponDetail,
                        clipped = coupon.Clipped,
                        redeemed = coupon.Redeemed,
                        total_spent = coupon.TotalSpent,
                        coupon_value = coupon.CouponValue,
                        used_date = coupon.UsedDate,
                        date_clipped = coupon.DateClipped,
                        home_store_number = coupon.HomeStoreNumber,
                        home_store_name = coupon.HomeStoreName,
                        store_number = coupon.StoreNumber,
                        store_name = coupon.StoreName,
                        upc = coupon.UPC,
                        point_cost = coupon.PointCost,
                        entered_coupon_value = coupon.EnteredCouponValue,
                        pivot_store = coupon.PivotStore,
                        global_id = coupon.GlobalID,
                        file_name = coupon.SourceFileName,
                        importedAt = DateTime.UtcNow
                    });
                }

                await _couponCollection.InsertManyAsync(documents);

                Console.WriteLine($"Successfully stored {couponRecords.Count} GiveX coupon records");
                return couponRecords.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing GiveX coupon data: {ex.Message}");
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
                Console.WriteLine("Testing MongoDB connection for GiveX service...");

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
                Console.WriteLine("Ensuring GiveX MongoDB collections exist...");

                var collections = await _database.ListCollectionNamesAsync();
                var collectionNames = await collections.ToListAsync();

                // Check if email collection exists, create if not
                if (!collectionNames.Contains("givex_email"))
                {
                    await _database.CreateCollectionAsync("givex_email");
                    Console.WriteLine("Created collection: givex_email");
                }
                else
                {
                    Console.WriteLine("Collection already exists: givex_email");
                }

                // Check if loyalty collection exists, create if not
                if (!collectionNames.Contains("givex_loyalty"))
                {
                    await _database.CreateCollectionAsync("givex_loyalty");
                    Console.WriteLine("Created collection: givex_loyalty");
                }
                else
                {
                    Console.WriteLine("Collection already exists: givex_loyalty");
                }

                // Check if coupon collection exists, create if not
                if (!collectionNames.Contains("givex_coupon"))
                {
                    await _database.CreateCollectionAsync("givex_coupon");
                    Console.WriteLine("Created collection: givex_coupon");
                }
                else
                {
                    Console.WriteLine("Collection already exists: givex_coupon");
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
                Console.WriteLine("Creating MongoDB indexes for GiveX collections...");

                // Create indexes for email collection
                var dateSentIndex = Builders<GiveXEmailDocument>.IndexKeys.Ascending(x => x.date_sent);
                await _emailCollection.Indexes.CreateOneAsync(new CreateIndexModel<GiveXEmailDocument>(dateSentIndex));
                Console.WriteLine("Created index on DateSent for givex_email collection");

                var campaignIdIndex = Builders<GiveXEmailDocument>.IndexKeys.Ascending(x => x.campaign_id);
                await _emailCollection.Indexes.CreateOneAsync(new CreateIndexModel<GiveXEmailDocument>(campaignIdIndex));
                Console.WriteLine("Created index on CampaignId for givex_email collection");

                // Create indexes for loyalty collection
                var shopperIdIndex = Builders<GiveXLoyaltyDocument>.IndexKeys.Ascending(x => x.shopper_id);
                await _loyaltyCollection.Indexes.CreateOneAsync(new CreateIndexModel<GiveXLoyaltyDocument>(shopperIdIndex));
                Console.WriteLine("Created index on ShopperID for givex_loyalty collection");

                var enrollmentDateIndex = Builders<GiveXLoyaltyDocument>.IndexKeys.Ascending(x => x.enrollment_date);
                await _loyaltyCollection.Indexes.CreateOneAsync(new CreateIndexModel<GiveXLoyaltyDocument>(enrollmentDateIndex));
                Console.WriteLine("Created index on EnrollmentDate for givex_loyalty collection");

                // Create indexes for coupon collection
                var tenantIdIndex = Builders<GiveXCouponDocument>.IndexKeys.Ascending(x => x.tenant_id);
                await _couponCollection.Indexes.CreateOneAsync(new CreateIndexModel<GiveXCouponDocument>(tenantIdIndex));
                Console.WriteLine("Created index on TenantId for givex_coupon collection");

                var cardNumberIndex = Builders<GiveXCouponDocument>.IndexKeys.Ascending(x => x.card_number);
                await _couponCollection.Indexes.CreateOneAsync(new CreateIndexModel<GiveXCouponDocument>(cardNumberIndex));
                Console.WriteLine("Created index on CardNumber for givex_coupon collection");

                var usedDateIndex = Builders<GiveXCouponDocument>.IndexKeys.Ascending(x => x.used_date);
                await _couponCollection.Indexes.CreateOneAsync(new CreateIndexModel<GiveXCouponDocument>(usedDateIndex));
                Console.WriteLine("Created index on UsedDate for givex_coupon collection");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating indexes: {ex.Message}");
                // Don't throw - indexes are optional
            }
        }

        /// <summary>
        /// Gets collection counts for email collection
        /// </summary>
        public async Task<int> GetCollectionCountsAsync()
        {
            try
            {
                var emailCount = (int)await _emailCollection.CountDocumentsAsync(_ => true);
                return emailCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting collection counts: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets collection counts for all GiveX collections
        /// </summary>
        public async Task<(int emailCount, int loyaltyCount, int couponCount)> GetAllCollectionCountsAsync()
        {
            try
            {
                var emailCount = (int)await _emailCollection.CountDocumentsAsync(_ => true);
                var loyaltyCount = (int)await _loyaltyCollection.CountDocumentsAsync(_ => true);
                var couponCount = (int)await _couponCollection.CountDocumentsAsync(_ => true);
                return (emailCount, loyaltyCount, couponCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting collection counts: {ex.Message}");
                return (0, 0, 0);
            }
        }
    }
}

