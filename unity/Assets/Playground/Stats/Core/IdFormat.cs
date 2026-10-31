namespace BaseGame.Playground.Stats
{
    /// <summary>The one ID rule for code and assets. ASCII only, because IDs are save keys and compared ordinally.</summary>
    internal static class IdFormat
    {
        public const string Rule = "must be PascalCase: start with an uppercase letter, then only ASCII letters and digits";

        public static bool IsValid(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] < 'A' || value[0] > 'Z')
            {
                return false;
            }

            foreach (char c in value)
            {
                if (!(c >= 'A' && c <= 'Z') && !(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
