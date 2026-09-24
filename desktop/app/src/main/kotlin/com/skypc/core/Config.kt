package com.skypc.core

import com.google.gson.Gson
import com.google.gson.GsonBuilder
import java.io.File

object AppPaths {
    val base: File = File(System.getProperty("user.home"), "SkyPC-AutoMusic").apply { mkdirs() }
    val settings: File = File(base, "settings.json")
    val marketDir: File = File(base, "market").apply { mkdirs() }
    val catalog: File = File(marketDir, "catalog.json")
}

class HumanizeSettings {
    var enabled = false
    var strength = 1.0

    var jitterEnabled = false
    var jitterMin = -15
    var jitterMax = 15
    var jitterMode = "Uniform"

    var holdEnabled = false
    var holdMin = -20
    var holdMax = 20

    var dropEnabled = false
    var dropChance = 0.02

    var wrongEnabled = false
    var wrongChance = 0.01

    var fatigueEnabled = false
    var fatigueStart = 0.6
    var fatigueEnd = 1.6
}

class AppSettings {
    var themeId = "classic"
    var language = "Auto"
    var combineKeys = true
    var backgroundImage = false
    var backgroundPlay = false
    var keyMapper = false
    var customKeys = false
    var subfolders = false
    var hotkeys = false
    var token = ""
    var category = "全部"
    var marketSearch = ""
    var humanize = HumanizeSettings()
    var sheetPaths = mutableListOf<String>()
}

object Config {
    private val gson: Gson = GsonBuilder().setPrettyPrinting().create()
    var current: AppSettings = AppSettings()
        private set

    fun load() {
        try {
            if (AppPaths.settings.exists()) {
                current = gson.fromJson(AppPaths.settings.readText(), AppSettings::class.java) ?: AppSettings()
            }
        } catch (e: Exception) {
            current = AppSettings()
        }
    }

    fun save() {
        try {
            AppPaths.settings.writeText(gson.toJson(current))
        } catch (_: Exception) {
        }
    }

    fun marketCatalogCache(): MarketCache? {
        return try {
            if (AppPaths.catalog.exists()) gson.fromJson(AppPaths.catalog.readText(), MarketCache::class.java) else null
        } catch (e: Exception) {
            null
        }
    }

    fun saveMarketCache(items: List<MarketItem>) {
        try {
            AppPaths.catalog.writeText(gson.toJson(MarketCache(System.currentTimeMillis(), items)))
        } catch (_: Exception) {
        }
    }
}

class MarketCache {
    var fetchedAt: Long = 0
    var items: List<MarketItem> = emptyList()

    constructor()
    constructor(fetchedAt: Long, items: List<MarketItem>) {
        this.fetchedAt = fetchedAt
        this.items = items
    }
}
