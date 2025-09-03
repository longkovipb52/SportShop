using Microsoft.AspNetCore.Mvc;

namespace SportShop.Controllers
{
    public class PlaceholderController : Controller
    {
        [Route("placeholder.svg")]
        public IActionResult GeneratePlaceholder(int width = 300, int height = 200, string text = "Placeholder")
        {
            // Tạo ảnh placeholder SVG đơn giản
            var svg = $@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{height}"" viewBox=""0 0 {width} {height}"">
                <rect width=""{width}"" height=""{height}"" fill=""#f8f9fa"" />
                <text x=""{width/2}"" y=""{height/2}"" font-family=""Arial"" font-size=""24"" fill=""#6c757d"" text-anchor=""middle"" dominant-baseline=""middle"">{text}</text>
            </svg>";

            return Content(svg, "image/svg+xml");
        }
    }
}