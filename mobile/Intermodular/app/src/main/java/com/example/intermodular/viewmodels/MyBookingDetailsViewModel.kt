package com.example.intermodular.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.intermodular.data.remote.ApiErrorHandler
import com.example.intermodular.data.repository.BookingRepository
import com.example.intermodular.data.repository.ReviewRepository
import com.example.intermodular.data.repository.RoomRepository
import com.example.intermodular.models.Booking
import com.example.intermodular.models.Review
import com.example.intermodular.models.Room
import com.example.intermodular.models.RoomFilter
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneOffset
import kotlin.let

/**
 * ViewModel para la vista que permite ver detalles, modificar y cancelar una reserva ya existente. Además, permite agregar una reseña
 * @author Axel Zaragoci
 *
 * @property bookingId - ID de la reserva sobre la que se van a aplicar las funciones
 * @property bookingRepository - Repositorio para obtener datos de reservas de la API
 * @property roomRepository - Repositorio para obtener datos de habitaciones de la API
 * @property reviewRepository - Repositorio para crear una reseña
 */
class MyBookingDetailsViewModel (
    private val bookingId : String,
    private val bookingRepository: BookingRepository,
    private val roomRepository: RoomRepository,
    private val reviewRepository: ReviewRepository
) : ViewModel() {

    // ==================== ESTADOS DE LA UI ====================
    /**
     * Reserva que se ha de mostrar
     */
    private val _booking = MutableStateFlow<Booking?>(null)
    val booking: StateFlow<Booking?> = _booking

    /**
     * Habitación reservada
     */
    private val _room = MutableStateFlow<Room?>(null)
    val room : StateFlow<Room?> = _room

    private val _reviewCreated = MutableStateFlow<Boolean>(false)
    val reviewCreated : StateFlow<Boolean> = _reviewCreated

    /**
     * Calificación de la reseña
     */
    private val _reviewRating = MutableStateFlow(0)
    val reviewRating: StateFlow<Int> = _reviewRating

    /**
     * Descripción de la reseña
     */
    private val _reviewDescription = MutableStateFlow("")
    val reviewDescription: StateFlow<String> = _reviewDescription

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

    // ==================== MÉTODOS PÚBLICOS ====================
    /**
     * Bloque de inicio del ViewModel
     * Se encarga de cargar los datos de la habitación y la reserva
     */
    init {
        loadBooking()
    }

    /**
     * Carga la información de la habitación y de la reserva
     *
     * Flujo principal:
     * 1. Obtiene la reserva por el ID
     * 2. Obtiene toda la lista de habitaciones
     * 3. Saca la habitación reservada de la lista
     * 4. Carga las reseñas y determina si ya se ha creado una para esta reserva
     *
     * Errores lanzados:
     * - En caso de no encontrar la reserva
     * - En caso de no encontrar la habitación
     * Al ocurrir un error se muestra un mensaje descriptivo de [ApiErrorHandler]
     */
    fun loadBooking() {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            try {
                _booking.value = bookingRepository.getBookingById(bookingId)
                if (_booking.value == null) throw Exception("No se pudo conseguir la reserva")
                val rooms = roomRepository.getRooms(RoomFilter())

                val found = rooms.firstOrNull { it.id == _booking.value?.roomId }
                if (found != null) {
                    _room.value = found
                }
                else {
                    throw Exception("No se encontró la habitación")
                }

                val roomReviews = reviewRepository.getReviewsByRoom(_booking.value?.roomId!!);
                roomReviews.forEach{
                    if (it.bookingId == _booking.value?.id) {
                        _reviewCreated.value = true
                    }
                }
            } catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            } finally {
                _isLoading.value = false
            }
        }
    }

    /**
     * Devuelve la fecha de inicio de la reserva en milisegundos
     */
    fun checkInDateToMilliseconds(): Long? {
        return _booking.value?.checkInDate?.let { localDateToUtcMillis(it) }
    }

    /**
     * Devuelve la fecha de fin de la reserva en milisegundos
     */
    fun checkOutDateToMilliseconds(): Long? {
        return _booking.value?.checkOutDate?.let { localDateToUtcMillis(it) }
    }

    /**
     * Actualiza la fecha de inicio
     *
     * @param newMillis - Fecha nueva de inicio en milisegundos
     */
    fun onStartDateChange(newMillis: Long?) {
        if (newMillis != null) {
            val newDate = utcMillisToLocalDate(newMillis)
            _booking.value = _booking.value?.copy(checkInDate = newDate)
            calculateTotalPrice()
        }
    }

    /**
     * Actualiza la fecha de fin
     *
     * @param newMillis - Fecha nueva de fin en milisegundos
     */
    fun onEndDateChange(newMillis: Long?) {
        if (newMillis != null) {
            val newDate = utcMillisToLocalDate(newMillis)
            _booking.value = _booking.value?.copy(checkOutDate = newDate)
            calculateTotalPrice()
        }
    }

    /**
     * Actualiza la cantidad de huéspedes
     *
     * @param guests - Cantidad de huéspedes seleccionados como [String]
     */
    fun onGuestsChange(guests: String) {
        val intGuests = guests.toIntOrNull()
        if (intGuests != null && intGuests > 0) {
            _booking.value = _booking.value?.copy(guests = intGuests)
        }
    }

    /**
     * Actualiza la calificación de la reseña
     *
     * @param rating - Calificación seleccionada
     */
    fun onRatingChange(rating: Int) {
        _reviewRating.value = rating
    }

    /**
     * Actualiza la descripción de la reseña
     *
     * @param description - Descripción escrita
     */
    fun onReviewDescriptionChange(description: String) {
        _reviewDescription.value = description
    }

    /**
     * Actualiza los datos de la reserva
     *
     * Flujo principal:
     * 1. Verifica que los datos sean válidos
     * 2. Actualiza la reserva en la API
     * 3. Si ha cambiado el precio total, simula un pago
     *
     * En caso de error muestra un mensaje descriptivo o el acorde a [ApiErrorHandler]
     */
    fun updateBooking() {
        val currentBooking = _booking.value ?: run {
            _errorMessage.value = "No hay datos de reserva para actualizar"
            return
        }

        val startMillis = checkInDateToMilliseconds() ?: run {
            _errorMessage.value = "Fecha de entrada no válida"
            return
        }

        val endMillis = checkOutDateToMilliseconds() ?: run {
            _errorMessage.value = "Fecha de salida no válida"
            return
        }

        if (currentBooking.guests <= 0) {
            _errorMessage.value = "El número de huéspedes debe ser mayor que cero"
            return
        }

        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            try {
                val updated = bookingRepository.updateBooking(
                    bookingId = currentBooking.id,
                    checkIn = startMillis,
                    checkOut = endMillis,
                    guests = currentBooking.guests
                )
                if (_booking.value?.totalPrice != updated.totalPrice) {
                     _navigateToPayment.value = updated.id
                }

                _booking.value = updated
            } catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            } finally {
                _isLoading.value = false
            }
        }
    }

    /**
     * Cancela la reserva
     *
     * Flujo principal:
     * 1. Cancela la reserva actual en la API
     * 2. Cambia la reserva actual a una igual con el estado "Cancelada"
     *
     * En caso de error muestra un mensaje descriptivo o el acorde a [ApiErrorHandler]
     */
    fun cancelBooking() {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            try {
                bookingRepository.cancelBooking(bookingId)
                _booking.value = _booking.value?.copy(status = "Cancelada")
            } catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            } finally {
                _isLoading.value = false
            }
        }
    }

    /**
     * Crea una nueva reseña
     *
     * Flujo principal:
     * 1. Valida la existencia de los datos
     * 2. Lanza corrutina para la creación de la reseña en la API
     *
     * En caso de error, muestra el error con un mensaje acorde a [ApiErrorHandler]
     */
    fun createReview() {
        val currentBooking = _booking.value ?: run {
            _errorMessage.value = "No hay datos de reserva"
            return
        }

        val roomId = currentBooking.roomId
        val rating = _reviewRating.value
        val description = _reviewDescription.value

        if (rating <= 0) {
            _errorMessage.value = "Debes seleccionar una calificación"
            return
        }

        if (description.isBlank()) {
            _errorMessage.value = "Debes escribir una descripción"
            return
        }

        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            try {
                reviewRepository.createReview(
                    roomId = roomId,
                    bookingId = bookingId,
                    rating = rating,
                    description = description
                )

                _reviewCreated.value = true;
            } catch (e: Exception) {
                _errorMessage.value = ApiErrorHandler.getErrorMessage(e)
            } finally {
                _isLoading.value = false
            }
        }
    }

    /**
     * Resetea la directiva de navegación al pago
     */
    fun onPaymentNavigated() {
        _navigateToPayment.value = null
    }

    // ==================== MÉTODOS PRIVADOS ====================

    /**
     * Devuelve la hora en zona UTC en milisegundos
     *
     * @param date - Fecha a transformar en [LocalDate]
     * @return [Long] - Milisegundos respectivos a la fecha
     */
    private fun localDateToUtcMillis(date: LocalDate): Long {
        return date.atStartOfDay()
            .toInstant(ZoneOffset.UTC)
            .toEpochMilli()
    }

    /**
     * Devuelve la hora en zona UTC en formato [LocalDate]
     *
     * @param millis - Milisegundos respectivos a una fecha
     * @return [LocalDate] - Fecha en formato [LocalDate] respectiva a los milisegundos
     */
    private fun utcMillisToLocalDate(millis: Long): LocalDate {
        return Instant.ofEpochMilli(millis)
            .atZone(ZoneOffset.UTC)
            .toLocalDate()
    }

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
        val start = _booking.value?.checkInDate ?: return
        val end = _booking.value?.checkOutDate ?: return

        val nights = java.time.temporal.ChronoUnit.DAYS.between(start, end).toInt()
        if (nights <= 0) {
            _booking.value = _booking.value?.copy(totalPrice = 0.0)
            return
        }

        val base = nights * room.pricePerNight
        val discount = room.offer?.let { base * (it / 100) } ?: 0.0
        _booking.value = _booking.value?.copy(totalPrice = (base - discount))
    }
}