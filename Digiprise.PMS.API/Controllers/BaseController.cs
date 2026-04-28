using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Digiprise.PMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : 0;

    protected int CurrentTenantId =>
        int.TryParse(User.FindFirstValue("tenantId"), out var id) ? id : 0;

    protected IActionResult HandleException(Exception ex) => ex switch
    {
        KeyNotFoundException => NotFound(new ProblemDetails { Title = "Not Found", Detail = ex.Message, Status = 404 }),
        UnauthorizedAccessException => Forbid(),
        InvalidOperationException => BadRequest(new ProblemDetails { Title = "Bad Request", Detail = ex.Message, Status = 400 }),
        ArgumentException => BadRequest(new ProblemDetails { Title = "Invalid Input", Detail = ex.Message, Status = 400 }),
        _ => StatusCode(500, new ProblemDetails { Title = "Internal Server Error", Detail = "An unexpected error occurred.", Status = 500 })
    };
}
