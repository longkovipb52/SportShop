using Microsoft.AspNetCore.Mvc;
using SportShop.Services;

namespace SportShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;

        public ChatbotController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { error = "Message is required" });
                }

                // Lấy UserID từ session nếu có
                var userId = HttpContext.Session.GetInt32("UserId");

                var response = await _chatbotService.GetResponseAsync(request.Message, userId);

                return Ok(new ChatResponse
                {
                    Message = response,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
    }

    public class ChatResponse
    {
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}
