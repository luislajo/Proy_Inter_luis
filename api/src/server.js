/**
 * @file Punto de entrada Express: middleware global, estáticos, montaje de routers y arranque (DB, email, job finalización reservas).
 */
import express from "express";
import dotenv from "dotenv";
import cors from 'cors';
import morgan from 'morgan';

import path from "path";
import { fileURLToPath } from "url";
import { connectEmail } from './lib/mail/mailing.js';
import connectDB from './config/db.js';
import { startFinalizePastBookingsJob } from './jobs/finalizePastBookings.js';

import bookingRouter from "./routers/bookingRouter.js";
import roomsRouter from "./routers/roomsRouter.js";
import usersRouter from "./routers/usersRouter.js";
import authRouter from "./routers/authRouter.js";
import photoRouter from "./lib/image/imageRouter.js";
import reviewsRouter from "./routers/reviewsRouter.js";
import auditRouter from "./routers/auditRouter.js";
import invoicesRouter from "./routers/invoicesRouter.js";
dotenv.config();
connectDB().then(() => {
  startFinalizePastBookingsJob();
});
connectEmail();

const PORT = process.env.APP_PORT;
const app = express();

app.use(cors());
app.use(express.json());
app.use(morgan("dev"));




const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const UPLOADS_DIR = path.join(__dirname, "../uploads");

app.use("/uploads", express.static(UPLOADS_DIR));

app.use("/booking", bookingRouter);
//app.use("/bookings", bookingRouter);
app.use("/room", roomsRouter);
app.use("/user", usersRouter);
app.use("/auth", authRouter);
app.use("/image", photoRouter);
app.use("/review", reviewsRouter);
app.use("/audit", auditRouter);
app.use("/invoices", invoicesRouter);

app.listen(PORT, () => {
  console.log(`Servidor en el puerto ${PORT}`);
});
