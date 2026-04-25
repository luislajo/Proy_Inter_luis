package com.example.intermodular.data.remote.dto

import com.squareup.moshi.Json
import java.time.Instant

/**
 * Clase que almacena la plantilla de datos a recibir por la API con la información de las reservas
 * @author Axel Zaragoci
 *
 * @param _id - Identificador único de la reserva
 * @param room - Identificador único de la habitación reservada
 * @param client - Identificador único del cliente que ha realizado la reserva
 * @param checkInDate - Fecha y hora de inicio de la reserva
 * @param checkOutDate - Fecha y hora de fin de la reserva
 * @param payDate - Fecha y hora en la que se realizó el pago y se creó la reserva (ocurren a la vez)
 * @param totalPrice - Precio total pagado por la reserva
 * @param pricePerNight - Precio por noche pagado por la reserva
 * @param offer - Porcentaje de descuento aplicado a los precios anteriores
 * @param status - Estado actual: "Abierta", "Finalizada" o "Cancelada"
 * @param guests - Cantidad de huéspedes para la reserva
 * @param totalNights - Cantidad total de noches de la reserva
 */
data class BookingDto (
    val _id : String,
    val room: String,
    val client: String,
    val checkInDate : Instant,
    val checkOutDate: Instant,
    val payDate: Instant,
    val totalPrice: Double,
    val pricePerNight: Double,
    val offer: Int,
    val status: String,
    val guests: Int,
    val totalNights: Int,
    @Json(name = "invoice_number") val invoice_number: String? = null
)