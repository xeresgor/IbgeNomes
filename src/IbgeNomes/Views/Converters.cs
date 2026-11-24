using System.Globalization;

namespace IbgeNomes.Views;

/// <summary>true → false e vice-versa (habilitar o seletor só quando não está carregando).</summary>
public class InverterBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : value;
}

/// <summary>true quando o valor é nulo (mostrar a dica "escolha um estado").</summary>
public class EhNuloConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
