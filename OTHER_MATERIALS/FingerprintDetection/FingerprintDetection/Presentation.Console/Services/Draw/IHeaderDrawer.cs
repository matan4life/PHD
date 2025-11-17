using Microsoft.Extensions.Options;
using Presentation.Console.Exceptions;
using Presentation.Console.Models.Draw.Colors;
using Presentation.Console.Models.Draw.Fonts;
using VectSharp;

namespace Presentation.Console.Services.Draw;

public interface IHeaderDrawer
{
    Page DrawHeader(Page canvas, string headerText);
}

public sealed class HeaderDrawerService(
    IOptions<HeaderColorOptions>? colorOptions,
    IOptions<HeaderFontOptions>? fontOptions)
    : IHeaderDrawer
{
    private HeaderColorOptions ColorOptions =>
        colorOptions?.Value ?? throw new InvalidConfigurationException(nameof(colorOptions));

    private HeaderFontOptions FontOptions =>
        fontOptions?.Value ?? throw new InvalidConfigurationException(nameof(fontOptions));

    public Page DrawHeader(Page canvas, string headerText)
    {
        var font = new Font(FontOptions.FontFamily, FontOptions.FontSize, underlined: true);
        var measuredTextSize = font.MeasureText(headerText);
        var textPosition = new Point(
            (canvas.Width / 2 - measuredTextSize.Width) / 2,
            measuredTextSize.Height / 2);
        var canvasGraphics = canvas.Graphics;
        canvasGraphics.StrokeText(
            textPosition,
            headerText,
            font,
            ColorOptions.HeaderStrokeColor,
            TextBaselines.Middle,
            lineJoin: LineJoins.Round);
        canvasGraphics.FillText(
            textPosition,
            headerText,
            font,
            ColorOptions.HeaderFillColor,
            TextBaselines.Middle);
        
        return canvas;
    }
}