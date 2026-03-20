import { Router } from "express";
import {
  createRoom,
  getRoomById,
  updateRoom,
  deleteRoom,
  getRoomsFiltered
} from "../controllers/roomsController.js";
import { getAuditLogByRoomId } from "../controllers/auditLogController.js";
import { authorizeRoles, verifyToken } from "../middlewares/authMiddleware.js";
import { auditMiddleware } from "../middlewares/auditMiddleware.js";

const router = Router();

router.post("/", verifyToken, 
              authorizeRoles(["Admin", "Trabajador"]),
              auditMiddleware("room", "roomID"), createRoom);
router.get("/", verifyToken, getRoomsFiltered);
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
