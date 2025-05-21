using System.Net;
using System.Text.Json;

namespace SellerManging.Middleware
{
	public class ExceptionMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<ExceptionMiddleware> _logger;
		public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context); // Call the next middleware in the pipeline
			}
			catch (Exception ex)
			{

				_logger.LogError(ex, "An unhandled exception occurred.");
				await HandleExceptionAsync(context, ex);
			}
		}

		private static Task HandleExceptionAsync(HttpContext context, Exception exception)
		{
			context.Response.ContentType = "application/json";
			context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

			var response = new
			{
				context.Response.StatusCode,
				Message = "An internal server error occurred. Please try again later.",
				Details = exception.Message // Include exception details for debugging (avoid in production)
			};

			var jsonResponse = JsonSerializer.Serialize(response);
			return context.Response.WriteAsync(jsonResponse);
		}
	}
}
