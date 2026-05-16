package com.example.intermodular.data.remote.dto

data class IncidentDto(
    val _id: String,
    val room_id: String,
    val type: String,
    val severity: String,
    val description: String,
    val status: String,
    val reported_at: String,
    val resolved_at: String? = null,
    /** Presente cuando personal del hotel ha tomado la incidencia */
    val assigned_to: String? = null
)

data class IncidentsResponseDto(
    val items: List<IncidentDto> = emptyList()
)

