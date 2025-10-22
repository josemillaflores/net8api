using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Serilog;

namespace ApiPedidos.Presentation;

public static class PresentationServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // ✅ LEER DESDE KeycloakSettings
        var keycloakSection = configuration.GetSection("KeycloakSettings");
        var keycloakAuthority = keycloakSection["Authority"] ?? "http://keycloak:8080/realms/MicroservicesRealm";
        var keycloakAudience = keycloakSection["Audience"] ?? "shopping-api";
        var metadataAddress = keycloakSection["MetadataAddress"] ?? $"{keycloakAuthority}/.well-known/openid-configuration";

        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
        
        // ✅ USAR SERILOG EN LUGAR DE CONSOLE.WRITELINE
        Log.Information("🔐 Configurando Keycloak desde sección KeycloakSettings:");
        Log.Information("🔐   Authority: {Authority}", keycloakAuthority);
        Log.Information("🔐   Audience: {Audience}", keycloakAudience);
        Log.Information("🔐   Metadata: {Metadata}", metadataAddress);
        Log.Information("🔐   Environment: {Environment}", environment);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = keycloakAuthority;
                options.Audience = keycloakAudience;
                options.RequireHttpsMetadata = false;
                options.MetadataAddress = metadataAddress;
                
                // ✅ CONFIGURACIÓN ROBUSTA PARA KEYCLOAK - SEGÚN TUS LOGS
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = environment != "Development",
                    ValidIssuer = keycloakAuthority,
                    ValidateAudience = environment != "Development",
                    ValidAudience = keycloakAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = environment != "Development",
                    // ✅ CLAVE: Usar el claim type EXACTO que envía Keycloak
                    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                    NameClaimType = "preferred_username",
                    ClockSkew = TimeSpan.FromMinutes(5)
                };
                
                // ✅ CONFIGURACIÓN ROBUSTA PARA DOCKER
                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Log.Error(context.Exception, "🔐 AUTH FAILED: {Message}", context.Exception.Message);
                        
                        if (context.Exception is HttpRequestException httpEx)
                        {
                            Log.Warning("🔐 Network error: {Message}. Trying to reach: {Authority}", 
                                httpEx.Message, keycloakAuthority);
                            
                            if (environment == "Development")
                            {
                                Log.Information("🔐 Development mode: Bypassing network error");
                                context.NoResult();
                                return Task.CompletedTask;
                            }
                        }
                        
                        return Task.CompletedTask;
                    },
                    
                    OnChallenge = context =>
                    {
                        Log.Warning("🔐 Authentication Challenge: {Error}. Description: {Description}", 
                            context.Error, context.ErrorDescription);
                        return Task.CompletedTask;
                    },
                    
                    OnTokenValidated = context =>
                    {
                        Log.Information("✅ Token validated for user: {User}", context.Principal?.Identity?.Name);
                        
                        // ✅ DEBUG COMPLETO SEGÚN TUS LOGS
                        if (environment == "Development")
                        {
                            var identity = context.Principal?.Identity as ClaimsIdentity;
                            if (identity != null)
                            {
                                Log.Debug("=== 🎯 CLAIMS ANALYSIS ===");
                                
                                // Mostrar claims importantes
                                var importantClaims = new[]
                                {
                                    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                                    "roles",
                                    ClaimTypes.Role,
                                    "realm_access",
                                    "preferred_username",
                                    "name",
                                    "email"
                                };
                                
                                foreach (var claimType in importantClaims)
                                {
                                    var claims = identity.FindAll(claimType).ToList();
                                    if (claims.Any())
                                    {
                                        Log.Debug("🔐 {ClaimType}: {Claims}", 
                                            claimType, string.Join(", ", claims.Select(c => c.Value)));
                                    }
                                }
                                
                                // Verificar roles específicos
                                Log.Debug("=== 🎯 ROLES VERIFICATION ===");
                                Log.Debug("🔐 IsInRole('api-service'): {IsApiService}", 
                                    context.Principal?.IsInRole("api-service"));
                                Log.Debug("🔐 IsInRole('admin'): {IsAdmin}", 
                                    context.Principal?.IsInRole("admin"));
                                
                                // Transformación adicional para garantizar compatibilidad
                                TransformRolesForCompatibility(identity);
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        // ✅ CONFIGURACIÓN DE AUTORIZACIÓN ROBUSTA
        services.AddAuthorization(options =>
        {
            // ✅ POLÍTICA PRINCIPAL MEJORADA
            options.AddPolicy("RequireApiServiceRole", policy =>
            {
                policy.RequireAuthenticatedUser();
                
                // ✅ MÚLTIPLES MÉTODOS DE VERIFICACIÓN
                policy.RequireAssertion(context =>
                {
                    if (context.User.Identity?.IsAuthenticated != true)
                        return false;

                    // Método 1: Verificación estándar con RoleClaimType configurado
                    if (context.User.IsInRole("api-service") || context.User.IsInRole("admin"))
                    {
                        Log.Debug("✅ Access granted via IsInRole");
                        return true;
                    }

                    // Método 2: Verificación directa de claims
                    var hasRoleClaim = context.User.HasClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "api-service") ||
                                      context.User.HasClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "admin") ||
                                      context.User.HasClaim("roles", "api-service") ||
                                      context.User.HasClaim("roles", "admin");

                    if (hasRoleClaim)
                    {
                        Log.Debug("✅ Access granted via direct claim check");
                        return true;
                    }

                    // Método 3: Verificación en realm_access (fallback)
                    var realmAccessClaim = context.User.FindFirst("realm_access");
                    if (realmAccessClaim != null)
                    {
                        try
                        {
                            var realmAccess = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);
                            if (realmAccess.RootElement.TryGetProperty("roles", out var roles))
                            {
                                foreach (var role in roles.EnumerateArray())
                                {
                                    var roleValue = role.GetString();
                                    if (roleValue == "api-service" || roleValue == "admin")
                                    {
                                        Log.Debug("✅ Access granted via realm_access: {Role}", roleValue);
                                        return true;
                                    }
                                }
                            }
                        }
                        catch (System.Text.Json.JsonException) { }
                    }

                    // Debug información
                    Log.Warning("❌ Access denied for user: {User}", context.User.Identity?.Name);
                    var allRoleClaims = context.User.FindAll(ClaimTypes.Role)
                        .Concat(context.User.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role"))
                        .Concat(context.User.FindAll("roles"))
                        .Select(c => $"{c.Type}={c.Value}")
                        .Distinct();
                    
                    Log.Debug("🔐 Available role claims: {Claims}", string.Join(", ", allRoleClaims));
                    
                    return false;
                });
            });
            
            // ✅ POLÍTICA POR DEFECTO MEJORADA PARA DESARROLLO
            if (environment == "Development")
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAssertion(context => 
                    {
                        Log.Debug("🔐 Development mode - Allowing request for: {User}", context.User.Identity?.Name);
                        return true;
                    })
                    .Build();
                    
                Log.Information("🔐 Development mode: Using permissive authorization policy");
            }
        });

        // ✅ SWAGGER CONFIGURADO
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo 
            { 
                Title = "API Pedidos - MicroservicesRealm", 
                Version = "v1",
                Description = "Microservicio para gestión de pedidos protegido con Keycloak"
            });

            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Password = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/token"),
                        AuthorizationUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/auth"),
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
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        }
                    },
                    new List<string>()
                }
            });
        });

        // ✅ CORS ACTUALIZADO (corregido typo)
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
                        "http://blazor-fronted:80", // ✅ Corregido: http:// en lugar de http:
                        "http://keycloak:8080",
                        "http://api-pedidos:8080",
                        "http://api-pago:8080",
                        "http://api-consulta:8080")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        // ✅ HEALTH CHECKS
        services.AddHealthChecks();

        Log.Information("✅ Servicios de presentación configurados exitosamente");

        return services;
    }

    private static void TransformRolesForCompatibility(ClaimsIdentity identity)
    {
        // Transformar roles de Keycloak a ClaimTypes.Role estándar
        var keycloakRoleClaims = identity.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        foreach (var roleClaim in keycloakRoleClaims)
        {
            if (!identity.HasClaim(ClaimTypes.Role, roleClaim.Value))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                Log.Debug("✅ Transformed role: {Role} -> ClaimTypes.Role", roleClaim.Value);
            }
        }

        // Transformar claims 'roles' (alternativo)
        var rolesClaims = identity.FindAll("roles");
        foreach (var roleClaim in rolesClaims)
        {
            if (!identity.HasClaim(ClaimTypes.Role, roleClaim.Value))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                Log.Debug("✅ Transformed role: {Role} from 'roles' claim", roleClaim.Value);
            }
        }
    }
}