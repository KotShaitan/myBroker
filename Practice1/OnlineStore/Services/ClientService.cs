using Microsoft.EntityFrameworkCore;

public class ClientService
{
    private readonly AppDbContext _context;

    public ClientService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetCustomersAsync()
    {
        return await _context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.CustomerId)
            .ToListAsync();
    }

    public async Task<Customer> UpdateCustomerEmailAsync(int customerId, string newEmail)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var customer = await _context.Customers.FindAsync(customerId);

            if (customer == null)
                throw new InvalidOperationException("Customer not found");

            customer.Email = newEmail;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return customer;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
