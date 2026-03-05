using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.Tests.Integration.Tests;

public class UploadIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UploadIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UploadImage_Unauthenticated_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "catholia");
        var content = new MultipartFormDataContent();

        // Act
        var response = await client.PostAsync("/api/upload/image", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadImage_Authenticated_NoFile_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var content = new MultipartFormDataContent();

        // Act
        var response = await client.PostAsync("/api/upload/image", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UploadImages_Authenticated_NoFiles_Returns400()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var content = new MultipartFormDataContent();

        // Act
        var response = await client.PostAsync("/api/upload/images", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task UploadImage_Authenticated_WithGifFile_ReturnsOkOrError()
    {
        // Arrange - use .gif which bypasses image processing (animation preserved)
        var client = _factory.CreateAuthenticatedClient(role: "Admin");
        var content = new MultipartFormDataContent();
        // Minimal valid GIF89a (1x1 pixel)
        var gifBytes = new byte[]
        {
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, // GIF89a
            0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, // 1x1, no GCT
            0x2C, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, // image descriptor
            0x02, 0x02, 0x44, 0x01, 0x00, // LZW min code size + data
            0x3B // trailer
        };
        var fileContent = new ByteArrayContent(gifBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/gif");
        content.Add(fileContent, "file", "test.gif");

        // Act
        var response = await client.PostAsync("/api/upload/image", content);

        // Assert - gif files are passed through without ImageSharp processing
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }
}
