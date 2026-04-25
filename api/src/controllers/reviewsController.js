import { reviewDatabaseModel } from "../models/reviewsModel.js";

/**
 * Crea una reseña vinculada a usuario, habitación y reserva.
 *
 * @async
 * @function createReview
 * @param {import("express").Request} req - Body: `user`, `room`, `booking`, `rating`, `description`.
 * @param {import("express").Response} res
 * @returns {Promise}
 */
export async function createReview(req, res) {
    try {
        const { user, room, booking, rating, description } = req.body;

        if (!user || !room || !booking || !rating || !description) {
            return res.status(400).json({ message: "Todos los campos son requeridos" });
        }

        const review = new reviewDatabaseModel({
            user,
            room,
            booking,
            rating,
            description,
        });

        const saved = await review.save();
        const populated = await reviewDatabaseModel.findById(saved._id)
            .populate("user", "firstName lastName email")
            .populate("booking", "checkInDate checkOutDate");

        return res.status(201).json(populated);
    } catch (err) {
        console.error("Error al crear reseña:", err.message);
        return res.status(500).json({ message: "Error interno del servidor" });
    }
}

/**
 * Lista todas las reseñas con populate de usuario, habitación y reserva.
 *
 * @async
 * @function getReviews
 * @param {import("express").Request} req
 * @param {import("express").Response} res
 * @returns {Promise}
 */
export async function getReviews(req, res) {
    try {
        const reviews = await reviewDatabaseModel.find()
            .populate("user", "firstName lastName email")
            .populate("room", "roomNumber type")
            .populate("booking", "checkInDate checkOutDate");
        return res.status(200).json(reviews);
    } catch (err) {
        console.error("Error al obtener reseñas:", err.message);
        return res.status(500).json({ message: "Error interno del servidor" });
    }
}

/**
 * Obtiene una reseña por `reviewID` con datos relacionados poblados.
 *
 * @async
 * @function getReviewById
 * @param {import("express").Request} req
 * @param {import("express").Response} res
 * @returns {Promise}
 */
export async function getReviewById(req, res) {
    try {
        const { reviewID } = req.params;
        const review = await reviewDatabaseModel.findById(reviewID)
            .populate("user", "firstName lastName email")
            .populate("room", "roomNumber type")
            .populate("booking", "checkInDate checkOutDate");

        if (!review) {
            return res.status(404).json({ message: "Reseña no encontrada" });
        }

        return res.status(200).json(review);
    } catch (err) {
        console.error("Error al obtener reseña:", err.message);
        return res.status(500).json({ message: "Error interno del servidor" });
    }
}

/**
 * Lista reseñas de una habitación (`roomID`).
 *
 * @async
 * @function getReviewsByRoom
 * @param {import("express").Request} req
 * @param {import("express").Response} res
 * @returns {Promise}
 */
export async function getReviewsByRoom(req, res) {
    try {
        const { roomID } = req.params;
        const reviews = await reviewDatabaseModel.find({ room: roomID })
            .populate("user", "firstName lastName email")
            .populate("booking", "checkInDate checkOutDate");
        return res.status(200).json(reviews);
    } catch (err) {
        console.error("Error al obtener reseñas por habitación:", err.message);
        return res.status(500).json({ message: "Error interno del servidor" });
    }
}

/**
 * Lista reseñas de un usuario (`userID`).
 *
 * @async
 * @function getReviewsByUser
 * @param {import("express").Request} req
 * @param {import("express").Response} res
 * @returns {Promise}
 */
export async function getReviewsByUser(req, res) {
    try {
        const { userID } = req.params;
        const reviews = await reviewDatabaseModel.find({ user: userID })
            .populate("room", "roomNumber type")
            .populate("booking", "checkInDate checkOutDate");
        return res.status(200).json(reviews);
    } catch (err) {
        console.error("Error al obtener reseñas por usuario:", err.message);
        return res.status(500).json({ message: "Error interno del servidor" });
    }
}

/**
 * Elimina una reseña por `reviewID`.
 *
 * @async
 * @function deleteReview
 * @param {import("express").Request} req
 * @param {import("express").Response} res
 * @returns {Promise}
 */
export async function deleteReview(req, res) {
    try {
        const { reviewID } = req.params;
        const deleted = await reviewDatabaseModel.findByIdAndDelete(reviewID);

        if (!deleted) {
            return res.status(404).json({ message: "Reseña no encontrada" });
        }

        return res.status(200).json({ message: "Reseña eliminada correctamente" });
    } catch (err) {
        console.error("Error al eliminar reseña:", err.message);
        return res.status(500).json({ message: "Error interno del servidor" });
    }
}
