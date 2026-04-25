/**
 * @file Rutas `/review`: CRUD de reseñas por recurso o filtros habitación/usuario.
 */
import { Router } from "express";
import {
    createReview,
    getReviews,
    getReviewById,
    getReviewsByRoom,
    getReviewsByUser,
    deleteReview
} from "../controllers/reviewsController.js";

const router = Router();

router.post("/", createReview);
router.get("/", getReviews);
router.get("/:reviewID", getReviewById);
router.get("/room/:roomID", getReviewsByRoom);
router.get("/user/:userID", getReviewsByUser);
router.delete("/:reviewID", deleteReview);

export default router;
