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
    public async Task<IActionResult> CreateTopic([FromBody] CreateTopicRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Amount < 1)
        {
            return BadRequest("Topic name is required and amount must be > 0");
        }

        try
        {
            await topicService.CreateTopic(request.Name, request.Amount);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("Subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TopicName) || request.ConsumerID < 1)
        {
            return BadRequest("Topic name and consumer id are required");
        }

        try
        {
            await subscribeService.Subscribe(request.TopicName, request.ConsumerID);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("Publish")]
    public async Task<IActionResult> Publish([FromBody] PublishRequest request)
    {
        if (request.TopicID < 1 || request.PubID < 1)
        {
            return BadRequest("Invalid ids");
        }

        try
        {
            var msg = await deliveryService.PublishAsync(request.TopicID, request.PubID, request.Payload);

            return Ok(new
            {
                msg.ID,
                msg.TopicID,
                msg.PubID,
                msg.Time
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("Consume")]
    public IActionResult Consume([FromQuery] ConsumeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TopicName) || request.ConsumerID < 1)
        {
            return BadRequest("Invalid ids");
        }

        try
        {
            var msg = deliveryService.Consume(request.TopicName, request.ConsumerID);
            if (msg is null) return NoContent();
            return Ok(msg);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
    }

    [HttpPatch("EditTopic")]
    public async Task<IActionResult> EditTopic([FromBody] EditTopicRequest request)
    {
        if (request.TopicID < 1)
        {
            return BadRequest("Topic id must be > 0");
        }

        if (request.Amount < 0)
        {
            return BadRequest("Amount cannot be less than 0");
        }

        await topicService.EditTopic(request.TopicID, request.Amount);
        return Ok();
    }

    [HttpDelete("DeleteTopic")]
    public async Task<IActionResult> DeleteTopic([FromBody] DeleteTopicRequest request)
    {
        if (request.TopicID < 1)
        {
            return BadRequest("ID cannot be < 1");
        }

        var topic = topicService.GetById(request.TopicID);
        if (topic is null) return NotFound("Topic not found");
        await topicService.RemoveTopic(request.TopicID);
        return Ok();
    }
}

