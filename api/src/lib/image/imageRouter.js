/**
 * @file Rutas `/image`: subida (una o varias) y borrado de fotos en disco bajo `uploads/`.
 */
import { Router } from "express";
import { upload } from "../../services/imageService.js";
import { uploadPhoto, uploadPhotos, deletePhoto, deletePhotos } from "./imageController.js";

const router = Router();

// Subir 1
router.post("/", upload.single("photo"), uploadPhoto);

// Subir varias
router.post("/many", upload.array("photos", 20), uploadPhotos);

// Borrar 1 por filename
router.delete("/:filename", deletePhoto);

// Borrar varias
router.delete("/", deletePhotos);

export default router;
