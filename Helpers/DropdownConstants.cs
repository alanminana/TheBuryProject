namespace TheBuryProject.Helpers
{
    /// <summary>
    /// Contiene constantes para listas desplegables reutilizables en toda la aplicación
    /// </summary>
    public static class DropdownConstants
    {
        public static readonly string[] TiposDocumento = { "DNI", "CUIL", "CUIT" };

        public static readonly string[] EstadosCiviles = 
        {
            "Soltero/a", "Casado/a", "Divorciado/a", "Viudo/a", "Unión de hecho"
        };

        public static readonly string[] TiposEmpleo = 
        {
            "Relación de dependencia", "Autónomo", "Monotributista", "Informal"
        };

        public static readonly string[] Provincias = 
        {   
            "Buenos Aires", "CABA", "Catamarca", "Chaco", "Chubut", "Córdoba",
            "Corrientes", "Entre Ríos", "Formosa", "Jujuy", "La Pampa", "La Rioja",
            "Mendoza", "Misiones", "Neuquén", "Río Negro", "Salta", "San Juan",
            "San Luis", "Santa Cruz", "Santa Fe", "Santiago del Estero",
            "Tierra del Fuego", "Tucumán"
        };

        public static readonly string[] DocumentosRequeridos = 
        {
            "DNI", "Recibo de Sueldo", "Servicio (Luz/Gas/Agua)", "Veraz"
        };
    }
}