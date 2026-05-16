package com.example.intermodular.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.intermodular.data.remote.ApiErrorHandler
import com.example.intermodular.data.repository.IncidentRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

/**
 * Envío de incidencia desde pantalla completa (habitación conocida por [roomId]).
 */
class ReportIncidentViewModel(
    private val roomId: String,
    private val incidentRepository: IncidentRepository
) : ViewModel() {

    private val _sending = MutableStateFlow(false)
    val sending: StateFlow<Boolean> = _sending

    private val _errorMessage = MutableStateFlow<String?>(null)
    val errorMessage: StateFlow<String?> = _errorMessage

    fun clearError() {
        _errorMessage.value = null
    }

    fun submit(type: String, severity: String, description: String, onSuccess: () -> Unit) {
        if (description.isBlank()) {
            _errorMessage.value = "Escribe una descripción del problema"
            return
        }
        viewModelScope.launch {
            _sending.value = true
            _errorMessage.value = null
            runCatching {
                incidentRepository.createIncident(roomId, type, severity, description)
            }.onSuccess {
                onSuccess()
            }.onFailure { e ->
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            }
            _sending.value = false
        }
    }
}
