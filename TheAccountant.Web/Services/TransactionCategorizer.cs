namespace TheAccountant.Web.Services
{
    public static class TransactionCategorizer
    {
        public static string Categorize(string? description)
        {
            var text = Normalize(description);

            if (ContainsAny(
                text,
                "ALBERTSONS",
                "WINCO",
                "COSTCO",
                "FRED-MEYER",
                "FRED MEYER",
                "M & W MARKETS",
                "OLD FASHIONED FRUIT"))
            {
                return "Groceries";
            }

            if (ContainsAny(
                text,
                "DOORDASH",
                "JERSEY MIKES",
                "POPEYES",
                "JAMBA",
                "AUNTIE ANNES",
                "PERCY",
                "REED'S DAIRY"))
            {
                return "Dining";
            }

            if (ContainsAny(
                text,
                "CHEVRON",
                "STINKER",
                "JACKSONS FOOD"))
            {
                return "Fuel";
            }

            if (ContainsAny(
                text,
                "NETFLIX",
                "DISNEY PLUS",
                "PARAMOUNT+",
                "PEACOCK",
                "PLAYSTATION",
                "BLIZZARD",
                "AUDIBLE",
                "UBER *ONE",
                "EPIX"))
            {
                return "Subscriptions";
            }

            if (ContainsAny(
                text,
                "SPARKLIGHT",
                "ATT*BILL",
                "ATT*PAYMENT",
                "MICROSOFT"))
            {
                return "Utilities";
            }

            if (ContainsAny(
                text,
                "FARM BUREAU"))
            {
                return "Insurance";
            }

            if (ContainsAny(
                text,
                "ST LUKES",
                "TRINITY HEALTH",
                "WALGREENS"))
            {
                return "Healthcare";
            }

            if (ContainsAny(
                text,
                "AMAZON",
                "WAL-MART",
                "WALMART",
                "DOLLAR TREE",
                "AUTOZONE"))
            {
                return "Shopping";
            }

            if (ContainsAny(
                text,
                "AGODA",
                "HOTEL",
                "AIRBNB"))
            {
                return "Travel";
            }

            if (ContainsAny(
                text,
                "GREAT CLIPS",
                "BROW 4 U"))
            {
                return "Personal Care";
            }

            return "Uncategorized";
        }

        private static bool ContainsAny(
            string value,
            params string[] terms)
        {
            return terms.Any(value.Contains);
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return string.Join(
                    " ",
                    value.Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries))
                .ToUpperInvariant();
        }
    }
}