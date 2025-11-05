using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AttributeManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AttributeManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        #region View Actions

        // GET: Admin/AttributeManagement/Sizes
        public async Task<IActionResult> Sizes(int? subCategoryId, int page = 1, int pageSize = 20)
        {
            ViewData["Title"] = "Quản lý Kích thước";
            
            var query = _context.SizeOptions
                .Include(s => s.SubCategory)
                    .ThenInclude(sc => sc!.Category)
                .AsQueryable();

            // Filter by SubCategory
            if (subCategoryId.HasValue && subCategoryId > 0)
            {
                query = query.Where(s => s.SubCategoryID == subCategoryId);
                ViewData["CurrentSubCategory"] = subCategoryId;
            }

            var totalRecords = await query.CountAsync();
            
            var sizes = await query
                .OrderBy(s => s.SubCategory!.Category!.Name)
                .ThenBy(s => s.SubCategory!.Name)
                .ThenBy(s => s.DisplayOrder)
                .ThenBy(s => s.SizeName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Load SubCategories for filter
            ViewBag.SubCategories = await _context.SubCategories
                .Include(sc => sc.Category)
                .Where(sc => sc.IsActive)
                .OrderBy(sc => sc.Category!.Name)
                .ThenBy(sc => sc.Name)
                .ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewData["TotalRecords"] = totalRecords;

            return View(sizes);
        }

        // GET: Admin/AttributeManagement/Colors
        public async Task<IActionResult> Colors(string search = "", int page = 1, int pageSize = 20)
        {
            ViewData["Title"] = "Quản lý Màu sắc";
            
            var query = _context.ColorOptions.AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.ColorName.Contains(search));
                ViewData["CurrentSearch"] = search;
            }

            var totalRecords = await query.CountAsync();
            
            var colors = await query
                .OrderBy(c => c.ColorName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewData["TotalRecords"] = totalRecords;

            return View(colors);
        }

        #endregion

        #region Size Options API

        // GET: Admin/AttributeManagement/GetSizes?categoryId=1
        [HttpGet]
        public async Task<IActionResult> GetSizes(int? categoryId)
        {
            try
            {
                var query = _context.SizeOptions.AsQueryable();

                if (categoryId.HasValue)
                {
                    query = query.Where(s => s.SubCategoryID == categoryId && s.IsActive);
                }
                else
                {
                    query = query.Where(s => s.IsActive);
                }

                var sizes = await query
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.SizeName)
                    .Select(s => new
                    {
                        sizeOptionID = s.SizeOptionID,
                        sizeName = s.SizeName,
                        subCategoryID = s.SubCategoryID,
                        displayOrder = s.DisplayOrder
                    })
                    .ToListAsync();

                return Json(new { success = true, data = sizes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tải danh sách kích thước: " + ex.Message });
            }
        }

        // POST: Admin/AttributeManagement/CreateSize
        [HttpPost]
        public async Task<IActionResult> CreateSize([FromBody] SizeOption model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Check duplicate
                var exists = await _context.SizeOptions
                    .AnyAsync(s => s.SizeName == model.SizeName && 
                                  s.SubCategoryID == model.SubCategoryID && 
                                  s.IsActive);
                
                if (exists)
                {
                    return Json(new { success = false, message = "Kích thước này đã tồn tại cho danh mục này" });
                }

                _context.SizeOptions.Add(model);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Thêm kích thước thành công",
                    data = new
                    {
                        sizeOptionID = model.SizeOptionID,
                        sizeName = model.SizeName,
                        subCategoryID = model.SubCategoryID,
                        displayOrder = model.DisplayOrder
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi thêm kích thước: " + ex.Message });
            }
        }

        // DELETE: Admin/AttributeManagement/DeleteSize/5
        [HttpPost]
        public async Task<IActionResult> DeleteSize(int id)
        {
            try
            {
                var size = await _context.SizeOptions.FindAsync(id);
                if (size == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy kích thước" });
                }

                // Soft delete
                size.IsActive = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa kích thước thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa kích thước: " + ex.Message });
            }
        }

        // POST: Admin/AttributeManagement/UpdateSize
        [HttpPost]
        public async Task<IActionResult> UpdateSize([FromBody] SizeOption model)
        {
            try
            {
                var size = await _context.SizeOptions.FindAsync(model.SizeOptionID);
                if (size == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy kích thước" });
                }

                // Check duplicate
                var exists = await _context.SizeOptions
                    .AnyAsync(s => s.SizeName == model.SizeName && 
                                  s.SubCategoryID == model.SubCategoryID && 
                                  s.SizeOptionID != model.SizeOptionID &&
                                  s.IsActive);
                
                if (exists)
                {
                    return Json(new { success = false, message = "Kích thước này đã tồn tại cho danh mục này" });
                }

                size.SizeName = model.SizeName;
                size.SubCategoryID = model.SubCategoryID;
                size.DisplayOrder = model.DisplayOrder;
                size.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật kích thước thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
            }
        }

        #endregion

        #region Color Options API

        // GET: Admin/AttributeManagement/GetColors
        [HttpGet]
        public async Task<IActionResult> GetColors()
        {
            try
            {
                var colors = await _context.ColorOptions
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        colorOptionID = c.ColorOptionID,
                        colorName = c.ColorName,
                        hexCode = c.HexCode ?? ""
                    })
                    .ToListAsync();

                return Json(new { success = true, data = colors });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tải danh sách màu sắc: " + ex.Message });
            }
        }

        // POST: Admin/AttributeManagement/CreateColor
        [HttpPost]
        public async Task<IActionResult> CreateColor([FromBody] ColorOption model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                // Check duplicate
                var exists = await _context.ColorOptions
                    .AnyAsync(c => c.ColorName == model.ColorName && c.IsActive);
                
                if (exists)
                {
                    return Json(new { success = false, message = "Màu này đã tồn tại" });
                }

                _context.ColorOptions.Add(model);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Thêm màu thành công",
                    data = new
                    {
                        colorOptionID = model.ColorOptionID,
                        colorName = model.ColorName,
                        hexCode = model.HexCode ?? ""
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi thêm màu: " + ex.Message });
            }
        }

        // DELETE: Admin/AttributeManagement/DeleteColor/5
        [HttpPost]
        public async Task<IActionResult> DeleteColor(int id)
        {
            try
            {
                var color = await _context.ColorOptions.FindAsync(id);
                if (color == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy màu" });
                }

                // Soft delete
                color.IsActive = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa màu thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa màu: " + ex.Message });
            }
        }

        // POST: Admin/AttributeManagement/UpdateColor
        [HttpPost]
        public async Task<IActionResult> UpdateColor([FromBody] ColorOption model)
        {
            try
            {
                var color = await _context.ColorOptions.FindAsync(model.ColorOptionID);
                if (color == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy màu" });
                }

                // Check duplicate
                var exists = await _context.ColorOptions
                    .AnyAsync(c => c.ColorName == model.ColorName && 
                                  c.ColorOptionID != model.ColorOptionID &&
                                  c.IsActive);
                
                if (exists)
                {
                    return Json(new { success = false, message = "Màu này đã tồn tại" });
                }

                color.ColorName = model.ColorName;
                color.HexCode = model.HexCode;
                color.IsActive = model.IsActive;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật màu thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
            }
        }

        #endregion

        #region Attribute Types API

        // GET: Admin/AttributeManagement/GetAttributeTypes?categoryId=1
        [HttpGet]
        public async Task<IActionResult> GetAttributeTypes(int? categoryId)
        {
            try
            {
                if (categoryId.HasValue)
                {
                    // Get attribute types for specific category
                    var categoryAttributes = await _context.CategoryAttributeTypes
                        .Include(cat => cat.AttributeType)
                        .Where(cat => cat.CategoryID == categoryId && cat.AttributeType!.IsActive)
                        .OrderBy(cat => cat.DisplayOrder)
                        .Select(cat => new
                        {
                            attributeTypeID = cat.AttributeTypeID,
                            name = cat.AttributeType!.Name,
                            inputType = cat.AttributeType.InputType,
                            isRequired = cat.IsRequired
                        })
                        .ToListAsync();

                    return Json(new { success = true, data = categoryAttributes });
                }
                else
                {
                    // Get all attribute types
                    var allTypes = await _context.AttributeTypes
                        .Where(at => at.IsActive)
                        .Select(at => new
                        {
                            attributeTypeID = at.AttributeTypeID,
                            name = at.Name,
                            inputType = at.InputType
                        })
                        .ToListAsync();

                    return Json(new { success = true, data = allTypes });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tải loại thuộc tính: " + ex.Message });
            }
        }

        #endregion
    }
}
