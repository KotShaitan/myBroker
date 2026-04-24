using Microsoft.EntityFrameworkCore;

public class ProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        return await _context.Products
            .OrderBy(product => product.ProductId)
            .ToListAsync();
    }

    public async Task<Product> AddProductAsync(string name, decimal price)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var product = new Product
            {
                ProductName = name,
                Price = price
            };

            _context.Products.Add(product);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return product;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
