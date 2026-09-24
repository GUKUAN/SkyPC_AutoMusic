package com.skypc.core

//播放器：后台线程按时间轴发按键（Windows 走 keybd_event）
class Player {
    var song: Song? = null
        private set

    @Volatile var playing = false
        private set

    @Volatile var positionMs = 0
    @Volatile var speed = 1f
    @Volatile var sustain = true
    var humanizeOn = true
    var onEnd: (() -> Unit)? = null

    private var thread: Thread? = null
    private var humanizer: Humanizer? = null
    private val pressed = HashSet<Int>()

    val duration: Int get() = song?.duration ?: 0
    val progress: Float get() = if (duration <= 0) 0f else (positionMs.toFloat() / duration).coerceIn(0f, 1f)

    fun load(s: Song) {
        stop()
        song = s
        positionMs = 0
        humanizer = if (humanizeOn && Config.current.humanize.enabled)
            Humanizer(Config.current.humanize, s.sourcePath.hashCode(), maxOf(1, s.notes.size)) else null
    }

    fun togglePlay() {
        if (song == null) return
        if (playing) pause() else play()
    }

    fun play() {
        val s = song ?: return
        if (playing || s.notes.isEmpty()) return
        playing = true
        startLoop(s)
    }

    fun pause() {
        playing = false
    }

    fun stop() {
        playing = false
        positionMs = 0
        releaseAll()
    }

    fun seek(fraction: Float) {
        positionMs = (fraction * duration).toInt()
        if (playing) {
            playing = false
            releaseAll()
            play()
        }
    }

    private fun startLoop(s: Song) {
        val notes = s.notes
        var idx = 0
        while (idx < notes.size && notes[idx].time < positionMs) idx++
        val startWall = System.currentTimeMillis()
        val startPos = positionMs
        val pending = java.util.PriorityQueue<Pair<Int, Int>>(compareBy { it.first })
        val local = HashSet<Int>()
        var finished = false

        thread = Thread {
            try {
                while (playing) {
                    val pos = startPos + ((System.currentTimeMillis() - startWall) * speed).toInt()
                    positionMs = pos
                    while (idx < notes.size && notes[idx].time <= pos) {
                        val n = notes[idx]
                        if (humanizer?.drop(idx) != true) {
                            var key = n.key
                            if (humanizer?.wrong(idx) == true) key = if (key >= 14) key - 1 else key + 1
                            WinKeys.press(key, true)
                            local.add(key)
                            val hold = humanizer?.hold(50, idx) ?: 50
                            pending.add((pos + hold) to key)
                        }
                        idx++
                    }
                    while (pending.isNotEmpty() && pending.peek().first <= pos) {
                        val (_, k) = pending.poll()
                        if (local.remove(k)) WinKeys.press(k, false)
                    }
                    if (idx >= notes.size && pending.isEmpty()) {
                        playing = false
                        finished = true
                        break
                    }
                    Thread.sleep(2)
                }
            } catch (_: InterruptedException) {
            } finally {
                for (k in local) WinKeys.press(k, false)
                local.clear()
                if (finished && onEnd != null) onEnd!!.invoke()
            }
        }.also { it.isDaemon = true; it.start() }
    }

    private fun releaseAll() {
        synchronized(pressed) {
            for (k in 0 until 15) WinKeys.press(k, false)
            pressed.clear()
        }
    }
}
