using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.GiveX
{
    /// <summary>
    /// CSV mapping configurations for GiveX data
    /// </summary>
    public class GiveXEmailMap : ClassMap<GiveXEmail>
    {
        public GiveXEmailMap()
        {
            Map(m => m.TenantId).Name("TenantId");
            Map(m => m.EmailSubject).Name("Email Subject");
            Map(m => m.CampaignId).Name("CampaignId");
            Map(m => m.DateSent).Name("Date Sent");
            Map(m => m.CampaignName).Name("CampaignName");
            Map(m => m.Sent).Name("Sent");
            Map(m => m.Delivered).Name("Delivered");
            Map(m => m.Opened).Name("Opened");
            Map(m => m.TotalClicks).Name("Total Clicks");
            Map(m => m.UniqueUserClicks).Name("Unique User Clicks");
            Map(m => m.HardBounces).Name("Hard Bounces");
            Map(m => m.SoftBounces).Name("Soft Bounces");
            Map(m => m.SpamComplaint).Name("Spam Complaint");
            Map(m => m.Unsubscribed).Name("Unsubscribed");
            // SourceFileName is set programmatically, not from CSV
            Map(m => m.SourceFileName).Ignore();
        }
    }

    /// <summary>
    /// CSV mapping configuration for GiveX Loyalty data
    /// </summary>
    public class GiveXLoyaltyMap : ClassMap<GiveXLoyalty>
    {
        public GiveXLoyaltyMap()
        {
            Map(m => m.Contact).Name("Contact");
            Map(m => m.Account).Name("Account");
            Map(m => m.ShopperID).Name("ShopperID");
            Map(m => m.Store).Name("Store");
            Map(m => m.CurrentBalance).Name("CurrentBalance");
            Map(m => m.EnrollmentDate).Name("EnrollmentDate");
            Map(m => m.FirstName).Name("FirstName");
            Map(m => m.LastName).Name("LastName");
            Map(m => m.StreetAddress1).Name("StreetAddress1");
            Map(m => m.StreetAddress2).Name("StreetAddress2");
            Map(m => m.City).Name("City");
            Map(m => m.State).Name("State");
            Map(m => m.ZipCode).Name("ZipCode");
            Map(m => m.Phone).Name("Phone");
            Map(m => m.Email).Name("Email");
            Map(m => m.User).Name("User");
            Map(m => m.StoreNumber).Name("StoreNumber");
            Map(m => m.BirthDate).Name("BirthDate");
            Map(m => m.ShopperLevel).Name("ShopperLevel");
            Map(m => m.LastShopDate).Name("LastShopDate");
            // SourceFileName is set programmatically, not from CSV
            Map(m => m.SourceFileName).Ignore();
        }
    }

    /// <summary>
    /// CSV mapping configuration for GiveX Coupon data
    /// </summary>
    public class GiveXCouponMap : ClassMap<GiveXCoupon>
    {
        public GiveXCouponMap()
        {
            Map(m => m.TenantId).Name("TenantId");
            Map(m => m.CardNumber).Name("CardNumber");
            Map(m => m.AccountId).Name("AccountId");
            Map(m => m.customerName).Name("customerName");
            Map(m => m.OfferID).Name("OfferID");
            Map(m => m.SourceID).Name("SourceID");
            Map(m => m.CouponSource).Name("CouponSource");
            Map(m => m.Brand).Name("Brand");
            Map(m => m.CouponDetail).Name("CouponDetail");
            Map(m => m.Clipped).Name("Clipped");
            Map(m => m.Redeemed).Name("Redeemed");
            Map(m => m.TotalSpent).Name("TotalSpent");
            Map(m => m.CouponValue).Name("CouponValue");
            Map(m => m.UsedDate).Name("UsedDate");
            Map(m => m.DateClipped).Name("DateClipped");
            Map(m => m.HomeStoreNumber).Name("HomeStoreNumber");
            Map(m => m.HomeStoreName).Name("HomeStoreName");
            Map(m => m.StoreNumber).Name("StoreNumber");
            Map(m => m.StoreName).Name("StoreName");
            Map(m => m.UPC).Name("UPC");
            Map(m => m.PointCost).Name("PointCost");
            Map(m => m.EnteredCouponValue).Name("EnteredCouponValue");
            Map(m => m.PivotStore).Name("PivotStore");
            Map(m => m.GlobalID).Name("GlobalID");
            // SourceFileName is set programmatically, not from CSV
            Map(m => m.SourceFileName).Ignore();
        }
    }

    /// <summary>
    /// Service for processing GiveX CSV files using CsvHelper
    /// </summary>
    public class GiveXCsvService
    {
        /// <summary>
        /// Processes email CSV file and returns list of GiveXEmail objects
        /// </summary>
        /// <param name="csvFilePath">Path to the email CSV file or directory</param>
        /// <returns>List of GiveXEmail objects</returns>
        public async Task<List<GiveXEmail>> ProcessEmailCsvAsync(string csvFilePath)
        {
            var emails = new List<GiveXEmail>();

            try
            {
                Console.WriteLine($"Processing GiveX email CSV files from: {csvFilePath}");

                List<string> emailFiles;

                // Check if path is a directory or a file
                if (Directory.Exists(csvFilePath))
                {
                    // Filter for email-related CSV files (contains "Email" for Certco or "AWG" for AWG)
                    emailFiles = Directory.EnumerateFiles(csvFilePath, "*.csv")
                        .Where(f => 
                        {
                            var fileName = Path.GetFileName(f).ToLower();
                            return fileName.Contains("email") || fileName.Contains("awg");
                        })
                        .ToList();
                }
                else if (File.Exists(csvFilePath))
                {
                    // Single file
                    emailFiles = new List<string> { csvFilePath };
                }
                else
                {
                    Console.WriteLine($"Path does not exist: {csvFilePath}");
                    return emails;
                }

                if (!emailFiles.Any())
                {
                    Console.WriteLine("No email CSV files found");
                    return emails;
                }

                Console.WriteLine($"Found {emailFiles.Count} email CSV files to process");

                foreach (string file in emailFiles)
                {
                    var fileName = Path.GetFileName(file);

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    };

                    using (var reader = new StreamReader(file))
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Context.RegisterClassMap<GiveXEmailMap>();

                        // Configure multiple date formats globally for this CSV reader
                        // Include formats with AM/PM for date fields
                        var dateOptions = new TypeConverterOptions
                        {
                            Formats = new[] { 
                                "M/d/yyyy", 
                                "yyyy-MM-dd", 
                                "MM/dd/yyyy", 
                                "M/dd/yyyy", 
                                "MM/d/yyyy", 
                                "yyyy/MM/dd", 
                                "M/d/yyyy HH:mm:ss", 
                                "yyyy-MM-dd HH:mm:ss",
                                "M/d/yyyy h:mm:ss tt",
                                "MM/dd/yyyy h:mm:ss tt",
                                "M/dd/yyyy h:mm:ss tt",
                                "MM/d/yyyy h:mm:ss tt",
                                "M/d/yyyy h:mm tt",
                                "MM/dd/yyyy h:mm tt"
                            }
                        };
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dateOptions);

                        var records = await Task.Run(() => csv.GetRecords<GiveXEmail>().ToList());
                        foreach (var record in records)
                        {
                            record.EmailSubject = record.EmailSubject?.Replace(",", " ");
                            record.CampaignName = record.CampaignName?.Replace(",", " ");
                            record.SourceFileName = fileName;
                            emails.Add(record);
                        }

                        Console.WriteLine($"Successfully processed {records.Count} email records from file: {fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing GiveX email CSV file: {ex.Message}");
                throw;
            }

            return emails;
        }

        /// <summary>
        /// Processes loyalty CSV file and returns list of GiveXLoyalty objects
        /// </summary>
        /// <param name="csvFilePath">Path to the loyalty CSV file or directory</param>
        /// <returns>List of GiveXLoyalty objects</returns>
        public async Task<List<GiveXLoyalty>> ProcessLoyaltyCsvAsync(string csvFilePath)
        {
            var loyaltyRecords = new List<GiveXLoyalty>();

            try
            {
                Console.WriteLine($"Processing GiveX loyalty CSV files from: {csvFilePath}");

                List<string> loyaltyFiles;

                // Check if path is a directory or a file
                if (Directory.Exists(csvFilePath))
                {
                    // Filter for loyalty-related CSV files (contains "Enrollment_")
                    loyaltyFiles = Directory.EnumerateFiles(csvFilePath, "*.csv")
                        .Where(f => Path.GetFileName(f).ToLower().Contains("enrollment_"))
                        .ToList();
                }
                else if (File.Exists(csvFilePath))
                {
                    // Single file
                    loyaltyFiles = new List<string> { csvFilePath };
                }
                else
                {
                    Console.WriteLine($"Path does not exist: {csvFilePath}");
                    return loyaltyRecords;
                }

                if (!loyaltyFiles.Any())
                {
                    Console.WriteLine("No loyalty CSV files found");
                    return loyaltyRecords;
                }

                Console.WriteLine($"Found {loyaltyFiles.Count} loyalty CSV files to process");

                foreach (string file in loyaltyFiles)
                {
                    var fileName = Path.GetFileName(file);

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    };

                    using (var reader = new StreamReader(file))
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Context.RegisterClassMap<GiveXLoyaltyMap>();

                        // Configure multiple date formats globally for this CSV reader
                        // Include formats with AM/PM for date fields
                        var dateOptions = new TypeConverterOptions
                        {
                            Formats = new[] { 
                                "M/d/yyyy", 
                                "yyyy-MM-dd", 
                                "MM/dd/yyyy", 
                                "M/dd/yyyy", 
                                "MM/d/yyyy", 
                                "yyyy/MM/dd", 
                                "M/d/yyyy HH:mm:ss", 
                                "yyyy-MM-dd HH:mm:ss",
                                "M/d/yyyy h:mm:ss tt",
                                "MM/dd/yyyy h:mm:ss tt",
                                "M/dd/yyyy h:mm:ss tt",
                                "MM/d/yyyy h:mm:ss tt",
                                "M/d/yyyy h:mm tt",
                                "MM/dd/yyyy h:mm tt"
                            }
                        };
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dateOptions);
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(dateOptions);

                        var records = await Task.Run(() => csv.GetRecords<GiveXLoyalty>().ToList());
                        foreach (var record in records)
                        {
                            record.SourceFileName = fileName;
                            loyaltyRecords.Add(record);
                        }

                        Console.WriteLine($"Successfully processed {records.Count} loyalty records from file: {fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing GiveX loyalty CSV file: {ex.Message}");
                throw;
            }

            return loyaltyRecords;
        }

        /// <summary>
        /// Processes coupon CSV file and returns list of GiveXCoupon objects
        /// </summary>
        /// <param name="csvFilePath">Path to the coupon CSV file or directory</param>
        /// <returns>List of GiveXCoupon objects</returns>
        public async Task<List<GiveXCoupon>> ProcessCouponCsvAsync(string csvFilePath)
        {
            var couponRecords = new List<GiveXCoupon>();

            try
            {
                Console.WriteLine($"Processing GiveX coupon CSV files from: {csvFilePath}");

                List<string> couponFiles;

                // Check if path is a directory or a file
                if (Directory.Exists(csvFilePath))
                {
                    // Filter for coupon-related CSV files (contains "Coupons_")
                    couponFiles = Directory.EnumerateFiles(csvFilePath, "*.csv")
                        .Where(f => Path.GetFileName(f).ToLower().Contains("coupons_"))
                        .ToList();
                }
                else if (File.Exists(csvFilePath))
                {
                    // Single file
                    couponFiles = new List<string> { csvFilePath };
                }
                else
                {
                    Console.WriteLine($"Path does not exist: {csvFilePath}");
                    return couponRecords;
                }

                if (!couponFiles.Any())
                {
                    Console.WriteLine("No coupon CSV files found");
                    return couponRecords;
                }

                Console.WriteLine($"Found {couponFiles.Count} coupon CSV files to process");

                foreach (string file in couponFiles)
                {
                    var fileName = Path.GetFileName(file);

                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = true,
                        Delimiter = ",",
                        IgnoreBlankLines = true,
                        BadDataFound = null,
                        MissingFieldFound = null
                    };

                    using (var reader = new StreamReader(file))
                    using (var csv = new CsvReader(reader, config))
                    {
                        csv.Context.RegisterClassMap<GiveXCouponMap>();

                        // Configure multiple date formats globally for this CSV reader
                        // Include formats with AM/PM for DateClipped field (e.g., "12/28/2025 2:03:00 PM")
                        var dateOptions = new TypeConverterOptions
                        {
                            Formats = new[] { 
                                "M/d/yyyy", 
                                "yyyy-MM-dd", 
                                "MM/dd/yyyy", 
                                "M/dd/yyyy", 
                                "MM/d/yyyy", 
                                "yyyy/MM/dd", 
                                "M/d/yyyy HH:mm:ss", 
                                "yyyy-MM-dd HH:mm:ss",
                                "M/d/yyyy h:mm:ss tt",
                                "MM/dd/yyyy h:mm:ss tt",
                                "M/dd/yyyy h:mm:ss tt",
                                "MM/d/yyyy h:mm:ss tt",
                                "M/d/yyyy h:mm tt",
                                "MM/dd/yyyy h:mm tt"
                            }
                        };
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime>(dateOptions);
                        csv.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(dateOptions);

                        var records = await Task.Run(() => csv.GetRecords<GiveXCoupon>().ToList());
                        foreach (var record in records)
                        {
                            record.SourceFileName = fileName;
                            couponRecords.Add(record);
                        }

                        Console.WriteLine($"Successfully processed {records.Count} coupon records from file: {fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing GiveX coupon CSV file: {ex.Message}");
                throw;
            }

            return couponRecords;
        }
    }
}

