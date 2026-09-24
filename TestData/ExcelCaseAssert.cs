namespace GDB.App.Tests.TestData;

public static class ExcelCaseAssert
{
    public static void ThrowsExpected(ExcelTestCase testCase, Action action)
    {
        string expectedType = testCase.GetRequiredString("ExpectedException");
        Exception? actual = null;

        try
        {
            action();
        }
        catch (Exception exception)
        {
            actual = exception;
        }

        Assert.IsNotNull(actual, $"{testCase.TestCaseId} expected {expectedType}.");
        Assert.AreEqual(expectedType, actual.GetType().Name, testCase.TestCaseId);

        string? expectedMessage = testCase.GetOptionalString("ExpectedMessage");
        if (expectedMessage is not null)
        {
            Assert.AreEqual(expectedMessage, actual.Message, testCase.TestCaseId);
        }
    }
}
