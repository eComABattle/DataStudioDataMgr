using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataStudioDataMgr.Services.ShopToCook;
using DataStudioDataMgr.Models;

namespace DataStudioDataMgr.Services.ShopToCook
{
    /// <summary>
    /// Test class to demonstrate CsvHelper functionality
    /// </summary>
    public class ShopToCookCsvTestService
    {
        private readonly ShopToCookCsvService _csvService;

        public ShopToCookCsvTestService()
        {
            _csvService = new ShopToCookCsvService();
        }

        /// <summary>
        /// Creates sample CSV files for testing
        /// </summary>
        public async Task CreateSampleCsvFilesAsync()
        {
            try
            {
                Console.WriteLine("Creating sample CSV files for testing...");

                // Create sample email CSV (simulating EmailData folder)
                var emailCsvPath = "EmailData_sample_email.csv";
                var emailCsvContent = @"Email,Date,Sent,Opened
test1@example.com,2024-01-01,100,25
test2@example.com,2024-01-02,150,30
test3@example.com,2024-01-03,200,45";

                await Task.Run(() => File.WriteAllText(emailCsvPath, emailCsvContent));
                Console.WriteLine($"Created sample email CSV: {emailCsvPath}");

                // Create sample web CSV (simulating WebsiteData folder)
                var webCsvPath = "WebsiteData_sample_web.csv";
                var webCsvContent = @"Date,Order,Total impressions,Total clicks
2024-01-01,1,1000,50
2024-01-02,2,1500,75
2024-01-03,3,2000,100";

                await Task.Run(() => File.WriteAllText(webCsvPath, webCsvContent));
                Console.WriteLine($"Created sample web CSV: {webCsvPath}");

                // Create sample kiosk CSV (simulating KioskData folder)
                var kioskCsvPath = "KioskData_sample_kiosk.csv";
                var kioskCsvContent = @"Feature,Date,Feature Count
Recipe Search,2024-01-01,25
Nutrition Info,2024-01-02,30
Shopping List,2024-01-03,20";

                await Task.Run(() => File.WriteAllText(kioskCsvPath, kioskCsvContent));
                Console.WriteLine($"Created sample kiosk CSV: {kioskCsvPath}");

                Console.WriteLine("Sample CSV files created successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating sample CSV files: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Tests CSV processing with sample files
        /// </summary>
        public async Task TestCsvProcessingAsync()
        {
            try
            {
                Console.WriteLine("=== TESTING CSV PROCESSING WITH CsvHelper ===");

                // Create sample files
                await CreateSampleCsvFilesAsync();

                // Test email CSV processing
                Console.WriteLine("\n--- Testing Email CSV Processing ---");
                var emailData = await _csvService.ProcessEmailCsvAsync("EmailData_sample_email.csv");
                Console.WriteLine($"Processed {emailData.Count} email records:");
                foreach (var email in emailData)
                {
                    Console.WriteLine($"  Email: {email.Email}, Date: {email.Date:yyyy-MM-dd}, Sent: {email.Sent}, Opened: {email.Opened}");
                }

                // Test web CSV processing
                Console.WriteLine("\n--- Testing Web CSV Processing ---");
                var webData = await _csvService.ProcessWebCsvAsync("WebsiteData_sample_web.csv");
                Console.WriteLine($"Processed {webData.Count} web records:");
                foreach (var web in webData)
                {
                    Console.WriteLine($"  Date: {web.Date:yyyy-MM-dd}, Order: {web.Order}, Impressions: {web.TotalImpressions}, Clicks: {web.TotalClicks}");
                }

                // Test kiosk CSV processing
                Console.WriteLine("\n--- Testing Kiosk CSV Processing ---");
                var kioskData = await _csvService.ProcessKioskCsvAsync("KioskData_sample_kiosk.csv");
                Console.WriteLine($"Processed {kioskData.Count} kiosk records:");
                foreach (var kiosk in kioskData)
                {
                    Console.WriteLine($"  Feature: {kiosk.Feature}, Date: {kiosk.Date:yyyy-MM-dd}, Count: {kiosk.FeatureCount}");
                }

                // Test file validation
                Console.WriteLine("\n--- Testing CSV Validation ---");
                var emailValid = await _csvService.ValidateCsvFileAsync("EmailData_sample_email.csv");
                var webValid = await _csvService.ValidateCsvFileAsync("WebsiteData_sample_web.csv");
                var kioskValid = await _csvService.ValidateCsvFileAsync("KioskData_sample_kiosk.csv");
                
                Console.WriteLine($"Email CSV valid: {emailValid}");
                Console.WriteLine($"Web CSV valid: {webValid}");
                Console.WriteLine($"Kiosk CSV valid: {kioskValid}");

                // Test generic processing
                Console.WriteLine("\n--- Testing Generic CSV Processing ---");
                var (emailData2, emailType) = await _csvService.ProcessCsvFileAsync("EmailData_sample_email.csv");
                var (webData2, webType) = await _csvService.ProcessCsvFileAsync("WebsiteData_sample_web.csv");
                var (kioskData2, kioskType) = await _csvService.ProcessCsvFileAsync("KioskData_sample_kiosk.csv");
                
                Console.WriteLine($"Email file type: {emailType}, Records: {((List<ShopToCookEmail>)emailData2).Count}");
                Console.WriteLine($"Web file type: {webType}, Records: {((List<ShopToCookWeb>)webData2).Count}");
                Console.WriteLine($"Kiosk file type: {kioskType}, Records: {((List<ShopToCookKiosk>)kioskData2).Count}");

                Console.WriteLine("\n=== CSV PROCESSING TEST COMPLETED SUCCESSFULLY ===");

                // Cleanup sample files
                CleanupSampleFiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CSV processing test: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Cleans up sample CSV files
        /// </summary>
        private void CleanupSampleFiles()
        {
            try
            {
                var filesToDelete = new[] { "EmailData_sample_email.csv", "WebsiteData_sample_web.csv", "KioskData_sample_kiosk.csv" };
                
                foreach (var file in filesToDelete)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted sample file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up sample files: {ex.Message}");
            }
        }
    }
}
