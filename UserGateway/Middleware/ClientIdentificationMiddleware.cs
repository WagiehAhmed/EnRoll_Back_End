using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace UserGateway.Middleware;

/// <summary>
/// Middleware that resolves a unique ClientIdentifier for Rate Limiting.
/// Priority order:
/// 1. Explicit 'X-Client-Id' (or configured header name) HTTP request header.
/// 2. Claims extracted from JWT Bearer token ('client_id', 'azp', 'sub', 'nameidentifier', etc.).
/// 3. Fallback to Client Remote IP Address.
/// </summary>
public class ClientIdentificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _clientIdHeader;

    public ClientIdentificationMiddleware(RequestDelegate next, string clientIdHeader = "X-Client-Id")
    {
        _next = next;
        _clientIdHeader = clientIdHeader;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? clientIdentifier = null;

        // 1. Check for explicit ClientId header
        if (context.Request.Headers.TryGetValue(_clientIdHeader, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
        {
            clientIdentifier = headerValue.ToString().Trim();
        }

        // 2. If no ClientId header, attempt to extract identification from JWT token
        if (string.IsNullOrEmpty(clientIdentifier))
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                clientIdentifier = ExtractIdentifierFromJwt(token);
            }
        }

        // 3. Fallback to Remote IP Address if no header or JWT claim was resolved
        if (string.IsNullOrEmpty(clientIdentifier))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString();
            clientIdentifier = !string.IsNullOrEmpty(ip) ? $"ip-{ip}" : "anonymous-client";
        }

        // Standardize the resolved ClientId into the request header for Ocelot / Rate Limiting downstream
        context.Request.Headers[_clientIdHeader] = clientIdentifier;

        await _next(context);
    }

    private static string? ExtractIdentifierFromJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);

                // Check for client_id or azp (OAuth client ID)
                var clientIdClaim = jwtToken.Claims.FirstOrDefault(c => 
                    c.Type.Equals("client_id", StringComparison.OrdinalIgnoreCase) ||
                    c.Type.Equals("azp", StringComparison.OrdinalIgnoreCase));

                if (clientIdClaim != null && !string.IsNullOrWhiteSpace(clientIdClaim.Value))
                {
                    return $"client-{clientIdClaim.Value}";
                }

                // Check for user identifier claims (sub, nameidentifier, uid, user_id)
                var userClaim = jwtToken.Claims.FirstOrDefault(c =>
                    c.Type.Equals(JwtRegisteredClaimNames.Sub, StringComparison.OrdinalIgnoreCase) ||
                    c.Type.Equals(ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase) ||
                    c.Type.Equals("sub", StringComparison.OrdinalIgnoreCase) ||
                    c.Type.Equals("user_id", StringComparison.OrdinalIgnoreCase) ||
                    c.Type.Equals("uid", StringComparison.OrdinalIgnoreCase));

                if (userClaim != null && !string.IsNullOrWhiteSpace(userClaim.Value))
                {
                    return $"user-{userClaim.Value}";
                }
            }
        }
        catch
        {
            // If JWT decoding fails or token is malformed, ignore and fall back to IP
        }

        return null;
    }
}

/// <summary>
/// Extension methods to register ClientIdentificationMiddleware.
/// </summary>
public static class ClientIdentificationMiddlewareExtensions
{
    public static IApplicationBuilder UseClientIdentification(this IApplicationBuilder builder, string clientIdHeader = "X-Client-Id")
    {
        return builder.UseMiddleware<ClientIdentificationMiddleware>(clientIdHeader);
    }
}
