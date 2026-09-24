using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace SkyPC_AutoMusic.Theme
{
    //预设主题集合（移植自 combatant-client-26.2 的 Themes）
    public static class Themes
    {
        public const string DefaultId = "classic";

        private static readonly List<ThemeEntry> presets = new List<ThemeEntry>();
        private static readonly Dictionary<string, ThemeEntry> byId = new Dictionary<string, ThemeEntry>();

        private static string currentId = DefaultId;

        public static event Action CurrentChanged;

        public static IReadOnlyList<ThemeEntry> Presets
        {
            get { return presets; }
        }

        public static string CurrentId
        {
            get { return currentId; }
        }

        public static ThemeEntry Current
        {
            get
            {
                ThemeEntry entry;
                return byId.TryGetValue(currentId, out entry) ? entry : presets[0];
            }
        }

        static Themes()
        {
            //经典
            Add(Flat("classic", "Classic",
                0xF012191B, 0xF0142226, 0x60203030, 0x16202523, 0x28353A3E, 0x5043A7C8, 0x334046,
                0xFFFFFFFF, 0x88C6D2CD, 0xFF5CC8E7, 0x805CC8E7, 0x60FFFFFF));
            Add(Flat("legacy", "Legacy",
                0xF0121314, 0xF0000205, 0xE1121314, 0xFF171819, 0xFF131315, 0x503F464D, 0x33191C20,
                0xFFF5F5FF, 0x88A5A5A5, 0xFF767C94, 0x80767C94, 0x60373746));
            Add(Flat("amber", "Amber",
                0xF019120D, 0xF0201711, 0x60C3652C, 0xFF221711, 0xFF2B1E16, 0x50E08C3A, 0x40302620,
                0xFFFFF7EB, 0x88E3C9A7, 0xFFE69A43, 0x80E69A43, 0x60F5D7B0));
            Add(Flat("noir", "Noir",
                0xF00C0D10, 0xF0121317, 0x60393F4A, 0xFF14161B, 0xFF1C1F26, 0x5039424F, 0x40282D35,
                0xFFF4F4F4, 0x88C7CBD3, 0xFFE16B78, 0x80E16B78, 0x605A6370));
            Add(Flat("purple", "Purple",
                0xF0120E1A, 0xF0181323, 0x60433A5A, 0xFF1C1627, 0xFF261D35, 0x505E4AA0, 0x40312644,
                0xFFF5F2FF, 0x88CFC6E6, 0xFF9B6BFF, 0x809B6BFF, 0x60705A9C));
            Add(Flat("blue", "Blue",
                0xF0080F1E, 0xF00D1626, 0x60405A82, 0xFF101B2B, 0xFF162338, 0x50406BB3, 0x4023324A,
                0xFFF0F5FF, 0x88B2C4E6, 0xFF2E64CC, 0x803A6FD6, 0x60465F8E));
            Add(Flat("azure", "Azure",
                0xF00C1220, 0xF0101A2A, 0x60324B66, 0xFF121C2C, 0xFF18253A, 0x503F6BAA, 0x40283348,
                0xFFF2F7FF, 0x88BBD0EA, 0xFF5DA7FF, 0x805DA7FF, 0x60507CA8));

            //带渐变的
            Add(new ThemeEntry("nitro", "Nitro",
                Palette(0xFF121624, 0xFF181E33, 0x60465580, 0xFF1A1F36, 0xFF232A45, 0x50A24CFF, 0x40303A52,
                        0xFFF3F6FF, 0x88C7D3F3, 0xFF7A5CFF, 0x807A5CFF, 0x60829AC9),
                G(true, 0xFF36205F, 0xFF0B7DFF, 45), G(true, 0xFF4B2FAF, 0xFF24C1FF, 45),
                G(true, 0xFF1A1F36, 0xFF121728, 90), G(true, 0xFF5A37D5, 0xFF2AD1FF, 45), G(true, 0xFF6C4BFF, 0xFF4BD0FF, 45)));
            Add(new ThemeEntry("sunset", "Sunset",
                Palette(0xFF1A1116, 0xFF211319, 0x605C2B2B, 0xFF24151C, 0xFF2E1A23, 0x50FF7A45, 0x40261A1A,
                        0xFFFFF2E8, 0x88E3B9A3, 0xFFFF7A45, 0x80FF7A45, 0x60F5C1A6),
                G(true, 0xFF2A1020, 0xFFB2431F, 45), G(true, 0xFF3B1626, 0xFFC25B2A, 45),
                G(true, 0xFF24151C, 0xFF1E1116, 90), G(true, 0xFFFF8A4C, 0xFFFF4B7A, 45), G(true, 0xFFFFB27A, 0xFFFF6A3A, 45)));
            Add(new ThemeEntry("aurora", "Aurora",
                Palette(0xFF0D1A18, 0xFF102320, 0x602B5B57, 0xFF132623, 0xFF1A312D, 0x5034D399, 0x40304742,
                        0xFFF1FFF9, 0x8897E3D0, 0xFF2DD9C3, 0x802DD9C3, 0x6071B5A6),
                G(true, 0xFF0B2A3C, 0xFF2A8E6B, 120), G(true, 0xFF0E3B46, 0xFF1FB88B, 120),
                G(true, 0xFF132623, 0xFF10201D, 90), G(true, 0xFF2DD9C3, 0xFF5A7CFF, 135), G(true, 0xFF78FFE5, 0xFF6FB1FF, 120)));
            Add(new ThemeEntry("mint", "Mint",
                Palette(0xFF0E1917, 0xFF112320, 0x60255B53, 0xFF132521, 0xFF1A2F2B, 0x5035CFA8, 0x40304742,
                        0xFFF1FFF9, 0x8897E3D0, 0xFF43E0B2, 0x8043E0B2, 0x6071B5A6),
                G(true, 0xFF0B2A24, 0xFF1B3E36, 120), G(true, 0xFF0F3A32, 0xFF1A6E59, 120),
                G(true, 0xFF132521, 0xFF0F1E1B, 90), G(true, 0xFF3DE3B4, 0xFFB7F2D9, 45), G(true, 0xFF48E6B9, 0xFF7EF0D6, 120)));
            Add(new ThemeEntry("peach", "Peach",
                Palette(0xFF1A1210, 0xFF221510, 0x605C3A2A, 0xFF241814, 0xFF2F211A, 0x50F2A269, 0x40382A22,
                        0xFFFFF7F0, 0x88E3C9A7, 0xFFFFA46B, 0x80FFB680, 0x60F5D7B0),
                G(true, 0xFF2A1512, 0xFF4B241A, 45), G(true, 0xFF3B1B14, 0xFF6A301F, 45),
                G(true, 0xFF241814, 0xFF1C1310, 90), G(true, 0xFFFFB47A, 0xFFFF7FA8, 45), G(true, 0xFFFFC28E, 0xFFFF8A5A, 45)));
            Add(new ThemeEntry("lilac", "Lilac",
                Palette(0xFF14131E, 0xFF1A1728, 0x60433A5A, 0xFF1B172A, 0xFF241D36, 0x506C5CBE, 0x40312644,
                        0xFFF5F2FF, 0x88CFC6E6, 0xFFB79BFF, 0x80C8B0FF, 0x60705A9C),
                G(true, 0xFF241A3B, 0xFF1A3A55, 120), G(true, 0xFF2F214A, 0xFF3D2F6A, 120),
                G(true, 0xFF1B172A, 0xFF15121F, 90), G(true, 0xFFB79BFF, 0xFF7AD9FF, 45), G(true, 0xFFC5B0FF, 0xFF8BB2FF, 120)));
            Add(new ThemeEntry("sand", "Sand",
                Palette(0xFF181510, 0xFF201A13, 0x6050432F, 0xFF221B14, 0xFF2C231B, 0x50E8C98C, 0x40322A21,
                        0xFFFFF4E8, 0x88D8C4A2, 0xFFE8C98C, 0x80F0D8A8, 0x607A6A54),
                G(true, 0xFF2A2016, 0xFF3A2F22, 45), G(true, 0xFF32261B, 0xFF4A3A2B, 45),
                G(true, 0xFF221B14, 0xFF1A1510, 90), G(true, 0xFFF2D29B, 0xFFE8B67A, 45), G(true, 0xFFF5E0B2, 0xFFD1A36C, 45)));
            Add(new ThemeEntry("dusk", "Dusk",
                Palette(0xFF141018, 0xFF1B1422, 0x60503A4A, 0xFF1D1626, 0xFF271B34, 0x50FF875A, 0x4030263B,
                        0xFFF6F1FF, 0x88D6C9E6, 0xFFFF8A4C, 0x80FF9B5A, 0x606B5A7A),
                G(true, 0xFF2B1630, 0xFF6B2A1F, 45), G(true, 0xFF3B1B3A, 0xFF8A3B24, 45),
                G(true, 0xFF1D1626, 0xFF141018, 90), G(true, 0xFF7C4BFF, 0xFFFF9B3D, 45), G(true, 0xFFA775FF, 0xFFFF7A3A, 45)));
            Add(new ThemeEntry("prism", "Prism",
                Palette(0xFF0F1220, 0xFF141A2B, 0x60435173, 0xFF151C2E, 0xFF1B243A, 0x504B7CFF, 0x4031324A,
                        0xFFF3F6FF, 0x88C2CDEB, 0xFF5AC8FA, 0x8074B6FF, 0x606B7A9E),
                G(true, 0xFF1A243A, 0xFF2A1E4A, 135), G(true, 0xFF223050, 0xFF352060, 135),
                G(true, 0xFF151C2E, 0xFF101522, 90), G(true, 0xFF59C4FF, 0xFFB35CFF, 45), G(true, 0xFF7AD4FF, 0xFFE36BFF, 45)));
            Add(new ThemeEntry("cinder", "Cinder",
                Palette(0xFF120E0F, 0xFF181012, 0x604A2F32, 0xFF1A1214, 0xFF23171A, 0x50D24A4A, 0x40281A1C,
                        0xFFFFF3F3, 0x88E3B9B9, 0xFFDC4A4A, 0x80E06A6A, 0x606B4E52),
                G(true, 0xFF1A0C0C, 0xFF2A1416, 45), G(true, 0xFF241012, 0xFF3A191C, 45),
                G(true, 0xFF1A1214, 0xFF120E0F, 90), G(true, 0xFFDC4A4A, 0xFF401010, 45), G(true, 0xFFE26A6A, 0xFF7A2A2A, 45)));
            Add(new ThemeEntry("forest", "Forest",
                Palette(0xFF101611, 0xFF141E15, 0x6043573D, 0xFF161F18, 0xFF1D2B20, 0x5071C36C, 0x40263225,
                        0xFFF3FFF1, 0x88C5E3C1, 0xFF7DD36B, 0x807DD36B, 0x60607B58),
                G(true, 0xFF1A241C, 0xFF2B3A24, 120), G(true, 0xFF1F2B21, 0xFF2F4B2E, 120),
                G(true, 0xFF161F18, 0xFF101611, 90), G(true, 0xFF7DD36B, 0xFFC6E886, 45), G(true, 0xFF9BE07C, 0xFF6BC26A, 45)));

            //配对调色板（两色生成整套语义色）
            AddPair("spearmint", "Spearmint", 0xFF61C2A2, 0xFF41826C, false);
            AddPair("jade_green", "Jade Green", 0xFF00A86B, 0xFF006942, false);
            AddPair("green_spirit", "Green Spirit", 0xFF00873E, 0xFF9FE2BF, true);
            AddPair("rosy_pink", "Rosy Pink", 0xFFFF66CC, 0xFFBF4D99, false);
            AddPair("magenta", "Magenta", 0xFFD53F77, 0xFF9D446E, false);
            AddPair("hot_pink", "Hot Pink", 0xFFE75480, 0xFFAC4FC6, true);
            AddPair("lavender", "Lavender", 0xFFDBA6F7, 0xFF9873AC, false);
            AddPair("amethyst", "Amethyst", 0xFF9063CD, 0xFF62438C, false);
            AddPair("purple_fire", "Purple Fire", 0xFF68478D, 0xFFB1A2CA, true);
            AddPair("sunset_pink", "Sunset Pink", 0xFFFF9114, 0xFFF569E7, true);
            AddPair("blaze_orange", "Blaze Orange", 0xFFFFA94D, 0xFFFF8200, false);
            AddPair("pink_blood", "Pink Blood", 0xFFE40046, 0xFFFFA6C9, true);
            AddPair("pastel", "Pastel", 0xFFFF6D6A, 0xFFBF5250, false);
            AddPair("neon_red", "Neon Red", 0xFFD22730, 0xFFB8192A, false);
            AddPair("red_coffee", "Red Coffee", 0xFFE1223B, 0xFF000000, false);
            AddPair("deep_ocean", "Deep Ocean", 0xFF3C5291, 0xFF001440, true);
            AddPair("chambray_blue", "Chambray Blue", 0xFF212EB6, 0xFF3C5291, false);
            AddPair("mint_blue", "Mint Blue", 0xFF429E9D, 0xFF285E5D, false);
            AddPair("pacific_blue", "Pacific Blue", 0xFF05A9C7, 0xFF047387, false);
            AddPair("tropical_ice", "Tropical Ice", 0xFF66FFD1, 0xFF0695FF, true);
        }

        public static bool SetCurrent(string id)
        {
            if (String.IsNullOrWhiteSpace(id))
                id = DefaultId;
            id = id.ToLowerInvariant();
            if (!byId.ContainsKey(id))
                id = DefaultId;
            if (id == currentId)
                return false;
            currentId = id;
            if (CurrentChanged != null)
                CurrentChanged();
            return true;
        }

        #region 构建辅助

        private static void Add(ThemeEntry entry)
        {
            presets.Add(entry);
            byId[entry.Id] = entry;
        }

        private static Color C(uint argb)
        {
            return ThemePalette.C(argb);
        }

        private static GradientSpec G(bool enabled, uint start, uint end, double angle)
        {
            return new GradientSpec(enabled, C(start), C(end), angle);
        }

        private static ThemePalette Palette(uint bg, uint header, uint stroke, uint surface, uint surfaceHover,
            uint cardEnabled, uint cardDisabled, uint textPrimary, uint textMuted, uint accent, uint accentSoft, uint strokeSoft)
        {
            return new ThemePalette(bg, header, stroke, surface, surfaceHover, cardEnabled, cardDisabled,
                textPrimary, textMuted, accent, accentSoft, strokeSoft);
        }

        private static ThemeEntry Flat(string id, string name, uint bg, uint header, uint stroke, uint surface,
            uint surfaceHover, uint cardEnabled, uint cardDisabled, uint textPrimary, uint textMuted,
            uint accent, uint accentSoft, uint strokeSoft)
        {
            ThemePalette palette = Palette(bg, header, stroke, surface, surfaceHover, cardEnabled, cardDisabled,
                textPrimary, textMuted, accent, accentSoft, strokeSoft);
            return new ThemeEntry(id, name, palette,
                GradientSpec.Flat(palette.WindowBg),
                SubtleHeader(palette.WindowHeader),
                GradientSpec.Flat(palette.Surface),
                GradientSpec.Flat(palette.CardEnabled),
                GradientSpec.Flat(palette.WindowStroke));
        }

        private static GradientSpec SubtleHeader(Color color)
        {
            return new GradientSpec(true, Adjust(color, 0.08f), Adjust(color, -0.06f), 90);
        }

        private static void AddPair(string id, string name, uint c1, uint c2, bool gradient)
        {
            Color primary = Opaque(C(c1));
            Color secondary = Opaque(C(c2));
            Color mid = Mix(primary, secondary, 0.5f);

            ThemePalette palette = new ThemePalette(
                WithAlpha(Mix(C(0xFF0C0F13), primary, 0.055f), 0xF4),
                WithAlpha(Mix(C(0xFF10141A), secondary, 0.075f), 0xF4),
                WithAlpha(mid, 0x60),
                Mix(C(0xFF171B21), primary, 0.055f),
                Mix(C(0xFF20252D), secondary, 0.085f),
                WithAlpha(Mix(primary, secondary, 0.28f), 0x50),
                WithAlpha(Mix(C(0xFF2A3038), secondary, 0.07f), 0x40),
                Mix(C(0xFFF6F8FB), primary, 0.025f),
                WithAlpha(Mix(C(0xFFBBC4D0), secondary, 0.10f), 0x88),
                primary,
                WithAlpha(secondary, 0x80),
                WithAlpha(mid, 0x60));

            if (!gradient)
            {
                Add(new ThemeEntry(id, name, palette,
                    GradientSpec.Flat(palette.WindowBg),
                    SubtleHeader(palette.WindowHeader),
                    GradientSpec.Flat(palette.Surface),
                    GradientSpec.Flat(palette.CardEnabled),
                    GradientSpec.Flat(palette.WindowStroke)));
                return;
            }

            uint p = ToUint(primary);
            uint s = ToUint(secondary);
            Add(new ThemeEntry(id, name, palette,
                G(true, MixArgba(0xFF0B0E12, p, 0.14f, 0xF4), MixArgba(0xFF0B0E12, s, 0.14f, 0xF4), 120),
                G(true, MixArgba(0xFF10141A, p, 0.19f, 0xF4), MixArgba(0xFF10141A, s, 0.19f, 0xF4), 120),
                G(true, MixArgba(0xFF171B21, p, 0.08f, 0xFF), MixArgba(0xFF171B21, s, 0.08f, 0xFF), 90),
                G(true, p, s, 45),
                G(true, p, s, 45)));
        }

        private static uint ToUint(Color c)
        {
            return (uint)((c.A << 24) | (c.R << 16) | (c.G << 8) | c.B);
        }

        private static uint MixArgba(uint baseRgb, uint overRgb, float t, uint alpha)
        {
            Color mixed = Mix(C(baseRgb | 0xFF000000), C(overRgb), t);
            return (alpha << 24) | (uint)((mixed.R << 16) | (mixed.G << 8) | mixed.B);
        }

        private static Color Opaque(Color c)
        {
            return Color.FromArgb(0xFF, c.R, c.G, c.B);
        }

        private static Color WithAlpha(Color c, uint alpha)
        {
            return Color.FromArgb((byte)(alpha & 0xFF), c.R, c.G, c.B);
        }

        private static Color Mix(Color a, Color b, float t)
        {
            t = t < 0 ? 0 : (t > 1 ? 1 : t);
            return Color.FromArgb(
                (byte)Math.Round(a.A + (b.A - a.A) * t),
                (byte)Math.Round(a.R + (b.R - a.R) * t),
                (byte)Math.Round(a.G + (b.G - a.G) * t),
                (byte)Math.Round(a.B + (b.B - a.B) * t));
        }

        private static Color Adjust(Color color, float delta)
        {
            int r = color.R, g = color.G, b = color.B;
            r = Clamp(r + (int)Math.Round((delta >= 0 ? (255 - r) : r) * delta));
            g = Clamp(g + (int)Math.Round((delta >= 0 ? (255 - g) : g) * delta));
            b = Clamp(b + (int)Math.Round((delta >= 0 ? (255 - b) : b) * delta));
            return Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
        }

        private static int Clamp(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
        }

        #endregion
    }
}
