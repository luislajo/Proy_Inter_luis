package com.example.intermodular.views.components

import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

/**
 * Formulario para crear y/o actualizar una reserva
 *
 * En caso de actualizar utiliza los datos de la reserva existente
 * En caso de actualizar utiliza datos recibidos en la vista [com.example.intermodular.views.screens.NewBookingScreen] desde [com.example.intermodular.views.screens.BookingScreen]
 *
 * Este formulario contiene:
 * - 2 [BookingDatePicker] para la fecha de inicio y de fin
 * - 1 [ComboBox] para seleccionar la cantidad de huéspedes
 * - 1 texto para mostrar el precio total
 * - 1 [Button] para ejecutar la función del formulario
 *
 * @author Axel Zaragoci
 *
 * @param create - Variable para indicar si el formulario se utiliza para crear o para actualizar una reserva
 * @param startDate - Fecha de inicio de la reserva
 * @param endDate - Fecha de fin de la reserva
 * @param guests - Cantidad de huéspedes de la reserva
 * @param totalPrice - Precio total de la reserva
 * @param onButtonClick - Callback para ejecutar la función de crear o actualizar
 * @param onStartDateChange - Callback al cambiar la fecha de inicio
 * @param onEndDateChange - Callback al cambiar la fecha de fin
 * @param onGuestsDataChange - Callback al cambiar la cantidad de huéspedes
 * @param showSubmitButton - Si es false, no se muestra el botón de enviar (p. ej. acciones en la pantalla padre)
 */
@Composable
fun BookingDataForm(
    create : Boolean,
    startDate: Long,
    endDate: Long,
    guests: String,
    totalPrice: Double?,
    onButtonClick: () -> Unit,
    onStartDateChange: (Long?) -> Unit,
    onEndDateChange: (Long?) -> Unit,
    onGuestsDataChange: (String) -> Unit,
    showSubmitButton: Boolean = true
) {
    Column() {
        Row( )
        {
            Column(
                modifier = Modifier
                    .weight(1f)
                    .padding(end = 10.dp)
            ) {
                BookingDatePicker(
                    onDateSelected = onStartDateChange,
                    selectedDateMillis = startDate,
                    label = "Fecha de inicio:"
                )
            }

            Column(
                modifier = Modifier
                    .weight(1f)
                    .padding(start = 10.dp)
            ) {
                BookingDatePicker(
                    onDateSelected = onEndDateChange,
                    selectedDateMillis = endDate,
                    label = "Fecha de fin:"
                )
            }
        }

        Row(
            modifier = Modifier.fillMaxWidth()
        ) {
            ComboBox(
                default = guests,
                onValueChanged = onGuestsDataChange,
                label = "Cantidad de huéspedes:",
                options = listOf("1", "2", "3", "4", "5")
            )
        }

        Column (
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 10.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Text(
                text = "Precio total:",
                style = MaterialTheme.typography.titleMedium,
                color = MaterialTheme.colorScheme.primary
            )

            Text(
                text = "$totalPrice €",
                style = MaterialTheme.typography.titleSmall,
                color = MaterialTheme.colorScheme.primary
            )
        }

        if (showSubmitButton) {
            Button(
                onClick = onButtonClick,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text(
                    text = if (create) "Reservar" else "Actualizar reserva"
                )
            }
        }
    }
}