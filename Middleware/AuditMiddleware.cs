using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TheBuryProject.Services;

namespace TheBuryProject.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;
        private readonly ICurrentUserService _currentUserService;

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger, ICurrentUserService currentUserService)
        {
            _next = next;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startedAt = DateTimeOffset.UtcNow;
            var method = context.Request.Method;
            var path = context.Request.Path.Value;
            var userName = _currentUserService.IsAuthenticated() ? _currentUserService.GetUsername() : "Anonymous";
            var userId = _currentUserService.IsAuthenticated() ? _currentUserService.GetUserId() : "anonymous";

            try
            {
                await _next(context);
            }
            finally
            {
                var statusCode = context.Response?.StatusCode;
                _logger.LogInformation(
                    "HTTP {Method} {Path} executed by {UserName} ({UserId}) at {StartedAt} => {StatusCode}",
                    method,
                    path,
                    userName,
                    userId,
                    startedAt,
                    statusCode);
            }
        }
    }
}
