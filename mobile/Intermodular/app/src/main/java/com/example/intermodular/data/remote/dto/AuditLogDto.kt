package com.example.intermodular.data.remote.dto

/**
 * Data class representing an audit log entry from the API.
 */
data class AuditLogDto(
    val _id: String,
    val entity_type: String,
    val entity_id: String,
    val action: String,
    val actor_id: String,
    val actor_type: String,
    val timestamp: String
)
