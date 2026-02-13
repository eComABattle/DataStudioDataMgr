using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents Campaign Store Metrics data from the DigitalStudio SQL Server database
    /// </summary>
    public class CampaignStoreMetrics
    {
        public string Platform { get; set; }
        public string MetricType { get; set; }
        public string MetricTypeId { get; set; }
        public string PostID { get; set; }
        public string PostName { get; set; }
        public string MetaData { get; set; }
        public string Url { get; set; }
        public string PostScheduledDate { get; set; }
        public string PostPublishedDate { get; set; }
        public string MetricDate { get; set; }
        public string ClientToken { get; set; }
        public string Store { get; set; }
        public int TotalMetricValue { get; set; }
        public int MetricDelta { get; set; }
    }
}

