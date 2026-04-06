package com.example.intermodular.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.intermodular.data.remote.ApiErrorHandler
import com.example.intermodular.data.repository.BookingRepository
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

/**
 * ViewModel encargado del flujo de pago de la reserva.
 *
 * @property bookingRepository - Repositorio para realizar el pago auditado en la API
 * @property bookingId - ID de la reserva a pagar
 *
 * @author Luis Lajo
 */
class PaymentViewModel(
    private val bookingRepository: BookingRepository,
    private val bookingId: String
) : ViewModel() {

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading

    private val _errorMessage = MutableStateFlow<String?>(null)
    val errorMessage: StateFlow<String?> = _errorMessage

    private val _paymentSuccessful = MutableStateFlow(false)
    val paymentSuccessful: StateFlow<Boolean> = _paymentSuccessful

    /**
     * Llama al endpoint de la API responsable de registrar el pago y originar el auditLog.
     */
    fun pay() {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null

            try {
                // Simulación opcional de procesamiento manual para mejor UX
                delay(1500)
                bookingRepository.payBooking(bookingId)
                _paymentSuccessful.value = true
            } catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            } finally {
                _isLoading.value = false
            }
        }
    }
}
