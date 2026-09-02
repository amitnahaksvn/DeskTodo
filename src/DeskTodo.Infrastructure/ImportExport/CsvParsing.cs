using System.Text;

namespace DeskTodo.Infrastructure.ImportExport;

/// <summary>
/// The RFC 4180 CSV tokenizer shared by <see cref="TaskImportService"/> (fixed-column import)
/// and <see cref="MassImportService"/> (Feature 89/90's arbitrary-column mapping) — extracted
/// out of <see cref="TaskImportService"/> so both can parse the same way without one importer
/// depending on the other.
/// </summary>
public static class CsvParsing
{
    /// <summary>Parses quoted fields, doubled-quote escaping, and commas/newlines inside quotes — a naive <c>Split(',')</c> would silently corrupt any field that itself contains a comma or newline.</summary>
    public static List<List<string>> Parse(string text)
    {
        var records = new List<List<string>>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i += 2;
                        continue;
                    }

                    inQuotes = false;
                    i++;
                    continue;
                }

                field.Append(c);
                i++;
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    i++;
                    break;
                case ',':
                    fields.Add(field.ToString());
                    field.Clear();
                    i++;
                    break;
                case '\r':
                    i++;
                    break;
                case '\n':
                    fields.Add(field.ToString());
                    field.Clear();
                    records.Add(fields);
                    fields = [];
                    i++;
                    break;
                default:
                    field.Append(c);
                    i++;
                    break;
            }
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            records.Add(fields);
        }

        return records;
    }
}
