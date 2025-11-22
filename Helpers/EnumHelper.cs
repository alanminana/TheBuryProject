using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TheBuryProject.Helpers
{
    /// <summary>
    /// Helper para obtener nombres legibles de valores enum con DisplayAttribute
    /// </summary>
    public static class EnumHelper
    {
        /// <summary>
        /// Obtiene el nombre mostrable de un valor enum desde su atributo [Display(Name="...")]
        /// </summary>
        public static string GetDisplayName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field == null)
                return value.ToString();

            var displayAttribute = field.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? value.ToString();
        }

        /// <summary>
        /// Obtiene la descripción de un valor enum desde su atributo [Display(Description="...")]
        /// </summary>
        public static string GetDisplayDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field == null)
                return string.Empty;

            var displayAttribute = field.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Description ?? string.Empty;
        }
    }
}