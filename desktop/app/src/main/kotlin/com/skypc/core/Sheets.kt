package com.skypc.core

import com.google.gson.JsonArray
import com.google.gson.JsonObject
import com.google.gson.JsonParser
import java.io.File

class Note(val time: Int, val key: Int)

class Song(
    var name: String,
    var author: String,
    var transcribedBy: String,
    var bpm: Int,
    var pitchLevel: Int,
    var notes: MutableList<Note>,
    var sourcePath: String,
) {
    val duration: Int get() = if (notes.isEmpty()) 0 else notes.last().time + 500
}

//兼容 SkyStudio / Nightly(v2,v3，带 songNotes) 与 VSRG(hitObjects)
object SheetParser {
    fun parse(text: String, sourcePath: String): Song? {
        return try {
            val start = text.indexOf('{')
            val end = text.lastIndexOf('}')
            if (start < 0 || end <= start) return null
            val json = text.substring(start, end + 1)
            val root = JsonParser.parseString(json).asJsonObject
            if (root.has("tracks") || root.has("hitObjects")) fromVsrg(root, sourcePath) else fromNormal(root, sourcePath)
        } catch (e: Exception) {
            null
        }
    }

    fun parseFile(path: String): Song? {
        return try {
            parse(File(path).readText(), path)
        } catch (e: Exception) {
            null
        }
    }

    private fun fromNormal(root: JsonObject, sourcePath: String): Song? {
        val notes = root.getAsJsonArray("songNotes") ?: return null
        val list = ArrayList<Note>(notes.size())
        for (el in notes) {
            val o = el.asJsonObject
            val key = keyIndex(o.get("key")?.asString ?: continue)
            list.add(Note(o.get("time").asInt, key))
        }
        list.sortBy { it.time }
        val bpm = root.get("bpm")?.asInt ?: 240
        val pitch = root.get("pitchLevel")?.asInt ?: 0
        return Song(
            str(root, "name", "Untitled"),
            str(root, "author", ""),
            str(root, "transcribedBy", ""),
            if (bpm <= 0) 240 else bpm,
            pitch,
            list,
            sourcePath,
        )
    }

    private fun fromVsrg(root: JsonObject, sourcePath: String): Song? {
        val list = ArrayList<Note>()
        val tracks = root.getAsJsonArray("tracks") ?: JsonArray()
        for (track in tracks) {
            val hits = track.asJsonObject.getAsJsonArray("hitObjects") ?: continue
            for (hit in hits) {
                val arr = hit.asJsonArray
                if (arr.size() < 4) continue
                val time = arr.get(1).asDouble.toInt()
                val pitches = arr.get(3)
                if (!pitches.isJsonArray) continue
                for (p in pitches.asJsonArray) {
                    list.add(Note(time, p.asInt.coerceIn(0, 14)))
                }
            }
        }
        if (list.isEmpty()) return null
        list.sortBy { it.time }
        val bpm = root.get("bpm")?.asInt ?: 240
        return Song(
            str(root, "name", "Untitled"),
            str(root, "author", ""),
            str(root, "transcribedBy", ""),
            if (bpm <= 0) 240 else bpm,
            pitchToLevel(root.get("pitch")?.asString),
            list,
            sourcePath,
        )
    }

    private fun str(o: JsonObject, key: String, def: String): String {
        return o.get(key)?.takeIf { it.isJsonPrimitive }?.asString ?: def
    }

    private fun keyIndex(raw: String): Int {
        val tail = raw.substringAfterLast("Key", "")
        var idx = tail.toIntOrNull() ?: 0
        if (idx == 15) idx = 1
        return idx.coerceIn(0, 14)
    }

    private fun pitchToLevel(p: String?): Int {
        return when (p) {
            "C" -> 0; "C#", "Db", "D♭" -> 1; "D" -> 2; "D#", "Eb", "E♭" -> 3; "E" -> 4
            "F" -> 5; "F#", "Gb", "G♭" -> 6; "G" -> 7; "G#", "Ab", "A♭" -> 8; "A" -> 9
            "A#", "Bb", "B♭" -> 10; "B" -> 11; else -> 0
        }
    }
}
