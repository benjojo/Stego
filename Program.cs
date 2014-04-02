using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Steg
{
    class Program
    {
        static void Main(string[] args)
        {
            Rand = new Random(); // Init the RNG
            string FileName = "";
            string EncodeTarget = "";
            foreach(string arg in args)
            {
                if (arg.EndsWith(".jpg") || arg.EndsWith(".png"))
                {
                    FileName = arg;
                }
                else
                {
                    EncodeTarget = ("" + (char)0xff) + arg + "\0";
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
                        Bitmap FiddleBackup = Fiddled; // this is needed in the case that a block cannot be warped.
                        Fiddled = Fiddle(H, W, E, Fiddled);
                        if (Fiddled == null)
                        {
                            Console.WriteLine("Block cannot be warped, Skipping.");
                            Fiddled = Fiddle(H, W, "" + (char)0xff, FiddleBackup);
                            if (Fiddled == null) // In case of double failure :(
                            {
                                Fiddled = FiddleBackup;
                                Console.WriteLine("Warning. Double Failure. This means the image won't be encoded 100% correctly");
                            }
                        }
                        else
                        {
                            StringPointer++;
                        }

                        if (StringPointer == EncodeTarget.Length)
                        {
                            goto QuickExit;
                        }
                    }
                }
            QuickExit: ;
                SaveWithJpegQuality(Fiddled).Save("./done.jpg");
            }
            else if (FileName != "")
            {
                // Decode time.
                Bitmap Orig = new Bitmap(Image.FromFile(FileName));
                DecodeImage(Orig);
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("Please enter a file name and a string to encode in there. $tool.exe hello.jpg \"I am a string\"");
            }
        }

        static public void DecodeImage(Bitmap Orig)
        {
            int CharsIn = 0;
            int HDiv = Orig.Height / 8;
            int WDiv = Orig.Width / 8;
            for (int H = 0; H < Orig.Height; H = H + 8)
            {
                for (int W = 0; W < Orig.Width; W = W + 8)
                {
                    string o = Get(H, W, Orig);
                    byte[] StrByte = System.Text.ASCIIEncoding.Default.GetBytes(o);
                    if (CharsIn == 0 && (StrByte[0] != 0xff || (int)StrByte[0] != 63 ))
                    {
                        Console.WriteLine("This does not look like a correct file. Stego files start with 0xff");
                        Console.WriteLine("This one starts with {0} or {1}", o, (int)StrByte[0]);
                        Environment.Exit(1);
                    }


                    if (o == "\0")
                    {
                        goto end;
                    }
                    else if (o == "" + (char)0xff)
                    {
                        // Don't do anything
                    }
                    else
                    {
                        Console.Write(Get(H, W, Orig));
                    }
                }
            }
        end: ;
            return;
        }
        // This function attempt to get that jpeg block to equal the letter requested
        static Random Rand;
        static Bitmap Fiddle(int TH, int TW, string E, Bitmap I)
        {
            Bitmap Temp = I;
            int FiddleAmount = 0; // Times this has been fiddled with.
        tryagain: ;
            int H = TH;
            int W = TW;
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

        // This function loops though all of the pixles in a block and gives you a string to hash.
        // This function could be improved by not using a massive long string of numbers and infact just
        // add in the int bytes in to a string to be hashed. Not sure if that will be faster or slower with
        // the bitconverter. It also breaks images that where encoded with older versions.
        static string CrunchBlock(int TH, int TW, Bitmap I)
        {
            string HashTarget = "";
            for (int H = TH; H < TH + 8; H++)
            {
                for (int W = TW; W < TW + 8; W++)
                {
                    if (I.Height <= H || I.Width <= W)
                        return "";
                    Color C = I.GetPixel(W, H);
                    
                    HashTarget = HashTarget + (int)C.R + (int)C.G + (int)C.B;

                }
            }
            return HashTarget;
        }

        static int ImageIterationCount = 0;
        // This function is used to get a bit of data back from the image
        static string Get(int TH, int TW, Bitmap I)
        {

            string boom = CrunchBlock(TH,TW,I);
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(boom + "");
            byte[] hash = md5.ComputeHash(inputBytes);
            return (System.Text.Encoding.ASCII.GetString(hash).Substring(0,1));
    
        }
        // This is a nice little shortcut fuction 
        static Bitmap SaveWithJpegQuality(Bitmap I)
        {
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;
            ImageCodecInfo myImageCodecInfo;
            System.Drawing.Imaging.Encoder myEncoder;
            myEncoder = System.Drawing.Imaging.Encoder.Quality;
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
            string HashTarget = CrunchBlock(TH, TW, I);

            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] HashInputBytes = System.Text.Encoding.ASCII.GetBytes(HashTarget + "");
            byte[] hash = md5.ComputeHash(HashInputBytes);
            byte[] c = System.Text.Encoding.ASCII.GetBytes(E);
            ImageIterationCount++;
            if (hash[0] == System.Text.Encoding.ASCII.GetBytes(E)[0])
            {
                Console.WriteLine("Warped block {0},{1} to be char {2}",TW,TH, E);
                return true;
            }
            else
            {
                if (ImageIterationCount % 100 == 0)
                    Console.WriteLine("Processed {0} images", ImageIterationCount);
                
                return false;
            }
        }
        // A quick shortcut function to do all the messy image encoder stuff.
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (int j = 0; j < encoders.Length; j++)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
    }
}
