using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents ShopToCook email data from CSV file
    /// </summary>
    public class ShopToCookEmail
    {
        //public string ClientToken { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
        public int Sent { get; set; }
        public int Opened { get; set; }
        public string SourceFileName { get; set; }
    }
}
