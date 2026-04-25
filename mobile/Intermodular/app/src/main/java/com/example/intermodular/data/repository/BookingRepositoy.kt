package com.example.intermodular.data.repository

import com.example.intermodular.data.remote.ApiService
import com.example.intermodular.data.remote.auth.SessionManager
import com.example.intermodular.data.remote.dto.AuditLogDto
import com.example.intermodular.data.remote.dto.CreateBookingDto
import com.example.intermodular.data.remote.dto.UpdateBookingDto
import com.example.intermodular.data.remote.mapper.toDomain
import com.example.intermodular.models.Booking
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.json.JSONObject
import retrofit2.HttpException
import java.io.IOException

/**
 * Repositorio encargado de gestionar las operaciones relacionadas con las reservas
 *
 * @param api Servicio de API para realizar las peticiones HTTP
 *
 * @author Axel Zaragoci
 */
class BookingRepository(
    private val api: ApiService
) {

    /**
     * Obtiene todas las reservas existentes
     *
     * @return [List<Booking>] - Lista de objetos del domino con todas las reservas
     * @throws retrofit2.HttpException - Si hay un error en la petición
     * @throws IOException - Si hay error de red
     */
    suspend fun getBookings(): List<Booking> {
        return api.getBookings()
            .map { it.toDomain() }
    }


    /**
     * Obtiene una reserva específica por su ID
     *
     * @param id - ID de la reserva
     * @return [Booking] - Objeto del dominio con la información de la reserva
     * @throws retrofit2.HttpException - Si hay un error en la petición
     * @throws IOException - Si hay error de red
     */
    suspend fun getBookingById(id : String) : Booking {
        return api.getBookingById(id).toDomain()
    }

    /**
     * Descarga los bytes del PDF de factura vía Retrofit ([ApiService.getInvoicePdf] → [okhttp3.ResponseBody]).
     *
     * @throws IOException Con mensaje legible (incluye cuerpo JSON `{ "error": "..." }` del servidor si existe)
     */
    suspend fun getInvoicePdfBytes(bookingId: String): ByteArray = withContext(Dispatchers.IO) {
        try {
            api.getInvoicePdf(bookingId).use { body ->
                val bytes = body.bytes()
                if (bytes.isEmpty()) {
                    throw IOException("El servidor no devolvió el PDF")
                }
                bytes
            }
        } catch (e: HttpException) {
            val errText = e.response()?.errorBody()?.string().orEmpty()
            val apiMsg = runCatching {
                JSONObject(errText).optString("error", "").trim()
            }.getOrDefault("")
            val message = when {
                apiMsg.isNotBlank() -> apiMsg
                e.code() == 401 -> "No autorizado. Por favor, inicia sesión nuevamente"
                e.code() == 403 -> "Acceso prohibido"
                e.code() == 404 -> "Reserva no encontrada"
                else -> "Error del servidor (código ${e.code()})"
            }
            throw IOException(message)
        }
    }

    /**
     * Obtiene una lista de las reservas de un cliente
     *
     * @param id - ID del cliente
     * @return [List<Booking>] - Lista de objetos del dominio con la información de las reservas
     * @throws retrofit2.HttpException - Si hay un error en la petición
     * @throws IOException - Si hay error de red
     */
    suspend fun getBookingsByUserId(id: String) : List<Booking> {
        return api.getBookingsByUserId(id)
            .map { it.toDomain() }
    }

    /**
     * Obtiene el historial de auditoría de un usuario
     *
     * @param id - ID del cliente
     * @return [List<AuditLogDto>] - Lista de acciones auditadas
     * @throws retrofit2.HttpException - Si hay un error en la petición
     * @throws IOException - Si hay error de red
     */
    suspend fun getAuditLogsByUserId(id: String): List<AuditLogDto> {
        return api.getAuditLogsByUserId(id)
    }


    /**
     * Crea una nueva reserva para el usuario autenticado.
     *
     * @param roomId - Identificador de la habitación a reservar
     * @param checkIn - Fecha de entrada (en milisegundos)
     * @param checkOut - Fecha de salida (en milisegundos)
     * @param guests - Número de huéspedes
     * @return [Booking] - Objeto del dominio con la información de la reserva creada
     * @throws IllegalStateException - Si el usuario no está autenticado
     * @throws retrofit2.HttpException - Si hay un error en la petición HTTP
     * @throws IOException - Si hay un error de red
     */
    suspend fun createBooking(
        roomId: String,
        checkIn: Long,
        checkOut: Long,
        guests: Int
    ): Booking {

        val clientId = SessionManager.getUserId()
            ?: throw IllegalStateException("Usuario no autenticado")

        val dto = CreateBookingDto(
            client = clientId,
            room = roomId,
            checkInDate = checkIn,
            checkOutDate = checkOut,
            guests = guests
        )

        return api.createBooking(dto).toDomain()
    }


    /**
     * Registra el pago en la auditoría
     *
     * @param bookingId - ID de la reserva a registrar el pago
     * @return [Booking] - Objeto del dominio de la reserva actualizada
     */
    suspend fun payBooking(bookingId: String): Booking {
        return api.payBooking(bookingId).booking.toDomain()
    }


    /**
     * Cancela una reserva existente
     *
     * @param bookingId - ID de la reserva a cancelar
     * @return [Booking] - Objeto del dominio con la información de la reserva cancelada
     * @throws retrofit2.HttpException - Si hay un error en la petición HTTP
     * @throws IOException - Si hay un error de red
     */
    suspend fun cancelBooking(bookingId: String): Booking {
        return api.cancelBooking(bookingId).toDomain()
    }


    /**
     * Edita una reserva existente
     *
     * @param bookingId - ID de la reserva a cancelar
     * @param checkIn - Fecha de entrada (en milisegundos)
     * @param checkOut - Fecha de salida (en milisegundos)
     * @param guests - Número de huéspedes
     * @return [Booking] - Objeto del dominio con la información de la reserva editada
     * @throws retrofit2.HttpException - Si hay un error en la petición HTTP
     * @throws IOException - Si hay un error de red
     */
    suspend fun updateBooking(
        bookingId: String,
        checkIn: Long,
        checkOut: Long,
        guests: Int
    ): Booking {
        val dto = UpdateBookingDto(
            checkInDate = checkIn,
            checkOutDate = checkOut,
            guests = guests
        )
        return api.updateBooking(bookingId, dto).toDomain()
    }

}
