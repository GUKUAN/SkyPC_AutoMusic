using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SkyPC_AutoMusic.Theme
{
    //把当前主题铺到应用资源上（自用键 + 覆盖 MaterialDesign 键）
    //资源里的画刷会被 WPF 冻结，所以换肤时重建整套画刷，并用窗口淡入软化切换
    public static class ThemeService
    {
        public const string WindowBrush = "GlassWindowBrush";
        public const string HeaderBrush = "GlassHeaderBrush";
        public const string PanelBrush = "GlassPanelBrush";
        public const string CardBrush = "GlassCardBrush";
        public const string StrokeBrush = "GlassStrokeBrush";
        public const string SurfaceHoverBrush = "GlassSurfaceHoverBrush";
        public const string CardDisabledBrush = "GlassCardDisabledBrush";
        public const string StrokeSoftBrush = "GlassStrokeSoftBrush";
        public const string TextPrimaryBrush = "TextPrimaryBrush";
        public const string TextMutedBrush = "TextMutedBrush";
        public const string AccentBrush = "AccentBrush";
        public const string AccentSoftBrush = "AccentSoftBrush";
        public const string AccentForegroundBrush = "AccentForegroundBrush";

        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
                return;
            initialized = true;
            Apply(Themes.Current, false);
        }

        public static void ApplyCurrent(bool animate)
        {
            Apply(Themes.Current, animate);
        }

        public static void Apply(ThemeEntry entry, bool animate)
        {
            if (entry == null || Application.Current == null)
                return;

            ResourceDictionary res = Application.Current.Resources;
            ThemePalette p = entry.Palette;

            //玻璃渐变
            res[WindowBrush] = Gradient(entry.WindowGradient);
            res[HeaderBrush] = Gradient(entry.HeaderGradient);
            res[PanelBrush] = Gradient(entry.SurfaceGradient);
            res[CardBrush] = Gradient(entry.CardGradient);
            res[StrokeBrush] = Gradient(entry.StrokeGradient);

            //语义实心色
            res[SurfaceHoverBrush] = Solid(p.SurfaceHover);
            res[CardDisabledBrush] = Solid(p.CardDisabled);
            res[StrokeSoftBrush] = Solid(p.StrokeSoft);
            res[TextPrimaryBrush] = Solid(p.TextPrimary);
            res[TextMutedBrush] = Solid(p.TextMuted);
            res[AccentBrush] = Solid(p.Accent);
            res[AccentSoftBrush] = Solid(p.AccentSoft);
            res[AccentForegroundBrush] = Solid(ContrastColor(p.Accent));

            //覆盖 MaterialDesign 的键
            res["MaterialDesignPaper"] = Solid(WithExtraAlpha(p.Surface, 0.85));
            res["MaterialDesignBody"] = Solid(p.TextPrimary);
            res["MaterialDesignBodyLight"] = Solid(p.TextMuted);
            res["MaterialDesignBodyMedium"] = Solid(p.TextMuted);
            res["MaterialDesignDivider"] = Solid(p.StrokeSoft);
            res["MaterialDesignCardBackground"] = Solid(WithExtraAlpha(p.Surface, 0.9));
            res["MaterialDesignTextFieldBoxBackground"] = Solid(WithExtraAlpha(p.SurfaceHover, 0.9));
            res["MaterialDesignTextFieldBoxHoverBackground"] = Solid(p.SurfaceHover);
            res["MaterialDesignTextFieldBoxDisabledBackground"] = Solid(p.CardDisabled);
            res["PrimaryHueMidBrush"] = Solid(p.Accent);
            res["PrimaryHueMidForegroundBrush"] = Solid(ContrastColor(p.Accent));
            res["PrimaryHueLightBrush"] = Solid(p.AccentSoft);
            res["PrimaryHueLightForegroundBrush"] = Solid(ContrastColor(p.Accent));
            res["PrimaryHueDarkBrush"] = Solid(p.Accent);
            res["PrimaryHueDarkForegroundBrush"] = Solid(ContrastColor(p.Accent));
            res["MaterialDesignSelection"] = Solid(p.AccentSoft);
            res["MaterialDesignSnackbarBackground"] = Solid(WithExtraAlpha(p.SurfaceHover, 0.96));
            res["MaterialDesignSnackbarForeground"] = Solid(p.TextPrimary);
            res["MaterialDesignSnackbarActionButtonForeground"] = Solid(p.Accent);
            res["MaterialDesignToolTipBackground"] = Solid(WithExtraAlpha(p.SurfaceHover, 0.96));
            res["MaterialDesignToolTipForeground"] = Solid(p.TextPrimary);
            res["MaterialDesignFlatButtonRipple"] = Solid(p.AccentSoft);
            res["MaterialDesignIconForeground"] = Solid(p.TextPrimary);

            if (animate && Application.Current.MainWindow != null)
            {
                Window window = Application.Current.MainWindow;
                DoubleAnimation fade = new DoubleAnimation(1, TimeSpan.FromMilliseconds(240))
                {
                    From = 0.55,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                window.BeginAnimation(UIElement.OpacityProperty, fade);
            }
        }

        private static SolidColorBrush Solid(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static LinearGradientBrush Gradient(GradientSpec spec)
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.GradientStops.Add(new GradientStop(spec.Start, 0));
            brush.GradientStops.Add(new GradientStop(spec.Enabled ? spec.End : spec.Start, 1));

            double rad = spec.Angle * Math.PI / 180.0;
            double dx = Math.Cos(rad);
            double dy = Math.Sin(rad);
            brush.StartPoint = new Point(0.5 - dx / 2, 0.5 - dy / 2);
            brush.EndPoint = new Point(0.5 + dx / 2, 0.5 + dy / 2);
            brush.Freeze();
            return brush;
        }

        private static Color ContrastColor(Color background)
        {
            double luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
            return luminance > 0.6 ? Color.FromRgb(0x12, 0x15, 0x18) : Color.FromRgb(0xF6, 0xF8, 0xFB);
        }

        private static Color WithExtraAlpha(Color color, double factor)
        {
            byte a = (byte)Math.Min(255, Math.Round(color.A * factor));
            return Color.FromArgb(a, color.R, color.G, color.B);
        }
    }
}
