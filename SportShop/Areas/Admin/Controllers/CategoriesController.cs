using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CategoriesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Admin/Categories
        public async Task<IActionResult> Index(string search = "", int page = 1, int pageSize = 10)
        {
            ViewData["Title"] = "Quản lý danh mục";
            
            var query = _context.Categories.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Description.Contains(search));
                ViewData["CurrentSearch"] = search;
            }

            // Đếm tổng số bản ghi
            var totalRecords = await query.CountAsync();
            
            // Phân trang
            var categories = await query
                .OrderBy(c => c.CategoryID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thông tin phân trang
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewData["TotalRecords"] = totalRecords;

            return View(categories);
        }

        // GET: Admin/Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.CategoryID == id);
                
            if (category == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Chi tiết danh mục - {category.Name}";
            return View(category);
        }

        // GET: Admin/Categories/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Thêm danh mục mới";
            return View();
        }

        // POST: Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile? imageFile)
        {
            // Remove Products from validation since it's not required for creation
            ModelState.Remove("Products");
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload hình ảnh
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Validate file type and size
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            TempData["ErrorMessage"] = "Chỉ chấp nhận file hình ảnh (JPG, PNG, GIF)";
                            ViewData["Title"] = "Thêm danh mục mới";
                            return View(category);
                        }
                        
                        if (imageFile.Length > 5 * 1024 * 1024) // 5MB
                        {
                            TempData["ErrorMessage"] = "Kích thước file không được vượt quá 5MB";
                            ViewData["Title"] = "Thêm danh mục mới";
                            return View(category);
                        }

                        var fileName = await SaveImageAsync(imageFile);
                        category.ImageURL = fileName;
                    }

                    _context.Add(category);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Thêm danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                }
            }

            ViewData["Title"] = "Thêm danh mục mới";
            return View(category);
        }

        // GET: Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Chỉnh sửa danh mục - {category.Name}";
            return View(category);
        }

        // POST: Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category, IFormFile? imageFile)
        {
            if (id != category.CategoryID)
            {
                return NotFound();
            }

            // Remove Products from validation since it's not required for editing
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCategory = await _context.Categories.FindAsync(id);
                    if (existingCategory == null)
                    {
                        return NotFound();
                    }

                    // Xử lý upload hình ảnh mới
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Validate file type and size
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            TempData["ErrorMessage"] = "Chỉ chấp nhận file hình ảnh (JPG, PNG, GIF)";
                            ViewData["Title"] = $"Chỉnh sửa danh mục - {category.Name}";
                            return View(category);
                        }
                        
                        if (imageFile.Length > 5 * 1024 * 1024) // 5MB
                        {
                            TempData["ErrorMessage"] = "Kích thước file không được vượt quá 5MB";
                            ViewData["Title"] = $"Chỉnh sửa danh mục - {category.Name}";
                            return View(category);
                        }

                        // Xóa hình cũ nếu có
                        if (!string.IsNullOrEmpty(existingCategory.ImageURL))
                        {
                            DeleteOldImage(existingCategory.ImageURL);
                        }

                        var fileName = await SaveImageAsync(imageFile);
                        existingCategory.ImageURL = fileName;
                    }

                    // Cập nhật thông tin
                    existingCategory.Name = category.Name;
                    existingCategory.Description = category.Description;

                    _context.Update(existingCategory);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                }
            }

            ViewData["Title"] = $"Chỉnh sửa danh mục - {category.Name}";
            return View(category);
        }

        // POST: Admin/Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);
                    
                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                // Kiểm tra xem danh mục có sản phẩm không
                if (category.Products != null && category.Products.Count > 0)
                {
                    return Json(new { success = false, message = $"Không thể xóa danh mục này vì có {category.Products.Count} sản phẩm thuộc danh mục này" });
                }

                // Xóa hình ảnh nếu có
                if (!string.IsNullOrEmpty(category.ImageURL))
                {
                    DeleteOldImage(category.ImageURL);
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Xóa danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // API endpoint để lấy thống kê danh mục
        [HttpGet]
        public async Task<IActionResult> GetCategoryStats(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category == null)
                {
                    return NotFound();
                }

                var totalProducts = category.Products?.Count ?? 0;
                var totalStock = category.Products?.Sum(p => p.Stock) ?? 0;
                var outOfStock = category.Products?.Count(p => p.Stock <= 0) ?? 0;

                return Json(new
                {
                    totalProducts,
                    totalStock,
                    outOfStock,
                    inStock = totalProducts - outOfStock
                });
            }
            catch (Exception)
            {
                return Json(new { error = "Không thể tải thống kê" });
            }
        }

        #region Helper Methods

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryID == id);
        }

        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "upload", "category");
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }

        private void DeleteOldImage(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            var filePath = Path.Combine(_environment.WebRootPath, "upload", "category", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        #endregion
    }
}