using ClosedXML.Excel;
using System.Globalization;

namespace GDB.App.Tests.TestData;

public static class ExcelTestData
{
    public const string WorkbookName = "GDB_Unit_Test_Cases.xlsx";
    public const string WorksheetName = "TestCases";

    public static readonly string[] RequiredColumns =
    [
        "serialNo",
        "TestCaseId",
        "Feature",
        "MethodUnderTest",
        "TestCaseType",
        "TestMethod",
        "AccountType",
        "InitialBalance",
        "Amount",
        "Limit",
        "Status",
        "StoredPin",
        "EnteredPin",
        "NewPin",
        "MinimumBalance",
        "OverdraftLimit",
        "InterestRate",
        "TenureMonths",
        "InactiveMonths",
        "EmployerName",
        "ExpectedBalance",
        "ExpectedResult",
        "ExpectedStatus",
        "ExpectedPin",
        "ExpectedConcreteType",
        "ExpectedException",
        "ExpectedMessage",
        "Iterations",
        "AccountNumber",
        "Name",
        "Age",
        "Privilege"
    ];

    private static readonly Lazy<IReadOnlyList<ExcelTestCase>> CachedCases =
        new(LoadWorkbook);

    public static IReadOnlyList<ExcelTestCase> All => CachedCases.Value;

    public static IEnumerable<object[]> LoadFor(string testMethod)
    {
        return All
            .Where(testCase => testCase.TestMethod == testMethod)
            .Select(testCase => new object[] { testCase });
    }

    private static IReadOnlyList<ExcelTestCase> LoadWorkbook()
    {
        string workbookPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            WorkbookName);

        if (!File.Exists(workbookPath))
        {
            throw new FileNotFoundException(
                $"Excel test data was not copied to '{workbookPath}'.",
                workbookPath);
        }

        using var workbook = new XLWorkbook(workbookPath);
        if (!workbook.TryGetWorksheet(WorksheetName, out IXLWorksheet? worksheet))
        {
            throw new InvalidDataException(
                $"Workbook '{WorkbookName}' does not contain worksheet '{WorksheetName}'.");
        }

        IXLRow? headerRow = worksheet.FirstRowUsed();
        if (headerRow is null)
        {
            throw new InvalidDataException("The test case worksheet is empty.");
        }

        var headers = headerRow.CellsUsed()
            .ToDictionary(
                cell => cell.GetString().Trim(),
                cell => cell.Address.ColumnNumber,
                StringComparer.OrdinalIgnoreCase);

        string[] missingColumns = RequiredColumns
            .Where(required => !headers.ContainsKey(required))
            .ToArray();

        if (missingColumns.Length > 0)
        {
            throw new InvalidDataException(
                $"Workbook is missing required columns: {string.Join(", ", missingColumns)}.");
        }

        var cases = new List<ExcelTestCase>();
        foreach (IXLRow row in worksheet.RowsUsed().Skip(1))
        {
            var values = headers.ToDictionary(
                header => header.Key,
                header => GetInvariantValue(row.Cell(header.Value)),
                StringComparer.OrdinalIgnoreCase);

            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            cases.Add(new ExcelTestCase(values));
        }

        return cases;
    }

    private static string GetInvariantValue(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        return cell.DataType switch
        {
            XLDataType.Number => cell.GetDouble().ToString("R", CultureInfo.InvariantCulture),
            XLDataType.Boolean => cell.GetBoolean().ToString(CultureInfo.InvariantCulture),
            _ => cell.GetString().Trim()
        };
    }
}
