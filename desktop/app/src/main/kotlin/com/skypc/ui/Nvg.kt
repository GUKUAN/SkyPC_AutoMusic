package com.skypc.ui

import com.skypc.theme.Grad
import org.lwjgl.nanovg.NVGColor
import org.lwjgl.nanovg.NVGPaint
import org.lwjgl.nanovg.NanoVG.*
import kotlin.math.cos
import kotlin.math.sin

class Input {
    var mx = 0f
    var my = 0f
    var down = false
    var clicked = false
    var scroll = 0f
    fun inside(x: Float, y: Float, w: Float, h: Float) = mx in x..(x + w) && my in y..(y + h)
}

private fun b(v: Int): Byte = (v and 0xFF).toByte()

//NanoVG 绘制封装：颜色/渐变/阴影/圆角/文字/图标
class Painter(val vg: Long) {
    private val cA: NVGColor = NVGColor.malloc()
    private val cB: NVGColor = NVGColor.malloc()
    private val paint: NVGPaint = NVGPaint.malloc()

    private fun argbToColor(argb: Int, out: NVGColor) {
        nvgRGBA(b((argb ushr 16) and 0xFF), b((argb ushr 8) and 0xFF), b(argb and 0xFF), b((argb ushr 24) and 0xFF), out)
    }

    fun col(argb: Int): NVGColor {
        argbToColor(argb, cA)
        return cA
    }

    fun fillColor(argb: Int) {
        nvgFillColor(vg, col(argb))
    }

    fun strokeColor(argb: Int) {
        nvgStrokeColor(vg, col(argb))
    }

    //角度 -> 线性渐变
    private fun gradPaint(g: Grad, x: Float, y: Float, w: Float, h: Float): NVGPaint {
        val rad = g.angle * Math.PI / 180.0
        val dx = cos(rad).toFloat() * w / 2f
        val dy = sin(rad).toFloat() * h / 2f
        argbToColor(g.start, cA)
        argbToColor(g.end, cB)
        val end = if (g.enabled) cB else cA
        nvgLinearGradient(vg, x + w / 2 - dx, y + h / 2 - dy, x + w / 2 + dx, y + h / 2 + dy, cA, end, paint)
        return paint
    }

    fun panel(x: Float, y: Float, w: Float, h: Float, r: Float, fill: Grad, border: Int, shadow: Boolean = false) {
        if (shadow) {
            nvgBeginPath(vg)
            nvgRGBA(b(0), b(0), b(0), b(90), cA)
            nvgRGBA(b(0), b(0), b(0), b(0), cB)
            nvgBoxGradient(vg, x - 4f, y - 2f, w + 8f, h + 16f, r + 4f, 14f, cA, cB, paint)
            nvgRect(vg, x - 14f, y - 8f, w + 28f, h + 34f)
            nvgFillPaint(vg, paint)
            nvgFill(vg)
        }
        nvgBeginPath(vg)
        nvgRoundedRect(vg, x, y, w, h, r)
        nvgFillPaint(vg, gradPaint(fill, x, y, w, h))
        nvgFill(vg)
        if ((border ushr 24) != 0) {
            nvgBeginPath(vg)
            nvgRoundedRect(vg, x + 0.5f, y + 0.5f, w - 1f, h - 1f, r)
            strokeColor(border)
            nvgStrokeWidth(vg, 1f)
            nvgStroke(vg)
        }
        //顶部高光，模拟玻璃受光
        nvgBeginPath(vg)
        nvgRoundedRect(vg, x + 1f, y + 1f, w - 2f, h * 0.5f, r)
        nvgRGBA(b(255), b(255), b(255), b(12), cA)
        nvgRGBA(b(255), b(255), b(255), b(0), cB)
        nvgLinearGradient(vg, x, y, x, y + h * 0.5f, cA, cB, paint)
        nvgFillPaint(vg, paint)
        nvgFill(vg)
    }

    fun text(t: String, x: Float, y: Float, size: Float, argb: Int, align: Int = NVG_ALIGN_LEFT or NVG_ALIGN_MIDDLE) {
        nvgFontFace(vg, "default")
        nvgFontSize(vg, size)
        nvgTextAlign(vg, align)
        fillColor(argb)
        nvgText(vg, x, y, t)
    }

    fun textWidth(t: String, size: Float): Float {
        nvgFontFace(vg, "default")
        nvgFontSize(vg, size)
        return nvgTextBounds(vg, 0f, 0f, t, floatArrayOf(0f, 0f, 0f, 0f))
    }
}

//简单矢量图标
object Icons {
    fun draw(p: Painter, name: String, cx: Float, cy: Float, s: Float, argb: Int) {
        val vg = p.vg
        nvgBeginPath(vg)
        var filled = false
        when (name) {
            "play" -> {
                nvgMoveTo(vg, cx - s * 0.3f, cy - s * 0.45f)
                nvgLineTo(vg, cx + s * 0.5f, cy)
                nvgLineTo(vg, cx - s * 0.3f, cy + s * 0.45f)
                nvgClosePath(vg)
                filled = true
            }
            "pause" -> {
                nvgRoundedRect(vg, cx - s * 0.4f, cy - s * 0.45f, s * 0.28f, s * 0.9f, 2f)
                nvgRoundedRect(vg, cx + s * 0.12f, cy - s * 0.45f, s * 0.28f, s * 0.9f, 2f)
                filled = true
            }
            "prev" -> {
                nvgRoundedRect(vg, cx - s * 0.5f, cy - s * 0.45f, s * 0.16f, s * 0.9f, 2f)
                nvgMoveTo(vg, cx + s * 0.45f, cy - s * 0.45f)
                nvgLineTo(vg, cx - s * 0.2f, cy)
                nvgLineTo(vg, cx + s * 0.45f, cy + s * 0.45f)
                nvgClosePath(vg)
                filled = true
            }
            "next" -> {
                nvgRoundedRect(vg, cx + s * 0.34f, cy - s * 0.45f, s * 0.16f, s * 0.9f, 2f)
                nvgMoveTo(vg, cx - s * 0.45f, cy - s * 0.45f)
                nvgLineTo(vg, cx + s * 0.2f, cy)
                nvgLineTo(vg, cx - s * 0.45f, cy + s * 0.45f)
                nvgClosePath(vg)
                filled = true
            }
            "piano" -> {
                nvgRoundedRect(vg, cx - s * 0.5f, cy - s * 0.4f, s, s * 0.8f, 3f)
                nvgMoveTo(vg, cx - s * 0.15f, cy - s * 0.4f); nvgLineTo(vg, cx - s * 0.15f, cy + s * 0.4f)
                nvgMoveTo(vg, cx + s * 0.15f, cy - s * 0.4f); nvgLineTo(vg, cx + s * 0.15f, cy + s * 0.4f)
            }
            "sheet" -> {
                nvgRoundedRect(vg, cx - s * 0.35f, cy - s * 0.5f, s * 0.7f, s, 2f)
                nvgMoveTo(vg, cx - s * 0.2f, cy - s * 0.25f); nvgLineTo(vg, cx + s * 0.2f, cy - s * 0.25f)
                nvgMoveTo(vg, cx - s * 0.2f, cy); nvgLineTo(vg, cx + s * 0.2f, cy)
                nvgMoveTo(vg, cx - s * 0.2f, cy + s * 0.25f); nvgLineTo(vg, cx + s * 0.2f, cy + s * 0.25f)
            }
            "cart" -> {
                nvgRoundedRect(vg, cx - s * 0.42f, cy - s * 0.35f, s * 0.84f, s * 0.6f, 3f)
                nvgMoveTo(vg, cx - s * 0.42f, cy + s * 0.25f); nvgLineTo(vg, cx + s * 0.42f, cy + s * 0.25f)
            }
            "gear" -> {
                nvgCircle(vg, cx, cy, s * 0.4f)
                nvgCircle(vg, cx, cy, s * 0.14f)
            }
            "close" -> {
                nvgMoveTo(vg, cx - s * 0.35f, cy - s * 0.35f); nvgLineTo(vg, cx + s * 0.35f, cy + s * 0.35f)
                nvgMoveTo(vg, cx + s * 0.35f, cy - s * 0.35f); nvgLineTo(vg, cx - s * 0.35f, cy + s * 0.35f)
            }
            "min" -> {
                nvgMoveTo(vg, cx - s * 0.35f, cy); nvgLineTo(vg, cx + s * 0.35f, cy)
            }
            "grid" -> {
                nvgRoundedRect(vg, cx - s * 0.45f, cy - s * 0.45f, s * 0.32f, s * 0.32f, 2f)
                nvgRoundedRect(vg, cx + s * 0.13f, cy - s * 0.45f, s * 0.32f, s * 0.32f, 2f)
                nvgRoundedRect(vg, cx - s * 0.45f, cy + s * 0.13f, s * 0.32f, s * 0.32f, 2f)
                nvgRoundedRect(vg, cx + s * 0.13f, cy + s * 0.13f, s * 0.32f, s * 0.32f, 2f)
            }
            "search" -> {
                nvgCircle(vg, cx - s * 0.1f, cy - s * 0.1f, s * 0.3f)
                nvgMoveTo(vg, cx + s * 0.12f, cy + s * 0.12f); nvgLineTo(vg, cx + s * 0.4f, cy + s * 0.4f)
            }
            "add" -> {
                nvgMoveTo(vg, cx - s * 0.35f, cy); nvgLineTo(vg, cx + s * 0.35f, cy)
                nvgMoveTo(vg, cx, cy - s * 0.35f); nvgLineTo(vg, cx, cy + s * 0.35f)
            }
            "trash" -> {
                nvgRoundedRect(vg, cx - s * 0.3f, cy - s * 0.2f, s * 0.6f, s * 0.62f, 2f)
                nvgMoveTo(vg, cx - s * 0.4f, cy - s * 0.35f); nvgLineTo(vg, cx + s * 0.4f, cy - s * 0.35f)
            }
            "folder" -> {
                nvgRoundedRect(vg, cx - s * 0.45f, cy - s * 0.25f, s * 0.9f, s * 0.55f, 2f)
            }
            "refresh" -> {
                nvgCircle(vg, cx, cy, s * 0.34f)
                nvgMoveTo(vg, cx + s * 0.34f, cy - s * 0.12f); nvgLineTo(vg, cx + s * 0.14f, cy - s * 0.34f)
            }
            "github" -> {
                nvgCircle(vg, cx, cy, s * 0.42f)
                nvgCircle(vg, cx, cy, s * 0.16f)
            }
            else -> nvgCircle(vg, cx, cy, s * 0.3f)
        }
        if (filled) {
            p.fillColor(argb)
            nvgFill(vg)
        } else {
            p.strokeColor(argb)
            nvgStrokeWidth(vg, 1.6f)
            nvgStroke(vg)
        }
    }
}
