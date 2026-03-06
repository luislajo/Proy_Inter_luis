import { Router } from "express";
import {
    getOneBookingById,
    getBookings,
    getBookingsByClientId,
    getBookingsByRoomId,
    createBooking,
    cancelBooking,
    updateBooking,
    deleteBooking
} from "../controllers/bookingController.js";
import { getAuditLogByBookingId } from "../controllers/auditLogController.js";
import { verifyToken, authorizeRoles } from "../middlewares/authMiddleware.js";

const router = Router();

router.get("/:id/audit", verifyToken, authorizeRoles(["Admin", "Trabajador"]), getAuditLogByBookingId);
router.get("/:id", verifyToken, getOneBookingById);
router.get("/client/:id", verifyToken, getBookingsByClientId);
router.get("/room/:id", verifyToken, authorizeRoles(["Admin", "Trabajador"]), getBookingsByRoomId);
router.get("/", verifyToken, getBookings);

router.post("/", verifyToken, createBooking);

router.patch("/:id/cancel", verifyToken, cancelBooking);
router.patch("/:id", verifyToken, updateBooking);

router.delete("/:id", verifyToken, authorizeRoles(["Admin"]), deleteBooking);

export default router;