import { bookingDatabaseModel } from "../models/bookingModel.js";
import {
    generateCheckInCode,
    isCheckInCodeSendWindowOpen,
    isCheckInDayToday
} from "./checkInCode.js";

/**
 * Genera el código de check-in si es el día de entrada y ya son las 11:00.
 * @param {import('mongoose').Document|null} booking
 * @returns {Promise<import('mongoose').Document|null>}
 */
export async function ensureCheckInCodeForBooking(booking) {
    if (!booking || booking.status !== "Abierta") return booking;
    if (booking.checkInCodeSentAt) return booking;
    if (!isCheckInDayToday(booking) || !isCheckInCodeSendWindowOpen()) return booking;

    booking.checkInCode = generateCheckInCode();
    booking.checkInCodeSentAt = new Date();
    await booking.save();
    console.log(`[check-in] Código generado al consultar reserva ${booking._id}`);
    return booking;
}

/**
 * @param {string} bookingId
 * @returns {Promise<import('mongoose').Document|null>}
 */
export async function ensureCheckInCodeForBookingId(bookingId) {
    const booking = await bookingDatabaseModel.findById(bookingId);
    if (!booking) return null;
    return ensureCheckInCodeForBooking(booking);
}
