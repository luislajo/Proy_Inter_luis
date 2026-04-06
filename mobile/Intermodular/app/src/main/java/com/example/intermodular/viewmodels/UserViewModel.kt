package com.example.intermodular.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.intermodular.data.remote.ApiErrorHandler
import com.example.intermodular.data.remote.auth.SessionManager
import com.example.intermodular.data.remote.dto.UpdateUserRequestDto
import com.example.intermodular.data.repository.UserRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import com.example.intermodular.models.UserModel
import kotlinx.coroutines.launch
import okhttp3.MultipartBody

/**
 * Estado de UI para la pantalla de usuario.
 *
 * Contiene:
 * - isLoading → indica si hay una operación en curso (carga, actualización, etc.)
 * - user → datos actuales del usuario autenticado
 *
 * @author Ian Rodriguez
 */
data class UserUiState(
    val isLoading: Boolean = false,
    val user: UserModel? = null
)

/**
 * ViewModel encargado de gestionar toda la lógica relacionada con el usuario:
 * - Obtener datos del perfil
 * - Actualizar datos personales
 * - Cambiar la foto de perfil
 * - Cambiar contraseña
 *
 * Utiliza:
 * - [UserRepository] para operaciones remotas
 * - [SessionManager] para obtener información de sesión
 * - [ApiErrorHandler] para transformar errores técnicos en mensajes legibles
 *
 * Expone:
 * - [uiState] → Estado observable de la pantalla
 * - [errorMessage] → Mensaje de error observable
 *
 * @author Ian Rodriguez
 *
 * @param repository - Repositorio encargado de las llamadas a la API
 * @param sessionManager - Gestor de sesión del usuario autenticado
 */
class UserViewModel(
    private val repository: UserRepository,
    private val sessionManager: SessionManager
) : ViewModel() {

    // Estado interno mutable
    private val _uiState = MutableStateFlow(UserUiState())

    // Estado público inmutable observado por la UI
    val uiState: StateFlow<UserUiState> = _uiState

    // Estado interno para mensajes de error
    private val _errorMessage = MutableStateFlow<String?>(null)

    // Flujo público de errores
    val errorMessage: StateFlow<String?> = _errorMessage

    // Al inicializar el ViewModel, se cargan los datos del usuario
    init {
        refresh()
    }

    /**
     * Obtiene los datos del usuario autenticado desde el servidor.
     *
     * Flujo:
     * 1. Activa estado de carga.
     * 2. Llama a repository.getMe().
     * 3. Actualiza el estado con el usuario recibido.
     * 4. En caso de error, desactiva carga y publica mensaje de error.
     */
    fun refresh() {
        val userId = SessionManager.getUserId()
        if (userId == null) {
            _errorMessage.value = "User is not logged in"
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true)

            runCatching {
                repository.getMe()
            }.onSuccess { user ->
                _uiState.value = UserUiState(
                    isLoading = false,
                    user = user
                )
            }.onFailure { throwable ->
                _uiState.value = _uiState.value.copy(isLoading = false)
                _errorMessage.value =
                    ApiErrorHandler.getErrorMessage(throwable)
            }
        }
    }

    /**
     * Actualiza la foto de perfil del usuario.
     *
     * Requiere que el usuario esté previamente cargado.
     * Envía la imagen como multipart al backend.
     *
     * @param photoPart - Imagen convertida a MultipartBody.Part
     */
    fun updatePhoto(photoPart: MultipartBody.Part) {
        val currentUser = _uiState.value.user ?: run {
            _errorMessage.value = "No hay datos del usuario cargados"
            return
        }

        viewModelScope.launch {
            runCatching {
                repository.updateProfilePhoto(currentUser, photoPart)
            }.onSuccess { updatedUser ->
                _uiState.value = _uiState.value.copy(user = updatedUser)
            }.onFailure { throwable ->
                _errorMessage.value =
                    ApiErrorHandler.getErrorMessage(throwable)
            }
        }
    }

    /**
     * Actualiza los datos personales del usuario.
     *
     * Construye un [UpdateUserRequestDto] con los nuevos valores
     * y lo envía al repositorio.
     *
     * Flujo:
     * 1. Verifica que exista un usuario cargado.
     * 2. Activa estado de carga.
     * 3. Llama a repository.updateUser().
     * 4. Actualiza el estado con el usuario actualizado.
     * 5. Maneja errores si ocurren.
     *
     * @param firstName - Nuevo nombre
     * @param lastName - Nuevo apellido
     * @param email - Nuevo email
     * @param dni - Nuevo DNI
     * @param phoneNumber - Nuevo teléfono (nullable)
     * @param birthDateIso - Fecha de nacimiento en formato ISO
     * @param cityName - Nueva ciudad
     * @param gender - Nuevo género
     */
    fun updateUser(
        firstName: String,
        lastName: String,
        email: String,
        dni: String,
        phoneNumber: Long?,
        birthDateIso: String,
        cityName: String,
        gender: String
    ) {
        val current = _uiState.value.user ?: run {
            _errorMessage.value = "No hay usuario cargado"
            return
        }

        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true)

            val body = UpdateUserRequestDto(
                id = SessionManager.getUserId(),
                firstName = firstName,
                lastName = lastName,
                email = email,
                dni = dni,
                phoneNumber = phoneNumber,
                birthDate = birthDateIso,
                cityName = cityName,
                gender = gender,
                imageRoute = current.imageRoute
            )

            runCatching {
                repository.updateUser(body)
            }.onSuccess { updated ->
                _uiState.value =
                    _uiState.value.copy(isLoading = false, user = updated)
            }.onFailure { throwable ->
                _uiState.value =
                    _uiState.value.copy(isLoading = false)
                _errorMessage.value =
                    ApiErrorHandler.getErrorMessage(throwable)
            }
        }
    }

    /**
     * Cambia la contraseña del usuario autenticado.
     *
     * Flujo:
     * 1. Activa estado de carga.
     * 2. Llama a repository.changePassword().
     * 3. Desactiva carga si se completa correctamente.
     * 4. Publica mensaje de error si falla.
     *
     * @param oldPassword - Contraseña actual
     * @param newPassword - Nueva contraseña
     * @param repeatNewPassword - Repetición de la nueva contraseña
     */
    fun changePassword(
        oldPassword: String,
        newPassword: String,
        repeatNewPassword: String
    ) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true)

            runCatching {
                repository.changePassword(
                    oldPassword,
                    newPassword,
                    repeatNewPassword
                )
            }.onSuccess {
                _uiState.value =
                    _uiState.value.copy(isLoading = false)
            }.onFailure { throwable ->
                _uiState.value =
                    _uiState.value.copy(isLoading = false)
                _errorMessage.value =
                    ApiErrorHandler.getErrorMessage(throwable)
            }
        }
    }

    /**
     * Limpia el mensaje de error actual.
     * Se utiliza cuando la UI ya ha mostrado el error.
     */
    fun clearError() {
        _errorMessage.value = null
    }
}