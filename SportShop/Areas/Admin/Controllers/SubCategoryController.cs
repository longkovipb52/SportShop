using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const int PageSize = 10;

        public SubCategoryController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/SubCategory
        public async Task<IActionResult> Index(string searchString, int? categoryId, int page = 1)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CategoryFilter"] = categoryId;
            
            // Load categories for filter dropdown
            ViewData["Categories"] = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                "CategoryID",
                "Name"
            );

            var subCategoriesQuery = _context.SubCategories
                .Include(s => s.Category)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchString))
            {
                subCategoriesQuery = subCategoriesQuery.Where(s =>
                    s.Name.Contains(searchString) ||
                    (s.Description != null && s.Description.Contains(searchString))
                );
            }

            // Apply category filter
            if (categoryId.HasValue)
            {
                subCategoriesQuery = subCategoriesQuery.Where(s => s.CategoryID == categoryId.Value);
            }

            // Get total count for pagination
            var totalItems = await subCategoriesQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Apply pagination and ordering
            var subCategories = await subCategoriesQuery
                .OrderBy(s => s.Category!.Name)
                .ThenBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["PageSize"] = PageSize;
            ViewData["TotalItems"] = totalItems;

            return View(subCategories);
        }

        // GET: Admin/SubCategory/Create
        public async Task<IActionResult> Create()
        {
            ViewData["Categories"] = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                "CategoryID",
                "Name"
            );
            return View();
        }

        // POST: Admin/SubCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategory subCategory, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate name in the same category
                var existingSubCategory = await _context.SubCategories
                    .FirstOrDefaultAsync(s => s.Name == subCategory.Name && s.CategoryID == subCategory.CategoryID);

                if (existingSubCategory != null)
                {
                    ModelState.AddModelError("Name", "Tên danh mục con đã tồn tại trong danh mục này.");
                    ViewData["Categories"] = new SelectList(
                        await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                        "CategoryID",
                        "Name",
                        subCategory.CategoryID
                    );
                    return View(subCategory);
                }

                // Handle image upload
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "upload", "subcategories");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    subCategory.ImageURL = "/upload/subcategories/" + uniqueFileName;
                }

                _context.Add(subCategory);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm danh mục con thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Categories"] = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                "CategoryID",
                "Name",
                subCategory.CategoryID
            );
            return View(subCategory);
        }

        // GET: Admin/SubCategory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subCategory = await _context.SubCategories.FindAsync(id);
            if (subCategory == null)
            {
                return NotFound();
            }

            ViewData["Categories"] = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                "CategoryID",
                "Name",
                subCategory.CategoryID
            );
            return View(subCategory);
        }

        // POST: Admin/SubCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SubCategory subCategory, IFormFile? imageFile, bool removeImage = false)
        {
            if (id != subCategory.SubCategoryID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate name in the same category (excluding current subcategory)
                    var existingSubCategory = await _context.SubCategories
                        .FirstOrDefaultAsync(s => s.Name == subCategory.Name 
                            && s.CategoryID == subCategory.CategoryID 
                            && s.SubCategoryID != id);

                    if (existingSubCategory != null)
                    {
                        ModelState.AddModelError("Name", "Tên danh mục con đã tồn tại trong danh mục này.");
                        ViewData["Categories"] = new SelectList(
                            await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                            "CategoryID",
                            "Name",
                            subCategory.CategoryID
                        );
                        return View(subCategory);
                    }

                    var existingEntity = await _context.SubCategories.AsNoTracking().FirstOrDefaultAsync(s => s.SubCategoryID == id);
                    var oldImageUrl = existingEntity?.ImageURL;

                    // Handle remove image
                    if (removeImage && !string.IsNullOrEmpty(oldImageUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, oldImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                        subCategory.ImageURL = null;
                    }
                    // Handle new image upload
                    else if (imageFile != null && imageFile.Length > 0)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(oldImageUrl))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, oldImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Save new image
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "upload", "subcategories");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        subCategory.ImageURL = "/upload/subcategories/" + uniqueFileName;
                    }
                    else
                    {
                        // Keep old image
                        subCategory.ImageURL = oldImageUrl;
                    }

                    _context.Update(subCategory);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật danh mục con thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubCategoryExists(subCategory.SubCategoryID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["Categories"] = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                "CategoryID",
                "Name",
                subCategory.CategoryID
            );
            return View(subCategory);
        }

        // POST: Admin/SubCategory/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var subCategory = await _context.SubCategories.FindAsync(id);
            if (subCategory == null)
            {
                return Json(new { success = false, message = "Không tìm thấy danh mục con." });
            }

            // Check if subcategory has products
            var hasProducts = await _context.Products.AnyAsync(p => p.SubCategoryID == id);
            if (hasProducts)
            {
                return Json(new { success = false, message = "Không thể xóa danh mục con vì đang có sản phẩm sử dụng." });
            }

            // Delete image if exists
            if (!string.IsNullOrEmpty(subCategory.ImageURL))
            {
                var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, subCategory.ImageURL.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.SubCategories.Remove(subCategory);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa danh mục con thành công!" });
        }

        // POST: Admin/SubCategory/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var subCategory = await _context.SubCategories.FindAsync(id);
            if (subCategory == null)
            {
                return Json(new { success = false, message = "Không tìm thấy danh mục con." });
            }

            subCategory.IsActive = !subCategory.IsActive;
            await _context.SaveChangesAsync();

            var status = subCategory.IsActive ? "kích hoạt" : "vô hiệu hóa";
            return Json(new { 
                success = true, 
                message = $"Đã {status} danh mục con thành công!",
                isActive = subCategory.IsActive
            });
        }

        private bool SubCategoryExists(int id)
        {
            return _context.SubCategories.Any(e => e.SubCategoryID == id);
        }
    }
}
