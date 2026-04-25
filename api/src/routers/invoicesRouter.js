/**
 * @file Rutas `/invoices`: historial de facturas (reservas con `invoice_number`), filtros según rol.
 */
import { Router } from "express";
import { getInvoicesByUserId } from "../controllers/bookingController.js";
import { verifyToken } from "../middlewares/authMiddleware.js";

const router = Router();

router.get("/", verifyToken, getInvoicesByUserId);

export default router;
