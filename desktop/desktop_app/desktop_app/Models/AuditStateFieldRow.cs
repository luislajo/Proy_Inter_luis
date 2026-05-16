namespace desktop_app.Models
{
    /// <summary>Fila campo-valor para la vista de comparación de auditoría.</summary>
    public class AuditStateFieldRow
    {
        public string FieldKey { get; set; } = "";
        public string FieldLabel { get; set; } = "";
        public string PreviousDisplay { get; set; } = "";
        public string NewDisplay { get; set; } = "";
        public bool IsChanged { get; set; }
    }
}
