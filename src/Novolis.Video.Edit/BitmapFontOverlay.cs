using Novolis.Video.Rtc;

namespace Novolis.Video.Edit;

/// <summary>Tiny 5×7 bitmap font burn-in so export works without Avalonia.</summary>
public sealed class BitmapFontOverlay : ITextOverlayRenderer
{
    // Digits/letters A-Z and a few punctuation glyphs, packed as 5 columns × 7 rows (LSB top).
    static readonly Dictionary<char, byte[]> Glyphs = BuildGlyphs();

    /// <inheritdoc />
    public void Apply(VideoFrame frame, IReadOnlyList<TextOverlay> overlays)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(overlays);
        foreach (var overlay in overlays)
            DrawCentered(frame, overlay);
    }

    static void DrawCentered(VideoFrame frame, TextOverlay overlay)
    {
        const int scale = 3;
        const int glyphW = 5;
        const int glyphH = 7;
        const int advance = (glyphW + 1) * scale;
        var text = overlay.Text.ToUpperInvariant();
        var widthPx = text.Length * advance;
        var heightPx = glyphH * scale;
        var originX = (int)(frame.Width * overlay.AnchorX - widthPx / 2.0);
        var originY = (int)(frame.Height * overlay.AnchorY - heightPx / 2.0);
        var color = overlay.Color;

        for (var i = 0; i < text.Length; i++)
        {
            if (!Glyphs.TryGetValue(text[i], out var cols))
                continue;
            var gx = originX + i * advance;
            for (var col = 0; col < glyphW; col++)
            {
                var bits = cols[col];
                for (var row = 0; row < glyphH; row++)
                {
                    if ((bits & (1 << row)) == 0)
                        continue;
                    FillRect(
                        frame,
                        gx + col * scale,
                        originY + row * scale,
                        scale,
                        scale,
                        color);
                }
            }
        }
    }

    static void FillRect(VideoFrame frame, int x, int y, int w, int h, Rgba8 color)
    {
        for (var yy = y; yy < y + h; yy++)
        {
            if (yy < 0 || yy >= frame.Height)
                continue;
            for (var xx = x; xx < x + w; xx++)
            {
                if (xx < 0 || xx >= frame.Width)
                    continue;
                var i = yy * frame.Stride + xx * 4;
                frame.Pixels[i] = color.B;
                frame.Pixels[i + 1] = color.G;
                frame.Pixels[i + 2] = color.R;
                frame.Pixels[i + 3] = color.A;
            }
        }
    }

    static Dictionary<char, byte[]> BuildGlyphs()
    {
        // Each entry: 5 column bitmasks (bit0 = top row).
        var d = new Dictionary<char, byte[]>
        {
            [' '] = [0, 0, 0, 0, 0],
            ['-'] = [0, 8, 8, 8, 0],
            ['.'] = [0, 0, 64, 0, 0],
            ['!'] = [0, 0, 95, 0, 0],
            [':'] = [0, 0, 20, 0, 0],
            ['0'] = [62, 65, 65, 65, 62],
            ['1'] = [0, 66, 127, 64, 0],
            ['2'] = [98, 81, 73, 73, 70],
            ['3'] = [34, 65, 73, 73, 54],
            ['4'] = [28, 20, 18, 127, 16],
            ['5'] = [39, 69, 69, 69, 57],
            ['6'] = [62, 73, 73, 73, 48],
            ['7'] = [1, 113, 9, 5, 3],
            ['8'] = [54, 73, 73, 73, 54],
            ['9'] = [6, 73, 73, 73, 62],
            ['A'] = [126, 17, 17, 17, 126],
            ['B'] = [127, 73, 73, 73, 54],
            ['C'] = [62, 65, 65, 65, 34],
            ['D'] = [127, 65, 65, 65, 62],
            ['E'] = [127, 73, 73, 73, 65],
            ['F'] = [127, 9, 9, 9, 1],
            ['G'] = [62, 65, 73, 73, 58],
            ['H'] = [127, 8, 8, 8, 127],
            ['I'] = [65, 65, 127, 65, 65],
            ['J'] = [32, 64, 65, 63, 1],
            ['K'] = [127, 8, 20, 34, 65],
            ['L'] = [127, 64, 64, 64, 64],
            ['M'] = [127, 2, 12, 2, 127],
            ['N'] = [127, 4, 8, 16, 127],
            ['O'] = [62, 65, 65, 65, 62],
            ['P'] = [127, 9, 9, 9, 6],
            ['Q'] = [62, 65, 81, 33, 94],
            ['R'] = [127, 9, 25, 41, 70],
            ['S'] = [38, 73, 73, 73, 50],
            ['T'] = [1, 1, 127, 1, 1],
            ['U'] = [63, 64, 64, 64, 63],
            ['V'] = [31, 32, 64, 32, 31],
            ['W'] = [127, 32, 24, 32, 127],
            ['X'] = [99, 20, 8, 20, 99],
            ['Y'] = [3, 4, 120, 4, 3],
            ['Z'] = [97, 81, 73, 69, 67],
        };
        return d;
    }
}
