package com.example.intermodular.data.remote.dto

data class IncidentDto(
    val _id: String,
    val room_id: String,
    val type: String,
    val severity: String,
    val description: String,
    val status: String,
    val reported_at: String,
    val resolved_at: String? = null
)

data class IncidentsResponseDto(
    val items: List<IncidentDto> = emptyList()
)

