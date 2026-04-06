package com.example.intermodular.models

import java.time.LocalDate

/**
 * Modelo del domino para almacenar los datos de una reserva y utilizarlos en la aplicación
 * @author Axel Zaragoci
 *
 * @param id - Identificador único de la reserva
 * @param roomId - Identificador único de la habitación reservada
 * @param clientId - Identificador único del cliente que ha realizado la reserva
 * @param checkInDate - Fecha de inicio de la reserva
 * @param checkOutDate - Fecha de fin de la reserva
 * @param payDate - Fecha en la que se realizó el pago y se creó la reserva (ocurren a la vez)
 * @param totalPrice - Precio total pagado por la reserva
 * @param pricePerNight - Precio por noche pagado por la reserva
 * @param offer - Porcentaje de descuento aplicado a los precios anteriores
 * @param status - Estado actual: "Abierta", "Finalizada" o "Cancelada"
 * @param guests - Cantidad de huéspedes para la reserva
 * @param totalNights - Cantidad total de noches de la reserva
 */
data class Booking(
    val id: String,
    val roomId: String,
    val clientId: String,
    val checkInDate: LocalDate,
    val checkOutDate: LocalDate,
    val payDate: LocalDate,
    val totalPrice: Double,
    val pricePerNight: Double,
    val offer: Int,
    val status: String,
    val guests: Int,
    val totalNights: Int
)