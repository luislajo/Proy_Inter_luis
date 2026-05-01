/**
 * @file Rutas `/room`: CRUD habitaciones, listado filtrado, auditoría por habitación.
 */
import { Router } from "express";
import {
  createRoom,
  getRoomById,
  updateRoom,
  deleteRoom,
  getRoomsFiltered,
  getRoomsStatusBoard,
  patchRoomStatus,
  getRoomStatusLog
} from "../controllers/roomsController.js";
import { createIncidentForRoom, getIncidentsForRoom, getMyIncidentsForRoom } from "../controllers/incidentsController.js";
import { getAuditLogByRoomId } from "../controllers/auditLogController.js";
import { authorizeRoles, verifyToken } from "../middlewares/authMiddleware.js";
import { auditMiddleware } from "../middlewares/auditMiddleware.js";

const router = Router();

router.post("/", verifyToken, 
              authorizeRoles(["Admin", "Trabajador"]),
              auditMiddleware("room", "roomID"), createRoom);
router.get("/", verifyToken, getRoomsFiltered);
router.get("/status-board", verifyToken, getRoomsStatusBoard);

// Incidencias: clientes pueden reportar (types limitados en controller)
router.post("/:roomID/incidents", verifyToken, createIncidentForRoom);
router.get("/:roomID/incidents/my", verifyToken, getMyIncidentsForRoom);
router.get(
  "/:roomID/incidents",
  verifyToken,
  authorizeRoles(["Admin", "Trabajador"]),
  getIncidentsForRoom
);

router.patch(
  "/:roomID/status",
  verifyToken,
  authorizeRoles(["Admin", "Trabajador"]),
  patchRoomStatus
);
router.get(
  "/:roomID/status-log",
  verifyToken,
  authorizeRoles(["Admin", "Trabajador"]),
  getRoomStatusLog
);
router.get("/:roomID/audit", verifyToken, authorizeRoles(["Admin", "Trabajador"]), getAuditLogByRoomId);
router.get("/:roomID", verifyToken, getRoomById);
router.patch("/:roomID", verifyToken, 
              authorizeRoles(["Admin", "Trabajador"]), 
              auditMiddleware("room", "roomID"), updateRoom);
router.delete("/:roomID", 
              verifyToken, 
              authorizeRoles(["Admin"]),
              auditMiddleware("room", "roomID"), deleteRoom);

export default router;
