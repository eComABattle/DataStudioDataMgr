using System;
using System.Collections.Generic;
using System.Configuration;

namespace DataStudioDataMgr.Configuration
{
    /// <summary>
    /// Represents configuration for a single store
    /// </summary>
    public class StoreConfiguration
    {
        public string StoreId { get; set; }
        public string StoreName { get; set; }
        public string AccessToken { get; set; }
        public string DeliveryType { get; set; } = "manual";
        public string Status { get; set; } = "sent";
        public string DateSentStart { get; set; } = "2025-01-01";
        public int MaxRecords { get; set; } = 10000;
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Manages multiple store configurations
    /// </summary>
    public static class StoreConfigurationManager
    {
        /// <summary>
        /// Calculates the DateSentStart as 17 months prior to the current month
        /// Business rule: 17 months prior to current month (endpoint allows up to 18 month date range)
        /// </summary>
        /// <returns>Date string in YYYY-MM-DD format for the first day of the month 17 months prior</returns>
        private static string CalculateDateSentStart()
        {
            // Get current date and calculate 17 months prior
            DateTime currentDate = DateTime.Now;
            DateTime dateSentStart = currentDate.AddMonths(-17);
            
            // Get the first day of that month
            DateTime firstDayOfMonth = new DateTime(dateSentStart.Year, dateSentStart.Month, 1);
            
            // Format as YYYY-MM-DD
            return firstDayOfMonth.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// Loads all store configurations from App.config
        /// </summary>
        /// <returns>List of store configurations</returns>
        public static List<StoreConfiguration> LoadStoreConfigurations()
        {
            var stores = new List<StoreConfiguration>();
            
            Console.WriteLine("=== DEBUG: Loading Store Configurations ===");
            
            // Calculate DateSentStart dynamically (17 months prior to current month)
            string defaultDateSentStart = CalculateDateSentStart();
            Console.WriteLine($"DEBUG: Calculated DateSentStart (17 months prior): {defaultDateSentStart}");
            
            // Load the original single store configuration for backward compatibility
            var originalToken = ConfigurationManager.AppSettings["EmfluenceAccessToken"];
            
            // Expand environment variables in access token
            originalToken = Environment.ExpandEnvironmentVariables(originalToken ?? "");
            
            Console.WriteLine($"DEBUG: Original token found: {!string.IsNullOrEmpty(originalToken)}");
            
            if (!string.IsNullOrEmpty(originalToken))
            {
                var defaultStore = new StoreConfiguration
                {
                    StoreId = "PDF",
                    StoreName = "Payless Discount Foods",
                    AccessToken = originalToken,
                    DeliveryType = ConfigurationManager.AppSettings["EmfluenceDeliveryType"] ?? "manual",
                    Status = ConfigurationManager.AppSettings["EmfluenceStatus"] ?? "sent",
                    DateSentStart = ConfigurationManager.AppSettings["EmfluenceDateSentStart"] ?? defaultDateSentStart,
                    MaxRecords = int.Parse(ConfigurationManager.AppSettings["EmfluenceMaxRecords"] ?? "10000"),
                    Enabled = bool.Parse(ConfigurationManager.AppSettings["RunEmfluenceApi"] ?? "true")
                };
                
                stores.Add(defaultStore);
                Console.WriteLine($"DEBUG: Added default store - ID: {defaultStore.StoreId}, Name: {defaultStore.StoreName}, DateSentStart: {defaultStore.DateSentStart}, Enabled: {defaultStore.Enabled}");
            }

            // Load additional store configurations
            // Look for keys in format: Store_1_AccessToken, Store_1_Name, etc.
            for (int i = 1; i <= 20; i++) // Support up to 20 additional stores
            {
                var accessToken = ConfigurationManager.AppSettings[$"Store_{i}_AccessToken"];
                
                // Expand environment variables in access token
                accessToken = Environment.ExpandEnvironmentVariables(accessToken ?? "");
                
                Console.WriteLine($"DEBUG: Checking Store_{i}_AccessToken: {!string.IsNullOrEmpty(accessToken)}");
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    var store = new StoreConfiguration
                    {
                        StoreId = $"store_{i}",
                        StoreName = ConfigurationManager.AppSettings[$"Store_{i}_Name"] ?? $"Store {i}",
                        AccessToken = accessToken,
                        DeliveryType = ConfigurationManager.AppSettings[$"Store_{i}_DeliveryType"] ?? "manual",
                        Status = ConfigurationManager.AppSettings[$"Store_{i}_Status"] ?? "sent",
                        DateSentStart = ConfigurationManager.AppSettings[$"Store_{i}_DateSentStart"] ?? defaultDateSentStart,
                        MaxRecords = int.Parse(ConfigurationManager.AppSettings[$"Store_{i}_MaxRecords"] ?? "10000"),
                        Enabled = bool.Parse(ConfigurationManager.AppSettings[$"Store_{i}_Enabled"] ?? "true")
                    };
                    
                    stores.Add(store);
                    Console.WriteLine($"DEBUG: Added Store_{i} - ID: {store.StoreId}, Name: {store.StoreName}, DateSentStart: {store.DateSentStart}, Enabled: {store.Enabled}");
                }
            }

            Console.WriteLine($"DEBUG: Total stores loaded: {stores.Count}");
            Console.WriteLine("=== END DEBUG ===");
            
            return stores;
        }

        /// <summary>
        /// Gets enabled store configurations only
        /// </summary>
        /// <returns>List of enabled store configurations</returns>
        public static List<StoreConfiguration> GetEnabledStores()
        {
            var allStores = LoadStoreConfigurations();
            var enabledStores = allStores.FindAll(s => s.Enabled);
            Console.WriteLine($"DEBUG: Enabled stores: {enabledStores.Count} out of {allStores.Count} total");
            return enabledStores;
        }

        /// <summary>
        /// Gets a specific store configuration by ID
        /// </summary>
        /// <param name="storeId">Store ID to find</param>
        /// <returns>Store configuration or null if not found</returns>
        public static StoreConfiguration GetStoreById(string storeId)
        {
            return LoadStoreConfigurations().Find(s => s.StoreId == storeId);
        }
    }
}