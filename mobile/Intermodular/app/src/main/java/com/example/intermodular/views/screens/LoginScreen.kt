package com.example.intermodular.views.screens

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Person
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import com.example.intermodular.R
import com.example.intermodular.data.remote.auth.DevAutoLogin
import com.example.intermodular.data.remote.auth.SessionManager
import com.example.intermodular.viewmodels.LoginViewModel

/**
 * Pantalla de inicio de sesión.
 *
 * Permite al usuario:
 * - Introducir email y contraseña.
 * - Iniciar sesión en el sistema.
 * - Navegar a la pantalla de registro si no tiene cuenta.
 *
 * Estados gestionados:
 * 1. isLoading → Deshabilita los campos y muestra indicador de carga.
 * 2. error → Muestra mensaje de error si el login falla.
 * 3. Login exitoso → Detecta token válido en SessionManager y ejecuta [onLoginSuccess].
 *
 * Diseño:
 * - Fondo con degradado radial.
 * - Imagen de fondo semitransparente.
 * - Tarjeta central con efecto glassmorphism.
 *
 * @author Ian Rodriguez
 *
 * @param viewModel - ViewModel encargado de la lógica de autenticación
 * @param onLoginSuccess - Callback ejecutado cuando el login es exitoso
 * @param onNavToRegister - Callback para navegar a la pantalla de registro
 */
@Composable
fun LoginScreen(
    viewModel: LoginViewModel,
    onLoginSuccess: () -> Unit,
    onNavToRegister: () -> Unit
) {

    // Estados observados desde el ViewModel
    val isLoading by viewModel.isLoading.collectAsState()
    val error by viewModel.errorMessage.collectAsState()

    // Estados locales para campos de entrada
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }

    LaunchedEffect(Unit) {
        if (DevAutoLogin.ENABLED && SessionManager.getToken().isNullOrBlank()) {
            email = DevAutoLogin.ADMIN_EMAIL
            password = DevAutoLogin.ADMIN_PASSWORD
            viewModel.login(email.trim(), password)
        }
    }

    /**
     * Detecta cuándo el login ha finalizado correctamente.
     * Si deja de estar cargando y existe un token válido en SessionManager,
     * se ejecuta la navegación de éxito.
     */
    LaunchedEffect(isLoading, error) {
        if (!isLoading && !SessionManager.getToken().isNullOrBlank()) {
            onLoginSuccess()
        }
    }

    // Colores del fondo degradado
    val edge = Color(0xFF0F2854)
    val center = Color(0xFF1C4D8D)

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(
                Brush.radialGradient(
                    colors = listOf(center, edge),
                    center = Offset.Unspecified,
                    radius = Float.POSITIVE_INFINITY
                )
            ),
        contentAlignment = Alignment.Center
    ) {

        // Imagen de fondo semitransparente
        Image(
            painter = painterResource(id = R.drawable.hotellogo),
            contentDescription = null,
            modifier = Modifier.fillMaxSize(),
            contentScale = ContentScale.FillWidth,
            alpha = 0.25f
        )

        Column(
            modifier = Modifier.fillMaxSize(),
            verticalArrangement = Arrangement.Center,
            horizontalAlignment = Alignment.CenterHorizontally
        ) {

            // Tarjeta principal del login
            Column(
                modifier = Modifier
                    .padding(25.dp)
                    .shadow(
                        elevation = 20.dp,
                        shape = RoundedCornerShape(20.dp),
                        ambientColor = Color.Black.copy(alpha = 0.2f),
                        spotColor = Color.Black.copy(alpha = 0.2f)
                    )
                    .background(
                        color = Color.White.copy(alpha = 0.08f),
                        shape = RoundedCornerShape(20.dp)
                    )
                    .border(
                        width = 1.dp,
                        color = Color.White.copy(alpha = 0.3f),
                        shape = RoundedCornerShape(20.dp)
                    )
                    .padding(25.dp),
                verticalArrangement = Arrangement.spacedBy(20.dp)
            ) {

                // Icono superior
                Icon(
                    imageVector = Icons.Default.Person,
                    contentDescription = "Usuario",
                    modifier = Modifier
                        .size(80.dp)
                        .align(Alignment.CenterHorizontally),
                    tint = Color.White
                )

                // Campo Email
                TextField(
                    value = email,
                    onValueChange = { email = it },
                    label = { Text("Email") },
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading
                )

                // Campo Contraseña
                TextField(
                    value = password,
                    onValueChange = { password = it },
                    label = { Text("Contraseña") },
                    visualTransformation = PasswordVisualTransformation(),
                    modifier = Modifier.fillMaxWidth(),
                    enabled = !isLoading
                )

                // Mostrar error si existe
                if (error != null) {
                    Text(
                        text = error!!,
                        modifier = Modifier.fillMaxWidth(),
                        textAlign = TextAlign.Center,
                        color = MaterialTheme.colorScheme.error
                    )
                }

                // Botón de login
                Button(
                    onClick = { viewModel.login(email.trim(), password) },
                    enabled = !isLoading,
                    modifier = Modifier
                        .width(150.dp)
                        .align(Alignment.CenterHorizontally)
                ) {
                    if (isLoading) {
                        CircularProgressIndicator(modifier = Modifier.size(18.dp))
                        Spacer(Modifier.width(10.dp))
                        Text("Entrando")
                    } else {
                        Text("Entrar")
                    }
                }
            }

            // Navegación a registro
            Column {
                TextButton(
                    onClick = onNavToRegister,
                    enabled = !isLoading,
                    modifier = Modifier.align(Alignment.CenterHorizontally)
                ) {
                    Text("¿No tienes cuenta? Regístrate")
                }
            }
        }
    }
}
