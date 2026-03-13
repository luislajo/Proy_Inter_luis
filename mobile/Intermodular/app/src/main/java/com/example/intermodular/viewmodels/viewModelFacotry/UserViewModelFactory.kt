package com.example.intermodular.viewmodels.viewModelFactory

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.intermodular.data.remote.auth.SessionManager
import com.example.intermodular.data.repository.RoomRepository
import com.example.intermodular.data.repository.UserRepository
import com.example.intermodular.data.repository.BookingRepository
import com.example.intermodular.viewmodels.RoomViewModel
import com.example.intermodular.viewmodels.UserViewModel

/**
 * Fábrica (Factory) encargada de instanciar [UserViewModel].
 *
 * Es obligatoria en Jetpack Compose cuando el ViewModel necesita recibir argumentos
 * por constructor (en este caso, el [UserRepository]).
 * Asegura que se pase el mismo repositorio cuando se recrea el ViewModel.
 *
 * @property repository La instancia de [UserRepository] que se inyectará en el ViewModel.
 */
class UserViewModelFactory(
    private val repository: UserRepository,
    private val bookingRepository: BookingRepository,
    private val sessionManager: SessionManager
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(UserViewModel::class.java)) {
            @Suppress("UNCHECKED_CAST")
            return UserViewModel(repository, bookingRepository, sessionManager) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class")
    }
}