import { Schema, Types, model } from "mongoose";

/**
 * @typedef {Object} Incident
 * @property {import("mongoose").Types.ObjectId} _id
 * @property {import("mongoose").Types.ObjectId} room_id
 * @property {"ruido"|"limpieza"|"averia"|"otro"} type
 * @property {"low"|"medium"|"high"} severity
 * @property {string} description
 * @property {"open"|"resolved"} status
 * @property {import("mongoose").Types.ObjectId} reported_by
 * @property {import("mongoose").Types.ObjectId|null} assigned_to
 * @property {{ note: string, author_id: import("mongoose").Types.ObjectId, created_at: Date }[]} internal_notes
 * @property {{ action: string, by: import("mongoose").Types.ObjectId|null, at: Date, details?: any }[]} history
 * @property {Date} reported_at
 * @property {Date|null} resolved_at
 */
const incidentSchema = new Schema({
  room_id: { type: Types.ObjectId, ref: "room", required: true, index: true },
  type: {
    type: String,
    enum: ["ruido", "limpieza", "averia", "otro"],
    required: true
  },
  severity: {
    type: String,
    enum: ["low", "medium", "high"],
    required: true
  },
  description: { type: String, required: true, trim: true, maxlength: 2000 },
  status: { type: String, enum: ["open", "resolved"], default: "open", index: true },
  reported_by: { type: Types.ObjectId, ref: "user", required: true },
  assigned_to: { type: Types.ObjectId, ref: "user", default: null, index: true },
  internal_notes: {
    type: [
      {
        note: { type: String, required: true, trim: true, maxlength: 2000 },
        author_id: { type: Types.ObjectId, ref: "user", required: true },
        created_at: { type: Date, default: Date.now }
      }
    ],
    default: []
  },
  history: {
    type: [
      {
        action: { type: String, required: true },
        by: { type: Types.ObjectId, ref: "user", default: null },
        at: { type: Date, default: Date.now },
        details: { type: Schema.Types.Mixed, default: null }
      }
    ],
    default: []
  },
  reported_at: { type: Date, default: Date.now, index: true },
  resolved_at: { type: Date, default: null }
});

incidentSchema.index({ room_id: 1, reported_at: -1 });
incidentSchema.index({ room_id: 1, status: 1, severity: 1, reported_at: -1 });
incidentSchema.index({ status: 1, severity: 1, reported_at: -1 });

export const incidentModel = model("incident", incidentSchema);

