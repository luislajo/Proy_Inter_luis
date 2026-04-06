package com.example.intermodular.views.screens

import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.PickVisualMediaRequest
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.outlined.Badge
import androidx.compose.material.icons.outlined.CalendarMonth
import androidx.compose.material.icons.outlined.Edit
import androidx.compose.material.icons.outlined.Email
import androidx.compose.material.icons.outlined.LocationOn
import androidx.compose.material.icons.outlined.Lock
import androidx.compose.material.icons.outlined.Person
import androidx.compose.material.icons.outlined.Phone
import androidx.compose.material.icons.outlined.History
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Divider
import androidx.compose.material3.ElevatedCard
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.painter.ColorPainter
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.navigation.NavController
import coil.compose.AsyncImage
import coil.request.ImageRequest
import com.example.intermodular.BuildConfig
import com.example.intermodular.data.remote.utils.uriToMultipart
import com.example.intermodular.models.UserModel
import com.example.intermodular.viewmodels.UserViewModel
import com.example.intermodular.views.components.ChangePasswordDialog
import com.example.intermodular.views.components.ProfileAvatar
import com.example.intermodular.views.components.ProfileRow
import com.example.intermodular.views.navigation.Routes
import java.time.format.DateTimeFormatter

/**
 * Pantalla de perfil de usuario.
 *
 * Esta pantalla muestra el estado del perfil del usuario y permite acceder a acciones típicas
 * relacionadas con su cuenta:
 * - Visualizar la información personal del usuario
 * - Cambiar la foto de perfil (seleccionando una imagen de la galería)
 * - Navegar al listado de reservas del usuario
 * - Navegar a la pantalla de edición de perfil
 * - Abrir el diálogo para cambiar la contraseña
 *
 * Estados gestionados en UI:
 * 1. **Carga**: muestra un [CircularProgressIndicator]
 * 2. **Error**: muestra el mensaje y un botón "Reintentar"
 * 3. **Contenido**: muestra avatar, nombre y tarjeta con información personal
 *
 * @author Ian Rodriguez
 *
 * @param isLoading - Indica si se están cargando los datos del usuario
 * @param user - Modelo del usuario a mostrar (null si aún no se ha cargado o hubo error)
 * @param error - Mensaje de error a mostrar (null si no hay error)
 * @param onRetry - Callback para reintentar la carga de datos
 * @param onErrorShown - Callback para limpiar/confirmar el error una vez mostrado
 * @param onViewMyBookings - Callback al pulsar "Mis reservas"
 * @param onMyHistory - Callback al pulsar "Mi historial" (auditoría de check-ins y pagos)
 * @param onChangePhoto - Callback al pulsar el botón de editar foto (dispara la selección de imagen)
 * @param onEditProfile - Callback al pulsar el botón de editar perfil
 * @param onChangePasswordClick - Callback al pulsar el botón de cambiar contraseña
 */
@Composable
fun UserScreen(
    isLoading: Boolean,
    user: UserModel?,
    error: String?,
    onRetry: () -> Unit,
    onErrorShown: () -> Unit,
    onViewMyBookings: () -> Unit,
    onMyHistory: () -> Unit,
    onChangePhoto: () -> Unit,
    onEditProfile: () -> Unit,
    onChangePasswordClick: () -> Unit
) {
    when {
        // Indicador de carga
        isLoading -> Box(
            modifier = Modifier.fillMaxSize(),
            contentAlignment = Alignment.Center
        ) {
            CircularProgressIndicator()
        }

        // Mostrar error + opción de reintentar
        error != null -> {
            Column(
                modifier = Modifier.fillMaxSize(),
                verticalArrangement = Arrangement.Center,
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(error)
                Spacer(modifier = Modifier.height(8.dp))
                Button(onClick = {
                    onErrorShown()
                    onRetry()
                }) {
                    Text("Reintentar")
                }
            }
        }

        // Contenido principal cuando hay usuario cargado
        user != null -> {

            val formattedDate = user.birthDate.format(
                DateTimeFormatter.ofPattern("dd/MM/yyyy")
            )

            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(horizontal = 16.dp)
                    .verticalScroll(rememberScrollState()),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Spacer(Modifier.height(18.dp))

                // Avatar con opción de cambiar foto y badge VIP
                ProfileAvatar (
                    imageRoute = user.imageRoute,
                    initials = "${user.firstName.take(1)}${user.lastName.take(1)}".uppercase(),
                    isVip = user.vipStatus,
                    onChangePhoto
                )

                Spacer(Modifier.height(14.dp))

                // Nombre del usuario
                Text(
                    text = "${user.firstName} ${user.lastName}".trim(),
                    style = MaterialTheme.typography.titleLarge
                )

                Spacer(Modifier.height(18.dp))

                // Tarjeta de información personal + accesos a editar/cambiar contraseña
                ElevatedCard(
                    modifier = Modifier
                        .fillMaxWidth()
                        .heightIn(min = 240.dp),
                    shape = RoundedCornerShape(22.dp)
                ) {
                    Column(
                        modifier = Modifier.padding(16.dp),
                        verticalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Text(
                                text = "Informacion Personal",
                                style = MaterialTheme.typography.titleMedium
                            )

                            IconButton(onClick = { onChangePasswordClick() }) {
                                Icon(Icons.Outlined.Lock, contentDescription = "Cambiar contraseña")
                            }
                            IconButton(onClick = onEditProfile) {
                                Icon(Icons.Outlined.Edit, contentDescription = "Editar perfil")
                            }
                        }

                        Divider()

                        // Filas de datos personales
                        ProfileRow(
                            icon = Icons.Outlined.Email,
                            label = "Email",
                            value = user.email
                        )
                        ProfileRow(
                            icon = Icons.Outlined.Badge,
                            label = "DNI",
                            value = user.dni
                        )
                        ProfileRow(
                            icon = Icons.Outlined.Phone,
                            label = "Teléfono",
                            value = user.phoneNumber?.toString() ?: "Desconocido"
                        )
                        ProfileRow(
                            icon = Icons.Outlined.CalendarMonth,
                            label = "Nacimiento",
                            value = formattedDate
                        )
                        ProfileRow(
                            icon = Icons.Outlined.LocationOn,
                            label = "Ciudad",
                            value = user.cityName
                        )
                        ProfileRow(
                            icon = Icons.Outlined.Person,
                            label = "Género",
                            value = user.gender
                        )
                    }
                }

                Spacer(Modifier.height(22.dp))

                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(bottom = 32.dp),
                    horizontalArrangement = Arrangement.spacedBy(12.dp),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Button(
                        onClick = onViewMyBookings,
                        modifier = Modifier.weight(1f)
                    ) {
                        Text(
                            text = "Mis reservas",
                            maxLines = 2,
                            style = MaterialTheme.typography.labelLarge
                        )
                    }
                    Button(
                        onClick = onMyHistory,
                        modifier = Modifier.weight(1f)
                    ) {
                        Row(
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.Center,
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Icon(
                                imageVector = Icons.Outlined.History,
                                contentDescription = null,
                                modifier = Modifier.size(18.dp)
                            )
                            Spacer(Modifier.width(6.dp))
                            Text(
                                text = "Mi historial",
                                maxLines = 2,
                                style = MaterialTheme.typography.labelLarge
                            )
                        }
                    }
                }
            }
        }
    }
}


/**
 * Versión con estado de [UserScreen] conectada a [UserViewModel].
 *
 * Funciones principales:
 * 1. Recolectar el estado de UI desde el ViewModel usando
 * 2. Lanzar el selector de imagen para actualizar la foto y enviarla como multipart
 * 3. Mostrar el diálogo de cambio de contraseña
 * 4. Refrescar los datos del usuario al volver a primer plano
 * 5. Gestionar navegación a pantallas de:
 *    - Mis reservas
 *    - Actualización de perfil
 *
 * @author Ian Rodriguez
 *
 * @param viewModel - ViewModel que expone el estado de perfil y operaciones (refresh, updatePhoto, changePassword, etc.)
 * @param navController - Controlador de navegación para moverse entre pantallas
 */
@Composable
fun UserScreenState(
    viewModel: UserViewModel,
    navController: NavController
) {
    val navigation = navController

    // Estados expuestos por el ViewModel
    val uiState by viewModel.uiState.collectAsStateWithLifecycle()
    val error by viewModel.errorMessage.collectAsStateWithLifecycle()

    val context = LocalContext.current

    // Launcher para seleccionar una imagen de la galería y actualizar la foto del perfil
    val pickImageLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.PickVisualMedia()
    ) { uri ->
        if (uri != null) {
            val part = uriToMultipart(context, uri, partName = "photo")
            viewModel.updatePhoto(part)
        }
    }

    // Control de visibilidad del diálogo de cambio de contraseña
    var showChangePassDialog by remember { mutableStateOf(false) }

    if (showChangePassDialog) {
        ChangePasswordDialog(
            onBack = { showChangePassDialog = false },
            onSave = { oldPass, newPass, repeatPass ->
                showChangePassDialog = false
                viewModel.changePassword(oldPass, newPass, repeatPass)
            }
        )
    }

    // Refrescar datos al volver a la pantalla
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) {
        viewModel.refresh()
    }

    // Carga de la vista sin estado
    UserScreen(
        isLoading = uiState.isLoading,
        user = uiState.user,
        error = error,
        onRetry = { viewModel.refresh() },
        onErrorShown = { viewModel.clearError() },
        onViewMyBookings = { navigation.navigate(Routes.MyBookings.route) },
        onMyHistory = { navigation.navigate(Routes.MyHistory.route) },
        onChangePhoto = {
            pickImageLauncher.launch(
                PickVisualMediaRequest(ActivityResultContracts.PickVisualMedia.ImageOnly)
            )
        },
        onEditProfile = { navigation.navigate(Routes.UpdateProfile.route) },
        onChangePasswordClick = { showChangePassDialog = true }
    )
}