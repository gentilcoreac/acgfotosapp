using System;
using System.Drawing;
using Wacton.Unicolour;

namespace AcgFotos.Core.Theme
{
    public class ColorConverter
    {
        public static HSLColorModel ConvertHexToHSL(string hexColor)
        {
            // Convertir el color HEX a RGB
            var unicolour = new Unicolour(hexColor);

            // Convertir el color RGB a HSL
            var hsl = unicolour.Hsl;

            // Obtener los componentes H, S, L del color hsl
            double h = hsl.H;
            double s = hsl.S * 100;
            double l = hsl.L * 100;

            // Construir la cadena de texto en formato HSL
            string hslColor = $"hsl(" +
                $"{Math.Round(h, 2).ToString().Replace(",", ".")}deg, " +
                $"{Math.Round(s, 2).ToString().Replace(",", ".")}%, " +
                $"{Math.Round(l, 2).ToString().Replace(",", ".")}%)";

            var hslColorModel = new HSLColorModel
            {
                Hsl = hslColor,
                HslHue = h,
                HslSaturation = s,
                HslLight = l,
            };
            return hslColorModel;
        }

        //HEX a RGB
        public static string ConvertHexToRGBValues(string hexColor)
        {
            // Remove the hash if it exists
            if (hexColor[0] == '#')
                hexColor = hexColor.Substring(1);

            // Convert the hexadecimal color to an actual Color object
            Color color = ColorTranslator.FromHtml($"#{hexColor}");

            // Extract the individual RGB components
            double r = color.R;
            double g = color.G;
            double b = color.B;

            // Construir la cadena de texto en formato RGB
            string rgbColor = $"{Math.Round(r, 2).ToString().Replace(",", ".")}, {Math.Round(g, 2).ToString().Replace(",", ".")}, {Math.Round(b, 2).ToString().Replace(",", ".")}";

            return rgbColor;
        }
    }
}
