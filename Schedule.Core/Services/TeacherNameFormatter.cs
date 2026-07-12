namespace Schedule.Core.Services;

public static class TeacherNameFormatter
{
    public static string ToNameSurname(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return string.Empty;
        }

        var parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

        return parts.Length >= 2
            ? $"{parts[1]} {parts[0]}"
            : parts[0];
    }
}
