// Мелкие помощники для работы со строками, чтобы не копировать один и тот же код в контроллеры
public static class Strings
{
    // null и пробелы по краям превращаем в аккуратную строку: "  Алиса " -> "Алиса", null -> ""
    public static string Clean(string? text)
    {
        return (text ?? "").Trim();
    }
}
