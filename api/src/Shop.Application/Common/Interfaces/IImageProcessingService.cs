namespace Shop.Application.Common.Interfaces;

public interface IImageProcessingService
{
    Task<Stream> ResizeAndConvertAsync(Stream input, int maxWidth = 1200, int maxHeight = 1200, int quality = 80);
    Task<Stream> CreateThumbnailAsync(Stream input, int width = 300, int height = 300, int quality = 75);
}
