using System.Globalization;

namespace GDB.App.Tests.TestData;

public sealed class ExcelTestCase
{
    private readonly IReadOnlyDictionary<string, string> _values;

    public ExcelTestCase(IReadOnlyDictionary<string, string> values)
    {
        _values = values;
    }

    public string TestCaseId => GetRequiredString("TestCaseId");
    public string TestMethod => GetRequiredString("TestMethod");
    public IReadOnlyCollection<string> Columns => _values.Keys.ToArray();

    public string GetRequiredString(string column)
    {
        string? value = GetOptionalString(column);
        if (value is null)
        {
            throw new InvalidDataException(
                $"Test case '{GetIdentifier()}' requires a value in '{column}'.");
        }

        return value;
    }

    public string? GetOptionalString(string column)
    {
        if (!_values.TryGetValue(column, out string? value))
        {
            throw new InvalidDataException(
                $"Workbook column '{column}' was not found for test case '{GetIdentifier()}'.");
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public decimal GetDecimal(string column)
    {
        string value = GetRequiredString(column);
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        throw InvalidValue(column, value, "decimal");
    }

    public decimal? GetOptionalDecimal(string column)
    {
        string? value = GetOptionalString(column);
        if (value is null)
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        throw InvalidValue(column, value, "decimal");
    }

    public double? GetOptionalDouble(string column)
    {
        string? value = GetOptionalString(column);
        if (value is null)
        {
            return null;
        }

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
        {
            return result;
        }

        throw InvalidValue(column, value, "number");
    }

    public int GetInt32(string column)
    {
        string value = GetRequiredString(column);
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        throw InvalidValue(column, value, "integer");
    }

    public int? GetOptionalInt32(string column)
    {
        string? value = GetOptionalString(column);
        if (value is null)
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        throw InvalidValue(column, value, "integer");
    }

    public bool GetBoolean(string column)
    {
        string value = GetRequiredString(column);
        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        throw InvalidValue(column, value, "boolean");
    }

    public TEnum GetEnum<TEnum>(string column) where TEnum : struct, Enum
    {
        string value = GetRequiredString(column);
        if (Enum.TryParse(value, true, out TEnum result))
        {
            return result;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericValue))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
        }

        throw InvalidValue(column, value, typeof(TEnum).Name);
    }

    public TEnum? GetOptionalEnum<TEnum>(string column) where TEnum : struct, Enum
    {
        string? value = GetOptionalString(column);
        if (value is null)
        {
            return null;
        }

        if (Enum.TryParse(value, true, out TEnum result))
        {
            return result;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericValue))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), numericValue);
        }

        throw InvalidValue(column, value, typeof(TEnum).Name);
    }

    public override string ToString() => TestCaseId;

    private string GetIdentifier()
    {
        return _values.TryGetValue("TestCaseId", out string? id) && !string.IsNullOrWhiteSpace(id)
            ? id
            : "unknown";
    }

    private InvalidDataException InvalidValue(string column, string value, string expectedType)
    {
        return new InvalidDataException(
            $"Test case '{GetIdentifier()}' has invalid {expectedType} value '{value}' in '{column}'.");
    }
}
