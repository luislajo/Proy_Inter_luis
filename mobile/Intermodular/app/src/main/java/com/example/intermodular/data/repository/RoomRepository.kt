package com.example.intermodular.data.repository

import com.example.intermodular.data.remote.ApiService
import com.example.intermodular.data.remote.mapper.toDomain
import com.example.intermodular.models.Room

import com.example.intermodular.models.RoomFilter

/**
 * Repositorio encargado de gestionar la lógica de acceso a datos para las Habitaciones ([Room]).
 * 
 * Actúa como intermediario entre los ViewModels y la fuente de datos remota ([ApiService]).
 *
 * @property api El servicio Retrofit utilizado para hacer las llamadas de red.
 */
class RoomRepository(
    private val api: ApiService
) {

    /**
     * Obtiene una lista de habitaciones desde la API, aplicando opcionalmente una serie de filtros.
     * 
     * Las habitaciones devueltas por la red como [RoomDto] son automáticamente mapeadas 
     * a objetos de dominio [Room] utilizando `toDomain()`.
     *
     * @param filter Objeto [RoomFilter] que contiene todos los criterios de búsqueda (precio, tipo, disponibilidad, etc.).
     * @return Una lista inmutable de modelos de dominio [Room] que cumplen los filtros.
     * @throws Exception Si ocurre algún error de red o durante el parseo de la respuesta.
     */
    suspend fun getRooms(filter: RoomFilter = RoomFilter()): List<Room> {
        return api.getRooms(
            type = filter.type,
            // Do not use server isAvailable filter: occupied/cleaning still reservable per business rule
            isAvailable = null,
            minPrice = filter.minPrice,
            maxPrice = filter.maxPrice,
            guests = filter.guests,
            hasExtraBed = filter.hasExtraBed,
            hasCrib = filter.hasCrib,
            hasOffer = filter.hasOffer,
            extras = filter.extras?.joinToString(","),
            sortBy = filter.sortBy,
            sortOrder = filter.sortOrder
        ).items
            .map { it.toDomain() }
            // hide only not-reservable statuses
            .filter { r -> r.status != "maintenance" && r.status != "blocked" }
    }

    suspend fun getRoomById(roomId: String): Room {
        return api.getRoomById(roomId).toDomain()
    }
}
