package com.example.intermodular.viewmodels.viewModelFactory

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.intermodular.data.repository.BookingRepository
import com.example.intermodular.viewmodels.MyAuditHistoryViewModel

class MyAuditHistoryViewModelFactory(
    private val bookingRepository: BookingRepository
) : ViewModelProvider.Factory {

    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(MyAuditHistoryViewModel::class.java)) {
            @Suppress("UNCHECKED_CAST")
            return MyAuditHistoryViewModel(bookingRepository) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class")
    }
}
