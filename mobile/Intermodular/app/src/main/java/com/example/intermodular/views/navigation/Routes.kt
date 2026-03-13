package com.example.intermodular.views.navigation

/**
 * Clase que define todas las rutas de navegación
 *
 * @property route - [String] que representa la ruta en el formato de navegación para Compose
 */
sealed class Routes(
    val route: String
) {
    /**
     * Pantalla principal de reservas
     * @author Axel Zaragoci
     */
    object Bookings : Routes("bookings")

    object Rooms : Routes("rooms")

    object User : Routes("user")
    object  Login : Routes("login")
    object Register : Routes("register")

    object UpdateProfile : Routes("updateProfile")

    /**
     * Pantalla de reservas del usuario
     * @author Axel Zaragoci
     */
    object MyBookings : Routes("myBookings")

    /**
     * Pantalla de detalles y actualizar reserva
     * @author Axel Zaragoci
     */
    object MyBookingDetails : Routes ("details/{bookingId}") {
        /**
         * Crea una ruta con el ID de reserva indicado
         *
         * @param bookingId - [String] con el ID de la reserva a mostrar
         * @return Ruta completa con el ID insertado
         */
        fun createRoute(bookingId: String) = "details/$bookingId"
    }

    /**
     * Pantalla de pago por pasarela simulada
     */
    object Payment : Routes("payment/{bookingId}") {
        fun createRoute(bookingId: String) = "payment/$bookingId"
    }

    /**
     * Pantalla para crear una nueva reserva
     * @author Axel Zaragoci
     */
    object BookRoom : Routes ("bookRoom/{roomId}?startDate={startDate}&endDate={endDate}&guests={guests}") {

        /**
         * Crea una ruta completa con los parámetros
         *
         * @param roomId - ID de la habitación a reservar (requerido)
         * @param startDate - Fecha de inicio en milisegundos
         * @param endDate - Fecha de fin en milisegundos
         * @param guests - Número de huéspedes como String
         */
        fun createRoute(roomId: String,
                        startDate : Long,
                        endDate : Long,
                        guests : String) = "bookRoom/$roomId?startDate=$startDate&endDate=$endDate&guests=$guests"
    }
    
    object RoomDetail : Routes("roomDetail/{roomId}") {
        fun createRoute(roomId: String) = "roomDetail/$roomId"
    }
}