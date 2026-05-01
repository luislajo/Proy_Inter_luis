package com.example.intermodular.data.remote.mapper

import com.example.intermodular.data.remote.dto.RoomDto
import com.example.intermodular.models.Room

/**
 * Función de extensión que convierte un Data Transfer Object [RoomDto] proveniente de la API
 * en un modelo de dominio [Room], que es el objeto limpio utilizado por la interfaz de usuario
 * y los ViewModels.
 *
 * Mapea las propiedades devolviendo un objeto inmutable listo para la UI de Android.
 *
 * @return El modelo de dominio [Room].
 */
fun RoomDto.toDomain(): Room {
    val s = status?.trim()?.lowercase()
    val reservable = s != "maintenance" && s != "blocked"
    return Room(
        id = _id,
        type = type,
        roomNumber = roomNumber,
        maxGuests = maxGuests,
        description = description,
        mainImage = mainImage,
        pricePerNight = pricePerNight,
        extraBed = extraBed,
        crib = crib,
        offer = offer,
        extras = extras,
        extraImages = extraImages,
        status = s,
        // business rule: allow reserve unless maintenance/blocked
        isAvailable = reservable,
        rate = rate
    )
}
