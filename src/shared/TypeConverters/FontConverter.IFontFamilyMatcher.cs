using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Drawing.Text;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Drawing;
using System.Linq;

namespace FastReport.TypeConverters
{
    public partial class FontConverter : TypeConverter
    {
        public static IFontFamilyMatcher FontFamilyMatcher { get; set; } = new DefaultFontFamilyMatcher();

        public interface IFontFamilyMatcher
        {
            public FontFamily GetFontFamilyOrDefault(string name);
        }

        public class DefaultFontFamilyMatcher : IFontFamilyMatcher
        {
            public FontFamily GetFontFamilyOrDefault(string name)
            {
                var fontFamily = FontFamily.Families.Where(f => f.Name == name).FirstOrDefault();
                return fontFamily ?? FontFamily.GenericSansSerif;
            }
        }
    }
}
