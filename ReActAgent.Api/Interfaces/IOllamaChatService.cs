using ReActAgent.Api.Models;
using ReActAgent.Api.Schema;

namespace ReActAgent.Api.Interfaces;

public interface IOllamaChatService
{
    Task<ChatResponse> ChatAsync(OllamaConversation ollamaConversation);
}
