package com.example.intermodular.views.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.compose.ui.platform.LocalContext
import androidx.navigation.NavHostController
import com.example.intermodular.models.Booking
import com.example.intermodular.models.Room
import com.example.intermodular.viewmodels.MyBookingsViewModel
import com.example.intermodular.views.components.BookingCard

/**
 * Pantalla de ver todas las reservas del usuario autenticado
 *
 * Flujo de UI:
 * 1. Muestra indicador de carga mientras se obtienen datos
 * 2. Muestra mensaje de error si ocurre algún problema
 *
 * @author Axel Zaragoci
 *
 * @param loading - Estado de carga de datos
 * @param error - Mensaje de error a mostrar (null si no hay error)
 * @param bookings - Lista de todas las reservas
 * @param rooms - Lista de todas las habitaciones
 * @param onDetailsButtonClick - Callback para evento click en el botón "Ver detalles"
 * @param invoiceOpening - Descarga de factura en curso (superposición ligera)
 * @param onOpenInvoice - Abre el PDF de factura para el id de reserva indicado
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MyBookingsScreen(
    loading : Boolean,
    error : String?,
    bookings : List<Booking>,
    rooms : List<Room>,
    onDetailsButtonClick: (String) -> Unit,
    invoiceOpening: Boolean = false,
    onOpenInvoice: (String) -> Unit = { _ -> }
) {
    Box(modifier = Modifier.fillMaxSize()) {
        Column (
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            // ESTADO DE CARGA
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

            // MENSAJE DE AUSENCIA DE RESERVAS
            if (bookings.isEmpty()) {
                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment = Alignment.Center
                ) {
                    Text(
                        text = "No se han encontrado reservas anteriores",
                        style = MaterialTheme.typography.titleLarge,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }

            // LISTA DE RESERVAS
            LazyColumn {
                items(bookings.size) { i ->
                    val booking = bookings[i]
                    val room = rooms.find { room -> booking.roomId == room.id }
                    if (room != null) {
                        BookingCard(
                            booking = booking,
                            room = room,
                            onDetailsButtonClick = onDetailsButtonClick,
                            onInvoiceClick = if (booking.invoiceNumber != null) {
                                { onOpenInvoice(booking.id) }
                            } else null
                        )
                    }
                }
            }
        }

        if (invoiceOpening) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .background(MaterialTheme.colorScheme.scrim.copy(alpha = 0.35f)),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }
        }
    }
}

/**
 * Versión de [MyBookingsScreen] con estado y conexión al ViewModel
 *
 * Funciones:
 * 1. Recolectar estados del [MyBookingsViewModel] usando [collectAsStateWithLifecycle]
 * 2. Pasar los estados a [MyBookingsScreen]
 * 3. Manejar la navegación al hacer click en "Reservar"
 *
 * Navegación:
 * Al hacer click en "Ver detalles", navega a la pantalla de reservar habitación y se pasa el ID de la reserva con la ruta:
 * "details/$bookingdId"
 *
 * @author Axel Zaragoci
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MyBookingsScreenState(
    viewModel: MyBookingsViewModel,
    navController: NavHostController
) {
    val context = LocalContext.current
    val bookings by viewModel.bookings.collectAsStateWithLifecycle()
    val loading by viewModel.isLoading.collectAsStateWithLifecycle()
    val error by viewModel.errorMessage.collectAsStateWithLifecycle()
    val rooms by viewModel.rooms.collectAsStateWithLifecycle()
    val invoiceOpening by viewModel.invoiceOpening.collectAsStateWithLifecycle()

    MyBookingsScreen(
        loading = loading,
        error = error,
        bookings = bookings,
        rooms = rooms,
        onDetailsButtonClick = { bookingId ->
            navController.navigate("details/$bookingId")
        },
        invoiceOpening = invoiceOpening,
        onOpenInvoice = { bookingId -> viewModel.openInvoicePdf(bookingId, context) }
    )
}