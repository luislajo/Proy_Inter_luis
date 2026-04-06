import express from "express";
import { getAllAuditLogs, getAuditLogByBookingId, getAuditLogByRoomId, getAuditLogByUserId } from "../controllers/auditLogController.js";
import { verifyToken, authorizeRoles } from "../middlewares/authMiddleware.js";

const auditRouter = express.Router();

// Rutas globales para auditoría, protegidas para usuarios con permisos elevados
auditRouter.get("/", verifyToken, authorizeRoles(['Admin', 'Trabajador']), getAllAuditLogs);
auditRouter.get("/booking/:id", verifyToken, authorizeRoles(['Admin', 'Trabajador']), getAuditLogByBookingId);
auditRouter.get("/room/:roomID", verifyToken, authorizeRoles(['Admin', 'Trabajador']), getAuditLogByRoomId);

// Ruta para que un cliente vea su propio historial de actividad
auditRouter.get("/client/:id", verifyToken, getAuditLogByUserId);

export default auditRouter;
