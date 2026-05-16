import { SignJWT } from 'jose';
import { comparePassword } from "../services/password.service.js";
import { userDatabaseModel } from '../models/usersModel.js';
import dotenv from 'dotenv';
dotenv.config();

/**
 * Controlador para el login de usuarios
 * @function login
 * @async
 * @description
 * Recibe el Email y la contraseña desde el cuerpo de la solicitud
 * Busca el usuario en la base de datos por su Email
 * Compara la contraseña proporcionada con la almacenada en la base de datos
 * Si las credenciales son válidas, genera un token JWT con la información del usuario (id, rol, vipStatus)
 * Devuelve el token al cliente
 * @param {import('express').Request} req
 * @param {import('express').Response} res
 * @returns {Promise} - Respuesta HTTP con:
 * - Código de estado 200 y el token si las credenciales son válidas
 * - Código de estado 400 y mensaje de error si faltan Email o contraseña
 * - Código de estado 401 y mensaje de error si las credenciales son inválidas
 * - Código de estado 500 y mensaje de error si hay un problema del servidor
 */
export async function login(req, res) {
    try {
        const { email, password } = req.body;
        if (!email || !password) return res.status(400).json({ error: 'Email y contraseña son requeridos' });

        const user = await userDatabaseModel.findOne({ email }).select("+password");
        if (!user) return res.status(401).json({ error: 'Credenciales inválidas' });

        const ok = await comparePassword(password, user.password);
        if (!ok) return res.status(401).json({ error: 'Credenciales inválidas' });

        const encoder = new TextEncoder();

        const idStr = String(user._id);
        const token = await new SignJWT({
            id: idStr,
            email: user.email,
            rol: user.rol,
            vipStatus: user.vipStatus,
        })
            .setProtectedHeader({ alg: "HS256" })
            .setIssuedAt()
            .setExpirationTime('1h')
            .setSubject(idStr)
            .sign(encoder.encode(process.env.JWT_SECRET));

        return res.send({ token, rol: user.rol, id: String(user._id) });
    } catch (error) {
        console.error('Error en el login:', error);
        return res.status(500).json({ error: 'Error del servidor' });
    }
}