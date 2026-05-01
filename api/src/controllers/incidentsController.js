import mongoose from "mongoose";
import { incidentModel } from "../models/incidentModel.js";
import { roomDatabaseModel } from "../models/roomsModel.js";
import { roomStatusLogModel } from "../models/roomStatusLogModel.js";

const CLIENT_ALLOWED_TYPES = new Set(["ruido", "limpieza", "averia"]);

function normalizeStr(v) {
  return String(v ?? "").trim();
}

/**
 * POST /room/:roomID/incidents
 * body: { type, severity, description }
 * If severity=high => set room.status=maintenance
 */
export const createIncidentForRoom = async (req, res) => {
  try {
    const { roomID } = req.params;
    if (!mongoose.isValidObjectId(roomID)) {
      return res.status(400).json({ message: "roomID no válido" });
    }

    const reqUser = /** @type {any} */ (req).user || {};
    const role = String(reqUser.rol ?? "");

    const type = normalizeStr(req.body?.type).toLowerCase();
    const severity = normalizeStr(req.body?.severity).toLowerCase();
    const description = normalizeStr(req.body?.description);

    if (!type) return res.status(400).json({ message: "type requerido" });
    if (!severity) return res.status(400).json({ message: "severity requerido" });
    if (!description) return res.status(400).json({ message: "description requerido" });

    if (role === "Usuario" && !CLIENT_ALLOWED_TYPES.has(type)) {
      return res.status(400).json({
        message: `type inválido para cliente. Valores: ${Array.from(CLIENT_ALLOWED_TYPES).join(", ")}`
      });
    }

    if (!["low", "medium", "high"].includes(severity)) {
      return res.status(400).json({ message: "severity inválido (low|medium|high)" });
    }

    const room = await roomDatabaseModel.findById(roomID);
    if (!room) return res.status(404).json({ message: "Room no encontrada" });

    const created = await incidentModel.create({
      room_id: room._id,
      type,
      severity,
      description,
      status: "open",
      reported_by: reqUser.id,
      history: [
        {
          action: "CREATE",
          by: reqUser.id ?? null,
          at: new Date(),
          details: { type, severity }
        }
      ]
    });

    if (severity === "high") {
      const prevStatus = room.status ?? (room.isAvailable ? "available" : "occupied");
      // set to maintenance
      room.status = "maintenance";
      room.isAvailable = false;
      await room.save();

      roomStatusLogModel.create({
        room_id: room._id,
        previous_status: prevStatus ?? null,
        new_status: "maintenance",
        reason: `incidencia grave: ${type}`,
        estimated_minutes: null,
        changed_by: reqUser.id ?? null,
        changed_by_role: reqUser.rol ?? "SYSTEM",
        changed_at: new Date()
      }).catch(() => {});
    }

    return res.status(201).json(created);
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

/**
 * GET /incidents
 * Query: ?status=open|resolved&severity=low|medium|high&roomId=...&type=...&assignedTo=...&limit=...
 */
export const getIncidents = async (req, res) => {
  try {
    /** @type {any} */
    const q = {};

    const status = normalizeStr(req.query?.status).toLowerCase();
    if (status && ["open", "resolved"].includes(status)) q.status = status;

    const severity = normalizeStr(req.query?.severity).toLowerCase();
    if (severity && ["low", "medium", "high"].includes(severity)) q.severity = severity;

    const type = normalizeStr(req.query?.type).toLowerCase();
    if (type) q.type = type;

    const roomId = normalizeStr(req.query?.roomId);
    if (roomId) q.room_id = roomId;

    const assignedTo = normalizeStr(req.query?.assignedTo);
    if (assignedTo) q.assigned_to = assignedTo;

    const limitRaw = Number(req.query?.limit ?? 200);
    const limit = Number.isFinite(limitRaw) ? Math.max(1, Math.min(500, limitRaw)) : 200;

    const items = await incidentModel
      .find(q)
      .sort({ reported_at: -1 })
      .limit(limit)
      .lean();

    return res.status(200).json({ items, filter: q, limit });
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

/**
 * GET /incidents/:id
 */
export const getIncidentById = async (req, res) => {
  try {
    const { id } = req.params;
    if (!mongoose.isValidObjectId(id)) return res.status(400).json({ message: "id no válido" });
    const item = await incidentModel.findById(id).lean();
    if (!item) return res.status(404).json({ message: "Incidencia no encontrada" });
    return res.status(200).json(item);
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

/**
 * PATCH /incidents/:id/assign
 * body: { employeeId }
 */
export const assignIncident = async (req, res) => {
  try {
    const { id } = req.params;
    if (!mongoose.isValidObjectId(id)) return res.status(400).json({ message: "id no válido" });
    const employeeId = normalizeStr(req.body?.employeeId);
    if (!mongoose.isValidObjectId(employeeId)) {
      return res.status(400).json({ message: "employeeId no válido" });
    }

    const reqUser = /** @type {any} */ (req).user || {};
    const updated = await incidentModel.findByIdAndUpdate(
      id,
      {
        $set: { assigned_to: employeeId },
        $push: {
          history: {
            action: "ASSIGN",
            by: reqUser.id ?? null,
            at: new Date(),
            details: { assigned_to: employeeId }
          }
        }
      },
      { new: true }
    );
    if (!updated) return res.status(404).json({ message: "Incidencia no encontrada" });
    return res.status(200).json(updated);
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

/**
 * POST /incidents/:id/notes
 * body: { note }
 */
export const addIncidentNote = async (req, res) => {
  try {
    const { id } = req.params;
    if (!mongoose.isValidObjectId(id)) return res.status(400).json({ message: "id no válido" });
    const note = normalizeStr(req.body?.note);
    if (!note) return res.status(400).json({ message: "note requerido" });

    const reqUser = /** @type {any} */ (req).user || {};
    const updated = await incidentModel.findByIdAndUpdate(
      id,
      {
        $push: {
          internal_notes: {
            note: note.slice(0, 2000),
            author_id: reqUser.id,
            created_at: new Date()
          },
          history: {
            action: "NOTE",
            by: reqUser.id ?? null,
            at: new Date()
          }
        }
      },
      { new: true }
    );
    if (!updated) return res.status(404).json({ message: "Incidencia no encontrada" });
    return res.status(200).json(updated);
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

/**
 * PATCH /incidents/:id/resolve
 * Marks resolved; if it was high and no other open high incidents for same room => room.status=available
 */
export const resolveIncident = async (req, res) => {
  try {
    const { id } = req.params;
    if (!mongoose.isValidObjectId(id)) return res.status(400).json({ message: "id no válido" });

    const incident = await incidentModel.findById(id);
    if (!incident) return res.status(404).json({ message: "Incidencia no encontrada" });

    if (incident.status === "resolved") return res.status(200).json(incident);

    incident.status = "resolved";
    incident.resolved_at = new Date();
    incident.history = Array.isArray(incident.history) ? incident.history : [];
    const reqUser = /** @type {any} */ (req).user || {};
    incident.history.push({
      action: "RESOLVE",
      by: reqUser.id ?? null,
      at: new Date()
    });
    const resolved = await incident.save();

    if (incident.severity === "high") {
      const roomId = incident.room_id;
      const remainingHigh = await incidentModel.exists({
        room_id: roomId,
        status: "open",
        severity: "high"
      });

      if (!remainingHigh) {
        const room = await roomDatabaseModel.findById(roomId);
        if (room) {
          const prevStatus = room.status ?? (room.isAvailable ? "available" : "occupied");
          room.status = "available";
          room.isAvailable = true;
          await room.save();

          roomStatusLogModel.create({
            room_id: room._id,
            previous_status: prevStatus ?? null,
            new_status: "available",
            reason: "resuelta última incidencia grave",
            estimated_minutes: null,
            changed_by: reqUser.id ?? null,
            changed_by_role: reqUser.rol ?? "SYSTEM",
            changed_at: new Date()
          }).catch(() => {});
        }
      }
    }

    return res.status(200).json(resolved);
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

/**
 * GET /room/:roomID/incidents
 * Query: ?status=open|resolved&type=...&severity=...&from=...&to=...&limit=...
 */
export const getIncidentsForRoom = async (req, res) => {
  try {
    const { roomID } = req.params;
    if (!mongoose.isValidObjectId(roomID)) {
      return res.status(400).json({ message: "roomID no válido" });
    }

    const q = {};
    q.room_id = roomID;

    const status = normalizeStr(req.query?.status).toLowerCase();
    if (status && ["open", "resolved"].includes(status)) q.status = status;

    const type = normalizeStr(req.query?.type).toLowerCase();
    if (type) q.type = type;

    const severity = normalizeStr(req.query?.severity).toLowerCase();
    if (severity && ["low", "medium", "high"].includes(severity)) q.severity = severity;

    const from = req.query?.from ? new Date(String(req.query.from)) : null;
    const to = req.query?.to ? new Date(String(req.query.to)) : null;
    if ((from && !isNaN(from.getTime())) || (to && !isNaN(to.getTime()))) {
      q.reported_at = {};
      if (from && !isNaN(from.getTime())) q.reported_at.$gte = from;
      if (to && !isNaN(to.getTime())) q.reported_at.$lte = to;
    }

    const limitRaw = Number(req.query?.limit ?? 100);
    const limit = Number.isFinite(limitRaw) ? Math.max(1, Math.min(200, limitRaw)) : 100;

    const items = await incidentModel
      .find(q)
      .sort({ reported_at: -1 })
      .limit(limit)
      .lean();

    return res.status(200).json({ items, filter: q, limit });
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

/**
 * GET /room/:roomID/incidents/my
 * Cliente ve solo sus incidencias en esa habitación, sin detalles técnicos.
 */
export const getMyIncidentsForRoom = async (req, res) => {
  try {
    const { roomID } = req.params;
    if (!mongoose.isValidObjectId(roomID)) {
      return res.status(400).json({ message: "roomID no válido" });
    }
    const reqUser = /** @type {any} */ (req).user || {};

    const items = await incidentModel
      .find({ room_id: roomID, reported_by: reqUser.id })
      .sort({ reported_at: -1 })
      .select("room_id type severity description status reported_at resolved_at")
      .lean();

    return res.status(200).json({ items });
  } catch (err) {
    return res.status(400).json({ message: err.message });
  }
};

