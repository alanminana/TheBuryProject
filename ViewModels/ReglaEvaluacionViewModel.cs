namespace TheBuryProject.ViewModels
{
    public class ReglaEvaluacionViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public bool Cumple { get; set; }
    public string? Detalle { get; set; }
    public int Peso { get; set; } // Puntos que suma/resta
}
}