import { bookingDatabaseModel, BookingEntryData } from "../models/bookingModel.js";
import { roomDatabaseModel } from "../models/roomsModel.js"
import mongoose from "mongoose";
import { finalizeBookingDocumentIfPast, finalizePastBookings } from "../jobs/finalizePastBookings.js";
import { userDatabaseModel } from "../models/usersModel.js";
import { sendEmail } from "../lib/mail/mailing.js";
import { auditLogModel } from "../models/auditLogModel.js";
import puppeteer from "puppeteer";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const DEFAULT_INVOICE_LOGO = path.resolve(__dirname, "../../assets/invoice-logo.png");

const INVOICE_PREFIX = process.env.INVOICE_PREFIX ?? "FAC";
const INVOICE_SEPARATOR = process.env.INVOICE_SEPARATOR ?? "-";
const INVOICE_SEQUENCE_PADDING = Number(process.env.INVOICE_SEQUENCE_PADDING ?? 6);
const DEFAULT_TAX_RATE = Number(process.env.INVOICE_DEFAULT_TAX_RATE ?? 21);

const HOTEL_NAME = process.env.HOTEL_NAME ?? "Hotel Pere Maria";
const HOTEL_TAX_ID = process.env.HOTEL_TAX_ID ?? "B84729163";
const HOTEL_ADDRESS =
    process.env.HOTEL_ADDRESS ?? "Av. de la Marina Baixa, 15, 03502 Benidorm , Alicante";

/**
 * @file Controlador de reservas: CRUD, pago con factura embebida, PDF y listado `/invoices`.
 * Helpers de cálculo y HTML de factura al inicio del archivo.
 */

/**
 * Redondea un importe a dos decimales para totales de factura.
 * @param {unknown} value
 * @returns {number}
 */
function toMoney(value) {
    return Number((Number(value || 0)).toFixed(2));
}

/**
 * Formatea una fecha para textos de factura (locale `es-ES`).
 * @param {string|Date|undefined|null} dateLike
 * @returns {string}
 */
function formatDate(dateLike) {
    if (!dateLike) return "";
    const d = new Date(dateLike);
    return d.toLocaleDateString("es-ES");
}

/**
 * Escapa caracteres HTML al generar la plantilla de factura (mitiga XSS en datos mostrados).
 * @param {unknown} s
 * @returns {string}
 */
function escapeHtml(s) {
    if (s == null || s === undefined) return "";
    return String(s)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}

/**
 * Normaliza `body.extras` del pago o parche de factura: nombre, cantidad, precio unitario y línea total.
 * @param {Record<string, unknown>|null|undefined} body
 * @returns {{ name: string, quantity: number, unitPrice: number, total: number }[]}
 */
function mapExtrasFromRequestBody(body) {
    const extrasInput = Array.isArray(body?.extras) ? body.extras : [];
    return extrasInput
        .map((extra) => ({
            name: String(extra?.name || "").trim(),
            quantity: Math.max(1, Number(extra?.quantity ?? 1)),
            unitPrice: Math.max(0, Number(extra?.unitPrice ?? 0))
        }))
        .filter((extra) => extra.name.length > 0)
        .map((extra) => ({ ...extra, total: toMoney(extra.quantity * extra.unitPrice) }));
}

/**
 * Descuento de noches = % de oferta de la habitación (`room.offer`) sobre tarifa de catálogo.
 * El importe cobrado por noche (`booking.pricePerNight`) ya incorpora ese descuento.
 *
 * @param {import("mongoose").Document|object} booking - Reserva con noches y precios.
 * @param {{ name: string, quantity: number, unitPrice: number, total: number }[]} mappedExtras - Líneas extra ya mapeadas.
 * @param {Record<string, unknown>} body - Opcional `taxRate` (porcentaje IVA); si falta usa env.
 * @param {object|null|undefined} room - Documento habitación lean (precio catálogo y oferta).
 * @returns {{
 *   nightsSubtotal: number,
 *   nightsListSubtotal: number,
 *   offerPercent: number,
 *   extrasSubtotal: number,
 *   discountAmount: number,
 *   taxRate: number,
 *   taxAmount: number,
 *   total: number
 * }}
 */
function applyInvoiceMath(booking, mappedExtras, body, room) {
    const extrasSubtotal = toMoney(mappedExtras.reduce((acc, extra) => acc + extra.total, 0));
    const offerPercent = Math.min(100, Math.max(0, Number(room?.offer ?? booking.offer ?? 0)));
    const listPricePerNight =
        room?.pricePerNight != null
            ? toMoney(room.pricePerNight)
            : offerPercent > 0 && offerPercent < 100
              ? toMoney(toMoney(booking.pricePerNight) / (1 - offerPercent / 100))
              : toMoney(booking.pricePerNight);
    const nightsListSubtotal = toMoney(booking.totalNights * listPricePerNight);
    const nightsSubtotal = toMoney(booking.totalNights * toMoney(booking.pricePerNight));
    const discountAmount =
        offerPercent > 0 ? toMoney(Math.max(0, nightsListSubtotal - nightsSubtotal)) : 0;
    const taxRate = Number(body?.taxRate ?? DEFAULT_TAX_RATE);
    const taxableBase = Math.max(0, nightsSubtotal + extrasSubtotal);
    const taxAmount = toMoney(taxableBase * (taxRate / 100));
    const total = toMoney(taxableBase + taxAmount);
    return {
        nightsSubtotal,
        nightsListSubtotal,
        offerPercent,
        extrasSubtotal,
        discountAmount,
        taxRate,
        taxAmount,
        total
    };
}

/**
 * Genera filas HTML de subtotal de noches; si hay descuento respecto al catálogo, muestra desglose.
 * @param {object} breakdown - Objeto tipo `invoiceBreakdown` (subtotales y porcentaje oferta).
 * @returns {string} Fragmento HTML (`<tr>...</tr>`).
 */
function renderInvoiceNightsSubtotalRows(breakdown) {
    const disc = toMoney(breakdown.discountAmount);
    const offerP = toMoney(breakdown.offerPercent ?? 0);
    const listN =
        breakdown.nightsListSubtotal != null ? toMoney(breakdown.nightsListSubtotal) : null;
    const ns = toMoney(breakdown.nightsSubtotal);
    if (disc > 0.005 && listN != null && listN > ns + 0.005) {
        return `
          <tr><td>Precio catálogo (noches):</td><td>${listN.toFixed(2)} EUR</td></tr>
          <tr><td>Descuento habitación (${offerP.toFixed(0)} %):</td><td>- ${disc.toFixed(2)} EUR</td></tr>
          <tr><td>Subtotal noches:</td><td>${ns.toFixed(2)} EUR</td></tr>`;
    }
    return `<tr><td>Subtotal noches:</td><td>${ns.toFixed(2)} EUR</td></tr>`;
}

/**
 * Bloque HTML opcional con datos de empresa del cliente (facturación B2B).
 * @param {{ name?: string|null, taxId?: string|null, address?: string|null }|null|undefined} company
 * @returns {string} HTML vacío si no hay nombre de empresa.
 */
function renderCompanySection(company) {
    if (!company?.name) return "";
    return `
    <div class="section">
      <p class="section-title">Datos de empresa</p>
      <p><strong>Empresa:</strong> ${company.name}</p>
      <p><strong>CIF/NIF:</strong> ${company.taxId || "-"}</p>
      <p><strong>Dirección:</strong> ${company.address || "-"}</p>
    </div>`;
}

/**
 * Imagen del logo en el PDF: data URI desde archivo (PNG/JPG).
 * Ruta: INVOICE_LOGO_PATH o api/assets/invoice-logo.png (copiar el logo del WPF).
 */
function invoiceLogoImgHtml() {
    const logoPath = process.env.INVOICE_LOGO_PATH || DEFAULT_INVOICE_LOGO;
    try {
        if (!fs.existsSync(logoPath)) return "";
        const buf = fs.readFileSync(logoPath);
        const ext = path.extname(logoPath).toLowerCase();
        const mime =
            ext === ".png"
                ? "image/png"
                : ext === ".jpg" || ext === ".jpeg"
                  ? "image/jpeg"
                  : "image/png";
        const src = `data:${mime};base64,${buf.toString("base64")}`;
        return `<img class="invoice-logo" src="${src}" alt="" />`;
    } catch {
        return "";
    }
}

/**
 * Construye el HTML completo de la factura para renderizarlo a PDF con Puppeteer.
 * @param {{ booking: object, client: object|null|undefined, room: object|null|undefined }} args
 * @returns {string}
 */
function buildInvoiceHtml({ booking, client, room }) {
    const breakdown = booking.invoiceBreakdown ?? {};
    const extras = Array.isArray(breakdown.extras) ? breakdown.extras : [];
    const extrasRows = extras.length > 0
        ? extras.map((extra) => `
            <tr>
              <td>${extra.name}</td>
              <td>${extra.quantity}</td>
              <td>${toMoney(extra.unitPrice).toFixed(2)} EUR</td>
              <td>${toMoney(extra.total).toFixed(2)} EUR</td>
            </tr>
          `).join("")
        : `<tr><td colspan="4">Sin extras</td></tr>`;

    const logoBlock = invoiceLogoImgHtml();

    return `<!doctype html>
    <html>
      <head>
        <meta charset="utf-8" />
        <style>
          body { font-family: Arial, sans-serif; color: #000; font-size: 12px; margin: 24px; line-height: 1.35; }
          .section { margin-bottom: 14px; }
          .section-title { font-weight: bold; margin: 0 0 6px 0; }
          p { margin: 2px 0; }
          .title-row { margin-bottom: 12px; }
          .title-row h1 { margin: 0 0 4px 0; font-size: 16px; font-weight: bold; }
          .invoice-logo { max-height: 80px; max-width: 260px; object-fit: contain; display: block; margin-bottom: 10px; }
          table.extras { width: 100%; border-collapse: collapse; margin-top: 6px; font-size: 12px; }
          table.extras th, table.extras td { border-bottom: 1px solid #999; padding: 4px 6px 4px 0; text-align: left; }
          table.extras th { font-weight: bold; border-bottom: 1px solid #000; }
          .totals { margin-top: 12px; width: 280px; margin-left: auto; border-collapse: collapse; font-size: 12px; }
          .totals td { padding: 2px 0; border: none; }
          .totals tr.total td { font-weight: bold; border-top: 1px solid #000; padding-top: 6px; }
        </style>
      </head>
      <body>
        ${logoBlock}
        <div class="title-row">
          <h1>Factura ${booking.invoice_number || "-"}</h1>
          <p>Fecha: ${formatDate(booking.invoiceDate || booking.payDate || new Date())}</p>
        </div>

        <div class="section">
          <p class="section-title">Emisor</p>
          <p>${escapeHtml(HOTEL_NAME)}</p>
          <p>NIF: ${escapeHtml(HOTEL_TAX_ID)}</p>
          <p>${escapeHtml(HOTEL_ADDRESS)}</p>
        </div>

        <div class="section">
          <p class="section-title">Cliente</p>
          <p>${client?.firstName || ""} ${client?.lastName || ""}</p>
          <p>Email: ${client?.email || "-"} · DNI: ${client?.dni || "-"}</p>
          <p>Ciudad fiscal: ${client?.cityName || "-"}</p>
        </div>

        ${renderCompanySection(booking.invoiceCompany)}

        <div class="section">
          <p class="section-title">Estancia</p>
          <p>Habitación ${room?.roomNumber || "-"} (${room?.type || "-"})</p>
          <p>Extras habitación: ${Array.isArray(room?.extras) && room.extras.length ? escapeHtml(room.extras.join(", ")) : "—"}</p>
          <p>${formatDate(booking.checkInDate)} – ${formatDate(booking.checkOutDate)} · ${booking.totalNights} noches · ${toMoney(booking.pricePerNight).toFixed(2)} EUR/noche</p>
        </div>

        <div class="section">
          <p class="section-title">Extras facturados</p>
          <table class="extras">
            <thead>
              <tr>
                <th>Concepto</th>
                <th>Cant.</th>
                <th>P. unit.</th>
                <th>Total</th>
              </tr>
            </thead>
            <tbody>
              ${extrasRows}
            </tbody>
          </table>
        </div>

        <table class="totals">
          ${renderInvoiceNightsSubtotalRows(breakdown)}
          <tr><td>Subtotal extras:</td><td>${toMoney(breakdown.extrasSubtotal).toFixed(2)} EUR</td></tr>
          <tr><td>IVA (${toMoney(breakdown.taxRate).toFixed(2)} %):</td><td>${toMoney(breakdown.taxAmount).toFixed(2)} EUR</td></tr>
          <tr class="total"><td>Total:</td><td>${toMoney(breakdown.total).toFixed(2)} EUR</td></tr>
        </table>
      </body>
    </html>`;
}

/**
 * Genera el siguiente número de factura del año en curso (`INVOICE_PREFIX`, año, secuencia acolchada).
 * @async
 * @returns {Promise<string>}
 */
async function generateNextInvoiceNumber() {
    const currentYear = new Date().getFullYear();
    const regex = new RegExp(`^${INVOICE_PREFIX}${INVOICE_SEPARATOR}${currentYear}${INVOICE_SEPARATOR}(\\d+)$`);
    const latestWithInvoice = await bookingDatabaseModel
        .findOne({ invoice_number: { $regex: regex } })
        .sort({ invoice_number: -1 })
        .lean();

    let nextSequence = 1;
    if (latestWithInvoice?.invoice_number) {
        const match = latestWithInvoice.invoice_number.match(regex);
        const current = Number(match?.[1] ?? 0);
        if (Number.isFinite(current) && current > 0) nextSequence = current + 1;
    }
    const padded = String(nextSequence).padStart(INVOICE_SEQUENCE_PADDING, "0");
    return `${INVOICE_PREFIX}${INVOICE_SEPARATOR}${currentYear}${INVOICE_SEPARATOR}${padded}`;
}
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

        const updated = await finalizeBookingDocumentIfPast(booking);
        return res.status(200).json(updated);
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
        await finalizePastBookings();
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

        await finalizePastBookings();
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

        await finalizePastBookings();
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

        booking.status = 'Cancelada';
        const bookingUpdated = await booking.save();

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
        if (booking.status !== 'Abierta') return res.status(400).json({ error: 'Solo se pueden modificar reservas abiertas' });

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

        await bookingDatabaseModel.findByIdAndDelete(id);

        return res.status(200).json({ status: 'Reserva eliminada correctamente' });
    }
    catch (error) {
        console.error('Error al actualizar la reserva:', error);
        return res.status(500).json({ error: 'Error del servidor al eliminar la reserva' });
    }
}

/**
 * Registra el pago de una reserva en la auditoría
 *
 * @async
 * @function payBooking
 *
 * @description
 * Obtiene el ID de la reserva de los parámetros
 * Registra en el log de auditoría el pago realizado
 *
 * @param {import('express').Response} res - Objeto de respuesta de Express.
 *
 * @returns {Promise} Respuesta HTTP
 */
export async function payBooking(req, res) {
    try {
        const { id } = req.params;
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID válido' });

        const booking = await bookingDatabaseModel.findById(id);
        if (!booking) return res.status(404).json({ error: 'No hay reserva con este ID' });
        // Abierta: flujo normal. Finalizada: permite emitir factura / registrar pago tras checkout automático.
        if (booking.status !== 'Abierta' && booking.status !== 'Finalizada') {
            return res.status(400).json({ error: 'Solo se pueden facturar reservas abiertas o finalizadas (no canceladas)' });
        }

        const mappedExtras = mapExtrasFromRequestBody(req.body);
        const room = await roomDatabaseModel.findById(booking.room).lean();
        const calc = applyInvoiceMath(booking, mappedExtras, req.body, room);

        if (!booking.invoice_number) {
            booking.invoice_number = await generateNextInvoiceNumber();
            booking.invoiceDate = new Date();
        }

        booking.invoiceIssuer = {
            name: HOTEL_NAME,
            taxId: HOTEL_TAX_ID,
            address: HOTEL_ADDRESS
        };
        booking.invoiceCompany = {
            name: req.body?.company?.name || null,
            taxId: req.body?.company?.taxId || null,
            address: req.body?.company?.address || null
        };
        booking.invoiceBreakdown = {
            nightsSubtotal: calc.nightsSubtotal,
            nightsListSubtotal: calc.nightsListSubtotal,
            offerPercent: calc.offerPercent,
            extrasSubtotal: calc.extrasSubtotal,
            discountAmount: calc.discountAmount,
            taxRate: calc.taxRate,
            taxAmount: calc.taxAmount,
            total: calc.total,
            extras: mappedExtras
        };

        booking.totalPrice = calc.total;
        booking.payDate = new Date();
        const bdBooking = await booking.save();

        // Registro de auditoría para el pago de la habitación
        auditLogModel.create({
            entity_type: 'booking',
            entity_id: bdBooking._id,
            action: 'PAYMENT',
            actor_id: req.user.id,
            actor_type: req.user.rol,
            previous_state: null, // Depending on requirements, previous state might be the unpaid booking
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

        return res.status(200).json({ status: 'Pago registrado correctamente y auditado', booking: bdBooking });
    } catch (error) {
        console.error('Error al procesar el pago de la reserva:', error);
        return res.status(500).json({ error: 'Error del servidor al procesar el pago' });
    }
}

/**
 * Actualiza datos de una factura ya emitida (empresa, extras, IVA). No asigna nuevo `invoice_number`.
 * Emisor (`invoiceIssuer`) fijado en servidor. Roles Admin o Trabajador.
 *
 * @async
 * @function patchBookingInvoice
 * @param {import("express").Request} req - `params.id` reserva; body: `extras`, `taxRate`, `company`.
 * @param {import("express").Response} res
 * @returns {Promise}
 */
export async function patchBookingInvoice(req, res) {
    try {
        const { id } = req.params;
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID válido' });

        const booking = await bookingDatabaseModel.findById(id);
        if (!booking) return res.status(404).json({ error: 'No hay reserva con este ID' });
        if (!booking.invoice_number) {
            return res.status(400).json({ error: 'La reserva no tiene factura; use POST /booking/:id/pay' });
        }

        const mappedExtras = mapExtrasFromRequestBody(req.body);
        const room = await roomDatabaseModel.findById(booking.room).lean();
        const calc = applyInvoiceMath(booking, mappedExtras, req.body, room);

        booking.invoiceIssuer = {
            name: HOTEL_NAME,
            taxId: HOTEL_TAX_ID,
            address: HOTEL_ADDRESS
        };
        booking.invoiceCompany = {
            name: req.body?.company?.name || null,
            taxId: req.body?.company?.taxId || null,
            address: req.body?.company?.address || null
        };
        booking.invoiceBreakdown = {
            nightsSubtotal: calc.nightsSubtotal,
            nightsListSubtotal: calc.nightsListSubtotal,
            offerPercent: calc.offerPercent,
            extrasSubtotal: calc.extrasSubtotal,
            discountAmount: calc.discountAmount,
            taxRate: calc.taxRate,
            taxAmount: calc.taxAmount,
            total: calc.total,
            extras: mappedExtras
        };
        booking.totalPrice = calc.total;

        const bdBooking = await booking.save();
        return res.status(200).json(bdBooking);
    } catch (error) {
        console.error('Error al actualizar datos de factura:', error);
        return res.status(500).json({ error: 'Error del servidor al actualizar la factura' });
    }
}

/**
 * Devuelve la factura en PDF (HTML + Puppeteer). Cliente solo la suya; staff según política de token.
 *
 * @async
 * @function getBookingInvoicePdf
 * @param {import("express").Request} req - `params.id` de la reserva; usuario en `req.user`.
 * @param {import("express").Response} res - `application/pdf` o JSON de error.
 * @returns {Promise}
 */
export async function getBookingInvoicePdf(req, res) {
    let browser;
    try {
        const { id } = req.params;
        if (!mongoose.isValidObjectId(id)) return res.status(400).json({ error: 'No es un ID válido' });

        const booking = await bookingDatabaseModel.findById(id).lean();
        if (!booking) return res.status(404).json({ error: "No existe la reserva" });
        if (!booking.invoice_number) return res.status(400).json({ error: "La reserva aún no tiene factura asignada" });

        if (req.user.rol === "Usuario" && String(req.user.id) !== String(booking.client)) {
            return res.status(403).json({ error: "No autorizado para descargar esta factura" });
        }

        const [client, room] = await Promise.all([
            userDatabaseModel.findById(booking.client).lean(),
            roomDatabaseModel.findById(booking.room).lean()
        ]);

        const html = buildInvoiceHtml({ booking, client, room });
        browser = await puppeteer.launch({
            headless: true,
            args: ["--no-sandbox", "--disable-setuid-sandbox"]
        });
        const page = await browser.newPage();
        await page.setContent(html, { waitUntil: "networkidle0" });
        const pdfBuffer = await page.pdf({
            format: "A4",
            printBackground: true,
            margin: { top: "20px", right: "20px", bottom: "20px", left: "20px" }
        });

        res.setHeader("Content-Type", "application/pdf");
        res.setHeader("Content-Disposition", `attachment; filename="factura-${booking.invoice_number}.pdf"`);
        return res.status(200).send(pdfBuffer);
    } catch (error) {
        console.error("Error al generar la factura en PDF:", error);
        return res.status(500).json({ error: "Error del servidor al generar la factura" });
    } finally {
        if (browser) await browser.close();
    }
}

/**
 * Escapa metacaracteres de regex para usar el string como literal en `RegExp` (p. ej. DNI en consulta).
 * @param {unknown} s
 * @returns {string}
 */
function escapeRegex(s) {
    return String(s).replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/**
 * Listado de reservas que tienen `invoice_number` (historial de facturas).
 * - Rol Usuario: solo las propias (query ignorado).
 * - Admin/Trabajador: todas, o filtro `?userId=` o `?dni=` (búsqueda literal de DNI/NIE en usuarios).
 *
 * @async
 * @function getInvoicesByUserId
 * @param {import("express").Request} req
 * @param {import("express").Response} res - JSON `{ total, invoices }`.
 * @returns {Promise}
 */
export async function getInvoicesByUserId(req, res) {
    try {
        const role = req.user?.rol;
        const isStaff = role === "Admin" || role === "Trabajador";

        const baseQuery = () =>
            bookingDatabaseModel
                .find({ invoice_number: { $ne: null } })
                .sort({ invoiceDate: -1, payDate: -1 })
                .select("_id room client checkInDate checkOutDate totalNights totalPrice invoice_number invoiceDate invoiceBreakdown status")
                .lean();

        if (role === "Usuario") {
            const invoices = await bookingDatabaseModel
                .find({ client: req.user.id, invoice_number: { $ne: null } })
                .sort({ invoiceDate: -1, payDate: -1 })
                .select("_id room client checkInDate checkOutDate totalNights totalPrice invoice_number invoiceDate invoiceBreakdown status")
                .lean();
            return res.status(200).json({
                total: invoices.length,
                invoices
            });
        }

        if (!isStaff) {
            return res.status(403).json({ error: "No autorizado" });
        }

        const userIdRaw = req.query.userId;
        const dniRaw = typeof req.query.dni === "string" ? req.query.dni.trim() : "";
        const hasUserId = userIdRaw != null && String(userIdRaw).trim().length > 0;
        const hasDni = dniRaw.length > 0;

        if (!hasUserId && !hasDni) {
            const invoices = await baseQuery();
            return res.status(200).json({
                total: invoices.length,
                invoices
            });
        }

        let clientId = null;
        if (hasUserId) {
            const userId = String(userIdRaw).trim();
            if (!mongoose.isValidObjectId(userId)) {
                return res.status(400).json({ error: "userId no válido" });
            }
            clientId = userId;
        } else {
            const user = await userDatabaseModel.findOne({
                dni: new RegExp(`^${escapeRegex(dniRaw)}$`, "i")
            }).lean();
            if (!user) {
                return res.status(404).json({ error: "Usuario no encontrado con ese DNI/NIE" });
            }
            clientId = user._id;
        }

        const invoices = await bookingDatabaseModel
            .find({ client: clientId, invoice_number: { $ne: null } })
            .sort({ invoiceDate: -1, payDate: -1 })
            .select("_id room client checkInDate checkOutDate totalNights totalPrice invoice_number invoiceDate invoiceBreakdown status")
            .lean();

        return res.status(200).json({
            total: invoices.length,
            invoices
        });
    } catch (error) {
        console.error("Error al obtener historial de facturas:", error);
        return res.status(500).json({ error: "Error del servidor al obtener historial de facturas" });
    }
}
