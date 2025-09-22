using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Index & Search

        // GET: Admin/Reviews
        public async Task<IActionResult> Index(int page = 1, string searchString = "", string statusFilter = "", int? ratingFilter = null, string sortOrder = "")
        {
            ViewData["SearchString"] = searchString;
            ViewData["StatusFilter"] = statusFilter;
            ViewData["RatingFilter"] = ratingFilter;
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["RatingSortParm"] = sortOrder == "Rating" ? "rating_desc" : "Rating";
            ViewData["StatusSortParm"] = sortOrder == "Status" ? "status_desc" : "Status";
            ViewData["UserSortParm"] = sortOrder == "User" ? "user_desc" : "User";

            var reviews = _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                reviews = reviews.Where(r => 
                    (r.Comment != null && r.Comment.Contains(searchString)) ||
                    (r.Product != null && r.Product.Name.Contains(searchString)) ||
                    (r.User != null && r.User.Username.Contains(searchString)));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(statusFilter))
            {
                reviews = reviews.Where(r => r.Status == statusFilter);
            }

            // Lọc theo rating
            if (ratingFilter.HasValue)
            {
                reviews = reviews.Where(r => r.Rating == ratingFilter.Value);
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "User":
                    reviews = reviews.OrderBy(r => r.User.Username);
                    break;
                case "user_desc":
                    reviews = reviews.OrderByDescending(r => r.User.Username);
                    break;
                case "Date":
                    reviews = reviews.OrderBy(r => r.CreatedAt);
                    break;
                case "date_desc":
                    reviews = reviews.OrderByDescending(r => r.CreatedAt);
                    break;
                case "Rating":
                    reviews = reviews.OrderBy(r => r.Rating);
                    break;
                case "rating_desc":
                    reviews = reviews.OrderByDescending(r => r.Rating);
                    break;
                case "Status":
                    reviews = reviews.OrderBy(r => r.Status);
                    break;
                case "status_desc":
                    reviews = reviews.OrderByDescending(r => r.Status);
                    break;
                default:
                    reviews = reviews.OrderByDescending(r => r.CreatedAt);
                    break;
            }

            // Thống kê cho dashboard
            ViewData["TotalReviews"] = await _context.Reviews.CountAsync();
            ViewData["PendingReviews"] = await _context.Reviews.CountAsync(r => r.Status == "Pending");
            ViewData["ApprovedReviews"] = await _context.Reviews.CountAsync(r => r.Status == "Approved");
            ViewData["RejectedReviews"] = await _context.Reviews.CountAsync(r => r.Status == "Rejected");
            ViewData["AverageRating"] = await _context.Reviews.Where(r => r.Rating.HasValue).AverageAsync(r => (double?)r.Rating) ?? 0;

            // Phân trang
            int pageSize = 10;
            int totalCount = await reviews.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalCount;
            ViewData["PageSize"] = pageSize;

            var pagedReviews = await reviews
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(pagedReviews);
        }

        #endregion

        #region Details

        // GET: Admin/Reviews/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReviewID == id);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        #endregion

        #region Edit

        // GET: Admin/Reviews/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var review = await _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.ReviewID == id);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        // POST: Admin/Reviews/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Review review)
        {
            if (id != review.ReviewID)
            {
                return NotFound();
            }

            try
            {
                var existingReview = await _context.Reviews.FindAsync(id);
                if (existingReview == null)
                {
                    return NotFound();
                }

                // Chỉ cho phép cập nhật Status và Comment
                existingReview.Status = review.Status;
                existingReview.Comment = review.Comment;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đánh giá đã được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReviewExists(review.ReviewID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        #endregion

        #region Delete

        // POST: Admin/Reviews/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đánh giá đã được xóa thành công!" });
            }
            return Json(new { success = false, message = "Không tìm thấy đánh giá!" });
        }

        #endregion

        #region Bulk Actions

        // POST: Admin/Reviews/BulkAction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string action, int[] selectedIds)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một đánh giá!" });
            }

            try
            {
                var reviews = await _context.Reviews.Where(r => selectedIds.Contains(r.ReviewID)).ToListAsync();

                switch (action)
                {
                    case "approve":
                        foreach (var review in reviews)
                        {
                            review.Status = "Approved";
                        }
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, message = $"Đã duyệt {reviews.Count} đánh giá!" });

                    case "reject":
                        foreach (var review in reviews)
                        {
                            review.Status = "Rejected";
                        }
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, message = $"Đã từ chối {reviews.Count} đánh giá!" });

                    case "delete":
                        _context.Reviews.RemoveRange(reviews);
                        await _context.SaveChangesAsync();
                        return Json(new { success = true, message = $"Đã xóa {reviews.Count} đánh giá!" });

                    default:
                        return Json(new { success = false, message = "Hành động không hợp lệ!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.ReviewID == id);
        }

        #endregion
    }
}