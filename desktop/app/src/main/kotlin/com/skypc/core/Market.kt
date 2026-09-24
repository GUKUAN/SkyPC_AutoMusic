package com.skypc.core

import com.google.gson.JsonParser
import java.io.File
import java.net.HttpURLConnection
import java.net.URI
import java.net.URLEncoder

class MarketItem {
    var path: String = ""
    var size: Long = 0
    var name: String = ""
    var group: String = ""
    var sub: String = ""

    constructor()
    constructor(path: String, size: Long, name: String, group: String, sub: String) {
        this.path = path
        this.size = size
        this.name = name
        this.group = group
        this.sub = sub
    }
}

object Market {
    const val REPO = "Ai-Vonie/Sky1984-Sheets-Collection"
    const val BRANCH = "master"
    private val roots = listOf("Songs/", "Multi-Sheet Songs/", "VSRG/")

    fun fetch(token: String): List<MarketItem> {
        val url = "https://api.github.com/repos/$REPO/git/trees/$BRANCH?recursive=1"
        val json = get(url, token) ?: return emptyList()
        val root = JsonParser.parseString(json).asJsonObject
        val tree = root.getAsJsonArray("tree") ?: return emptyList()
        val items = ArrayList<MarketItem>()
        for (node in tree) {
            val o = node.asJsonObject
            if (o.get("type")?.asString != "blob") continue
            val path = o.get("path")?.asString ?: continue
            if (!path.endsWith(".txt", true)) continue
            if (roots.none { path.startsWith(it) }) continue
            val fileName = path.substringAfterLast('/')
            if (fileName.startsWith(".")) continue
            val seg = path.split('/')
            items.add(
                MarketItem(
                    path,
                    o.get("size")?.asLong ?: 0,
                    fileName.removeSuffix(".txt"),
                    seg.getOrElse(0) { "" },
                    seg.getOrElse(1) { "" },
                )
            )
        }
        return items
    }

    fun rawUrl(path: String): String {
        val escaped = path.split('/').joinToString("/") { URLEncoder.encode(it, "UTF-8").replace("+", "%20") }
        return "https://raw.githubusercontent.com/$REPO/$BRANCH/$escaped"
    }

    fun localFile(path: String): File = File(AppPaths.marketDir, path.replace('/', File.separatorChar))

    fun download(item: MarketItem, token: String): File? {
        val local = localFile(item.path)
        if (local.exists()) return local
        val bytes = getBytes(rawUrl(item.path), token) ?: return null
        local.parentFile?.mkdirs()
        local.writeBytes(bytes)
        return local
    }

    private fun get(url: String, token: String): String? {
        return try {
            val conn = open(url, token)
            if (conn.responseCode !in 200..299) return null
            conn.inputStream.bufferedReader().readText()
        } catch (e: Exception) {
            null
        }
    }

    private fun getBytes(url: String, token: String): ByteArray? {
        return try {
            val conn = open(url, token)
            if (conn.responseCode !in 200..299) return null
            conn.inputStream.readBytes()
        } catch (e: Exception) {
            null
        }
    }

    private fun open(url: String, token: String): HttpURLConnection {
        val conn = URI(url).toURL().openConnection() as HttpURLConnection
        conn.setRequestProperty("User-Agent", "SkyPC-AutoMusic")
        conn.connectTimeout = 20000
        conn.readTimeout = 60000
        if (token.isNotBlank()) conn.setRequestProperty("Authorization", "token ${token.trim()}")
        return conn
    }
}
