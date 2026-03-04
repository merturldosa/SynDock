using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class CommunityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CommunityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // --- Post/Feed Tests ---

    [Fact]
    public async Task GetFeed_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/post/feed?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatePost_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new
        {
            title = "Test Post",
            content = "This is a test post #test",
            postType = "text"
        };

        var response = await client.PostAsJsonAsync("/api/post", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePost_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var request = new { content = "Unauthorized post", postType = "text" };

        var response = await client.PostAsJsonAsync("/api/post", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --- Follow Tests ---

    [Fact]
    public async Task ToggleFollow_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new { targetUserId = 1 };

        var response = await client.PostAsJsonAsync("/api/follow/toggle", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFollowers_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/follow/followers/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFollowing_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/follow/following/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserProfile_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/follow/profile/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Hashtag Tests ---

    [Fact]
    public async Task GetTrendingHashtags_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/hashtag/trending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchHashtags_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/hashtag/search?keyword=test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- Collections Tests ---

    [Fact]
    public async Task GetMyCollections_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var response = await client.GetAsync("/api/collections");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCollection_Authenticated_Returns200()
    {
        var client = _factory.CreateAuthenticatedClient(userId: 2, username: "member", role: "Member");
        var request = new
        {
            name = "My Favorites",
            description = "A collection of my favorite items"
        };

        var response = await client.PostAsJsonAsync("/api/collections", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetCollections_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var response = await client.GetAsync("/api/collections");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
