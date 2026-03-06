using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using System.Text.Json;
using ExpenseManagement.Models;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly ExpenseService _expenseService;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IConfiguration configuration, ExpenseService expenseService, ILogger<ChatService> logger)
    {
        _configuration = configuration;
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task<string> ChatAsync(string userMessage)
    {
        var endpoint = _configuration["OpenAI:Endpoint"];
        var deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4o";

        if (string.IsNullOrEmpty(endpoint))
        {
            return "⚠️ **GenAI services are not configured.**\n\n" +
                   "To enable the AI chat assistant, please deploy the GenAI services by running:\n\n" +
                   "```bash\nbash deploy-with-chat.sh\n```\n\n" +
                   "This will deploy Azure OpenAI (GPT-4o) and AI Search resources and configure the chat experience.";
        }

        try
        {
            // Use ManagedIdentityCredential with explicit client ID
            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
            Azure.Core.TokenCredential credential;

            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                _logger.LogInformation("Using ManagedIdentityCredential with client ID: {ClientId}", managedIdentityClientId);
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }
            else
            {
                _logger.LogInformation("Using DefaultAzureCredential");
                credential = new DefaultAzureCredential();
            }

            var client = new AzureOpenAIClient(new Uri(endpoint), credential);
            var chatClient = client.GetChatClient(deploymentName);

            // Define function tools
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    "get_all_expenses",
                    "Retrieves all expenses from the expense management system with user, category, and status details"),
                ChatTool.CreateFunctionTool(
                    "get_expense_by_id",
                    "Retrieves a specific expense by its ID",
                    BinaryData.FromString("""{"type":"object","properties":{"expense_id":{"type":"integer","description":"The ID of the expense to retrieve"}},"required":["expense_id"]}""")),
                ChatTool.CreateFunctionTool(
                    "get_expenses_by_user",
                    "Retrieves all expenses for a specific user",
                    BinaryData.FromString("""{"type":"object","properties":{"user_id":{"type":"integer","description":"The ID of the user"}},"required":["user_id"]}""")),
                ChatTool.CreateFunctionTool(
                    "get_expenses_by_status",
                    "Retrieves expenses filtered by status (1=Draft, 2=Submitted, 3=Approved, 4=Rejected)",
                    BinaryData.FromString("""{"type":"object","properties":{"status_id":{"type":"integer","description":"Status ID: 1=Draft, 2=Submitted, 3=Approved, 4=Rejected"}},"required":["status_id"]}""")),
                ChatTool.CreateFunctionTool(
                    "get_users",
                    "Retrieves all users in the system"),
                ChatTool.CreateFunctionTool(
                    "get_categories",
                    "Retrieves all expense categories"),
                ChatTool.CreateFunctionTool(
                    "get_statuses",
                    "Retrieves all expense status types"),
                ChatTool.CreateFunctionTool(
                    "submit_expense",
                    "Submits a draft expense for manager approval",
                    BinaryData.FromString("""{"type":"object","properties":{"expense_id":{"type":"integer","description":"The ID of the expense to submit"}},"required":["expense_id"]}""")),
                ChatTool.CreateFunctionTool(
                    "approve_expense",
                    "Approves a submitted expense",
                    BinaryData.FromString("""{"type":"object","properties":{"expense_id":{"type":"integer","description":"The ID of the expense to approve"},"reviewed_by":{"type":"integer","description":"The user ID of the manager approving the expense"}},"required":["expense_id","reviewed_by"]}""")),
                ChatTool.CreateFunctionTool(
                    "reject_expense",
                    "Rejects a submitted expense",
                    BinaryData.FromString("""{"type":"object","properties":{"expense_id":{"type":"integer","description":"The ID of the expense to reject"},"reviewed_by":{"type":"integer","description":"The user ID of the manager rejecting the expense"}},"required":["expense_id","reviewed_by"]}""")),
            };

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(
                    "You are an intelligent assistant for the Expense Management System. " +
                    "You have access to real-time expense data through function tools. " +
                    "When users ask about expenses, users, categories or statuses, use the appropriate function to retrieve accurate data. " +
                    "When listing items, format them as clear bullet-point lists with key details. " +
                    "Amounts are in GBP. AmountMinor values are in pence (divide by 100 for pounds). " +
                    "Always be helpful, concise, and professional."),
                ChatMessage.CreateUserMessage(userMessage)
            };

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
                options.Tools.Add(tool);

            // Agentic loop: handle function calls
            var maxIterations = 5;
            for (int i = 0; i < maxIterations; i++)
            {
                var completion = await chatClient.CompleteChatAsync(messages, options);
                var response = completion.Value;

                if (response.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Add assistant response to messages
                    messages.Add(ChatMessage.CreateAssistantMessage(response));

                    // Execute tool calls
                    foreach (var toolCall in response.ToolCalls)
                    {
                        var result = await ExecuteToolCallAsync(toolCall.FunctionName, toolCall.FunctionArguments.ToString());
                        messages.Add(ChatMessage.CreateToolMessage(toolCall.Id, result));
                    }
                }
                else
                {
                    // Final response
                    return response.Content[0].Text;
                }
            }

            return "I was unable to complete the request after multiple attempts. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChatService.ChatAsync");
            return $"⚠️ Error communicating with Azure OpenAI: {ex.Message}";
        }
    }

    private async Task<string> ExecuteToolCallAsync(string functionName, string argumentsJson)
    {
        try
        {
            var args = JsonDocument.Parse(argumentsJson).RootElement;

            return functionName switch
            {
                "get_all_expenses" => JsonSerializer.Serialize(await _expenseService.GetExpensesAsync()),
                "get_expense_by_id" => JsonSerializer.Serialize(await _expenseService.GetExpenseByIdAsync(args.GetProperty("expense_id").GetInt32())),
                "get_expenses_by_user" => JsonSerializer.Serialize(await _expenseService.GetExpensesByUserAsync(args.GetProperty("user_id").GetInt32())),
                "get_expenses_by_status" => JsonSerializer.Serialize(await _expenseService.GetExpensesByStatusAsync(args.GetProperty("status_id").GetInt32())),
                "get_users" => JsonSerializer.Serialize(await _expenseService.GetUsersAsync()),
                "get_categories" => JsonSerializer.Serialize(await _expenseService.GetCategoriesAsync()),
                "get_statuses" => JsonSerializer.Serialize(await _expenseService.GetStatusesAsync()),
                "submit_expense" => JsonSerializer.Serialize(new { success = await _expenseService.SubmitExpenseAsync(args.GetProperty("expense_id").GetInt32()) }),
                "approve_expense" => JsonSerializer.Serialize(new { success = await _expenseService.ApproveExpenseAsync(args.GetProperty("expense_id").GetInt32(), args.GetProperty("reviewed_by").GetInt32()) }),
                "reject_expense" => JsonSerializer.Serialize(new { success = await _expenseService.RejectExpenseAsync(args.GetProperty("expense_id").GetInt32(), args.GetProperty("reviewed_by").GetInt32()) }),
                _ => JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool call {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
