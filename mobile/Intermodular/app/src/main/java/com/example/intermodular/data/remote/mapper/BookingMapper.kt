package com.example.intermodular.data.remote.mapper


import com.example.intermodular.data.remote.dto.BookingDto
import com.example.intermodular.models.Booking
import java.time.ZoneOffset

/**
 * Función de ampliación para la clase [BookingDto] para transformarlo a objeto del dominio
 * Transforma los Instant a LocalDate usando UTC como zona horaria
 * @author Axel Zaragoci
 *
 * @return [Booking] - Objeto del dominio con todos los datos mapeados desde el DTO
 */
fun BookingDto.toDomain() : Booking {
    return Booking(
        id = _id,
        roomId = room,
        clientId = client,
        checkInDate = checkInDate
            .atZone(ZoneOffset.UTC)
            .toLocalDate(),
        checkOutDate = checkOutDate
            .atZone(ZoneOffset.UTC)
            .toLocalDate(),
        payDate = payDate
            .atZone(ZoneOffset.UTC)
            .toLocalDate(),
        totalPrice = totalPrice,
        pricePerNight = pricePerNight,
        offer = offer,
        status = status,
        guests = guests,
        totalNights = totalNights,
        invoiceNumber = invoice_number
    )
}