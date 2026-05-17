package com.example.intermodular.views.screens

import androidx.compose.foundation.background
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
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.graphics.painter.ColorPainter
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavHostController
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.example.intermodular.BuildConfig
import com.example.intermodular.models.Booking
import com.example.intermodular.models.Room
import com.example.intermodular.viewmodels.MyBookingDetailsViewModel
import com.example.intermodular.views.components.BookingDataForm
import androidx.compose.runtime.LaunchedEffect
import com.example.intermodular.views.navigation.Routes
import java.time.LocalDate

private val CheckInSuccessBackground = Color(0xFFC8E6C9)
private val CheckInSuccessText = Color(0xFF1B5E20)

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
 * @param canReportIncident - True si hoy estás dentro del periodo de la reserva (para avisar incidencias)
 * @param onOpenReportIncident - Abre la pantalla completa para escribir una incidencia
 * @param checkInSubmitting - Carga mientras se valida el check-in
 * @param onSubmitCheckIn - Callback para confirmar el check-in
 * @param checkOutSubmitting - Carga mientras se valida el check-out
 * @param onSubmitCheckOut - Callback para confirmar el check-out
 * @param stayActionMessage - Aviso tras check-in/check-out (p. ej. visible en historial)
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
    onRatingChange: (Int) -> Unit,
    canReportIncident: Boolean = false,
    onOpenReportIncident: () -> Unit = {},
    invoiceOpening: Boolean = false,
    onOpenInvoice: () -> Unit = {},
    checkInSubmitting: Boolean = false,
    onSubmitCheckIn: () -> Unit = {},
    checkOutSubmitting: Boolean = false,
    onSubmitCheckOut: () -> Unit = {},
    stayActionMessage: String? = null
) {
    LaunchedEffect(navigateToPayment) {
        navigateToPayment?.let {
            onNavigateToPayment(it)
        }
    }
    Box(modifier = Modifier.fillMaxSize()) {
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

                    stayActionMessage?.let { msg ->
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(bottom = 12.dp)
                                .background(CheckInSuccessBackground, MaterialTheme.shapes.medium)
                                .padding(12.dp)
                        ) {
                            Text(
                                text = msg,
                                color = CheckInSuccessText,
                                style = MaterialTheme.typography.bodyMedium
                            )
                        }
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

                    if (booking?.status == "Abierta" && booking.isCheckInDayToday) {
                        Spacer(modifier = Modifier.height(10.dp))
                        when {
                            booking.checkedIn -> {
                                Box(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .background(
                                            CheckInSuccessBackground,
                                            shape = MaterialTheme.shapes.medium
                                        )
                                        .padding(14.dp)
                                ) {
                                    Column {
                                        Text(
                                            text = "Check-in completado · Habitación nº${room?.roomNumber}",
                                            color = CheckInSuccessText,
                                            style = MaterialTheme.typography.titleMedium,
                                            fontWeight = FontWeight.SemiBold
                                        )
                                        Spacer(modifier = Modifier.height(6.dp))
                                        Text(
                                            text = "Tu llegada queda registrada. El hotel actualiza el estado de la habitación y puedes usar los servicios de tu estancia.",
                                            color = CheckInSuccessText.copy(alpha = 0.9f),
                                            style = MaterialTheme.typography.bodySmall
                                        )
                                    }
                                }
                            }
                            !booking.checkInCode.isNullOrBlank() -> {
                                Box(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .background(
                                            MaterialTheme.colorScheme.primaryContainer,
                                            shape = MaterialTheme.shapes.medium
                                        )
                                        .padding(16.dp),
                                    contentAlignment = Alignment.Center
                                ) {
                                    Column(horizontalAlignment = Alignment.CenterHorizontally) {
                                        Text(
                                            text = "Habitación nº${room?.roomNumber}",
                                            style = MaterialTheme.typography.titleMedium,
                                            color = MaterialTheme.colorScheme.onPrimaryContainer,
                                            fontWeight = FontWeight.SemiBold
                                        )
                                        Spacer(modifier = Modifier.height(8.dp))
                                        Text(
                                            text = "Código check-in",
                                            style = MaterialTheme.typography.bodyMedium,
                                            color = MaterialTheme.colorScheme.onPrimaryContainer
                                        )
                                        Text(
                                            text = booking.checkInCode,
                                            style = MaterialTheme.typography.headlineLarge,
                                            color = MaterialTheme.colorScheme.primary,
                                            fontWeight = FontWeight.Bold
                                        )
                                    }
                                }
                                if (booking.canSubmitCheckIn) {
                                    Spacer(modifier = Modifier.height(12.dp))
                                    Button(
                                        onClick = onSubmitCheckIn,
                                        modifier = Modifier.fillMaxWidth(),
                                        enabled = !checkInSubmitting
                                    ) {
                                        if (checkInSubmitting) {
                                            CircularProgressIndicator(
                                                modifier = Modifier.height(20.dp),
                                                color = MaterialTheme.colorScheme.onPrimary,
                                                strokeWidth = 2.dp
                                            )
                                        } else {
                                            Text("Confirmar check-in")
                                        }
                                    }
                                }
                            }
                            else -> {
                                Box(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .background(
                                            MaterialTheme.colorScheme.surfaceVariant,
                                            shape = MaterialTheme.shapes.medium
                                        )
                                        .padding(12.dp)
                                ) {
                                    Text(
                                        text = "El código de check-in estará disponible aquí a partir de las 11:00",
                                        style = MaterialTheme.typography.bodyMedium,
                                        color = MaterialTheme.colorScheme.onSurfaceVariant
                                    )
                                }
                            }
                        }
                        Spacer(modifier = Modifier.height(20.dp))
                    }

                    if (booking?.status == "Abierta" && booking.isCheckOutDayToday) {
                        when {
                            booking.checkedOut -> {
                                Box(
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .background(
                                            CheckInSuccessBackground,
                                            MaterialTheme.shapes.medium
                                        )
                                        .padding(14.dp)
                                ) {
                                    Text(
                                        text = "Check-out completado · Habitación nº${room?.roomNumber}",
                                        color = CheckInSuccessText,
                                        style = MaterialTheme.typography.titleMedium,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                }
                            }
                            booking.canSubmitCheckOut -> {
                                Text(
                                    text = "Check-out",
                                    style = MaterialTheme.typography.titleMedium,
                                    color = MaterialTheme.colorScheme.primary
                                )
                                Spacer(modifier = Modifier.height(8.dp))
                                Text(
                                    text = "Confirma tu salida del hotel:",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                                Spacer(modifier = Modifier.height(8.dp))
                                Button(
                                    onClick = onSubmitCheckOut,
                                    modifier = Modifier.fillMaxWidth(),
                                    enabled = !checkOutSubmitting
                                ) {
                                    if (checkOutSubmitting) {
                                        CircularProgressIndicator(
                                            modifier = Modifier.height(20.dp),
                                            color = MaterialTheme.colorScheme.onPrimary,
                                            strokeWidth = 2.dp
                                        )
                                    } else {
                                        Text("Confirmar check-out")
                                    }
                                }
                            }
                            booking.checkedIn -> {
                                Text(
                                    text = "El check-out estará disponible a partir de las 11:00 del día de salida.",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                            else -> {
                                Text(
                                    text = "Debes hacer check-in antes de poder registrar el check-out.",
                                    style = MaterialTheme.typography.bodyMedium,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant
                                )
                            }
                        }
                        Spacer(modifier = Modifier.height(20.dp))
                    }

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
                        onGuestsDataChange = onGuestsChange,
                        showSubmitButton = false
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    // Actualizar y cancelar en la misma fila
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        Button(
                            onClick = onUpdateClick,
                            modifier = Modifier.weight(1f)
                        ) {
                            Text("Actualizar reserva")
                        }
                        Button(
                            onClick = onCancelClick,
                            modifier = Modifier.weight(1f),
                            colors = ButtonDefaults.buttonColors(
                                containerColor = MaterialTheme.colorScheme.error,
                                contentColor = MaterialTheme.colorScheme.onError
                            )
                        ) {
                            Text("Cancelar reserva")
                        }
                    }

                    // Factura e incidencias (solo durante la estancia)
                    if (booking?.invoiceNumber != null || canReportIncident) {
                        Spacer(modifier = Modifier.height(12.dp))
                        val hasInvoice = booking?.invoiceNumber != null
                        if (hasInvoice && canReportIncident) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.spacedBy(8.dp)
                            ) {
                                Button(
                                    onClick = onOpenInvoice,
                                    modifier = Modifier.weight(1f)
                                ) {
                                    Text("Ver factura (PDF)")
                                }
                                Button(
                                    onClick = onOpenReportIncident,
                                    modifier = Modifier.weight(1f),
                                    colors = ButtonDefaults.buttonColors(
                                        containerColor = MaterialTheme.colorScheme.error,
                                        contentColor = MaterialTheme.colorScheme.onError
                                    )
                                ) {
                                    Text("Escribir incidencia")
                                }
                            }
                        } else if (hasInvoice) {
                            Button(
                                onClick = onOpenInvoice,
                                modifier = Modifier.fillMaxWidth()
                            ) {
                                Text("Ver factura (PDF)")
                            }
                        } else {
                            Button(
                                onClick = onOpenReportIncident,
                                modifier = Modifier.fillMaxWidth(),
                                colors = ButtonDefaults.buttonColors(
                                    containerColor = MaterialTheme.colorScheme.error,
                                    contentColor = MaterialTheme.colorScheme.onError
                                )
                            ) {
                                Text("Escribir incidencia")
                            }
                        }
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
    navController: NavHostController,
    onNavigateToPayment: (String) -> Unit
) {
    val context = LocalContext.current
    val loading by viewModel.isLoading.collectAsStateWithLifecycle()
    val error by viewModel.errorMessage.collectAsStateWithLifecycle()
    val room by viewModel.room.collectAsStateWithLifecycle()
    val booking by viewModel.booking.collectAsStateWithLifecycle()
    val reviewDescription by viewModel.reviewDescription.collectAsStateWithLifecycle()
    val reviewRate by viewModel.reviewRating.collectAsStateWithLifecycle()
    val reviewCreated by viewModel.reviewCreated.collectAsStateWithLifecycle()
    val navigateToPayment by viewModel.navigateToPayment.collectAsStateWithLifecycle()
    val invoiceOpening by viewModel.invoiceOpening.collectAsStateWithLifecycle()
    val checkInSubmitting by viewModel.checkInSubmitting.collectAsStateWithLifecycle()
    val checkOutSubmitting by viewModel.checkOutSubmitting.collectAsStateWithLifecycle()
    val stayActionMessage by viewModel.stayActionMessage.collectAsStateWithLifecycle()

    val today = LocalDate.now()
    val canReportIncident = booking?.let { b ->
        b.status == "Abierta" &&
            !today.isBefore(b.checkInDate) &&
            !today.isAfter(b.checkOutDate)
    } ?: false

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
        onCreateReviewClick = viewModel::createReview,
        canReportIncident = canReportIncident,
        onOpenReportIncident = {
            room?.id?.let { rid ->
                navController.navigate(Routes.ReportIncident.createRoute(rid))
            }
        },
        invoiceOpening = invoiceOpening,
        onOpenInvoice = { viewModel.openInvoicePdf(context) },
        checkInSubmitting = checkInSubmitting,
        onSubmitCheckIn = viewModel::submitCheckIn,
        checkOutSubmitting = checkOutSubmitting,
        onSubmitCheckOut = viewModel::submitCheckOut,
        stayActionMessage = stayActionMessage
    )
}