using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Configuration;

namespace DataStudioDataMgr.Services.Brick
{
    public class BrickApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;

        public BrickApiService()
        {
            _httpClient = new HttpClient();
            
            // Get configuration from App.config, with defaults
            string baseUrl = ConfigurationManager.AppSettings["BrickBaseUrl"] ?? "https://serve.withbrick.com/api/v1";
            _username = ConfigurationManager.AppSettings["BrickUsername"] ?? "awg";
            //string password = ConfigurationManager.AppSettings["BrickPassword"] ?? "AWGbduzZyk5$";
            string password = "AWGbduzZyk5$";
            _password = Environment.ExpandEnvironmentVariables(password);
            
            // Ensure base URL ends with /api/v1
            if (!baseUrl.EndsWith("/api/v1"))
            {
                if (baseUrl.EndsWith("/"))
                {
                    baseUrl = baseUrl.TrimEnd('/');
                }
                if (!baseUrl.EndsWith("/api/v1"))
                {
                    baseUrl = baseUrl.TrimEnd('/') + "/api/v1";
                }
            }
            
            _baseUrl = baseUrl;
            
            // Set up basic authentication
            // Brick API requires username and password to be base64 encoded separately
            var encodedUsername = Convert.ToBase64String(Encoding.ASCII.GetBytes(_username));
            var encodedPassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(_password));
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{encodedUsername}:{encodedPassword}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public BrickApiService(HttpClient httpClient, string baseUrl, string username, string password)
        {
            _httpClient = httpClient;
            _username = username;
            _password = password;
            
            // Ensure base URL ends with /api/v1
            if (!baseUrl.EndsWith("/api/v1"))
            {
                if (baseUrl.EndsWith("/"))
                {
                    baseUrl = baseUrl.TrimEnd('/');
                }
                if (!baseUrl.EndsWith("/api/v1"))
                {
                    baseUrl = baseUrl.TrimEnd('/') + "/api/v1";
                }
            }
            
            _baseUrl = baseUrl;
            
            // Set up basic authentication
            // Brick API requires username and password to be base64 encoded separately
            var encodedUsername = Convert.ToBase64String(Encoding.ASCII.GetBytes(_username));
            var encodedPassword = Convert.ToBase64String(Encoding.ASCII.GetBytes(_password));
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{encodedUsername}:{encodedPassword}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Tests the connection to the Brick API by calling Get Campaign List By Advertising ID endpoint
        /// Uses base64 encoded username and password for authentication (configured in constructor)
        /// </summary>
        /// <param name="advertisingId">The advertising ID to use for the test (default: 1)</param>
        /// <returns>True if connection is successful, false otherwise</returns>
        public async Task<bool> TestConnectionAsync(int advertisingId = 1)
        {
            try
            {
                //var client = new HttpClient();
                //var request = new HttpRequestMessage(HttpMethod.Get, "https://serve.withbrick.com/api/v1/cam/adv/1");
                //request.Headers.Add("Authorization", "Basic YXdnOkFXR2JkdXpaeWs1JA==");
                //var response = await client.SendAsync(request);
                //response.EnsureSuccessStatusCode();
                //Console.WriteLine(await response.Content.ReadAsStringAsync());


                Console.WriteLine($"Testing Brick API connection...");
                Console.WriteLine($"Base URL: {_baseUrl}");
                Console.WriteLine($"Username: {_username} (will be base64 encoded for authentication)");
                
                string endpoint = $"{_baseUrl}/cam/adv/{advertisingId}";
                Console.WriteLine($"Calling endpoint: {endpoint}");

                // Authentication header with base64 encoded credentials is already set in constructor
                //var response = await _httpClient.GetAsync(endpoint);

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, "https://serve.withbrick.com/api/v1/cam/adv/1");
                request.Headers.Add("Authorization", "Basic YXdnOkFXR2JkdXpaeWs1JA==");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                Console.WriteLine(await response.Content.ReadAsStringAsync());

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Brick API connection test successful!");
                    Console.WriteLine($"Response status: {response.StatusCode}");
                    Console.WriteLine($"Response length: {content.Length} characters");
                    
                    if (content.Length > 0 && content.Length < 500)
                    {
                        Console.WriteLine($"Response preview: {content.Substring(0, Math.Min(content.Length, 200))}...");
                    }
                    
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Brick API connection test failed!");
                    Console.WriteLine($"Response status: {response.StatusCode}");
                    Console.WriteLine($"Error content: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing Brick API connection: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the campaign list by advertiser ID from the Brick API
        /// </summary>
        /// <param name="advertiserId">The advertiser ID (default: 1)</param>
        /// <returns>JSON response string containing the campaign list</returns>
        public async Task<string> GetCampaignListByAdvertiserIdAsync(int advertiserId = 1)
        {
            try
            {
                Console.WriteLine($"Getting campaign list for advertiser ID: {advertiserId}");
                
                string endpoint = $"{_baseUrl}/cam/adv/{advertiserId}";
                Console.WriteLine($"Calling endpoint: {endpoint}");

                //var response = await _httpClient.GetAsync(endpoint);

                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Add("Authorization", "Basic YXdnOkFXR2JkdXpaeWs1JA==");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                Console.WriteLine(await response.Content.ReadAsStringAsync());

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Brick API request failed with status {response.StatusCode}: {errorContent}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Brick API response received: {jsonContent.Length} characters");
                Console.WriteLine($"Response status: {response.StatusCode}");
                
                return jsonContent;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error getting campaign list: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting campaign list: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Gets campaign daily statistics from the Brick API
        /// </summary>
        /// <param name="campaignId">The campaign ID</param>
        /// <param name="startDate">The start date in YYYY-MM-DD format</param>
        /// <param name="endDate">The end date in YYYY-MM-DD format</param>
        /// <returns>JSON response string containing the campaign daily statistics</returns>
        public async Task<string> GetCampaignDailyStatisticsAsync(int campaignId, string startDate, string endDate)
        {
            try
            {
                Console.WriteLine($"Getting campaign daily statistics for campaign ID: {campaignId}");
                Console.WriteLine($"Date range: {startDate} to {endDate}");

                string endpoint = $"{_baseUrl}/cam/{campaignId}/statistics/daily/{startDate}/{endDate}";
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                request.Headers.Add("Authorization", "Basic YXdnOkFXR2JkdXpaeWs1JA==");
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                Console.WriteLine(await response.Content.ReadAsStringAsync());

                
                Console.WriteLine($"Calling endpoint: {endpoint}");

                //var response = await _httpClient.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Brick API request failed with status {response.StatusCode}: {errorContent}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Brick API response received: {jsonContent.Length} characters");
                Console.WriteLine($"Response status: {response.StatusCode}");
                
                return jsonContent;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error getting campaign daily statistics: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting campaign daily statistics: {ex.Message}");
                Console.WriteLine($"Error type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Gets campaign daily statistics from the Brick API using DateTime objects
        /// </summary>
        /// <param name="campaignId">The campaign ID</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <returns>JSON response string containing the campaign daily statistics</returns>
        public async Task<string> GetCampaignDailyStatisticsAsync(int campaignId, DateTime startDate, DateTime endDate)
        {
            string startDateString = startDate.ToString("yyyy-MM-dd");
            string endDateString = endDate.ToString("yyyy-MM-dd");
            return await GetCampaignDailyStatisticsAsync(campaignId, startDateString, endDateString);
        }

        /// <summary>
        /// Disposes of the HTTP client
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
