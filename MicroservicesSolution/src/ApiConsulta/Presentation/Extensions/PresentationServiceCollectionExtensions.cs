using ApiConsulta.Presentation.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text.Json;

namespace ApiConsulta.Presentation.Extensions;

public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // Security
        services.AddAuthentication(configuration);
        services.AddAuthorization(configuration);
        
        // API
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(configuration);
        
        // HTTP
        services.ConfigureHttpClients();
        services.ConfigureJsonOptions();
        
        // Health Checks
        services.AddHealthChecks()
            .AddCheck("api-consulta", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

        return services;
    }

    private static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloakSection = configuration.GetSection("KeycloakSettings");
        var keycloakAuthority = keycloakSection["Authority"] ?? "http://keycloak:8080/realms/MicroservicesRealm";
        var keycloakAudience = keycloakSection["Audience"] ?? "shopping-api";
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloakAuthority;
                options.Audience = keycloakAudience;
                options.RequireHttpsMetadata = false;
                options.MetadataAddress = $"{keycloakAuthority}/.well-known/openid-configuration";
                
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = environment != "Development",
                    ValidIssuer = keycloakAuthority,
                    ValidateAudience = environment != "Development",
                    ValidAudience = keycloakAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = environment != "Development",
                    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                    NameClaimType = "preferred_username",
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
                
                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                
                options.Events = CreateJwtBearerEvents(environment);
            });

        return services;
    }

    private static JwtBearerEvents CreateJwtBearerEvents(string environment) => new()
    {
        OnAuthenticationFailed = context => HandleAuthenticationFailed(context, environment),
        OnTokenValidated = context => HandleTokenValidated(context, environment)
    };

    private static Task HandleAuthenticationFailed(AuthenticationFailedContext context, string environment)
    {
        Console.WriteLine($"🔐 AUTH FAILED: {context.Exception.Message}");
        
        if (context.Exception is HttpRequestException httpEx && environment == "Development")
        {
            Console.WriteLine("🔐 Development mode: Bypassing network error");
            context.NoResult();
        }
        
        return Task.CompletedTask;
    }

    private static Task HandleTokenValidated(TokenValidatedContext context, string environment)
    {
        Console.WriteLine($"✅ Token validated for user: {context.Principal?.Identity?.Name}");
        
        if (environment == "Development" && context.Principal?.Identity is ClaimsIdentity identity)
        {
            TransformRolesForCompatibility(identity);
        }
        
        return Task.CompletedTask;
    }

    private static void TransformRolesForCompatibility(ClaimsIdentity identity)
    {
        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim != null)
        {
            try
            {
                var realmAccess = JsonDocument.Parse(realmAccessClaim.Value);
                if (realmAccess.RootElement.TryGetProperty("roles", out var roles))
                {
                    foreach (var role in roles.EnumerateArray())
                    {
                        var roleValue = role.GetString();
                        if (!identity.HasClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", roleValue))
                        {
                            identity.AddClaim(new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", roleValue));
                        }
                    }
                }
            }
            catch (JsonException) { }
        }
    }

    private static IServiceCollection AddAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";

        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireApiServiceRole", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireAssertion(context => AuthorizationHelper.ValidateRoles(context)));

            if (environment == "Development")
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAssertion(context => true)
                    .Build();
            }
        });

        return services;
    }

    private static IServiceCollection AddSwaggerGen(this IServiceCollection services, IConfiguration configuration)
    {
        var keycloakAuthority = configuration["KeycloakSettings:Authority"] ?? "http://keycloak:8080/realms/MicroservicesRealm";

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "API Consulta - MicroservicesRealm",
                Version = "v1",
                Description = "Microservicio para consultas de pedidos y pagos protegido con Keycloak"
            });

            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/auth"),
                        TokenUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            { "openid", "OpenID Connect" },
                            { "profile", "User profile" },
                            { "email", "Email address" }
                        }
                    }
                },
                Description = "Keycloak Authentication"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                    },
                    new List<string>()
                }
            });
        });
        
        services.AddCors(options =>
        {
            options.AddPolicy("KeycloakCors", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:15001",
                        "http://localhost:15002",
                        "http://localhost:15003",
                        "http://localhost:18080",
                        "http://localhost:15004",
                        "http:blazor-fronted:80",
                        "http://keycloak:8080",
                        "http://api-pedidos:8080",
                        "http://api-pago:8080",
                        "http://api-consulta:8080")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    private static IServiceCollection ConfigureHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("Default")
            .ConfigurePrimaryHttpMessageHandler(() => 
                new HttpClientHandler 
                { 
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true 
                });

        return services;
    }

    private static IServiceCollection ConfigureJsonOptions(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
        });

        return services;
    }
}

// Helper class for authorization logic
public static class AuthorizationHelper
{
    public static bool ValidateRoles(AuthorizationHandlerContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return false;

        // Role verification logic
        var validRoles = new[] { "api-service", "admin", "consulta" };
        
        return validRoles.Any(role => 
            context.User.IsInRole(role) ||
            context.User.HasClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", role) ||
            context.User.HasClaim("roles", role) ||
            HasRoleInRealmAccess(context.User, role));
    }

    private static bool HasRoleInRealmAccess(ClaimsPrincipal user, string role)
    {
        var realmAccessClaim = user.FindFirst("realm_access");
        if (realmAccessClaim != null)
        {
            try
            {
                var realmAccess = JsonDocument.Parse(realmAccessClaim.Value);
                if (realmAccess.RootElement.TryGetProperty("roles", out var roles))
                {
                    return roles.EnumerateArray().Any(r => r.GetString() == role);
                }
            }
            catch (JsonException) { }
        }
        return false;
    }
}