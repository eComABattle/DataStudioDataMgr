using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace DataStudioDataMgr.Services.Coupon
{
    /// <summary>
    /// MongoDB document for a single coupon record in digital_coupon_analytics.coupon_api.
    /// Schema: client_token, post_metric_date, coupon_id, brand, clip_count, redemption_count,
    /// description, offer_language_offer, redemption_value.
    /// </summary>
    public class CouponApiDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("client_token")]
        public string client_token { get; set; } = "AWG";

        [BsonElement("post_metric_date")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime post_metric_date { get; set; }

        [BsonElement("coupon_id")]
        public int coupon_id { get; set; }

        [BsonElement("brand")]
        public string brand { get; set; }

        [BsonElement("clip_count")]
        public int clip_count { get; set; }

        [BsonElement("redemption_count")]
        public int redemption_count { get; set; }

        [BsonElement("description")]
        public string description { get; set; }

        [BsonElement("offer_language_offer")]
        public string offer_language_offer { get; set; }

        [BsonElement("redemption_value")]
        public double redemption_value { get; set; }
    }

    /// <summary>
    /// Stores Coupon API (clip redemption report) results in MongoDB.
    /// Each record from the API result is stored as a separate document.
    /// Database: digital_coupon_analytics, Collection: coupon_api.
    /// </summary>
    public class CouponApiMongoService
    {
        private const string DatabaseName = "digital_coupon_analytics";
        private const string CollectionName = "coupon_api";
        private readonly IMongoCollection<CouponApiDocument> _collection;

        public CouponApiMongoService()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDbConnectionString"]?.ConnectionString;
            if (!string.IsNullOrEmpty(connectionString))
                connectionString = System.Environment.ExpandEnvironmentVariables(connectionString);
            if (string.IsNullOrEmpty(connectionString))
                connectionString = "mongodb://localhost:27017";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(DatabaseName);
            _collection = database.GetCollection<CouponApiDocument>(CollectionName);
        }

        public CouponApiMongoService(string connectionString, string databaseName = null, string collectionName = null)
        {
            if (!string.IsNullOrEmpty(connectionString))
                connectionString = System.Environment.ExpandEnvironmentVariables(connectionString);
            var client = new MongoClient(connectionString ?? "mongodb://localhost:27017");
            var db = client.GetDatabase(databaseName ?? DatabaseName);
            _collection = db.GetCollection<CouponApiDocument>(collectionName ?? CollectionName);
        }

        /// <summary>
        /// Stores each record from the Coupon API result as a separate document in digital_coupon_analytics.coupon_api.
        /// </summary>
        /// <param name="apiResult">The result from the Coupon API (GetCouponRedemptionReportAsync).</param>
        /// <returns>Number of documents inserted.</returns>
        public async Task<int> StoreCouponApiResultAsync(CouponApi.RootObject apiResult)
        {
            if (apiResult?.data == null || apiResult.data.Count == 0)
                return 0;

            var postMetricDate = DateTime.UtcNow;
            var documents = apiResult.data.Select(c => new CouponApiDocument
            {
                client_token = "AWG",
                post_metric_date = postMetricDate,
                coupon_id = c.couponId,
                brand = c.brand,
                clip_count = c.clipCount,
                redemption_count = c.redemptionCount,
                description = c.description,
                offer_language_offer = c.offerLanguageOffer,
                redemption_value = c.redemptionValue
            }).ToList();

            await _collection.InsertManyAsync(documents).ConfigureAwait(false);
            return documents.Count;
        }
    }
}
