using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents GiveX coupon data from CSV file
    /// </summary>
    public class GiveXCoupon
    {
        public string TenantId { get; set; }
        public string CardNumber { get; set; }
        public string AccountId { get; set; }
        public string customerName { get; set; }
        public string OfferID { get; set; }
        public string SourceID { get; set; }
        public string CouponSource { get; set; }
        public string Brand { get; set; }
        public string CouponDetail { get; set; }
        public int? Clipped { get; set; }
        public int? Redeemed { get; set; }
        public decimal? TotalSpent { get; set; }
        public decimal? CouponValue { get; set; }
        public DateTime? UsedDate { get; set; }
        public DateTime? DateClipped { get; set; }
        public string HomeStoreNumber { get; set; }
        public string HomeStoreName { get; set; }
        public string StoreNumber { get; set; }
        public string StoreName { get; set; }
        public string UPC { get; set; }
        public decimal? PointCost { get; set; }
        public decimal? EnteredCouponValue { get; set; }
        public string PivotStore { get; set; }
        public string GlobalID { get; set; }
        public string SourceFileName { get; set; }
    }
}


