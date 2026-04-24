using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        if (await context.Customers.AnyAsync() || await context.Products.AnyAsync())
        {
            return;
        }

        var customers = new[]
        {
            new Customer
            {
                FirstName = "Ivan",
                LastName = "Petrov",
                Email = "ivan.petrov@example.com"
            },
            new Customer
            {
                FirstName = "Anna",
                LastName = "Sidorova",
                Email = "anna.sidorova@example.com"
            }
        };

        var products = new[]
        {
            new Product { ProductName = "Laptop", Price = 1200m },
            new Product { ProductName = "Mouse", Price = 35m },
            new Product { ProductName = "Keyboard", Price = 80m }
        };

        await context.Customers.AddRangeAsync(customers);
        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
