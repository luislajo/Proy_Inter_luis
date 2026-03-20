import { Router } from "express";
import {
    getOneBookingById,
    getBookings,
    getBookingsByClientId,
    getBookingsByRoomId,
    createBooking,
    cancelBooking,
    updateBooking,
    deleteBooking,
    payBooking,
    getBookingInvoicePdf
} from "../controllers/bookingController.js";
import { getAuditLogByBookingId } from "../controllers/auditLogController.js";
import { verifyToken, authorizeRoles } from "../middlewares/authMiddleware.js";
import { auditMiddleware } from "../middlewares/auditMiddleware.js";

const router = Router();

router.get("/:id/audit", verifyToken, authorizeRoles(["Admin", "Trabajador"]), getAuditLogByBookingId);
router.get("/:id/invoice", verifyToken, getBookingInvoicePdf);
router.get("/:id", verifyToken, getOneBookingById);
router.get("/client/:id", verifyToken, getBookingsByClientId);
router.get("/room/:id", verifyToken, authorizeRoles(["Admin", "Trabajador"]), getBookingsByRoomId);
router.get("/", verifyToken, getBookings);

router.post("/", verifyToken, createBooking);
router.post("/:id/pay", verifyToken, payBooking);

router.patch("/:id/cancel", verifyToken, auditMiddleware("booking", "id"), cancelBooking);
router.patch("/:id", verifyToken, auditMiddleware("booking", "id"), updateBooking);

router.delete("/:id", verifyToken, authorizeRoles(["Admin"]), auditMiddleware("booking", "id"), deleteBooking);

export default router;