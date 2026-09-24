package com.skypc

import com.skypc.core.Config
import com.skypc.core.Market
import com.skypc.core.MarketItem
import com.skypc.core.Player
import com.skypc.core.SheetParser
import com.skypc.core.Song
import com.skypc.theme.Grad
import com.skypc.theme.Themes
import com.skypc.ui.Icons
import com.skypc.ui.Input
import com.skypc.ui.Painter
import org.lwjgl.glfw.GLFW.*
import org.lwjgl.glfw.GLFWCursorPosCallback
import org.lwjgl.glfw.GLFWKeyCallback
import org.lwjgl.glfw.GLFWMouseButtonCallback
import org.lwjgl.glfw.GLFWScrollCallback
import org.lwjgl.opengl.GL
import org.lwjgl.opengl.GL11.*
import org.lwjgl.nanovg.NanoVG.*
import org.lwjgl.nanovg.NanoVGGL3
import org.lwjgl.system.MemoryUtil.NULL
import java.io.File
import javax.swing.SwingUtilities

private const val AL_CENTER = NVG_ALIGN_CENTER or NVG_ALIGN_MIDDLE
private const val AL_RIGHT = NVG_ALIGN_RIGHT or NVG_ALIGN_MIDDLE
private const val AL_LEFT = NVG_ALIGN_LEFT or NVG_ALIGN_MIDDLE

class AppState {
    val player = Player()
    val songs = java.util.Collections.synchronizedList(ArrayList<Song>())

    @Volatile var marketItems: List<MarketItem> = emptyList()
    @Volatile var marketLoading = false

    var tab = 0
    var clickGui = false
    var selectedSong = 0
    var mode = 0 // 0 停下 1 单曲 2 列表
    var seekPending = -1f

    fun combineKeys(): Boolean = Config.current.combineKeys
    fun headline(): String = player.song?.name ?: "SkyPC AutoMusic"

    fun refreshMarket() {
        if (marketLoading) return
        marketLoading = true
        Thread {
            try {
                val items = Market.fetch(Config.current.token)
                if (items.isNotEmpty()) {
                    Config.saveMarketCache(items)
                    marketItems = items
                }
            } finally {
                marketLoading = false
            }
        }.also { it.isDaemon = true }.start()
    }

    fun importFiles(files: List<File>) {
        for (f in files) {
            val song = SheetParser.parseFile(f.absolutePath) ?: continue
            val existing = songs.any { it.sourcePath == song.sourcePath }
            if (!existing) songs.add(song)
        }
        if (player.song == null && songs.isNotEmpty()) {
            selectedSong = 0
            player.load(songs[0])
        }
        persistSheets()
    }

    fun persistSheets() {
        Config.current.sheetPaths = songs.map { it.sourcePath }.toMutableList()
        Config.save()
    }

    fun chooseFiles() {
        SwingUtilities.invokeLater {
            val dlg = java.awt.FileDialog(null as java.awt.Frame?, "导入乐谱", java.awt.FileDialog.LOAD)
            dlg.isMultipleMode = true
            dlg.setFilenameFilter { _, name -> name.lowercase().endsWith(".txt") }
            dlg.isVisible = true
            val files = dlg.files
            if (files != null && files.isNotEmpty()) {
                Thread { importFiles(files.toList()) }.also { it.isDaemon = true }.start()
            }
        }
    }

    fun downloadAndImport(item: MarketItem) {
        Thread {
            val f = Market.download(item, Config.current.token) ?: return@Thread
            importFiles(listOf(f))
        }.also { it.isDaemon = true }.start()
    }
}

fun main() {
    Config.load()
    Themes.select(Config.current.themeId)

    if (!glfwInit()) return
    glfwDefaultWindowHints()
    glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3)
    glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3)
    glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE)
    glfwWindowHint(GLFW_DECORATED, GLFW_FALSE)
    glfwWindowHint(GLFW_FLOATING, GLFW_TRUE)
    glfwWindowHint(GLFW_SAMPLES, 4)
    glfwWindowHint(GLFW_VISIBLE, GLFW_FALSE)

    val window = glfwCreateWindow(940, 640, "SkyPC AutoMusic", NULL, NULL)
    if (window == NULL) {
        glfwTerminate(); return
    }
    glfwGetVideoMode(glfwGetPrimaryMonitor())?.let { glfwSetWindowPos(window, (it.width() - 940) / 2, (it.height() - 640) / 2) }
    glfwMakeContextCurrent(window)
    glfwSwapInterval(1)
    GL.createCapabilities()

    val vg = NanoVGGL3.nvgCreate(1 or 8)
    if (vg == NULL) return
    loadFont(vg)
    glfwShowWindow(window)

    val state = AppState()
    Config.marketCatalogCache()?.let { state.marketItems = it.items }
    for (p in Config.current.sheetPaths) state.importFiles(listOf(File(p)))
    state.player.humanizeOn = true
    state.player.onEnd = {
        if (state.mode == 1) state.player.play()
        else if (state.mode == 2) {
            val list = state.songs
            if (list.isNotEmpty()) {
                state.selectedSong = (state.selectedSong + 1) % list.size
                state.player.load(list[state.selectedSong]); state.player.play()
            }
        }
    }

    val input = Input()
    var dragging = false
    var dragLastX = 0.0
    var dragLastY = 0.0

    GLFWCursorPosCallback.create { _, x, y ->
        input.mx = x.toFloat(); input.my = y.toFloat()
        if (dragging) {
            val wx = IntArray(1); val wy = IntArray(1); glfwGetWindowPos(window, wx, wy)
            glfwSetWindowPos(window, (wx[0] + (x - dragLastX)).toInt(), (wy[0] + (y - dragLastY)).toInt())
            dragLastX = x; dragLastY = y
        }
    }.set(window)
    GLFWMouseButtonCallback.create { _, button, action, _ ->
        if (button == GLFW_MOUSE_BUTTON_LEFT) {
            if (action == GLFW_PRESS) { input.down = true; input.clicked = true } else input.down = false
        }
    }.set(window)
    GLFWScrollCallback.create { _, _, dy -> input.scroll += dy.toFloat() }.set(window)
    GLFWKeyCallback.create { _, key, _, action, _ ->
        if (key == GLFW_KEY_RIGHT_SHIFT && action == GLFW_PRESS) state.clickGui = !state.clickGui
    }.set(window)

    while (!glfwWindowShouldClose(window)) {
        glfwPollEvents()
        val fbW = IntArray(1); val fbH = IntArray(1)
        glfwGetFramebufferSize(window, fbW, fbH)
        val w = fbW[0]; val h = fbH[0]
        glViewport(0, 0, w, h)
        val bg = Themes.current.p.windowBg
        glClearColor(((bg ushr 16) and 0xFF) / 255f, ((bg ushr 8) and 0xFF) / 255f, (bg and 0xFF) / 255f, 1f)
        glClear(GL_COLOR_BUFFER_BIT or GL_STENCIL_BUFFER_BIT or GL_DEPTH_BUFFER_BIT)
        nvgBeginFrame(vg, w.toFloat(), h.toFloat(), 1f)
        drawApp(Painter(vg), state, input, window, w.toFloat(), h.toFloat())
        nvgEndFrame(vg)
        glfwSwapBuffers(window)
        input.clicked = false
        input.scroll = 0f
    }
    NanoVGGL3.nvgDelete(vg)
    glfwTerminate()
}

private fun loadFont(vg: Long) {
    for (path in listOf("C:/Windows/Fonts/msyh.ttc", "C:/Windows/Fonts/msyh.ttf", "C:/Windows/Fonts/simhei.ttf", "C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/arial.ttf")) {
        if (File(path).exists() && nvgCreateFont(vg, "default", path) != -1) return
    }
}

private fun fmt(ms: Int): String {
    val s = (ms / 1000).coerceAtLeast(0)
    return "%02d:%02d".format(s / 60, s % 60)
}

private fun drawApp(p: Painter, s: AppState, input: Input, window: Long, w: Float, h: Float) {
    val pal = Themes.current.p
    p.panel(0f, 0f, w, h, 0f, Themes.current.windowG, pal.windowStroke)
    p.panel(0f, 0f, w, 46f, 0f, Themes.current.headerG, pal.strokeSoft)
    Icons.draw(p, "piano", 24f, 23f, 18f, pal.accent)
    p.text("SkyPC AutoMusic", 42f, 23f, 15f, pal.textPrimary, AL_LEFT)
    if (iconBtn(p, input, w - 34f, 8f, "grid", pal.accent)) s.clickGui = !s.clickGui
    if (iconBtn(p, input, w - 72f, 8f, "min", pal.textPrimary)) glfwIconifyWindow(window)
    if (iconBtn(p, input, w - 110f, 8f, "close", pal.textPrimary)) glfwSetWindowShouldClose(window, true)

    val nav = listOf("piano" to "演奏", "sheet" to "乐谱", "cart" to "市场", "gear" to "设置")
    nav.forEachIndexed { i, (icon, label) ->
        val y = 56f + i * 66f
        val sel = s.tab == i
        val hovered = input.inside(8f, y, 58f, 58f)
        p.panel(8f, y, 58f, 58f, 12f, if (sel) Themes.current.cardG else Themes.current.surfaceG, if (sel) pal.accent else pal.strokeSoft)
        if (hovered) { nvgBeginPath(p.vg); nvgRoundedRect(p.vg, 8f, y, 58f, 58f, 12f); p.fillColor(0x14FFFFFF.toInt()); nvgFill(p.vg) }
        Icons.draw(p, icon, 37f, y + 22f, 20f, if (sel) pal.accent else pal.textMuted)
        p.text(label, 37f, y + 44f, 11f, if (sel) pal.textPrimary else pal.textMuted, AL_CENTER)
        if (hovered && input.clicked) s.tab = i
    }

    val cx = 82f; val cw = w - cx - 16f
    when (s.tab) {
        0 -> drawPlay(p, s, input, cx, 62f, cw, h - 78f)
        1 -> drawSheets(p, s, input, cx, 62f, cw, h - 78f)
        2 -> drawMarket(p, s, input, cx, 62f, cw, h - 78f)
        else -> drawSettings(p, s, input, cx, 62f, cw, h - 78f)
    }
    if (s.clickGui) drawClickGui(p, s, input, w, h)
}

private fun iconBtn(p: Painter, input: Input, x: Float, y: Float, icon: String, argb: Int): Boolean {
    val hot = input.inside(x, y, 30f, 30f)
    if (hot) p.panel(x, y, 30f, 30f, 9f, Themes.current.surfaceG, Themes.current.p.strokeSoft)
    Icons.draw(p, icon, x + 15f, y + 15f, 16f, argb)
    return hot && input.clicked
}

private fun drawPlay(p: Painter, s: AppState, input: Input, x: Float, y: Float, w: Float, h: Float) {
    val pal = Themes.current.p
    p.text(s.headline(), x + w / 2, y + 14f, 22f, pal.textPrimary, AL_CENTER)
    p.panel(x, y + 44f, w, 76f, 14f, Themes.current.surfaceG, pal.strokeSoft, true)
    val prog = if (s.seekPending >= 0f) s.seekPending else s.player.progress
    slider(p, input, x + 18f, y + 66f, w - 36f, prog) { s.seekPending = it }
    if (s.seekPending >= 0f && !input.down) {
        s.player.seek(s.seekPending)
        s.seekPending = -1f
    }
    p.text(fmt(s.player.positionMs), x + w / 2 - 30f, y + 100f, 11f, pal.textMuted, AL_CENTER)
    p.text("/", x + w / 2, y + 100f, 11f, pal.textMuted, AL_CENTER)
    p.text(fmt(s.player.duration), x + w / 2 + 30f, y + 100f, 11f, pal.textMuted, AL_CENTER)

    val cy = y + 150f
    p.panel(x, cy, w, 156f, 14f, Themes.current.surfaceG, pal.strokeSoft, true)
    val yy = cy + 62f
    if (iconBtnHit(p, input, x + w / 2 - 92f, yy, "prev")) { if (s.songs.isNotEmpty()) { s.selectedSong = (s.selectedSong - 1 + s.songs.size) % s.songs.size; s.player.load(s.songs[s.selectedSong]) } }
    val px = x + w / 2 - 32f; val py = yy - 32f
    p.panel(px, py, 64f, 64f, 18f, Grad(false, pal.accent, pal.accent, 90f), pal.accent, true)
    Icons.draw(p, if (s.player.playing) "pause" else "play", px + 32f, py + 32f, 24f, 0xFF0E1114.toInt())
    if (input.inside(px, py, 64f, 64f) && input.clicked) s.player.togglePlay()
    if (iconBtnHit(p, input, x + w / 2 + 92f, yy, "next")) { if (s.songs.isNotEmpty()) { s.selectedSong = (s.selectedSong + 1) % s.songs.size; s.player.load(s.songs[s.selectedSong]) } }

    if (textBtn(p, input, x + w / 2 - 130f, cy + 108f, when (s.mode) { 0 -> "播完即停"; 1 -> "单曲循环"; else -> "列表循环" })) s.mode = (s.mode + 1) % 3
    s.player.humanizeOn = Config.current.humanize.enabled
    if (toggle(p, input, x + w / 2 + 50f, cy + 112f, Config.current.humanize.enabled)) { Config.current.humanize.enabled = !Config.current.humanize.enabled; s.player.humanizeOn = Config.current.humanize.enabled; Config.save() }
    p.text("拟人化", x + w / 2 + 70f, cy + 130f, 10f, pal.textMuted, AL_CENTER)

    p.panel(x, cy + 168f, w, 64f, 14f, Themes.current.surfaceG, pal.strokeSoft, true)
    p.text("倍速", x + 18f, cy + 200f, 12f, pal.textMuted, AL_LEFT)
    slider(p, input, x + 60f, cy + 188f, w - 150f, (s.player.speed - 0.1f) / 1.9f) { s.player.speed = 0.1f + it * 1.9f }
    p.text("x%.1f".format(s.player.speed), x + w - 18f, cy + 200f, 12f, pal.accent, AL_RIGHT)
}

private fun drawSheets(p: Painter, s: AppState, input: Input, x: Float, y: Float, w: Float, h: Float) {
    val pal = Themes.current.p
    val list = s.songs.toList()
    p.text("乐谱(${list.size})", x, y + 12f, 22f, pal.textPrimary, AL_LEFT)
    if (iconBtn(p, input, x + w - 34f, y - 2f, "add", pal.accent)) s.chooseFiles()
    if (iconBtn(p, input, x + w - 70f, y - 2f, "folder", pal.accent)) s.chooseFiles()
    p.panel(x, y + 40f, w, h - 40f, 14f, Themes.current.surfaceG, pal.strokeSoft)
    val visible = list.take(12)
    visible.forEachIndexed { i, song ->
        val ry = y + 52f + i * 52f
        val selected = i == s.selectedSong
        if (input.inside(x + 8f, ry, w - 16f, 46f)) p.panel(x + 8f, ry, w - 16f, 46f, 10f, Themes.current.surfaceG, if (selected) pal.accent else pal.strokeSoft)
        p.text(song.name, x + 20f, ry + 16f, 13f, if (selected) pal.accent else pal.textPrimary, AL_LEFT)
        p.text("${song.author}  ${song.transcribedBy}", x + 20f, ry + 34f, 11f, pal.textMuted, AL_LEFT)
        if (input.inside(x + 8f, ry, w - 16f, 46f) && input.clicked) { s.selectedSong = i; s.player.load(song) }
    }
    if (list.isEmpty()) p.text("还没有乐谱，点右上角 + 或文件夹导入", x + w / 2, y + 90f, 13f, pal.textMuted, AL_CENTER)
}

private fun drawMarket(p: Painter, s: AppState, input: Input, x: Float, y: Float, w: Float, h: Float) {
    val pal = Themes.current.p
    p.text("曲谱市场", x, y + 12f, 22f, pal.textPrimary, AL_LEFT)
    val status = if (s.marketLoading) "正在获取曲库清单..." else "曲库 ${s.marketItems.size} 首"
    p.text(status, x + w, y + 12f, 11f, pal.textMuted, AL_RIGHT)
    if (textBtn(p, input, x, y + 34f, Config.current.category.ifEmpty { "全部" })) {
        Config.current.category = if (Config.current.category == "全部" || Config.current.category.isEmpty()) "Songs" else "全部"
    }
    p.panel(x + 186f, y + 34f, w - 380f, 32f, 10f, Themes.current.surfaceG, pal.strokeSoft)
    p.text(if (Config.current.marketSearch.isEmpty()) "搜索曲名" else Config.current.marketSearch, x + 198f, y + 50f, 12f, if (Config.current.marketSearch.isEmpty()) pal.textMuted else pal.textPrimary, AL_LEFT)
    if (iconBtn(p, input, x + w - 106f, y + 34f, "refresh", pal.accent)) s.refreshMarket()

    val search = Config.current.marketSearch.lowercase()
    val cat = Config.current.category
    val items = s.marketItems.asSequence()
        .filter { cat.isEmpty() || cat == "全部" || it.group == cat }
        .filter { search.isEmpty() || it.name.lowercase().contains(search) }
        .take(12).toList()
    p.text("下载并导入当前列表 ›", x + w - 8f, y + 84f, 12f, pal.accent, AL_RIGHT)
    val ly = y + 92f
    p.panel(x, ly, w, h - 92f, 14f, Themes.current.surfaceG, pal.strokeSoft)
    items.forEachIndexed { i, item ->
        val ry = ly + 10f + i * 46f
        if (input.inside(x + 8f, ry, w - 16f, 40f)) p.panel(x + 8f, ry, w - 16f, 40f, 9f, Themes.current.surfaceG, pal.strokeSoft)
        p.text(item.name, x + 20f, ry + 15f, 13f, pal.textPrimary, AL_LEFT)
        p.text(item.group, x + 20f, ry + 31f, 10f, pal.textMuted, AL_LEFT)
        p.text("%.1fKB".format(item.size / 1024.0), x + w - 210f, ry + 20f, 11f, pal.textMuted, AL_LEFT)
        if (textBtn(p, input, x + w - 130f, ry + 5f, "下载并导入", accent = true)) s.downloadAndImport(item)
    }
    if (items.isEmpty()) p.text(if (s.marketLoading) "加载中..." else "点右上角刷新获取曲库；或没有匹配结果", x + w / 2, ly + 60f, 13f, pal.textMuted, AL_CENTER)
}

private fun drawSettings(p: Painter, s: AppState, input: Input, x: Float, y: Float, w: Float, h: Float) {
    val pal = Themes.current.p
    p.text("设置", x + w / 2, y + 14f, 22f, pal.textPrimary, AL_CENTER)
    p.text("外观 · 主题", x, y + 44f, 12f, pal.textMuted, AL_LEFT)
    p.panel(x, y + 56f, w, 150f, 14f, Themes.current.surfaceG, pal.strokeSoft)
    Themes.presets.forEachIndexed { i, e ->
        val col = i % 13; val row = i / 13
        val sx = x + 10f + col * 57f; val sy = y + 64f + row * 46f
        val sel = e.id == Themes.current.id
        p.panel(sx, sy, 52f, 38f, 9f, Grad(true, e.p.accent, e.p.accentSoft, 45f), if (sel) pal.accent else pal.strokeSoft)
        p.text(e.name, sx + 26f, sy + 32f, 8f, pal.textPrimary, AL_CENTER)
        if (input.inside(sx, sy, 52f, 38f) && input.clicked) { Themes.select(e.id); Config.current.themeId = e.id; Config.save() }
    }
    var yy = y + 216f
    yy = settingToggle(p, input, x, yy, w, "合并连续相同按键", Config.current.combineKeys) { Config.current.combineKeys = it; s.player.sustain = it; Config.save() }
    yy = settingToggle(p, input, x, yy, w, "后台演奏", Config.current.backgroundPlay) { Config.current.backgroundPlay = it; Config.save() }
    yy = settingToggle(p, input, x, yy, w, "键位映射(SkyStudio)", Config.current.keyMapper) { Config.current.keyMapper = it; Config.save() }
    yy = settingToggle(p, input, x, yy, w, "自定义键位", Config.current.customKeys) { Config.current.customKeys = it; Config.save() }
    yy = settingToggle(p, input, x, yy, w, "递归导入子文件夹", Config.current.subfolders) { Config.current.subfolders = it; Config.save() }
    yy = settingToggle(p, input, x, yy, w, "启用全局热键", Config.current.hotkeys) { Config.current.hotkeys = it; Config.save() }
    settingToggle(p, input, x, yy, w, "拟人化", Config.current.humanize.enabled) { Config.current.humanize.enabled = it; Config.save() }
}

private fun drawClickGui(p: Painter, s: AppState, input: Input, w: Float, h: Float) {
    val pal = Themes.current.p
    p.panel(0f, 0f, w, h, 0f, Grad(false, 0x66000000, 0x66000000, 90f), 0)
    val px = 140f; val py = 100f; val pw = 560f; val ph = 360f
    p.panel(px, py, pw, ph, 14f, Themes.current.surfaceG, pal.strokeSoft, true)
    p.panel(px, py, pw, 40f, 14f, Themes.current.cardG, pal.strokeSoft)
    p.text("ClickGUI", px + 14f, py + 20f, 14f, pal.accent, AL_LEFT)
    Icons.draw(p, "close", px + pw - 22f, py + 20f, 16f, pal.textMuted)
    if (input.inside(px + pw - 36f, py + 6f, 28f, 28f) && input.clicked) s.clickGui = false
    val mods = listOf("自动演奏" to s.player.playing, "合并连续相同按键" to Config.current.combineKeys, "后台演奏" to Config.current.backgroundPlay, "拟人化" to Config.current.humanize.enabled, "全局热键" to Config.current.hotkeys)
    mods.forEachIndexed { i, (name, on) ->
        val my = py + 56f + i * 42f
        p.panel(px + 12f, my, pw - 24f, 34f, 9f, if (on) Themes.current.cardG else Themes.current.surfaceG, if (on) pal.accent else pal.strokeSoft)
        p.text(name, px + 26f, my + 17f, 12f, pal.textPrimary, AL_LEFT)
        if (toggle(p, input, px + pw - 58f, my + 6f, on)) {
            when (i) {
                0 -> s.player.togglePlay()
                1 -> { Config.current.combineKeys = !Config.current.combineKeys; s.player.sustain = Config.current.combineKeys }
                2 -> Config.current.backgroundPlay = !Config.current.backgroundPlay
                3 -> { Config.current.humanize.enabled = !Config.current.humanize.enabled; s.player.humanizeOn = Config.current.humanize.enabled }
                4 -> Config.current.hotkeys = !Config.current.hotkeys
            }
            Config.save()
        }
        if (i == 0 && s.songs.isEmpty()) p.text("(无乐谱)", px + 140f, my + 17f, 11f, pal.textMuted, AL_LEFT)
    }
}

private fun slider(p: Painter, input: Input, x: Float, y: Float, w: Float, value: Float, onChange: (Float) -> Unit) {
    val pal = Themes.current.p
    val cy = y + 8f
    nvgBeginPath(p.vg); nvgRoundedRect(p.vg, x, cy - 3f, w, 6f, 3f); p.fillColor(pal.surfaceHover); nvgFill(p.vg)
    nvgBeginPath(p.vg); nvgRoundedRect(p.vg, x, cy - 3f, w * value.coerceIn(0f, 1f), 6f, 3f); p.fillColor(pal.accent); nvgFill(p.vg)
    nvgBeginPath(p.vg); nvgCircle(p.vg, x + w * value.coerceIn(0f, 1f), cy, 8f); p.fillColor(pal.accent); nvgFill(p.vg)
    if (input.down && input.inside(x - 8f, y - 6f, w + 16f, 28f)) onChange(((input.mx - x) / w).coerceIn(0f, 1f))
}

private fun toggle(p: Painter, input: Input, x: Float, y: Float, checked: Boolean): Boolean {
    val pal = Themes.current.p
    nvgBeginPath(p.vg); nvgRoundedRect(p.vg, x, y, 40f, 22f, 11f); p.fillColor(if (checked) pal.accent else pal.surfaceHover); nvgFill(p.vg)
    nvgBeginPath(p.vg); nvgCircle(p.vg, if (checked) x + 29f else x + 11f, y + 11f, 8f); p.fillColor(pal.textPrimary); nvgFill(p.vg)
    return input.inside(x, y, 40f, 22f) && input.clicked
}

private fun textBtn(p: Painter, input: Input, x: Float, y: Float, label: String, accent: Boolean = false): Boolean {
    val pal = Themes.current.p
    val tw = p.textWidth(label, 12f) + 24f
    val hot = input.inside(x, y, tw, 30f)
    p.panel(x, y, tw, 30f, 9f, if (hot) Themes.current.surfaceG else Grad(false, 0x00000000, 0x00000000, 90f), if (hot) pal.strokeSoft else 0)
    p.text(label, x + tw / 2, y + 15f, 12f, if (accent) pal.accent else if (hot) pal.textPrimary else pal.textMuted, AL_CENTER)
    return hot && input.clicked
}

private fun settingToggle(p: Painter, input: Input, x: Float, y: Float, w: Float, label: String, checked: Boolean, onChange: (Boolean) -> Unit): Float {
    val pal = Themes.current.p
    p.panel(x, y, w, 40f, 11f, Themes.current.surfaceG, pal.strokeSoft)
    p.text(label, x + 16f, y + 20f, 12f, pal.textPrimary, AL_LEFT)
    if (toggle(p, input, x + w - 54f, y + 9f, checked)) onChange(!checked)
    return y + 46f
}

private fun iconBtnHit(p: Painter, input: Input, cx: Float, cy: Float, icon: String): Boolean {
    val hot = input.inside(cx - 22f, cy - 22f, 44f, 44f)
    if (hot) p.panel(cx - 22f, cy - 22f, 44f, 44f, 12f, Themes.current.surfaceG, Themes.current.p.strokeSoft)
    Icons.draw(p, icon, cx, cy, 20f, Themes.current.p.textPrimary)
    return hot && input.clicked
}
