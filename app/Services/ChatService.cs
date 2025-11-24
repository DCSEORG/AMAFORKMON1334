using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using System.Text.Json;
using OpenAI.Chat;

namespace app.Services;

public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage, List<string> conversationHistory);
    bool IsConfigured();
}

public class ChatService : IChatService
{
    private readonly IConfiguration _configuration;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ChatService> _logger;
    private readonly DefaultAzureCredential _credential;
    private AzureOpenAIClient? _openAIClient;
    private ChatClient? _chatClient;
    private readonly bool _isConfigured;

    public ChatService(
        IConfiguration configuration, 
        IDatabaseService databaseService, 
        ILogger<ChatService> logger,
        DefaultAzureCredential credential)
    {
        _configuration = configuration;
        _databaseService = databaseService;
        _logger = logger;
        _credential = credential;
        
        var endpoint = _configuration["OpenAI:Endpoint"];
        var deploymentName = _configuration["OpenAI:DeploymentName"];
        
        _isConfigured = !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(deploymentName);
        
        if (_isConfigured)
        {
            try
            {
                _openAIClient = new AzureOpenAIClient(new Uri(endpoint!), _credential);
                _chatClient = _openAIClient.GetChatClient(deploymentName!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OpenAI client");
                _isConfigured = false;
            }
        }
    }

    public bool IsConfigured() => _isConfigured;

    public async Task<string> GetChatResponseAsync(string userMessage, List<string> conversationHistory)
    {
        if (!_isConfigured || _chatClient == null)
        {
            return "GenAI services are not deployed. Please run deploy-with-chat.sh to enable AI-powered chat functionality.";
        }

        try
        {
            // Build messages list
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"You are an AI assistant for an Expense Management System. You can help users:
- View expenses (all, by status, pending approval)
- Create new expenses
- Approve expenses
- Get information about expense categories and statuses

When users ask to perform an action, use the appropriate function call. 
Be helpful, concise, and professional.")
            };

            // Add conversation history
            foreach (var msg in conversationHistory)
            {
                if (msg.StartsWith("User:"))
                {
                    messages.Add(new UserChatMessage(msg.Substring(5).Trim()));
                }
                else if (msg.StartsWith("Assistant:"))
                {
                    messages.Add(new AssistantChatMessage(msg.Substring(10).Trim()));
                }
            }

            // Add current user message
            messages.Add(new UserChatMessage(userMessage));

            // Define function tools
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    "get_all_expenses",
                    "Retrieves all expenses from the database",
                    BinaryData.FromString("{\"type\":\"object\",\"properties\":{}}")
                ),
                ChatTool.CreateFunctionTool(
                    "get_expenses_by_status",
                    "Retrieves expenses filtered by status",
                    BinaryData.FromString(@"{""type"":""object"",""properties"":{""status"":{""type"":""string"",""enum"":[""Draft"",""Submitted"",""Approved"",""Rejected""]}},""required"":[""status""]}")
                ),
                ChatTool.CreateFunctionTool(
                    "get_pending_expenses",
                    "Retrieves expenses that are pending approval",
                    BinaryData.FromString("{\"type\":\"object\",\"properties\":{}}")
                ),
                ChatTool.CreateFunctionTool(
                    "create_expense",
                    "Creates a new expense",
                    BinaryData.FromString(@"{""type"":""object"",""properties"":{""amount"":{""type"":""number""},""categoryId"":{""type"":""integer""},""date"":{""type"":""string""},""description"":{""type"":""string""}},""required"":[""amount"",""categoryId"",""date""]}")
                ),
                ChatTool.CreateFunctionTool(
                    "approve_expense",
                    "Approves an expense",
                    BinaryData.FromString(@"{""type"":""object"",""properties"":{""expenseId"":{""type"":""integer""}},""required"":[""expenseId""]}")
                ),
                ChatTool.CreateFunctionTool(
                    "get_categories",
                    "Gets all expense categories",
                    BinaryData.FromString("{\"type\":\"object\",\"properties\":{}}")
                )
            };

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            // Get completion
            var completion = await _chatClient.CompleteChatAsync(messages, options);
            var responseMessage = completion.Value.Content[0].Text;

            // Handle function calls
            if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                var toolCalls = completion.Value.ToolCalls;
                var functionResults = new List<string>();

                foreach (var toolCall in toolCalls)
                {
                    if (toolCall is ChatToolCall functionCall)
                    {
                        var functionName = functionCall.FunctionName;
                        var functionArgs = functionCall.FunctionArguments;

                        _logger.LogInformation($"Function call: {functionName} with args: {functionArgs}");

                        var result = await ExecuteFunctionAsync(functionName, functionArgs);
                        functionResults.Add(result);
                    }
                }

                // Add function results to messages and get final response
                messages.Add(new AssistantChatMessage(toolCalls));
                
                foreach (var result in functionResults)
                {
                    messages.Add(new SystemChatMessage(result));
                }

                var finalCompletion = await _chatClient.CompleteChatAsync(messages);
                responseMessage = finalCompletion.Value.Content[0].Text;
            }

            return responseMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat completion");
            return $"I encountered an error: {ex.Message}. Please try again.";
        }
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string argumentsJson)
    {
        try
        {
            switch (functionName)
            {
                case "get_all_expenses":
                    var (expenses, error) = await _databaseService.GetAllExpensesAsync();
                    if (error != null)
                        return $"Error: {error}";
                    return JsonSerializer.Serialize(expenses);

                case "get_expenses_by_status":
                    var args = JsonSerializer.Deserialize<Dictionary<string, string>>(argumentsJson);
                    var status = args?["status"] ?? "Submitted";
                    var (statusExpenses, statusError) = await _databaseService.GetExpensesByStatusAsync(status);
                    if (statusError != null)
                        return $"Error: {statusError}";
                    return JsonSerializer.Serialize(statusExpenses);

                case "get_pending_expenses":
                    var (pending, pendingError) = await _databaseService.GetPendingExpensesAsync();
                    if (pendingError != null)
                        return $"Error: {pendingError}";
                    return JsonSerializer.Serialize(pending);

                case "create_expense":
                    var createArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                    if (createArgs == null) return "Invalid arguments";
                    
                    var request = new Models.CreateExpenseRequest
                    {
                        UserId = 1,
                        Amount = createArgs["amount"].GetDecimal(),
                        CategoryId = createArgs["categoryId"].GetInt32(),
                        ExpenseDate = DateTime.Parse(createArgs["date"].GetString()!),
                        Description = createArgs.ContainsKey("description") ? createArgs["description"].GetString() : null,
                        SubmitNow = true
                    };
                    var (newExpense, createError) = await _databaseService.CreateExpenseAsync(request);
                    if (createError != null)
                        return $"Error: {createError}";
                    return JsonSerializer.Serialize(newExpense);

                case "approve_expense":
                    var approveArgs = JsonSerializer.Deserialize<Dictionary<string, int>>(argumentsJson);
                    var expenseId = approveArgs?["expenseId"] ?? 0;
                    var (approved, approveError) = await _databaseService.ApproveExpenseAsync(expenseId, 2); // Manager ID = 2
                    if (approveError != null)
                        return $"Error: {approveError}";
                    return JsonSerializer.Serialize(approved);

                case "get_categories":
                    var (categories, catError) = await _databaseService.GetAllCategoriesAsync();
                    if (catError != null)
                        return $"Error: {catError}";
                    return JsonSerializer.Serialize(categories);

                default:
                    return $"Unknown function: {functionName}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing function {functionName}");
            return $"Error executing {functionName}: {ex.Message}";
        }
    }
}
