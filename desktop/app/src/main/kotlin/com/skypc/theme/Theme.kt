package com.skypc.theme

//渐变描述
class Grad(val enabled: Boolean, val start: Int, val end: Int, val angle: Float)

//12 个语义色（0xAARRGGBB）
class Palette(
    val windowBg: Int,
    val windowHeader: Int,
    val windowStroke: Int,
    val surface: Int,
    val surfaceHover: Int,
    val cardEnabled: Int,
    val cardDisabled: Int,
    val textPrimary: Int,
    val textMuted: Int,
    val accent: Int,
    val accentSoft: Int,
    val strokeSoft: Int,
)

class Entry(
    val id: String,
    val name: String,
    val p: Palette,
    val windowG: Grad,
    val headerG: Grad,
    val surfaceG: Grad,
    val cardG: Grad,
    val strokeG: Grad,
)

private fun mix(a: Int, b: Int, t: Float): Int {
    val tt = t.coerceIn(0f, 1f)
    fun ch(shift: Int): Int {
        val x = (a ushr shift) and 0xFF
        val y = (b ushr shift) and 0xFF
        return (x + (y - x) * tt).toInt().coerceIn(0, 255)
    }
    return (ch(24) shl 24) or (ch(16) shl 16) or (ch(8) shl 8) or ch(0)
}

private fun alpha(c: Int, a: Int): Int = (a.coerceIn(0, 255) shl 24) or (c and 0x00FFFFFF)
private fun opaque(c: Int): Int = c or (0xFF shl 24)
private fun flat(c: Int) = Grad(false, c, c, 90f)

private fun subtleHeader(c: Int): Grad {
    fun adj(ch: Int, d: Float): Int {
        val v = if (d >= 0) ch + ((255 - ch) * d).toInt() else ch + (ch * d).toInt()
        return v.coerceIn(0, 255)
    }
    val a = (c ushr 24) and 0xFF
    val s = (a shl 24) or (adj((c ushr 16) and 0xFF, 0.08f) shl 16) or (adj((c ushr 8) and 0xFF, 0.08f) shl 8) or adj(c and 0xFF, 0.08f)
    val e = (a shl 24) or (adj((c ushr 16) and 0xFF, -0.06f) shl 16) or (adj((c ushr 8) and 0xFF, -0.06f) shl 8) or adj(c and 0xFF, -0.06f)
    return Grad(true, s, e, 90f)
}

private fun theme(
    id: String, name: String,
    bg: Int, header: Int, stroke: Int, surface: Int, hover: Int,
    cardEn: Int, cardDis: Int, text: Int, muted: Int, accent: Int, accentSoft: Int, strokeSoft: Int,
    wg: Grad? = null, hg: Grad? = null, sg: Grad? = null, cg: Grad? = null, stg: Grad? = null,
): Entry {
    val p = Palette(bg, header, stroke, surface, hover, cardEn, cardDis, text, muted, accent, accentSoft, strokeSoft)
    return Entry(id, name, p, wg ?: flat(p.windowBg), hg ?: subtleHeader(p.windowHeader), sg ?: flat(p.surface), cg ?: flat(p.cardEnabled), stg ?: flat(p.windowStroke))
}

private fun pair(id: String, name: String, c1: Int, c2: Int, grad: Boolean): Entry {
    val primary = opaque(c1)
    val secondary = opaque(c2)
    val mid = mix(primary, secondary, 0.5f)
    val p = Palette(
        alpha(mix(0xFF0C0F13.toInt(), primary, 0.055f), 0xF4),
        alpha(mix(0xFF10141A.toInt(), secondary, 0.075f), 0xF4),
        alpha(mid, 0x60),
        mix(0xFF171B21.toInt(), primary, 0.055f),
        mix(0xFF20252D.toInt(), secondary, 0.085f),
        alpha(mix(primary, secondary, 0.28f), 0x50),
        alpha(mix(0xFF2A3038.toInt(), secondary, 0.07f), 0x40),
        mix(0xFFF6F8FB.toInt(), primary, 0.025f),
        alpha(mix(0xFFBBC4D0.toInt(), secondary, 0.10f), 0x88),
        primary,
        alpha(secondary, 0x80),
        alpha(mid, 0x60),
    )
    val g = Entry(
        id, name, p,
        Grad(true, alpha(mix(0xFF0B0E12.toInt(), primary, 0.14f), 0xF4), alpha(mix(0xFF0B0E12.toInt(), secondary, 0.14f), 0xF4), 120f),
        Grad(true, alpha(mix(0xFF10141A.toInt(), primary, 0.19f), 0xF4), alpha(mix(0xFF10141A.toInt(), secondary, 0.19f), 0xF4), 120f),
        Grad(true, mix(0xFF171B21.toInt(), primary, 0.08f), mix(0xFF171B21.toInt(), secondary, 0.08f), 90f),
        Grad(true, primary, secondary, 45f),
        Grad(true, primary, secondary, 45f),
    )
    if (grad) return g
    return Entry(id, name, p, flat(p.windowBg), subtleHeader(p.windowHeader), flat(p.surface), flat(p.cardEnabled), flat(p.windowStroke))
}

object Themes {
    val presets: List<Entry> = listOf(
        theme("classic", "Classic", 0xF012191B.toInt(), 0xF0142226.toInt(), 0x60203030, 0x16202523, 0x28353A3E, 0x5043A7C8, 0x334046, 0xFFFFFFFF.toInt(), 0x88C6D2CD.toInt(), 0xFF5CC8E7.toInt(), 0x805CC8E7.toInt(), 0x60FFFFFF),
        theme("legacy", "Legacy", 0xF0121314.toInt(), 0xF0000205.toInt(), 0xE1121314.toInt(), 0xFF171819.toInt(), 0xFF131315.toInt(), 0x503F464D, 0x33191C20, 0xFFF5F5FF.toInt(), 0x88A5A5A5.toInt(), 0xFF767C94.toInt(), 0x80767C94.toInt(), 0x60373746),
        theme("amber", "Amber", 0xF019120D.toInt(), 0xF0201711.toInt(), 0x60C3652C, 0xFF221711.toInt(), 0xFF2B1E16.toInt(), 0x50E08C3A, 0x40302620, 0xFFFFF7EB.toInt(), 0x88E3C9A7.toInt(), 0xFFE69A43.toInt(), 0x80E69A43.toInt(), 0x60F5D7B0),
        theme("noir", "Noir", 0xF00C0D10.toInt(), 0xF0121317.toInt(), 0x60393F4A, 0xFF14161B.toInt(), 0xFF1C1F26.toInt(), 0x5039424F, 0x40282D35, 0xFFF4F4F4.toInt(), 0x88C7CBD3.toInt(), 0xFFE16B78.toInt(), 0x80E16B78.toInt(), 0x605A6370),
        theme("purple", "Purple", 0xF0120E1A.toInt(), 0xF0181323.toInt(), 0x60433A5A, 0xFF1C1627.toInt(), 0xFF261D35.toInt(), 0x505E4AA0, 0x40312644, 0xFFF5F2FF.toInt(), 0x88CFC6E6.toInt(), 0xFF9B6BFF.toInt(), 0x809B6BFF.toInt(), 0x60705A9C),
        theme("blue", "Blue", 0xF0080F1E.toInt(), 0xF00D1626.toInt(), 0x60405A82, 0xFF101B2B.toInt(), 0xFF162338.toInt(), 0x50406BB3, 0x4023324A, 0xFFF0F5FF.toInt(), 0x88B2C4E6.toInt(), 0xFF2E64CC.toInt(), 0x803A6FD6.toInt(), 0x60465F8E),
        theme("azure", "Azure", 0xF00C1220.toInt(), 0xF0101A2A.toInt(), 0x60324B66, 0xFF121C2C.toInt(), 0xFF18253A.toInt(), 0x503F6BAA, 0x40283348, 0xFFF2F7FF.toInt(), 0x88BBD0EA.toInt(), 0xFF5DA7FF.toInt(), 0x805DA7FF.toInt(), 0x60507CA8),
        theme("nitro", "Nitro", 0xFF121624.toInt(), 0xFF181E33.toInt(), 0x60465580, 0xFF1A1F36.toInt(), 0xFF232A45.toInt(), 0x50A24CFF, 0x40303A52, 0xFFF3F6FF.toInt(), 0x88C7D3F3.toInt(), 0xFF7A5CFF.toInt(), 0x807A5CFF.toInt(), 0x60829AC9,
            Grad(true, 0xFF36205F.toInt(), 0xFF0B7DFF.toInt(), 45f), Grad(true, 0xFF4B2FAF.toInt(), 0xFF24C1FF.toInt(), 45f),
            Grad(true, 0xFF1A1F36.toInt(), 0xFF121728.toInt(), 90f), Grad(true, 0xFF5A37D5.toInt(), 0xFF2AD1FF.toInt(), 45f), Grad(true, 0xFF6C4BFF.toInt(), 0xFF4BD0FF.toInt(), 45f)),
        theme("sunset", "Sunset", 0xFF1A1116.toInt(), 0xFF211319.toInt(), 0x605C2B2B, 0xFF24151C.toInt(), 0xFF2E1A23.toInt(), 0x50FF7A45, 0x40261A1A, 0xFFFFF2E8.toInt(), 0x88E3B9A3.toInt(), 0xFFFF7A45.toInt(), 0x80FF7A45.toInt(), 0x60F5C1A6,
            Grad(true, 0xFF2A1020.toInt(), 0xFFB2431F.toInt(), 45f), Grad(true, 0xFF3B1626.toInt(), 0xFFC25B2A.toInt(), 45f),
            Grad(true, 0xFF24151C.toInt(), 0xFF1E1116.toInt(), 90f), Grad(true, 0xFFFF8A4C.toInt(), 0xFFFF4B7A.toInt(), 45f), Grad(true, 0xFFFFB27A.toInt(), 0xFFFF6A3A.toInt(), 45f)),
        theme("aurora", "Aurora", 0xFF0D1A18.toInt(), 0xFF102320.toInt(), 0x602B5B57, 0xFF132623.toInt(), 0xFF1A312D.toInt(), 0x5034D399, 0x40304742, 0xFFF1FFF9.toInt(), 0x8897E3D0.toInt(), 0xFF2DD9C3.toInt(), 0x802DD9C3.toInt(), 0x6071B5A6,
            Grad(true, 0xFF0B2A3C.toInt(), 0xFF2A8E6B.toInt(), 120f), Grad(true, 0xFF0E3B46.toInt(), 0xFF1FB88B.toInt(), 120f),
            Grad(true, 0xFF132623.toInt(), 0xFF10201D.toInt(), 90f), Grad(true, 0xFF2DD9C3.toInt(), 0xFF5A7CFF.toInt(), 135f), Grad(true, 0xFF78FFE5.toInt(), 0xFF6FB1FF.toInt(), 120f)),
        theme("mint", "Mint", 0xFF0E1917.toInt(), 0xFF112320.toInt(), 0x60255B53, 0xFF132521.toInt(), 0xFF1A2F2B.toInt(), 0x5035CFA8, 0x40304742, 0xFFF1FFF9.toInt(), 0x8897E3D0.toInt(), 0xFF43E0B2.toInt(), 0x8043E0B2.toInt(), 0x6071B5A6,
            Grad(true, 0xFF0B2A24.toInt(), 0xFF1B3E36.toInt(), 120f), Grad(true, 0xFF0F3A32.toInt(), 0xFF1A6E59.toInt(), 120f),
            Grad(true, 0xFF132521.toInt(), 0xFF0F1E1B.toInt(), 90f), Grad(true, 0xFF3DE3B4.toInt(), 0xFFB7F2D9.toInt(), 45f), Grad(true, 0xFF48E6B9.toInt(), 0xFF7EF0D6.toInt(), 120f)),
        theme("peach", "Peach", 0xFF1A1210.toInt(), 0xFF221510.toInt(), 0x605C3A2A, 0xFF241814.toInt(), 0xFF2F211A.toInt(), 0x50F2A269, 0x40382A22, 0xFFFFF7F0.toInt(), 0x88E3C9A7.toInt(), 0xFFFFA46B.toInt(), 0x80FFB680.toInt(), 0x60F5D7B0,
            Grad(true, 0xFF2A1512.toInt(), 0xFF4B241A.toInt(), 45f), Grad(true, 0xFF3B1B14.toInt(), 0xFF6A301F.toInt(), 45f),
            Grad(true, 0xFF241814.toInt(), 0xFF1C1310.toInt(), 90f), Grad(true, 0xFFFFB47A.toInt(), 0xFFFF7FA8.toInt(), 45f), Grad(true, 0xFFFFC28E.toInt(), 0xFFFF8A5A.toInt(), 45f)),
        theme("lilac", "Lilac", 0xFF14131E.toInt(), 0xFF1A1728.toInt(), 0x60433A5A, 0xFF1B172A.toInt(), 0xFF241D36.toInt(), 0x506C5CBE, 0x40312644, 0xFFF5F2FF.toInt(), 0x88CFC6E6.toInt(), 0xFFB79BFF.toInt(), 0x80C8B0FF.toInt(), 0x60705A9C,
            Grad(true, 0xFF241A3B.toInt(), 0xFF1A3A55.toInt(), 120f), Grad(true, 0xFF2F214A.toInt(), 0xFF3D2F6A.toInt(), 120f),
            Grad(true, 0xFF1B172A.toInt(), 0xFF15121F.toInt(), 90f), Grad(true, 0xFFB79BFF.toInt(), 0xFF7AD9FF.toInt(), 45f), Grad(true, 0xFFC5B0FF.toInt(), 0xFF8BB2FF.toInt(), 120f)),
        theme("sand", "Sand", 0xFF181510.toInt(), 0xFF201A13.toInt(), 0x6050432F, 0xFF221B14.toInt(), 0xFF2C231B.toInt(), 0x50E8C98C, 0x40322A21, 0xFFFFF4E8.toInt(), 0x88D8C4A2.toInt(), 0xFFE8C98C.toInt(), 0x80F0D8A8.toInt(), 0x607A6A54,
            Grad(true, 0xFF2A2016.toInt(), 0xFF3A2F22.toInt(), 45f), Grad(true, 0xFF32261B.toInt(), 0xFF4A3A2B.toInt(), 45f),
            Grad(true, 0xFF221B14.toInt(), 0xFF1A1510.toInt(), 90f), Grad(true, 0xFFF2D29B.toInt(), 0xFFE8B67A.toInt(), 45f), Grad(true, 0xFFF5E0B2.toInt(), 0xFFD1A36C.toInt(), 45f)),
        theme("dusk", "Dusk", 0xFF141018.toInt(), 0xFF1B1422.toInt(), 0x60503A4A, 0xFF1D1626.toInt(), 0xFF271B34.toInt(), 0x50FF875A, 0x4030263B, 0xFFF6F1FF.toInt(), 0x88D6C9E6.toInt(), 0xFFFF8A4C.toInt(), 0x80FF9B5A.toInt(), 0x606B5A7A,
            Grad(true, 0xFF2B1630.toInt(), 0xFF6B2A1F.toInt(), 45f), Grad(true, 0xFF3B1B3A.toInt(), 0xFF8A3B24.toInt(), 45f),
            Grad(true, 0xFF1D1626.toInt(), 0xFF141018.toInt(), 90f), Grad(true, 0xFF7C4BFF.toInt(), 0xFFFF9B3D.toInt(), 45f), Grad(true, 0xFFA775FF.toInt(), 0xFFFF7A3A.toInt(), 45f)),
        theme("prism", "Prism", 0xFF0F1220.toInt(), 0xFF141A2B.toInt(), 0x60435173, 0xFF151C2E.toInt(), 0xFF1B243A.toInt(), 0x504B7CFF, 0x4031324A, 0xFFF3F6FF.toInt(), 0x88C2CDEB.toInt(), 0xFF5AC8FA.toInt(), 0x8074B6FF.toInt(), 0x606B7A9E,
            Grad(true, 0xFF1A243A.toInt(), 0xFF2A1E4A.toInt(), 135f), Grad(true, 0xFF223050.toInt(), 0xFF352060.toInt(), 135f),
            Grad(true, 0xFF151C2E.toInt(), 0xFF101522.toInt(), 90f), Grad(true, 0xFF59C4FF.toInt(), 0xFFB35CFF.toInt(), 45f), Grad(true, 0xFF7AD4FF.toInt(), 0xFFE36BFF.toInt(), 45f)),
        theme("cinder", "Cinder", 0xFF120E0F.toInt(), 0xFF181012.toInt(), 0x604A2F32, 0xFF1A1214.toInt(), 0xFF23171A.toInt(), 0x50D24A4A, 0x40281A1C, 0xFFFFF3F3.toInt(), 0x88E3B9B9.toInt(), 0xFFDC4A4A.toInt(), 0x80E06A6A.toInt(), 0x606B4E52,
            Grad(true, 0xFF1A0C0C.toInt(), 0xFF2A1416.toInt(), 45f), Grad(true, 0xFF241012.toInt(), 0xFF3A191C.toInt(), 45f),
            Grad(true, 0xFF1A1214.toInt(), 0xFF120E0F.toInt(), 90f), Grad(true, 0xFFDC4A4A.toInt(), 0xFF401010.toInt(), 45f), Grad(true, 0xFFE26A6A.toInt(), 0xFF7A2A2A.toInt(), 45f)),
        theme("forest", "Forest", 0xFF101611.toInt(), 0xFF141E15.toInt(), 0x6043573D, 0xFF161F18.toInt(), 0xFF1D2B20.toInt(), 0x5071C36C, 0x40263225, 0xFFF3FFF1.toInt(), 0x88C5E3C1.toInt(), 0xFF7DD36B.toInt(), 0x807DD36B.toInt(), 0x60607B58,
            Grad(true, 0xFF1A241C.toInt(), 0xFF2B3A24.toInt(), 120f), Grad(true, 0xFF1F2B21.toInt(), 0xFF2F4B2E.toInt(), 120f),
            Grad(true, 0xFF161F18.toInt(), 0xFF101611.toInt(), 90f), Grad(true, 0xFF7DD36B.toInt(), 0xFFC6E886.toInt(), 45f), Grad(true, 0xFF9BE07C.toInt(), 0xFF6BC26A.toInt(), 45f)),
        pair("spearmint", "Spearmint", 0xFF61C2A2.toInt(), 0xFF41826C.toInt(), false),
        pair("jade_green", "Jade Green", 0xFF00A86B.toInt(), 0xFF006942.toInt(), false),
        pair("green_spirit", "Green Spirit", 0xFF00873E.toInt(), 0xFF9FE2BF.toInt(), true),
        pair("rosy_pink", "Rosy Pink", 0xFFFF66CC.toInt(), 0xFFBF4D99.toInt(), false),
        pair("magenta", "Magenta", 0xFFD53F77.toInt(), 0xFF9D446E.toInt(), false),
        pair("hot_pink", "Hot Pink", 0xFFE75480.toInt(), 0xFFAC4FC6.toInt(), true),
        pair("lavender", "Lavender", 0xFFDBA6F7.toInt(), 0xFF9873AC.toInt(), false),
        pair("amethyst", "Amethyst", 0xFF9063CD.toInt(), 0xFF62438C.toInt(), false),
        pair("purple_fire", "Purple Fire", 0xFF68478D.toInt(), 0xFFB1A2CA.toInt(), true),
        pair("sunset_pink", "Sunset Pink", 0xFFFF9114.toInt(), 0xFFF569E7.toInt(), true),
        pair("blaze_orange", "Blaze Orange", 0xFFFFA94D.toInt(), 0xFFFF8200.toInt(), false),
        pair("pink_blood", "Pink Blood", 0xFFE40046.toInt(), 0xFFFFA6C9.toInt(), true),
        pair("pastel", "Pastel", 0xFFFF6D6A.toInt(), 0xFFBF5250.toInt(), false),
        pair("neon_red", "Neon Red", 0xFFD22730.toInt(), 0xFFB8192A.toInt(), false),
        pair("red_coffee", "Red Coffee", 0xFFE1223B.toInt(), 0xFF000000.toInt(), false),
        pair("deep_ocean", "Deep Ocean", 0xFF3C5291.toInt(), 0xFF001440.toInt(), true),
        pair("chambray_blue", "Chambray Blue", 0xFF212EB6.toInt(), 0xFF3C5291.toInt(), false),
        pair("mint_blue", "Mint Blue", 0xFF429E9D.toInt(), 0xFF285E5D.toInt(), false),
        pair("pacific_blue", "Pacific Blue", 0xFF05A9C7.toInt(), 0xFF047387.toInt(), false),
        pair("tropical_ice", "Tropical Ice", 0xFF66FFD1.toInt(), 0xFF0695FF.toInt(), true),
    )

    private val byId = presets.associateBy { it.id }
    var current: Entry = presets[0]
        private set

    fun find(id: String?): Entry = byId[id ?: ""] ?: presets[0]
    fun select(id: String?) { current = find(id) }
}
