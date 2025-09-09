using HttpTrafficProxy.Application.RequestHandlers.Abstractions;
using HttpTrafficProxy.Domain;
using HttpTrafficProxy.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace HttpTrafficProxy.Controllers;

[ApiController]
[Route("api/proxy")]
public class ProxyController : ControllerBase
{
    private readonly IProxyRequestHandler requestHandler;

    public ProxyController(IProxyRequestHandler requestHandler)
    {
        this.requestHandler = requestHandler;
    }

    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS", "HEAD")]
    [Route("{*path}")]
    public async Task<IActionResult> HandleAsync(string? path, CancellationToken cancellationToken)
    {
        var fullPath = string.Concat("/", path ?? string.Empty, HttpContext.Request.QueryString.Value);

        var request = new HttpProxyRequest(
            Path: fullPath,
            Method: HttpContext.Request.Method);

        try
        {
            var response = await requestHandler.HandleAsync(request, cancellationToken);
            HttpContext.Response.StatusCode = response.StatusCode;
            if (!string.IsNullOrEmpty(response.Body))
            {
                await HttpContext.Response.WriteAsync(response.Body, cancellationToken);
            }
         
            return new EmptyResult();
        }
        catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (HttpProxyException e)
        {
            return Problem(title: "Ошибка при выполнении запроса.", detail: e.Message);
        }
    }
}
