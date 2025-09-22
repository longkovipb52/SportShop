using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Contacts
        public async Task<IActionResult> Index(
            string searchString = "",
            string statusFilter = "",
            string sortOrder = "",
            int page = 1)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParam = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.DateSortParam = sortOrder == "date" ? "date_desc" : "date";
            ViewBag.StatusSortParam = sortOrder == "status" ? "status_desc" : "status";
            ViewBag.CurrentFilter = searchString;
            ViewBag.StatusFilter = statusFilter;

            var contacts = from c in _context.Contacts
                          select c;

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                contacts = contacts.Where(c => c.Name.Contains(searchString) ||
                                             c.Email.Contains(searchString) ||
                                             c.Title != null && c.Title.Contains(searchString) ||
                                             c.Message.Contains(searchString));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(statusFilter))
            {
                contacts = contacts.Where(c => c.Status == statusFilter);
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "name_desc":
                    contacts = contacts.OrderByDescending(c => c.Name);
                    break;
                case "date":
                    contacts = contacts.OrderBy(c => c.CreatedAt);
                    break;
                case "date_desc":
                    contacts = contacts.OrderByDescending(c => c.CreatedAt);
                    break;
                case "status":
                    contacts = contacts.OrderBy(c => c.Status);
                    break;
                case "status_desc":
                    contacts = contacts.OrderByDescending(c => c.Status);
                    break;
                default:
                    contacts = contacts.OrderBy(c => c.Name);
                    break;
            }

            // Phân trang
            int pageSize = 10;
            int totalItems = await contacts.CountAsync();
            var paginatedContacts = await contacts
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalItems = totalItems;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Thống kê
            ViewBag.TotalContacts = await _context.Contacts.CountAsync();
            ViewBag.NewContacts = await _context.Contacts.CountAsync(c => c.Status == "New");
            ViewBag.RepliedContacts = await _context.Contacts.CountAsync(c => c.Status == "Replied");

            return View(paginatedContacts);
        }

        // GET: Admin/Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(m => m.ContactID == id);

            if (contact == null)
            {
                return NotFound();
            }

            // Lấy thông tin người trả lời nếu có
            if (contact.RepliedBy.HasValue)
            {
                var replier = await _context.Users.FindAsync(contact.RepliedBy.Value);
                ViewBag.ReplierName = replier?.FullName ?? "Admin";
            }

            return View(contact);
        }

        // POST: Admin/Contacts/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int contactId, string replyMessage)
        {
            if (string.IsNullOrWhiteSpace(replyMessage))
            {
                TempData["Error"] = "Vui lòng nhập nội dung trả lời.";
                return RedirectToAction("Details", new { id = contactId });
            }

            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null)
                {
                    return NotFound();
                }

                contact.Reply = replyMessage;
                contact.Status = "Replied";
                contact.RepliedAt = DateTime.Now;
                // TODO: Lấy ID của admin đang đăng nhập
                contact.RepliedBy = 1; // Tạm thời hardcode, cần cập nhật khi có authentication

                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã trả lời liên hệ thành công.";
                return RedirectToAction("Details", new { id = contactId });
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi trả lời liên hệ.";
                return RedirectToAction("Details", new { id = contactId });
            }
        }

        // POST: Admin/Contacts/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int contactId, string status)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact == null)
                {
                    return NotFound();
                }

                contact.Status = status;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã cập nhật trạng thái thành công.";
                return RedirectToAction("Details", new { id = contactId });
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi cập nhật trạng thái.";
                return RedirectToAction("Details", new { id = contactId });
            }
        }

        // POST: Admin/Contacts/BulkAction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string action, int[] selectedContacts)
        {
            if (selectedContacts == null || selectedContacts.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một liên hệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var contacts = await _context.Contacts
                    .Where(c => selectedContacts.Contains(c.ContactID))
                    .ToListAsync();

                switch (action)
                {
                    case "mark-new":
                        foreach (var contact in contacts)
                        {
                            contact.Status = "New";
                        }
                        break;
                    case "mark-replied":
                        foreach (var contact in contacts)
                        {
                            contact.Status = "Replied";
                        }
                        break;
                    case "delete":
                        _context.Contacts.RemoveRange(contacts);
                        break;
                    default:
                        TempData["Error"] = "Hành động không hợp lệ.";
                        return RedirectToAction(nameof(Index));
                }

                await _context.SaveChangesAsync();

                var actionText = action switch
                {
                    "mark-new" => "đánh dấu là mới",
                    "mark-replied" => "đánh dấu là đã trả lời",
                    "delete" => "xóa",
                    _ => "cập nhật"
                };

                TempData["Success"] = $"Đã {actionText} {contacts.Count} liên hệ thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi thực hiện hành động.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(m => m.ContactID == id);

            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Admin/Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact != null)
                {
                    _context.Contacts.Remove(contact);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã xóa liên hệ thành công.";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy liên hệ cần xóa.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa liên hệ.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}