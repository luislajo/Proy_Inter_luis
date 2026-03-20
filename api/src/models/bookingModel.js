import { Schema, Types, model, isValidObjectId } from 'mongoose';
import { roomDatabaseModel } from './roomsModel.js';
import { userDatabaseModel } from '../models/usersModel.js';

/**
 * @typedef {Object} bookingSchema
 * 
 * @property {import('mongoose').Types.ObjectId} room - Identificador de la habitación
 * @property {import('mongoose').Types.ObjectId} client - Identificador del cliente
 * @property {Date} checkInDate - Fecha de inicio de la reserva
 * @property {Date} checkOutDate - Fecha de fin de la reserva
 * @property {Date} payDate - Fecha de pago, como la reserva se paga al hacerse, se indica que el dato sea la fecha de creación de la reserva
 * @property {number} totalPrice - Precio total de la reserva
 * @property {number} pricePerNight - Precio por noche de la reserva (Se guarda para facilitar el mostrar información en las aplicaciones correspondientes)
 * @property {number} offer - Porcentaje de descuento (Se guarda para facilitar el mostrar información en las aplicaciones correspondientes)
 * @property {"Cancelada"|"Abierta"} status - Estado de la reserva, "Finalizada" para las que ya han pasado el checkOutDate, "Cancelada" para las reservas canceladas por el usuario o empleados y "Abierta" para el resto
 * @property {number} guests - Cantidad de huéspedes en la habitación reservada
 * @property {number} totalNights - Cantidad total de noches
 * 
 * @description Documento correspondiente a los datos en MongoDB
*/
const bookingDatabaseSchema = new Schema({
    room: {
        type: Types.ObjectId,
        required: true,
        ref: 'rooms'
    },
    client: {
        type: Types.ObjectId,
        required: true,
        ref: 'users'
    },
    checkInDate: {
        type: Date,
        required: true
    },
    checkOutDate: {
        type: Date,
        required: true
    },
    payDate: {
        type: Date,
        default: Date.now
    },
    totalPrice: {
        type: Number,
        required: true
    },
    pricePerNight: {
        type: Number,
        required: true
    },
    offer: {
        type: Number,
        required: true
    },
    status: {
        type: String,
        enum: ["Cancelada", "Abierta"],
        default: "Abierta"
    },
    guests: {
        type: Number,
        required: true
    },
    totalNights: {
        type: Number,
        required: true
    },
    invoice_number: {
        type: String,
        default: null
    },
    invoiceDate: {
        type: Date,
        default: null
    },
    invoiceCompany: {
        name: { type: String, default: null },
        taxId: { type: String, default: null },
        address: { type: String, default: null }
    },
    invoiceBreakdown: {
        nightsSubtotal: { type: Number, default: null },
        extrasSubtotal: { type: Number, default: 0 },
        discountAmount: { type: Number, default: 0 },
        taxRate: { type: Number, default: 21 },
        taxAmount: { type: Number, default: null },
        total: { type: Number, default: null },
        extras: [{
            name: { type: String, required: true },
            quantity: { type: Number, default: 1 },
            unitPrice: { type: Number, required: true },
            total: { type: Number, required: true }
        }]
    }
}, {
    toJSON: {
        /**
         * @param {{[x: string] : unknown}} _doc 
         * @param {Record<String, any>} ret 
         * @returns 
         */
    }
});

bookingDatabaseSchema.methods.poblar = async function () {
    const userObject = await userDatabaseModel.findById(this.client).lean();
    const roomObject = await roomDatabaseModel.findById(this.room).lean();

    return {
        ...this._doc,
        userObject,
        roomObject
    }
}

bookingDatabaseSchema.set('toJSON', {
    transform: (_doc, ret) => {
        ret.totalPrice = Math.floor(ret.totalPrice);
        ret.pricePerNight = Math.floor(ret.pricePerNight);
        ret.offer = Math.floor(ret.offer);
        ret.guests = Math.floor(ret.guests);
        ret.totalNights = Math.floor(ret.totalNights);
        return ret;
    }
});


export const bookingDatabaseModel = model('booking', bookingDatabaseSchema)

/** Clase que obtiene los datos para la reserva */
export class BookingEntryData {
    /**
     * Crea una nueva entrada de datos
     * @param {import('mongoose').Types.ObjectId} roomID 
     * @param {import('mongoose').Types.ObjectId} clientID 
     * @param {Date} checkInDate 
     * @param {Date} checkOutDate 
     * @param {number} guests 
     */
    constructor(roomID, clientID, checkInDate, checkOutDate, guests) {
        this.roomID = roomID
        this.clientID = clientID
        this.checkInDate = new Date(checkInDate)
        this.checkOutDate = new Date(checkOutDate)
        this.guests = guests
        this.doc = null
        this.ready = false
    }


    /**
     * Completa los datos de la reserva para poder crearla
     * @param {number} pricePerNight 
     * @param {number} offer 
     */
    completeBookingData(pricePerNight, offer) {
        this.offer = offer;

        this.totalNights = Math.ceil(
            (this.checkOutDate.getTime() - this.checkInDate.getTime()) / (1000 * 60 * 60 * 24)
        );

        this.pricePerNight = Math.round(pricePerNight * (1 - offer / 100));

        this.totalPrice = this.totalNights * this.pricePerNight;

        this.ready = true;
    }


    /**
     * Método que valida los datos para evitar errores en la base de datos
     * @throws {Error} Ha ocurrido 1 o más errores de validación. El mensaje contiene los errores
     */
    async validate() {
        function isNumeric(o) {
            return typeof (o) === 'number' && o > 0
        }
        function isValidDate(d) {
            return d instanceof Date && !isNaN(d.getTime());
        }

        const errors = [];
        if (!isValidObjectId(this.roomID)) errors.push("El ID de la habitación es inválido");
        if (!isValidObjectId(this.clientID)) errors.push("El ID del usuario es inválido");

        if (!isValidDate(this.checkInDate)) {
            errors.push("La fecha de check-in no es válida");
        }

        if (!isValidDate(this.checkOutDate)) {
            errors.push("La fecha de check-out no es válida");
        }

        if (isValidDate(this.checkInDate) && isValidDate(this.checkOutDate)) {
            if (this.checkOutDate <= this.checkInDate) {
                errors.push("La fecha de fin no puede ser anterior o igual a la de inicio");
            }

            const today = new Date();
            today.setHours(0, 0, 0, 0);

            if (this.checkInDate < today) {
                errors.push("No se puede hacer una reserva en el pasado");
            }

            if (
                await dateOverlap(
                    this.roomID,
                    this.checkInDate,
                    this.checkOutDate,
                    this.doc?._id ?? null
                )
            ) {
                errors.push("Ya hay reserva en esas fechas");
            }
        }


        if (!isNumeric(this.guests)) errors.push("La cantidad de huéspedes debe ser un número mayor que 0");

        if (errors.length != 0) {
            throw new Error(errors.join(", "));
        }
    }

    /**
     * Método para guardar o actualizar un elemento en la base de datos
     * @returns {Promise}
     */
    save() {
        if (!this.ready) throw new Error("Reserva no lista. Completa la información");
        const doc = this.doc ?? new bookingDatabaseModel();
        const data = {
            room: this.roomID,
            client: this.clientID,
            checkInDate: this.checkInDate.toISOString(),
            checkOutDate: this.checkOutDate.toISOString(),
            totalPrice: this.totalPrice,
            pricePerNight: this.pricePerNight,
            offer: this.offer,
            guests: this.guests,
            totalNights: this.totalNights
        };
        return doc.set(data).save();
    }

    /**
     * Método para almacenar un documento base para las actualizaciones
     * @param {import("mongoose").Document} booking 
     */
    async fromDocument(booking) {
        this.doc = booking;
    }
}

/**
 * Método para saber si hay solapamiento de fechas de reservas de una habitación
 * @param {import("mongoose").Types.ObjectId} roomID 
 * @param {Date} checkInDate 
 * @param {Date} checkOutDate 
 * @param {import("mongoose").Types.ObjectId|null} id 
 * 
 * @returns {Promise|null} Devuelve una promesa de un documento o null. Si se encuentra un documento (hay solapamiento de fechas) y si no se encuentra se devuelve null
 */
async function dateOverlap(roomID, checkInDate, checkOutDate, id = null) {
    const query = {
        room: roomID,
        status: "Abierta",
        checkInDate: { $lt: checkOutDate },
        checkOutDate: { $gt: checkInDate }
    };
    if (id) query._id = { $ne: id }
    return await bookingDatabaseModel.exists(query)
}