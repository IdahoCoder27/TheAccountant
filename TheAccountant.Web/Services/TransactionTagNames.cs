using TheAccountant.Web.Models;

namespace TheAccountant.Web.Services
{
    public static class TransactionTagNames
    {
        public static string NormalizeWhitespace(string value)
        {
            return string.Join(
                " ",
                value.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries));
        }

        public static string Normalize(string value)
        {
            return NormalizeWhitespace(value)
                .ToUpperInvariant();
        }

        public static bool TryParse(
            string? text,
            out List<string> names,
            out string? error)
        {
            names = new();
            error = null;

            if (text?.Length > 1000)
            {
                error = "Tags must be 1,000 characters or fewer.";
                return false;
            }

            names = (text ?? string.Empty)
                .Split(',')
                .Select(NormalizeWhitespace)
                .Where(name => name.Length > 0)
                .DistinctBy(Normalize)
                .ToList();

            if (names.Count > 10 ||
                names.Any(name =>
                    name.Length > 80 ||
                    Normalize(name).Length > 80))
            {
                error =
                    "Use up to 10 tags, each 80 characters or fewer.";
            }

            return error is null;
        }

        public static void Apply(
            Transaction transaction,
            IReadOnlyList<string> names)
        {
            var desired = names.ToDictionary(
                Normalize,
                name => name);

            transaction.Tags.RemoveAll(
                tag => !desired.ContainsKey(tag.NormalizedName));

            foreach (var pair in desired)
            {
                var existingTag = transaction.Tags
                    .FirstOrDefault(tag =>
                        tag.NormalizedName == pair.Key);

                if (existingTag is null)
                {
                    transaction.Tags.Add(new TransactionTag
                    {
                        Name = pair.Value,
                        NormalizedName = pair.Key
                    });
                }
                else
                {
                    existingTag.Name = pair.Value;
                }
            }
        }
    }
}