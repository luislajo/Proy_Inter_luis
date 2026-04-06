package com.example.intermodular.views.screens

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Payment
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import com.example.intermodular.viewmodels.PaymentViewModel
import kotlinx.coroutines.delay

@Composable
fun PaymentScreen(
    isLoading: Boolean,
    error: String?,
    paymentSuccessful: Boolean,
    onPayClick: () -> Unit,
    onNavigateNext: () -> Unit
) {
    LaunchedEffect(paymentSuccessful) {
        if (paymentSuccessful) {
            delay(1500) // Pequeña espera para UX antes de redirigir
            onNavigateNext()
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(24.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        if (paymentSuccessful) {
            Icon(
                imageVector = Icons.Filled.CheckCircle,
                contentDescription = "Pago completado",
                tint = MaterialTheme.colorScheme.primary,
                modifier = Modifier.size(100.dp)
            )
            Spacer(modifier = Modifier.height(24.dp))
            Text(
                text = "¡Pago Completado!",
                style = MaterialTheme.typography.headlineMedium,
                color = MaterialTheme.colorScheme.primary,
                textAlign = TextAlign.Center
            )
            Spacer(modifier = Modifier.height(16.dp))
            Text(
                text = "Redirigiendo a tus reservas...",
                style = MaterialTheme.typography.bodyLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = TextAlign.Center
            )
        } else {
            Icon(
                imageVector = Icons.Filled.Payment,
                contentDescription = "Método de pago",
                tint = MaterialTheme.colorScheme.secondary,
                modifier = Modifier.size(100.dp)
            )
            Spacer(modifier = Modifier.height(32.dp))
            
            Text(
                text = "Confirmación de Pago",
                style = MaterialTheme.typography.headlineMedium,
                color = MaterialTheme.colorScheme.onSurface,
                textAlign = TextAlign.Center
            )
            Spacer(modifier = Modifier.height(16.dp))
            Text(
                text = "Tu reserva ya ha sido confirmada. Pulsa Pagar para completar la transacción.",
                style = MaterialTheme.typography.bodyLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                textAlign = TextAlign.Center
            )
            
            Spacer(modifier = Modifier.height(48.dp))

            if (isLoading) {
                CircularProgressIndicator(color = MaterialTheme.colorScheme.primary)
                Spacer(modifier = Modifier.height(16.dp))
                Text("Procesando pago de forma segura...")
            } else {
                Button(
                    onClick = onPayClick,
                    modifier = Modifier.fillMaxWidth().height(56.dp)
                ) {
                    Text(
                        text = "Pagar Reserva",
                        style = MaterialTheme.typography.titleLarge
                    )
                }
            }

            error?.let {
                Spacer(modifier = Modifier.height(24.dp))
                Text(
                    text = it,
                    color = MaterialTheme.colorScheme.error,
                    style = MaterialTheme.typography.bodyMedium,
                    textAlign = TextAlign.Center
                )
            }
        }
    }
}

@Composable
fun PaymentState(
    viewModel: PaymentViewModel,
    onNavigateNext: () -> Unit
) {
    val isLoading by viewModel.isLoading.collectAsStateWithLifecycle()
    val error by viewModel.errorMessage.collectAsStateWithLifecycle()
    val paymentSuccessful by viewModel.paymentSuccessful.collectAsStateWithLifecycle()

    PaymentScreen(
        isLoading = isLoading,
        error = error,
        paymentSuccessful = paymentSuccessful,
        onPayClick = viewModel::pay,
        onNavigateNext = onNavigateNext
    )
}
