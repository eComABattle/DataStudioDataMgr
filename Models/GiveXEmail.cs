using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents GiveX email data from CSV file
    /// </summary>
    public class GiveXEmail
    {
        public string TenantId { get; set; }
        public string EmailSubject { get; set; }
        public string CampaignId { get; set; }
        public DateTime DateSent { get; set; }
        public string CampaignName { get; set; }
        public int Sent { get; set; }
        public int Delivered { get; set; }
        public int Opened { get; set; }
        public int TotalClicks { get; set; }
        public int UniqueUserClicks { get; set; }
        public int HardBounces { get; set; }
        public int SoftBounces { get; set; }
        public int SpamComplaint { get; set; }
        public int Unsubscribed { get; set; }
        public string SourceFileName { get; set; }
    }
}


