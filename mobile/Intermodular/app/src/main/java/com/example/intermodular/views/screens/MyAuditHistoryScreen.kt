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
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.ArrowBackIosNew
import androidx.compose.material.icons.outlined.Event
import androidx.compose.material.icons.outlined.Payments
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ElevatedCard
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.compose.ui.platform.LocalContext
import androidx.navigation.NavController
import com.example.intermodular.models.AuditHistoryEntry
import com.example.intermodular.viewmodels.MyAuditHistoryViewModel

@Composable
fun MyAuditHistoryScreen(
    isLoading: Boolean,
    checkInOutEntries: List<AuditHistoryEntry>,
    paymentEntries: List<AuditHistoryEntry>,
    error: String?,
    invoiceOpening: Boolean = false,
    onOpenInvoice: (String) -> Unit = { _ -> },
    onBack: () -> Unit,
    onRetry: () -> Unit,
    onErrorShown: () -> Unit
) {
    Box(modifier = Modifier.fillMaxSize()) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 16.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onBack) {
                Icon(Icons.Outlined.ArrowBackIosNew, contentDescription = "Volver")
            }
            Spacer(Modifier.width(8.dp))
            Text("Mi historial", style = MaterialTheme.typography.titleLarge)
        }

        when {
            isLoading -> Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }

            error != null -> Column(
                modifier = Modifier.fillMaxSize(),
                verticalArrangement = Arrangement.Center,
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(error, color = MaterialTheme.colorScheme.error)
                Spacer(Modifier.height(12.dp))
                Button(onClick = {
                    onErrorShown()
                    onRetry()
                }) {
                    Text("Reintentar")
                }
            }

            checkInOutEntries.isEmpty() && paymentEntries.isEmpty() -> Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = "No hay actividad registrada aún",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            else -> LazyColumn(
                verticalArrangement = Arrangement.spacedBy(12.dp),
                modifier = Modifier.padding(bottom = 24.dp)
            ) {
                item {
                    Text(
                        text = "Check-ins y check-outs",
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.onSurface,
                        modifier = Modifier.padding(top = 4.dp, bottom = 4.dp)
                    )
                }
                if (checkInOutEntries.isEmpty()) {
                    item {
                        Text(
                            text = "Sin registros",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                } else {
                    items(checkInOutEntries, key = { it.id }) { entry ->
                        AuditHistoryCard(entry = entry, onInvoiceClick = null)
                    }
                }

                item { Spacer(Modifier.height(8.dp)) }

                item {
                    Text(
                        text = "Pagos",
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.onSurface,
                        modifier = Modifier.padding(top = 8.dp, bottom = 4.dp)
                    )
                }
                if (paymentEntries.isEmpty()) {
                    item {
                        Text(
                            text = "Sin registros",
                            style = MaterialTheme.typography.bodyMedium,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                } else {
                    items(paymentEntries, key = { it.id }) { entry ->
                        val bid = entry.bookingId
                        AuditHistoryCard(
                            entry = entry,
                            onInvoiceClick = if (entry.isPayment && bid != null) {
                                { onOpenInvoice(bid) }
                            } else null
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

@Composable
private fun AuditHistoryCard(
    entry: AuditHistoryEntry,
    onInvoiceClick: (() -> Unit)?
) {
    val icon: ImageVector = if (entry.isPayment) Icons.Outlined.Payments else Icons.Outlined.Event
    ElevatedCard(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(16.dp)
    ) {
        Row(
            modifier = Modifier.padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(
                imageVector = icon,
                contentDescription = null,
                tint = MaterialTheme.colorScheme.primary,
                modifier = Modifier.padding(end = 12.dp)
            )
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = entry.actionLabel,
                    style = MaterialTheme.typography.titleSmall
                )
                Spacer(Modifier.height(4.dp))
                Text(
                    text = entry.dateTimeText,
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.primary
                )
            }
            if (onInvoiceClick != null) {
                TextButton(onClick = onInvoiceClick) {
                    Text("Factura PDF")
                }
            }
        }
    }
}

@Composable
fun MyAuditHistoryScreenState(
    viewModel: MyAuditHistoryViewModel,
    navController: NavController
) {
    val context = LocalContext.current
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val error by viewModel.errorMessage.collectAsStateWithLifecycle()

    MyAuditHistoryScreen(
        isLoading = uiState.isLoading,
        checkInOutEntries = uiState.checkInOutEntries,
        paymentEntries = uiState.paymentEntries,
        error = error,
        invoiceOpening = uiState.invoiceOpening,
        onOpenInvoice = { bookingId -> viewModel.openInvoicePdf(bookingId, context) },
        onBack = { navController.popBackStack() },
        onRetry = { viewModel.load() },
        onErrorShown = { viewModel.clearError() }
    )
}
