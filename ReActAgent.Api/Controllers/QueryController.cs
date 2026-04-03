using Microsoft.AspNetCore.Mvc;
using ReActAgent.Api.Interfaces;
using ReActAgent.Api.Models;

namespace ReActAgent.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QueryController(IReActService reActService) : ControllerBase
{
    [HttpPost()]
    public async Task<IActionResult> Query([FromBody] QueryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest();
        }

        var result = await reActService.ReasonAndAct(request.Question, cancellationToken);
        return Ok(result);
    }
}
