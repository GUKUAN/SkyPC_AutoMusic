using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SkyPC_AutoMusic.Theme
{
    //渐变描述（移植自 combatant-client 的 GradientSpec）
    public class GradientSpec
    {
        public bool Enabled;
        public Color Start;
        public Color End;
        public double Angle;

        public GradientSpec(bool enabled, Color start, Color end, double angle)
        {
            Enabled = enabled;
            Start = start;
            End = end;
            Angle = angle;
        }

        public static GradientSpec Flat(Color color)
        {
            return new GradientSpec(false, color, color, 90);
        }
    }

    //12 个语义色（移植自 combatant-client 的 Theme record）
    public class ThemePalette
    {
        public Color WindowBg;
        public Color WindowHeader;
        public Color WindowStroke;
        public Color Surface;
        public Color SurfaceHover;
        public Color CardEnabled;
        public Color CardDisabled;
        public Color TextPrimary;
        public Color TextMuted;
        public Color Accent;
        public Color AccentSoft;
        public Color StrokeSoft;

        public ThemePalette(uint bg, uint header, uint stroke, uint surface, uint surfaceHover,
            uint cardEnabled, uint cardDisabled, uint textPrimary, uint textMuted,
            uint accent, uint accentSoft, uint strokeSoft)
        {
            WindowBg = C(bg);
            WindowHeader = C(header);
            WindowStroke = C(stroke);
            Surface = C(surface);
            SurfaceHover = C(surfaceHover);
            CardEnabled = C(cardEnabled);
            CardDisabled = C(cardDisabled);
            TextPrimary = C(textPrimary);
            TextMuted = C(textMuted);
            Accent = C(accent);
            AccentSoft = C(accentSoft);
            StrokeSoft = C(strokeSoft);
        }

        public ThemePalette(Color bg, Color header, Color stroke, Color surface, Color surfaceHover,
            Color cardEnabled, Color cardDisabled, Color textPrimary, Color textMuted,
            Color accent, Color accentSoft, Color strokeSoft)
        {
            WindowBg = bg;
            WindowHeader = header;
            WindowStroke = stroke;
            Surface = surface;
            SurfaceHover = surfaceHover;
            CardEnabled = cardEnabled;
            CardDisabled = cardDisabled;
            TextPrimary = textPrimary;
            TextMuted = textMuted;
            Accent = accent;
            AccentSoft = accentSoft;
            StrokeSoft = strokeSoft;
        }

        //0xAARRGGBB -> Color
        public static Color C(uint argb)
        {
            return Color.FromArgb(
                (byte)((argb >> 24) & 0xFF),
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF));
        }
    }

    //一套主题（调色板 + 各部分渐变）
    public class ThemeEntry
    {
        public string Id;
        public string Name;
        public ThemePalette Palette;
        public GradientSpec WindowGradient;
        public GradientSpec HeaderGradient;
        public GradientSpec SurfaceGradient;
        public GradientSpec CardGradient;
        public GradientSpec StrokeGradient;

        public ThemeEntry(string id, string name, ThemePalette palette,
            GradientSpec window, GradientSpec header, GradientSpec surface, GradientSpec card, GradientSpec stroke)
        {
            Id = id;
            Name = name;
            Palette = palette;
            WindowGradient = window;
            HeaderGradient = header;
            SurfaceGradient = surface;
            CardGradient = card;
            StrokeGradient = stroke;
        }

        //主题选择器里用的预览色块（accent -> accentSoft 渐变）
        public System.Windows.Media.Brush PreviewBrush
        {
            get
            {
                LinearGradientBrush brush = new LinearGradientBrush();
                brush.StartPoint = new Point(0, 0);
                brush.EndPoint = new Point(1, 1);
                brush.GradientStops.Add(new GradientStop(Palette.Accent, 0));
                brush.GradientStops.Add(new GradientStop(Palette.AccentSoft, 1));
                brush.Freeze();
                return brush;
            }
        }
    }
}
