using System;

namespace DataStudioDataMgr
{
    /// <summary>
    /// Represents an Ad Campaign from the SQL Server database
    /// </summary>
    public class Campaign
    {
        public int Id { get; set; }
        public int BrandId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
}
