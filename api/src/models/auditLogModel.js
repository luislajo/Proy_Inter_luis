import { Schema, Types, model } from 'mongoose';

/**
 * @typedef {Object} AuditLog
 * 
 * @property {import('mongoose').Types.ObjectId} _id
 * @property {"booking"|"room"} entity_type - Tipo de entidad afectada
 * @property {import('mongoose').Types.ObjectId} entity_id - ID de la reserva o habitación
 * @property {"CREATE"|"UPDATE"|"CANCEL"|"DELETE"} action - Acción realizada
 * @property {import('mongoose').Types.ObjectId} actor_id - ID del usuario que realiza la acción
 * @property {"Admin"|"Trabajador"|"Usuario"} actor_type - Rol del actor
 * @property {Object|null} previous_state - Estado anterior (null en CREATE)
 * @property {Object|null} new_state - Estado posterior (null en DELETE)
 * @property {Date} timestamp - Fecha y hora de la acción
 * 
 * @description Registro inmutable de auditoría para reservas y habitaciones
 */
const auditLogSchema = new Schema({
    entity_type: {
        type: String,
        enum: ['booking', 'room'],
        required: true
    },
    entity_id: {
        type: Types.ObjectId,
        required: true
    },
    action: {
        type: String,
        enum: ['CREATE', 'UPDATE', 'CANCEL', 'DELETE'],
        required: true
    },
    actor_id: {
        type: Types.ObjectId,
        ref: 'user',
        required: true
    },
    actor_type: {
        type: String,
        enum: ['Admin', 'Trabajador', 'Usuario'],
        required: true
    },
    previous_state: {
        type: Schema.Types.Mixed,
        default: null
    },
    new_state: {
        type: Schema.Types.Mixed,
        default: null
    },
    timestamp: {
        type: Date,
        default: Date.now
    }
});

// Índices para consultas eficientes
auditLogSchema.index({ entity_type: 1, entity_id: 1 });
auditLogSchema.index({ timestamp: -1 });

export const auditLogModel = model('audit_log', auditLogSchema);
