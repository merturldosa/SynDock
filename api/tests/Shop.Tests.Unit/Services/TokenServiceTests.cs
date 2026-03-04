using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Shop.Domain.Entities;
using Shop.Infrastructure.Services;
using Shop.Tests.Unit.TestFixtures;

namespace Shop.Tests.Unit.Services;

public class TokenServiceTests
{
    private readonly TokenService _sut;
    private readonly IConfiguration _configuration;

    private const string Secret = "SynDockShopPlatform2026SuperSecretKey!@#$%^&*()_+ForJWTAuth";
    private const string Issuer = "SynDock.Shop";
    private const string Audience = "SynDock.Shop.Client";

    public TokenServiceTests()
    {
        var config = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = Secret,
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:ExpirationMinutes"] = "60"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();

        _sut = new TokenService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var tenant = TestDataBuilder.CreateTenant();

        // Act
        var token = _sut.GenerateAccessToken(user, tenant);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var jwt = handler.ReadJwtToken(token);
        jwt.Issuer.Should().Be(Issuer);
        jwt.Audiences.Should().Contain(Audience);
    }

    [Fact]
    public void GenerateAccessToken_ContainsTenantClaim()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var tenant = TestDataBuilder.CreateTenant(id: 5, slug: "mohyun");

        // Act
        var token = _sut.GenerateAccessToken(user, tenant);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "tenant_id" && c.Value == "5");
        jwt.Claims.Should().Contain(c => c.Type == "tenant_slug" && c.Value == "mohyun");
    }

    [Fact]
    public void GenerateAccessToken_ContainsRoleClaim()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(role: "Admin");
        var tenant = TestDataBuilder.CreateTenant();

        // Act
        var token = _sut.GenerateAccessToken(user, tenant);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void ValidateAccessToken_ReturnsUserId_ForValidToken()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser(id: 42);
        var tenant = TestDataBuilder.CreateTenant();
        var token = _sut.GenerateAccessToken(user, tenant);

        // Act
        var userId = _sut.ValidateAccessToken(token);

        // Assert
        userId.Should().Be(42);
    }

    [Fact]
    public void ValidateAccessToken_ReturnsNull_ForExpiredToken()
    {
        // Arrange - create token with -1 min expiration (already expired)
        var config = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = Secret,
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:ExpirationMinutes"] = "0"
        };
        var expiredConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
        var service = new TokenService(expiredConfig);

        var user = TestDataBuilder.CreateUser();
        var tenant = TestDataBuilder.CreateTenant();

        // Manually create an expired token
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiredJwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "1")],
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: credentials);
        var expiredToken = new JwtSecurityTokenHandler().WriteToken(expiredJwt);

        // Act
        var userId = _sut.ValidateAccessToken(expiredToken);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_ReturnsNull_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid.payload";

        // Act
        var userId = _sut.ValidateAccessToken(invalidToken);

        // Assert
        userId.Should().BeNull();
    }
}
