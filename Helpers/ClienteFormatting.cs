using TheBuryProject.Models.Entities;

namespace TheBuryProject.Helpers
{
    public static class ClienteFormatting
    {
        public static string ToDisplayName(this Cliente cliente)
        {
            return $"{cliente.Apellido}, {cliente.Nombre} - DNI: {cliente.NumeroDocumento}";
        }
    }
}