namespace SmartShip.AdminService.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        { _next = next; _logger = logger; }

        public async Task InvokeAsync(HttpContext context)
        {
            try { await _next(context); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ex switch
            {
                KeyNotFoundException => 404,
                UnauthorizedAccessException => 401,
                ArgumentException => 400,
                _ => 500
            };

            return context.Response.WriteAsJsonAsync(new
            {
                statusCode = context.Response.StatusCode,
                message = ex switch
                {
                    KeyNotFoundException => ex.Message,
                    UnauthorizedAccessException => "Unauthorized.",
                    ArgumentException => ex.Message,
                    _ => "An unexpected error occurred."
                },
                timestamp = DateTime.UtcNow
            });
        }
    }
}
