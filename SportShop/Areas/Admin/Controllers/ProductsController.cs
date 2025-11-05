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

            var product = await _context.Products
                .Include(p => p.Attributes)
                .FirstOrDefaultAsync(p => p.ProductID == id);
                
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "Name", product.CategoryID);
            ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "BrandID", "Name", product.BrandID);
            
            // Load SubCategories for the selected Category
            if (product.CategoryID > 0)
            {
                ViewBag.SubCategories = new SelectList(
                    await _context.SubCategories
                        .Where(sc => sc.CategoryID == product.CategoryID && sc.IsActive)
                        .OrderBy(sc => sc.DisplayOrder)
                        .ToListAsync(), 
                    "SubCategoryID", "Name", product.SubCategoryID);
            }

            var viewModel = new ProductEditViewModel
            {
                ProductID = product.ProductID,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                CategoryID = product.CategoryID,
                SubCategoryID = product.SubCategoryID,
                BrandID = product.BrandID ?? 0,
                CurrentImageURL = product.ImageURL,
                Attributes = product.Attributes?.ToList() ?? new List<ProductAttribute>()
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

        [HttpGet]
        public async Task<IActionResult> GetAttributes(int productId)
        {
            try
            {
                var attributes = await _context.ProductAttributes
                    .Where(a => a.ProductID == productId)
                    .Select(a => new
                    {
                        a.AttributeID,
                        a.ProductID,
                        Size = a.Size ?? "",
                        Color = a.Color ?? "",
                        a.Stock,
                        a.Price,
                        ImageURL = a.ImageURL ?? ""
                    })
                    .OrderBy(a => a.Size)
                    .ThenBy(a => a.Color)
                    .ToListAsync();

                return Json(new { success = true, data = attributes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tải thuộc tính: " + ex.Message });
            }
        }

        // API: Get single attribute
        [HttpGet]
        public async Task<IActionResult> GetAttribute(int id)
        {
            try
            {
                var attribute = await _context.ProductAttributes.FindAsync(id);
                if (attribute == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thuộc tính" });
                }

                // Return only the data we need (no navigation properties)
                var result = new
                {
                    attribute.AttributeID,
                    attribute.ProductID,
                    Size = attribute.Size ?? "",
                    Color = attribute.Color ?? "",
                    attribute.Stock,
                    attribute.Price,
                    ImageURL = attribute.ImageURL ?? ""
                };

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // API: Create attribute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAttribute([FromForm] ProductAttributeViewModel model)
        {
            try
            {
                // Custom validation: Phải có ít nhất Size hoặc Color
                if (string.IsNullOrWhiteSpace(model.Size) && string.IsNullOrWhiteSpace(model.Color))
                {
                    return Json(new { success = false, message = "Vui lòng nhập ít nhất một trong hai: Kích thước hoặc Màu sắc" });
                }

                // Nếu có SizeOptionID, lấy Size từ master data
                if (model.SizeOptionID.HasValue && model.SizeOptionID > 0)
                {
                    var sizeOption = await _context.SizeOptions.FindAsync(model.SizeOptionID.Value);
                    if (sizeOption != null)
                    {
                        model.Size = sizeOption.SizeName;
                    }
                }

                // Nếu có ColorOptionID, lấy Color từ master data
                if (model.ColorOptionID.HasValue && model.ColorOptionID > 0)
                {
                    var colorOption = await _context.ColorOptions.FindAsync(model.ColorOptionID.Value);
                    if (colorOption != null)
                    {
                        model.Color = colorOption.ColorName;
                    }
                }

                // Get product to check stock limit
                var product = await _context.Products.FindAsync(model.ProductID);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                // Check total stock of existing attributes
                var existingAttributesStock = await _context.ProductAttributes
                    .Where(a => a.ProductID == model.ProductID)
                    .SumAsync(a => a.Stock);

                // Check if new stock would exceed product stock
                if (existingAttributesStock + model.Stock > product.Stock)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Tổng tồn kho thuộc tính ({existingAttributesStock + model.Stock}) không được vượt quá tồn kho sản phẩm ({product.Stock}). Hiện tại đã phân bổ {existingAttributesStock}, còn lại {product.Stock - existingAttributesStock}" 
                    });
                }

                // Check if combination already exists (so sánh theo Size và Color, bỏ qua các giá trị null)
                var normalizedSize = string.IsNullOrWhiteSpace(model.Size) ? null : model.Size.Trim();
                var normalizedColor = string.IsNullOrWhiteSpace(model.Color) ? null : model.Color.Trim();

                var exists = await _context.ProductAttributes
                    .AnyAsync(a => a.ProductID == model.ProductID && 
                                   a.Size == normalizedSize && 
                                   a.Color == normalizedColor);

                if (exists)
                {
                    return Json(new { success = false, message = "Thuộc tính với kích thước và màu sắc này đã tồn tại" });
                }

                var attribute = new ProductAttribute
                {
                    ProductID = model.ProductID,
                    Size = normalizedSize,
                    Color = normalizedColor,
                    Stock = model.Stock,
                    Price = model.Price,
                    SizeOptionID = model.SizeOptionID > 0 ? model.SizeOptionID : null,
                    ColorOptionID = model.ColorOptionID > 0 ? model.ColorOptionID : null
                };

                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "upload", "attributes");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    attribute.ImageURL = "/upload/attributes/" + uniqueFileName;
                }

                _context.ProductAttributes.Add(attribute);
                await _context.SaveChangesAsync();

                // Return only the data we need (no navigation properties)
                var result = new
                {
                    attribute.AttributeID,
                    attribute.ProductID,
                    Size = attribute.Size ?? "",
                    Color = attribute.Color ?? "",
                    attribute.Stock,
                    attribute.Price,
                    ImageURL = attribute.ImageURL ?? ""
                };

                return Json(new { success = true, message = "Thêm thuộc tính thành công", data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // API: Update attribute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttribute([FromForm] ProductAttributeViewModel model)
        {
            try
            {
                // Custom validation: Phải có ít nhất Size hoặc Color
                if (string.IsNullOrWhiteSpace(model.Size) && string.IsNullOrWhiteSpace(model.Color))
                {
                    return Json(new { success = false, message = "Vui lòng nhập ít nhất một trong hai: Kích thước hoặc Màu sắc" });
                }

                // Nếu có SizeOptionID, lấy Size từ master data
                if (model.SizeOptionID.HasValue && model.SizeOptionID > 0)
                {
                    var sizeOption = await _context.SizeOptions.FindAsync(model.SizeOptionID.Value);
                    if (sizeOption != null)
                    {
                        model.Size = sizeOption.SizeName;
                    }
                }

                // Nếu có ColorOptionID, lấy Color từ master data
                if (model.ColorOptionID.HasValue && model.ColorOptionID > 0)
                {
                    var colorOption = await _context.ColorOptions.FindAsync(model.ColorOptionID.Value);
                    if (colorOption != null)
                    {
                        model.Color = colorOption.ColorName;
                    }
                }

                var attribute = await _context.ProductAttributes.FindAsync(model.AttributeID);
                if (attribute == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thuộc tính" });
                }

                // Get product to check stock limit
                var product = await _context.Products.FindAsync(model.ProductID);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                // Check total stock of existing attributes (excluding current one being updated)
                var existingAttributesStock = await _context.ProductAttributes
                    .Where(a => a.ProductID == model.ProductID && a.AttributeID != model.AttributeID)
                    .SumAsync(a => a.Stock);

                // Check if new stock would exceed product stock
                if (existingAttributesStock + model.Stock > product.Stock)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Tổng tồn kho thuộc tính ({existingAttributesStock + model.Stock}) không được vượt quá tồn kho sản phẩm ({product.Stock}). Các thuộc tính khác đã phân bổ {existingAttributesStock}, còn lại {product.Stock - existingAttributesStock}" 
                    });
                }

                // Normalize Size và Color
                var normalizedSize = string.IsNullOrWhiteSpace(model.Size) ? null : model.Size.Trim();
                var normalizedColor = string.IsNullOrWhiteSpace(model.Color) ? null : model.Color.Trim();

                // Check if new combination already exists (excluding current record)
                var exists = await _context.ProductAttributes
                    .AnyAsync(a => a.ProductID == model.ProductID && 
                                   a.Size == normalizedSize && 
                                   a.Color == normalizedColor &&
                                   a.AttributeID != model.AttributeID);

                if (exists)
                {
                    return Json(new { success = false, message = "Thuộc tính với kích thước và màu sắc này đã tồn tại" });
                }

                attribute.Size = normalizedSize;
                attribute.Color = normalizedColor;
                attribute.Stock = model.Stock;
                attribute.Price = model.Price;
                attribute.SizeOptionID = model.SizeOptionID > 0 ? model.SizeOptionID : null;
                attribute.ColorOptionID = model.ColorOptionID > 0 ? model.ColorOptionID : null;

                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(attribute.ImageURL))
                    {
                        var oldImagePath = Path.Combine(_environment.WebRootPath, attribute.ImageURL.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "upload", "attributes");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    attribute.ImageURL = "/upload/attributes/" + uniqueFileName;
                }

                _context.Update(attribute);
                await _context.SaveChangesAsync();

                // Return only the data we need (no navigation properties)
                var result = new
                {
                    attribute.AttributeID,
                    attribute.ProductID,
                    Size = attribute.Size ?? "",
                    Color = attribute.Color ?? "",
                    attribute.Stock,
                    attribute.Price,
                    ImageURL = attribute.ImageURL ?? ""
                };

                return Json(new { success = true, message = "Cập nhật thuộc tính thành công", data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // API: Delete attribute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttribute(int id)
        {
            try
            {
                var attribute = await _context.ProductAttributes.FindAsync(id);
                if (attribute == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thuộc tính" });
                }

                // Check if attribute is being used in cart or orders
                var isInCart = await _context.Carts.AnyAsync(c => c.AttributeID == id);
                var isInOrder = await _context.OrderItems.AnyAsync(o => o.AttributeID == id);

                if (isInCart || isInOrder)
                {
                    return Json(new { success = false, message = "Không thể xóa thuộc tính đang được sử dụng trong giỏ hàng hoặc đơn hàng" });
                }

                // Delete image if exists
                if (!string.IsNullOrEmpty(attribute.ImageURL))
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, attribute.ImageURL.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.ProductAttributes.Remove(attribute);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa thuộc tính thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // API: Get size options by subcategory
        [HttpGet]
        public async Task<IActionResult> GetSizeOptions(int? subCategoryId)
        {
            try
            {
                var query = _context.SizeOptions.Where(s => s.IsActive);

                // Filter by subcategory if provided
                if (subCategoryId.HasValue && subCategoryId > 0)
                {
                    query = query.Where(s => s.SubCategoryID == subCategoryId);
                }

                var sizeOptions = await query
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new
                    {
                        s.SizeOptionID,
                        s.SizeName,
                        s.SubCategoryID
                    })
                    .ToListAsync();

                return Json(new { success = true, data = sizeOptions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // API: Get color options
        [HttpGet]
        public async Task<IActionResult> GetColorOptions()
        {
            try
            {
                var colorOptions = await _context.ColorOptions
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.ColorName)
                    .Select(c => new
                    {
                        c.ColorOptionID,
                        c.ColorName,
                        c.HexCode
                    })
                    .ToListAsync();

                return Json(new { success = true, data = colorOptions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductID == id);
        }
    }
}