using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents UNFI Post Campaign Store data from the MediaStudio SQL Server database
    /// </summary>
    public class UnfiPostCampaignStore
    {
        public string ClientToken { get; set; }
        public int Id { get; set; }
        public int GroupCampaignId { get; set; }
        public string CustomerGuid { get; set; }
        public string ExternalId { get; set; }
        public string SecondaryExternalId { get; set; }
        public string Url { get; set; }
    }
}
