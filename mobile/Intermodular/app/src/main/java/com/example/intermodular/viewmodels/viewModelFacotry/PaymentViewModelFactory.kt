package com.example.intermodular.viewmodels.viewModelFacotry

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.intermodular.data.repository.BookingRepository
import com.example.intermodular.viewmodels.PaymentViewModel

class PaymentViewModelFactory(
    private val bookingRepository: BookingRepository,
    private val bookingId: String
) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(PaymentViewModel::class.java)) {
            @Suppress("UNCHECKED_CAST")
            return PaymentViewModel(bookingRepository, bookingId) as T
        }
        throw IllegalArgumentException("Clase ViewModel desconocida")
    }
}
