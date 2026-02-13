using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents BrData POS (Point of Sales) data from CSV file
    /// </summary>
    public class BrDataPos
    {
        public string Store { get; set; }
        public string UPC { get; set; }
        public DateTime? Date { get; set; }
        public string Priority { get; set; }
        public decimal? QtySold { get; set; }
        public decimal? AmountSold { get; set; }
        public decimal? WgtSold { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? NetUnitCost { get; set; }
        public decimal? PrcQty { get; set; }
        public decimal? Price { get; set; }
        public string PriceType { get; set; }
        public string CouponType { get; set; }
        public decimal? CouponBValue { get; set; }
        public string Department { get; set; }
        public string Brand { get; set; }
        public string Central_Description { get; set; }
        public string CERTCO_Description { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string UnitofMeasure { get; set; }
        public string SourceFileName { get; set; }
    }
}


