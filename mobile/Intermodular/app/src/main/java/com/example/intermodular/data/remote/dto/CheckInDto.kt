package com.example.intermodular.data.remote.dto

/**
 * Petición de validación del código de check-in enviado por correo.
 */
data class CheckInRequestDto(
    val code: String
)

/**
 * Respuesta del endpoint POST /booking/:id/check-in
 */
data class CheckInResponseDto(
    val message: String,
    val booking: BookingDto
)
