using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;

namespace BlazorFrontend.Services;

public class AuthService
{
    private readonly NavigationManager _navigation;
    private readonly SignOutSessionStateManager _signOutManager;
    private readonly AuthenticationStateProvider _authStateProvider;

    
    public AuthService(
        NavigationManager navigation,
        SignOutSessionStateManager signOutManager,
        AuthenticationStateProvider authStateProvider)
    {
        _navigation = navigation;
        _signOutManager = signOutManager;
        _authStateProvider = authStateProvider;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }

    public async Task<string?> GetUserNameAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst("preferred_username")?.Value 
            ?? authState.User.FindFirst("name")?.Value 
            ?? authState.User.Identity?.Name;
    }

    public async Task<string?> GetEmailAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst("email")?.Value;
    }

    public async Task<IEnumerable<string>> GetRolesAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        return authState.User.Claims
            .Where(c => c.Type == "role")
            .Select(c => c.Value);
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        var roles = await GetRolesAsync();
        return roles.Contains(role);
    }

    public async Task LogoutAsync()
    {
        await _signOutManager.SetSignOutState();
        _navigation.NavigateTo("authentication/logout");
    }
}