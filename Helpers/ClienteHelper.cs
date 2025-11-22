using TheBuryProject.ViewModels;

namespace TheBuryProject.Helpers
{
    /// <summary>
    /// Contiene métodos auxiliares para operaciones comunes con clientes
    /// </summary>
    public static class ClienteHelper
    {
        /// <summary>
        /// Calcula la edad basada en la fecha de nacimiento
        /// </summary>
        public static int? CalcularEdad(DateTime? fechaNacimiento)
        {
            if (!fechaNacimiento.HasValue)
                return null;

            var hoy = DateTime.Today;
            var edad = hoy.Year - fechaNacimiento.Value.Year;

            // Restar 1 si el cumpleaños aún no ha ocurrido este año
            if (fechaNacimiento.Value.Date > hoy.AddYears(-edad))
                edad--;

            return edad;
        }

        /// <summary>
        /// Aplica el cálculo de edad a una colección de ViewModels
        /// </summary>
        public static void AplicarEdadAMultiples(IEnumerable<ClienteViewModel> viewModels)
        {
            foreach (var vm in viewModels)
            {
                vm.Edad = CalcularEdad(vm.FechaNacimiento);
            }
        }
    }
}