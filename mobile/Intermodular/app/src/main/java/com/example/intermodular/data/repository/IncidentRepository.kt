package com.example.intermodular.data.repository

import com.example.intermodular.data.remote.ApiService
import com.example.intermodular.data.remote.mapper.toDomain
import com.example.intermodular.models.Incident

class IncidentRepository(
    private val api: ApiService
) {
    suspend fun createIncident(roomId: String, type: String, severity: String, description: String) {
        api.createIncident(
            roomId,
            mapOf(
                "type" to type,
                "severity" to severity,
                "description" to description
            )
        )
    }

    suspend fun getMyIncidents(roomId: String): List<Incident> {
        return api.getMyIncidentsByRoom(roomId).items.map { it.toDomain() }
    }
}

