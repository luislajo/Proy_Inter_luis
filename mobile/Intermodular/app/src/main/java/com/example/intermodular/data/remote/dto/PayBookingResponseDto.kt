package com.example.intermodular.data.remote.dto

/**
 * Respuesta de POST `booking/{id}/pay`: la API envía mensaje y la reserva en [booking].
 */
data class PayBookingResponseDto(
    val status: String,
    val booking: BookingDto
)
