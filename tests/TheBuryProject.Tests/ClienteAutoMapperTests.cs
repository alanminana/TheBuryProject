using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.ViewModels;
using Xunit;

namespace TheBuryProject.Tests
{
    public class ClienteAutoMapperTests
    {
        private static IMapper CreateMapper()
        {
            // El proyecto configura AutoMapper con ILoggerFactory; usar NullLoggerFactory para tests.
            var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);

            // Nota: no usamos AssertConfigurationIsValid() porque el MappingProfile tiene muchos
            // mapas con propiedades destino "solo-UI" que se completan fuera de AutoMapper.

            return config.CreateMapper();
        }

        [Fact]
        public void Cliente_To_ClienteViewModel_IgnoresDeletedCreditsInAggregates()
        {
            var mapper = CreateMapper();

            var cliente = new Cliente
            {
                Nombre = "Juan",
                Apellido = "Perez",
                TipoDocumento = "DNI",
                NumeroDocumento = "123",
                Creditos =
                {
                    new Credito { Estado = EstadoCredito.Activo, SaldoPendiente = 100m, IsDeleted = false },
                    new Credito { Estado = EstadoCredito.Activo, SaldoPendiente = 999m, IsDeleted = true },
                    new Credito { Estado = EstadoCredito.Finalizado, SaldoPendiente = 50m, IsDeleted = false }
                }
            };

            var vm = mapper.Map<ClienteViewModel>(cliente);

            Assert.Equal(1, vm.CreditosActivos);
            Assert.Equal(100m, vm.MontoAdeudado);
        }
    }
}
