using app.Services;
using Microsoft.AspNetCore.Mvc;

namespace app.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<string> ConversationHistory { get; set; } = new();
    }

    /// <summary>
    /// Send a chat message and get AI response
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<string>> PostMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty");
        }

        var response = await _chatService.GetChatResponseAsync(request.Message, request.ConversationHistory);
        return Ok(new { response });
    }

    /// <summary>
    /// Check if chat service is configured
    /// </summary>
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        var isConfigured = _chatService.IsConfigured();
        return Ok(new { configured = isConfigured });
    }
}
