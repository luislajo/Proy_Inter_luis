package com.example.intermodular.views.components

import android.widget.Scroller
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowDropDown
import androidx.compose.material.icons.filled.ArrowDropUp
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.RangeSlider
import androidx.compose.material3.Slider
import androidx.compose.material3.SliderDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import kotlin.math.roundToInt
import androidx.compose.material3.FilterChip

/**
 * Componente interactivo que despliega un formulario completo de filtros para el catálogo de habitaciones.
 *
 * Contiene controles granulares (Sliders, Checkboxes, Dropdowns, Chips) que se enlazan mediante 
 * elevación de estado (State Hoisting) a los valores individuales almacenados en el `RoomViewModel`.
 *
 * @param showFilters Controla la visibilidad del panel de filtros de forma expandible.
 * @param changeVisibility Callback para alternar u ocultar la visibilidad del panel.
 * @param type Filtro seleccionado de tipo de habitación.
 * @param onTypeChanged Callback cuando se selecciona un nuevo tipo.
 * @param minPrice Valor inferior seleccionado en el rango de precio.
 * @param onMinPriceChanged Callback al modificar el precio mínimo.
 * @param maxPrice Valor superior seleccionado en el rango de precio.
 * @param onMaxPriceChanged Callback al modificar el precio máximo.
 * @param guests Número de huéspedes introducido.
 * @param onGuestsChanged Callback al cambiar el texto de huéspedes.
 * @param isAvailable Estado del checkbox "Solo disponibles".
 * @param onIsAvailableChanged Callback al cambiar el checkbox de disponibilidad.
 * @param hasExtraBed Estado del checkbox "Cama supletoria".
 * @param onExtraBedChanged Callback al cambiar el checkbox de cama extra.
 * @param hasCrib Estado del checkbox "Cuna".
 * @param onCribChanged Callback al cambiar el checkbox de cuna.
 * @param hasOffer Estado del checkbox "Ofertas especiales".
 * @param onOfferChanged Callback al cambiar el checkbox de ofertas.
 * @param sortBy Campo actual por el que se ordenarán los resultados.
 * @param onSortByChanged Callback al seleccionar un nuevo campo de ordenación.
 * @param sortOrder Dirección actual de la ordenación ("asc" o "desc").
 * @param onSortOrderChanged Callback al invertir la dirección de ordenación.
 * @param filter Función ejecutada al pulsar el botón "Buscar" (aplica los valores al DTO y repinta listado).
 * @param clearFilters Función ejecutada al pulsar el botón "Limpiar Filtros".
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RoomFilterList(
    showFilters: Boolean,
    changeVisibility: () -> Unit,
    type: String,
    onTypeChanged: (String) -> Unit,
    minPrice: Int,
    onMinPriceChanged: (Int) -> Unit,
    maxPrice: Int,
    onMaxPriceChanged: (Int) -> Unit,
    guests: String,
    onGuestsChanged: (String) -> Unit,
    isAvailable: Boolean,
    onIsAvailableChanged: (Boolean) -> Unit,
    hasExtraBed: Boolean,
    onExtraBedChanged: (Boolean) -> Unit,
    hasCrib: Boolean,
    onCribChanged: (Boolean) -> Unit,
    hasOffer: Boolean,
    onOfferChanged: (Boolean) -> Unit,
    sortBy: String,
    onSortByChanged: (String) -> Unit,
    sortOrder: String,
    onSortOrderChanged: (String) -> Unit,
    filter: () -> Unit,
    clearFilters: () -> Unit
) {
    if (showFilters) {
        Column(
            modifier = Modifier
                .padding(horizontal = 20.dp, vertical = 16.dp)
                .background(
                    color = MaterialTheme.colorScheme.surfaceVariant,
                    shape = MaterialTheme.shapes.large
                )
                .fillMaxWidth(),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Column(
                modifier = Modifier
                    .verticalScroll(rememberScrollState())
                    .padding(16.dp)
                    .fillMaxWidth(),
                verticalArrangement = Arrangement.spacedBy(20.dp)
            ) {
            var expanded by remember { mutableStateOf(false) }
            val types = listOf("single", "double", "suite", "family")

            ExposedDropdownMenuBox(
                expanded = expanded,
                onExpandedChange = { expanded = !expanded },
                modifier = Modifier
                    .padding(horizontal = 20.dp)
                    .fillMaxWidth()
            ) {
                OutlinedTextField(
                    readOnly = true,
                    value = if (type.isBlank()) "Todos" else type,
                    onValueChange = { },
                    label = { Text("Tipo de habitación") },
                    trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
                    colors = ExposedDropdownMenuDefaults.outlinedTextFieldColors(),
                    modifier = Modifier
                        .menuAnchor()
                        .fillMaxWidth()
                )
                ExposedDropdownMenu(
                    expanded = expanded,
                    onDismissRequest = { expanded = false }
                ) {
                    DropdownMenuItem(
                        text = { Text("Todos") },
                        onClick = {
                            onTypeChanged("")
                            expanded = false
                        }
                    )
                    types.forEach { selectionOption ->
                        DropdownMenuItem(
                            text = { Text(selectionOption) },
                            onClick = {
                                onTypeChanged(selectionOption)
                                expanded = false
                            }
                        )
                    }
                }
            }

            Column(
                modifier = Modifier
                    .padding(horizontal = 20.dp)
                    .fillMaxWidth()
            ) {
                Text(
                    text = "Rango de precio: ${minPrice}€ - ${maxPrice}€",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                
                val rangePosition = remember(minPrice, maxPrice) { 
                    minPrice.toFloat()..maxPrice.toFloat() 
                }
                
                RangeSlider(
                    value = rangePosition,
                    onValueChange = { range ->
                        onMinPriceChanged(range.start.roundToInt())
                        onMaxPriceChanged(range.endInclusive.roundToInt())
                    },
                    valueRange = 0f..500f,
                    steps = 49,
                    colors = SliderDefaults.colors(
                        thumbColor = MaterialTheme.colorScheme.secondary,
                        activeTrackColor = MaterialTheme.colorScheme.primary,
                        inactiveTrackColor = MaterialTheme.colorScheme.outline,
                        activeTickColor = Color.Transparent,
                        inactiveTickColor = Color.Transparent,
                    ),
                )
            }

            Row(
                modifier = Modifier
                    .padding(horizontal = 20.dp)
                    .fillMaxWidth()
            ) {
                ComboBox(
                    default = guests,
                    onValueChanged = onGuestsChanged,
                    label = "Cantidad de huéspedes",
                    options = listOf("1","2","3","4","5"))
            }

            Row(
                modifier = Modifier
                    .padding(horizontal = 20.dp)
                    .fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceAround
            ) {
                Row(
                    horizontalArrangement = Arrangement.spacedBy(2.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Checkbox(
                        checked = isAvailable,
                        onCheckedChange = onIsAvailableChanged
                    )
                    Text(text = "Reservable")
                }

                Row(
                    horizontalArrangement = Arrangement.spacedBy(2.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Checkbox(
                        checked = hasExtraBed,
                        onCheckedChange = onExtraBedChanged
                    )
                    Text(text = "Cama extra")
                }
            }

            Row(
                modifier = Modifier
                    .padding(horizontal = 20.dp)
                    .fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceAround
            ) {
                Row(
                    horizontalArrangement = Arrangement.spacedBy(2.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Checkbox(
                        checked = hasCrib,
                        onCheckedChange = onCribChanged
                    )
                    Text(text = "Cuna")
                }

                Row(
                    horizontalArrangement = Arrangement.spacedBy(2.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Checkbox(
                        checked = hasOffer,
                        onCheckedChange = onOfferChanged
                    )
                    Text(text = "Oferta")
                }
            }

            Column(
                modifier = Modifier
                    .padding(horizontal = 20.dp)
                    .fillMaxWidth()
            ) {
                Text(
                    text = "Ordenar por:",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Row(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    modifier = Modifier.padding(top = 8.dp)
                ) {
                    FilterChip(
                        selected = sortBy == "roomNumber",
                        onClick = { onSortByChanged("roomNumber") },
                        label = { Text("Número") }
                    )
                    FilterChip(
                        selected = sortBy == "pricePerNight",
                        onClick = { onSortByChanged("pricePerNight") },
                        label = { Text("Precio") }
                    )
                }
                Row(
                    horizontalArrangement = Arrangement.spacedBy(8.dp),
                    modifier = Modifier.padding(top = 4.dp)
                ) {
                    FilterChip(
                        selected = sortOrder == "asc",
                        onClick = { onSortOrderChanged("asc") },
                        label = { Text("ASC") }
                    )
                    FilterChip(
                        selected = sortOrder == "desc",
                        onClick = { onSortOrderChanged("desc") },
                        label = { Text("DESC") }
                    )
                }
            }

            Row(
                modifier = Modifier
                    .padding(vertical = 20.dp)
                    .fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                Button(
                    onClick = { filter() },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.primary,
                        contentColor = MaterialTheme.colorScheme.onPrimary
                    )
                ) {
                    Text(text = "Filtrar")
                }

                Button(
                    onClick = { clearFilters() },
                    colors = ButtonDefaults.buttonColors(
                        containerColor = MaterialTheme.colorScheme.primary,
                        contentColor = MaterialTheme.colorScheme.onPrimary
                    )
                ) {
                    Text(text = "Limpiar filtros")
                }
                }
            }
            }
        }
    }

