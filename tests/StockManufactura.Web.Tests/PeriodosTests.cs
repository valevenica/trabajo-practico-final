using StockManufactura.Web.Infrastructure;
using Xunit;

namespace StockManufactura.Web.Tests;

public class PeriodosTests
{
    [Fact]
    public void UltimosDoceMeses_EmpiezaConTodoYSigueConElMesActual()
    {
        var periodos = Periodos.UltimosDoceMeses("Todos los períodos");

        Assert.Equal(13, periodos.Count);
        Assert.Equal(new Periodo("todo", null, null, "Todos los períodos"), periodos[0]);
        Assert.Equal(Periodos.MesActual, periodos[1].Clave);
        Assert.Equal(DateTime.UtcNow.Year, periodos[1].Year);
        Assert.Equal(DateTime.UtcNow.Month, periodos[1].Month);
    }

    [Fact]
    public void UltimosDoceMeses_VaDelMasRecienteAlMasAntiguoSinRepetir()
    {
        var meses = Periodos.UltimosDoceMeses("Todo").Skip(1).ToList();

        for (var i = 1; i < meses.Count; i++)
        {
            var anterior = new DateTime(meses[i - 1].Year!.Value, meses[i - 1].Month!.Value, 1);
            var actual = new DateTime(meses[i].Year!.Value, meses[i].Month!.Value, 1);
            Assert.Equal(anterior.AddMonths(-1), actual);
        }

        Assert.Equal(meses.Count, meses.Select(x => x.Clave).Distinct().Count());
    }

    [Fact]
    public void UltimosDoceMeses_EtiquetaEnCastellanoConMayusculaInicial()
    {
        var mes = Periodos.UltimosDoceMeses("Todo")[1];
        var esperado = new DateTime(mes.Year!.Value, mes.Month!.Value, 1)
            .ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-AR"));

        Assert.Equal(char.ToUpperInvariant(esperado[0]) + esperado[1..], mes.Etiqueta);
        Assert.True(char.IsUpper(mes.Etiqueta[0]));
    }
}
