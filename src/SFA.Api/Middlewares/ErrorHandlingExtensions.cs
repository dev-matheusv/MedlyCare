namespace SFA.Api.Middlewares;

public static class ErrorHandlingExtensions
{
  public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
  {
    return app.UseMiddleware<ErrorHandlingMiddleware>();
  }
}
