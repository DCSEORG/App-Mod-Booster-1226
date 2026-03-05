using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    private readonly IConfiguration _configuration;

    public bool IsGenAIConfigured { get; set; }

    public ChatModel(IConfiguration configuration) => _configuration = configuration;

    public void OnGet()
    {
        IsGenAIConfigured = !string.IsNullOrEmpty(_configuration["OpenAI:Endpoint"]);
    }
}
