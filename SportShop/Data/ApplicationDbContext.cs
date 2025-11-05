using Microsoft.EntityFrameworkCore;
using SportShop.Models;

namespace SportShop.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<UserVoucher> UserVouchers { get; set; }
        
        // New master data tables for Option 3 (Hybrid Approach)
        public DbSet<SizeOption> SizeOptions { get; set; }
        public DbSet<ColorOption> ColorOptions { get; set; }
        public DbSet<AttributeType> AttributeTypes { get; set; }
        public DbSet<CategoryAttributeType> CategoryAttributeTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình quan hệ
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryID);
            
            // Quan hệ Product - SubCategory
            modelBuilder.Entity<Product>()
                .HasOne(p => p.SubCategory)
                .WithMany(sc => sc.Products)
                .HasForeignKey(p => p.SubCategoryID)
                .IsRequired(false);
            
            // Quan hệ SubCategory - Category
            modelBuilder.Entity<SubCategory>()
                .HasOne(sc => sc.Category)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(sc => sc.CategoryID)
                .OnDelete(DeleteBehavior.Restrict); // Ngăn xóa Category khi còn SubCategory
                
            // Quan hệ Product - Brand
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandID)
                .IsRequired(false);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserID);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductID);
                
            // Cấu hình quan hệ giữa Contact và User cho UserID
            modelBuilder.Entity<Contact>()
                .HasOne<User>()
                .WithMany(u => u.Contacts)
                .HasForeignKey(c => c.UserID)
                .IsRequired(false);
            
            // Cấu hình quan hệ giữa Contact và User cho RepliedBy
            modelBuilder.Entity<Contact>()
                .HasOne<User>()
                .WithMany(u => u.RepliedContacts)
                .HasForeignKey(c => c.RepliedBy)
                .IsRequired(false);

            // Check constraint cho Rating
            modelBuilder.Entity<Review>()
                .HasCheckConstraint("CK_Review_Rating", "Rating >= 1 AND Rating <= 5");
            
            // Check constraint cho Quantity
            modelBuilder.Entity<OrderItem>()
                .HasCheckConstraint("CK_OrderItem_Quantity", "Quantity > 0");

            modelBuilder.Entity<Cart>()
                .HasCheckConstraint("CK_Cart_Quantity", "Quantity > 0");

            // Cấu hình quan hệ Product-ProductAttribute
            modelBuilder.Entity<ProductAttribute>()
                .HasOne(pa => pa.Product)
                .WithMany(p => p.Attributes)
                .HasForeignKey(pa => pa.ProductID);

            // Cấu hình quan hệ Cart-ProductAttribute
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Attribute)
                .WithMany()
                .HasForeignKey(c => c.AttributeID)
                .IsRequired(false);

            // Cấu hình quan hệ UserVoucher
            modelBuilder.Entity<UserVoucher>()
                .HasOne(uv => uv.User)
                .WithMany()
                .HasForeignKey(uv => uv.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserVoucher>()
                .HasOne(uv => uv.Voucher)
                .WithMany()
                .HasForeignKey(uv => uv.VoucherID)
                .OnDelete(DeleteBehavior.Cascade);

            // Đảm bảo mỗi user chỉ có một voucher cụ thể
            modelBuilder.Entity<UserVoucher>()
                .HasIndex(uv => new { uv.UserID, uv.VoucherID })
                .IsUnique();

            // Cấu hình quan hệ cho SizeOption - liên kết với SubCategory
            modelBuilder.Entity<SizeOption>()
                .HasOne(s => s.SubCategory)
                .WithMany()
                .HasForeignKey(s => s.SubCategoryID)
                .IsRequired(false);

            // Cấu hình quan hệ cho CategoryAttributeType
            modelBuilder.Entity<CategoryAttributeType>()
                .HasOne(cat => cat.Category)
                .WithMany()
                .HasForeignKey(cat => cat.CategoryID);

            modelBuilder.Entity<CategoryAttributeType>()
                .HasOne(cat => cat.AttributeType)
                .WithMany()
                .HasForeignKey(cat => cat.AttributeTypeID);

            }
    }
}