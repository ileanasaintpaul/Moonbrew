namespace Moonbrew.Rendering
{
    public static class SharpText
    {
        // Rich-text tag that draws the text fully transparent while keeping its layout and colors.
        private const string Invisible = "<alpha=#00>";

        public static string Hide(string text)
        {
            return Invisible + (text ?? string.Empty);
        }

        public static bool IsHidden(string text)
        {
            return text != null && text.StartsWith(Invisible, System.StringComparison.Ordinal);
        }
    }
}
