package com.example.intermodular.data.remote.dto

/**
 * Objeto de Transferencia de Datos (DTO) que representa una Habitación (Room) recibida desde la API.
 * 
 * Contiene todos los campos crudos devueltos por el backend antes de ser mapeados al modelo de dominio.
 *
 * @property _id Identificador único devuelto por MongoDB.
 * @property type Tipo de la habitación (ej. "doble", "suite").
 * @property roomNumber Número o identificador físico de la habitación.
 * @property maxGuests Número máximo de huéspedes permitidos.
 * @property description Descripción de la habitación.
 * @property mainImage URL o ruta de la imagen principal.
 * @property pricePerNight Precio por noche.
 * @property extraBed Indica si dispone de cama supletoria.
 * @property crib Indica si dispone de cuna.
 * @property offer Descuento o precio de oferta (si aplica), puede ser null.
 * @property extras Lista de comodidades adicionales (ej. "WIFI", "TV").
 * @property extraImages Lista de imágenes secundarias de la habitación.
 * @property isAvailable Indica si la habitación está disponible para ser reservada.
 * @property rate Puntuación media o categoría de la habitación.
 */
data class RoomDto(
    val _id: String,
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
    val status: String? = null,
    val isAvailable: Boolean,
    val rate: Int
)
