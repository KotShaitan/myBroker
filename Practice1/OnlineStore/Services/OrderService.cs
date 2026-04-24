using Microsoft.EntityFrameworkCore;

public class OrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .OrderBy(order => order.OrderId)
            .ToListAsync();
    }

    public async Task<Order> CreateOrderAsync(int customerId, List<(int productId, int quantity)> items)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var customerExists = await _context.Customers.AnyAsync(customer => customer.CustomerId == customerId);
            if (!customerExists)
            {
                throw new InvalidOperationException($"Customer with id {customerId} not found");
            }

            if (items.Count == 0)
            {
                throw new InvalidOperationException("Order must contain at least one item");
            }

            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = 0
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            decimal total = 0;

            foreach (var item in items)
            {
                if (item.quantity <= 0)
                {
                    throw new InvalidOperationException("Quantity must be greater than zero");
                }

                var product = await _context.Products.FindAsync(item.productId);
                if (product == null)
                    throw new InvalidOperationException($"Product with id {item.productId} not found");

                var subtotal = product.Price * item.quantity;

                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = item.productId,
                    Quantity = item.quantity,
                    Subtotal = subtotal
                };

                total += subtotal;

                _context.OrderItems.Add(orderItem);
            }

            await _context.SaveChangesAsync();

            order.TotalAmount = total;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return await _context.Orders
                .AsNoTracking()
                .Include(createdOrder => createdOrder.Customer)
                .Include(createdOrder => createdOrder.Items)
                .ThenInclude(createdItem => createdItem.Product)
                .SingleAsync(createdOrder => createdOrder.OrderId == order.OrderId);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
