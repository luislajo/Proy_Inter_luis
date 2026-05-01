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
    val resolvedAt: OffsetDateTime?
) {
    val statusLabel: String
        get() = when (status) {
            "open" -> "Comunicado"
            "resolved" -> "Resuelto"
            else -> status
        }
}

