import { getHotelCalendarDate, getHotelCalendarDateFrom, getHotelHour } from "./hotelTime.js";

export const CHECKIN_CODE_SEND_HOUR = Number(process.env.CHECKIN_CODE_SEND_HOUR ?? 11);
export const CHECKIN_CODE_LENGTH = 5;

/**
 * @returns {string} Código numérico de 5 dígitos (sin leading zeros obligatorios en UI, guardado con pad)
 */
export function generateCheckInCode() {
    const n = Math.floor(Math.random() * 100000);
    return String(n).padStart(CHECKIN_CODE_LENGTH, "0");
}

/**
 * @param {Date} [now]
 * @returns {boolean}
 */
export function isCheckInCodeSendWindowOpen(now = new Date()) {
    return getHotelHour(now) >= CHECKIN_CODE_SEND_HOUR;
}

/**
 * El día calendario de entrada de la reserva es hoy (zona hotel).
 * @param {{ checkInDate: Date|string }} booking
 * @param {Date} [now]
 */
export function isCheckInDayToday(booking, now = new Date()) {
    return getHotelCalendarDateFrom(booking.checkInDate) === getHotelCalendarDate(now);
}

/**
 * El día calendario de salida de la reserva es hoy (zona hotel).
 * @param {{ checkOutDate: Date|string }} booking
 * @param {Date} [now]
 */
export function isCheckOutDayToday(booking, now = new Date()) {
    return getHotelCalendarDateFrom(booking.checkOutDate) === getHotelCalendarDate(now);
}
