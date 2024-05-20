//
// C# 
// InfoCellTowers
// v 0.1, 20.05.2024
// https://github.com/dkxce
// en,ru,1251,utf-8
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace InfoCellTowers
{
    internal class Program
    {
        public static string version = "20.05.2024";

        // All is very simple
        // Plugin Must return fileName in last line (or in single line)
        // if last line (or single) is empty - file is not exists
        static void Main()
        {
            ClearCache();

            string outFile = null;
            string geo = "55.600000, 37.500000";
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("InfoCellTowers Grabber by milokz@gmail.com");
            Console.WriteLine("** version " + version + " **");
            
            if (InputBox.Show("InfoCellTowers", "Введите координаты (широта, долгота):", ref geo) == DialogResult.OK)
            {
                PointD pd = LatLonParser.Parse(geo = geo.Trim());
                byte[] data = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new { lat = pd.Y, lng = pd.X }));

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create("https://infocelltowers.ru/ymaps");
                wr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0";
                wr.ContentType = "application/json";                
                wr.Referer = "https://infocelltowers.ru/";
                wr.Method = "POST";                
                wr.ContentLength = data.Length;
                try
                {
                    Console.WriteLine($"Get data from infocelltowers.ru {pd.Y}, {pd.X}...");
                    using (Stream stream = wr.GetRequestStream()) stream.Write(data, 0, data.Length);
                    HttpWebResponse response = (HttpWebResponse)wr.GetResponse();
                    string respJson = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                    Console.WriteLine($" ... {respJson.Length} symbols received");

                    FeatureCollection fc = JsonConvert.DeserializeObject<FeatureCollection>(respJson);
                    if (fc != null && fc.type == "FeatureCollection")
                    {
                        Console.WriteLine($" ... {fc.features?.Length} stations received");
                        outFile = SaveKML(fc, $"InfoCellTowers ({geo})");
                    }
                    else Console.WriteLine($" ... wrong response!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    MessageBox.Show(ex.Message, "InfoCellTowers", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
            };
            Console.WriteLine("Done");
            if (outFile != null)
            {
                Console.WriteLine("Data saved to file: ");
                Console.WriteLine(outFile);
            };
            System.Threading.Thread.Sleep(500);
        }

        private static void ClearCache()
        {
            try
            {
                foreach (string f in Directory.GetFiles(System.AppDomain.CurrentDomain.BaseDirectory, "InfoCellTowers_*.kml"))
                    File.Delete(f);
            }
            catch { };
        }

        private class FeatureCollection
        {
            public class Feature
            {
                public class Geometry
                {
                    public string type;
                    public double[] coordinates;
                }

                public class Properties
                {
                    public string hintContent;
                    public string balloonContentHeader;
                    public string balloonContentBody;
                    public string balloonContentFooter;
                    public string iconContent;
                }

                public long id;
                public string type;
                public Geometry geometry;
                public Properties properties;
            }

            public string type;
            public Feature[] features;

            public static string StripHTML(string input)
            {
                return Regex.Replace(input, "<.*?>", String.Empty);
            }
        }

        private static string SaveKML(Program.FeatureCollection fc, string layerName)
        {
            if ((fc == null) || (fc.features == null) || (fc.features.Length == 0)) return "";
            string dt = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            string fileName = System.AppDomain.CurrentDomain.BaseDirectory + $"\\InfoCellTowers_{dt}.kml";
            FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            StreamWriter sb = new StreamWriter(fs, Encoding.UTF8);

            sb.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.WriteLine("<kml>");
            sb.WriteLine("<Document>");
            sb.WriteLine($"<name>{layerName}</name>");
            sb.WriteLine("<createdby>InfoCellTowers</createdby>");
            sb.WriteLine(String.Format("<Folder><name><![CDATA[{0} (Объектов: {1})]]></name>", layerName, fc.features.Length));
            Multilang ml = new Multilang();
            foreach (Program.FeatureCollection.Feature f in fc.features)
            {
                sb.WriteLine("<Placemark>");
                sb.WriteLine(String.Format("<styleUrl>#cat{0}</styleUrl>", ml.Translit(f.properties.hintContent)));
                sb.WriteLine(String.Format("<name><![CDATA[{0}]]></name>", $"{FeatureCollection.StripHTML(f.properties.balloonContentHeader)} ({f.id})"));
                sb.WriteLine(String.Format("<description><![CDATA[{0}]]></description>", f.properties.balloonContentBody));
                sb.WriteLine(String.Format(System.Globalization.CultureInfo.InvariantCulture, "<Point><coordinates>{1},{0},0</coordinates></Point>", f.geometry.coordinates[0], f.geometry.coordinates[1]));
                sb.WriteLine("</Placemark>");
            };
            sb.WriteLine("</Folder>");
            for (int k = 0; k <= 14; k++)
                sb.WriteLine(String.Format("<Style id=\"cat{0}\"><IconStyle><Icon><href>images/cat{0}.png</href></Icon></IconStyle></Style>", k));
            sb.WriteLine("</Document>");
            sb.WriteLine("</kml>");
            sb.Close();
            return fileName;
        }

        [Serializable]
        private class Multilang
        {
            private Dictionary<string, string> words = new Dictionary<string, string>();

            private string en = "";
            private string ru = "";

            public string EN
            {
                get { return en; }
                set
                {
                    en = System.Web.HttpUtility.HtmlEncode(value);
                }
            }

            public string RU
            {
                get { return ru; }
                set
                {
                    ru = System.Web.HttpUtility.HtmlEncode(value);
                    if ((en == null) || (en == String.Empty) || (en.Length == 0)) en = Translit(ru);
                }
            }

            private void InitDict()
            {
                words.Add("а", "a");
                words.Add("б", "b");
                words.Add("в", "v");
                words.Add("г", "g");
                words.Add("д", "d");
                words.Add("е", "e");
                words.Add("ё", "yo");
                words.Add("ж", "zh");
                words.Add("з", "z");
                words.Add("и", "i");
                words.Add("й", "j");
                words.Add("к", "k");
                words.Add("л", "l");
                words.Add("м", "m");
                words.Add("н", "n");
                words.Add("о", "o");
                words.Add("п", "p");
                words.Add("р", "r");
                words.Add("с", "s");
                words.Add("т", "t");
                words.Add("у", "u");
                words.Add("ф", "f");
                words.Add("х", "h");
                words.Add("ц", "c");
                words.Add("ч", "ch");
                words.Add("ш", "sh");
                words.Add("щ", "sch");
                words.Add("ъ", "j");
                words.Add("ы", "i");
                words.Add("ь", "j");
                words.Add("э", "e");
                words.Add("ю", "yu");
                words.Add("я", "ya");
                words.Add("А", "A");
                words.Add("Б", "B");
                words.Add("В", "V");
                words.Add("Г", "G");
                words.Add("Д", "D");
                words.Add("Е", "E");
                words.Add("Ё", "Yo");
                words.Add("Ж", "Zh");
                words.Add("З", "Z");
                words.Add("И", "I");
                words.Add("Й", "J");
                words.Add("К", "K");
                words.Add("Л", "L");
                words.Add("М", "M");
                words.Add("Н", "N");
                words.Add("О", "O");
                words.Add("П", "P");
                words.Add("Р", "R");
                words.Add("С", "S");
                words.Add("Т", "T");
                words.Add("У", "U");
                words.Add("Ф", "F");
                words.Add("Х", "H");
                words.Add("Ц", "C");
                words.Add("Ч", "Ch");
                words.Add("Ш", "Sh");
                words.Add("Щ", "Sch");
                words.Add("Ъ", "J");
                words.Add("Ы", "I");
                words.Add("Ь", "J");
                words.Add("Э", "E");
                words.Add("Ю", "Yu");
                words.Add("Я", "Ya");

            }

            public string Translit(string RU)
            {
                string EN = RU;
                foreach (KeyValuePair<string, string> pair in words)
                    EN = EN.Replace(pair.Key, pair.Value);
                return EN;
            }

            public Multilang()
            {
                InitDict();
            }

            public Multilang(string EN, string RU)
            {
                InitDict();
                this.EN = EN;
                this.RU = RU;
            }
        }
    }
}
