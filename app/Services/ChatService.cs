using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using System.Text.Json;

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
    private OpenAIClient? _openAIClient;
    private readonly bool _isConfigured;
    private readonly string? _deploymentName;

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
        _deploymentName = _configuration["OpenAI:DeploymentName"];
        
        _isConfigured = !string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(_deploymentName);
        
        if (_isConfigured)
        {
            try
            {
                _openAIClient = new OpenAIClient(new Uri(endpoint!), _credential);
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
        if (!_isConfigured || _openAIClient == null || _deploymentName == null)
        {
            return "GenAI services are not deployed. Please run deploy-with-chat.sh to enable AI-powered chat functionality.";
        }

        try
        {
            // Build messages for Azure OpenAI
            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(@"You are an AI assistant for an Expense Management System. You can help users:
- View expenses (all, by status, pending approval)
- Create new expenses
- Approve expenses
- Get information about expense categories and statuses

When users ask to perform an action, describe what you would do. 
Be helpful, concise, and professional.")
            };

            // Add conversation history
            foreach (var msg in conversationHistory)
            {
                if (msg.StartsWith("User:"))
                {
                    messages.Add(new ChatRequestUserMessage(msg.Substring(5).Trim()));
                }
                else if (msg.StartsWith("Assistant:"))
                {
                    messages.Add(new ChatRequestAssistantMessage(msg.Substring(10).Trim()));
                }
            }

            // Add current user message
            messages.Add(new ChatRequestUserMessage(userMessage));

            var chatOptions = new ChatCompletionsOptions(_deploymentName, messages)
            {
                Temperature = 0.7f,
                MaxTokens = 800
            };

            // For now, simplified without function calling - just get a response
            var response = await _openAIClient.GetChatCompletionsAsync(chatOptions);
            var choice = response.Value.Choices[0];
            
            return choice.Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat completion");
            return $"I encountered an error: {ex.Message}. Please try again.";
        }
    }
}
