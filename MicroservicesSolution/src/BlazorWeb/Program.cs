using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorFrontend;
using BlazorFrontend.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using BlazorWeb;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 🔐 CONFIGURACIÓN DE AUTENTICACIÓN KEYCLOAK
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Keycloak", options.ProviderOptions);
    
    // Configuración adicional
    options.ProviderOptions.DefaultScopes.Add("roles");
    options.ProviderOptions.DefaultScopes.Add("email");
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.UserOptions.RoleClaim = "roles";
    options.UserOptions.NameClaim = "preferred_username";
});

// Configurar HttpClient para APIs CON AUTENTICACIÓN
builder.Services.AddHttpClient("ApiPago", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiEndpoints:ApiPago"] ?? "http://api-pago:8080");
});
//API - PAGO NO TINE TOKEN

builder.Services.AddHttpClient("ApiPedidos", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiEndpoints:ApiPedidos"] ?? "http://api-pedidos:8080");
})
.AddHttpMessageHandler<TokenHandler>();

builder.Services.AddHttpClient("ApiConsulta", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiEndpoints:ApiConsulta"] ?? "http://api-consulta:8080");
})
.AddHttpMessageHandler<TokenHandler>();

// HttpClient base para requests no autenticados
builder.Services.AddScoped(sp => new HttpClient { 
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
});

// Registrar servicios
builder.Services.AddScoped<TokenHandler>();
builder.Services.AddScoped<ApiPedidosService>();
builder.Services.AddScoped<ApiPagoService>();
builder.Services.AddScoped<ApiConsultaService>();
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();