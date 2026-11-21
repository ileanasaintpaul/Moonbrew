using System.Text.RegularExpressions;

namespace Moonbrew.Player
{
    // The body part a character model dresses, read from its name: "SK_FANT_VILL_02_10TORS_HU01" dresses "TORS".
    // A part's model replaces the base model of the same body part.
    public static class BodyPartSlot
    {
        private static readonly Regex Code = new Regex(@"_\d{2}([A-Z]{4})_");

        // Null when the name has no body part code.
        public static string Of(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                return null;
            }

            var match = Code.Match(modelName);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
