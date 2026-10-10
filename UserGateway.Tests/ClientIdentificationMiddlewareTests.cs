using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using UserGateway.Middleware;
using Xunit;

namespace UserGateway.Tests;

public class ClientIdentificationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldUseExplicitClientIdHeader_WhenHeaderIsPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Client-Id"] = "explicit-app-key-123";

        var middleware = new ClientIdentificationMiddleware(next: (innerContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("explicit-app-key-123", context.Request.Headers["X-Client-Id"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldExtractSubClaimFromJwt_WhenNoHeaderIsPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", "user-abc-999")
            })
        };
        var token = tokenHandler.CreateEncodedJwt(tokenDescriptor);
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        var middleware = new ClientIdentificationMiddleware(next: (innerContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("user-user-abc-999", context.Request.Headers["X-Client-Id"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldExtractClientIdClaimFromJwt_WhenNoHeaderIsPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("client_id", "service-client-456")
            })
        };
        var token = tokenHandler.CreateEncodedJwt(tokenDescriptor);
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        var middleware = new ClientIdentificationMiddleware(next: (innerContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("client-service-client-456", context.Request.Headers["X-Client-Id"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldFallbackToIp_WhenNeitherHeaderNorJwtIsPresent()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.50");

        var middleware = new ClientIdentificationMiddleware(next: (innerContext) => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("ip-192.168.1.50", context.Request.Headers["X-Client-Id"].ToString());
    }
}
