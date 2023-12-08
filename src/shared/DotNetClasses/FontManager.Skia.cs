using SkiaSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using Topten.RichTextKit;

namespace FastReport
{
    public static partial class FontManager
    {
        // do not remove
        private static readonly CharacterMatcher characterMatcher = new();
        private static readonly FontFamilyMatcher fontFamilyMatcher = new();

        /// <summary>
        /// Gets a list of fallback fonts.
        /// </summary>
        /// <remarks>
        /// Fallback font is used to display characters that not present in the original font. It is often used for CJK.
        /// For example, you may add "Noto Sans CJK JP" fallback. User defined fallback fonts will then be checked first.
        /// </remarks>
        public static List<FontFallback> FallbackFonts { get; } = new();

        private static FontFamily FindFontFamilyInternal(string name)
        {
            FontFamily fontFamily = null;

            if (TemporaryFontCollection != null)
            {
                fontFamily = TemporaryFontCollection.FindInternalByGDIFontFamilyName(name);
            }

            // font family not found in temp list
            if (fontFamily == null)
            {
                fontFamily = PrivateFontCollection.FindInternalByGDIFontFamilyName(name);

                // font family not found in private list
                if (fontFamily == null)
                {
                    fontFamily = InstalledFontCollection.FindInternalByGDIFontFamilyName(name);
                }
            }

            // may be null
            return fontFamily;
        }

        /// <summary>
        /// Defines a font fallback item.
        /// </summary>
        public class FontFallback
        {
            public string Name { get; }
            public SKTypeface SKTypeface { get; }

            /// <summary>
            /// Creates a new instance of font fallback item.
            /// </summary>
            /// <param name="name">The font name, e.g. "Noto Sans CJK JP"</param>
            public FontFallback(string name)
            {
                Name = name;
                SKTypeface = SKTypeface.FromFamilyName(name);
            }
        }

        private class CharacterMatcher : ICharacterMatcher
        {
            private readonly Topten.RichTextKit.ICharacterMatcher oldCharacterMatcher;

            public CharacterMatcher()
            {
                oldCharacterMatcher = Topten.RichTextKit.FontFallback.CharacterMatcher;
                Topten.RichTextKit.FontFallback.CharacterMatcher = this;
            }

            public SKTypeface MatchCharacter(string familyName, int weight, int width, SKFontStyleSlant slant, string[] bcp47, int character)
            {
                // check user defined fallbacks
                foreach (var ff in FallbackFonts)
                {
                    var tf = ff.SKTypeface;
                    if (tf != null)
                    {
                        if (tf.ContainsGlyph(character))
                        {
                            return tf;
                        }
                    }
                }

                return oldCharacterMatcher?.MatchCharacter(familyName, weight, width, slant, bcp47, character);
            }
        }

        private class FontFamilyMatcher : FontFamily.IFontFamilyMatcher
        {
            public FontFamilyMatcher()
            {
                FontFamily.FontFamilyMatcher = this;
            }

            public FontFamily GetFontFamilyOrDefault(string name) => FontManager.GetFontFamilyOrDefault(name);
        }
    }
}
