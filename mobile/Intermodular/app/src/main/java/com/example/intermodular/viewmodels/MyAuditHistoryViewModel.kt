package com.example.intermodular.viewmodels

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.intermodular.data.remote.ApiErrorHandler
import com.example.intermodular.data.remote.auth.SessionManager
import com.example.intermodular.data.remote.dto.AuditLogDto
import com.example.intermodular.data.repository.BookingRepository
import com.example.intermodular.models.AuditHistoryEntry
import com.example.intermodular.util.InvoicePdfOpener
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.time.Instant
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Locale

data class MyAuditHistoryUiState(
    val isLoading: Boolean = false,
    /** Reservas y cambios (CREATE, UPDATE, CANCEL, etc.), no pagos */
    val checkInOutEntries: List<AuditHistoryEntry> = emptyList(),
    val paymentEntries: List<AuditHistoryEntry> = emptyList(),
    val invoiceOpening: Boolean = false
)

class MyAuditHistoryViewModel(
    private val bookingRepository: BookingRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(MyAuditHistoryUiState())
    val uiState: StateFlow<MyAuditHistoryUiState> = _uiState

    private val _errorMessage = MutableStateFlow<String?>(null)
    val errorMessage: StateFlow<String?> = _errorMessage

    init {
        load()
    }

    fun load() {
        val userId = SessionManager.getUserId()
        if (userId == null) {
            _errorMessage.value = "Usuario no autenticado"
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true)
            runCatching {
                bookingRepository.getAuditLogsByUserId(userId)
            }.onSuccess { dtos ->
                val sorted = dtos.sortedByDescending { dto ->
                    runCatching { Instant.parse(dto.timestamp) }.getOrElse { Instant.EPOCH }
                }
                val entries = sorted.map { it.toEntry() }
                _uiState.value = MyAuditHistoryUiState(
                    isLoading = false,
                    checkInOutEntries = entries.filter { !it.isPayment },
                    paymentEntries = entries.filter { it.isPayment },
                    invoiceOpening = false
                )
            }.onFailure { throwable ->
                _uiState.value = _uiState.value.copy(isLoading = false)
                _errorMessage.value = ApiErrorHandler.getErrorMessage(throwable)
            }
        }
    }

    fun clearError() {
        _errorMessage.value = null
    }

    fun openInvoicePdf(bookingId: String, context: Context) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(invoiceOpening = true)
            _errorMessage.value = null
            try {
                val bytes = bookingRepository.getInvoicePdfBytes(bookingId)
                if (!InvoicePdfOpener.openPdfFromBytes(context, bookingId, bytes)) {
                    _errorMessage.value = "No hay ninguna aplicación para abrir el PDF"
                }
            } catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            } finally {
                _uiState.value = _uiState.value.copy(invoiceOpening = false)
            }
        }
    }

    private fun AuditLogDto.toEntry(): AuditHistoryEntry {
        val payment = action == "PAYMENT"
        val bookingRef =
            if (entity_type.equals("booking", ignoreCase = true)) entity_id else null
        return AuditHistoryEntry(
            id = _id,
            actionLabel = action.toActionLabel(),
            dateTimeText = formatTimestamp(timestamp),
            isPayment = payment,
            bookingId = bookingRef
        )
    }

    private fun String.toActionLabel(): String = when (this) {
        "CREATE" -> "Check-in (reserva creada)"
        "PAYMENT" -> "Pago"
        "UPDATE" -> "Cambio en la reserva"
        "CANCEL" -> "Reserva cancelada"
        "DELETE" -> "Reserva eliminada"
        else -> "Acción: $this"
    }

    companion object {
        private val dateTimeFormatter: DateTimeFormatter =
            DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm", Locale.forLanguageTag("es-ES"))

        private fun formatTimestamp(iso: String): String {
            return runCatching {
                val instant = Instant.parse(iso)
                instant.atZone(ZoneId.systemDefault()).format(dateTimeFormatter)
            }.getOrElse { iso }
        }
    }
}
