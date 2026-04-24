using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ClientService _clientService;

    public CustomersController(ClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
    {
        var customers = await _clientService.GetCustomersAsync();
        return Ok(customers);
    }

    [HttpPut("{customerId:int}/email")]
    public async Task<ActionResult<Customer>> UpdateCustomerEmail(int customerId, UpdateCustomerEmailRequest request)
    {
        try
        {
            var customer = await _clientService.UpdateCustomerEmailAsync(customerId, request.Email);
            return Ok(customer);
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
