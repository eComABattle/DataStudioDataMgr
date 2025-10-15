using Newtonsoft.Json;
using DataStudioDataMgr;
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
    static async Task Main(string[] args)
    {
        try
        {
            LogMessage("DataStudio Data Manager - Starting...");
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
                bool runMediaStudioData = bool.Parse(ConfigurationManager.AppSettings["RunMediaStudioData"] ?? "false");
                bool runDigitalStudioData = bool.Parse(ConfigurationManager.AppSettings["RunDigitalStudioData"] ?? "false");
                bool runEntryMetricsData = bool.Parse(ConfigurationManager.AppSettings["RunEntryMetricsData"] ?? "false");

                if (!runEmfluenceApi && !runCouponApi && !runTestEmailMethod && !runCampaignData && !runMediaStudioData && !runDigitalStudioData && !runEntryMetricsData)
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
            string startDate = ConfigurationManager.AppSettings["CouponStartDate"];
            string endDate = ConfigurationManager.AppSettings["CouponEndDate"];
            int daysBack = int.Parse(ConfigurationManager.AppSettings["CouponDaysBack"] ?? "30");
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
                couponResult = await couponService.GetCouponRedemptionReportForLastDaysAsync(daysBack);
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
                        int storedCount = await mongoService.StoreCampaignsAsync(campaigns);
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
}