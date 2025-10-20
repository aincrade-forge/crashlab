package unity.of.bugs

import android.util.Log
import kotlin.concurrent.thread

object KotlinPlugin {
    @JvmStatic fun `throw`() {
        try {
            throw Exception("Bugs in Kotlin 🐛")
        }
        catch (e: Exception) {
            Log.e("test", "Exception thrown in Kotlin!", e)
            throw e
        }
    }
    @JvmStatic fun throwOnBackgroundThread() {
        thread(start = true) {
            throw Exception("Kotlin 🐛 from a background thread.")
        }
    }

    @JvmStatic fun oom() {
        val blobs = mutableListOf<ByteArray>()
        var size = 8 * 1024 * 1024 // 8MB blocks to accelerate OOM
        try {
            while (true) {
                blobs.add(ByteArray(size))
                if (size < 128 * 1024 * 1024) size *= 2
            }
        } catch (e: OutOfMemoryError) {
            // Let the default uncaught handler (installed by Sentry) capture it
            Log.e("CrashLab", "Kotlin OOM reached", e)
            throw e
        }
    }

    @JvmStatic fun oomOnBackgroundThread() {
        thread(start = true) {
            val blobs = mutableListOf<ByteArray>()
            var size = 8 * 1024 * 1024
            while (true) {
                blobs.add(ByteArray(size))
                if (size < 128 * 1024 * 1024) size *= 2
            }
        }
    }
}
