using FRELODYSHRD.Models.ChordDraw;
using System.Globalization;
using System.Text;

namespace FRELODYAPIs.Services.ChordCharts
{
    /// <summary>
    /// Renders a ChordDrawData to a self-contained SVG string for storage / OG cards / print
    /// fallback. Produces the "normal" (clean) style — JS-side svguitar handles handdrawn
    /// during live editing. Output is theme-neutral (uses currentColor where possible).
    /// </summary>
    public sealed class ChordSvgRenderer
    {
        // CellHeight = 1.5 × CellWidth matches svguitar's default fretSize so the saved
        // fallback SVG has the same neck proportions as the live JS preview / editor.
        private const double CellWidth = 40;
        private const double CellHeight = 60;
        private const double PaddingX = 28;
        private const double PaddingTop = 60;
        private const double PaddingBottom = 28;
        private const double TuningGap = 20;
        private const double TitleFontSize = 22;
        private const double FretLabelFontSize = 13;
        private const double TuningFontSize = 13;
        private const double FingerFontSize = 13;
        private const double OpenMutedFontSize = 16;
        private const double DotRadius = 16;
        private const double StrokeWidth = 1.6;

        public string Render(ChordDrawData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            var s = data.Settings ?? ChordDrawSettings.CreateDefault();
            var strings = Math.Max(2, s.Strings);
            var frets = Math.Max(1, s.Frets);
            var tuning = s.Tuning ?? Array.Empty<string>();
            var color = string.IsNullOrWhiteSpace(s.Color) ? "currentColor" : s.Color!;
            var bg = string.IsNullOrWhiteSpace(s.BackgroundColor) ? "transparent" : s.BackgroundColor!;

            var gridWidth = (strings - 1) * CellWidth;
            var gridHeight = frets * CellHeight;
            var width = gridWidth + PaddingX * 2;
            var height = gridHeight + PaddingTop + PaddingBottom + (tuning.Length > 0 ? TuningGap : 0);

            var sb = new StringBuilder();
            sb.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {F(width)} {F(height)}\" role=\"img\" aria-label=\"{Escape(s.Title ?? "Chord diagram")}\">");
            sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"0\" y=\"0\" width=\"{F(width)}\" height=\"{F(height)}\" fill=\"{Escape(bg)}\"/>");

            if (!string.IsNullOrWhiteSpace(s.Title))
            {
                sb.Append(CultureInfo.InvariantCulture, $"<text x=\"{F(width / 2)}\" y=\"{F(28)}\" text-anchor=\"middle\" font-family=\"system-ui, sans-serif\" font-size=\"{F(TitleFontSize)}\" font-weight=\"600\" fill=\"{Escape(color)}\">{Escape(s.Title!)}</text>");
            }

            var gridLeft = PaddingX;
            var gridTop = PaddingTop;
            var nutThick = s.Position <= 1 ? 4 : StrokeWidth;

            // Open/Muted markers above each string
            var openMuted = ComputeOpenMutedStates(data, strings);
            for (int i = 0; i < strings; i++)
            {
                var x = gridLeft + i * CellWidth;
                var y = gridTop - 14;
                var st = openMuted[i];
                if (st == ChordEmptyStringState.Open)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{F(x)}\" cy=\"{F(y)}\" r=\"6\" fill=\"none\" stroke=\"{Escape(color)}\" stroke-width=\"{F(StrokeWidth)}\"/>");
                }
                else if (st == ChordEmptyStringState.Muted)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"<g stroke=\"{Escape(color)}\" stroke-width=\"{F(StrokeWidth)}\" stroke-linecap=\"round\">");
                    sb.Append(CultureInfo.InvariantCulture, $"<line x1=\"{F(x - 5)}\" y1=\"{F(y - 5)}\" x2=\"{F(x + 5)}\" y2=\"{F(y + 5)}\"/>");
                    sb.Append(CultureInfo.InvariantCulture, $"<line x1=\"{F(x + 5)}\" y1=\"{F(y - 5)}\" x2=\"{F(x - 5)}\" y2=\"{F(y + 5)}\"/>");
                    sb.Append("</g>");
                }
            }

            // Nut / starting-fret label. Use linecap="butt" so the line ends exactly at
            // x1/x2 (square would extend by nutThick/2 and overshoot the outer strings,
            // whose default butt cap stops at gridLeft ± StrokeWidth/2).
            if (s.Position <= 1)
            {
                sb.Append(CultureInfo.InvariantCulture, $"<line x1=\"{F(gridLeft - StrokeWidth / 2)}\" y1=\"{F(gridTop)}\" x2=\"{F(gridLeft + gridWidth + StrokeWidth / 2)}\" y2=\"{F(gridTop)}\" stroke=\"{Escape(color)}\" stroke-width=\"{F(nutThick)}\" stroke-linecap=\"butt\"/>");
            }
            else
            {
                sb.Append(CultureInfo.InvariantCulture, $"<text x=\"{F(gridLeft - 12)}\" y=\"{F(gridTop + CellHeight / 2 + 4)}\" text-anchor=\"end\" font-family=\"system-ui, sans-serif\" font-size=\"{F(FretLabelFontSize)}\" fill=\"{Escape(color)}\">{s.Position}fr</text>");
            }

            // Horizontal fret lines
            for (int f = 1; f <= frets; f++)
            {
                var y = gridTop + f * CellHeight;
                sb.Append(CultureInfo.InvariantCulture, $"<line x1=\"{F(gridLeft)}\" y1=\"{F(y)}\" x2=\"{F(gridLeft + gridWidth)}\" y2=\"{F(y)}\" stroke=\"{Escape(color)}\" stroke-width=\"{F(StrokeWidth)}\"/>");
            }
            // Top edge (nut for position>1 is a thin line)
            if (s.Position > 1)
            {
                sb.Append(CultureInfo.InvariantCulture, $"<line x1=\"{F(gridLeft)}\" y1=\"{F(gridTop)}\" x2=\"{F(gridLeft + gridWidth)}\" y2=\"{F(gridTop)}\" stroke=\"{Escape(color)}\" stroke-width=\"{F(StrokeWidth)}\"/>");
            }

            // Vertical string lines
            for (int i = 0; i < strings; i++)
            {
                var x = gridLeft + i * CellWidth;
                sb.Append(CultureInfo.InvariantCulture, $"<line x1=\"{F(x)}\" y1=\"{F(gridTop)}\" x2=\"{F(x)}\" y2=\"{F(gridTop + gridHeight)}\" stroke=\"{Escape(color)}\" stroke-width=\"{F(StrokeWidth)}\"/>");
            }

            // Barres
            foreach (var b in data.Chord?.Barres ?? new())
            {
                if (b.Fret < 1 || b.Fret > frets) continue;
                var fromIdx = Math.Abs(b.FromString - strings);
                var toIdx = Math.Abs(b.ToString - strings);
                var lo = Math.Min(fromIdx, toIdx);
                var hi = Math.Max(fromIdx, toIdx);
                var x1 = gridLeft + lo * CellWidth;
                var x2 = gridLeft + hi * CellWidth;
                var y = gridTop + (b.Fret - 0.5) * CellHeight;
                var fill = string.IsNullOrWhiteSpace(b.Color) ? color : b.Color!;
                sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{F(x1 - DotRadius)}\" y=\"{F(y - DotRadius)}\" width=\"{F(x2 - x1 + DotRadius * 2)}\" height=\"{F(DotRadius * 2)}\" rx=\"{F(DotRadius)}\" ry=\"{F(DotRadius)}\" fill=\"{Escape(fill)}\"/>");
                if (!string.IsNullOrWhiteSpace(b.Text))
                {
                    var tx = (x1 + x2) / 2;
                    sb.Append(CultureInfo.InvariantCulture, $"<text x=\"{F(tx)}\" y=\"{F(y + 5)}\" text-anchor=\"middle\" font-family=\"system-ui, sans-serif\" font-size=\"{F(FingerFontSize)}\" fill=\"{Escape(b.TextColor ?? "#ffffff")}\">{Escape(b.Text!)}</text>");
                }
            }

            // Fingers
            foreach (var f in data.Chord?.Fingers ?? new())
            {
                if (f.Fret < 1 || f.Fret > frets) continue;
                var stringIdx = Math.Abs(f.String - strings);
                if (stringIdx < 0 || stringIdx >= strings) continue;
                var cx = gridLeft + stringIdx * CellWidth;
                var cy = gridTop + (f.Fret - 0.5) * CellHeight;
                var fill = string.IsNullOrWhiteSpace(f.Color) ? color : f.Color!;
                RenderShape(sb, f.Shape ?? ChordShape.Circle, cx, cy, DotRadius, fill);
                if (!string.IsNullOrWhiteSpace(f.Text))
                {
                    sb.Append(CultureInfo.InvariantCulture, $"<text x=\"{F(cx)}\" y=\"{F(cy + 5)}\" text-anchor=\"middle\" font-family=\"system-ui, sans-serif\" font-size=\"{F(FingerFontSize)}\" font-weight=\"600\" fill=\"{Escape(f.TextColor ?? "#ffffff")}\">{Escape(f.Text!)}</text>");
                }
            }

            // Tuning labels below grid
            if (tuning.Length > 0)
            {
                var ty = gridTop + gridHeight + TuningGap;
                for (int i = 0; i < strings; i++)
                {
                    var x = gridLeft + i * CellWidth;
                    var label = i < tuning.Length ? tuning[i] : string.Empty;
                    if (string.IsNullOrWhiteSpace(label)) continue;
                    sb.Append(CultureInfo.InvariantCulture, $"<text x=\"{F(x)}\" y=\"{F(ty)}\" text-anchor=\"middle\" font-family=\"system-ui, sans-serif\" font-size=\"{F(TuningFontSize)}\" fill=\"{Escape(color)}\">{Escape(label)}</text>");
                }
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static ChordEmptyStringState[] ComputeOpenMutedStates(ChordDrawData data, int strings)
        {
            var result = new ChordEmptyStringState[strings];
            for (int i = 0; i < strings; i++) result[i] = ChordEmptyStringState.NotEmpty;

            // Determine which string indices have any active finger or barre
            var hasFinger = new bool[strings];
            foreach (var f in data.Chord?.Fingers ?? new())
            {
                if (f.Fret < 1) continue;
                var idx = Math.Abs(f.String - strings);
                if (idx >= 0 && idx < strings) hasFinger[idx] = true;
            }
            foreach (var b in data.Chord?.Barres ?? new())
            {
                var fromIdx = Math.Abs(b.FromString - strings);
                var toIdx = Math.Abs(b.ToString - strings);
                var lo = Math.Min(fromIdx, toIdx);
                var hi = Math.Max(fromIdx, toIdx);
                for (int i = lo; i <= hi; i++)
                    if (i >= 0 && i < strings) hasFinger[i] = true;
            }

            // Apply explicit open/muted markers from fret=0 / fret=-1 fingers
            foreach (var f in data.Chord?.Fingers ?? new())
            {
                var idx = Math.Abs(f.String - strings);
                if (idx < 0 || idx >= strings) continue;
                if (hasFinger[idx]) continue;
                if (f.Fret == 0) result[idx] = ChordEmptyStringState.Open;
                else if (f.Fret < 0) result[idx] = ChordEmptyStringState.Muted;
            }

            // Strings with no marker and no finger default to open
            for (int i = 0; i < strings; i++)
            {
                if (hasFinger[i]) result[i] = ChordEmptyStringState.NotEmpty;
                else if (result[i] == ChordEmptyStringState.NotEmpty) result[i] = ChordEmptyStringState.Open;
            }

            return result;
        }

        private static void RenderShape(StringBuilder sb, ChordShape shape, double cx, double cy, double r, string fill)
        {
            switch (shape)
            {
                case ChordShape.Square:
                    sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{F(cx - r)}\" y=\"{F(cy - r)}\" width=\"{F(r * 2)}\" height=\"{F(r * 2)}\" fill=\"{Escape(fill)}\"/>");
                    return;
                case ChordShape.Triangle:
                    sb.Append(CultureInfo.InvariantCulture, $"<polygon points=\"{F(cx)},{F(cy - r)} {F(cx + r)},{F(cy + r)} {F(cx - r)},{F(cy + r)}\" fill=\"{Escape(fill)}\"/>");
                    return;
                case ChordShape.Pentagon:
                    sb.Append(BuildPolygon(cx, cy, r, 5, -Math.PI / 2, fill));
                    return;
                case ChordShape.Circle:
                default:
                    sb.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"{Escape(fill)}\"/>");
                    return;
            }
        }

        private static string BuildPolygon(double cx, double cy, double r, int sides, double startAngle, string fill)
        {
            var sb = new StringBuilder();
            sb.Append("<polygon points=\"");
            for (int i = 0; i < sides; i++)
            {
                var a = startAngle + i * (2 * Math.PI / sides);
                var x = cx + r * Math.Cos(a);
                var y = cy + r * Math.Sin(a);
                if (i > 0) sb.Append(' ');
                sb.Append(CultureInfo.InvariantCulture, $"{F(x)},{F(y)}");
            }
            sb.Append(CultureInfo.InvariantCulture, $"\" fill=\"{Escape(fill)}\"/>");
            return sb.ToString();
        }

        private static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private static string Escape(string value) => value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
