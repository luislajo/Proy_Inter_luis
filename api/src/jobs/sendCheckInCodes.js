import { bookingDatabaseModel } from "../models/bookingModel.js";
import { isCheckInCodeSendWindowOpen, isCheckInDayToday } from "../lib/checkInCode.js";
import { ensureCheckInCodeForBooking } from "../lib/bookingStay.js";

/**
 * Genera el código de 5 dígitos para reservas con entrada hoy,
 * a partir de las 11:00 (hora del hotel). El cliente lo ve en la app.
 *
 * @returns {Promise<{ issued: number }>}
 */
export async function issueDueCheckInCodes() {
    if (!isCheckInCodeSendWindowOpen()) {
        return { issued: 0 };
    }

    const candidates = await bookingDatabaseModel
        .find({
            status: "Abierta",
            checkInCodeSentAt: null
        })
        .lean();

    let issued = 0;

    for (const row of candidates) {
        if (!isCheckInDayToday(row)) continue;

        const booking = await bookingDatabaseModel.findById(row._id);
        if (!booking || booking.checkInCodeSentAt) continue;

        await ensureCheckInCodeForBooking(booking);

        issued += 1;
        console.log(`[check-in] Código generado para reserva ${booking._id}`);
    }

    return { issued };
}

const INTERVAL_MS = Number(process.env.CHECKIN_CODE_JOB_MS ?? 60 * 1000);

/**
 * Revisa cada minuto si ya son las 11:00 y hay reservas de entrada hoy sin código.
 */
export function startSendCheckInCodesJob() {
    const tick = () => {
        issueDueCheckInCodes().catch((err) =>
            console.error("[check-in] Error en job de códigos:", err)
        );
    };
    tick();
    setInterval(tick, INTERVAL_MS);
}
