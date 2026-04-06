package com.example.intermodular.views.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.painter.ColorPainter
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.example.intermodular.BuildConfig
import com.example.intermodular.models.Booking
import com.example.intermodular.models.Room
import com.example.intermodular.viewmodels.MyBookingDetailsViewModel
import com.example.intermodular.views.components.BookingDataForm
import androidx.compose.runtime.LaunchedEffect

/**
 * Pantalla de detalles y actualización de una reserva
 *
 * Flujo de UI:
 * 1. Muestra indicador de carga mientras se obtienen datos
 * 2. Muestra mensaje de error si ocurre algún problema
 *
 * Componentes externos:
 * - **Formulario de actualizar**: [BookingDataForm] para editar fechas y huéspedes
 * - **Popup de pago**: [PaymentPopup] para simular una pasarela de pago
 *
 * @author Axel Zaragoci
 *
 * @param loading - Estado de carga de datos
 * @param error - Mensaje de error a mostrar (null si no hay error)
 * @param room - Habitación asociada a la reserva
 * @param status - Estado actual de la reserva ("Abierta", "Finalizada", "Cancelada")
 * @param booking - Datos completos de la reserva
 * @param startDate - Fecha de inicio seleccionada (en milisegundos)
 * @param endDate - Fecha de fin seleccionada (en milisegundos)
 * @param navigateToPayment - ID de la reserva a la que navegar para el pago
 * @param onNavigateToPayment - Callback que ejecuta la navegación
 * @param reviewDescription - Descripción de la reseña
 * @param rating - Calificación de la reseña
 * @param onUpdateClick - Callback al hacer clic en "Actualizar"
 * @param onCancelClick - Callback al hacer clic en "Cancelar reserva"
 * @param onStartDateChange - Callback al cambiar fecha de entrada
 * @param onEndDateChange - Callback al cambiar fecha de salida
 * @param onGuestsChange - Callback al cambiar número de huéspedes
 * @param onCreateReviewClick - Callback al añadir una reseña
 * @param onReviewDescriptionChange - Callback al modificar la descripción de la reseña
 * @param onRatingChange - Callback al modificar la calificación de la reseña
 */
@Composable
fun MyBookingDetailsScreen(
    loading: Boolean,
    error: String?,
    room: Room?,
    status: String?,
    booking: Booking?,
    startDate: Long?,
    endDate: Long?,
    navigateToPayment: String?,
    onNavigateToPayment: (String) -> Unit,
    reviewDescription: String,
    rating: Int,
    created: Boolean,
    onUpdateClick: () -> Unit,
    onCancelClick: () -> Unit,
    onStartDateChange: (Long?) -> Unit,
    onEndDateChange: (Long?) -> Unit,
    onGuestsChange: (String) -> Unit,
    onCreateReviewClick: () -> Unit,
    onReviewDescriptionChange: (String) -> Unit,
    onRatingChange: (Int) -> Unit
) {
    LaunchedEffect(navigateToPayment) {
        navigateToPayment?.let {
            onNavigateToPayment(it)
        }
    }
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(20.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        // ESTADO DE CARGA
        if (loading) {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
            }
        } else {
            LazyColumn(
                modifier = Modifier.fillMaxSize()
            ) {
                item {
                    // MENSAJE DE ERROR
                    error?.let {
                        Text(
                            text = it,
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodyLarge,
                            modifier = Modifier.padding(bottom = 16.dp)
                        )
                    }

                    // Construcción url
                    val relativePath = if (room?.mainImage?.startsWith("/") == true)
                        room.mainImage.substring(1)
                    else
                        room?.mainImage

                    val imageUrl = if (room?.mainImage?.startsWith("http") == true)
                        room.mainImage
                    else
                        "${BuildConfig.BASE_URL}$relativePath"

                    // IMÁGEN PRINCIPAL DE LA HABITACIÓN
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

                    Spacer(modifier = Modifier.height(16.dp))

                    // NÚMERO DE LA HABITACIÓN
                    Text(
                        text = "Habitación nº${room?.roomNumber}",
                        style = MaterialTheme.typography.titleLarge,
                        color = MaterialTheme.colorScheme.primary
                    )

                    // ESTADO DE LA RESERVA
                    Text(
                        text = "Estado: $status",
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.secondary
                    )

                    Spacer(modifier = Modifier.height(24.dp))

                    // FORMULARIO DE EDICIÓN DE RESERVA
                    BookingDataForm(
                        create = false,
                        startDate = startDate ?: 0L,
                        endDate = endDate ?: 0L,
                        guests = booking?.guests?.toString() ?: "",
                        totalPrice = booking?.totalPrice,
                        onButtonClick = onUpdateClick,
                        onStartDateChange = onStartDateChange,
                        onEndDateChange = onEndDateChange,
                        onGuestsDataChange = onGuestsChange
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    // BOTÓN DE CANCELAR
                    Button(
                        onClick = onCancelClick,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Text("Cancelar reserva")
                    }

                    Spacer(modifier = Modifier.height(30.dp))


                    if (!created) {
                        // AGREGAR RESEÑA
                        Text(
                            text = "Dejar una reseña",
                            style = MaterialTheme.typography.titleMedium,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.fillMaxWidth()
                        )

                        // SELECTOR DE ESTRELLAS
                        Row (
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.Center
                        ){
                            for (i in 1..5) {
                                IconButton(onClick = { onRatingChange(i) }) {
                                    Icon(
                                        Icons.Default.Star,
                                        contentDescription = "Estrella $i",
                                        tint = if (i <= rating)
                                            MaterialTheme.colorScheme.primary
                                        else
                                            MaterialTheme.colorScheme.outlineVariant
                                    )
                                }
                            }
                        }

                        // Campo de descripción
                        OutlinedTextField(
                            value = reviewDescription,
                            onValueChange = onReviewDescriptionChange,
                            label = { Text("Tu opinión") },
                            modifier = Modifier.fillMaxWidth(),
                            maxLines = 4
                        )

                        Spacer(modifier = Modifier.height(8.dp))

                        // Botón enviar
                        Button(
                            onClick = onCreateReviewClick,
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Text("Enviar reseña")
                        }
                    }
                    else {
                        Text(
                            text = "Ya se ha añadido una reseña",
                            style = MaterialTheme.typography.titleMedium,
                            color = MaterialTheme.colorScheme.primary,
                            textAlign = TextAlign.Center,
                            modifier = Modifier.fillMaxWidth()
                        )
                    }
                }
            }
        }
    }
}

/**
 * Versión de [MyBookingDetailsScreen] con estado y conexión al ViewModel
 *
 * Funciones:
 * 1. Recolectar estados del [MyBookingDetailsViewModel] usando [collectAsStateWithLifecycle]
 * 2. Pasar los estados a [MyBookingDetailsScreen]
 *
 * @author Axel Zaragoci
 *
 * @param viewModel - Instancia de [MyBookingDetailsViewModel] de la que sacar los datos
 */
@Composable
fun MyBookingDetailsState(
    viewModel: MyBookingDetailsViewModel,
    onNavigateToPayment: (String) -> Unit
) {
    val loading by viewModel.isLoading.collectAsStateWithLifecycle()
    val error by viewModel.errorMessage.collectAsStateWithLifecycle()
    val room by viewModel.room.collectAsStateWithLifecycle()
    val booking by viewModel.booking.collectAsStateWithLifecycle()
    val reviewDescription by viewModel.reviewDescription.collectAsStateWithLifecycle()
    val reviewRate by viewModel.reviewRating.collectAsStateWithLifecycle()
    val reviewCreated by viewModel.reviewCreated.collectAsStateWithLifecycle()
    val navigateToPayment by viewModel.navigateToPayment.collectAsStateWithLifecycle()

    MyBookingDetailsScreen(
        loading = loading,
        error = error,
        room = room,
        status = booking?.status,
        booking = booking,
        navigateToPayment = navigateToPayment,
        onNavigateToPayment = { 
            viewModel.onPaymentNavigated()
            onNavigateToPayment(it) 
        },
        reviewDescription = reviewDescription,
        rating = reviewRate,
        created = reviewCreated,
        startDate = viewModel.checkInDateToMilliseconds(),
        endDate = viewModel.checkOutDateToMilliseconds(),
        onUpdateClick = viewModel::updateBooking,
        onCancelClick = viewModel::cancelBooking,
        onStartDateChange = viewModel::onStartDateChange,
        onEndDateChange = viewModel::onEndDateChange,
        onGuestsChange = viewModel::onGuestsChange,
        onReviewDescriptionChange = viewModel::onReviewDescriptionChange,
        onRatingChange = viewModel::onRatingChange,
        onCreateReviewClick = viewModel::createReview
    )
}