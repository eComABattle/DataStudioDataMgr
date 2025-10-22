using System;
using System.Xml.Schema;
using CsvHelper.Configuration.Attributes;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents ShopToCook web data from CSV file
    /// </summary>
    public class ShopToCookWeb
    {
        //public string ClientToken { get; set; }
        public DateTime Date { get; set; }
        public string Order { get; set; }

        [Name("Total impressions")] 
        public int TotalImpressions { get; set; }

        [Name("Total clicks")]
        public int TotalClicks { get; set; }
        public string SourceFileName { get; set; }
    }
}
