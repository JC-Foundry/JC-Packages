using QRCoder;

namespace JC.Web.Helpers;

public enum QrCodeFormat
{
    Svg,
    Base64
}

public class QrCodeHelper
{
    private readonly QRCodeGenerator.ECCLevel _eccLevel;
    private readonly QrCodeFormat _format;
    private readonly int _pixelsPerModule;
    public const string Base64ImgPrefix = "data:image/png;base64,";

    public QrCodeHelper() : this(QrCodeFormat.Svg, 10)
    {
    }

    /// <summary>
    /// Creates a QR code helper with the specified settings.
    /// </summary>
    /// <param name="format">Output format (SVG or Base64 PNG)</param>
    /// <param name="pixelsPerModule">Size of each QR module in pixels (typical: 5-20). For SVG, this affects the viewBox size.</param>
    /// <param name="eccLevel">Error correction level: L(7%), M(15%), Q(25%), H(30%)</param>
    public QrCodeHelper(QrCodeFormat format, int pixelsPerModule, QRCodeGenerator.ECCLevel eccLevel = QRCodeGenerator.ECCLevel.M)
    {
        _format = format;
        _eccLevel = eccLevel;
        _pixelsPerModule = pixelsPerModule <= 0 ? 10 : pixelsPerModule;
    }

    /// <summary>
    /// Generates a QR code from the provided content.
    /// </summary>
    /// <param name="content">The data to encode in the QR code</param>
    /// <returns>SVG string or base64-encoded PNG data URI</returns>
    public string GenerateQrCode(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty", nameof(content));

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, _eccLevel);

        return _format switch
        {
            QrCodeFormat.Svg => GenerateSvg(data),
            QrCodeFormat.Base64 => GenerateBase64Png(data),
            _ => throw new ArgumentOutOfRangeException(nameof(_format))
        };
    }

    private string GenerateSvg(QRCodeData data)
    {
        var svgQrCode = new SvgQRCode(data);
        return svgQrCode.GetGraphic(_pixelsPerModule);
    }

    private string GenerateBase64Png(QRCodeData data)
    {
        var pngQrCode = new PngByteQRCode(data);
        var pngBytes = pngQrCode.GetGraphic(_pixelsPerModule);
        var base64 = Convert.ToBase64String(pngBytes);
        return $"{Base64ImgPrefix}{base64}";
    }
}
