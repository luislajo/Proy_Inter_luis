import { bookingDatabaseModel, BookingEntryData } from "../models/bookingModel.js";
import { roomDatabaseModel } from "../models/roomsModel.js"
import mongoose from "mongoose";
import { userDatabaseModel } from "../models/usersModel.js";
import { sendEmail } from "../lib/mail/mailing.js";
import { auditLogModel } from "../models/auditLogModel.js";
/**
 * Obtener una reserva específica a partir de su ID
 * 
 * @async
 * @function getOneBookingById
 * 
 * @description
 * Recibe un ID de reserva en el cuerpo de la petición
 * Busca la reserva correspondiente y la devuelve en formato JSON
 * Maneja errores de validación, no encontrar la reserva y errores del servidor
 * 
 * @param {import('express').Response} res - Objeto de respuesta de Express
 * 
 * 
 * @returns {Promise} Respuesta HTTP con:
 * - Código 200 y el objeto de la reserva si se encuentra
 * - Código 400 si no se proporciona el ID
 * - Código 404 si no se encuentra la reserva
 * - Código 500 si ocurre un error inesperado
 */
export async function getOneBookingById(req, res) {
    try {
        const { id } = req.params;
        if (!id) return res.status(400).json({ error: 'Se requiere ID de la reserva' });
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID' })

        const booking = await bookingDatabaseModel.findById(id);
        if (!booking) return res.status(404).json({ error: 'Reserva no encontrada' });

        return res.status(200).json(booking);
    }
    catch (error) {
        console.error('Error al obtener la reserva:', error);
        return res.status(500).json({ error: 'Error del servidor' });
    }
}


/**
 *  Obtener todas las reservas
 * 
 * @async
 * @function getBookings
 * 
 * @description
 * Busca todas las reservas y las devuelve en formato JSON
 * Maneja errores de no encontrar la reserva y errores del servidor
 * 
 * 
 * @param {import('express').Response} res - Objeto de respuesta de Express
 * 
 * 
 * @returns {Promise} Respuesta HTTP con:
 * - Código 200 y el las reservas si las encuentra
 * - Código 404 si no se encuentra reservas
 * - Código 500 si ocurre un error inesperado
 */
export async function getBookings(req, res) {
    try {
        const bookings = await bookingDatabaseModel.find();
        if (bookings.length == 0) return res.status(404).json({ error: 'No se encontraron reservas' });

        return res.status(200).json(bookings);
    }
    catch (error) {
        console.error('Error al obtener la reserva:', error);
        return res.status(500).json({ error: 'Error del servidor' });
    }
}


/**
 * Obtener las reservas de un cliente
 * 
 * @async
 * @function getBookingsByClientId
 * 
 * @description
 * Recibe el ID del cliente por el cuerpo de la petición
 * Busca las reservas del cliente y las devuelve en formato JSON
 * Maneja errores de validación, no encontrar reservas y errores del servidor
 * 
 * 
 * @param {import('express').Response} res - Objeto de respuesta de Express
 * 
 * @returns {Promise} Respuesta HTTP con:
 * - Código 200 con las reservas del cliente
 * - Código 400 si no consigue el ID del cliente
 * - Código 404 si no encuentra reservas
 * - Código 500 si ocurre un error inesperado 
 */
export async function getBookingsByClientId(req, res) {
    try {
        const { id } = req.params;
        if (!id) return res.status(400).json({ error: 'Se requiere ID del cliente' });
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID' })

        const bookings = await bookingDatabaseModel.find({ client: id });
        if (!bookings) return res.status(404).json({ error: 'No se encontraron reservas para el cliente' });

        return res.status(200).json(bookings);
    }
    catch (error) {
        console.error('Error al obtener las reservas del cliente:', error);
        return res.status(500).json({ error: 'Error del servidor' });
    }
}


/**
 * Obtener las reservas de una habitación
 * 
 * @async
 * @function getBookingsByRoomId
 * 
 * @description
 * Recibe el ID de la habitación por el cuerpo de la petición
 * Busca las reservas de la habitación y las devuelve en formato JSON
 * Maneja errores de validación, no encontrar reservas y errores del servidor
 * 
 * @param {import('express').Response} res - Objeto de respuesta de Express
 * 
 * @returns {Promise} Respuesta HTTP con:
 * - Código 200 con las reservas de la habitación
 * - Código 400 si no consigue el ID de la habitación
 * - Código 404 si no encuentra reservas
 * - Código 500 si ocurre un error inesperado
 */
export async function getBookingsByRoomId(req, res) {
    try {
        const { id } = req.params;
        if (!id) return res.status(400).json({ error: 'Se requiere ID de la habitación' });
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID' })

        const bookings = await bookingDatabaseModel.find({ room: id });
        if (!bookings) return res.status(404).json({ error: 'No se ha encontrado reservas para la habitación' });

        return res.status(200).json(bookings);
    }
    catch (error) {
        console.error('Error al obtener las reservas de la habitación:', error);
        return res.status(500).json({ error: 'Error del servidor' });
    }
}


/**
 * Crea una nueva reserva para una habitación.
 *
 * @async
 * @function createBooking
 *
 * @description
 * Recibe los datos del cuerpo de la solicitud y la sesión actual
 * Crea una nueva reserva y la devuelve en formato JSON
 * Valida la existencia y validez de datos y los errores de servidor
 *
 * @param {import('express').Response} res - Objeto de respuesta de Express.
 *
 * @returns {Promise} Respuesta HTTP con:
 * - 200 y el objeto de la reserva si se crea correctamente
 * - 400 si faltan datos obligatorios o la validación falla
 * - 404 si la habitación no existe
 * - 500 si ocurre un error interno del servidor
 */
export async function createBooking(req, res) {
    try {
        const { client, room, checkInDate, checkOutDate, guests } = req.body;

        if (!room) return res.status(400).json({ error: 'Se requiere ID de habitación' });
        if (!client) return res.status(400).json({ error: 'Se requiere ID de cliente' });
        if (!checkInDate) return res.status(400).json({ error: 'Se requiere fecha de check in' });
        if (!checkOutDate) return res.status(400).json({ error: 'Se requiere fecha de check out' });
        if (!guests) return res.status(400).json({ error: 'Se requiere cantidad de huéspedes' });

        const dbRoom = await roomDatabaseModel.findById(room);
        if (!dbRoom) return res.status(404).json({ error: 'No se encuentra habitación con ese ID' });

        const booking = new BookingEntryData(room, client, checkInDate, checkOutDate, guests);

        if (guests > dbRoom.maxGuests) return res.status(400).json({ error: 'Se supera el límite de huéspedes de la habitación' })

        booking.completeBookingData(dbRoom.pricePerNight, dbRoom.offer);
        try {
            await booking.validate()
        }
        catch (err) {
            return res.status(400).json({ error: err.message });
        }

        const user = await userDatabaseModel.findById(booking.clientID);
        if (!user) return res.status(404).json({ error: 'Usuario no encontrado' })

        const bdBooking = await booking.save()
        // @ts-ignore - poblar es un método definido en el schema
        const populated = await bdBooking.poblar()

        // Registro de auditoría para la creación
        auditLogModel.create({
            entity_type: 'booking',
            entity_id: bdBooking._id,
            action: 'CREATE',
            actor_id: req.user.id,
            actor_type: req.user.rol,
            previous_state: null,
            new_state: bdBooking.toJSON(),
            timestamp: new Date()
        }).catch(err => console.error('Error al crear audit log:', err));

        // Registro de auditoría para el pago de la habitación
        auditLogModel.create({
            entity_type: 'booking',
            entity_id: bdBooking._id,
            action: 'PAYMENT',
            actor_id: req.user.id,
            actor_type: req.user.rol,
            previous_state: null,
            new_state: {
                totalPrice: bdBooking.totalPrice,
                pricePerNight: bdBooking.pricePerNight,
                offer: bdBooking.offer,
                totalNights: bdBooking.totalNights,
                payDate: bdBooking.payDate,
                room: bdBooking.room,
                client: bdBooking.client
            },
            timestamp: new Date()
        }).catch(err => console.error('Error al crear audit log de pago:', err));

        sendEmail(user.email, "Reserva confirmada", "newBooking", populated)
        return res.status(201).json(bdBooking)
    }
    catch (error) {
        console.error('Error al crear la reserva:', error);
        return res.status(500).json({ error: 'Error del servidor al crear la reserva' });
    }
}


/**
 * Cancela una reserva
 *
 * @async
 * @function cancelBooking
 *
 * @description
 * Obtiene el ID de la reserva de los parámetros
 * Cancela la reserva si su estado es Abierta y devuelve la reserva cerrada
 * Verifica la validez del ID, la existencia y el estado de la reserva 
 *
 *
 * @param {import('express').Response} res - Objeto de respuesta de Express.
 *
 * @returns {Promise} Respuesta HTTP con:
 * - 200 y el objeto de la reserva si se cancela
 * - 400 si falta el ID o la validación falla
 * - 404 si la reserva no existe
 * - 500 si ocurre un error interno del servidor
 */
export async function cancelBooking(req, res) {
    try {
        const { id } = req.params;
        if (!id) return res.status(400).json({ error: 'No hay ID de la reserva' })
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID' })

        const booking = await bookingDatabaseModel.findById(id);
        if (!booking) return res.status(404).json({ error: 'No hay reserva con este ID' });
        if (booking.status != 'Abierta') return res.status(400).json({ error: 'La reserva no está abierta' })

        // Guardar estado anterior antes de cancelar
        const previousState = booking.toJSON();

        booking.status = 'Cancelada';
        const bookingUpdated = await booking.save();

        // Registro de auditoría para la cancelación
        auditLogModel.create({
            entity_type: 'booking',
            entity_id: bookingUpdated._id,
            action: 'CANCEL',
            actor_id: req.user.id,
            actor_type: req.user.rol,
            previous_state: previousState,
            new_state: bookingUpdated.toJSON(),
            timestamp: new Date()
        }).catch(err => console.error('Error al crear audit log:', err));

        // @ts-ignore - poblar es un método definido en el schema
        const populated = await bookingUpdated.poblar();
        const user = await userDatabaseModel.findById(bookingUpdated.client);
        sendEmail(user.email, "Reserva cancelada", "cancelBooking", populated)

        return res.status(200).json(bookingUpdated);
    }
    catch (error) {
        console.error('Error al cancelar la reserva: ', error);
        return res.status(500).json({ error: 'Error del servidor  al cancelar la reserva' });
    }
}


/**
 * Actualiza una reserva
 *
 * @async
 * @function updateBooking
 *
 * @description
 * Obtiene el ID de la reserva de los parámetros, el ID del cliente de la sesión y los datos nuevos del cuerpo de la solicitud
 * Actualiza la reserva
 * Verifica la existencia y validez de los datos y errores de servidor
 *
 * @param {import('express').Response} res - Objeto de respuesta de Express.
 *
 * @returns {Promise} Respuesta HTTP con:
 * - 200 y el objeto de la reserva si se modifica
 * - 400 si faltan datos obligatorios o la validación falla
 * - 404 si la reserva no existe
 * - 500 si ocurre un error interno del servidor
 */
export async function updateBooking(req, res) {
    try {
        const { id } = req.params;
        const { checkInDate, checkOutDate, guests } = req.body;
        if (!checkInDate || !checkOutDate || !guests) return res.status(400).json({ error: 'Faltan datos necesarios' })
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID' })

        const booking = await bookingDatabaseModel.findById(id);
        if (!booking) return res.status(404).json({ error: 'No hay reserva con este ID' });

        // Guardar estado anterior antes de actualizar
        const previousState = booking.toJSON();

        const bookingData = new BookingEntryData(booking.room, booking.client, checkInDate, checkOutDate, guests);
        bookingData.fromDocument(booking);

        const room = await roomDatabaseModel.findById(booking.room);
        if (guests > room.maxGuests) return res.status(400).json({ error: 'Se supera el límite de huéspedes de la habitación' })

        try {
            await bookingData.validate()
        }
        catch (err) {
            return res.status(400).json({ error: err.message });
        }

        if (bookingData.checkInDate != booking.checkInDate) {
            bookingData.completeBookingData(room.pricePerNight, room.offer);
        }
        else {
            bookingData.completeBookingData(room.pricePerNight, booking.offer);
        }

        const updatedBooking = await bookingData.save();

        // Registro de auditoría para la actualización
        auditLogModel.create({
            entity_type: 'booking',
            entity_id: updatedBooking._id,
            action: 'UPDATE',
            actor_id: req.user.id,
            actor_type: req.user.rol,
            previous_state: previousState,
            new_state: updatedBooking.toJSON(),
            timestamp: new Date()
        }).catch(err => console.error('Error al crear audit log:', err));

        return res.status(200).json(updatedBooking)
    }

    catch (error) {
        console.error('Error al actualizar la reserva:', error);
        return res.status(500).json({ error: 'Error del servidor al actualizar la reserva' })
    }
}


/**
 * Elimina una reserva
 *
 * @async
 * @function deleteBooking
 *
 * @description
 * Obtiene el ID de la reserva de los parámetros
 * Elimina la reserva si está cancelada
 * Verifica la validez del ID, la existencia y el estado de la reserva 
 *
 * @param {import('express').Response} res - Objeto de respuesta de Express.
 *
 * @returns {Promise} Respuesta HTTP con:
 * - 200 y un texto de confirmación si se elimina
 * - 400 si falta el ID o no está cancelada
 * - 404 si la reserva no existe
 * - 500 si ocurre un error interno del servidor 
 */
export async function deleteBooking(req, res) {
    try {
        const { id } = req.params;
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID' })

        const booking = await bookingDatabaseModel.findById(id);
        if (!booking) return res.status(404).json({ error: 'No hay reserva con ese ID' })
        if (booking.status != 'Cancelada') return res.status(400).json({ error: 'Solo se pueden eliminar reservas canceladas' });

        // Guardar estado anterior antes de eliminar
        const previousState = booking.toJSON();

        await bookingDatabaseModel.findByIdAndDelete(id);

        // Registro de auditoría para la eliminación
        auditLogModel.create({
            entity_type: 'booking',
            entity_id: id,
            action: 'DELETE',
            actor_id: req.user.id,
            actor_type: req.user.rol,
            previous_state: previousState,
            new_state: null,
            timestamp: new Date()
        }).catch(err => console.error('Error al crear audit log:', err));

        return res.status(200).json({ status: 'Reserva eliminada correctamente' });
    }
    catch (error) {
        console.error('Error al actualizar la reserva:', error);
        return res.status(500).json({ error: 'Error del servidor al eliminar la reserva' });
    }
}