using System;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;

static class TestRunnerProgram
{
    static int Main()
    {
        try
        {
            Console.WriteLine("Running quick functional checks...");

            // ClienteFormatting test
            var cliente = new Cliente { Apellido = "Perez", Nombre = "Juan", NumeroDocumento = "12345678" };
            var display = cliente.ToDisplayName();
            if (!display.Contains("Perez") || !display.Contains("Juan") || !display.Contains("12345678"))
                throw new Exception($"ToDisplayName failed: {display}");

            // ClienteHelper tests
            int? edadNull = ClienteHelper.CalcularEdad(null);
            if (edadNull != null) throw new Exception("CalcularEdad(null) should return null");

            var fecha = DateTime.Today.AddYears(-30);
            var edad = ClienteHelper.CalcularEdad(fecha);
            if (edad != 30) throw new Exception($"CalcularEdad expected 30 but was {edad}");

            var fecha2 = DateTime.Today.AddYears(-30).AddDays(1);
            var edad2 = ClienteHelper.CalcularEdad(fecha2);
            if (edad2 != 29) throw new Exception($"CalcularEdad expected 29 but was {edad2}");

            Console.WriteLine("All functional checks passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Functional checks failed: " + ex.Message);
            return 2;
        }
    }
}
