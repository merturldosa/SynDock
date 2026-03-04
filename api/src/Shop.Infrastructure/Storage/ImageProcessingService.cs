using Shop.Application.Common.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Shop.Infrastructure.Storage;

public class ImageProcessingService : IImageProcessingService
{
    public async Task<Stream> ResizeAndConvertAsync(Stream input, int maxWidth = 1200, int maxHeight = 1200, int quality = 80)
    {
        using var image = await Image.LoadAsync(input);

        if (image.Width > maxWidth || image.Height > maxHeight)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(maxWidth, maxHeight),
                Mode = ResizeMode.Max
            }));
        }

        var output = new MemoryStream();
        await image.SaveAsync(output, new WebpEncoder { Quality = quality });
        output.Position = 0;
        return output;
    }

    public async Task<Stream> CreateThumbnailAsync(Stream input, int width = 300, int height = 300, int quality = 75)
    {
        using var image = await Image.LoadAsync(input);

        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Max
        }));

        var output = new MemoryStream();
        await image.SaveAsync(output, new WebpEncoder { Quality = quality });
        output.Position = 0;
        return output;
    }
}
