using System;
using System.Collections.Generic;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents a Brick Campaign from the Brick API
    /// </summary>
    public class BrickCampaign
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? AdvertiserId { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Metrics
        public int? Impressions { get; set; }
        public int? Clicks { get; set; }
        
        // Additional properties that may exist in the API response
        public Dictionary<string, object> AdditionalProperties { get; set; }
    }
}

