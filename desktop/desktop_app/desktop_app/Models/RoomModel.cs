using desktop_app.Services;
using System.Text.Json.Serialization;

namespace desktop_app.Models
{
    /// <summary>
    /// Representa una habitación de hotel.
    /// </summary>
    public class RoomModel
    {
        /// <summary>Identificador único (MongoDB ObjectId).</summary>
        [JsonPropertyName("_id")]
        public string Id { get; set; } = "";

        /// <summary>Tipo de habitación (single, double, suite, family).</summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        /// <summary>Número de habitación.</summary>
        [JsonPropertyName("roomNumber")]
        public string RoomNumber { get; set; } = "";

        /// <summary>Capacidad máxima de huéspedes.</summary>
        [JsonPropertyName("maxGuests")]
        public int MaxGuests { get; set; }

        /// <summary>Descripción de la habitación.</summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        /// <summary>Ruta relativa de la imagen principal ("/uploads/foto.jpg").</summary>
        [JsonPropertyName("mainImage")]
        public string? MainImage { get; set; }

        /// <summary>URL absoluta de la imagen principal (propiedad calculada).</summary>
        public string? MainImageAbs
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MainImage)) return null;
                if (Uri.TryCreate(MainImage, UriKind.Absolute, out _))
                    return MainImage;
                return ImageService.ToAbsoluteUrl(MainImage);
            }
        }

        /// <summary>Precio por noche en euros.</summary>
        [JsonPropertyName("pricePerNight")]
        public decimal? PricePerNight { get; set; }

        /// <summary>Indica si tiene cama extra disponible.</summary>
        [JsonPropertyName("extraBed")]
        public bool ExtraBed { get; set; }

        /// <summary>Indica si tiene cuna disponible.</summary>
        [JsonPropertyName("crib")]
        public bool Crib { get; set; }

        /// <summary>Descuento u oferta actual.</summary>
        [JsonPropertyName("offer")]
        public decimal? Offer { get; set; }

        /// <summary>Lista de extras (wifi, parking, minibar, etc.).</summary>
        [JsonPropertyName("extras")]
        public List<string> Extras { get; set; } = new();

        /// <summary>Rutas de imágenes adicionales.</summary>
        [JsonPropertyName("extraImages")]
        public List<string> ExtraImages { get; set; } = new();

        /// <summary>Indica si la habitación está disponible.</summary>
        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }

        /// <summary>Estado operativo actual (available/occupied/cleaning/maintenance/blocked).</summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = "available";

        [JsonIgnore]
        public string StatusLabel => Status switch
        {
            "available" => "Disponible",
            "occupied" => "Ocupada",
            "cleaning" => "Limpieza",
            "maintenance" => "Mantenimiento",
            "blocked" => "Bloqueada",
            _ => Status
        };

        /// <summary>Valoración media (0-5).</summary>
        [JsonPropertyName("rate")]
        public double Rate { get; set; }

        /// <summary>Media de reseñas calculada (se establece al cargar).</summary>
        [JsonIgnore]
        public double? AverageRating { get; set; }

        /// <summary>Texto formateado de la media de reseñas.</summary>
        [JsonIgnore]
        public string AverageRatingText => AverageRating.HasValue ? $"★ {AverageRating:F1}" : "";

        /// <summary>Indica si hay reseñas para mostrar la media.</summary>
        [JsonIgnore]
        public bool HasReviews => AverageRating.HasValue && AverageRating > 0;
    }

    /// <summary>
    /// Filtro para buscar habitaciones.
    /// </summary>
    public class RoomsFilter
    {
        /// <summary>Tipo de habitación.</summary>
        public string? Type { get; set; }

        /// <summary>Solo disponibles.</summary>
        public bool? IsAvailable { get; set; }

        /// <summary>Precio mínimo.</summary>
        public decimal? MinPrice { get; set; }

        /// <summary>Precio máximo.</summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>Capacidad mínima de huéspedes.</summary>
        public int? Guests { get; set; }

        /// <summary>Con cama extra.</summary>
        public bool? HasExtraBed { get; set; }

        /// <summary>Con cuna.</summary>
        public bool? HasCrib { get; set; }

        /// <summary>Con oferta activa.</summary>
        public bool? HasOffer { get; set; }

        /// <summary>Extras requeridos.</summary>
        public List<string>? Extras { get; set; }

        /// <summary>Número de habitación específico.</summary>
        public string? RoomNumber { get; set; }

        /// <summary>Campo de ordenamiento.</summary>
        public string? SortBy { get; set; }

        /// <summary>Dirección de ordenamiento (asc/desc).</summary>
        public string? SortOrder { get; set; }
    }

    /// <summary>
    /// Respuesta de la API al solicitar habitaciones.
    /// </summary>
    public class RoomsResponse
    {
        /// <summary>Lista de habitaciones.</summary>
        [JsonPropertyName("items")]
        public List<RoomModel> Items { get; set; } = new();

        /// <summary>Filtro aplicado por la API.</summary>
        [JsonPropertyName("appliedFilter")]
        public System.Text.Json.JsonElement AppliedFilter { get; set; }

        /// <summary>Ordenamiento aplicado.</summary>
        [JsonPropertyName("sort")]
        public Dictionary<string, int> Sort { get; set; } = new();
    }
}