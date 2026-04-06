import { auditLogModel } from '../models/auditLogModel.js';
import { bookingDatabaseModel } from '../models/bookingModel.js';
import { roomDatabaseModel } from '../models/roomsModel.js';

/**
 * Middleware de auditoría que intercepta PATCH y DELETE sobre reservas/habitaciones.
 * Captura el estado anterior del documento antes de la operación y,
 * tras la respuesta exitosa, inserta un registro inmutable en audit_logs.
 *
 * @param {"booking"|"room"} entityType - Tipo de entidad a auditar
 * @param {string} [paramName="id"] - Nombre del parámetro de ruta que contiene el ID
 * @returns {import('express').RequestHandler}
 */
export function auditMiddleware(entityType, paramName = 'id') {
    const Model = /** @type {any} */ (entityType === 'booking' ? bookingDatabaseModel : roomDatabaseModel);

    return async (req, res, next) => {
        try {
            // Obtener el ID de la entidad desde los parámetros de la ruta
            const entityId = req.params[paramName];
            if (!entityId) return next();

            // Guardar el estado anterior del documento
            const previousDoc = await Model.findById(entityId).lean();
            const auditPreviousState = previousDoc || null;

            // Determinar la acción según el método HTTP y la URL
            const auditAction = determineAction(req.method, req.originalUrl);

            // Obtener usuario del request (con casting manual para evitar TS errors locales)
            const reqUser = /** @type {any} */ (req).user || {};

            // Sobreescribir res.json para capturar la respuesta
            const originalJson = res.json.bind(res);
            res.json = (body) => {
                // Solo registrar si la respuesta es exitosa (2xx)
                if (res.statusCode >= 200 && res.statusCode < 300) {
                    const newState = auditAction === 'DELETE' ? null : body;

                    // Insertar el log de forma asíncrona (no bloquea la respuesta)
                    auditLogModel.create({
                        entity_type: entityType,
                        entity_id: entityId,
                        action: auditAction,
                        actor_id: reqUser.id,
                        actor_type: reqUser.rol,
                        previous_state: auditPreviousState,
                        new_state: newState,
                        timestamp: new Date()
                    }).catch(err => console.error('Error al crear audit log:', err));
                }

                return originalJson(body);
            };

            next();
        } catch (err) {
            console.error('Error en audit middleware:', err);
            next();
        }
    };
}

/**
 * Determina la acción de auditoría según el método HTTP y la URL
 * @param {string} method - Método HTTP (PATCH, DELETE, etc.)
 * @param {string} url - URL original de la petición
 * @returns {"UPDATE"|"CANCEL"|"DELETE"|"PAYMENT"}
 */
function determineAction(method, url) {
    if (method === 'DELETE') return 'DELETE';
    if (method === 'PATCH' && url.includes('/cancel')) return 'CANCEL';
    return 'UPDATE';
}
