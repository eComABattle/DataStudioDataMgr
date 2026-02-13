using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataStudioDataMgr.Services.Coupon
{
    public class CouponApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _authorizationHeader;
        private readonly string _cookieHeader;

        public CouponApiService()
        {
            _httpClient = new HttpClient();
            _baseUrl = "https://ecpreportapi.azurewebsites.net/api/v1/clipredemptionreport";
            _authorizationHeader = "Basic RWNvbUFXR1VzZXI6VDJVM3BnLkQ2NX59";
            _cookieHeader = "ARRAffinity=1c57d3eb0a0d8573a5deb8ee29cdf9f6c22d00156f0934389302522664094fa0; ARRAffinitySameSite=1c57d3eb0a0d8573a5deb8ee29cdf9f6c22d00156f0934389302522664094fa0";
        }

        public CouponApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = "https://ecpreportapi.azurewebsites.net/api/v1/clipredemptionreport";
            _authorizationHeader = "Basic RWNvbUFXR1VzZXI6VDJVM3BnLkQ2NX59";
            _cookieHeader = "ARRAffinity=1c57d3eb0a0d8573a5deb8ee29cdf9f6c22d00156f0934389302522664094fa0; ARRAffinitySameSite=1c57d3eb0a0d8573a5deb8ee29cdf9f6c22d00156f0934389302522664094fa0";
        }

        /// <summary>
        /// Fetches coupon redemption report data from the Coupon API
        /// </summary>
        /// <param name="statuses">Array of status strings to filter by</param>
        /// <param name="startDate">Start date for filtering (ISO 8601 format)</param>
        /// <param name="endDate">End date for filtering (ISO 8601 format)</param>
        /// <returns>API response containing coupon redemption data</returns>
        public async Task<CouponApi.RootObject> GetCouponRedemptionReportAsync(
            string[] statuses = null,
            string startDate = null,
            string endDate = null)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl);

                // Add headers
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("Authorization", _authorizationHeader);
                request.Headers.Add("Cookie", _cookieHeader);

                // Format dates using ISO 8601 format ("O")
                // If dates are not provided, use current date as default
                DateTime currentDate = DateTime.Now;
                DateTime startDateValue = currentDate.Date;
                DateTime endDateValue = currentDate.Date.AddDays(1).AddTicks(-1);

                // Parse provided dates if they exist
                if (!string.IsNullOrEmpty(startDate))
                {
                    if (DateTime.TryParse(startDate, out DateTime parsedStartDate))
                    {
                        startDateValue = parsedStartDate.Date;
                    }
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    if (DateTime.TryParse(endDate, out DateTime parsedEndDate))
                    {
                        endDateValue = parsedEndDate.Date.AddDays(1).AddTicks(-1);
                    }
                }

                // Format dates as ISO 8601 strings
                var startDateString = startDateValue.ToString("O");
                var endDateString = endDateValue.ToString("O");

                // Use provided statuses or default to ["string"]
                var statusesArray = statuses ?? new[] { "string" };
                var statusesJson = string.Join(",", statusesArray.Select(s => $"\"{s}\""));

                // Create properly formatted JSON body
                var jsonBody = $@"{{
        ""statuses"": [{statusesJson}],
        ""startDate"": ""{startDateString}"",
        ""endDate"": ""{endDateString}""
      }}";

                request.Content = new StringContent(jsonBody, Encoding.UTF8, "text/json");

                Console.WriteLine($"Making Coupon API request to: {_baseUrl}");
                Console.WriteLine($"Request body: {jsonBody}");

                // Make the API call
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<CouponApi.RootObject>(responseBody);

                if (result == null)
                {
                    throw new InvalidOperationException("Failed to deserialize coupon API response");
                }

                Console.WriteLine($"Coupon API Response received: {responseBody.Length} characters");
                Console.WriteLine($"Result: {result.result}");
                Console.WriteLine($"Message: {result.message}");
                Console.WriteLine($"Timestamp: {result.timestamp}");
                Console.WriteLine($"Number of coupons: {result.data?.Count ?? 0}");

                return result;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Coupon API HTTP Error: {ex.Message}");
                throw;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Coupon API JSON Parsing Error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Coupon API Unexpected Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Fetches coupon redemption report data with date range
        /// </summary>
        /// <param name="startDateString">Start date string (YYYY-MM-DD format)</param>
        /// <param name="endDateString">End date string (YYYY-MM-DD format)</param>
        /// <param name="statuses">Array of status strings to filter by</param>
        /// <returns>API response containing coupon redemption data</returns>
        public async Task<CouponApi.RootObject> GetCouponRedemptionReportByDateRangeAsync(
            string startDateString,
            string endDateString,
            string[] statuses = null)
        {
            return await GetCouponRedemptionReportAsync(statuses, startDateString, endDateString);
        }

        /// <summary>
        /// Fetches coupon redemption report data for the last N days
        /// </summary>
        /// <param name="days">Number of days to look back</param>
        /// <param name="statuses">Array of status strings to filter by</param>
        /// <returns>API response containing coupon redemption data</returns>
        public async Task<CouponApi.RootObject> GetCouponRedemptionReportForLastDaysAsync( DateTime startDt, DateTime endDt,
            int days = 1,
            string[] statuses = null)
        {
            //var endDate = endDt.ToString("yyyy-MM-dd");
            //var startDate = startDt.ToString("yyyy-MM-dd");

            var endDate = endDt.ToString("O");
            var startDate = startDt.ToString("O");

            return await GetCouponRedemptionReportAsync(statuses, startDate, endDate);
        }

        /// <summary>
        /// String fields are quoted and escaped so commas and emojis don't break CSV
        /// </summary>
        static string Quote(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return "\"\"";

            string value = token.ToString().Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        /// <summary>
        /// Converts coupon redemption data to CSV format and saves to file
        /// </summary>
        /// <param name="couponResponse">The coupon API response</param>
        /// <param name="outputFilePath">Path where the CSV file should be saved</param>
        public void ConvertCouponResponseToCsv(CouponApi.RootObject couponResponse, string outputFilePath)
        {
            try
            {
                if (couponResponse?.data == null)
                {
                    Console.WriteLine("No coupon data found in response to convert to CSV.");
                    return;
                }

                using (var writer = new StreamWriter(outputFilePath))
                {
                    // Write CSV header
                    writer.WriteLine("couponId,brand,clipCount,redemptionCount,description,offerLanguageOffer,redemptionValue");

                    // Loop through coupons and write data
                    foreach (var coupon in couponResponse.data)
                    {
                        writer.WriteLine(string.Join(",",
                            coupon.couponId,
                            Quote(coupon.brand),
                            coupon.clipCount,
                            coupon.redemptionCount,
                            Quote(coupon.description),
                            Quote(coupon.offerLanguageOffer),
                            coupon.redemptionValue
                        ));
                    }
                }

                Console.WriteLine($"Coupon CSV file saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting coupon response to CSV: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Converts a JSON response string to CSV format and saves to file
        /// </summary>
        /// <param name="jsonResponse">The JSON response string from the API</param>
        /// <param name="outputFilePath">Path where the CSV file should be saved</param>
        public void ConvertJsonToCsv(string jsonResponse, string outputFilePath)
        {
            try
            {
                // Parse JSON
                JObject obj = JObject.Parse(jsonResponse);
                JArray coupons = (JArray)obj["data"];

                if (coupons == null)
                {
                    Console.WriteLine("No coupon data found in JSON response to convert to CSV.");
                    return;
                }

                using (var writer = new StreamWriter(outputFilePath))
                {
                    // Write CSV header
                    writer.WriteLine("couponId,brand,clipCount,redemptionCount,description,offerLanguageOffer,redemptionValue");

                    // Loop through coupons and write data
                    foreach (var coupon in coupons)
                    {
                        writer.WriteLine(string.Join(",",
                            coupon["couponId"],
                            Quote(coupon["brand"]),
                            coupon["clipCount"],
                            coupon["redemptionCount"],
                            Quote(coupon["description"]),
                            Quote(coupon["offerLanguageOffer"]),
                            coupon["redemptionValue"]
                        ));
                    }
                }

                Console.WriteLine($"Coupon CSV file saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting JSON to CSV: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Saves the coupon API response as JSON to a file
        /// </summary>
        /// <param name="couponResponse">The coupon API response</param>
        /// <param name="outputFilePath">Path where the JSON file should be saved</param>
        public void SaveCouponResponseAsJson(CouponApi.RootObject couponResponse, string outputFilePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(couponResponse, Formatting.Indented);
                File.WriteAllText(outputFilePath, json);
                Console.WriteLine($"Coupon JSON file saved to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving coupon response as JSON: {ex.Message}");
                throw;
            }
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
