package com.example.intermodular.models

/**
 * Representa una habitación en el sistema del hotel.
 *
 * @property id Identificador único de la habitación.
 * @property type Tipo de habitación (ej. "Suite", "Estandar").
 * @property roomNumber Número de la habitación.
 * @property maxGuests Número máximo de huéspedes permitidos.
 * @property description Descripción detallada de la habitación.
 * @property mainImage URL de la imagen principal de la habitación.
 * @property pricePerNight Precio por noche en euros.
 * @property extraBed Indica si hay disponibilidad de cama extra.
 * @property crib Indica si hay disponibilidad de cuna.
 * @property offer Porcentaje de descuento aplicable (si existe), o null.
 * @property extras Lista de características extra incluidas (ej. "Wifi", "TV").
 * @property extraImages Lista de URLs de imágenes adicionales de la habitación.
 * @property isAvailable Indica si la habitación está disponible actualmente.
 * @property rate Calificación o valoración de la habitación.
 */
data class Room(
    val id: String,
    val type: String,
    val roomNumber: String,
    val maxGuests: Int,
    val description: String,
    val mainImage: String,
    val pricePerNight: Int,
    val extraBed: Boolean,
    val crib: Boolean,
    val offer: Double?,
    val extras: List<String>,
    val extraImages: List<String>,
    val status: String?,
    val isAvailable: Boolean,
    val rate: Int
)
