import mongoose from "mongoose";
import { bookingDatabaseModel } from "../models/bookingModel.js";
import { roomDatabaseModel } from "../models/roomsModel.js";

/**
 * Reserva vigente: Abierta y el instante actual está entre check-in y check-out.
 *
 * @param {Date} now
 */
function activeBookingFilter(now) {
  return {
    status: "Abierta",
    checkInDate: { $lte: now },
    checkOutDate: { $gt: now }
  };
}

/**
 * Pone habitaciones en `occupied` si tienen estancia activa.
 * Si estaban `occupied` sin estancia activa (checkout ya pasado, etc.) → `cleaning`
 * (no las pasa a disponible; eso lo hace personal tras limpiar).
 * No modifica `maintenance`, `blocked` ni habitaciones ya en `cleaning`.
 *
 * @returns {Promise<{ occupied: number, toCleaning: number }>}
 */
export async function syncRoomOccupancyFromBookings() {
  const now = new Date();

  const activeRows = await bookingDatabaseModel
    .find(activeBookingFilter(now))
    .select("room")
    .lean();

  const occupiedByBooking = [
    ...new Set(activeRows.map((b) => String(b.room)).filter(Boolean))
  ];

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

  // Solo liberamos ocupación → limpieza (no tocar `cleaning`: esperan al personal).
  const releaseFilter = { status: "occupied" };
  if (occupiedObjectIds.length > 0) {
    releaseFilter._id = { $nin: occupiedObjectIds };
  }

  const releaseRes = await roomDatabaseModel.updateMany(releaseFilter, {
    $set: { status: "cleaning", isAvailable: false }
  });
  toCleaning = releaseRes.modifiedCount ?? 0;

  return { occupied, toCleaning };
}

const INTERVAL_MS = Number(process.env.ROOM_OCCUPANCY_SYNC_MS ?? 15 * 60 * 1000);

/**
 * Ejecuta al arrancar y de forma periódica (por defecto cada 15 min).
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
