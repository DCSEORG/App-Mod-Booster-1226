using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

/// <summary>Chat API for AI-powered expense assistant</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>Send a message to the AI assistant</summary>
    [HttpPost]
    public async Task<ActionResult<object>> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { error = "Message cannot be empty" });

        var response = await _chatService.ChatAsync(request.Message);
        return Ok(new { response });
    }
}
