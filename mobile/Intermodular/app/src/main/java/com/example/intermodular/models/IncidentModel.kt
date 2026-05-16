package com.example.intermodular.models

import java.time.OffsetDateTime

data class Incident(
    val id: String,
    val roomId: String,
    val type: String,
    val severity: String,
    val description: String,
    val status: String,
    val reportedAt: OffsetDateTime,
    val resolvedAt: OffsetDateTime?,
    val assignedTo: String? = null
) {
    val statusLabel: String
        get() = when {
            status == "resolved" -> "Resuelto"
            !assignedTo.isNullOrBlank() -> "En proceso"
            status == "open" -> "Comunicado"
            else -> status
        }
}

