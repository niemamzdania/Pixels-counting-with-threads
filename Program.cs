using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Threading;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Charting;

namespace Obrazek
{
    class Program
    {
        static Dictionary<Color, int>[] slowniki;
        static Bitmap obrazek;
        static Bitmap[] obrazki;

        struct Zakres
        {
            public int koniecSzer;
            public int koniecWys;
            public int numerWatku;
        }

        static void PoliczPiksele(object zakres)
        {
            Zakres zakresik = (Zakres)zakres;

            for (int i = 0; i < zakresik.koniecSzer; i++)
            {
                for (int j = 0; j < zakresik.koniecWys; j++)
                {
                    if (slowniki[zakresik.numerWatku].Keys.Contains(obrazki[zakresik.numerWatku].GetPixel(i, j)))
                    {
                        slowniki[zakresik.numerWatku][obrazki[zakresik.numerWatku].GetPixel(i, j)] += 1;
                    }
                    else
                    {
                        slowniki[zakresik.numerWatku].Add(obrazki[zakresik.numerWatku].GetPixel(i, j), 1);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Liczenie częstości występowania pikseli...\n");
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //Console.WriteLine("Podaj nazwę obrazka do wczytania");
            //string nazwaPliku = Console.ReadLine();
            int liczbaWatkow = 8;
            string sciezka = Directory.GetCurrentDirectory();
            string nazwaObrazka = "aaa.jpg";
            sciezka += @"\" + nazwaObrazka;
            //sciezka += @"\" + nazwaPliku;
            obrazek = new Bitmap(sciezka);
            obrazki = new Bitmap[liczbaWatkow];

            System.Drawing.Imaging.PixelFormat format = obrazek.PixelFormat;
            Rectangle prostokat;
            int poczatekSzer;
            int koniecSzer;
            int szer;
            int wys = obrazek.Height;

            Zakres zakres = new Zakres();
            zakres.koniecWys = obrazek.Height;
            Zakres[] zakresy = new Zakres[liczbaWatkow];

            for (int i=0; i<liczbaWatkow; i++)
            {
                poczatekSzer = obrazek.Width / liczbaWatkow * i;
                if (i != liczbaWatkow - 1)
                    koniecSzer = obrazek.Width / liczbaWatkow * i + obrazek.Width / liczbaWatkow;
                else koniecSzer = obrazek.Width;
                szer = koniecSzer - poczatekSzer;
                prostokat = new Rectangle(poczatekSzer, 0, szer, wys);
                obrazki[i] = obrazek.Clone(prostokat, format);
                zakresy[i].koniecSzer = szer;
                zakresy[i].koniecWys = obrazek.Height;
                zakresy[i].numerWatku = i;
            }

            slowniki = new Dictionary<Color, int>[liczbaWatkow];

            for (int i = 0; i < liczbaWatkow; i++)
            {
                slowniki[i] = new Dictionary<Color, int>();
            }

            //Parallel.ForEach(zakresy, (i) => PoliczPiksele(i));
            Thread[] watki = new Thread[liczbaWatkow];
            for(int i=0; i<liczbaWatkow; i++)
            {
                watki[i] = new Thread(PoliczPiksele);
            }
            for(int i=0; i<liczbaWatkow; i++)
            {
                watki[i].Start(zakresy[i]);
            }
            for(int i=0; i<liczbaWatkow; i++)
            {
                watki[i].Join();
            }

            Dictionary<Color, int> prawilnySlownik = new Dictionary<Color, int>();
            for(int i=0; i<slowniki.Length; i++)
            {
                foreach(var piksel in slowniki[i])
                {
                    if (prawilnySlownik.Keys.Contains(piksel.Key))
                    {
                        prawilnySlownik[piksel.Key] += piksel.Value;
                    }
                    else
                    {
                        prawilnySlownik.Add(piksel.Key, piksel.Value);
                    }
                }
            }

            var posortowany = prawilnySlownik.OrderByDescending(x => x.Value);

            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            Console.WriteLine("Nazwa obrazka: " + nazwaObrazka);
            Console.WriteLine("Rozmiar obrazka: {0}x{1}", obrazek.Width, obrazek.Height);
            Console.WriteLine("Suma pikseli: " + obrazek.Width * obrazek.Height);
            Console.WriteLine("Liczba wątków: " + liczbaWatkow);
            Console.WriteLine("\nNajczęściej występujące piksele:");

            List<string> linieTekstu = new List<string>();
            linieTekstu.Add("Nazwa obrazka: " + nazwaObrazka);
            linieTekstu.Add("Rozmiar obrazka: " + obrazek.Width.ToString() + "x" + obrazek.Height.ToString());
            
            linieTekstu.Add("Suma pikseli: " + obrazek.Width * obrazek.Height);
            linieTekstu.Add("Liczba wątków: " + liczbaWatkow.ToString());
            linieTekstu.Add("Czas wykonania: " + ts.Seconds.ToString() + ":" + ts.Milliseconds.ToString());
            linieTekstu.Add("");
            linieTekstu.Add("Najczęściej występujące piksele:");

            int licznik = 0;
            foreach (var pixel in posortowany)
            {
                licznik++;
                Console.WriteLine("Piksel nr {4}: [R={0}, G={1}, B={2}], liczba wystąpień: {3}", pixel.Key.R, pixel.Key.G, pixel.Key.B, pixel.Value, licznik);
                linieTekstu.Add("Piksel nr " + licznik + ": [R=" + pixel.Key.R + ", G=" + pixel.Key.G + ", B=" + pixel.Key.B + "], liczba wystąpień: " + pixel.Value);
                if (licznik >= 10)
                    break;
            }
            Console.WriteLine("Czas wykonania: " + ts + "\n");

            PdfDocument pdf = new PdfDocument();
            PdfPage strona = pdf.AddPage();
            XGraphics graf = XGraphics.FromPdfPage(strona);
            XFont font = new XFont("Calibri", 15);
            graf.DrawString("Przemysław Prusik", font, XBrushes.Black, new XRect(10, 10, strona.Width.Point, strona.Height.Point), XStringFormats.TopCenter);
            font = new XFont("Calibri Light", 15);
            int przesuniecie = 30;
            foreach (var linia in linieTekstu)
            {
                graf.DrawString(linia, font, XBrushes.Black, new XRect(10, przesuniecie, strona.Width.Point, strona.Height.Point), XStringFormats.TopLeft);
                przesuniecie += 20;
            }

            Console.WriteLine("Generowanie pliku pdf...\n");

            double[] R = new double[256];
            double[] G = new double[256];
            double[] B = new double[256];

            foreach (var pixel in posortowany)
            {
                R[pixel.Key.R] += pixel.Value;
                G[pixel.Key.G] += pixel.Value;
                B[pixel.Key.B] += pixel.Value;
            }

            Chart histogram = Histogram(R, G, B);

            ChartFrame ramaHistogramu = new ChartFrame();
            ramaHistogramu.Location = new XPoint(50, 40);
            ramaHistogramu.Size = new XSize(495, 300);
            ramaHistogramu.Add(histogram);

            PdfPage strona2 = pdf.AddPage();

            XGraphics hist = XGraphics.FromPdfPage(strona2);
            font = new XFont("Calibri", 15, XFontStyle.Bold);
            hist.DrawString("Histogram", font, XBrushes.Black, new XRect(10, 10, strona.Width.Point, strona.Height.Point), XStringFormats.TopCenter);
            ramaHistogramu.Draw(hist);

            pdf.Save("wyniki.pdf");

            Console.WriteLine("Zakończono działanie programu");

            Console.ReadKey();
        }

        static Chart Histogram(double[] R, double[] G, double[] B)
        {
            Chart chart = new Chart(ChartType.Line);

            Series dane = chart.SeriesCollection.AddSeries();
            dane.Name = "R";
            dane.Add(R);
            dane.MarkerStyle = MarkerStyle.None;
            dane.MarkerBackgroundColor = XColor.FromArgb(255, 0, 0);

            dane = chart.SeriesCollection.AddSeries();
            dane.Name = "G";
            dane.Add(G);
            dane.MarkerStyle = MarkerStyle.None;
            dane.MarkerBackgroundColor = XColor.FromArgb(0, 255, 0);

            dane = chart.SeriesCollection.AddSeries();
            dane.Name = "B";
            dane.Add(B);
            dane.MarkerStyle = MarkerStyle.None;
            dane.MarkerBackgroundColor = XColor.FromArgb(0, 0, 255);

            chart.XAxis.Title.Caption = "nasycenie";
            chart.YAxis.Title.Caption = "liczba wystąpień";
            chart.YAxis.HasMajorGridlines = true;

            chart.Legend.Docking = DockingType.Right;

            XSeries xseries = chart.XValues.AddXSeries();
            
            for(int i=0; i<256; i++)
            {
                if (i % 51 == 0)
                    xseries.Add(i.ToString());
                else xseries.Add("");
            }

            return chart;
        }
    }
}
