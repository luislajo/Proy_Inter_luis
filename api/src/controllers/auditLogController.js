import { auditLogModel } from '../models/auditLogModel.js';
import { bookingDatabaseModel } from '../models/bookingModel.js';
import mongoose from 'mongoose';

/**
 * Obtener el historial de auditoría global
 * 
 * @async
 * @function getAllAuditLogs
 * 
 * @description
 * Devuelve todos los registros de auditoría del sistema ordenados cronológicamente (más recientes primero)
 * 
 * @param {import('express').Request} req
 * @param {import('express').Response} res
 * 
 * @returns {Promise} Respuesta HTTP con:
 * - 200 y el array de registros de auditoría
 * - 404 si no se encuentran registros
 * - 500 si ocurre un error del servidor
 */
export async function getAllAuditLogs(req, res) {
    try {
        const logs = await auditLogModel
            .find({})
            .sort({ timestamp: -1 })
            .lean();

        if (logs.length === 0) return res.status(404).json({ error: 'No se encontraron registros de auditoría' });

        return res.status(200).json(logs);
    } catch (error) {
        console.error('Error al obtener audit logs globales:', error);
        return res.status(500).json({ error: 'Error del servidor' });
    }
}

/**
 * Obtener el historial de auditoría de una reserva
 * 
 * @async
 * @function getAuditLogByBookingId
 * 
 * @description
 * Devuelve todos los registros de auditoría de una reserva ordenados cronológicamente (más recientes primero)
 * 
 * @param {import('express').Request} req
 * @param {import('express').Response} res
 * 
 * @returns {Promise} Respuesta HTTP con:
 * - 200 y el array de registros de auditoría
 * - 400 si el ID no es válido
 * - 404 si no se encuentran registros
 * - 500 si ocurre un error del servidor
 */
export async function getAuditLogByBookingId(req, res) {
    try {
        const { id } = req.params;
        if (!id) return res.status(400).json({ error: 'Se requiere ID de la reserva' });
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID válido' });

        const logs = await auditLogModel
            .find({ entity_type: 'booking', entity_id: id })
            .sort({ timestamp: -1 })
            .lean();

        if (logs.length === 0) return res.status(404).json({ error: 'No se encontraron registros de auditoría para esta reserva' });

        return res.status(200).json(logs);
    } catch (error) {
        console.error('Error al obtener audit log de reserva:', error);
        return res.status(500).json({ error: 'Error del servidor' });
    }
}

/**
 * Obtener el historial de auditoría de una habitación
 * 
 * @async
 * @function getAuditLogByRoomId
 * 
 * @description
 * Devuelve todos los registros de auditoría de una habitación ordenados cronológicamente (más recientes primero)
 * 
 * @param {import('express').Request} req
 * @param {import('express').Response} res
 * 
 * @returns {Promise} Respuesta HTTP con:
 * - 200 y el array de registros de auditoría
 * - 400 si el ID no es válido
 * - 404 si no se encuentran registros
 * - 500 si ocurre un error del servidor
 */
export async function getAuditLogByRoomId(req, res) {
    try {
        const { roomID } = req.params;
        if (!roomID) return res.status(400).json({ error: 'Se requiere ID de la habitación' });
        if (!mongoose.isValidObjectId(roomID)) return res.status(400).json({ error: 'No es un ID válido' });

        const logs = await auditLogModel
            .find({ entity_type: 'room', entity_id: roomID })
            .sort({ timestamp: -1 })
            .lean();

        if (logs.length === 0) return res.status(404).json({ error: 'No se encontraron registros de auditoría para esta habitación' });

        return res.status(200).json(logs);
    } catch (error) {
        console.error('Error al obtener audit log de habitación:', error);
        return res.status(500).json({ error: 'Error del servidor' });
    }
}

/**
 * Obtener el historial de auditoría de un usuario
 * 
 * @async
 * @function getAuditLogByUserId
 * 
 * @description
 * Devuelve todos los registros de auditoría de las reservas de un usuario
 * ordenados cronológicamente (más recientes primero)
 * 
 * @param {import('express').Request} req
 * @param {import('express').Response} res
 * 
 * @returns {Promise} Respuesta HTTP con:
 * - 200 y el array de registros de auditoría
 * - 400 si el ID no es válido
 * - 404 si no se encuentran registros
 * - 500 si ocurre un error del servidor
 */
export async function getAuditLogByUserId(req, res) {
    try {
        const { id } = req.params;
        if (!id) return res.status(400).json({ error: 'Se requiere ID del usuario' });
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID válido' });

        // 1. Obtener todas las reservas del usuario
        const userBookings = await bookingDatabaseModel.find({ client: id }).select('_id').lean();
        const bookingIds = userBookings.map(b => b._id);

        if (bookingIds.length === 0) {
            return res.status(200).json([]); // El usuario no tiene reservas, por lo tanto no tiene logs
        }

        // 2. Obtener los logs de auditoría para esas reservas
        const logs = await auditLogModel
            .find({ entity_type: 'booking', entity_id: { $in: bookingIds } })
            .sort({ timestamp: -1 })
            .lean();

        return res.status(200).json(logs);
    } catch (error) {
        console.error('Error al obtener audit log de usuario:', error);
        return res.status(500).json({ error: 'Error del servidor' });
    }
}
