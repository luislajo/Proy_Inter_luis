import express from "express";
import { getAllAuditLogs, getAuditLogByBookingId, getAuditLogByRoomId } from "../controllers/auditLogController.js";
import { verifyToken, authorizeRoles } from "../middlewares/authMiddleware.js";

const auditRouter = express.Router();

// Rutas globales para auditoría, protegidas para usuarios con permisos elevados
auditRouter.get("/", verifyToken, authorizeRoles(['Admin', 'Trabajador']), getAllAuditLogs);
auditRouter.get("/booking/:id", verifyToken, authorizeRoles(['Admin', 'Trabajador']), getAuditLogByBookingId);
auditRouter.get("/room/:roomID", verifyToken, authorizeRoles(['Admin', 'Trabajador']), getAuditLogByRoomId);

export default auditRouter;
