namespace Atlas.Api.Http;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ApiExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new ErrorResponse(ex.Errors.ToList(), StatusCodes.Status400BadRequest));
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("AzureDevopsToken", StringComparison.Ordinal) ||
            ex.Message.Contains("Azure DevOps PAT", StringComparison.Ordinal))
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
    }
}
