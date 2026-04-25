package com.example.intermodular.models

/**
 * Entrada de UI para una fila del historial de auditoría.
 * Los pagos ([isPayment]) se muestran en la sección inferior; el resto en check-ins / actividad de reserva.
 */
data class AuditHistoryEntry(
    val id: String,
    val actionLabel: String,
    val dateTimeText: String,
    val isPayment: Boolean,
    /** Id de reserva cuando el log está asociado a una reserva (p. ej. pago). */
    val bookingId: String? = null
)
