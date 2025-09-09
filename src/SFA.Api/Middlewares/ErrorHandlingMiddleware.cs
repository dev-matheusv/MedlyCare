using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace SFA.Api.Middlewares;

public class ErrorHandlingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<ErrorHandlingMiddleware> _logger;

  public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task Invoke(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unhandled exception");

      var problem = new ProblemDetails
      {
        Status = (int)HttpStatusCode.InternalServerError,
        Title = "An unexpected error occurred",
        Detail = context.RequestServices
          .GetRequiredService<IHostEnvironment>()
          .IsDevelopment()
          ? ex.ToString() // em DEV mostra stacktrace
          : "Internal Server Error",
        Instance = context.Request.Path
      };

      context.Response.ContentType = "application/problem+json";
      context.Response.StatusCode = problem.Status.Value;
      await context.Response.WriteAsJsonAsync(problem);
    }
  }
}
