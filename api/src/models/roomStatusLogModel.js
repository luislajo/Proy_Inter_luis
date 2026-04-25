import { Schema, Types, model } from "mongoose";

/**
 * @typedef {Object} RoomStatusLog
 * @property {import("mongoose").Types.ObjectId} _id
 * @property {import("mongoose").Types.ObjectId} room_id
 * @property {"available"|"occupied"|"cleaning"|"maintenance"|"blocked"|null} previous_status
 * @property {"available"|"occupied"|"cleaning"|"maintenance"|"blocked"} new_status
 * @property {string|null} reason
 * @property {number|null} estimated_minutes
 * @property {import("mongoose").Types.ObjectId|null} changed_by
 * @property {"Admin"|"Trabajador"|"SYSTEM"} changed_by_role
 * @property {Date} changed_at
 */
const roomStatusLogSchema = new Schema({
  room_id: { type: Types.ObjectId, required: true, ref: "room" },
  previous_status: {
    type: String,
    enum: ["available", "occupied", "cleaning", "maintenance", "blocked", null],
    default: null
  },
  new_status: {
    type: String,
    enum: ["available", "occupied", "cleaning", "maintenance", "blocked"],
    required: true
  },
  reason: { type: String, default: null },
  estimated_minutes: { type: Number, default: null, min: 0 },
  changed_by: { type: Types.ObjectId, ref: "user", default: null },
  changed_by_role: { type: String, enum: ["Admin", "Trabajador", "SYSTEM"], required: true },
  changed_at: { type: Date, default: Date.now }
});

roomStatusLogSchema.index({ room_id: 1, changed_at: -1 });
roomStatusLogSchema.index({ changed_at: -1 });

export const roomStatusLogModel = model("room_status_log", roomStatusLogSchema);

