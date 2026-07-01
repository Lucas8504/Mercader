using Mercader.Services;

namespace Mercader.Tests;

public class ValidationServiceTests
{
    // ======================================================================
    // ValidateRequired
    // ======================================================================

    [Fact]
    public void ValidateRequired_NullValue_ReturnsFail()
    {
        var result = ValidationService.ValidateRequired(null, "nombre");

        Assert.False(result.IsValid);
        Assert.Equal("nombre", result.FieldName);
    }

    [Fact]
    public void ValidateRequired_EmptyValue_ReturnsFail()
    {
        var result = ValidationService.ValidateRequired("", "descripción");

        Assert.False(result.IsValid);
        Assert.Equal("descripción", result.FieldName);
    }

    [Fact]
    public void ValidateRequired_WhitespaceValue_ReturnsFail()
    {
        var result = ValidationService.ValidateRequired("   ", "test");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateRequired_ValidValue_ReturnsSuccess()
    {
        var result = ValidationService.ValidateRequired("producto", "nombre");

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    // ======================================================================
    // ValidateDecimal
    // ======================================================================

    [Fact]
    public void ValidateDecimal_NullValue_ReturnsFail()
    {
        var result = ValidationService.ValidateDecimal(null, "precio");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateDecimal_EmptyValue_ReturnsFail()
    {
        var result = ValidationService.ValidateDecimal("", "monto");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateDecimal_InvalidText_ReturnsFail()
    {
        var result = ValidationService.ValidateDecimal("abc", "precio");

        Assert.False(result.IsValid);
        Assert.Contains("numérico", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDecimal_ValidIntegerString_ReturnsSuccess()
    {
        var result = ValidationService.ValidateDecimal("123", "precio");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateDecimal_ValidDecimalString_ReturnsSuccess()
    {
        var result = ValidationService.ValidateDecimal("123.45", "precio");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateDecimal_NegativeDecimalString_ReturnsSuccess()
    {
        // ValidateDecimal solo verifica que sea un número válido, no el signo
        var result = ValidationService.ValidateDecimal("-50", "descuento");

        Assert.True(result.IsValid);
    }

    // ======================================================================
    // ParseDecimal
    // ======================================================================

    [Fact]
    public void ParseDecimal_NullValue_ReturnsFailure()
    {
        var (success, value, error) = ValidationService.ParseDecimal(null);

        Assert.False(success);
        Assert.Null(value);
        Assert.NotNull(error);
    }

    [Fact]
    public void ParseDecimal_EmptyValue_ReturnsFailure()
    {
        var (success, value, error) = ValidationService.ParseDecimal("");

        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void ParseDecimal_InvalidValue_ReturnsFailure()
    {
        var (success, value, error) = ValidationService.ParseDecimal("not-a-number");

        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void ParseDecimal_NegativeValue_ReturnsFailure()
    {
        var (success, value, error) = ValidationService.ParseDecimal("-1");

        Assert.False(success);
        Assert.Null(value);
        Assert.Contains("negativo", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseDecimal_ValidPositiveValue_ReturnsSuccess()
    {
        var (success, value, error) = ValidationService.ParseDecimal("42.50");

        Assert.True(success);
        Assert.Equal(42.50m, value);
        Assert.Null(error);
    }

    [Fact]
    public void ParseDecimal_ZeroValue_ReturnsSuccess()
    {
        // ParseDecimal rechaza negativo (< 0), pero cero pasa
        var (success, value, error) = ValidationService.ParseDecimal("0");

        Assert.True(success);
        Assert.Equal(0m, value);
        Assert.Null(error);
    }

    // ======================================================================
    // ValidatePositive
    // ======================================================================

    [Fact]
    public void ValidatePositive_ZeroValue_ReturnsFail()
    {
        var result = ValidationService.ValidatePositive(0, "precio");

        Assert.False(result.IsValid);
        Assert.Contains("mayor a 0", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidatePositive_NegativeValue_ReturnsFail()
    {
        var result = ValidationService.ValidatePositive(-10, "monto");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidatePositive_PositiveValue_ReturnsSuccess()
    {
        var result = ValidationService.ValidatePositive(1, "precio");

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidatePositive_LargePositiveValue_ReturnsSuccess()
    {
        var result = ValidationService.ValidatePositive(999999.99m, "precio");

        Assert.True(result.IsValid);
    }

    // ======================================================================
    // ValidationResult factory methods
    // ======================================================================

    [Fact]
    public void ValidationResult_Success_HasCorrectDefaults()
    {
        var result = ValidationService.ValidationResult.Success();

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.FieldName);
    }

    [Fact]
    public void ValidationResult_Fail_HasCorrectValues()
    {
        var result = ValidationService.ValidationResult.Fail("Campo requerido", "email");

        Assert.False(result.IsValid);
        Assert.Equal("Campo requerido", result.ErrorMessage);
        Assert.Equal("email", result.FieldName);
    }
}
