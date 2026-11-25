using Microsoft.Data.SqlClient;

namespace EfCoreCompatibility.Api.Services;

public sealed record DatabaseExceptionInfo(
    string ExceptionType,
    string Message,
    int? SqlErrorNumber,
    string? SqlExceptionType);

public static class DatabaseExceptionInspector
{
    public static DatabaseExceptionInfo Describe(Exception exception)
    {
        var sqlException = FindSqlException(exception);
        var message = sqlException?.Message ?? exception.Message;
        return new DatabaseExceptionInfo(
            exception.GetType().FullName ?? exception.GetType().Name,
            Sanitize(message),
            sqlException?.Number,
            sqlException?.GetType().FullName);
    }

    public static SqlException? FindSqlException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException)
            {
                return sqlException;
            }
        }

        return null;
    }

    private static string Sanitize(string message)
    {
        if (message.Contains("Password=", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Pwd=", StringComparison.OrdinalIgnoreCase))
        {
            return "The provider returned an error. The message was suppressed because it contained credential material.";
        }

        return message;
    }
}
