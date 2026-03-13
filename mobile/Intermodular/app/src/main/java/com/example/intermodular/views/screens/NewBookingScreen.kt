package com.example.intermodular.views.screens

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.painter.ColorPainter
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.example.intermodular.BuildConfig
import com.example.intermodular.models.Booking
import com.example.intermodular.models.Room
import com.example.intermodular.viewmodels.MyBookingsViewModel
import com.example.intermodular.viewmodels.NewBookingViewModel
import com.example.intermodular.views.components.BookingDataForm
import androidx.compose.runtime.LaunchedEffect

/**
 * Pantalla de crear nueva reserva
 *
 * Componentes externos:
 * - **Formulario de actualizar**: [BookingDataForm] para editar fechas y huéspedes
 * - **Popup de pago**: [PaymentPopup] para simular una pasarela de pago
 *
 *@author Axel Zaragoci
 *
 * @param loading - Estado de carga de datos
 * @param error - Mensaje de error a mostrar (null si no hay error)
 * @param room - Habitación seleccionada para reservar
 * @param guests - Número de huéspedes como String
 * @param startDate - Fecha de inicio seleccionada (en milisegundos)
 * @param endDate - Fecha de fin seleccionada (en milisegundos)
 * @param totalPrice - Precio total calculado para la reserva
 * @param newBooking - Flag para saber si se ha creado la reserva
 * @param navigateToPayment - ID de la reserva a la que navegar para el pago
 * @param onNavigateToPayment - Callback que ejecuta la navegación
 * @param onFormButtonClick - Callback al hacer clic en el botón del formulario (crear reserva)
 * @param onStartDateChange - Callback al cambiar la fecha de entrada
 * @param onEndDateChange - Callback al cambiar la fecha de salida
 * @param onGuestsChange - Callback al cambiar el número de huéspedes
 */
@Composable
fun NewBookingScreen(
    loading : Boolean,
    error : String?,
    room : Room?,
    guests : String,
    startDate : Long?,
    endDate : Long?,
    totalPrice: Double?,
    newBooking: Boolean,
    navigateToPayment: String?,
    onNavigateToPayment: (String) -> Unit,
    onFormButtonClick: () -> Unit,
    onStartDateChange: (Long?) -> Unit,
    onEndDateChange: (Long?) -> Unit,
    onGuestsChange: (String) -> Unit
) {
    LaunchedEffect(navigateToPayment) {
        navigateToPayment?.let {
            onNavigateToPayment(it)
        }
    }
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(20.dp)
    ) {
        if (!newBooking) {
            // ESTADO DE CARGAR
            if (loading) {
                Box (
                    modifier = Modifier
                        .fillMaxSize()
                ) {
                    CircularProgressIndicator(
                        color = MaterialTheme.colorScheme.primary
                    )
                }
            }

            // MENSAJE DE ERROR
            error?.let {
                Text(
                    text = it,
                    color = MaterialTheme.colorScheme.error,
                    style = MaterialTheme.typography.bodyLarge
                )
            }

            // Creación de la ruta de la imagen
            val relativePath = if (room?.mainImage?.startsWith("/") == true)
                room.mainImage.substring(1)
            else
                room?.mainImage

            val imageUrl = if (room?.mainImage?.startsWith("http") == true)
                room.mainImage
            else
                "${BuildConfig.BASE_URL}$relativePath"

            // IMAGEN PRINCIPAL DE LA HABITACIÓN
            AsyncImage(
                model = ImageRequest.Builder(LocalContext.current)
                    .data(imageUrl)
                    .crossfade(true)
                    .build(),
                contentDescription = "Imagen de la habitación",
                contentScale = ContentScale.Crop,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(200.dp),
                error = ColorPainter(MaterialTheme.colorScheme.errorContainer)
            )

            // FORMULARIO DE CREAR RESERVA
            Box(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 30.dp)
            ) {
                BookingDataForm(
                    create = true,
                    startDate = startDate!!,
                    endDate = endDate!!,
                    guests = guests,
                    totalPrice = totalPrice,
                    onButtonClick = onFormButtonClick,
                    onStartDateChange = onStartDateChange,
                    onEndDateChange = onEndDateChange,
                    onGuestsDataChange = onGuestsChange
                )
            }
        }

        // SI HAY RESERVA CREADA
        if (newBooking) {
            Text(
                text = "Nueva reserva creada",
                style = MaterialTheme.typography.titleLarge,
                color = MaterialTheme.colorScheme.primary
            )
        }

    }
}

/**
 * Versión de [NewBookingScreen] con estado y conexión al ViewModel
 *
 * Funciones:
 * 1. Recolectar estados del [NewBookingViewModel] usando [collectAsStateWithLifecycle]
 * 2. Pasar los estados a [NewBookingScreen]
 *
 * @author Axel Zaragoci
 */
@Composable
fun NewBookingState(
    viewModel: NewBookingViewModel,
    onNavigateToPayment: (String) -> Unit
) {
    val isLoading by viewModel.isLoading.collectAsStateWithLifecycle()
    val error by viewModel.errorMessage.collectAsStateWithLifecycle()

    val guests by viewModel.Guests.collectAsStateWithLifecycle()
    val selectedStartDate by viewModel.StartDate.collectAsStateWithLifecycle()
    val selectedEndDate by viewModel.EndDate.collectAsStateWithLifecycle()
    val totalPrice by viewModel.totalPrice.collectAsStateWithLifecycle()
    val newBooking by viewModel.bookingCreated.collectAsStateWithLifecycle()
    val navigateToPayment by viewModel.navigateToPayment.collectAsStateWithLifecycle()
    val room by viewModel.Room.collectAsStateWithLifecycle()

    NewBookingScreen(
        loading = isLoading,
        error = error,
        room = room,
        guests = guests,
        startDate = selectedStartDate,
        endDate = selectedEndDate,
        totalPrice = totalPrice,
        newBooking = newBooking,
        navigateToPayment = navigateToPayment,
        onNavigateToPayment = { 
            viewModel.onPaymentNavigated()
            onNavigateToPayment(it) 
        },
        onFormButtonClick = viewModel::createBooking,
        onStartDateChange = viewModel::onStartDateChange,
        onEndDateChange = viewModel::onEndDateChange,
        onGuestsChange = viewModel::onGuestsChange
    )
}