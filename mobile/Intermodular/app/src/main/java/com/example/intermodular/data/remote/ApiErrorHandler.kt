package com.example.intermodular.data.remote

import android.util.Log
import kotlinx.coroutines.CancellationException
import org.json.JSONObject
import retrofit2.HttpException
import java.io.IOException

/**
 * Objeto para el manejo de los diferentes errores que pueden ocurrir en la API.
 * Permite obtener el mensaje de error proveniente de la API de los diferentes errores
 *
 * @author Axel Zaragoci
 */
object ApiErrorHandler {

    /**
     * Devuelve un mensaje de error listo para ser visto por el usuario desde una excepción
     *
     * @param throwable - Excepción capturada en los métodos que mandan solicitudes a la API
     * @return [String] - Mensaje de error legible para el usuario
     */
    fun getErrorMessage(throwable: Throwable) : String {
        if (throwable is CancellationException) throw throwable
        throwable.printStackTrace()
        return when (throwable) {
            is HttpException -> handleHttpException(throwable)
            is java.net.ConnectException -> "Error de conexión: No se pudo conectar al servidor"
            is java.net.SocketTimeoutException -> "Tiempo de espera agotado"
            is IOException -> throwable.message?.takeIf { it.isNotBlank() }
                ?: "Error de red o al leer la respuesta"
            else -> fallbackMessage(throwable)
        }
    }

    private fun fallbackMessage(throwable: Throwable): String {
        val direct = throwable.message?.takeIf { it.isNotBlank() }
        val fromCause = throwable.cause?.message?.takeIf { it.isNotBlank() }
        if (direct != null) return direct
        if (fromCause != null) return fromCause
        Log.w("ApiErrorHandler", "Excepción sin mensaje: ${throwable::class.java.name}", throwable)
        return "Error (${throwable::class.simpleName}). Comprueba la URL del servidor y la red."
    }

    /**
     * Procesa una excepción HTTP de Retrofit para conseguir el mensaje de error
     * Intenta obtener el cuerpo del error y convertirlo. En caso de falla devuelve el mensaje de error según el código
     *
     *
     * @param e - Excepción HTTP de Retrofit
     * @return [String] - Mensaje de error legible para el usuario
     */
    private fun handleHttpException(e: HttpException): String {
        return try {
            val errorBody = e.response()?.errorBody()?.string()
            if (!errorBody.isNullOrBlank()) {
                parseApiError(errorBody, e.code())
            } else {
                getDefaultMessageForCode(e.code())
            }
        } catch (_: Exception) {
            getDefaultMessageForCode(e.code())
        }
    }

    /**
     * Parsea el cuerpo de la respuesta HTTP en formato JSON
     * Busca el campo "error" y en caso de no encontrarlo devuelve la cadena para su código de error
     *
     * @param errorBody - Cuerpo del error de la respuesta en formato String
     * @param httpCode - Código de la respuesta para usar en caso de no haber campo "error"
     *
     * @return [String] - Mensaje de error sacado del JSON o mensaje para el código de error
     */
    private fun parseApiError(errorBody: String, httpCode: Int): String {
        return try {
            val jsonObject = JSONObject(errorBody)

            if (jsonObject.has("error")) {
                jsonObject.getString("error")
            }
            else {
                getDefaultMessageForCode(httpCode)
            }
        } catch (e: Exception) {
            errorBody.takeIf { it.isNotBlank() } ?: getDefaultMessageForCode(httpCode)
        }
    }

    /**
     * Obtiene un mensaje de error basado en el código de la respuesta
     * Devuelve mensajes entendibles para mostrar a los usuarios
     *
     * @param code - Código de la respuesta HTTP
     * @return [String] - Cadena con el mensaje correspondiente a su código
     */
    private fun getDefaultMessageForCode(code: Int): String {
        return when (code) {
            400 -> "Datos incorrectos"
            401 -> "No autorizado. Por favor, inicia sesión nuevamente"
            403 -> "Acceso prohibido"
            404 -> "Recurso no encontrado"
            500 -> "Error interno del servidor"
            else -> "Error del servidor (Código $code)"
        }
    }
}