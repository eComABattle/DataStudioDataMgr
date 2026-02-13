using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents AppCard data from CSV file
    /// </summary>
    public class AppCardData
    {
        public DateTime TransactionDate { get; set; }
        public int TotalClipCount { get; set; }
        public int TotalRedemptionsCount { get; set; }
        public string MerchantName { get; set; }
        public string ContentProviderName { get; set; }
        public string DigitalOfferId { get; set; }
        public decimal DiscountAmount { get; set; }
        public int NumberOfUniqueRedeemers { get; set; }
        public string SourceFileName { get; set; }
    }
}

