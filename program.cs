using Newtonsoft.Json;
using DataStudioDataMgr;
using DataStudioDataMgr.Services.Emfluence;
using DataStudioDataMgr.Services.Coupon;
using DataStudioDataMgr.Services.SqlServer;
using DataStudioDataMgr.Services.MediaStudio;
using DataStudioDataMgr.Services.DigitalStudio;
using DataStudioDataMgr.Services.MongoDb;
using DataStudioDataMgr.Services.ShopToCook;
using DataStudioDataMgr.Services.Brick;
using DataStudioDataMgr.Services.GiveX;
using DataStudioDataMgr.Services.BrData;
using DataStudioDataMgr.Services.AppCard;
using DataStudioDataMgr.Models;
using DataStudioDataMgr.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Configuration;

class Program
{
    private static readonly string OutputDirectory = ConfigurationManager.AppSettings["OutputDirectory"] ?? "output";
    private static readonly bool EnableDetailedLogging = bool.Parse(ConfigurationManager.AppSettings["EnableDetailedLogging"] ?? "true");
    private static readonly bool LogToFile = bool.Parse(ConfigurationManager.AppSettings["LogToFile"] ?? "false");
    private static readonly string LogFilePath = ConfigurationManager.AppSettings["LogFilePath"] ?? "DataStudioDataMgr.log";
    private static readonly bool EnableMongoDbStorage = bool.Parse(ConfigurationManager.AppSettings["EnableMongoDbStorage"] ?? "true");

    /// <summary>
    /// Validates that config values using environment variables (%VAR%) are set on this machine.
    /// Logs warnings when values still contain unexpanded placeholders after expansion.
    /// See DEPLOYMENT.md for deployment and environment variable setup.
    /// </summary>
    private static void ValidateConfigOnStartupIfEnabled()
    {
        bool validate = bool.Parse(ConfigurationManager.AppSettings["ValidateConfigOnStartup"] ?? "false");
        if (!validate) return;

        LogMessage("Validating configuration (environment variables)...");
        var warnings = new System.Collections.Generic.List<string>();

        // Connection strings
        try
        {
            var cs = ConfigurationManager.ConnectionStrings["MongoDbConnectionString"]?.ConnectionString;
            if (!string.IsNullOrEmpty(cs))
            {
                var expanded = Environment.ExpandEnvironmentVariables(cs);
                if (expanded.Contains("%"))
                    warnings.Add("MongoDbConnectionString may have unset env vars (still contains %...%). Set MONGODB_USERNAME, MONGODB_PASSWORD, MONGODB_HOST, MONGODB_DATABASE on this machine.");
            }
        }
        catch { /* ignore */ }

        // App settings that commonly use %VAR%
        var appKeys = new[]
        {
            "EmfluenceAccessToken", "ShopToCookSftpPassword", "AppCardSftpHost", "AppCardSftpPassword",
            "BrickPassword", "MongoDbDatabaseName", "MongoDbTestDatabaseName"
        };
        foreach (var key in appKeys)
        {
            try
            {
                var raw = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrEmpty(raw)) continue;
                var expanded = Environment.ExpandEnvironmentVariables(raw);
                if (expanded.Contains("%"))
                    warnings.Add($"{key} may not be set on this machine (still contains %...%). Set the corresponding environment variable on this machine.");
            }
            catch { /* ignore */ }
        }

        // Store_N_AccessToken
        for (int i = 1; i <= 20; i++)
        {
            try
            {
                var raw = ConfigurationManager.AppSettings[$"Store_{i}_AccessToken"];
                if (string.IsNullOrEmpty(raw)) continue;
                var expanded = Environment.ExpandEnvironmentVariables(raw);
                if (expanded.Contains("%"))
                    warnings.Add($"Store_{i}_AccessToken may not be set on this machine (still contains %...%). Set STORE_{i}_ACCESS_TOKEN (or the value in config) on this machine.");
            }
            catch { /* ignore */ }
        }

        if (warnings.Count > 0)
        {
            LogMessage("Configuration validation found potential issues (environment variables not set on this machine):");
            foreach (var w in warnings)
                LogMessage("  WARNING: " + w);
            LogMessage("See DEPLOYMENT.md for how to set environment variables on the deployment target.");
        }
        else
            LogMessage("Configuration validation: no unexpanded %VAR% placeholders detected.");
    }

    static async Task Main(string[] args)
    {
        try
        {
            LogMessage("DataStudio Data Manager - Starting...");
            ValidateConfigOnStartupIfEnabled();
            bool useLocalFile = args.Length > 0 && args[0].ToLower() == "--local";
            if (useLocalFile)
            {
                LogMessage("Command line override: Using local JSON file...");
                await ProcessLocalJsonFileAsync();
            }
            else
            {
                bool runEmfluenceApi = bool.Parse(ConfigurationManager.AppSettings["RunEmfluenceApi"] ?? "true");
                bool runCouponApi = bool.Parse(ConfigurationManager.AppSettings["RunCouponApi"] ?? "true");
                bool runTestEmailMethod = bool.Parse(ConfigurationManager.AppSettings["RunTestEmailMethod"] ?? "false");
                bool runCampaignData = bool.Parse(ConfigurationManager.AppSettings["RunCampaignData"] ?? "false");
                bool runUnfiCustomerData = bool.Parse(ConfigurationManager.AppSettings["RunUnfiCustomerData"] ?? "false");
                bool runMediaStudioData = bool.Parse(ConfigurationManager.AppSettings["RunMediaStudioData"] ?? "false");
                bool runDigitalStudioData = bool.Parse(ConfigurationManager.AppSettings["RunDigitalStudioData"] ?? "false");
                bool runEntryMetricsData = bool.Parse(ConfigurationManager.AppSettings["RunEntryMetricsData"] ?? "false");
                bool runShopToCookData = bool.Parse(ConfigurationManager.AppSettings["RunShopToCookData"] ?? "false");
                bool runGiveXEmail = bool.Parse(ConfigurationManager.AppSettings["RunGiveXEmail"] ?? "false");
                bool runGiveXEmailData = bool.Parse(ConfigurationManager.AppSettings["RunGiveXEmailData"] ?? "false");
                bool runGiveXLoyaltyData = bool.Parse(ConfigurationManager.AppSettings["RunGiveXLoyaltyData"] ?? "false");
                bool runGiveXCouponData = bool.Parse(ConfigurationManager.AppSettings["RunGiveXCouponData"] ?? "false");
                bool runAwgEmailStats = bool.Parse(ConfigurationManager.AppSettings["RunAwgEmailStats"] ?? "false");
                bool runBrDataPOSData = bool.Parse(ConfigurationManager.AppSettings["RunBrDataPOSData"] ?? "false");
                bool runAppCardData = bool.Parse(ConfigurationManager.AppSettings["RunAppCardData"] ?? "true");
                bool runBrickCampaignData = bool.Parse(ConfigurationManager.AppSettings["RunBrickCampaignData"] ?? "false");
                bool runBrickDailyStatistics = bool.Parse(ConfigurationManager.AppSettings["RunBrickDailyStatistics"] ?? "false");
                bool runBrickBackupDeleteAndReprocess = bool.Parse(ConfigurationManager.AppSettings["RunBrickBackupDeleteAndReprocess"] ?? "false");
                bool testMongoDbConnection = bool.Parse(ConfigurationManager.AppSettings["TestMongoDbConnection"] ?? "false");
                bool testBrickConnection = bool.Parse(ConfigurationManager.AppSettings["TestBrickConnection"] ?? "false");

                if (!runEmfluenceApi && !runCouponApi && !runTestEmailMethod && !runCampaignData && !runUnfiCustomerData && !runMediaStudioData && !runDigitalStudioData && !runEntryMetricsData && !runShopToCookData && !runGiveXEmail && !runGiveXEmailData && !runGiveXLoyaltyData && !runGiveXCouponData && !runAwgEmailStats && !runBrDataPOSData && !runAppCardData && !runBrickCampaignData && !runBrickDailyStatistics && !runBrickBackupDeleteAndReprocess && !testMongoDbConnection && !testBrickConnection)
                {
                    LogMessage("No services configured to run. Check App.config settings.");
                    return;
                }
                EnsureOutputDirectory();
                if (runEmfluenceApi)
                {
                    LogMessage("=== RUNNING EMFLUENCE API SERVICE ===");
                    await ProcessEmfluenceApiAsync();
                }
                if (runCouponApi)
                {
                    LogMessage("=== RUNNING COUPON API SERVICE ===");
                    await ProcessCouponApiAsync();
                }
                // Run configured services
                if (runTestEmailMethod)
                {
                    LogMessage("=== RUNNING TEST EMAIL METHOD (90 DAYS) ===");

                    // Create EmfluenceApiService instance
                    EmfluenceApiService apiService = null;
                    try
                    {
                        string accessToken = ConfigurationManager.AppSettings["EmfluenceAccessToken"];
                        
                        // Expand environment variables in access token
                        accessToken = Environment.ExpandEnvironmentVariables(accessToken ?? "");
                        
                        string deliveryType = ConfigurationManager.AppSettings["EmfluenceDeliveryType"] ?? "manual";
                        string status = ConfigurationManager.AppSettings["EmfluenceStatus"] ?? "sent";
                        string startDate = ConfigurationManager.AppSettings["TestStartDate"];

                        if (string.IsNullOrEmpty(accessToken))
                        {
                            LogMessage("ERROR: EmfluenceAccessToken not configured in App.config");
                            return;
                        }

                        LogMessage("Initializing Emfluence API service for daily test...");
                        apiService = new EmfluenceApiService(accessToken);

                        string outputPath = Path.Combine(OutputDirectory, "test_daily_emails.csv");

                        int totalRecords = await apiService.TestDailyEmailRecordsAsync(
                            deliveryType: deliveryType,
                            status: status,
                            startDate: startDate,
                            outputFilePath: outputPath
                        );

                        LogMessage($"Daily email test completed successfully. Total records: {totalRecords}");
                    }
                    catch (HttpRequestException ex)
                    {
                        LogMessage($"Emfluence API Error during daily test: {ex.Message}");
                        LogMessage("This might be due to:");
                        LogMessage("  - Invalid access token");
                        LogMessage("  - Network connectivity issues");
                        LogMessage("  - API endpoint changes");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error during daily email test: {ex.Message}");
                    }
                    finally
                    {
                        apiService?.Dispose();
                    }
                }

                if (runCampaignData)
                {
                    LogMessage("=== RUNNING CAMPAIGN DATA SERVICE ===");
                    await ProcessCampaignDataAsync();
                }

                if (runUnfiCustomerData)
                {
                    LogMessage("=== RUNNING UNFI CUSTOMER DATA SERVICE ===");
                    await ProcessUnfiCustomerDataAsync();
                }

                if (runMediaStudioData)
                {
                    LogMessage("=== RUNNING MEDIASTUDIO DATA SERVICE ===");
                    await ProcessMediaStudioDataAsync();
                }

                if (runDigitalStudioData)
                {
                    LogMessage("=== RUNNING DIGITALSTUDIO DATA SERVICE ===");
                    await ProcessDigitalStudioDataAsync();
                }

                if (runEntryMetricsData)
                {
                    LogMessage("=== RUNNING ENTRY METRICS DATA SERVICE ===");
                    await ProcessEntryMetricsDataAsync();
                }

                if (runShopToCookData)
                {
                    LogMessage("=== RUNNING SHOPTOCOOK DATA SERVICE ===");
                    await ProcessShopToCookDataAsync();
                }

                if (runGiveXEmail || runGiveXEmailData)
                {
                    LogMessage("=== RUNNING GIVEX EMAIL DATA SERVICE ===");
                    await ProcessGiveXEmailDataAsync();
                }

                if (runGiveXLoyaltyData)
                {
                    LogMessage("=== RUNNING GIVEX LOYALTY DATA SERVICE ===");
                    await ProcessGiveXLoyaltyDataAsync();
                }

                if (runGiveXCouponData)
                {
                    LogMessage("=== RUNNING GIVEX COUPON DATA SERVICE ===");
                    await ProcessGiveXCouponDataAsync();
                }

                if (runAwgEmailStats)
                {
                    LogMessage("=== RUNNING AWG EMAIL STATS SERVICE (GIVEX CURATED) ===");
                    await ProcessAwgEmailStatsAsync();
                }

                if (runBrDataPOSData)
                {
                    LogMessage("=== RUNNING BRDATA POS DATA SERVICE ===");
                    await ProcessBrDataPOSDataAsync();
                }

                if (runAppCardData)
                {
                    LogMessage("=== RUNNING APPCARD DATA SERVICE ===");
                    await ProcessAppCardDataAsync();
                }

                if (runBrickCampaignData)
                {
                    LogMessage("=== RUNNING BRICK CAMPAIGN DATA SERVICE ===");
                    await ProcessBrickCampaignDataAsync();
                }

                if (runBrickDailyStatistics)
                {
                    // Skip if ProcessBrickCampaignDataAsync was already called, as it already processes daily statistics for all campaigns
                    if (!runBrickCampaignData)
                    {
                        LogMessage("=== RUNNING BRICK DAILY STATISTICS SERVICE ===");
                        await ProcessBrickDailyStatisticsAsync();
                    }
                    else
                    {
                        LogMessage("=== SKIPPING BRICK DAILY STATISTICS SERVICE ===");
                        LogMessage("ProcessBrickCampaignDataAsync already processed daily statistics for all campaigns.");
                        LogMessage("To avoid duplication, only enable RunBrickCampaignData (which includes daily statistics processing).");
                    }
                }

                if (runBrickBackupDeleteAndReprocess)
                {
                    LogMessage("=== RUNNING BRICK BACKUP, DELETE, AND REPROCESS ===");
                    await BackupDeleteAndReprocessBrickDataAsync();
                }

                // Test MongoDB connection if enabled
                if (testMongoDbConnection)
                {
                    LogMessage("=== TESTING MONGODB CONNECTION ===");
                    await TestMongoDbConnectionAsync();
                }

                // Test Brick connection if enabled
                if (testBrickConnection)
                {
                    LogMessage("=== TESTING BRICK CONNECTION ===");
                    await TestBrickConnectionAsync();
                }

            }
            LogMessage("DataStudio Data Manager - Completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error: {ex.Message}");
            LogMessage($"Stack Trace: {ex.StackTrace}");
        }
    }
    private static void LogMessage(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";
        Console.WriteLine(logEntry);
        if (LogToFile)
        {
            try
            {
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
    private static void EnsureOutputDirectory()
    {
        if (!Directory.Exists(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
            LogMessage($"Created output directory: {OutputDirectory}");
        }
    }
    static async Task ProcessEmfluenceApiAsync()
    {
        try
        {
            LogMessage("Loading store configurations...");
            var stores = StoreConfigurationManager.GetEnabledStores();
            
            if (stores.Count == 0)
            {
                LogMessage("ERROR: No enabled stores configured in App.config");
                return;
            }
            
            LogMessage($"Found {stores.Count} enabled store(s) to process");
            
            // Process each store
            foreach (var store in stores)
            {
                await ProcessStoreAsync(store);
            }
        }
        catch (Exception ex)
        {
            LogMessage($"ERROR in ProcessEmfluenceApiAsync: {ex.Message}");
            throw;
        }
    }

    static async Task ProcessStoreAsync(StoreConfiguration store)
    {
        EmfluenceApiService apiService = null;
        try
        {
            LogMessage($"=== PROCESSING STORE: {store.StoreName} (ID: {store.StoreId}) ===");
            
            if (string.IsNullOrEmpty(store.AccessToken))
            {
                LogMessage($"ERROR: Access token not configured for store {store.StoreName}");
                return;
            }
            
            LogMessage($"Initializing Emfluence API service for store: {store.StoreName}...");
            apiService = new EmfluenceApiService(store);
            
            LogMessage("=== FETCHING ALL DETAILED RECORDS ===");
            var allDetailRecords = await apiService.GetAllEmailRecordsAsync(
                deliveryType: store.DeliveryType, 
                status: store.Status, 
                dateSentStart: store.DateSentStart, 
                automatedResults: "detail", 
                maxRecords: store.MaxRecords);
            
            LogMessage($"Total detailed records fetched for {store.StoreName}: {allDetailRecords.Count}");
            
            var allDetailResponse = new EmfluenceAPI.RootResponse
            {
                Data = new EmfluenceAPI.DataWrapper
                {
                    Records = allDetailRecords,
                    Paging = null
                },
                Success = 1,
                Code = 200,
                RequestID = Guid.NewGuid().ToString()
            };
            
            // Create store-specific file names
            string storePrefix = $"{store.StoreId}_";
            string allDetailJsonPath = Path.Combine(OutputDirectory, $"{storePrefix}all_records_detail.json");
            string allDetailCsvPath = Path.Combine(OutputDirectory, $"{storePrefix}all_records_detail.csv");
            
            string allDetailJson = JsonConvert.SerializeObject(allDetailResponse, Formatting.Indented);
            File.WriteAllText(allDetailJsonPath, allDetailJson);
            LogMessage($"All detailed records saved to: {allDetailJsonPath}");
            
            apiService.ConvertResponseToCsv(allDetailResponse, allDetailCsvPath);

            // Store in MongoDB if enabled
            if (EnableMongoDbStorage)
            {
                LogMessage($"=== STORING DATA IN MONGODB FOR STORE: {store.StoreName} ===");
                MongoDbService mongoService = null;
                try
                {
                    mongoService = new MongoDbService();

                    // Create indexes for better performance
                    await mongoService.CreateIndexesAsync();

                    // Store the records with store information
                    int storedCount = await mongoService.StoreEmfluenceEmailsAsync(allDetailRecords, store.StoreId, store.StoreName);
                    LogMessage($"Successfully stored {storedCount} records in MongoDB for store: {store.StoreName}");

                    // Get total count in collection
                    long totalCount = await mongoService.GetEmfluenceEmailCountAsync();
                    LogMessage($"Total records in MongoDB collection: {totalCount}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error storing data in MongoDB for store {store.StoreName}: {ex.Message}");
                    LogMessage("Continuing with file operations...");
                }
            }

            LogMessage($"Emfluence API processing completed successfully for store: {store.StoreName}");
        }
        catch (HttpRequestException ex)
        {
            LogMessage($"Emfluence API Error for store {store.StoreName}: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid access token");
            LogMessage("  - Network connectivity issues");
            LogMessage("  - API endpoint changes");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing Emfluence API data for store {store.StoreName}: {ex.Message}");
        }
        finally
        {
            apiService?.Dispose();
        }
    }
    static async Task ProcessCouponApiAsync()
    {
        CouponApiService couponService = null;
        try
        {
            //string startDate = ConfigurationManager.AppSettings["CouponStartDate"];
            //string endDate = ConfigurationManager.AppSettings["CouponEndDate"];

            DateTime startOfDay = DateTime.Now.Date.AddDays(-1);
            DateTime endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            string startDate = startOfDay.ToString();
            string endDate = endOfDay.ToString();


            int daysBack = int.Parse(ConfigurationManager.AppSettings["CouponDaysBack"] ?? "1");
            string statusesConfig = ConfigurationManager.AppSettings["CouponStatuses"] ?? "string";
            LogMessage("Initializing Coupon API service...");
            couponService = new CouponApiService();
            CouponApi.RootObject couponResult;
            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                LogMessage($"Using configured date range: {startDate} to {endDate}");
                couponResult = await couponService.GetCouponRedemptionReportByDateRangeAsync(startDate, endDate);
            }
            else
            {
                LogMessage($"Using last {daysBack} days");
                couponResult = await couponService.GetCouponRedemptionReportForLastDaysAsync(startOfDay,endOfDay, daysBack);
            }
            if (couponResult?.data == null)
            {
                LogMessage("No coupon data found in API response.");
            }
            else
            {
                LogMessage($"Found {couponResult.data.Count} coupons from coupon API.");
                if (EnableDetailedLogging)
                {
                    LogMessage("Sample coupons from API:");
                    int displayCount = Math.Min(5, couponResult.data.Count);
                    for (int i = 0; i < displayCount; i++)
                    {
                        var coupon = couponResult.data[i];
                        LogMessage($"  CouponID: {coupon.couponId}, Brand: {coupon.brand}, Clip Count: {coupon.clipCount}, Redemption Count: {coupon.redemptionCount}");
                    }
                    if (couponResult.data.Count > displayCount)
                    {
                        LogMessage($"  ... and {couponResult.data.Count - displayCount} more coupons");
                    }
                }
                string couponJsonPath = Path.Combine(OutputDirectory, "coupon_data.json");
                couponService.SaveCouponResponseAsJson(couponResult, couponJsonPath);
                string couponCsvPath = Path.Combine(OutputDirectory, "coupon_data.csv");
                couponService.ConvertCouponResponseToCsv(couponResult, couponCsvPath);
            }

            // Load couponResult into MongoDB (digital_coupon_analytics.coupon_api)
            if (couponResult != null)
            {
                try
                {
                    var couponMongoService = new CouponApiMongoService();
                    int stored = await couponMongoService.StoreCouponApiResultAsync(couponResult);
                    LogMessage($"Coupon API result loaded into MongoDB (digital_coupon_analytics.coupon_api): {stored} document(s).");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error loading Coupon API result into MongoDB: {ex.Message}");
                }
            }

            LogMessage("Coupon API processing completed successfully.");
        }
        catch (HttpRequestException ex)
        {
            LogMessage($"Coupon API Error: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid authorization header");
            LogMessage("  - Network connectivity issues");
            LogMessage("  - API endpoint changes");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing Coupon API data: {ex.Message}");
        }
        finally
        {
            couponService?.Dispose();
        }
    }

    static async Task ProcessCampaignDataAsync()
    {
        SqlServerService sqlService = null;
        try
        {
            LogMessage("Initializing SQL Server service...");
            sqlService = new SqlServerService();

            // Test connection first
            bool connectionOk = await sqlService.TestConnectionAsync();
            if (!connectionOk)
            {
                LogMessage("ERROR: Could not connect to AdStudioUnfi database");
                return;
            }

            LogMessage("=== FETCHING CAMPAIGN DATA ===");
            var campaigns = await sqlService.GetCampaignsAsync();
            LogMessage($"Total campaigns retrieved: {campaigns.Count}");

            if (campaigns.Count > 0)
            {
                // Save to JSON
                string campaignJsonPath = Path.Combine(OutputDirectory, "campaign_data.json");
                await sqlService.SaveCampaignsToJsonAsync(campaigns, campaignJsonPath);
                LogMessage($"Campaign data saved to: {campaignJsonPath}");

                // Save to CSV
                string campaignCsvPath = Path.Combine(OutputDirectory, "campaign_data.csv");
                await sqlService.SaveCampaignsToCsvAsync(campaigns, campaignCsvPath);
                LogMessage($"Campaign data saved to: {campaignCsvPath}");

                // Store in MongoDB if enabled
                if (EnableMongoDbStorage)
                {
                    LogMessage("=== STORING CAMPAIGN DATA IN MONGODB ===");
                    MongoDbService mongoService = null;
                    try
                    {
                        mongoService = new MongoDbService();

                        // Create indexes for better performance
                        await mongoService.CreateIndexesAsync();

                        // Store the campaigns
                        int storedCount = await mongoService.StoreCampaignsAsync(campaigns, "campaign_data.csv");
                        LogMessage($"Successfully stored {storedCount} campaign records in MongoDB");

                        // Get total count in campaign collection
                        long totalCampaignCount = await mongoService.GetCampaignCountAsync();
                        LogMessage($"Total campaign records in MongoDB collection: {totalCampaignCount}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error storing campaign data in MongoDB: {ex.Message}");
                        LogMessage("Continuing with file operations...");
                    }
                }
            }
            else
            {
                LogMessage("No campaign data found to save");
            }

            LogMessage("Campaign data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing campaign data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid connection string");
            LogMessage("  - Database server not accessible");
            LogMessage("  - Table or column name changes");
            LogMessage("  - Permission issues");
        }
    }

    static async Task ProcessUnfiCustomerDataAsync()
    {
        UnfiCustomerService customerService = null;
        try
        {
            LogMessage("Initializing UNFI Customer SQL service...");
            customerService = new UnfiCustomerService();

            bool connectionOk = await customerService.TestConnectionAsync();
            if (!connectionOk)
            {
                LogMessage("ERROR: Could not connect to AdStudioUnfi database for customer data");
                return;
            }

            LogMessage("=== FETCHING UNFI CUSTOMER DATA ===");
            var records = await customerService.GetCustomersAsync();
            LogMessage($"Total UNFI customer records retrieved: {records.Count}");

            if (records.Count > 0)
            {
                string jsonPath = Path.Combine(OutputDirectory, "unfi_customer_data.json");
                await customerService.SaveUnfiCustomersToJsonAsync(records, jsonPath);
                LogMessage($"UNFI customer data saved to: {jsonPath}");

                string csvPath = Path.Combine(OutputDirectory, "unfi_customer_data.csv");
                await customerService.SaveUnfiCustomersToCsvAsync(records, csvPath);
                LogMessage($"UNFI customer data saved to: {csvPath}");
            }
            else
            {
                LogMessage("No UNFI customer rows returned from SQL (CustomerID > 0 filter)");
            }

            if (EnableMongoDbStorage)
            {
                LogMessage("=== STORING UNFI CUSTOMER DATA IN MONGODB ===");
                try
                {
                    var mongoService = new MongoDbService();
                    await mongoService.CreateIndexesAsync();
                    int storedCount = await mongoService.StoreCustomersAsync(records);
                    LogMessage($"MongoDB customer collection load completed; inserted {storedCount} document(s) (collection cleared first)");
                    long total = await mongoService.GetCustomerCountAsync();
                    LogMessage($"Total UNFI customer documents in MongoDB (ad_campaign.customer): {total}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error storing UNFI customer data in MongoDB: {ex.Message}");
                    LogMessage("Continuing...");
                }
            }

            LogMessage("UNFI customer data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing UNFI customer data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid connection string");
            LogMessage("  - Database server not accessible");
            LogMessage("  - Table or column name changes");
            LogMessage("  - Permission issues");
        }
    }

    static async Task ProcessMediaStudioDataAsync()
    {
        MediaStudioService mediaStudioService = null;
        try
        {
            LogMessage("Initializing MediaStudio service...");
            mediaStudioService = new MediaStudioService();

            // Test connection first
            bool connectionOk = await mediaStudioService.TestConnectionAsync();
            if (!connectionOk)
            {
                LogMessage("ERROR: Could not connect to MediaStudio database");
                return;
            }

            LogMessage("=== FETCHING UNFI POST CAMPAIGN STORE DATA ===");
            var records = await mediaStudioService.GetUnfiPostCampaignStoreAsync();
            LogMessage($"Total UNFI Post Campaign Store records retrieved: {records.Count}");

            if (records.Count > 0)
            {
                // Save to JSON
                string recordsJsonPath = Path.Combine(OutputDirectory, "unfi_post_campaign_store.json");
                await mediaStudioService.SaveUnfiPostCampaignStoreToJsonAsync(records, recordsJsonPath);
                LogMessage($"UNFI Post Campaign Store data saved to: {recordsJsonPath}");

                // Save to CSV
                string recordsCsvPath = Path.Combine(OutputDirectory, "unfi_post_campaign_store.csv");
                await mediaStudioService.SaveUnfiPostCampaignStoreToCsvAsync(records, recordsCsvPath);
                LogMessage($"UNFI Post Campaign Store data saved to: {recordsCsvPath}");

                // Store in MongoDB if enabled
                if (EnableMongoDbStorage)
                {
                    LogMessage("=== STORING UNFI POST CAMPAIGN STORE DATA IN MONGODB ===");
                    MongoDbService mongoService = null;
                    try
                    {
                        mongoService = new MongoDbService();

                        // Create indexes for better performance
                        await mongoService.CreateIndexesAsync();

                        // Store the records
                        int storedCount = await mongoService.StoreUnfiPostCampaignStoreAsync(records);
                        LogMessage($"Successfully stored {storedCount} UNFI Post Campaign Store records in MongoDB");

                        // Get total count in collection
                        long totalCount = await mongoService.GetUnfiPostCampaignStoreCountAsync();
                        LogMessage($"Total UNFI Post Campaign Store records in MongoDB collection: {totalCount}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error storing UNFI Post Campaign Store data in MongoDB: {ex.Message}");
                        LogMessage("Continuing with file operations...");
                    }
                }
            }
            else
            {
                LogMessage("No UNFI Post Campaign Store data found to save");
            }

            LogMessage("MediaStudio data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing MediaStudio data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid connection string");
            LogMessage("  - Database server not accessible");
            LogMessage("  - Table or column name changes");
            LogMessage("  - Permission issues");
        }
    }

    static async Task ProcessDigitalStudioDataAsync()
    {
        DigitalStudioService digitalStudioService = null;
        try
        {
            LogMessage("Initializing DigitalStudio service...");
            digitalStudioService = new DigitalStudioService();

            // Test connection first
            bool connectionOk = await digitalStudioService.TestConnectionAsync();
            if (!connectionOk)
            {
                LogMessage("ERROR: Could not connect to DigitalStudio database");
                return;
            }

            LogMessage("=== FETCHING CAMPAIGN STORE METRICS DATA ===");
            var records = await digitalStudioService.GetCampaignStoreMetricsAsync();
            LogMessage($"Total Campaign Store Metrics records retrieved: {records.Count}");

            if (records.Count > 0)
            {
                // Save to JSON
                string recordsJsonPath = Path.Combine(OutputDirectory, "campaign_store_metrics.json");
                await digitalStudioService.SaveCampaignStoreMetricsToJsonAsync(records, recordsJsonPath);
                LogMessage($"Campaign Store Metrics data saved to: {recordsJsonPath}");

                // Save to CSV
                string recordsCsvPath = Path.Combine(OutputDirectory, "campaign_store_metrics.csv");
                await digitalStudioService.SaveCampaignStoreMetricsToCsvAsync(records, recordsCsvPath);
                LogMessage($"Campaign Store Metrics data saved to: {recordsCsvPath}");

                // Store in MongoDB if enabled
                if (EnableMongoDbStorage)
                {
                    LogMessage("=== STORING CAMPAIGN STORE METRICS DATA IN MONGODB ===");
                    MongoDbService mongoService = null;
                    try
                    {
                        mongoService = new MongoDbService();

                        // Create indexes for better performance
                        await mongoService.CreateIndexesAsync();

                        // Store the records
                        int storedCount = await mongoService.StoreCampaignStoreMetricsAsync(records);
                        LogMessage($"Successfully stored {storedCount} Campaign Store Metrics records in MongoDB");

                        // Get total count in collection
                        long totalCount = await mongoService.GetCampaignStoreMetricsCountAsync();
                        LogMessage($"Total Campaign Store Metrics records in MongoDB collection: {totalCount}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error storing Campaign Store Metrics data in MongoDB: {ex.Message}");
                        LogMessage("Continuing with file operations...");
                    }
                }
            }
            else
            {
                LogMessage("No Campaign Store Metrics data found to save");
            }

            LogMessage("DigitalStudio data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing DigitalStudio data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid connection string");
            LogMessage("  - Database server not accessible");
            LogMessage("  - Table or column name changes");
            LogMessage("  - Permission issues");
        }
    }

    static async Task ProcessEntryMetricsDataAsync()
    {
        DigitalStudioService digitalStudioService = null;
        try
        {
            LogMessage("Initializing DigitalStudio service for Entry Metrics...");
            digitalStudioService = new DigitalStudioService();

            // Test connection first
            bool connectionOk = await digitalStudioService.TestConnectionAsync();
            if (!connectionOk)
            {
                LogMessage("ERROR: Could not connect to DigitalStudio database");
                return;
            }

            LogMessage("=== FETCHING ENTRY METRICS DATA ===");
            var records = await digitalStudioService.GetEntryMetricsAsync();
            LogMessage($"Total Entry Metrics records retrieved: {records.Count}");

            if (records.Count > 0)
            {
                // Save to JSON
                string recordsJsonPath = Path.Combine(OutputDirectory, "entry_metrics.json");
                await digitalStudioService.SaveEntryMetricsToJsonAsync(records, recordsJsonPath);
                LogMessage($"Entry Metrics data saved to: {recordsJsonPath}");

                // Save to CSV
                string recordsCsvPath = Path.Combine(OutputDirectory, "entry_metrics.csv");
                await digitalStudioService.SaveEntryMetricsToCsvAsync(records, recordsCsvPath);
                LogMessage($"Entry Metrics data saved to: {recordsCsvPath}");

                // Store in MongoDB if enabled
                if (EnableMongoDbStorage)
                {
                    LogMessage("=== STORING ENTRY METRICS DATA IN MONGODB ===");
                    MongoDbService mongoService = null;
                    try
                    {
                        mongoService = new MongoDbService();

                        // Create indexes for better performance
                        await mongoService.CreateIndexesAsync();

                        // Store the records
                        int storedCount = await mongoService.StoreEntryMetricsAsync(records);
                        LogMessage($"Successfully stored {storedCount} Entry Metrics records in MongoDB");

                        // Get total count in collection
                        long totalCount = await mongoService.GetEntryMetricsCountAsync();
                        LogMessage($"Total Entry Metrics records in MongoDB collection: {totalCount}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error storing Entry Metrics data in MongoDB: {ex.Message}");
                        LogMessage("Continuing with file operations...");
                    }
                }
            }
            else
            {
                LogMessage("No Entry Metrics data found to save");
            }

            LogMessage("Entry Metrics data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing Entry Metrics data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid connection string");
            LogMessage("  - Database server not accessible");
            LogMessage("  - Table or column name changes");
            LogMessage("  - Permission issues");
        }
    }

    static Task ProcessLocalJsonFileAsync()
    {
        try
        {
            string jsonFilePath = ConfigurationManager.AppSettings["JsonFilePath"] ?? "response.json";
            if (!File.Exists(jsonFilePath))
            {
                LogMessage($"ERROR: Local JSON file not found: {jsonFilePath}");
                return Task.CompletedTask;
            }
            LogMessage($"Using local JSON file: {jsonFilePath}");
            EnsureOutputDirectory();
            string json = File.ReadAllText(jsonFilePath);
            var result = JsonConvert.DeserializeObject<EmfluenceAPI.RootResponse>(json);
            if (result?.Data?.Records == null)
            {
                LogMessage("No records found in local JSON file.");
                return Task.CompletedTask;
            }
            LogMessage($"Found {result.Data.Records.Count} records in local JSON file.");
            if (EnableDetailedLogging)
            {
                foreach (var record in result.Data.Records)
                {
                    LogMessage($"EmailID: {record.EmailID}, Subject: {record.Subject}, Unique Opens: {record.Metrics?.UniqueViews ?? 0}");
                }
            }
            var apiService = new EmfluenceApiService("459F7AAF-77D6-4AAB-8EC4-75D2E926790E");
            string csvPath = Path.Combine(OutputDirectory, "local_response.csv");
            apiService.ConvertResponseToCsv(result, csvPath);
            apiService.Dispose();
            LogMessage($"Local JSON converted to CSV: {csvPath}");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing local JSON file: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    static async Task ProcessAppCardDataAsync()
    {
        AppCardService appCardService = null;
        try
        {
            LogMessage("Initializing AppCard service...");
            appCardService = new AppCardService();

            LogMessage("=== PROCESSING APPCARD DATA ===");
            await appCardService.ProcessAppCardDataAsync();

            LogMessage("AppCard data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing AppCard data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid SFTP credentials");
            LogMessage("  - Network connectivity issues");
            LogMessage("  - MongoDB connection problems");
            LogMessage("  - CSV file format issues");
        }
    }

    static async Task ProcessGiveXEmailDataAsync()
    {
        GiveXService giveXService = null;
        try
        {
            LogMessage("Initializing GiveX service...");
            giveXService = new GiveXService();

            LogMessage("=== PROCESSING GIVEX EMAIL DATA ===");
            await giveXService.ProcessGiveXEmailDataAsync();

            LogMessage("GiveX email data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing GiveX email data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Network share access issues");
            LogMessage("  - Network connectivity problems");
            LogMessage("  - MongoDB connection problems");
            LogMessage("  - CSV file format issues");
        }
    }

    static async Task ProcessGiveXLoyaltyDataAsync()
    {
        GiveXService giveXService = null;
        try
        {
            LogMessage("Initializing GiveX service...");
            giveXService = new GiveXService();

            LogMessage("=== PROCESSING GIVEX LOYALTY DATA ===");
            await giveXService.ProcessGiveXLoyaltyDataAsync();

            LogMessage("GiveX loyalty data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing GiveX loyalty data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Network share access issues");
            LogMessage("  - Network connectivity problems");
            LogMessage("  - MongoDB connection problems");
            LogMessage("  - CSV file format issues");
        }
    }

    static async Task ProcessGiveXCouponDataAsync()
    {
        GiveXService giveXService = null;
        try
        {
            LogMessage("Initializing GiveX service...");
            giveXService = new GiveXService();

            LogMessage("=== PROCESSING GIVEX COUPON DATA ===");
            await giveXService.ProcessGiveXCouponDataAsync();

            LogMessage("GiveX coupon data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing GiveX coupon data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Network share access issues");
            LogMessage("  - Network connectivity problems");
            LogMessage("  - MongoDB connection problems");
            LogMessage("  - CSV file format issues");
        }
    }

    static async Task ProcessAwgEmailStatsAsync()
    {
        try
        {
            var awgEmailStatsService = new AwgEmailStatsService();
            await awgEmailStatsService.ProcessAwgEmailStatsAsync();
            LogMessage("AWG email stats processing finished.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing AWG email stats: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Network share access issues (GiveXAwgNetworkPath / GiveXAwgEmailArchivePath)");
            LogMessage("  - MongoDB connection problems");
            LogMessage("  - CSV file format or header mismatch");
        }
    }

    static async Task ProcessBrDataPOSDataAsync()
    {
        BrDataService brDataService = null;
        try
        {
            LogMessage("Initializing BrData service...");
            brDataService = new BrDataService();

            LogMessage("=== PROCESSING BRDATA POS DATA ===");
            await brDataService.ProcessBrDataPosDataAsync();

            LogMessage("BrData POS data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing BrData POS data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Network share access issues");
            LogMessage("  - Network connectivity problems");
            LogMessage("  - MongoDB connection problems");
            LogMessage("  - CSV file format issues");
        }
    }

    static async Task ProcessShopToCookDataAsync()
    {
        ShopToCookService shopToCookService = null;
        try
        {
            LogMessage("Initializing ShopToCook service...");
            shopToCookService = new ShopToCookService();

            LogMessage("=== PROCESSING SHOPTOCOOK DATA ===");
            await shopToCookService.ProcessShopToCookDataAsync();

            LogMessage("ShopToCook data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing ShopToCook data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid SFTP credentials");
            LogMessage("  - Network connectivity issues");
            LogMessage("  - MongoDB connection problems");
            LogMessage("  - CSV file format issues");
        }
    }

    static async Task TestMongoDbConnectionAsync()
    {
        try
        {
            LogMessage("Testing MongoDB connection...");
            var mongoService = new ShopToCookMongoService();
            
            bool connectionSuccessful = await mongoService.TestConnectionAsync();
            
            // If the first test fails, try the alternative method
            if (!connectionSuccessful)
            {
                LogMessage("Primary connection test failed, trying alternative method...");
                connectionSuccessful = await mongoService.TestConnectionSimpleAsync();
            }
            
            if (connectionSuccessful)
            {
                LogMessage("MongoDB connection test successful!");
            }
            else
            {
                LogMessage("MongoDB connection test failed!");
                LogMessage("This might be due to:");
                LogMessage("  - MongoDB server not running");
                LogMessage("  - Incorrect connection string");
                LogMessage("  - Network connectivity issues");
                LogMessage("  - Invalid credentials");
                LogMessage("  - MongoDB driver compatibility issues");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error testing MongoDB connection: {ex.Message}");
            LogMessage($"Error type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                LogMessage($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    static async Task ProcessBrickCampaignDataAsync()
    {
        BrickService brickService = null;
        try
        {
            LogMessage("Initializing Brick service...");
            brickService = new BrickService();

            LogMessage("=== PROCESSING BRICK CAMPAIGN DATA ===");
            await brickService.ProcessBrickCampaignDataAsync();

            LogMessage("Brick campaign data processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing Brick campaign data: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid Brick API credentials");
            LogMessage("  - Network connectivity issues");
            LogMessage("  - Invalid endpoint URL");
            LogMessage("  - Authentication problems");
        }
        finally
        {
            brickService?.Dispose();
        }
    }

    static async Task ProcessBrickDailyStatisticsAsync()
    {
        BrickService brickService = null;
        try
        {
            LogMessage("WARNING: This method uses configuration values for dates (deprecated approach).");
            LogMessage("The current use case requires dates from the Brick Campaign endpoint.");
            LogMessage("Consider using ProcessBrickCampaignDataAsync instead, which gets dates from campaign data.");
            
            LogMessage("Initializing Brick service...");
            brickService = new BrickService();

            LogMessage("=== PROCESSING BRICK DAILY STATISTICS (DEPRECATED - Uses Config Values) ===");
            await brickService.ProcessBrickDailyStatisticsAsync();

            LogMessage("Brick daily statistics processing completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error processing Brick daily statistics: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - Invalid Brick API credentials");
            LogMessage("  - Network connectivity issues");
            LogMessage("  - Invalid endpoint URL");
            LogMessage("  - Invalid campaign ID or date range");
            LogMessage("  - Authentication problems");
        }
        finally
        {
            brickService?.Dispose();
        }
    }

    static async Task TestBrickConnectionAsync()
    {
        BrickApiService brickApiService = null;
        try
        {
            LogMessage("Testing Brick API connection...");
            brickApiService = new BrickApiService();
            
            bool connectionSuccessful = await brickApiService.TestConnectionAsync(1);
            
            if (connectionSuccessful)
            {
                LogMessage("Brick API connection test successful!");
            }
            else
            {
                LogMessage("Brick API connection test failed!");
                LogMessage("This might be due to:");
                LogMessage("  - Invalid username/password");
                LogMessage("  - Incorrect endpoint URL");
                LogMessage("  - Network connectivity issues");
                LogMessage("  - Brick server not accessible");
                LogMessage("  - Authentication problems");
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error testing Brick connection: {ex.Message}");
            LogMessage($"Error type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                LogMessage($"Inner exception: {ex.InnerException.Message}");
            }
        }
        finally
        {
            brickApiService?.Dispose();
        }
    }

    static async Task BackupDeleteAndReprocessBrickDataAsync()
    {
        BrickService brickService = null;
        try
        {
            LogMessage("Initializing Brick service...");
            brickService = new BrickService();

            LogMessage("=== BACKING UP, DELETING, AND REPROCESSING BRICK DATA ===");
            await brickService.BackupDeleteAndReprocessAsync(OutputDirectory);

            LogMessage("Brick data backup, delete, and reprocess completed successfully.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error during Brick data backup, delete, and reprocess: {ex.Message}");
            LogMessage("This might be due to:");
            LogMessage("  - MongoDB connection issues");
            LogMessage("  - Invalid Brick API credentials");
            LogMessage("  - Network connectivity issues");
            LogMessage("  - Invalid endpoint URL");
            LogMessage("  - Authentication problems");
        }
        finally
        {
            brickService?.Dispose();
        }
    }
}