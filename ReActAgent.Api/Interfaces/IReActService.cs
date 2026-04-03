using ReActAgent.Api.Models;

namespace ReActAgent.Api.Interfaces;

public interface IReActService
{
    Task<QueryResponse> ReasonAndAct(string question, CancellationToken cancellationToken);
}
