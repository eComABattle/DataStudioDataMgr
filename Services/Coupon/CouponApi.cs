using System;
using System.Collections.Generic;

namespace DataStudioDataMgr.Services.Coupon
{
    public class CouponApi
    {
        public class RootObject
        {
            public string result { get; set; }
            public int code { get; set; }
            public string message { get; set; }
            public List<Coupon> data { get; set; } // Represents the array of coupon objects
            public DateTime timestamp { get; set; }
            public int apiCode { get; set; }
        }

        public class Coupon
        {
            public int couponId { get; set; }
            public string brand { get; set; }
            public int clipCount { get; set; }
            public int redemptionCount { get; set; }
            public string description { get; set; }
            public string offerLanguageOffer { get; set; } // This represents the string containing multiple offer IDs
            public double redemptionValue { get; set; }
        }
    }
}