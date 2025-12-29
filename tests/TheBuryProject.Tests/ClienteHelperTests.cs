using System;
using TheBuryProject.Helpers;
using Xunit;

namespace TheBuryProject.Tests
{
    public class ClienteHelperTests
    {
        [Fact]
        public void CalcularEdad_ReturnsNull_WhenFechaNacimientoNull()
        {
            int? edad = ClienteHelper.CalcularEdad(null);
            Assert.Null(edad);
        }

        [Fact]
        public void CalcularEdad_CalculatesCorrectAge()
        {
            var fecha = DateTime.Today.AddYears(-30);
            var edad = ClienteHelper.CalcularEdad(fecha);
            Assert.Equal(30, edad);
        }

        [Fact]
        public void CalcularEdad_SubtractsOneIfBirthdayNotReached()
        {
            var fecha = DateTime.Today.AddYears(-30).AddDays(1); // birthday tomorrow -> still 29
            var edad = ClienteHelper.CalcularEdad(fecha);
            Assert.Equal(29, edad);
        }
    }
}
