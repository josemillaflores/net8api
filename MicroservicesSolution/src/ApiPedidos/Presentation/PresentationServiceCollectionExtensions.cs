using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        
        Console.WriteLine($"🔐 Configuring Keycloak from KeycloakSettings section:");
        Console.WriteLine($"🔐   Authority: {keycloakAuthority}");
        Console.WriteLine($"🔐   Audience: {keycloakAudience}");
        Console.WriteLine($"🔐   Metadata: {metadataAddress}");
        Console.WriteLine($"🔐   Environment: {environment}");

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
                        Console.WriteLine($"🔐 AUTH FAILED: {context.Exception.Message}");
                        Console.WriteLine($"🔐 Exception Type: {context.Exception.GetType().Name}");
                        
                        if (context.Exception is HttpRequestException httpEx)
                        {
                            Console.WriteLine($"🔐 Network error: {httpEx.Message}");
                            Console.WriteLine($"🔐 Trying to reach: {keycloakAuthority}");
                            
                            if (environment == "Development")
                            {
                                Console.WriteLine("🔐 Development mode: Bypassing network error");
                                context.NoResult();
                                return Task.CompletedTask;
                            }
                        }
                        
                        return Task.CompletedTask;
                    },
                    
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"🔐 Authentication Challenge: {context.Error}");
                        Console.WriteLine($"🔐 Challenge Description: {context.ErrorDescription}");
                        return Task.CompletedTask;
                    },
                    
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"✅ Token validated for user: {context.Principal?.Identity?.Name}");
                        
                        // ✅ DEBUG COMPLETO SEGÚN TUS LOGS
                        if (environment == "Development")
                        {
                            var identity = context.Principal?.Identity as ClaimsIdentity;
                            if (identity != null)
                            {
                                Console.WriteLine("=== 🎯 CLAIMS ANALYSIS ===");
                                
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
                                        Console.WriteLine($"🔐 {claimType}: {string.Join(", ", claims.Select(c => c.Value))}");
                                    }
                                }
                                
                                // Verificar roles específicos
                                Console.WriteLine("=== 🎯 ROLES VERIFICATION ===");
                                Console.WriteLine($"🔐 IsInRole('api-service'): {context.Principal?.IsInRole("api-service")}");
                                Console.WriteLine($"🔐 IsInRole('admin'): {context.Principal?.IsInRole("admin")}");
                                Console.WriteLine($"🔐 IsInRole('user'): {context.Principal?.IsInRole("user")}");
                                
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
                        Console.WriteLine($"✅ Access granted via IsInRole");
                        return true;
                    }

                    // Método 2: Verificación directa de claims
                    var hasRoleClaim = context.User.HasClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "api-service") ||
                                      context.User.HasClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "admin") ||
                                      context.User.HasClaim("roles", "api-service") ||
                                      context.User.HasClaim("roles", "admin");

                    if (hasRoleClaim)
                    {
                        Console.WriteLine($"✅ Access granted via direct claim check");
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
                                        Console.WriteLine($"✅ Access granted via realm_access: {roleValue}");
                                        return true;
                                    }
                                }
                            }
                        }
                        catch (System.Text.Json.JsonException) { }
                    }

                    // Debug información
                    Console.WriteLine("❌ Access denied - Debug info:");
                    var allRoleClaims = context.User.FindAll(ClaimTypes.Role)
                        .Concat(context.User.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role"))
                        .Concat(context.User.FindAll("roles"))
                        .Select(c => $"{c.Type}={c.Value}")
                        .Distinct();
                    
                    Console.WriteLine($"🔐 Available role claims: {string.Join(", ", allRoleClaims)}");
                    
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
                        Console.WriteLine($"🔐 Development mode - Allowing request for: {context.User.Identity?.Name}");
                        return true;
                    })
                    .Build();
                    
                Console.WriteLine("🔐 Development mode: Using permissive authorization policy");
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

        // ✅ CORS ACTUALIZADO
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

  
    private static void TransformRolesForCompatibility(ClaimsIdentity identity)
    {
        // Transformar roles de Keycloak a ClaimTypes.Role estándar
        var keycloakRoleClaims = identity.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
        foreach (var roleClaim in keycloakRoleClaims)
        {
            if (!identity.HasClaim(ClaimTypes.Role, roleClaim.Value))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                Console.WriteLine($"✅ Transformed role: {roleClaim.Value} -> ClaimTypes.Role");
            }
        }

       
        var rolesClaims = identity.FindAll("roles");
        foreach (var roleClaim in rolesClaims)
        {
            if (!identity.HasClaim(ClaimTypes.Role, roleClaim.Value))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                Console.WriteLine($"✅ Transformed role: {roleClaim.Value} from 'roles' claim");
            }
        }
    }
}