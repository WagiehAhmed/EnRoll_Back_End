using Microsoft.AspNetCore.Authentication.JwtBearer;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using UserGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Ocelot Configuration ------------------------------------------------------------------
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddOcelot(builder.Environment);

// Register Ocelot services
builder.Services.AddOcelot(builder.Configuration);

// JWT Authentication --------------------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = true;
    });

var app = builder.Build();

// Custom Middleware: Resolve ClientId & JWT User Claims prior to Rate Limiting
app.UseClientIdentification("X-Client-Id");

app.UseHttpsRedirection();
app.UseAuthentication();

// Execute Ocelot Gateway Pipeline
await app.UseOcelot();

app.Run();
