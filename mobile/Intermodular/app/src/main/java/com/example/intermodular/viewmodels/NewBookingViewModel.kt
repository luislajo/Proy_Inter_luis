package com.example.intermodular.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.intermodular.data.remote.ApiErrorHandler
import com.example.intermodular.data.repository.BookingRepository
import com.example.intermodular.data.repository.RoomRepository
import com.example.intermodular.models.Room
import com.example.intermodular.models.RoomFilter
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneId

/**
 * ViewModel para la vista que permite reservar una habitación
 * @author Axel Zaragoci
 *
 * @property bookingRepository - Repositorio para obtener datos de reservas de la API
 * @property roomRepository - Repositorio para obtener datos de habitaciones de la API
 * @property startDate - Fecha de inicio marcada en los filtros de disponibilidad de [BookingViewModel]
 * @property endDate - Fecha de fin marcada en los filtros de disponibilidad de [BookingViewModel]
 * @property guests - Cantidad de huéspedes marcados en los filtros de disponibilidad de [BookingViewModel]
 */
class NewBookingViewModel(
    private val bookingRepository: BookingRepository,
    private val roomRepository: RoomRepository,
    private val roomId : String,
    private val startDate : Long,
    private val endDate : Long,
    private val guests : String
) : ViewModel() {

    // ==================== ESTADOS DE LA UI ====================
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

    /**
     * Indicador para navegar a la pantalla de pago con el ID de reserva
     */
    private val _navigateToPayment = MutableStateFlow<String?>(null)
    val navigateToPayment: StateFlow<String?> = _navigateToPayment

    /**
     * Indicador de si se ha creado la reserva
     */
    private val _bookingCreated = MutableStateFlow(false)
    val bookingCreated: StateFlow<Boolean> = _bookingCreated


    // ==================== FILTROS ====================
    /**
     * Cantidad de huéspedes seleccionados
     */
    private val _guests = MutableStateFlow(guests)
    val Guests : StateFlow<String> = _guests

    /**
     * Fecha de inicio seleccionada en milisegundos
     */
    private val _startDate = MutableStateFlow(startDate)
    val StartDate: StateFlow<Long?> = _startDate

    /**
     * Fecha de fin seleccionada en milisegundos
     */
    private val _endDate = MutableStateFlow(endDate)
    val EndDate : StateFlow<Long?> = _endDate

    /**
     * Habitación que se quiere reservar
     */
    private val _room = MutableStateFlow<Room?>(null)
    val Room : StateFlow<Room?> = _room

    /**
     * Precio total de la reserva
     */
    private val _totalPrice = MutableStateFlow<Double?>(null)
    val totalPrice : StateFlow<Double?> = _totalPrice

    // ==================== MÉTODOS PÚBLICOS ====================
    /**
     * Bloque de inicio del ViewModel
     * Se encarga de cargar los datos de la habitación a reservar
     */
    init {
        loadRoom()
    }

    /**
     * Carga el toda la información a mostrar
     *
     * Flujo principal:
     * 1. Carga una lista con todas las habitaciones
     * 2. Filtra la lista para obtener la que se quiere reservar
     * 3. Calcula el precio total de la reserva
     *
     *  En caso de error, muestra el error correspondiente según [ApiErrorHandler]
     */
    fun loadRoom() {
        viewModelScope.launch {
            _isLoading.value = true
            try {
                val rooms = roomRepository.getRooms(RoomFilter())
                _room.value = rooms.first{ it.id == roomId }
                calculateTotalPrice()
            }
            catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            }
            finally {
                _isLoading.value = false
            }
        }
    }

    /**
     * Devuelve la fecha de inicio seleccionada como [LocalDate]
     */
    fun startDateAsLocalDate(): LocalDate? =
        StartDate.value?.let {
            Instant.ofEpochMilli(it)
                .atZone(ZoneId.systemDefault())
                .toLocalDate()
        }

    /**
     * Devuelve la fecha de fin seleccionada como [LocalDate]
     */
    fun endDateAsLocalDate(): LocalDate? =
        EndDate.value?.let {
            Instant.ofEpochMilli(it)
                .atZone(ZoneId.systemDefault())
                .toLocalDate()
        }

    /**
     * Actualiza la fecha de inicio y recalcula el precio de manera acorde
     *
     * @param date - Fecha seleccionada en milisegundos
     */
    fun onStartDateChange(date: Long?) {
        date?.let { _startDate.value = it }
        calculateTotalPrice()
    }

    /**
     * Actualiza la fecha de fin y recalcula el precio de manera acorde
     *
     * @param date - Fecha seleccionada en milisegundos
     */
    fun onEndDateChange(date: Long?) {
        date?.let { _endDate.value = it }
        calculateTotalPrice()
    }

    /**
     * Actualiza la cantidad de huéspedes
     *
     * @param value - Cantidad de huéspedes seleccionados como [String]
     */
    fun onGuestsChange(value: String) {
        _guests.value = value
    }

    /**
     * Crea una nueva reseña
     *
     * Flujo principal:
     * 1. Se intenta crear la nueva reserva
     * 2. Se simula un proceso de pago
     * 3. Se indica que se ha creado la reserva
     *
     * En caso de error se muestra el código acorde al [ApiErrorHandler]
     */
    fun createBooking() {
        viewModelScope.launch {
            try {
                _isLoading.value = true

                val booking = bookingRepository.createBooking(
                    roomId = roomId,
                    checkIn = _startDate.value,
                    checkOut = _endDate.value,
                    guests = _guests.value.toInt()
                )

                _bookingCreated.value = true
                _navigateToPayment.value = booking.id

            } catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            } finally {
                _isLoading.value = false
            }
        }
    }

    /**
     * Resetea el evento de navegación de pago
     */
    fun onPaymentNavigated() {
        _navigateToPayment.value = null
    }

    // ==================== MÉTODOS PRIVADOS ====================

    /**
     * Calcula el precio total de la reserva
     *
     * Flujo principal:
     * 1. Calcula la cantidad de noches
     * 2. Calcula el coste de la reserva
     * 3. Si hay oferta, descuenta del coste la parte correspondiente
     *
     * En caso de no haber habitación, fecha de inicio, fecha de fin o de que la cantidad de noches sea 0 o negativa, para el cálculo del precio para no causar un error
     */
    private fun calculateTotalPrice() {
        val room = _room.value ?: return
        val start = startDateAsLocalDate() ?: return
        val end = endDateAsLocalDate() ?: return

        val nights = java.time.temporal.ChronoUnit.DAYS.between(start, end).toInt()
        if (nights <= 0) {
            _totalPrice.value = 0.0
            return
        }

        val base = nights * room.pricePerNight
        val discount = room.offer?.let { base * (it / 100) } ?: 0.0
        _totalPrice.value = (base - discount)
    }
}