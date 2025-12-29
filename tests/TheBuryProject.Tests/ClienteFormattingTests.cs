using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using Xunit;

namespace TheBuryProject.Tests
{
    public class ClienteFormattingTests
    {
        [Fact]
        public void ToDisplayName_IncludesApellidoNombreAndDocumento()
        {
            var cliente = new Cliente
            {
                Apellido = "Perez",
                Nombre = "Juan",
                NumeroDocumento = "12345678"
            };

            var display = cliente.ToDisplayName();

            Assert.Contains("Perez", display);
            Assert.Contains("Juan", display);
            Assert.Contains("12345678", display);
        }

        [Fact]
        public void ToDisplayName_HandlesMissingDocumento()
        {
            var cliente = new Cliente
            {
                Apellido = "Lopez",
                Nombre = "Ana",
                NumeroDocumento = string.Empty
            };

            var display = cliente.ToDisplayName();

            Assert.Equal("Lopez, Ana - DNI: ", display);
        }
    }
}
