using System;

namespace DataStudioDataMgr.Models.Brick
{
    /// <summary>
    /// Represents Brick API request parameters
    /// </summary>
    public class BrickRequest
    {
        public string Type { get; set; } = "JSON"; // XML or JSON
        public string Data { get; set; } = string.Empty;
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public int CampaignId { get; set; } = 4; // Default campaign ID from sample
    }

    /// <summary>
    /// Represents Brick API response data
    /// </summary>
    public class BrickResponse
    {
        public bool Success { get; set; }
        public string Data { get; set; }
        public string ContentType { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents Brick statistics data structure
    /// </summary>
    public class BrickStatistics
    {
        public DateTime Date { get; set; }
        public int CampaignId { get; set; }
        public string CampaignName { get; set; }
        public int Impressions { get; set; }
        public int Clicks { get; set; }
        public decimal Revenue { get; set; }
        public decimal CPM { get; set; }
        public decimal CPC { get; set; }
        public decimal CTR { get; set; }
        public string SourceFileName { get; set; }
    }

    /// <summary>
    /// Represents Brick configuration
    /// </summary>
    public class BrickConfiguration
    {
        public string BaseUrl { get; set; } = "https://serve.withbrick.com/api/v1";
        public string Username { get; set; }
        public string Password { get; set; }
        public int DefaultCampaignId { get; set; } = 1;
        public string DefaultDataType { get; set; } = "JSON";
        public int TimeoutSeconds { get; set; } = 30;
        public bool EnableLogging { get; set; } = true;
    }
}

