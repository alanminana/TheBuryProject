namespace TheBuryProject.Models.Enums
{
    /// <summary>
    /// Tipos de documentos que puede presentar un cliente
    /// </summary>
    public enum TipoDocumentoCliente
    {
        DNI = 1,
        ReciboSueldo = 2,
        ServicioLuz = 3,
        ServicioGas = 4,
        ServicioAgua = 5,
        ConstanciaCUIL = 6,
        DeclaracionJurada = 7,
        Veraz = 8,
        Otro = 99
    }
}