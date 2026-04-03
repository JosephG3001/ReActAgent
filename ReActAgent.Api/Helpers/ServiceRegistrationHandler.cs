using ReActAgent.Api.Interfaces;
using ReActAgent.Api.Services;

namespace ReActAgent.Api.Helpers;

public static class ServiceRegistrationHandler
{
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddTransient<IReActService, ReActService>();

        services.AddHttpClient<IOllamaChatService, OllamaChatService>(
            client => client.BaseAddress = new Uri("http://127.0.0.1:11434"));
    }
}
