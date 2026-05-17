/**
 * Envío de correo desactivado (no se usa SMTP en este proyecto).
 * Se mantienen stubs por compatibilidad si algún módulo importa estas funciones.
 */

/** @returns {Promise<void>} */
export async function connectEmail() {
    // noop
}

/**
 * @returns {Promise<null>}
 */
export async function sendEmail() {
    return null;
}
