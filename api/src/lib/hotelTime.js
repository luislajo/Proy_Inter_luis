/**
 * Fecha y hora del hotel (por defecto Europe/Madrid).
 */

const HOTEL_TZ = process.env.HOTEL_TIMEZONE ?? "Europe/Madrid";

/**
 * @param {Date} [date]
 * @returns {string} YYYY-MM-DD en zona del hotel
 */
export function getHotelCalendarDate(date = new Date()) {
    return new Intl.DateTimeFormat("en-CA", {
        timeZone: HOTEL_TZ,
        year: "numeric",
        month: "2-digit",
        day: "2-digit"
    }).format(date);
}

/**
 * @param {Date} [date]
 * @returns {number} Hora 0–23 en zona del hotel
 */
export function getHotelHour(date = new Date()) {
    const hour = new Intl.DateTimeFormat("en-GB", {
        timeZone: HOTEL_TZ,
        hour: "numeric",
        hour12: false
    }).format(date);
    return parseInt(hour, 10);
}

/**
 * @param {Date|string|number} dateLike
 * @returns {string}
 */
export function getHotelCalendarDateFrom(dateLike) {
    return getHotelCalendarDate(new Date(dateLike));
}
