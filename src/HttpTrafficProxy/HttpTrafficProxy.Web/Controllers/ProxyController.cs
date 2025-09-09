using HttpTrafficProxy.Application.RequestHandlers.Abstractions;
using HttpTrafficProxy.Domain;
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
        if (string.IsNullOrWhiteSpace(path))
        {
            return ValidationProblem(title: "Не указан запрос к серверу.");
        }

        try
        {
            var request = new HttpProxyRequest(
                Path: path,
                Method: HttpContext.Request.Method);

            var response = await requestHandler.HandleAsync(request, cancellationToken);

            HttpContext.Response.StatusCode = response.StatusCode;
            if (!string.IsNullOrEmpty(response.Body))
            {
                await HttpContext.Response.WriteAsync(response.Body, cancellationToken);
            }
         
            return new EmptyResult();
        }
        catch (ApplicationException)
        {
            return StatusCode(500, "Ошибка системы.");
        }
        catch (Exception)
        {
            throw;
        }
    }
}
