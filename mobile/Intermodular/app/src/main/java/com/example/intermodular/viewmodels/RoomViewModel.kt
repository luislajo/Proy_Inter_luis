package com.example.intermodular.viewmodels

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.intermodular.data.repository.RoomRepository
import com.example.intermodular.models.Room
import com.example.intermodular.models.RoomFilter
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

/**
 * ViewModel que gestiona el estado y la lógica de negocio para la pantalla del catálogo de habitaciones ([RoomScreen]).
 *
 * Mantiene de forma reactiva la lista de habitaciones, los estados de carga y errores,
 * así como los valores individuales de cada filtro aplicado en la interfaz de usuario.
 *
 * @property repository El repositorio de datos ([RoomRepository]) encargado de servir las habitaciones.
 */
class RoomViewModel(
    private val repository: RoomRepository
) : ViewModel() {
    private val _rooms = MutableStateFlow<List<Room>>(emptyList())
    val rooms: StateFlow<List<Room>> = _rooms

    private val _filteredRooms = MutableStateFlow<List<Room>>(emptyList())
    val filteredRooms: StateFlow<List<Room>> = _filteredRooms

    private val _errorMessage = MutableStateFlow<String?>(null)
    val errorMessage: StateFlow<String?> = _errorMessage

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading

    private val _showFilters = MutableStateFlow(true)
    val showFilters: StateFlow<Boolean> = _showFilters

    private val _type = MutableStateFlow("")
    val type: StateFlow<String> = _type

    private val _minPrice = MutableStateFlow(0)
    val minPrice: StateFlow<Int> = _minPrice

    private val _maxPrice = MutableStateFlow(500)
    val maxPrice: StateFlow<Int> = _maxPrice

    private val _guests = MutableStateFlow("")
    val guests: StateFlow<String> = _guests

    private val _isAvailable = MutableStateFlow(false)
    val isAvailable: StateFlow<Boolean> = _isAvailable

    private val _hasExtraBed = MutableStateFlow(false)
    val hasExtraBed: StateFlow<Boolean> = _hasExtraBed

    private val _hasCrib = MutableStateFlow(false)
    val hasCrib: StateFlow<Boolean> = _hasCrib

    private val _hasOffer = MutableStateFlow(false)
    val hasOffer: StateFlow<Boolean> = _hasOffer

    private val _sortBy = MutableStateFlow("roomNumber")
    val sortBy: StateFlow<String> = _sortBy

    private val _sortOrder = MutableStateFlow("asc")
    val sortOrder: StateFlow<String> = _sortOrder

    private val _currentFilter = MutableStateFlow(RoomFilter())

    init {
        loadRooms()
    }

    fun changeFilterVisibility() {
        _showFilters.value = !_showFilters.value
    }

    fun onTypeChanged(value: String) {
        _type.value = value
    }

    fun onMinPriceChanged(value: Int) {
        _minPrice.value = value
    }

    fun onMaxPriceChanged(value: Int) {
        _maxPrice.value = value
    }

    fun onGuestsChanged(value: String) {
        _guests.value = value
    }

    fun onIsAvailableChanged(value: Boolean) {
        _isAvailable.value = value
    }

    fun onExtraBedChanged(value: Boolean) {
        _hasExtraBed.value = value
    }

    fun onCribChanged(value: Boolean) {
        _hasCrib.value = value
    }

    fun onOfferChanged(value: Boolean) {
        _hasOffer.value = value
    }

    fun onSortByChanged(value: String) {
        _sortBy.value = value
    }

    fun onSortOrderChanged(value: String) {
        _sortOrder.value = value
    }

    /**
     * Recopila todos los valores actuales de los StateFlow de filtros, 
     * construye un objeto [RoomFilter] y desencadena una nueva carga de datos
     * llamando a [loadRooms]. Oculta el panel de filtros tras aplicar.
     */
    fun filter() {
        _currentFilter.value = RoomFilter(
            type = _type.value.ifBlank { null },
            // do not forward server-side availability; reservable rule handled client-side
            isAvailable = null,
            minPrice = _minPrice.value.toDouble(),
            maxPrice = _maxPrice.value.toDouble(),
            guests = _guests.value.toIntOrNull(),
            hasExtraBed = if (_hasExtraBed.value) true else null,
            hasCrib = if (_hasCrib.value) true else null,
            hasOffer = if (_hasOffer.value) true else null,
            sortBy = _sortBy.value,
            sortOrder = _sortOrder.value
        )
        _showFilters.value = false
        loadRooms()
    }

    /**
     * Restablece todos los filtros visuales a sus valores por defecto,
     * limpia el [RoomFilter] actual, oculta el panel de filtros y recarga 
     * la lista completa de habitaciones sin filtrado.
     */
    fun clearFilters() {
        _type.value = ""
        _minPrice.value = 0
        _maxPrice.value = 500
        _guests.value = ""
        _isAvailable.value = false
        _hasExtraBed.value = false
        _hasCrib.value = false
        _hasOffer.value = false
        _sortBy.value = "roomNumber"
        _sortOrder.value = "asc"
        
        _currentFilter.value = RoomFilter()
        _showFilters.value = false
        loadRooms()
    }

    /**
     * Realiza una petición asíncrona al repositorio para cargar la lista de habitaciones
     * aplicando el filtro actual ([_currentFilter]).
     * Gestiona automáticamente los estados [_isLoading] y [_errorMessage].
     */
    fun loadRooms() {
        viewModelScope.launch {
            _isLoading.value = true
            _errorMessage.value = null
            try {
                val rooms = repository.getRooms(_currentFilter.value)
                _rooms.value = rooms
                _filteredRooms.value = rooms
            }
            catch (e: Exception) {
                _errorMessage.value = "Error loading rooms: ${e.message}"
            }
            finally {
                _isLoading.value = false
            }
        }
    }
}
