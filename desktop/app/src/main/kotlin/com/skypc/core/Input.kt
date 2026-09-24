package com.skypc.core

import com.sun.jna.Library
import com.sun.jna.Native

private interface User32Key : Library {
    fun keybd_event(bVk: Byte, bScan: Byte, dwFlags: Int, dwExtraInfo: Int)
    fun MapVirtualKey(uCode: Int, uMapType: Int): Int
}

//Windows 输入注入（keybd_event）；其它平台先空实现
object WinKeys {
    private val user32: User32Key? = try {
        if (System.getProperty("os.name").lowercase().contains("win"))
            Native.load("user32", User32Key::class.java)
        else null
    } catch (e: Throwable) {
        null
    }

    //默认天空 15 键布局 YUIOP HJKL; NM,./
    val defaultVk = intArrayOf(0x59, 0x55, 0x49, 0x4F, 0x50, 0x48, 0x4A, 0x4B, 0x4C, 0xBA, 0x4E, 0x4D, 0xBC, 0xBE, 0xBF)

    fun press(noteIndex: Int, down: Boolean, custom: IntArray? = null) {
        val vk = (custom?.getOrNull(noteIndex) ?: defaultVk[noteIndex.coerceIn(0, 14)])
        val u = user32 ?: return
        val scan = u.MapVirtualKey(vk, 0)
        u.keybd_event(vk.toByte(), scan.toByte(), if (down) 0 else 2, 0)
    }
}
