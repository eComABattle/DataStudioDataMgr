using System;

namespace DataStudioDataMgr
{
    /// <summary>
    /// Represents Entry Metrics data from the DigitalStudio SQL Server database
    /// </summary>
    public class EntryMetrics
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string Customer { get; set; }
        public string CampaignName { get; set; }
        public int EntryId { get; set; }
        public string EntryType { get; set; }
        public string MetricType { get; set; }
        public string Timestamp { get; set; }
        public string MetaData { get; set; }
    }
}
