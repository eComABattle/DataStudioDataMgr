using System;

namespace DataStudioDataMgr.Models
{
    /// <summary>
    /// Represents GiveX loyalty/enrollment data from CSV file
    /// </summary>
    public class GiveXLoyalty
    {
        public string Contact { get; set; }
        public string Account { get; set; }
        public string ShopperID { get; set; }
        public string Store { get; set; }
        public decimal? CurrentBalance { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StreetAddress1 { get; set; }
        public string StreetAddress2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string User { get; set; }
        public string StoreNumber { get; set; }
        public DateTime? BirthDate { get; set; }
        public string ShopperLevel { get; set; }
        public DateTime? LastShopDate { get; set; }
        public string SourceFileName { get; set; }
    }
}


