package com.example.intermodular.views.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Star
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import coil.ImageLoader
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.example.intermodular.BuildConfig
import com.example.intermodular.models.Review
import com.example.intermodular.viewmodels.RoomDetailViewModel

/**
 * Pantalla que muestra el detalle de una habitación específica, incluyendo información detallada,
 * imágenes, características y reseñas asociadas.
 *
 * @param viewModel ViewModel que proporciona los datos y el estado de la pantalla.
 * @param onBackClick Función lambda que se ejecuta al pulsar la acción de volver (aunque la navegación actual es por sistema).
 */
@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RoomDetailScreen(
    viewModel: RoomDetailViewModel,
    onBackClick: () -> Unit
) {
    val roomState by viewModel.room.collectAsStateWithLifecycle()
    val currentRoom = roomState
    val reviews by viewModel.reviews.collectAsStateWithLifecycle()
    val isLoading by viewModel.isLoading.collectAsStateWithLifecycle()
    val errorMessage by viewModel.errorMessage.collectAsStateWithLifecycle()
    val averageRating by viewModel.averageRating.collectAsStateWithLifecycle()
    
    // List of all images to show
    val allImages = remember(currentRoom) {
        if (currentRoom != null) {
            (listOf(currentRoom.mainImage) + currentRoom.extraImages).filter { it.isNotBlank() }
        } else {
            emptyList()
        }
    }

    val context = LocalContext.current
    LaunchedEffect(allImages) {
        allImages.forEach { imagePath ->
            val relativePath = if (imagePath.startsWith("/")) imagePath.substring(1) else imagePath
            val imageUrl = if (imagePath.startsWith("http")) imagePath else "${BuildConfig.BASE_URL}$relativePath"
            
            val request = ImageRequest.Builder(context)
                .data(imageUrl)
                // Optional: You can set diskCachePolicy/memoryCachePolicy if you want to be explicit, but defaults are usually fine
                .build()
            ImageLoader(context).enqueue(request)
        }
    }

    Surface(
        modifier = Modifier.fillMaxSize(),
        color = MaterialTheme.colorScheme.background
    ) {
        if (currentRoom == null) {
            Box(
                modifier = Modifier.fillMaxSize(),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator()
            }
        } else {
            LazyColumn(
                modifier = Modifier.fillMaxSize()
            ) {
                item {
                    if (allImages.isNotEmpty()) {
                        val pagerState = androidx.compose.foundation.pager.rememberPagerState(pageCount = { allImages.size })
                        
                        Box {
                            androidx.compose.foundation.pager.HorizontalPager(
                                state = pagerState,
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .height(250.dp)
                            ) { page ->
                                val imagePath = allImages[page]
                                val relativePath = if (imagePath.startsWith("/")) imagePath.substring(1) else imagePath
                                val imageUrl = if (imagePath.startsWith("http")) imagePath else "${BuildConfig.BASE_URL}$relativePath"
                                
                                AsyncImage(
                                    model = imageUrl,
                                    contentDescription = "Imagen de la habitación",
                                    modifier = Modifier.fillMaxSize(),
                                    contentScale = ContentScale.Crop
                                )
                            }
                            
                            // Optional: Add page indicator if needed, but not explicitly requested. 
                            // Keeping it simple as requested: "carrusel con la mainImage y etxtraImages"
                            
                            // Page indicator (simple text 1/N)
                            if (allImages.size > 1) {
                                Surface(
                                    modifier = Modifier
                                        .align(Alignment.BottomEnd)
                                        .padding(8.dp),
                                    shape = MaterialTheme.shapes.small,
                                    color = MaterialTheme.colorScheme.surface.copy(alpha = 0.7f)
                                ) {
                                    Text(
                                        text = "${pagerState.currentPage + 1}/${allImages.size}",
                                        style = MaterialTheme.typography.labelSmall,
                                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp)
                                    )
                                }
                            }
                        }
                    }
                }

            item {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(16.dp)
                ) {
                    Text(
                        text = "Habitación ${currentRoom.roomNumber}",
                        style = MaterialTheme.typography.headlineMedium,
                        fontWeight = FontWeight.Bold
                    )
                    
                    Spacer(modifier = Modifier.height(8.dp))
                    
                    Text(
                        text = currentRoom.type,
                        style = MaterialTheme.typography.titleMedium,
                        color = MaterialTheme.colorScheme.primary
                    )

                    if (currentRoom.status == "maintenance" || currentRoom.status == "blocked") {
                        Spacer(modifier = Modifier.height(10.dp))
                        Text(
                            text = "Próximamente libre",
                            style = MaterialTheme.typography.titleMedium,
                            color = MaterialTheme.colorScheme.error
                        )
                    }
                    
                    Spacer(modifier = Modifier.height(16.dp))
                    
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween
                    ) {
                        Text(
                            text = "${currentRoom.pricePerNight}€/noche",
                            style = MaterialTheme.typography.titleLarge,
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.primary
                        )
                        
                        // Only show rating if there are reviews
                        averageRating?.let { rating ->
                            Row(
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Icon(
                                    Icons.Default.Star,
                                    contentDescription = null,
                                    tint = MaterialTheme.colorScheme.primary,
                                    modifier = Modifier.size(20.dp)
                                )
                                Text(
                                    text = " ${String.format("%.1f", rating)}/5",
                                    style = MaterialTheme.typography.bodyLarge
                                )
                            }
                        }
                    }
                    
                    Spacer(modifier = Modifier.height(16.dp))
                    
                    Text(
                        text = "Descripción",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )
                    
                    Spacer(modifier = Modifier.height(8.dp))
                    
                    Text(
                        text = currentRoom.description,
                        style = MaterialTheme.typography.bodyMedium
                    )
                    
                    Spacer(modifier = Modifier.height(16.dp))
                    
                    Text(
                        text = "Características",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.Bold
                    )
                    
                    Spacer(modifier = Modifier.height(8.dp))
                    
                    CharacteristicItem("Máximo de huéspedes", "${currentRoom.maxGuests} personas")
                    CharacteristicItem("Cama extra", if (currentRoom.extraBed) "Disponible" else "No disponible")
                    CharacteristicItem("Cuna", if (currentRoom.crib) "Disponible" else "No disponible")
                    
                    if (currentRoom.extras.isNotEmpty()) {
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "Extras: ${currentRoom.extras.joinToString(", ")}",
                            style = MaterialTheme.typography.bodyMedium
                        )
                    }
                }
            }

            item {
                Divider(modifier = Modifier.padding(vertical = 16.dp))
                
                Text(
                    text = "Reseñas (${reviews.size})",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.padding(horizontal = 16.dp)
                )
                
                Spacer(modifier = Modifier.height(16.dp))
            }

            if (isLoading) {
                item {
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(32.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        CircularProgressIndicator()
                    }
                }
            } else if (errorMessage != null) {
                item {
                    Text(
                        text = errorMessage ?: "Error desconocido",
                        color = MaterialTheme.colorScheme.error,
                        modifier = Modifier.padding(16.dp)
                    )
                }
            } else if (reviews.isEmpty()) {
                item {
                    Text(
                        text = "No hay reseñas aún",
                        style = MaterialTheme.typography.bodyMedium,
                        modifier = Modifier.padding(16.dp)
                    )
                }
            } else {
                items(reviews) { review ->
                    ReviewCard(review)
                }
            }
        }
        }
    }
}

/**
 * Composable que muestra un item de característica de la habitación (etiqueta y valor).
 *
 * @param label Etiqueta descriptiva de la característica.
 * @param value Valor de la característica.
 */
@Composable
fun CharacteristicItem(label: String, value: String) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(
            text = label,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Text(
            text = value,
            style = MaterialTheme.typography.bodyMedium,
            fontWeight = FontWeight.Medium
        )
    }
}

/**
 * Composable que muestra la información de una reseña individual en una tarjeta.
 *
 * @param review Objeto [Review] que contiene los datos de la reseña a mostrar.
 */
@Composable
fun ReviewCard(review: Review) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 8.dp),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Text(
                    text = review.userName ?: "Usuario",
                    style = MaterialTheme.typography.titleSmall,
                    fontWeight = FontWeight.Bold
                )
                
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Icon(
                        Icons.Default.Star,
                        contentDescription = null,
                        tint = MaterialTheme.colorScheme.primary,
                        modifier = Modifier.size(16.dp)
                    )
                    Text(
                        text = " ${review.rating}",
                        style = MaterialTheme.typography.bodyMedium
                    )
                }
            }
            
            Spacer(modifier = Modifier.height(8.dp))
            
            Text(
                text = review.description,
                style = MaterialTheme.typography.bodyMedium
            )
            
            Spacer(modifier = Modifier.height(4.dp))
            
            Text(
                text = review.createdAt.substringBefore("T"),
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
    }
}
