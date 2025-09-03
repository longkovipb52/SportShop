namespace SportShop.Models.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<Category> FeaturedCategories { get; set; }
        public IEnumerable<Product> FeaturedProducts { get; set; }
        public IEnumerable<Brand> FeaturedBrands { get; set; }
    }
}