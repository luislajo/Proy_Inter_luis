package com.example.intermodular.viewmodels.viewModelFacotry

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.intermodular.data.repository.BookingRepository
import com.example.intermodular.data.repository.ReviewRepository
import com.example.intermodular.data.repository.RoomRepository
import com.example.intermodular.viewmodels.MyBookingDetailsViewModel

/**
 * Fábrica de ViewModels para crear instancias de [MyBookingDetailsViewModel] con las dependencias necesarias
 * @author Axel Zaragoci
 *
 * @param bookingId - ID de la reserva de la que se quieren ver los detalles
 * @param bookingRepository - Repositorio de reservas
 * @param roomRepository - Repositorio de habitaciones
 * @param reviewRepository - Repositorio de reseñas
 */
class MyBookingDetailsViewModelFactory(
    private val bookingId: String,
    private val bookingRepository: BookingRepository,
    private val roomRepository: RoomRepository,
    private val reviewRepository: ReviewRepository
) : ViewModelProvider.Factory {

    /**
     * Crea una instancia de [MyBookingDetailsViewModel]
     * Este método verifica que la clase solicitada sea [MyBookingDetailsViewModel] y si es así, crea una nueva instancia inyectando los repositorios necesarios.
     * Si la clase no es compatible, lanza una excepción.
     *
     * @param modelClass La clase del ViewModel que se desea instanciar
     * @return Una nueva instancia de [T] ([MyBookingDetailsViewModel])
     * @throws IllegalArgumentException Si [modelClass] no es asignable desde [MyBookingDetailsViewModel]
     *
     * @suppress UNCHECKED_CAST La conversión a T es segura porque hemos verificado que modelClass es asignable desde BookingViewModel
     */
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(MyBookingDetailsViewModel::class.java)) {
            @Suppress("UNCHECKED_CAST")
            return MyBookingDetailsViewModel(
                bookingId,
                bookingRepository,
                roomRepository,
                reviewRepository
            ) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class")
    }
}