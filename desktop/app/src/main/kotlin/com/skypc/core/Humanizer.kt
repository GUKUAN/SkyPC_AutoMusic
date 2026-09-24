package com.skypc.core

import java.util.Random
import kotlin.math.cos
import kotlin.math.ln
import kotlin.math.sqrt

//按种子给每个音符生成稳定的拟人化误差
class Humanizer(private val hs: HumanizeSettings, private val seed: Int, private val count: Int) {
    private fun rnd(i: Int, salt: Int): Random = Random((seed xor (i * 486187739) xor (salt * 16777619)).toLong())

    private fun fatigue(i: Int): Double {
        if (!hs.fatigueEnabled) return 1.0
        val t = if (count <= 1) 0.0 else i.toDouble() / (count - 1)
        return hs.fatigueStart + (hs.fatigueEnd - hs.fatigueStart) * t
    }

    fun offset(i: Int): Double {
        if (!hs.jitterEnabled) return 0.0
        val lo = minOf(hs.jitterMin, hs.jitterMax).toDouble()
        val hi = maxOf(hs.jitterMin, hs.jitterMax).toDouble()
        val r = rnd(i, 1).nextDouble()
        val v = when (hs.jitterMode) {
            "Gaussian" -> {
                val mean = (lo + hi) / 2
                val sigma = (hi - lo) / 6
                (mean + sigma * gaussian(rnd(i, 7))).coerceIn(lo, hi)
            }
            "Early" -> lo + (hi - lo) * r * r
            "Late" -> hi - (hi - lo) * r * r
            "Triangular" -> lo + (hi - lo) * (r + rnd(i, 8).nextDouble()) / 2
            else -> lo + (hi - lo) * r
        }
        return v * hs.strength * fatigue(i)
    }

    fun hold(base: Int, i: Int): Int {
        if (!hs.holdEnabled) return base
        val lo = minOf(hs.holdMin, hs.holdMax).toDouble()
        val hi = maxOf(hs.holdMin, hs.holdMax).toDouble()
        val d = lo + (hi - lo) * rnd(i, 3).nextDouble()
        return maxOf(5, (base + d * hs.strength * fatigue(i)).toInt())
    }

    fun drop(i: Int): Boolean = hs.dropEnabled && rnd(i, 4).nextDouble() < hs.dropChance * hs.strength * fatigue(i)

    fun wrong(i: Int): Boolean = hs.wrongEnabled && rnd(i, 5).nextDouble() < hs.wrongChance * hs.strength * fatigue(i)

    private fun gaussian(r: Random): Double {
        val u1 = 1.0 - r.nextDouble()
        val u2 = 1.0 - r.nextDouble()
        return sqrt(-2.0 * ln(u1)) * cos(2.0 * Math.PI * u2)
    }
}
