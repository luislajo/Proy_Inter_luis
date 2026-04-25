package com.example.intermodular.util

import android.content.Context
import android.content.Intent
import androidx.core.content.FileProvider
import com.example.intermodular.BuildConfig
import java.io.File

/**
 * Guarda el PDF en caché y lo abre con una app del sistema (Drive, Chrome, visor PDF).
 * Evita embebibles nativos de PDF que suelen complicar dependencias y versiones de NDK.
 */
object InvoicePdfOpener {

    fun openPdfFromBytes(context: Context, bookingId: String, bytes: ByteArray): Boolean {
        val dir = File(context.cacheDir, "invoices").apply { mkdirs() }
        val file = File(dir, "factura_$bookingId.pdf")
        file.writeBytes(bytes)
        val uri = FileProvider.getUriForFile(
            context,
            "${BuildConfig.APPLICATION_ID}.fileprovider",
            file
        )
        val intent = Intent(Intent.ACTION_VIEW).apply {
            setDataAndType(uri, "application/pdf")
            addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)
        }
        return try {
            context.startActivity(Intent.createChooser(intent, "Abrir factura"))
            true
        } catch (_: Exception) {
            false
        }
    }
}
