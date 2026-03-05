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

const router = Router();

router.post("/", verifyToken, authorizeRoles(["Admin", "Trabajador"]), createRoom);
router.get("/", verifyToken, getRoomsFiltered);
router.get("/:roomID/audit", verifyToken, authorizeRoles(["Admin", "Trabajador"]), getAuditLogByRoomId);
router.get("/:roomID", verifyToken, getRoomById);
router.patch("/:roomID", verifyToken, authorizeRoles(["Admin", "Trabajador"]), updateRoom);
router.delete("/:roomID", verifyToken, authorizeRoles(["Admin"]), deleteRoom);

export default router;
