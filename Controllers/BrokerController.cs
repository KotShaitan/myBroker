using Microsoft.AspNetCore.Mvc;

public class MessageController : BaseController
{
    private readonly TopicService topicService;
    private readonly SubscribeService subscribeService;
    private readonly DeliveryService deliveryService;

    public MessageController(TopicService topicService_, SubscribeService subscribeService_, DeliveryService deliveryService_)
    {
        topicService = topicService_;
        subscribeService = subscribeService_;
        deliveryService = deliveryService_;
    }

    [HttpPost("CreateTopic")]
    public async Task<IActionResult> CreateTopic([FromQuery]string name, [FromQuery]int amount)
    {
        if (amount < 1)
        {
            return BadRequest("количество должно быть > 0");
        }
        try
        {
            await topicService.CreateTopic(name, amount);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex);
        }
    }
        
    [HttpPost("Subscribe")]
    public async Task<IActionResult> Subscribe([FromQuery]int topicID, [FromQuery]int consumerID)
    {
        if (topicID < 1 || consumerID < 1)
        {
            return BadRequest("ID не может быть < 1");
        }
        if (topicService.GetById(topicID) is null) return NotFound("Topic not found");
        await subscribeService.Subscribe(topicID, consumerID);
        return Ok();

    }
    [HttpPost("Publish")]
    public async Task<IActionResult> Publish([FromQuery] int topicID, [FromQuery]int pubID, [FromQuery]string payload)
    {
        if (topicID < 1 || pubID < 1)
        return BadRequest("Invalid ids");
    
    var msg = await deliveryService.PublishAsync(
        topicID,
        pubID,
        payload);

    return Ok(new
    {
        msg.ID,
        msg.TopicID,
        msg.PubID,
        msg.Time
    });
    
    }

    [HttpGet("Consume")]
    public async Task<IActionResult> Consume([FromQuery] int topicID, [FromQuery] int consumerID)
    {
        if (topicID < 1 || consumerID < 1)
            return BadRequest("Invalid ids");

        try
        {
            var msg = await deliveryService.ConsumeAsync(topicID, consumerID);
            if (msg is null) return NoContent();
            return Ok(msg);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }
    /*
    [HttpPatch("EditTopic")]
    public async Task<IActionResult> EditTopic([FromBody]int amount)
    {
        if (amount < 0)
        {
            return BadRequest("количество не может быть меньше 0");
        }
    } 
    */
}
