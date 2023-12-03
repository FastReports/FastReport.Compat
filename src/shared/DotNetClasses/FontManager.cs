using System.Collections.Generic;
using System.Drawing.Text;

namespace System.Drawing
{
    public static class FontManager
    {
        /// <summary>
        /// Gets a PrivateFontCollection instance.
        /// </summary>
        /// <remarks>
        /// NOT THREAD SAFE!
        /// </remarks>
        public static PrivateFontCollection PrivateFontCollection { get; } = new PrivateFontCollection();

        /// <summary>
        /// Gets a temporary font collection instance.
        /// </summary>
        /// <remarks>
        /// NOT THREAD SAFE!
        /// Do not update PrivateFontCollection at realtime, you must update property value then dispose previous.
        /// </remarks>
        public static PrivateFontCollection TemporaryFontCollection { get; set; } = null;

        /// <summary>
        /// Gets installed font collection.
        /// </summary>
        public static InstalledFontCollection InstalledFontCollection { get; } = new InstalledFontCollection();

        public static List<FontSubstitute> SubstituteFonts = new List<FontSubstitute>();

        private static FontFamily FindFontFamilyInternal(string name)
        {
            FontCollection collection = TemporaryFontCollection;

            if (collection != null)
            {
                foreach (FontFamily font in collection.Families)
                {
                    if (name.Equals(font.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return font;
                    }
                }
            }

            // font family not found in temp list
            collection = PrivateFontCollection;
            foreach (FontFamily font in collection.Families)
            {
                if (name.Equals(font.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return font;
                }
            }

            // font family not found in private list
            collection = InstalledFontCollection;
            FontFamily[] privateFontList = collection.Families;
            foreach (FontFamily font in privateFontList)
            {
                if (name.Equals(font.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return font;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a FontFamily by its name.
        /// </summary>
        /// <param name="name">The family name, e.g. "Arial".</param>
        /// <returns>The FontFamily instance if found; otherwise default FontFamily.GenericSerif.</returns>
        public static FontFamily GetFontFamilyOrDefault(string name)
        {
            var fontFamily = FindFontFamilyInternal(name);

            if (fontFamily == null)
            {
                // try to substitute
                foreach (var item in SubstituteFonts)
                {
                    if (item.Name == name)
                    {
                        fontFamily = item.SubstituteFamily; 
                        break;
                    }
                }
            }

            if (fontFamily == null)
            {
                // use the default one
                fontFamily = FontFamily.GenericSansSerif;
            }

            // not null
            return fontFamily;
        }

        /// <summary>
        /// Defines a font substitute item.
        /// </summary>
        public class FontSubstitute
        {
            public string Name { get; }
            
            public string SubstituteList { get; }
            
            // null value indicates that no substitute found
            internal FontFamily SubstituteFamily { get; }

            /// <summary>
            /// Creates a new instance of font substitute item.
            /// </summary>
            /// <param name="name">The original font name, e.g. "Arial"</param>
            /// <param name="substituteList">The alternatives list, e.g. "Ubuntu Sans;Liberation Sans;Helvetica"</param>
            public FontSubstitute(string name, string substituteList)
            {
                Name = name;
                SubstituteList = substituteList;

                // try original name first
                SubstituteFamily = FindFontFamilyInternal(Name);
                
                // not found, try alternatives
                if (SubstituteFamily == null)
                {
                    var list = SubstituteList.Split(';');
                    foreach (var item in list)
                    {
                        SubstituteFamily = FindFontFamilyInternal(item);
                        if (SubstituteFamily != null)
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
