using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Xml.Linq;

namespace ospConverter.wpfFunc
{
    public class HeightToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var height = (double)value;
            CornerRadius radius = new CornerRadius(height / 2);
            return radius;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PathToFilenameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object result = null;

            if (value != null)
            {
                var content = value.ToString();

                if (string.IsNullOrWhiteSpace(content) == false)
                {
                    if (content.Substring(0, 1) == "<") //XElement eines Metalinks wurde eingefügt
                    {
                        XElement metalink = XElement.Parse(content) as XElement;
                        result = metalink.Attribute("name").Value;
                    }
                    else //Datei wurde eingefügt
                    {
                        result = Path.GetFileName(content);
                    }
                }
            }
            return result;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class PathToFilesizeConverter : IValueConverter
    {
        static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            object result = null;
            decimal fileSize;
            
            if (value != null)
            {
                var content = value.ToString();

                if (string.IsNullOrWhiteSpace(content) == false)
                {
                    if (content.Substring(0, 1) == "<") //XElement eines Metalinks wurde eingefügt
                    {
                        XElement metalink = XElement.Parse(content) as XElement;
                        XNamespace ad = "urn:ietf:params:xml:ns:metalink";

                        fileSize = decimal.Parse(metalink.Element(ad+"size").Value);
                    }
                    else //Datei wurde eingefügt
                    {

                        fileSize = new FileInfo(content).Length;
                    }



                    
                    int i = 0;
                    while (Math.Round(fileSize / 1024) >= 1)
                    {
                        fileSize /= 1024;
                        i++;
                    }
                    return string.Format("{0:n1} {1}", fileSize, SizeSuffixes[i]);
                }


            }

            return result;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
