using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SynDock.Core.Interfaces;

namespace Shop.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User
                ?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User
                    ?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return id != null ? int.Parse(id) : null;
        }
    }

    public string? Username => _httpContextAccessor.HttpContext?.User
        ?.FindFirst(ClaimTypes.Name)?.Value
        ?? _httpContextAccessor.HttpContext?.User
            ?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

    public string? Role => _httpContextAccessor.HttpContext?.User
        ?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
