package com.example.intermodular.views.navigation

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.NavHost
import androidx.navigation.NavHostController
import androidx.navigation.NavType
import androidx.navigation.compose.composable
import androidx.navigation.navArgument
import com.example.intermodular.data.remote.RetrofitProvider
import com.example.intermodular.data.remote.auth.SessionManager
import com.example.intermodular.data.repository.BookingRepository
import com.example.intermodular.data.repository.LoginRepository
import com.example.intermodular.data.repository.RegisterRepository
import com.example.intermodular.data.repository.ReviewRepository
import com.example.intermodular.viewmodels.BookingViewModel
import com.example.intermodular.viewmodels.viewModelFacotry.BookingViewModelFactory
import com.example.intermodular.viewmodels.MyBookingsViewModel
import com.example.intermodular.viewmodels.viewModelFacotry.MyBookingsViewModelFactory
import com.example.intermodular.views.screens.BookingScreenState
import com.example.intermodular.views.screens.MyBookingsScreenState
import com.example.intermodular.views.screens.RoomScreen
import com.example.intermodular.data.repository.RoomRepository
import com.example.intermodular.data.repository.UserRepository
import com.example.intermodular.viewmodels.UserViewModel
import com.example.intermodular.viewmodels.LoginViewModel
import com.example.intermodular.viewmodels.viewModelFacotry.LoginViewModelFactory
import com.example.intermodular.viewmodels.RegisterViewModel
import com.example.intermodular.viewmodels.viewModelFacotry.RegisterViewModelFactory
import com.example.intermodular.viewmodels.RoomViewModel
import com.example.intermodular.views.screens.LoginScreen
import com.example.intermodular.views.screens.RegisterScreen
import com.example.intermodular.viewmodels.MyBookingDetailsViewModel
import com.example.intermodular.viewmodels.NewBookingViewModel
import com.example.intermodular.viewmodels.RoomDetailViewModel
import com.example.intermodular.viewmodels.viewModelFacotry.MyBookingDetailsViewModelFactory
import com.example.intermodular.viewmodels.viewModelFacotry.NewBookingViewModelFactory
import com.example.intermodular.viewmodels.viewModelFacotry.RoomViewModelFactory
import com.example.intermodular.viewmodels.viewModelFacotry.RoomDetailViewModelFactory
import com.example.intermodular.viewmodels.MyAuditHistoryViewModel
import com.example.intermodular.viewmodels.viewModelFactory.UserViewModelFactory
import com.example.intermodular.viewmodels.viewModelFactory.MyAuditHistoryViewModelFactory
import com.example.intermodular.viewmodels.PaymentViewModel
import com.example.intermodular.viewmodels.viewModelFacotry.PaymentViewModelFactory
import com.example.intermodular.views.screens.MyBookingDetailsState
import com.example.intermodular.views.screens.NewBookingState
import com.example.intermodular.views.screens.PaymentState
import com.example.intermodular.views.screens.RoomDetailScreen
import com.example.intermodular.views.screens.UpdateProfileScreenState
import com.example.intermodular.views.screens.MyAuditHistoryScreenState
import com.example.intermodular.views.screens.UserScreenState

@Composable
fun Navigation(
    navigationController: NavHostController,
    modifier: Modifier
) {
    NavHost (
        navController = navigationController,
        startDestination = Routes.Login.route,
        modifier = modifier
    ) {

        /**
         * Pantalla principal de búsqueda y filtrado de habitaciones.
         *
         * @author Axel Zaragoci
         *
         * - **Ruta** - [Routes.Bookings]
         * - **ViewModel** - [BookingViewModel]
         * - **Repositorios** - BookingRepository, RoomRepository
         * - **Navegación** - Puede navegar a BookRoom
         */
        composable(Routes.Bookings.route) {
            val api = RetrofitProvider.api
            val bookingRepository = BookingRepository(api)
            val roomRepository = RoomRepository(api)

            val viewModel: BookingViewModel = viewModel(
                factory = BookingViewModelFactory(bookingRepository, roomRepository)
            )

            BookingScreenState(
                viewModel = viewModel,
                navController = navigationController
            )
        }

        composable(Routes.Rooms.route) {
            val api = RetrofitProvider.api
            val repository = RoomRepository(api)
            
             val viewModel: RoomViewModel = viewModel(
                factory = RoomViewModelFactory(repository)
            )

            RoomScreen(
                roomViewModel = viewModel,
                onRoomClick = { roomId ->
                    navigationController.navigate(Routes.RoomDetail.createRoute(roomId))
                }
            )
        }

        /**
         * Ruta de navegación hacia la pantalla de perfil de usuario.
         *
         * Flujo:
         * 1. Se obtiene la instancia de la API desde [RetrofitProvider].
         * 2. Se crea el repositorio [UserRepository] utilizando dicha API.
         * 3. Se obtiene la instancia del [SessionManager] para la gestión de sesión.
         * 4. Se construye el [UserViewModel] mediante su fábrica [UserViewModelFactory].
         * 5. Se carga la pantalla con estado [UserScreenState].
         *
         * Esta pantalla permite:
         * - Visualizar datos del perfil
         * - Cambiar foto
         * - Editar perfil
         * - Cambiar contraseña
         * - Navegar a reservas
         *
         * @author Ian Rodriguez
         */
        composable(Routes.User.route) {

            // Obtener dependencias necesarias
            val api = RetrofitProvider.api
            val repository = UserRepository(api)
            val sessionManager = SessionManager

            // Crear ViewModel con su factory personalizada
            val viewModel: UserViewModel = viewModel(
                factory = UserViewModelFactory(repository, sessionManager)
            )

            // Cargar pantalla conectada al estado
            UserScreenState(
                viewModel = viewModel,
                navController = navigationController
            )
        }

        composable(Routes.MyHistory.route) {
            val api = RetrofitProvider.api
            val bookingRepository = BookingRepository(api)
            val viewModel: MyAuditHistoryViewModel = viewModel(
                factory = MyAuditHistoryViewModelFactory(bookingRepository)
            )
            MyAuditHistoryScreenState(
                viewModel = viewModel,
                navController = navigationController
            )
        }

        /**
         * Ruta de navegación hacia la pantalla de edición de perfil.
         *
         * Flujo:
         * 1. Se obtiene la API desde [RetrofitProvider].
         * 2. Se crea el [UserRepository].
         * 3. Se pasa el [SessionManager] al ViewModel.
         * 4. Se instancia el [UserViewModel] mediante [UserViewModelFactory].
         * 5. Se carga [UpdateProfileScreenState].
         *
         * Esta pantalla permite modificar y guardar los datos personales del usuario.
         *
         * @author Ian Rodriguez
         */
        composable(Routes.UpdateProfile.route) {

            // Obtener dependencias necesarias
            val api = RetrofitProvider.api
            val repository = UserRepository(api)
            val sessionManager = SessionManager

            // Crear ViewModel con su factory personalizada
            val viewModel: UserViewModel = viewModel(
                factory = UserViewModelFactory(repository, sessionManager)
            )

            // Cargar pantalla conectada al estado
            UpdateProfileScreenState(
                viewModel = viewModel,
                navController = navigationController
            )
        }

        /**
         * Pantalla que lista todas las reservas del usuario actual.
         *
         * @author Axel Zaragoci
         *
         * - **Ruta** - [Routes.MyBookings]
         * - **ViewModel** - [MyBookingsViewModel]
         * - **Repositorios** - BookingRepository, RoomRepository
         * - **Navegación** - Puede navegar a MyBookingDetails
         */
        composable(Routes.MyBookings.route) {
            val api = RetrofitProvider.api
            val bookingRepository = BookingRepository(api)
            val roomRepository = RoomRepository(api)

            val viewModel : MyBookingsViewModel = viewModel(
                factory = MyBookingsViewModelFactory(bookingRepository, roomRepository)
            )

            MyBookingsScreenState(
                viewModel = viewModel,
                navController = navigationController
            )
        }

        /**
         * Pantalla para crear una nueva reserva.
         *
         * @author Axel Zaragoci
         *
         * ## Parámetros de ruta:
         * - **roomId** - ID de la habitación a reservar (requerido)
         * - **startDate** - Fecha de entrada (timestamp)
         * - **endDate** - Fecha de salida (timestamp)
         * - **guests** - Número de huéspedes
         *
         * - **Ruta** - [Routes.BookRoom]
         * - **ViewModel** - [NewBookingViewModel]
         * - **Repositorios** - BookingRepository, RoomRepository
         */
        composable(
            route = Routes.BookRoom.route,
            arguments = listOf(
                navArgument("roomId") { type = NavType.StringType },
                navArgument("startDate") { type = NavType.LongType },
                navArgument("endDate") { type = NavType.LongType },
                navArgument("guests") { type = NavType.StringType }
            )
        ) { backStackEntry ->

            val roomId = backStackEntry.arguments?.getString("roomId")!!
            val startDate = backStackEntry.arguments?.getLong("startDate")
            val endDate = backStackEntry.arguments?.getLong("endDate")
            val guests = backStackEntry.arguments?.getString("guests")!!

            val api = RetrofitProvider.api
            val bookingRepository = BookingRepository(api)
            val roomRepository = RoomRepository(api)

            val viewModel: NewBookingViewModel = viewModel(
                factory = NewBookingViewModelFactory(bookingRepository, roomRepository, roomId, startDate!!, endDate!!, guests)
            )

            NewBookingState(
                viewModel = viewModel,
                onNavigateToPayment = { bookingId ->
                    navigationController.navigate(Routes.Payment.createRoute(bookingId)) {
                        popUpTo(Routes.Bookings.route) // Opcional, para limpiar la pila de pantallas si se desea
                    }
                }
            )
        }

        /**
         * Pantalla de detalle y actualización de una reserva existente y creación de reseña.
         *
         * @author Axel Zaragoci
         *
         * ## Parámetros de ruta:
         * - **bookingId** - ID de la reserva a mostrar (requerido)
         *
         * - **Ruta** - [Routes.MyBookingDetails]
         * - **ViewModel** - [MyBookingDetailsViewModel]
         * - **Repositorios** - BookingRepository, RoomRepository, ReviewRepository
         */
        composable(
            route = Routes.MyBookingDetails.route,
            arguments = listOf(
                navArgument("bookingId") { type = NavType.StringType }
            )
        ) { backStackEntry ->
            val bookingId = backStackEntry.arguments?.getString("bookingId")!!

            val api = RetrofitProvider.api
            val bookingRepository = BookingRepository(api)
            val roomRepository = RoomRepository(api)
            val reviewRepository = ReviewRepository(api)

            val viewModel: MyBookingDetailsViewModel = viewModel(
                factory = MyBookingDetailsViewModelFactory(
                    bookingId,
                    bookingRepository,
                    roomRepository,
                    reviewRepository
                )
            )

            MyBookingDetailsState(
                viewModel = viewModel,
                onNavigateToPayment = { id ->
                    navigationController.navigate(Routes.Payment.createRoute(id))
                }
            )
        }
        
        /**
         * Pantalla de pago interactivo post-reserva/modificación
         */
        composable(
            route = Routes.Payment.route,
            arguments = listOf(
                navArgument("bookingId") { type = NavType.StringType }
            )
        ) { backStackEntry ->
            val bookingId = backStackEntry.arguments?.getString("bookingId")!!
            val api = RetrofitProvider.api
            val bookingRepository = BookingRepository(api)
            
            val viewModel: PaymentViewModel = viewModel(
                factory = PaymentViewModelFactory(bookingRepository, bookingId)
            )
            
            PaymentState(
                viewModel = viewModel,
                onNavigateNext = {
                    navigationController.navigate(Routes.MyBookings.route) {
                        popUpTo(Routes.Bookings.route)
                    }
                }
            )
        }

        composable(
            route = Routes.RoomDetail.route,
            arguments = listOf(
                navArgument("roomId") { type = NavType.StringType }
            )
        ) { backStackEntry ->
            val roomId = backStackEntry.arguments?.getString("roomId")!!
            
            val api = RetrofitProvider.api
            val roomRepository = RoomRepository(api)
            val reviewRepository = ReviewRepository(api)
            
            val viewModel: RoomDetailViewModel = viewModel(
                factory = RoomDetailViewModelFactory(
                    roomId,
                    roomRepository,
                    reviewRepository
                )
            )
            
            RoomDetailScreen(
                viewModel = viewModel,
                onBackClick = { navigationController.popBackStack() }
            )
        }
        composable(Routes.Login.route) {
            val api = RetrofitProvider.api
            val repository = LoginRepository(api)
            val viewModel: LoginViewModel = viewModel(factory = LoginViewModelFactory(repository))


            LoginScreen(
                viewModel = viewModel,
                onLoginSuccess = {
                    navigationController.navigate(Routes.Bookings.route) {
                        popUpTo(Routes.Login.route) { inclusive = true }
                    }
                },
                onNavToRegister = {
                    navigationController.navigate(Routes.Register.route)
                }
            )
        }
        composable(Routes.Register.route) {
            val api = RetrofitProvider.api

            val loginRepository = LoginRepository(api)
            val loginViewModel: LoginViewModel = viewModel(factory = LoginViewModelFactory(loginRepository))
            val registerRepository = RegisterRepository(api, loginRepository)

            val viewModel: RegisterViewModel = viewModel(
                factory = RegisterViewModelFactory(registerRepository)
            )

            RegisterScreen(
                viewModel = viewModel,
                onRegisterSuccess = {
                    navigationController.navigate(Routes.Bookings.route) {
                        popUpTo(Routes.Register.route) { inclusive = true }
                    }
                },
                onNavToLogin = {
                    navigationController.popBackStack()
                }
            )
        }
    }
}