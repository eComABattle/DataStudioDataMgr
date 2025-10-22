using System;
using CsvHelper.Configuration.Attributes;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents ShopToCook kiosk data from CSV file
    /// </summary>
    public class ShopToCookKiosk
    {
        //public string ClientToken { get; set; }
        public string Feature { get; set; }
        public DateTime Date { get; set; }

        [Name("Feature Count")]
        public int FeatureCount { get; set; }
        public string SourceFileName { get; set; }
    }
}
