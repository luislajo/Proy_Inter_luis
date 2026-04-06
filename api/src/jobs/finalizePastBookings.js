import { bookingDatabaseModel } from '../models/bookingModel.js';

/**
 * Marca como "Finalizada" las reservas en "Abierta" cuya fecha de check-out ya pasó.
 *
 * @returns {Promise<import('mongoose').mongodb.UpdateResult>}
 */
export async function finalizePastBookings() {
    const now = new Date();
    const result = await bookingDatabaseModel.updateMany(
        { status: 'Abierta', checkOutDate: { $lt: now } },
        { $set: { status: 'Finalizada' } }
    );
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
