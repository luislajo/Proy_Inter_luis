package com.example.intermodular.data.remote.mapper

import com.example.intermodular.data.remote.dto.IncidentDto
import com.example.intermodular.models.Incident
import java.time.OffsetDateTime

fun IncidentDto.toDomain(): Incident {
    val reported = runCatching { OffsetDateTime.parse(reported_at) }.getOrElse { OffsetDateTime.now() }
    val resolved = resolved_at?.let { runCatching { OffsetDateTime.parse(it) }.getOrNull() }
    return Incident(
        id = _id,
        roomId = room_id,
        type = type,
        severity = severity,
        description = description,
        status = status,
        reportedAt = reported,
        resolvedAt = resolved,
        assignedTo = assigned_to?.takeIf { it.isNotBlank() }
    )
}

