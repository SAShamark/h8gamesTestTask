namespace Services
{
    public static class NumberFormatter
    {
        private static readonly string[] Suffixes =
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "De",
            "UnDe", "DuDe", "TrDe", "QaDe", "QiDe", "SxDe", "SpDe", "OcDe", "NoDe"
        };

        private const double THOUSAND = 1000.0;
        private const int CUTOFF_FOR_SUFFIX = 10_000;

        public static string FormatNumber(double value)
        {
            return FormatNumber(value, null);
        }

        public static string FormatBalance(double value)
        {
            return FormatNumber(value, scaledValue => System.Math.Floor(scaledValue * 10.0) / 10.0);
        }

        public static string FormatCost(double value)
        {
            return FormatNumber(value, scaledValue => System.Math.Ceiling(scaledValue * 10.0) / 10.0);
        }

        private static string FormatNumber(double value, System.Func<double, double> round)
        {
            if (value < CUTOFF_FOR_SUFFIX)
            {
                return ((int)value).ToString();
            }

            int suffixIndex = 0;
            while (value >= THOUSAND && suffixIndex < Suffixes.Length - 1)
            {
                value /= THOUSAND;
                suffixIndex++;
            }

            if (round != null)
            {
                value = round(value);
            }

            return $"{value:0.#}{Suffixes[suffixIndex]}";
        }
    }
}
