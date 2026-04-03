import { bookingDatabaseModel, BookingEntryData } from "../models/bookingModel.js";
import { roomDatabaseModel } from "../models/roomsModel.js"
import mongoose from "mongoose";
import { userDatabaseModel } from "../models/usersModel.js";
import { sendEmail } from "../lib/mail/mailing.js";
import { auditLogModel } from "../models/auditLogModel.js";
import puppeteer from "puppeteer";

const INVOICE_PREFIX = process.env.INVOICE_PREFIX ?? "FAC";
const INVOICE_SEPARATOR = process.env.INVOICE_SEPARATOR ?? "-";
const INVOICE_SEQUENCE_PADDING = Number(process.env.INVOICE_SEQUENCE_PADDING ?? 6);
const DEFAULT_TAX_RATE = Number(process.env.INVOICE_DEFAULT_TAX_RATE ?? 21);
const HOTEL_NAME = process.env.HOTEL_NAME ?? "Hotel Intermodular";
const HOTEL_TAX_ID = process.env.HOTEL_TAX_ID ?? "B00000000";
const HOTEL_ADDRESS = process.env.HOTEL_ADDRESS ?? "Dirección fiscal no configurada";

function toMoney(value) {
    return Number((Number(value || 0)).toFixed(2));
}

function formatDate(dateLike) {
    if (!dateLike) return "";
    const d = new Date(dateLike);
    return d.toLocaleDateString("es-ES");
}

function renderCompanySection(company) {
    if (!company?.name) return "";
    return `
    <div class="section">
      <h3>Datos de empresa</h3>
      <p><strong>Empresa:</strong> ${company.name}</p>
      <p><strong>CIF/NIF:</strong> ${company.taxId || "-"}</p>
      <p><strong>Dirección:</strong> ${company.address || "-"}</p>
    </div>`;
}

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

    return `<!doctype html>
    <html>
      <head>
        <meta charset="utf-8" />
        <style>
          body { font-family: Arial, sans-serif; color: #222; margin: 28px; }
          .header { display: flex; justify-content: space-between; margin-bottom: 20px; }
          .section { margin-bottom: 16px; }
          .box { border: 1px solid #ddd; border-radius: 6px; padding: 10px; }
          h1 { margin: 0; font-size: 24px; }
          h3 { margin: 0 0 8px 0; }
          p { margin: 4px 0; }
          table { width: 100%; border-collapse: collapse; margin-top: 10px; }
          th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
          th { background: #f6f6f6; }
          .totals { margin-top: 14px; width: 320px; margin-left: auto; }
          .totals td { border: none; padding: 4px 0; }
          .totals tr.total td { font-size: 18px; font-weight: bold; border-top: 1px solid #ccc; padding-top: 8px; }
        </style>
      </head>
      <body>
        <div class="header">
          <div>
            <h1>Factura ${booking.invoice_number || "-"}</h1>
            <p><strong>Fecha:</strong> ${formatDate(booking.invoiceDate || booking.payDate || new Date())}</p>
          </div>
          <div class="box">
            <p><strong>${HOTEL_NAME}</strong></p>
            <p>NIF: ${HOTEL_TAX_ID}</p>
            <p>${HOTEL_ADDRESS}</p>
          </div>
        </div>

        <div class="section box">
          <h3>Datos del cliente</h3>
          <p><strong>Nombre:</strong> ${client?.firstName || ""} ${client?.lastName || ""}</p>
          <p><strong>Email:</strong> ${client?.email || "-"}</p>
          <p><strong>DNI:</strong> ${client?.dni || "-"}</p>
          <p><strong>Ciudad fiscal:</strong> ${client?.cityName || "-"}</p>
        </div>

        ${renderCompanySection(booking.invoiceCompany)}

        <div class="section box">
          <h3>Detalle de estancia</h3>
          <p><strong>Habitación:</strong> ${room?.roomNumber || "-"} (${room?.type || "-"})</p>
          <p><strong>Fechas:</strong> ${formatDate(booking.checkInDate)} - ${formatDate(booking.checkOutDate)}</p>
          <p><strong>Noches:</strong> ${booking.totalNights}</p>
          <p><strong>Precio por noche:</strong> ${toMoney(booking.pricePerNight).toFixed(2)} EUR</p>
        </div>

        <div class="section">
          <h3>Extras</h3>
          <table>
            <thead>
              <tr>
                <th>Concepto</th>
                <th>Cantidad</th>
                <th>Precio unitario</th>
                <th>Total</th>
              </tr>
            </thead>
            <tbody>
              ${extrasRows}
            </tbody>
          </table>
        </div>

        <table class="totals">
          <tr><td>Subtotal noches:</td><td>${toMoney(breakdown.nightsSubtotal).toFixed(2)} EUR</td></tr>
          <tr><td>Subtotal extras:</td><td>${toMoney(breakdown.extrasSubtotal).toFixed(2)} EUR</td></tr>
          <tr><td>Descuentos:</td><td>- ${toMoney(breakdown.discountAmount).toFixed(2)} EUR</td></tr>
          <tr><td>Impuestos (${toMoney(breakdown.taxRate).toFixed(2)} %):</td><td>${toMoney(breakdown.taxAmount).toFixed(2)} EUR</td></tr>
          <tr class="total"><td>Total:</td><td>${toMoney(breakdown.total).toFixed(2)} EUR</td></tr>
        </table>
      </body>
    </html>`;
}

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

        const extrasInput = Array.isArray(req.body?.extras) ? req.body.extras : [];
        const mappedExtras = extrasInput
            .map((extra) => ({
                name: String(extra?.name || "").trim(),
                quantity: Math.max(1, Number(extra?.quantity ?? 1)),
                unitPrice: Math.max(0, Number(extra?.unitPrice ?? 0))
            }))
            .filter((extra) => extra.name.length > 0)
            .map((extra) => ({ ...extra, total: toMoney(extra.quantity * extra.unitPrice) }));

        const nightsSubtotal = toMoney(booking.totalNights * booking.pricePerNight);
        const extrasSubtotal = toMoney(mappedExtras.reduce((acc, extra) => acc + extra.total, 0));
        const discountAmount = toMoney(Number(req.body?.discountAmount ?? 0));
        const taxableBase = Math.max(0, nightsSubtotal + extrasSubtotal - discountAmount);
        const taxRate = Number(req.body?.taxRate ?? DEFAULT_TAX_RATE);
        const taxAmount = toMoney(taxableBase * (taxRate / 100));
        const total = toMoney(taxableBase + taxAmount);

        if (!booking.invoice_number) {
            booking.invoice_number = await generateNextInvoiceNumber();
            booking.invoiceDate = new Date();
        }

        booking.invoiceCompany = {
            name: req.body?.company?.name || null,
            taxId: req.body?.company?.taxId || null,
            address: req.body?.company?.address || null
        };
        booking.invoiceBreakdown = {
            nightsSubtotal,
            extrasSubtotal,
            discountAmount,
            taxRate,
            taxAmount,
            total,
            extras: mappedExtras
        };

        booking.totalPrice = total;
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

function escapeRegex(s) {
    return String(s).replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/**
 * Historial de facturas:
 * - Usuario: solo las suyas (ignora filtros de query).
 * - Admin/Trabajador: sin query → todas las facturas con número;
 *   ?userId= → por id de cliente; ?dni= → por DNI/NIE (campo dni en users).
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
