package com.example.intermodular.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.intermodular.data.remote.ApiErrorHandler
import com.example.intermodular.data.remote.auth.SessionManager
import android.content.Context
import com.example.intermodular.data.repository.BookingRepository
import com.example.intermodular.data.repository.RoomRepository
import com.example.intermodular.models.Booking
import com.example.intermodular.models.Room
import com.example.intermodular.models.RoomFilter
import com.example.intermodular.util.InvoicePdfOpener
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

/**
 * ViewModel para la vista que permite ver las reservas anteriores del usuario autenticado
 * @author Axel Zaragoci
 *
 * @property bookingRepository - Repositorio para obtener datos de reservas de la API
 * @property roomRepository - Repositorio para obtener datos de habitaciones de la API
 */
class MyBookingsViewModel(
    private val bookingRepository: BookingRepository,
    private val roomRepository: RoomRepository
) : ViewModel() {

    // ==================== ESTADOS DE LA UI ====================
    /**
     * Lista de todas las reservas del usuario autenticado
     */
    private val _bookings = MutableStateFlow<List<Booking>>(emptyList())
    val bookings: StateFlow<List<Booking>> = _bookings

    /**
     * Lista de todas las habitaciones
     */
    private val _rooms = MutableStateFlow<List<Room>>(emptyList())
    val rooms : StateFlow<List<Room>> = _rooms

    /**
     * Mensaje de error a mostrar al usuario
     */
    private val _errorMessage = MutableStateFlow<String?>(null)
    val errorMessage: StateFlow<String?> = _errorMessage

    /**
     * Indicador de carga para funciones asíncronas
     */
    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading

    private val _invoiceOpening = MutableStateFlow(false)
    val invoiceOpening: StateFlow<Boolean> = _invoiceOpening

    //==================== MÉTODOS PÚBLICOS ==============================
    /**
     * Bloque de inicio del ViewModel
     * Se encarga de cargar todos los datos que se van a utilizar
     */
    init {
        loadBookings()
    }

    /**
     * Carga todos los datos necesarios
     *
     * Flujo principal:
     * 1. Obtiene todas las habitaciones
     * 2. Carga todas las reservas del usuario que tenga la sesión iniciada
     *
     * En caso de error, muestra el error acorde al [ApiErrorHandler]
     */
    fun loadBookings() {
        viewModelScope.launch {
            _isLoading.value = true
            try {
                _rooms.value = roomRepository.getRooms(RoomFilter())

                _bookings.value = bookingRepository.getBookingsByUserId(SessionManager.getUserId()!!)
            } catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            } finally {
                _isLoading.value = false
            }
        }
    }

    /**
     * Descarga el PDF de factura y lo abre con una app externa.
     */
    fun openInvoicePdf(bookingId: String, context: Context) {
        viewModelScope.launch {
            _invoiceOpening.value = true
            _errorMessage.value = null
            try {
                val bytes = bookingRepository.getInvoicePdfBytes(bookingId)
                if (!InvoicePdfOpener.openPdfFromBytes(context, bookingId, bytes)) {
                    _errorMessage.value = "No hay ninguna aplicación para abrir el PDF"
                }
            } catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            } finally {
                _invoiceOpening.value = false
            }
        }
    }
}