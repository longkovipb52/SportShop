using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using System.Threading.Tasks;

namespace SportShop.ViewComponents
{
    public class WishlistCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public WishlistCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                
                if (!userId.HasValue)
                {
                    return Content("0");
                }

                var count = await _context.Wishlists
                    .CountAsync(w => w.UserID == userId.Value);

                return Content(count.ToString());
            }
            catch (System.Exception)
            {
                return Content("0");
            }
        }
    }
}
