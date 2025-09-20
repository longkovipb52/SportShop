using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index(string searchString, int? categoryId, int? brandId, 
            decimal? minPrice, decimal? maxPrice, string stockStatus, int page = 1, int pageSize = 10)
        {
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentBrand = brandId;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentStockStatus = stockStatus;

            // Populate dropdowns
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "Name");
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "Name");

            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Attributes)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) || 
                                            p.Description.Contains(searchString));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                products = products.Where(p => p.CategoryID == categoryId);
            }

            if (brandId.HasValue && brandId > 0)
            {
                products = products.Where(p => p.BrandID == brandId);
            }

            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice);
            }

            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice);
            }

            if (!string.IsNullOrEmpty(stockStatus))
            {
                switch (stockStatus)
                {
                    case "in-stock":
                        products = products.Where(p => p.Stock > 0);
                        break;
                    case "low-stock":
                        products = products.Where(p => p.Stock > 0 && p.Stock <= 10);
                        break;
                    case "out-of-stock":
                        products = products.Where(p => p.Stock <= 0);
                        break;
                }
            }

            var totalItems = await products.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var productList = await products
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new ProductListViewModel
            {
                Products = productList,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                SearchString = searchString,
                CategoryId = categoryId,
                BrandId = brandId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                StockStatus = stockStatus
            };

            return View(viewModel);
        }

        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Attributes)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .Include(p => p.OrderItems)
                    .ThenInclude(oi => oi.Order)
                .Include(p => p.Wishlists)
                .FirstOrDefaultAsync(m => m.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "Name");
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "Name");
            
            var viewModel = new ProductCreateViewModel();
            return View(viewModel);
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description ?? string.Empty,
                    Price = viewModel.Price,
                    Stock = viewModel.Stock,
                    CategoryID = viewModel.CategoryID,
                    BrandID = viewModel.BrandID,
                    CreatedAt = DateTime.Now
                };

                // Handle image upload
                if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "upload", "product");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.ImageFile.CopyToAsync(fileStream);
                    }

                    product.ImageURL = uniqueFileName;
                }

                _context.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Sản phẩm đã được thêm thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Reload dropdowns if validation fails
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "Name", viewModel.CategoryID);
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "Name", viewModel.BrandID);
            
            return View(viewModel);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "Name", product.CategoryID);
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "Name", product.BrandID);

            var viewModel = new ProductEditViewModel
            {
                ProductID = product.ProductID,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                CategoryID = product.CategoryID,
                BrandID = product.BrandID ?? 0,
                CurrentImageURL = product.ImageURL
            };

            return View(viewModel);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel viewModel)
        {
            if (id != viewModel.ProductID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var product = await _context.Products.FindAsync(id);
                    if (product == null)
                    {
                        return NotFound();
                    }

                    product.Name = viewModel.Name;
                    product.Description = viewModel.Description ?? string.Empty;
                    product.Price = viewModel.Price;
                    product.Stock = viewModel.Stock;
                    product.CategoryID = viewModel.CategoryID;
                    product.BrandID = viewModel.BrandID;

                    // Handle image upload
                    if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(product.ImageURL))
                        {
                            var oldImagePath = Path.Combine(_environment.WebRootPath, "upload", "product", product.ImageURL);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Upload new image
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "upload", "product");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.ImageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await viewModel.ImageFile.CopyToAsync(fileStream);
                        }

                        product.ImageURL = uniqueFileName;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(viewModel.ProductID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Reload dropdowns if validation fails
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "Name", viewModel.CategoryID);
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "Name", viewModel.BrandID);
            
            return View(viewModel);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Attributes)
                .Include(p => p.Reviews)
                .Include(p => p.OrderItems)
                .Include(p => p.Carts)
                .Include(p => p.Wishlists)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
            }

            // Check if product is in any orders
            if (product.OrderItems.Any())
            {
                return Json(new { success = false, message = "Không thể xóa sản phẩm đã có trong đơn hàng!" });
            }

            try
            {
                // Remove related data first
                _context.ProductAttributes.RemoveRange(product.Attributes);
                _context.Reviews.RemoveRange(product.Reviews);
                _context.Carts.RemoveRange(product.Carts);
                _context.Wishlists.RemoveRange(product.Wishlists);

                // Delete product image if exists
                if (!string.IsNullOrEmpty(product.ImageURL))
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, "upload", "product", product.ImageURL);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Sản phẩm đã được xóa thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa sản phẩm: " + ex.Message });
            }
        }

        // POST: Admin/Products/ToggleOutOfStock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleOutOfStock(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
            }

            // Toggle stock status (set to 0 if > 0, restore to 1 if = 0)
            if (product.Stock > 0)
            {
                product.Stock = 0; // Mark as out of stock
            }
            else
            {
                product.Stock = 1; // Restore stock (minimal amount)
            }

            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();

                var status = product.Stock > 0 ? "có hàng" : "hết hàng";
                return Json(new { success = true, message = $"Đã cập nhật trạng thái sản phẩm thành {status}!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // API: Get product statistics
        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            try
            {
                var totalProducts = await _context.Products.CountAsync();
                var inStockProducts = await _context.Products.CountAsync(p => p.Stock > 0);
                var outOfStockProducts = await _context.Products.CountAsync(p => p.Stock <= 0);
                var lowStockProducts = await _context.Products.CountAsync(p => p.Stock > 0 && p.Stock <= 10);

                var totalValue = await _context.Products.SumAsync(p => p.Price * p.Stock);

                return Json(new
                {
                    totalProducts,
                    inStockProducts,
                    outOfStockProducts,
                    lowStockProducts,
                    totalValue
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }
    }
}