using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// GiveX AWG aggregated email stats row (CSV) plus load metadata.
    /// CSV headers: TenantId, Email Subject, CampaignId, Date Sent, CampaignName, Sent, Delivered, Opened,
    /// Total Clicks, Unique User Clicks, Hard Bounces, Soft Bounces, Spam Complaint, Unsubscribed.
    /// </summary>
    public class AwgEmailStats
    {
        public const string CuratedSource = "GiveX Curated Emails";
        public const string AwgClientToken = "AWG";

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

        public DateTime CreatedAt { get; set; }
        public string Source { get; set; }
        public string ClientToken { get; set; }
        public string SourceFileName { get; set; }
    }
}
