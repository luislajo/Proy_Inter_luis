package com.example.intermodular.views.components

import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier

/**
 * Componente personalizado de ComboBox.
 *
 * Características:
 * - Campo de solo lectura - Muestra el valor seleccionado actual
 * - Colores personalizados - Sigue la paleta de colores de la aplicación
 * - Selección - Al seleccionar una opción el menú se oculta por defecto
 *
 * @author Axel Zaragoci
 *
 * @param default - Valor seleccionado inicial
 * @param onValueChanged - Callback a ejecutar al cambiar el valor seleccionado
 * @param label - Texto a mostrar en el campo
 * @param options - Lista de opciones disponibles como [String]
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ComboBox(
    default : String,
    onValueChanged : (String) -> Unit,
    label : String,
    options : List<String>
) {
    var expanded by remember { mutableStateOf(false) }

    ExposedDropdownMenuBox(
        expanded = expanded,
        onExpandedChange = { expanded = !expanded }
    ) {
        OutlinedTextField(
            value = default,
            onValueChange = { },
            readOnly = true,
            label = {
                Text(
                    text = label
                )
            },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded = expanded) },
            colors = OutlinedTextFieldDefaults.colors(
                focusedTextColor = MaterialTheme.colorScheme.onSurface,
                unfocusedTextColor = MaterialTheme.colorScheme.onSurface,
                focusedContainerColor = MaterialTheme.colorScheme.surfaceVariant,
                unfocusedContainerColor = MaterialTheme.colorScheme.surfaceVariant,

                focusedBorderColor = MaterialTheme.colorScheme.primary,
                unfocusedBorderColor = MaterialTheme.colorScheme.outline,

                focusedLabelColor = MaterialTheme.colorScheme.primary,
                unfocusedLabelColor = MaterialTheme.colorScheme.onSurfaceVariant,

                cursorColor = MaterialTheme.colorScheme.primary
            ),
            modifier = Modifier
                .fillMaxWidth()
                .menuAnchor()
        )

        DropdownMenu(
            expanded = expanded,
            onDismissRequest = { expanded = false },
            modifier = Modifier.exposedDropdownSize(matchTextFieldWidth = true)
        ) {
            options.forEach { option ->
                DropdownMenuItem(
                    text = { Text(option) },
                    onClick = {
                        onValueChanged(option)
                        expanded = false
                    }
                )
            }
        }
    }
}