import mongoose from "mongoose";
import { userDatabaseModel } from "../models/usersModel.js";

/**
 * ObjectId del documento en la colección `users` para auditoría (`room_status_log.changed_by`).
 * Comprueba que el id exista en `users`; si no, prueba el siguiente candidato del JWT y al final el email.
 * Evita persistir un ObjectId válido que sea de otra colección (p. ej. habitación).
 *
 * @param {Record<string, unknown>|null|undefined} reqUser - `req.user` tras `jwtVerify`
 * @returns {Promise<import("mongoose").Types.ObjectId|null>}
 */
export async function resolveChangedByUserId(reqUser) {
  if (!reqUser || typeof reqUser !== "object") return null;

  const candidates = [
    reqUser.sub,
    reqUser.id,
    reqUser._id,
    reqUser.userId,
    reqUser.user_id
  ];

  const seen = new Set();
  for (const raw of candidates) {
    if (raw == null) continue;
    const s = String(raw).trim();
    if (!mongoose.isValidObjectId(s)) continue;
    const key = s.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);
    try {
      const oid = new mongoose.Types.ObjectId(s);
      if (await userDatabaseModel.exists({ _id: oid })) return oid;
    } catch {
      /* siguiente candidato */
    }
  }

  const emailRaw = reqUser.email;
  if (emailRaw != null) {
    const email = String(emailRaw).trim();
    if (email.length > 0) {
      const u = await userDatabaseModel.findOne({ email }).select("_id").lean();
      if (u?._id) return /** @type {import("mongoose").Types.ObjectId} */ (u._id);
    }
  }

  return null;
}
