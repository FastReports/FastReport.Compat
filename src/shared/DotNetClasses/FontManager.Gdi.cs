using System;
using System.Drawing;
using System.Drawing.Text;

namespace FastReport
{
    public static partial class FontManager
    {
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
    }
}
