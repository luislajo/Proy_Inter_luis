package com.example.intermodular.views.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import com.example.intermodular.views.components.ComboBox
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavHostController
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.intermodular.data.remote.RetrofitProvider
import com.example.intermodular.data.repository.IncidentRepository
import com.example.intermodular.viewmodels.ReportIncidentViewModel
import com.example.intermodular.viewmodels.viewModelFacotry.ReportIncidentViewModelFactory

/**
 * Pantalla completa para enviar una incidencia (sustituye el diálogo en perfil / detalle reserva).
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReportIncidentScreen(
    sending: Boolean,
    error: String?,
    onClearError: () -> Unit,
    onBack: () -> Unit,
    onSubmit: (type: String, severity: String, description: String) -> Unit
) {
    val categoryOptions = listOf("Ruido", "Limpieza", "Avería")
    val severityOptions = listOf("Baja", "Media", "Alta")

    var categoryLabel by remember { mutableStateOf("Ruido") }
    var severityLabel by remember { mutableStateOf("Media") }
    var description by remember { mutableStateOf("") }

    fun categoryApi(): String = when (categoryLabel) {
        "Ruido" -> "ruido"
        "Limpieza" -> "limpieza"
        "Avería" -> "averia"
        else -> "ruido"
    }

    fun severityApi(): String = when (severityLabel) {
        "Baja" -> "low"
        "Media" -> "medium"
        "Alta" -> "high"
        else -> "medium"
    }

    if (error != null) {
        AlertDialog(
            onDismissRequest = onClearError,
            title = { Text("No se pudo enviar") },
            text = { Text(error) },
            confirmButton = {
                TextButton(onClick = onClearError) { Text("Aceptar") }
            }
        )
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Reportar incidencia") },
                navigationIcon = {
                    IconButton(onClick = onBack, enabled = !sending) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Volver")
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState())
                .padding(20.dp)
        ) {
            ComboBox(
                default = categoryLabel,
                onValueChanged = { categoryLabel = it },
                label = "Categoría",
                options = categoryOptions
            )

            Spacer(Modifier.padding(vertical = 12.dp))

            ComboBox(
                default = severityLabel,
                onValueChanged = { severityLabel = it },
                label = "Severidad",
                options = severityOptions
            )

            Spacer(Modifier.padding(vertical = 12.dp))

            OutlinedTextField(
                value = description,
                onValueChange = { description = it },
                label = { Text("Descripción") },
                modifier = Modifier.fillMaxWidth(),
                minLines = 5,
                enabled = !sending
            )

            Spacer(Modifier.padding(vertical = 16.dp))

            Button(
                onClick = { onSubmit(categoryApi(), severityApi(), description.trim()) },
                modifier = Modifier.fillMaxWidth(),
                enabled = !sending
            ) {
                Row(
                    verticalAlignment = Alignment.CenterVertically,
                    horizontalArrangement = Arrangement.Center
                ) {
                    if (sending) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(22.dp),
                            strokeWidth = 2.dp
                        )
                        Spacer(Modifier.width(10.dp))
                    }
                    Text("Enviar incidencia")
                }
            }
        }
    }
}

@Composable
fun ReportIncidentScreenState(
    roomId: String,
    navController: NavHostController
) {
    val api = RetrofitProvider.api
    val repo = IncidentRepository(api)
    val viewModel: ReportIncidentViewModel = viewModel(
        factory = ReportIncidentViewModelFactory(roomId, repo)
    )

    val sending by viewModel.sending.collectAsStateWithLifecycle()
    val error by viewModel.errorMessage.collectAsStateWithLifecycle()

    ReportIncidentScreen(
        sending = sending,
        error = error,
        onClearError = viewModel::clearError,
        onBack = { if (!sending) navController.popBackStack() },
        onSubmit = { type, sev, desc ->
            viewModel.submit(type, sev, desc) {
                navController.popBackStack()
            }
        }
    )
}
