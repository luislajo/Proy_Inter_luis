/**
 * @file CRUD y listado filtrado de habitaciones; integra validación con `RoomEntryData` y auditoría vía middleware en rutas.
 */
import { roomDatabaseModel, RoomEntryData } from "../models/roomsModel.js";
import { auditLogModel } from "../models/auditLogModel.js";
import { roomStatusLogModel } from "../models/roomStatusLogModel.js";

/**
 * Crea una nueva habitación.
 *
 * @async
 * @function createRoom
 * @param {import("express").Request} req - Request de Express (body con datos de la habitación).
 * @param {import("express").Response} res - Response de Express.
 * @returns {Promise<import("express").Response>} Respuesta HTTP con la habitación creada o un error.
 *
 * @example
 * // POST /rooms
 * // body: { type, roomNumber, maxGuests, description, mainImage, pricePerNight, ... }
 */
export const createRoom = async (req, res) => {
  try {
    const {
      type,
      roomNumber,
      maxGuests,
      description,
      mainImage,
      pricePerNight,
      extraBed,
      crib,
      offer,
      extras,
      extraImages,
    } = req.body;

    // Crea la entrada base con los campos obligatorios
    const entry = new RoomEntryData(
      type,
      roomNumber,
      maxGuests,
      description,
      mainImage,
      pricePerNight
    );

    // Completa campos opcionales con valores por defecto si no vienen en el body
    entry.completeRoomData(
      extraBed ?? false,
      crib ?? false,
      offer ?? 0,
      extras ?? [],
      extraImages ?? []
    );

    // Convierte a documento Mongoose y guarda
    const doc = entry.toDocument();
    const saved = await doc.save();

    // Registro de auditoría para la creación
    auditLogModel.create({
      entity_type: 'room',
      entity_id: saved._id,
      action: 'CREATE',
      actor_id: req.user.id,
      actor_type: req.user.rol,
      previous_state: null,
      new_state: saved.toJSON(),
      timestamp: new Date()
    }).catch(err => console.error('Error al crear audit log:', err));

    return res.status(200).json(saved);
  } catch (err) {
    // Error de clave duplicada (por ejemplo roomNumber unique)
    if (err?.code === 11000) {
      return res.status(409).json({ message: "roomNumber ya existe" });
    }
    // Validación / datos incorrectos
    return res.status(400).json({ message: err.message });
  }
};

/**
 * Obtiene todas las habitaciones.
 *
 * @async
 * @function getAllRooms
 * @param {import("express").Request} req - Request de Express.
 * @param {import("express").Response} res - Response de Express.
 * @returns {Promise<import("express").Response>} Lista de habitaciones o error.
 *
 * @example
 * // GET /rooms
 */
export const getAllRooms = async (req, res) => {
  try {
    const rooms = await roomDatabaseModel.find();
    return res.status(200).json(rooms);
  } catch (err) {
    return res.status(500).json({ message: err.message });
  }
};

/**
 * Obtiene una habitación por su ID.
 *
 * @async
 * @function getRoomById
 * @param {import("express").Request} req - Request de Express (params.id).
 * @param {import("express").Response} res - Response de Express.
 * @returns {Promise<import("express").Response>} Habitación encontrada o error.
 *
 * @example
 * // GET /rooms/:id
 */
export const getRoomById = async (req, res) => {
  try {
    const { roomID } = req.params;

    const room = await roomDatabaseModel.findById(roomID);

    // Si no existe, 404
    if (!room) return res.status(404).json({ message: "no se encontro esa habitacion" });

    return res.status(200).json(room);
  } catch (err) {
    return res.status(500).json({ message: err.message });
  }
};

/**
 * Actualiza una habitación por su ID.
 *
 * Nota: este handler espera recibir `id` por params.
 * (En tu código faltaba `const { id } = req.params;` antes de usarlo.)
 *
 * @async
 * @function updateRoom
 * @param {import("express").Request} req - Request de Express (params.id y body con campos a actualizar).
 * @param {import("express").Response} res - Response de Express.
 * @returns {Promise<import("express").Response>} Habitación actualizada o error.
 *
 * @example
 * // PATCH /rooms/:id
 * // body: { description: "Nueva descripción" }
 */
export const updateRoom = async (req, res) => {
  try {
    const { roomID } = req.params;

    const update = {};

    // Log payload for debugging
    console.log("UPDATE PAYLOAD (RECV):", req.body);

    // allowlist de campos actualizables
    const fields = [
      "type",
      "roomNumber",
      "maxGuests",
      "description",
      "mainImage",
      "pricePerNight",
      "extraBed",
      "crib",
      "offer",
      "extras",
      "extraImages",
      "rate",
      "isAvailable",
    ];

    for (const f of fields) {
      if (req.body[f] !== undefined) update[f] = req.body[f];
    }

    // coerce robusto de isAvailable (por si llega string o number)
    if (update.isAvailable !== undefined) {
      const val = update.isAvailable;
      if (typeof val === "string") {
        const v = val.trim().toLowerCase();
        update.isAvailable = (v === "true" || v === "1" || v === "on");
      } else if (typeof val === "number") {
        update.isAvailable = (val === 1);
      } else {
        update.isAvailable = Boolean(val);
        console.log(update.isAvailable)
      }
    }

    console.log("UPDATE PAYLOAD (PROCESSED):", update);

    console.log(update)

    const updated = await roomDatabaseModel.findByIdAndUpdate(
      roomID,
      { $set: update },
      { new: true, runValidators: true, context: "query" }
    );

    if (!updated) return res.status(404).json({ message: "Room no encontrada" });

    return res.status(200).json(updated);
  } catch (err) {
    if (err?.code === 11000) return res.status(409).json({ message: "roomNumber ya existe" });
    return res.status(400).json({ message: err.message });
  }
};


/**
 * Elimina una habitación por su ID.
 * También elimina todas las reservas asociadas a esa habitación.
 *
 * @async
 * @function deleteRoom
 * @param {import("express").Request} req - Request de Express (params.id).
 * @param {import("express").Response} res - Response de Express.
 * @returns {Promise<import("express").Response>} Mensaje de éxito con el doc eliminado o error.
 *
 * @example
 * // DELETE /rooms/:id
 */
export const deleteRoom = async (req, res) => {
  try {
    const { roomID } = req.params;

    // Primero eliminar todas las reservas de esta habitación
    const { bookingDatabaseModel } = await import("../models/bookingModel.js");
    await bookingDatabaseModel.deleteMany({ room: roomID });

    const deleted = await roomDatabaseModel.findByIdAndDelete(roomID);

    if (!deleted) return res.status(404).json({ message: "Room no encontrada" });

    return res.status(200).json({ message: "Room eliminada", deleted });
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

/**
 * @typedef {Object} RoomsQuery
 * @property {string} [type]
 * @property {string} [isAvailable]
 * @property {string} [minPrice]
 * @property {string} [maxPrice]
 * @property {string} [guests]
 * @property {string} [hasExtraBed]
 * @property {string} [hasCrib]
 * @property {string} [hasOffer]
 * @property {string} [extras]
 * @property {string} [sortBy]
 * @property {string} [sortOrder]
 * @property {string} [roomNumber]
 */

/**
 * Obtiene el listado de rooms con filtros por query params (sin paginación).
 *
 * @async
 * @function getRoomsFiltered
 * @param {import("express").Request<{}, {}, {}, RoomsQuery>} req
 * @param {import("express").Response} res
 * @returns {Promise}
 */
export const getRoomsFiltered = async (req, res) => {
  try {
    const {
      type,
      isAvailable,
      minPrice,
      maxPrice,
      guests,
      hasExtraBed,
      hasCrib,
      hasOffer,
      extras,
      sortBy,
      sortOrder,
      roomNumber,
    } = req.query;

    const filter = {};

    if (type) filter.type = String(type);

    if (isAvailable !== undefined) {
      filter.isAvailable = String(isAvailable).toLowerCase() === "true";
    }

    const num = (v) => {
      const n = Number(v);
      return Number.isFinite(n) ? n : undefined;
    };

    const min = num(minPrice);
    const max = num(maxPrice);

    if (min !== undefined || max !== undefined) {
      filter.pricePerNight = {};
      if (min !== undefined) filter.pricePerNight.$gte = min;
      if (max !== undefined) filter.pricePerNight.$lte = max;
    }

    if (guests !== undefined) {
      const g = Number(guests);
      if (Number.isFinite(g)) filter.maxGuests = { $gte: g };
    }

    if (roomNumber !== undefined) {
      const rn = Number(roomNumber);
      if (Number.isFinite(rn)) filter.roomNumber = rn;
    }

    if (extras) {
      const extrasArr = String(extras)
        .split(",")
        .map((e) => e.trim())
        .filter(Boolean);

      if (extrasArr.length) {
        filter.extras = { $all: extrasArr };
      }
    }

    // flags
    if (hasExtraBed !== undefined) {
      filter.extraBed = String(hasExtraBed).toLowerCase() === "true";
    }
    if (hasCrib !== undefined) {
      filter.crib = String(hasCrib).toLowerCase() === "true";
    }
    if (hasOffer !== undefined) {
      const wantOffer = String(hasOffer).toLowerCase() === "true";
      filter.offer = wantOffer ? { $gt: 0 } : 0;
    }

    // sorting
    const allowedSort = new Set(["pricePerNight", "rate", "roomNumber", "type", "maxGuests"]);

    /** @type {string} */
    const sortByStr = String(sortBy ?? "");

    /** @type {keyof any} */
    const sortField = allowedSort.has(sortByStr) ? sortByStr : "roomNumber";

    /** @type {1 | -1} */
    const sortDir = String(sortOrder).toLowerCase() === "desc" ? -1 : 1;

    /** @type {Record<string, 1 | -1>} */
    const sort = { [sortField]: sortDir };

    const items = await roomDatabaseModel.find(filter).sort(sort);


    res.status(200).json({ items, appliedFilter: filter, sort });
    return;

  } catch (err) {
    res.status(400).json({ message: err.message });
    return;
  }
};

const ROOM_STATUSES = /** @type {const} */ ([
  "available",
  "occupied",
  "cleaning",
  "maintenance",
  "blocked"
]);

/**
 * Devuelve todas las habitaciones con su estado actual (para tablero).
 * GET /room/status-board
 */
export const getRoomsStatusBoard = async (req, res) => {
  try {
    const rooms = await roomDatabaseModel
      .find({}, { roomNumber: 1, type: 1, status: 1, isAvailable: 1 })
      .sort({ roomNumber: 1 });
    return res.status(200).json(rooms);
  } catch (err) {
    return res.status(500).json({ message: err.message });
  }
};

/**
 * Actualiza solo el estado de una habitación y registra en `room_status_log`.
 * PATCH /room/:roomID/status
 * body: { status, reason? }
 */
export const patchRoomStatus = async (req, res) => {
  try {
    const { roomID } = req.params;
    const statusRaw = req.body?.status;
    const reasonRaw = req.body?.reason;
    const estimatedRaw = req.body?.estimatedMinutes;

    const status = String(statusRaw ?? "").trim().toLowerCase();
    if (!ROOM_STATUSES.includes(/** @type {any} */ (status))) {
      return res.status(400).json({
        message: `status inválido. Valores: ${ROOM_STATUSES.join(", ")}`
      });
    }

    const reason =
      reasonRaw == null ? null : String(reasonRaw).trim().slice(0, 500);
    if (status !== "available" && (!reason || reason.length === 0)) {
      return res.status(400).json({
        message: "reason requerido cuando status no es available"
      });
    }

    const room = await roomDatabaseModel.findById(roomID);
    if (!room) return res.status(404).json({ message: "Room no encontrada" });

    const previousStatus =
      room.status ?? (room.isAvailable ? "available" : "occupied");

    if (previousStatus === status) {
      return res.status(200).json(room);
    }

    room.status = status;
    room.isAvailable = status === "available";
    const updated = await room.save();

    const reqUser = /** @type {any} */ (req).user || {};
    let estimatedMinutes = null;
    if (estimatedRaw !== undefined && estimatedRaw !== null && String(estimatedRaw).trim().length) {
      const n = Number(estimatedRaw);
      if (Number.isFinite(n) && n >= 0) estimatedMinutes = Math.floor(n);
    }
    await roomStatusLogModel.create({
      room_id: updated._id,
      previous_status: previousStatus ?? null,
      new_status: status,
      reason: reason,
      estimated_minutes: estimatedMinutes,
      changed_by: reqUser.id ?? null,
      changed_by_role: reqUser.rol ?? "SYSTEM",
      changed_at: new Date()
    });

    return res.status(200).json(updated);
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

/**
 * Historial de cambios de estado de una habitación.
 * GET /room/:roomID/status-log
 */
export const getRoomStatusLog = async (req, res) => {
  try {
    const { roomID } = req.params;
    const items = await roomStatusLogModel
      .find({ room_id: roomID })
      .sort({ changed_at: -1 })
      .limit(200)
      .lean();
    return res.status(200).json(items);
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

