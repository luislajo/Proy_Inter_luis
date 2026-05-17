import mongoose from "mongoose";
import { bookingDatabaseModel } from "../models/bookingModel.js";
import { roomDatabaseModel } from "../models/roomsModel.js";

/**
 * Inicio y fin del día calendario (hora local del servidor).
 * @param {Date} [date]
 * @returns {{ dayStart: Date, dayEnd: Date }}
 */
export function getLocalDayBounds(date = new Date()) {
  const dayStart = new Date(date);
  dayStart.setHours(0, 0, 0, 0);
  const dayEnd = new Date(date);
  dayEnd.setHours(23, 59, 59, 999);
  return { dayStart, dayEnd };
}

/**
 * Reservas «Abierta» cuya estancia solapa el día de hoy (aunque el check-in sea más tarde).
 * Ej.: reserva 16/5 → 17/5 marca la habitación ocupada todo el 16/5.
 *
 * @param {Date} [now]
 * @returns {object} Filtro MongoDB
 */
export function bookingOverlapsTodayFilter(now = new Date()) {
  const { dayStart, dayEnd } = getLocalDayBounds(now);
  return {
    status: "Abierta",
    checkInDate: { $lte: dayEnd },
    checkOutDate: { $gt: dayStart }
  };
}

/**
 * Reserva con estancia activa en este instante (check-in ya pasó y check-out no).
 *
 * @param {Date} now
 * @returns {object}
 */
export function activeBookingNowFilter(now) {
  return {
    status: "Abierta",
    checkInDate: { $lte: now },
    checkOutDate: { $gt: now }
  };
}

/**
 * Habitaciones que deben estar `occupied`: unión de reservas que solapan hoy
 * y reservas activas ahora mismo.
 *
 * @param {Date} [now]
 * @returns {Promise<string[]>} IDs de habitación (string)
 */
export async function findRoomIdsWithOccupyingBookings(now = new Date()) {
  const [todayRows, activeNowRows] = await Promise.all([
    bookingDatabaseModel.find(bookingOverlapsTodayFilter(now)).select("room").lean(),
    bookingDatabaseModel.find(activeBookingNowFilter(now)).select("room").lean()
  ]);

  return [
    ...new Set(
      [...todayRows, ...activeNowRows]
        .map((b) => String(b.room))
        .filter(Boolean)
    )
  ];
}

/**
 * Pone habitaciones en `occupied` si tienen reserva abierta que ocupa el día de hoy
 * (o estancia activa en este momento).
 * Si estaban `occupied` sin reserva que lo justifique → `cleaning`.
 * No modifica `maintenance`, `blocked` ni habitaciones ya en `cleaning`.
 *
 * @returns {Promise<{ occupied: number, toCleaning: number, roomsWithBooking: number }>}
 */
export async function syncRoomOccupancyFromBookings() {
  const now = new Date();
  const occupiedByBooking = await findRoomIdsWithOccupyingBookings(now);

  const occupiedObjectIds = occupiedByBooking
    .filter((id) => mongoose.isValidObjectId(id))
    .map((id) => new mongoose.Types.ObjectId(id));

  let occupied = 0;
  let toCleaning = 0;

  for (const roomId of occupiedByBooking) {
    const res = await roomDatabaseModel.updateOne(
      {
        _id: roomId,
        status: { $nin: ["maintenance", "blocked"] }
      },
      { $set: { status: "occupied", isAvailable: false } }
    );
    if (res.modifiedCount > 0) occupied += 1;
  }

  const releaseFilter = { status: "occupied" };
  if (occupiedObjectIds.length > 0) {
    releaseFilter._id = { $nin: occupiedObjectIds };
  }

  const releaseRes = await roomDatabaseModel.updateMany(releaseFilter, {
    $set: { status: "cleaning", isAvailable: false }
  });
  toCleaning = releaseRes.modifiedCount ?? 0;

  if (occupied > 0 || toCleaning > 0) {
    console.log(
      `[rooms] Sync ocupación: ${occupiedByBooking.length} hab. con reserva hoy/activa, ` +
        `${occupied} → occupied, ${toCleaning} → cleaning`
    );
  }

  return { occupied, toCleaning, roomsWithBooking: occupiedByBooking.length };
}

const INTERVAL_MS = Number(process.env.ROOM_OCCUPANCY_SYNC_MS ?? 15 * 60 * 1000);

/**
 * Job periódico: al arrancar y cada 15 min (configurable con ROOM_OCCUPANCY_SYNC_MS).
 * Marca ocupadas las habitaciones con reserva el mismo día o estancia en curso.
 */
export function startRoomOccupancySyncJob() {
  syncRoomOccupancyFromBookings().catch((err) =>
    console.error("[rooms] Error sync ocupación:", err)
  );
  setInterval(() => {
    syncRoomOccupancyFromBookings().catch((err) =>
      console.error("[rooms] Error sync ocupación:", err)
    );
  }, INTERVAL_MS);
}
