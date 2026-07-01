using System.Globalization;

namespace Mercader.Services
{
    /// <summary>
    /// Servicios de validación para formularios.
    /// </summary>
    public static class ValidationService
    {
        /// <summary>
        /// Resultado de validación.
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string? ErrorMessage { get; set; }
            public string? FieldName { get; set; }

            public static ValidationResult Success() => new() { IsValid = true };
            public static ValidationResult Fail(string message, string field) => new()
            {
                IsValid = false,
                ErrorMessage = message,
                FieldName = field
            };
        }

        /// <summary>
        /// Valida que un string no esté vacío.
        /// </summary>
        public static ValidationResult ValidateRequired(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Fail($"Por favor, ingrese {fieldName}", fieldName);

            return ValidationResult.Success();
        }

        /// <summary>
        /// Valida que un string sea un número decimal válido.
        /// </summary>
        public static ValidationResult ValidateDecimal(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ValidationResult.Fail($"Por favor, ingrese {fieldName}", fieldName);

            if (!decimal.TryParse(value, CultureInfo.InvariantCulture, out _))
                return ValidationResult.Fail($"Por favor, ingrese un valor numérico válido para {fieldName}", fieldName);

            return ValidationResult.Success();
        }

        /// <summary>
        /// Valida y convierte a decimal.
        /// </summary>
        public static (bool Success, decimal? Value, string? Error) ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (false, null, "El valor no puede estar vacío");

            if (!decimal.TryParse(value, CultureInfo.InvariantCulture, out var result))
                return (false, null, "Valor numérico inválido");

            if (result < 0)
                return (false, null, "El valor no puede ser negativo");

            return (true, result, null);
        }

        /// <summary>
        /// Valida que el valor sea > 0.
        /// </summary>
        public static ValidationResult ValidatePositive(decimal value, string fieldName)
        {
            if (value <= 0)
                return ValidationResult.Fail($"{fieldName} debe ser mayor a 0", fieldName);

            return ValidationResult.Success();
        }
    }
}
