using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SportShop.Services
{
    public class MoMoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _partnerCode;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _apiUrl;
        private readonly string _redirectUrl;
        private readonly string _ipnUrl;
        private readonly string _requestType;
        private readonly string _extraData;

        public MoMoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _partnerCode = _configuration["MoMo:PartnerCode"] ?? "";
            _accessKey = _configuration["MoMo:AccessKey"] ?? "";
            _secretKey = _configuration["MoMo:SecretKey"] ?? "";
            _apiUrl = _configuration["MoMo:ApiUrl"] ?? "";
            _redirectUrl = _configuration["MoMo:RedirectUrl"] ?? "";
            _ipnUrl = _configuration["MoMo:IpnUrl"] ?? "";
            _requestType = "captureWallet";
            _extraData = _configuration["MoMo:ExtraData"] ?? "";
        }

        public async Task<MoMoCreatePaymentResponse> CreatePaymentAsync(string orderId, long amount, string orderInfo)
        {
            try
            {
                // Tạo request ID duy nhất
                var requestId = Guid.NewGuid().ToString();
                
                // Tạo raw signature string
                var rawSignature = $"accessKey={_accessKey}&amount={amount}&extraData={_extraData}&ipnUrl={_ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={_partnerCode}&redirectUrl={_redirectUrl}&requestId={requestId}&requestType={_requestType}";
                
                // Tạo signature
                var signature = ComputeHmacSha256(rawSignature, _secretKey);

                // Tạo request object
                var request = new MoMoCreatePaymentRequest
                {
                    PartnerCode = _partnerCode,
                    AccessKey = _accessKey,
                    RequestId = requestId,
                    Amount = amount,
                    OrderId = orderId,
                    OrderInfo = orderInfo,
                    RedirectUrl = _redirectUrl,
                    IpnUrl = _ipnUrl,
                    ExtraData = _extraData,
                    RequestType = _requestType,
                    Signature = signature,
                    Lang = "vi",
                    PartnerName = "SportShop",
                    StoreId = "SportShop_Store",
                    AutoCapture = true,
                    OrderGroupId = ""
                };

                // Serialize request to JSON
                var jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Debug: Log request
                Console.WriteLine($"MoMo Request JSON: {jsonRequest}");

                // Gửi request đến MoMo API
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Debug: Log response
                Console.WriteLine($"MoMo Response JSON: {responseContent}");

                // Deserialize response
                var momoResponse = JsonSerializer.Deserialize<MoMoCreatePaymentResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return momoResponse ?? new MoMoCreatePaymentResponse();
            }
            catch (Exception ex)
            {
                // Log error (bạn có thể thêm logging ở đây)
                return new MoMoCreatePaymentResponse
                {
                    ResultCode = -1,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public bool VerifySignature(string signature, Dictionary<string, string> parameters)
        {
            try
            {
                // Theo doc MoMo, signature cho callback có thứ tự cố định
                var accessKey = parameters.GetValueOrDefault("accessKey", _accessKey);
                var amount = parameters.GetValueOrDefault("amount", "");
                var extraData = parameters.GetValueOrDefault("extraData", "");
                var message = parameters.GetValueOrDefault("message", "");
                var orderId = parameters.GetValueOrDefault("orderId", "");
                var orderInfo = parameters.GetValueOrDefault("orderInfo", "");
                var orderType = parameters.GetValueOrDefault("orderType", "");
                var partnerCode = parameters.GetValueOrDefault("partnerCode", "");
                var payType = parameters.GetValueOrDefault("payType", "");
                var requestId = parameters.GetValueOrDefault("requestId", "");
                var responseTime = parameters.GetValueOrDefault("responseTime", "");
                var resultCode = parameters.GetValueOrDefault("resultCode", "");
                var transId = parameters.GetValueOrDefault("transId", "");
                
                // Tạo raw signature string theo thứ tự MoMo callback
                var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
                
                // Tính toán signature
                var computedSignature = ComputeHmacSha256(rawSignature, _secretKey);
                
                return signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VerifySignature] Lỗi: {ex.Message}");
                return false;
            }
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }

    // Request Models
    public class MoMoCreatePaymentRequest
    {
        public string PartnerCode { get; set; } = "";
        public string AccessKey { get; set; } = "";
        public string RequestId { get; set; } = "";
        public long Amount { get; set; }
        public string OrderId { get; set; } = "";
        public string OrderInfo { get; set; } = "";
        public string RedirectUrl { get; set; } = "";
        public string IpnUrl { get; set; } = "";
        public string ExtraData { get; set; } = "";
        public string RequestType { get; set; } = "";
        public string Signature { get; set; } = "";
        public string Lang { get; set; } = "vi";
        public string PartnerName { get; set; } = "";
        public string StoreId { get; set; } = "";
        public string OrderGroupId { get; set; } = "";
        public bool AutoCapture { get; set; } = true;
    }

    // Response Models
    public class MoMoCreatePaymentResponse
    {
        public string PartnerCode { get; set; } = "";
        public string RequestId { get; set; } = "";
        public string OrderId { get; set; } = "";
        public long Amount { get; set; }
        public long ResponseTime { get; set; }
        public string Message { get; set; } = "";
        public int ResultCode { get; set; }
        public string PayUrl { get; set; } = "";
        public string DeepLink { get; set; } = "";
        public string QrCodeUrl { get; set; } = "";
    }

    public class MoMoPaymentResult
    {
        public string PartnerCode { get; set; } = "";
        public string OrderId { get; set; } = "";
        public string RequestId { get; set; } = "";
        public long Amount { get; set; }
        public string OrderInfo { get; set; } = "";
        public string OrderType { get; set; } = "";
        public string TransId { get; set; } = "";
        public int ResultCode { get; set; }
        public string Message { get; set; } = "";
        public string PayType { get; set; } = "";
        public long ResponseTime { get; set; }
        public string ExtraData { get; set; } = "";
        public string Signature { get; set; } = "";
    }
}
