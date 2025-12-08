using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts; // Add this

namespace EvoAITest.Tests.Utilities;

/// <summary>
/// Helper class for generating test images for visual regression testing.
/// </summary>
public static class TestImageGenerator
{
    /// <summary>
    /// Creates a solid color image.
    /// </summary>
    public static byte[] CreateSolidColorImage(int width, int height, Color color)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => ctx.BackgroundColor(color));
        
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates an image with text.
    /// </summary>
    public static byte[] CreateImageWithText(int width, int height, string text, Color backgroundColor, Color textColor)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(backgroundColor);
            
            // Use SystemFonts from SixLabors.Fonts
            if (SystemFonts.TryGet("Arial", out var fontFamily))
            {
                var font = fontFamily.CreateFont(24);
                var textOptions = new RichTextOptions(font)
                {
                    Origin = new PointF(10, 10),
                    WrappingLength = width - 20
                };
                
                ctx.DrawText(textOptions, text, textColor);
            }
        });
        
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates an image with a rectangle.
    /// </summary>
    public static byte[] CreateImageWithRectangle(
        int width, 
        int height, 
        Color backgroundColor, 
        int rectX, 
        int rectY, 
        int rectWidth, 
        int rectHeight, 
        Color rectColor)
    {
        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx =>
        {
            ctx.BackgroundColor(backgroundColor);
            ctx.Fill(rectColor, new Rectangle(rectX, rectY, rectWidth, rectHeight));
        });
        
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates an image with slight pixel differences.
    /// </summary>
    public static byte[] CreateImageWithSlightDifference(byte[] baseImage, int differenceCount = 100)
    {
        using var image = Image.Load<Rgba32>(baseImage);
        
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < differenceCount; i++)
        {
            var x = random.Next(0, image.Width);
            var y = random.Next(0, image.Height);
            
            var pixel = image[x, y];
            pixel.R = (byte)Math.Min(255, pixel.R + 10);
            image[x, y] = pixel;
        }
        
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates an image with a specific region modified.
    /// </summary>
    public static byte[] CreateImageWithRegionDifference(
        byte[] baseImage, 
        int regionX, 
        int regionY, 
        int regionWidth, 
        int regionHeight, 
        Color newColor)
    {
        using var image = Image.Load<Rgba32>(baseImage);
        
        image.Mutate(ctx =>
        {
            ctx.Fill(newColor, new Rectangle(regionX, regionY, regionWidth, regionHeight));
        });
        
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates a gradient image.
    /// </summary>
    public static byte[] CreateGradientImage(int width, int height, Color startColor, Color endColor)
    {
        using var image = new Image<Rgba32>(width, height);
        
        // Convert Color to Rgba32 for accessing components
        var start = startColor.ToPixel<Rgba32>();
        var end = endColor.ToPixel<Rgba32>();
        
        for (int y = 0; y < height; y++)
        {
            var ratio = (float)y / height;
            var r = (byte)(start.R + (end.R - start.R) * ratio);
            var g = (byte)(start.G + (end.G - start.G) * ratio);
            var b = (byte)(start.B + (end.B - start.B) * ratio);
            
            for (int x = 0; x < width; x++)
            {
                image[x, y] = new Rgba32(r, g, b);
            }
        }
        
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates a checkerboard pattern image.
    /// </summary>
    public static byte[] CreateCheckerboardImage(int width, int height, int squareSize, Color color1, Color color2)
    {
        using var image = new Image<Rgba32>(width, height);
        
        // Convert Color to Rgba32
        var rgba1 = color1.ToPixel<Rgba32>();
        var rgba2 = color2.ToPixel<Rgba32>();
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var isColor1 = ((x / squareSize) + (y / squareSize)) % 2 == 0;
                image[x, y] = isColor1 ? rgba1 : rgba2;
            }
        }
        
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates an identical copy of an image.
    /// </summary>
    public static byte[] CloneImage(byte[] sourceImage)
    {
        using var image = Image.Load<Rgba32>(sourceImage);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// Creates a test page HTML for browser testing.
    /// </summary>
    public static string CreateTestPageHtml(string title, string content, bool includeTimestamp = false)
    {
        var timestamp = includeTimestamp ? $"<div id=\"timestamp\">{DateTimeOffset.UtcNow:o}</div>" : "";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <title>{title}</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; }}
        .content {{ margin: 20px 0; }}
        #timestamp {{ position: absolute; top: 10px; right: 10px; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>{title}</h1>
    </div>
    <div class=""content"">
        {content}
    </div>
    {timestamp}
</body>
</html>";
    }
}
