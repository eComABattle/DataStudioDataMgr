using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents UNFI Customer data from the AdStudioUnfi SQL Server database
    /// </summary>
    public class UnfiCustomer
    {
        public string ClientToken { get; set; }
        public int CustomerId { get; set; }
        public DateTime? ActiveThroughDate { get; set; }
        public int AdminLevel { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedDate { get; set; }
        public string CustomerName { get; set; }
        public string CustomerNumber { get; set; }
        public int? CustomerServiceRepId { get; set; }
        public string Email { get; set; }
        public string Fax { get; set; }
        public bool IsActive { get; set; }
        public bool IsInternal { get; set; }
        public string LastLoginDate { get; set; }
        public int ModifiedBy { get; set; }
        public string ModifiedDate { get; set; }
        public string Phone { get; set; }
        public string PreferredLanguageToken { get; set; }
        public int? SalesRepId { get; set; }
        public string WebUrl { get; set; }
        public string CustomerGuid { get; set; }
        public bool CanPreMerchandise { get; set; }
        public string Notes { get; set; }
    }
}
