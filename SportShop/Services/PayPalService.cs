using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SportShop.Services
{
    public class PayPalService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl;

        public PayPalService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            // PayPal Sandbox Test Credentials (working test account)
            _clientId = "AROXnLoQ-DqFFeWlhTv-qO9CLNbA3TH9sLstJFnnYmxHTwwP8PihpyMkbw87FEE0eXSiuEnOQa_GXwBR";
            _clientSecret = "EEc4T2xwfo6kLS9MgqWYVAjT9e2PkCQrIzSjTqhrncrwMqWucojlmCUaSIz7m1PZmWdjAAVHLT6Ta21T";
            _baseUrl = "https://api-m.sandbox.paypal.com"; // Sandbox URL
        }

        // Lấy Access Token từ PayPal
        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                Console.WriteLine($"[PayPal] Getting access token...");
                Console.WriteLine($"[PayPal] Client ID: {_clientId}");
                Console.WriteLine($"[PayPal] Base URL: {_baseUrl}");
                
                var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
                
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v1/oauth2/token");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");
                
                Console.WriteLine($"[PayPal] Sending token request to: {request.RequestUri}");
                var response = await _httpClient.SendAsync(request);
                Console.WriteLine($"[PayPal] Token response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[PayPal] Token response: {jsonResponse}");
                    var tokenResponse = JsonSerializer.Deserialize<PayPalTokenResponse>(jsonResponse);
                    Console.WriteLine($"[PayPal] Access token received successfully");
                    return tokenResponse?.access_token ?? throw new Exception("Access token is null");
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[PayPal] Token error: {errorContent}");
                throw new Exception($"Failed to get PayPal access token. Status: {response.StatusCode}, Error: {errorContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayPal] Authentication exception: {ex.Message}");
                throw new Exception($"PayPal authentication error: {ex.Message}", ex);
            }
        }

        // Tạo Payment Order
        public async Task<PayPalOrderResponse> CreateOrderAsync(decimal amount, string currency = "USD")
        {
            try
            {
                var accessToken = await GetAccessTokenAsync();
                
                var orderRequest = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            amount = new
                            {
                                currency_code = currency,
                                value = amount.ToString("F2", CultureInfo.InvariantCulture)
                            },
                            description = "Đơn hàng SportShop"
                        }
                    },
                    application_context = new
                    {
                        return_url = $"{_configuration["BaseUrl"]}/Cart/PayPalReturn",
                        cancel_url = $"{_configuration["BaseUrl"]}/Cart/PayPalCancel",
                        brand_name = "SportShop",
                        locale = "en-US",
                        landing_page = "BILLING",
                        shipping_preference = "NO_SHIPPING",
                        user_action = "PAY_NOW"
                    }
                };

                var json = JsonSerializer.Serialize(orderRequest);
                
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var orderResponse = JsonSerializer.Deserialize<PayPalOrderResponse>(jsonResponse);
                    return orderResponse ?? throw new Exception("Order response is null");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create PayPal order. Status: {response.StatusCode}, Error: {errorContent}");
            }
            catch (Exception ex)
            {
                throw new Exception($"PayPal order creation error: {ex.Message}", ex);
            }
        }

        // Capture Payment
        public async Task<PayPalCaptureResponse> CaptureOrderAsync(string orderId)
        {
            try
            {
                Console.WriteLine($"[PayPal] Capturing order: {orderId}");
                var accessToken = await GetAccessTokenAsync();
                
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/checkout/orders/{orderId}/capture");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

                Console.WriteLine($"[PayPal] Sending capture request...");
                var response = await _httpClient.SendAsync(request);
                Console.WriteLine($"[PayPal] Capture response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[PayPal] Capture response: {jsonResponse}");
                    var captureResponse = JsonSerializer.Deserialize<PayPalCaptureResponse>(jsonResponse);
                    return captureResponse ?? throw new Exception("Capture response is null");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[PayPal] Capture error: {errorContent}");
                throw new Exception($"Failed to capture PayPal payment. Status: {response.StatusCode}, Error: {errorContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PayPal] Capture exception: {ex.Message}");
                throw new Exception($"PayPal capture error: {ex.Message}", ex);
            }
        }
    }

    // PayPal Response Models
    public class PayPalTokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }

    public class PayPalOrderResponse
    {
        public string id { get; set; }
        public string status { get; set; }
        public PayPalLink[] links { get; set; }
    }

    public class PayPalLink
    {
        public string href { get; set; }
        public string rel { get; set; }
        public string method { get; set; }
    }

    public class PayPalCaptureResponse
    {
        public string id { get; set; }
        public string status { get; set; }
        public PayPalPurchaseUnit[] purchase_units { get; set; }
    }

    public class PayPalPurchaseUnit
    {
        public PayPalPayments payments { get; set; }
    }

    public class PayPalPayments
    {
        public PayPalCapture[] captures { get; set; }
    }

    public class PayPalCapture
    {
        public string id { get; set; }
        public string status { get; set; }
        public PayPalAmount amount { get; set; }
    }

    public class PayPalAmount
    {
        public string currency_code { get; set; }
        public string value { get; set; }
    }
}
