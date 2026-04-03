using ReActAgent.Api.Interfaces;
using ReActAgent.Api.Models;
using ReActAgent.Api.Schema;

namespace ReActAgent.Api.Services;

/// <summary>
/// ReAct - Reasoning and Acting
/// </summary>
public class ReActService(
    IOllamaChatService ollamaChatService) : IReActService
{
    private enum StringTools
    {
        CapitaliseTool, 
        RemoveSpacesTool,
        ReverseStringTool,
    }

    private static readonly string _toolNames = string.Join(",", Enum.GetNames<StringTools>()).TrimEnd(',');
    private static readonly List<OllamaTool> _tools = new List<OllamaTool>();


    static ReActService()
    {
        var toolsSchema = new Dictionary<StringTools, string>
        {
            { StringTools.CapitaliseTool, "Changes the casing of a string to uppercase" },
            { StringTools.RemoveSpacesTool, "Removes the spaces of a string" },
            { StringTools.ReverseStringTool, "Reverses the string" },
        };

        foreach (var toolSchema in toolsSchema)
        {
            var function = new OllamaFunction
            {
                Description = toolSchema.Value,
                Name = toolSchema.Key.ToString(),
                Parameters = new OllamaFunctionParameters 
                {
                    Type = "object",
                    Required = ["input"],
                    Properties = new 
                    {
                        input = new 
                        {
                            type = "string",
                            description = "The user's input",
                        } 
                    }
                }
            };

            _tools.Add(new OllamaTool 
            {
                Type = "function",
                Function = function,
            });
        }
    }

    public async Task<QueryResponse> ReasonAndAct(string question, CancellationToken cancellationToken)
    {
        var conversation = new OllamaConversation
        {
            Model = "qwen2.5:3b ",
            Tools = _tools.ToArray(),
            Stream = false,
            Messages = 
            [
                new OllamaMessage
                {
                    Role = "system",
                    Content =   "You are a helpful assistant. You must output ONLY the final answer. Do not use any internal monologue, reasoning, or phrases. Go directly to the answer." +
                                $"You have access to the tools: {_toolNames}. You can call the tools across multiple turns."
                },
                new OllamaMessage
                {
                    Role = "user",
                    Content = question,
                },
            ],                      
        };

        // Store called tools to prevent the model executing the same tool twice.
        var toolCallHistory = new List<StringTools>();
        var maxIterations = 5;

        for (int i = 0; i < maxIterations; i++)
        {
            var chatResponse = await ollamaChatService.ChatAsync(conversation);
            if (!chatResponse.IsSuccess)
            {
                return new QueryResponse(false, chatResponse.ErrorMessage);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new QueryResponse(false, "The operation was cancelled.");
            }

            var assistantMessage = chatResponse.Result?["message"];
            var toolCalls = assistantMessage?["tool_calls"]?.AsArray();
            var assistantContent = assistantMessage?["content"]?.GetValue<string>();

            // No tool calls = LLM is satisfied — this is the final answer
            if (toolCalls == null || toolCalls.Count == 0)
            {
                return new QueryResponse(true, assistantContent ?? "No Response");
            }

            // Append the assistant's reasoning turn to the conversation
            conversation.Messages.Add(new OllamaMessage 
            {
                Role = "assistant",
                Content = assistantContent,
                ToolCalls = toolCalls,
            });

            for (int t = 0; t < toolCalls.Count; t++)
            {
                var toolName = toolCalls[t]?["function"]?["name"]?.GetValue<string>();
                if (!Enum.TryParse<StringTools>(toolName, out var stringTool))
                {
                    return new QueryResponse(false, $"Invalid tool returned from LLM: {toolName}");
                }

                if (toolCallHistory.Contains(stringTool))
                {
                    continue;
                }
                toolCallHistory.Add(stringTool);

                switch (stringTool)
                {
                    case StringTools.CapitaliseTool:
                        question = question.ToUpper();

                        conversation.Messages.Add(new OllamaMessage 
                        {
                            Role = "tool",
                            Content = question,
                        });
                        break;

                    case StringTools.RemoveSpacesTool:
                        question = question.Replace(" ", "");

                        conversation.Messages.Add(new OllamaMessage
                        {
                            Role = "tool",
                            Content = question,
                        });
                        break;

                    case StringTools.ReverseStringTool:
                        question = new string(question.Reverse().ToArray());

                        conversation.Messages.Add(new OllamaMessage
                        {
                            Role = "tool",
                            Content = question,
                        });
                        break;

                    default:
                        break;
                }
            }
        }

        return new QueryResponse(false, "Iteration limit reached. Agent was unable to process the message");
    }
}
