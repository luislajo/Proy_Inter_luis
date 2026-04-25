package com.example.intermodular.views.components

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.capitalize
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.example.intermodular.BuildConfig
import com.example.intermodular.models.Booking
import com.example.intermodular.models.Room
import java.util.Locale

/**
 * Tarjeta para mostrar la información básica de una reserva utilizada en [com.example.intermodular.views.screens.MyBookingsScreen]
 * Utiliza el objeto de reserva y el de habitación para mostrar todos los datos y cargar la imagen de la habitación reservada
 *
 * @author Axel Zaragoci
 *
 * @param booking - Reserva que se quiere mostrar
 * @param room - Habitación reservada
 * @param onDetailsButtonClick - Callback al hacer click en el botón de "Mostrar detalles"
 * @param onInvoiceClick - Si la reserva tiene factura en servidor, abre el PDF (null si no se usa)
 */
@Composable
fun BookingCard(
    booking: Booking,
    room : Room,
    onDetailsButtonClick: (String) -> Unit,
    onInvoiceClick: (() -> Unit)? = null
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(8.dp),
        shape = MaterialTheme.shapes.medium,
        elevation = CardDefaults.cardElevation(defaultElevation = 4.dp)
    ) {
        Column {
            val relativePath =
                if (room.mainImage.startsWith("/")) room.mainImage.substring(1) else room.mainImage
            val imageUrl =
                if (room.mainImage.startsWith("http")) room.mainImage else "${BuildConfig.BASE_URL}$relativePath"

            android.util.Log.d("Booking", "Carga de imagen: $imageUrl")

            AsyncImage(
                model = ImageRequest.Builder(LocalContext.current)
                    .data(imageUrl)
                    .crossfade(true)
                    .listener(
                        onError = { _, result ->
                            android.util.Log.e(
                                "Booking",
                                "Error cargando imagen: $imageUrl",
                                result.throwable
                            )
                        },
                        onSuccess = { _, _ ->
                            android.util.Log.d("Booking", "Imagen cargada correctamente: $imageUrl")
                        }
                    )
                    .build(),
                contentDescription = "Imagen de la habitación",
                contentScale = ContentScale.Crop,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(200.dp),
                error = androidx.compose.ui.graphics.painter.ColorPainter(MaterialTheme.colorScheme.errorContainer)
            )

            Column(modifier = Modifier.padding(16.dp)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(
                        text = "Habitación ${room.roomNumber}",
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                    Text(
                        text = room.type.replaceFirstChar { if (it.isLowerCase()) it.titlecase(Locale.ROOT) else it.toString() },
                        style = MaterialTheme.typography.titleLarge,
                        fontWeight = FontWeight.Bold
                    )
                }
                Spacer(modifier = Modifier.height(2.dp))
                Text(
                    text = "Estado: ${booking.status}",
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.SemiBold
                )

                Spacer(modifier = Modifier.height(4.dp))


                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    Text(
                        text = "${booking.totalPrice}€ en total",
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.primary
                    )

                    Text(
                        text = "${booking.guests} huéspedes",
                        style = MaterialTheme.typography.titleMedium,
                        color = if (room.isAvailable) MaterialTheme.colorScheme.tertiary else MaterialTheme.colorScheme.error
                    )
                }

                Spacer(modifier = Modifier.height(8.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween
                ) {
                    InformationComponent(
                        title = "Fecha de inicio",
                        value = booking.checkInDate.dayOfMonth.toString() + "/" + booking.checkInDate.monthValue + "/" + booking.checkInDate.year
                    )

                    InformationComponent(
                        title = "Fecha de fin",
                        value = booking.checkOutDate.dayOfMonth.toString() + "/" + booking.checkOutDate.monthValue + "/" + booking.checkOutDate.year
                    )
                }

                if (booking.invoiceNumber != null && onInvoiceClick != null) {
                    Spacer(modifier = Modifier.height(8.dp))
                    OutlinedButton(
                        onClick = onInvoiceClick,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Text(text = "Ver factura (PDF)")
                    }
                }

                Spacer(modifier = Modifier.height(8.dp))

                Button(
                    onClick = { onDetailsButtonClick(booking.id) },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(
                        text = "Ver detalles"
                    )
                }
            }
        }
    }
}