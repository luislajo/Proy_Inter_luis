import { Router } from "express";
import { verifyToken, authorizeRoles } from "../middlewares/authMiddleware.js";
import {
  resolveIncident,
  getIncidents,
  getIncidentById,
  assignIncident,
  addIncidentNote
} from "../controllers/incidentsController.js";

const router = Router();

router.get("/", verifyToken, authorizeRoles(["Admin", "Trabajador"]), getIncidents);
router.get("/:id", verifyToken, authorizeRoles(["Admin", "Trabajador"]), getIncidentById);
router.patch("/:id/assign", verifyToken, authorizeRoles(["Admin", "Trabajador"]), assignIncident);
router.post("/:id/notes", verifyToken, authorizeRoles(["Admin", "Trabajador"]), addIncidentNote);
router.patch(
  "/:id/resolve",
  verifyToken,
  authorizeRoles(["Admin", "Trabajador"]),
  resolveIncident
);

export default router;

