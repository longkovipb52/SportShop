using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BrandsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public BrandsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Admin/Brands
        public async Task<IActionResult> Index(string search = "", int page = 1, int pageSize = 10)
        {
            ViewData["Title"] = "Quản lý thương hiệu";
            
            var query = _context.Brands.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Name.Contains(search) || b.Description.Contains(search));
                ViewData["CurrentSearch"] = search;
            }

            // Đếm tổng số bản ghi
            var totalRecords = await query.CountAsync();
            
            // Phân trang
            var brands = await query
                .OrderBy(b => b.BrandID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thông tin phân trang
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewData["TotalRecords"] = totalRecords;

            return View(brands);
        }

        // GET: Admin/Brands/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(m => m.BrandID == id);
                
            if (brand == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Chi tiết thương hiệu - {brand.Name}";
            return View(brand);
        }

        // GET: Admin/Brands/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Thêm thương hiệu mới";
            return View();
        }

        // POST: Admin/Brands/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand, IFormFile? logoFile)
        {
            // Remove Products from validation since it's not required for creation
            ModelState.Remove("Products");
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload logo
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        // Validate file type and size
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var fileExtension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            TempData["ErrorMessage"] = "Chỉ chấp nhận file hình ảnh (JPG, PNG, GIF)";
                            ViewData["Title"] = "Thêm thương hiệu mới";
                            return View(brand);
                        }
                        
                        if (logoFile.Length > 5 * 1024 * 1024) // 5MB
                        {
                            TempData["ErrorMessage"] = "Kích thước file không được vượt quá 5MB";
                            ViewData["Title"] = "Thêm thương hiệu mới";
                            return View(brand);
                        }

                        var fileName = await SaveLogoAsync(logoFile);
                        brand.Logo = fileName;
                    }

                    brand.CreatedAt = DateTime.Now;
                    _context.Add(brand);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Thêm thương hiệu thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                }
            }

            ViewData["Title"] = "Thêm thương hiệu mới";
            return View(brand);
        }

        // GET: Admin/Brands/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Chỉnh sửa thương hiệu - {brand.Name}";
            return View(brand);
        }

        // POST: Admin/Brands/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand, IFormFile? logoFile)
        {
            if (id != brand.BrandID)
            {
                return NotFound();
            }

            // Remove Products from validation since it's not required for editing
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBrand = await _context.Brands.FindAsync(id);
                    if (existingBrand == null)
                    {
                        return NotFound();
                    }

                    // Xử lý upload logo mới
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        // Validate file type and size
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var fileExtension = Path.GetExtension(logoFile.FileName).ToLowerInvariant();
                        
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            TempData["ErrorMessage"] = "Chỉ chấp nhận file hình ảnh (JPG, PNG, GIF)";
                            ViewData["Title"] = $"Chỉnh sửa thương hiệu - {brand.Name}";
                            return View(brand);
                        }
                        
                        if (logoFile.Length > 5 * 1024 * 1024) // 5MB
                        {
                            TempData["ErrorMessage"] = "Kích thước file không được vượt quá 5MB";
                            ViewData["Title"] = $"Chỉnh sửa thương hiệu - {brand.Name}";
                            return View(brand);
                        }

                        // Xóa logo cũ nếu có
                        if (!string.IsNullOrEmpty(existingBrand.Logo))
                        {
                            DeleteOldLogo(existingBrand.Logo);
                        }

                        var fileName = await SaveLogoAsync(logoFile);
                        existingBrand.Logo = fileName;
                    }

                    // Cập nhật thông tin
                    existingBrand.Name = brand.Name;
                    existingBrand.Description = brand.Description;

                    _context.Update(existingBrand);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Cập nhật thương hiệu thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.BrandID))
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

            ViewData["Title"] = $"Chỉnh sửa thương hiệu - {brand.Name}";
            return View(brand);
        }

        // POST: Admin/Brands/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var brand = await _context.Brands
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.BrandID == id);
                    
                if (brand == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thương hiệu" });
                }

                // Kiểm tra xem thương hiệu có sản phẩm không
                if (brand.Products != null && brand.Products.Count > 0)
                {
                    return Json(new { success = false, message = $"Không thể xóa thương hiệu này vì có {brand.Products.Count} sản phẩm thuộc thương hiệu này" });
                }

                // Xóa logo nếu có
                if (!string.IsNullOrEmpty(brand.Logo))
                {
                    DeleteOldLogo(brand.Logo);
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Xóa thương hiệu thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // API endpoint để lấy thống kê thương hiệu
        [HttpGet]
        public async Task<IActionResult> GetBrandStats(int id)
        {
            try
            {
                var brand = await _context.Brands
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.BrandID == id);

                if (brand == null)
                {
                    return NotFound();
                }

                var totalProducts = brand.Products?.Count ?? 0;
                var totalStock = brand.Products?.Sum(p => p.Stock) ?? 0;
                var outOfStock = brand.Products?.Count(p => p.Stock <= 0) ?? 0;

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

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.BrandID == id);
        }

        private async Task<string> SaveLogoAsync(IFormFile logoFile)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "upload", "branch");
            
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(logoFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }

        private void DeleteOldLogo(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return;

            var filePath = Path.Combine(_environment.WebRootPath, "upload", "branch", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        #endregion
    }
}