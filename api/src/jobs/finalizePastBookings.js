import { bookingDatabaseModel } from '../models/bookingModel.js';
import { roomDatabaseModel } from '../models/roomsModel.js';
import { roomStatusLogModel } from '../models/roomStatusLogModel.js';

/**
 * Marca como "Finalizada" las reservas en "Abierta" cuya fecha de check-out ya pasó.
 *
 * @returns {Promise<import('mongoose').mongodb.UpdateResult>}
 */
export async function finalizePastBookings() {
    const now = new Date();
    const toFinalize = await bookingDatabaseModel
        .find({ status: 'Abierta', checkOutDate: { $lt: now } })
        .select('_id room')
        .lean();

    if (toFinalize.length === 0) {
        return { acknowledged: true, matchedCount: 0, modifiedCount: 0, upsertedCount: 0, upsertedIds: {} };
    }

    const bookingIds = toFinalize.map((b) => b._id);
    const result = await bookingDatabaseModel.updateMany(
        { _id: { $in: bookingIds }, status: 'Abierta' },
        { $set: { status: 'Finalizada' } }
    );

    const roomIds = Array.from(new Set(toFinalize.map((b) => String(b.room)).filter(Boolean)));
    const rooms = await roomDatabaseModel.find({ _id: { $in: roomIds } }).select('_id status isAvailable').lean();

    const roomsById = new Map(rooms.map((r) => [String(r._id), r]));
    const statusLogs = [];

    for (const roomId of roomIds) {
        const room = roomsById.get(roomId);
        if (!room) continue;
        const currentStatus = room.status ?? (room.isAvailable ? 'available' : 'occupied');

        // default policy: do not override maintenance/blocked
        if (currentStatus === 'maintenance' || currentStatus === 'blocked') continue;

        if (currentStatus !== 'cleaning') {
            await roomDatabaseModel.updateOne(
                { _id: roomId },
                { $set: { status: 'cleaning', isAvailable: false } }
            );

            statusLogs.push({
                room_id: roomId,
                previous_status: currentStatus ?? null,
                new_status: 'cleaning',
                reason: 'auto-checkout (checkOutDate passed)',
                changed_by: null,
                changed_by_role: 'SYSTEM',
                changed_at: new Date()
            });
        }
    }

    if (statusLogs.length) {
        roomStatusLogModel.insertMany(statusLogs).catch((err) =>
            console.error('[rooms] Error al insertar room_status_log:', err)
        );
    }

    if (result.modifiedCount > 0) {
        console.log(
            `[bookings] ${result.modifiedCount} reserva(s) marcadas como Finalizada (check-out pasado).`
        );
    }
    return result;
}

/**
 * Si la reserva sigue "Abierta" y el check-out ya pasó, la persiste como "Finalizada".
 *
 * @param {import('mongoose').Document | null} booking
 * @returns {Promise<import('mongoose').Document | null>}
 */
export async function finalizeBookingDocumentIfPast(booking) {
    if (!booking) return booking;
    if (booking.status !== 'Abierta') return booking;
    if (booking.checkOutDate >= new Date()) return booking;
    booking.status = 'Finalizada';
    return booking.save();
}

const INTERVAL_MS = 60 * 60 * 1000;

/**
 * Ejecuta la finalización al arrancar y cada hora.
 */
export function startFinalizePastBookingsJob() {
    finalizePastBookings().catch((err) =>
        console.error('[bookings] Error al finalizar reservas pasadas:', err)
    );
    setInterval(() => {
        finalizePastBookings().catch((err) =>
            console.error('[bookings] Error al finalizar reservas pasadas:', err)
        );
    }, INTERVAL_MS);
}
