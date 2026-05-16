package com.example.intermodular.viewmodels.viewModelFacotry

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import com.example.intermodular.data.repository.IncidentRepository
import com.example.intermodular.viewmodels.ReportIncidentViewModel

class ReportIncidentViewModelFactory(
    private val roomId: String,
    private val incidentRepository: IncidentRepository
) : ViewModelProvider.Factory {

    @Suppress("UNCHECKED_CAST")
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        if (modelClass.isAssignableFrom(ReportIncidentViewModel::class.java)) {
            return ReportIncidentViewModel(roomId, incidentRepository) as T
        }
        throw IllegalArgumentException("Unknown ViewModel class")
    }
}
