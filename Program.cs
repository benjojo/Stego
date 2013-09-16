using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.IO;

namespace Steg
{
    class Program
    {
        static void Main(string[] args)
        {
            Rand = new Random();
            string FileName = "";
            string EncodeTarget = "";
            string WD = Path.GetDirectoryName(Environment.CurrentDirectory);
            foreach(string arg in args)
            {
                if (arg.EndsWith(".jpg") || arg.EndsWith(".png"))
                {
                    FileName = arg;
                }
                else
                {
                    EncodeTarget = arg + "\0";
                }
            }
            if (FileName != "" && EncodeTarget != "")
            {

                Bitmap Orig = new Bitmap (Image.FromFile(FileName));
                Bitmap Fiddled = Orig;
                int HDiv = Orig.Height / 8;
                int WDiv = Orig.Width / 8;
                int StringPointer = 0;
                for (int H = 0; H < Orig.Height; H = H + 8)
                {
                    for (int W = 0; W < Orig.Width; W = W + 8)
                    {
                        string E = EncodeTarget.Substring(StringPointer, 1);
                        Bitmap FiddleBackup = Fiddled;
                        Fiddled = Fiddle(H, W, E, Fiddled);
                        if (Fiddled == null)
                        {
                            Fiddled = Fiddle(H, W, string.Format("{0}", 0xff), FiddleBackup);
                        }
                        else
                        {
                            StringPointer++;
                        }

                        if (StringPointer == EncodeTarget.Length)
                        {
                            goto boop;
                        }
                    }
                }
            boop: ;
                SaveWithJpegQuality(Fiddled).Save("./done.jpg");
            }
            else if (FileName != "")
            {
                // Decode time.
                Bitmap Orig = new Bitmap(Image.FromFile(FileName));

                int HDiv = Orig.Height / 8;
                int WDiv = Orig.Width / 8;
                int StringPointer = 0;
                for (int H = 0; H < HDiv; H = H + 8)
                {
                    for (int W = 0; W < WDiv; W = W + 8)
                    {
                        Console.Write(Get(H, W, Orig));
                    }
                }
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("Please enter a file name and a string to encode in there. $tool.exe hello.jpg \"I am a string\"");
            }
        }
        
        // This function attempt to get that jpeg block to equal the letter requested
        static Random Rand;
        static Bitmap Fiddle(int TH, int TW, string E, Bitmap I)
        {
            Bitmap Temp = I;
        tryagain: ;
            int H = TH;
            int W = TW;
            int FiddleAmount = 0; // Times this has been fiddled with.
            for (H = TH; H < TH + 8; H++)
            {
                for (W = TW; W < TW + 8; W++)
                {
                    FiddleAmount++;

                    Color C = I.GetPixel(W, H);
                    
                    int R = Rand.Next(0, 255);
                    int G = Rand.Next(0, 255);
                    int B = Rand.Next(0, 255);
                    I.SetPixel(W, H, Color.FromArgb(255,R, G, B));
                    if (FiddleAmount > 9000) // over 9000!?
                    {
                        return null;
                    }
                    if (Check(TH, TW, E, I))
                    {
                        return Temp;
                    }
                    else
                    {
                        I.SetPixel(W, H, C);
                    }
                }
            }
            goto tryagain;
            
        }

        static int aaa = 0;
        // This function is used to get a bit of data back from the image
        static string Get(int TH, int TW, Bitmap I)
        {
             string boom = "";
             for (int H = TH; H < TH + 8; H++)
             {
                 for (int W = TW; W < TW + 8; W++)
                 {
                     Color C = I.GetPixel(W, H);
                     int R = C.R;
                     int G = C.G;
                     int B = C.B;

                     boom = boom + R + G + B;

                 }
             }
             MD5 md5 = System.Security.Cryptography.MD5.Create();
             byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(boom + "");
             byte[] hash = md5.ComputeHash(inputBytes);
             return (System.Text.Encoding.ASCII.GetString(hash).Substring(0,1));
    
        }

        static Bitmap SaveWithJpegQuality(Bitmap I)
        {
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;
            ImageCodecInfo myImageCodecInfo;
            Encoder myEncoder;
            myEncoder = Encoder.Quality;
            // Save the bitmap as a JPEG file with quality level 25.
            myImageCodecInfo = GetEncoderInfo("image/jpeg");
            myEncoderParameter = new EncoderParameter(myEncoder, 25L);
            myEncoderParameters = new EncoderParameters(1);
            myEncoderParameters.Param[0] = myEncoderParameter;
            MemoryStream A = new MemoryStream();
            I.Save(A, myImageCodecInfo, myEncoderParameters);
            Bitmap Iold = I;
            I = new Bitmap(Image.FromStream(A));
            return I;
        }

        // This function is used to test if the "fiddle" with the jpeg block actually worked.
        static bool Check(int TH, int TW, string E, Bitmap I)
        {
            I = SaveWithJpegQuality(I);
            string boom = "";
            for (int H = TH; H < TH + 8; H++)
            {
                for (int W = TW; W < TW + 8; W++)
                {
                    //Console.WriteLine("Checked {0} {1}", H, W);
                    Color C = I.GetPixel(W, H);
                    int R = C.R;
                    int G = C.G;
                    int B = C.B;

                    boom = boom + R + G + B;

                }
            }

            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(boom + "");
            byte[] hash = md5.ComputeHash(inputBytes);
            byte[] c = System.Text.Encoding.ASCII.GetBytes(E);
            aaa++;
            if (hash[0] == System.Text.Encoding.ASCII.GetBytes(E)[0])
            {
                Console.WriteLine("Got a Ding on {0}", E);
                return true;
            }
            else
            {
                if (aaa % 100 == 0)
                {
                    Console.WriteLine("Done {0} jpgs", aaa);
                }
                return false;
            }
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
    }
}
