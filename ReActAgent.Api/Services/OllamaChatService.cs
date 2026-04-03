using ReActAgent.Api.Interfaces;
using ReActAgent.Api.Models;
using ReActAgent.Api.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReActAgent.Api.Services;

public class OllamaChatService(HttpClient httpClient) : IOllamaChatService
{
    public async Task<ChatResponse> ChatAsync(OllamaConversation ollamaConversation)
    {
		try
		{			
			var response = await httpClient.PostAsJsonAsync("/api/chat", ollamaConversation);
			response.EnsureSuccessStatusCode();

			var responseText = await response.Content.ReadAsStringAsync();
            return ChatResponse.Success(JsonSerializer.Deserialize<JsonObject>(responseText));
        }
		catch (Exception ex)
		{
			return ChatResponse.Failure($"ChatAsync failed: {ex.Message}");			
		}
    }
}
