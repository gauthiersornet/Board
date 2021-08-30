using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModuleBOARD.Elements.Base
{
    public class BibliothèqueImage
    {
        static public Image InitImageVide(string sig)
        {
            int w, h;
            SigExImageInfo(sig, out _, out w, out h);
            Image img;
            if (0 < w && w < OutilsRéseau.NB_WPixel_MAX && 0 < h && h < OutilsRéseau.NB_HPixel_MAX) // ni trop petite ni trop grande ?
                img = InitImageVide(w, h);
            else img = InitErrorImage(20, 20);
            img.Tag = sig;
            return img;
        }

        static public Image InitImageVide(int w, int h)
        {
            Image img = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(img);
            Pen p = new Pen(Color.Red, 5);
            Brush b = new SolidBrush(Color.Black);
            g.FillRectangle(b, 0, 0, w, h);
            g.DrawRectangle(p, 0, 0, w, h);
            byte[] hash = new byte[16];
            img.Tag = ImageInfoToSig(hash, 100, 0, 0, w, h, "");
            return img;
        }

        static public Image InitErrorImage(int w, int h)
        {
            Image img = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(img);
            Pen p = new Pen(Color.Red, 5);
            Brush b = new SolidBrush(Color.Black);
            g.FillRectangle(b, 0, 0, w, h);
            g.DrawRectangle(p, 0, 0, w, h);
            g.DrawLine(p, 0, 0, w, h);
            g.DrawLine(p, 0, w, 0, h);
            byte[] hash = new byte[16];
            for (int i = 0; i < 16; ++i) hash[i] = 0xFF;
            img.Tag = ImageInfoToSig(new byte[16], 100, 0, 0, w, h, "");
            return img;
        }

        static public Bitmap Rencoder(Bitmap nbtmap, Guid format, Int64 qualité)
        {
            ImageCodecInfo Encoder = ImageCodecInfo.GetImageDecoders().First(ecd => ecd.FormatID == format);
            EncoderParameter myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qualité);
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            myEncoderParameters.Param[0] = myEncoderParameter;
            MemoryStream strm = new MemoryStream();
            nbtmap.Save(strm, Encoder, myEncoderParameters);
            return Image.FromStream(strm) as Bitmap;
        }

        static private int CalculDist(uint cref, uint ctest)
        {
            byte ctestA = (byte)((ctest >> 24) & 0xFF);
            if (ctestA < 128) return 0;
            else return Math.Max(Math.Abs((int)((cref >> 16) & 0xFF) - (int)((ctest >> 16) & 0xFF)),
                    Math.Max(Math.Abs((int)((cref >> 8) & 0xFF) - (int)((ctest >> 8) & 0xFF)),
                    Math.Abs((int)((cref >> 0) & 0xFF) - (int)((ctest >> 0) & 0xFF))));
        }

        static public Bitmap ImageVersMemoryBitmap(Bitmap btmp)
        {
            if (btmp.RawFormat.Guid != ImageFormat.MemoryBmp.Guid)
            {
                Bitmap nbtmap = new Bitmap(btmp.Width, btmp.Height);
                Graphics.FromImage(nbtmap).DrawImage(btmp, new Rectangle(0, 0, btmp.Width, btmp.Height), new Rectangle(0, 0, btmp.Width, btmp.Height), GraphicsUnit.Pixel);
                nbtmap.Tag = btmp.Tag;
                btmp = nbtmap;
            }
            return btmp;
        }

        static public Bitmap FaireTransparent(Bitmap btmp, int seuil, ref uint coulTrans)//coulTrans = 0x00FFFFFF
        {
            uint ctrans;
            btmp = ImageVersMemoryBitmap(btmp);
            BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    uint* grePtr = (uint*)btdt.Scan0.ToPointer();
                    if (coulTrans != 0 && coulTrans != 0x00FFFFFF) ctrans = coulTrans;
                    else ctrans = *grePtr;
                    ctrans &= 0xFFFFFF;

                    int x, y;
                    y = btmp.Height - 1;
                    for (x = 0; x < btmp.Width; ++x)
                    {
                        for (int sY = 0; sY < btmp.Height && CalculDist(ctrans, *(grePtr + x + sY * btmp.Width)) < seuil; ++sY)
                            *(grePtr + x + sY * btmp.Width) = ctrans;

                        for (int sY = y; sY >= 0 && CalculDist(ctrans, *(grePtr + x + sY * btmp.Width)) < seuil; --sY)
                            *(grePtr + x + sY * btmp.Width) = ctrans;
                    }

                    x = btmp.Width - 1;
                    for (y = 0; y < btmp.Height; ++y)
                    {
                        for (int sX = 0; sX < btmp.Width && CalculDist(ctrans, *(grePtr + sX + y * btmp.Width)) < seuil; ++sX)
                            *(grePtr + sX + y * btmp.Width) = ctrans;

                        for (int sX = x; sX >= 0 && CalculDist(ctrans, *(grePtr + sX + y * btmp.Width)) < seuil; --sX)
                            *(grePtr + sX + y * btmp.Width) = ctrans;
                    }
                }
            }
            catch { }
            finally { btmp.UnlockBits(btdt); }

            return btmp;
        }

        static public Bitmap AppliquerTransparence(Bitmap nbtmap, Guid gformat, ushort coin, ref uint alphaCoul)
        {
            if (alphaCoul != 0) nbtmap = BibliothèqueImage.FaireTransparent(nbtmap as Bitmap, gformat, ref alphaCoul);
            if (coin > 0)
            {
                nbtmap = BibliothèqueImage.ImageVersMemoryBitmap(nbtmap as Bitmap);
                BibliothèqueImage.ArrondirCoins(nbtmap as Bitmap, coin, alphaCoul);
            }
            return nbtmap;
        }

        static public Bitmap FaireTransparent(Bitmap nbtmap, Guid gformat, ref uint coulTransp) //coulTransp = 0x00FFFFFF
        {
            //Formats déstructif
            if (gformat == ImageFormat.Jpeg.Guid || gformat == ImageFormat.Wmf.Guid)
                return FaireTransparent(nbtmap, 30, ref coulTransp);
            else if (coulTransp == 0x00FFFFFF) nbtmap.MakeTransparent();
            else nbtmap.MakeTransparent(Color.FromArgb((int)coulTransp));
            return nbtmap;
        }

        static public void ArrondirCoins(Bitmap nbtmap, int arrondiCoins, uint coulTrans = 0x00000000)
        {
            int r = (arrondiCoins * Math.Min(nbtmap.Width, nbtmap.Height)) / 2000;
            if (r > 1)
            {
                uint ctrans;
                BitmapData btdt = nbtmap.LockBits(new Rectangle(0, 0, nbtmap.Width, nbtmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                try
                {
                    unsafe
                    {
                        uint* grePtr = (uint*)btdt.Scan0.ToPointer();

                        if(coulTrans==0 || coulTrans == 0x00FFFFFF)ctrans = *grePtr;
                        else ctrans = coulTrans;
                        ctrans &= 0xFFFFFF;

                        double r2 = r * r;
                        for (int j = 0; j <= r; ++j)
                        {
                            int l = r - (int)((r * Math.Sqrt(1.0 - ((r - j) * (r - j)) / r2)) + 0.5);

                            for (int i = 0; i < l; ++i)
                            {
                                *(grePtr + (i) + (j) * nbtmap.Width) = ctrans;
                                *(grePtr + (nbtmap.Width - 1 - i) + (j) * nbtmap.Width) = ctrans;
                                *(grePtr + (i) + (nbtmap.Height - 1 - j) * nbtmap.Width) = ctrans;
                                *(grePtr + (nbtmap.Width - 1 - i) + (nbtmap.Height - 1 - j) * nbtmap.Width) = ctrans;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                }
                finally { nbtmap.UnlockBits(btdt); }
            }
        }

        static public readonly Image ImageVide = InitImageVide(20, 20);
        private Dictionary<string, (Image, Guid, byte, ushort, uint)> DImgs = new Dictionary<string, (Image, Guid, byte, ushort, uint)>();
        //private Dictionary<string, Image> DImgsInco = new Dictionary<string, Image>();//images inconnues
        //private HashSet<string> HSetImgsInco = new HashSet<string>();
        private SortedSet<string> LstImgsInco = new SortedSet<string>();

        /*static public MemoryStream ImageVersStream(MemoryStream memStream, Image img)
        {
            string sig = img.Tag as string;
            if (sig != null)
            {
                sig = sig.Substring(sig.Length - 5).ToUpper();
                if (sig.EndsWith(".JPEG") || sig.EndsWith(".JPG")) img.Save(memStream, ImageFormat.Jpeg);
                else if (sig.EndsWith(".PNG")) img.Save(memStream, ImageFormat.Png);
                else if (sig.EndsWith(".BMP")) img.Save(memStream, ImageFormat.Bmp);
                else img.Save(memStream, ImageFormat.Jpeg);
            }
            else img.Save(memStream, ImageFormat.Jpeg);
            return memStream;
        }*/

        /*static public Image ChargerImage(string sig, MemoryStream memStream)
        {
            Image img = new Bitmap(memStream);
            img.Tag = sig;
            return img;
        }*/

        static public Image ChargerImage(ref MemoryStream stream)
        {
            string sig = stream.ReadString();
            {
                //On ajuste le flux en supprimant ce qui a été consomé de façon à charger l'image sans accro
                MemoryStream ms = new MemoryStream();
                stream.CopyTo(ms);
                stream = ms;
            }
            Image img = Bitmap.FromStream(stream);
            if (sig.Length >= 28)
            {
                int w, h;
                SigExImageInfo(sig, out _, out w, out h);
                if (img.Width == w && img.Height == h)
                {
                    img.Tag = sig;
                    return img;
                }
                else return null;
            }
            else return null;
        }

        static public bool SauvegarderImage((Image, Guid, byte, ushort, uint) img, Stream stream)
        {
            if (img.Item1 != null && img.Item2 != ImageFormat.MemoryBmp.Guid)
            {
                stream.WriteByte(img.Item3);
                stream.WriteBytes(BitConverter.GetBytes(img.Item4));
                stream.WriteBytes(BitConverter.GetBytes(img.Item5));
                string sig = img.Item1.Tag as string;
                stream.SerialiserObject(sig ?? "");
                if (img.Item1.RawFormat.Guid != ImageFormat.MemoryBmp.Guid)
                    img.Item1.Save(stream, img.Item1.RawFormat);
                else
                {
                    Bitmap btmp = Rencoder(img.Item1 as Bitmap, img.Item2, 100);
                    btmp.Save(stream, btmp.RawFormat);
                }
                return true;
            }
            else return false;
        }

        public bool RécupérerImage(string sig, Stream stream)
        {
            lock (DImgs)
            {
                (Image, Guid, byte, ushort, uint) img;
                if (DImgs.TryGetValue(sig, out img) && img.Item2 != ImageFormat.MemoryBmp.Guid)
                {
                    return SauvegarderImage(img, stream);
                }
                else return false;
            }
        }

        static public string SigExImageInfo(string sig, out byte[] hash, out int w, out int h)
        {
            w = h = 0;
            if (sig.Length >= 28)
            {
                byte[] bs = Convert.FromBase64String(sig.Substring(0, 28));
                hash = new byte[16];

                w |= (bs[0] << 0);
                w |= (bs[1] << 8);
                w |= ((bs[2] & 0x0F) << 16);
                h |= (((bs[2] >> 4) & 0x0F) << 16);
                h |= (bs[3] << 8);
                h |= (bs[4] << 0);

                Array.Copy(bs, 5, hash, 0, hash.Length);

                return sig.Substring(28);
            }
            else
            {
                hash = null;
                return null;
            }
        }

        static public string ImageInfoToSig(byte[] bMd5Hash, byte qualité, ushort coin, uint alpahCoul, int w, int h, string fileName)
        {
            byte[] bsig = new byte[bMd5Hash.Length + 5];
            bsig[0] = (byte)((w >> 0) & 0xFF);
            bsig[1] = (byte)((w >> 8) & 0xFF);
            bsig[2] = (byte)((w >> 16) & 0x0F);
            bsig[2] |= (byte)(((h >> 16) & 0x0F) << 4);
            bsig[3] = (byte)((h >> 8) & 0xFF);
            bsig[4] = (byte)((h >> 0) & 0xFF);
            Array.Copy(bMd5Hash, 0, bsig, 5, bMd5Hash.Length);
            //if (alpahCoul != 0) bsig[20] |= 1; else bsig[20] &= 0xFE;
            //if (fileName.Length > 2) fileName = fileName.Substring(0,2);
            return Convert.ToBase64String(bsig);// + fileName;
        }

        static public string GetImageSig(string fileName, byte qualité, ushort coin, uint alpahCoul, Bitmap btmp, int x, int y)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] block = UTF8Encoding.UTF8.GetBytes(fileName);
                md5Hash.TransformBlock(block, 0, block.Length, block, 0);
                if (btmp.RawFormat.Guid == ImageFormat.MemoryBmp.Guid)
                {
                    BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb); //PixelFormat.Format32bppArgb
                    block = new byte[4 * btmp.Width * btmp.Height];
                    System.Runtime.InteropServices.Marshal.Copy(btdt.Scan0, block, 0, block.Length);
                    md5Hash.TransformFinalBlock(block, 0, block.Length);
                    btmp.UnlockBits(btdt);
                }
                else
                {
                    block = btmp.RawFormat.Guid.ToByteArray();
                    md5Hash.TransformBlock(block, 0, block.Length, block, 0);
                    using (MemoryStream strm = new MemoryStream())
                    {
                        btmp.Save(strm, btmp.RawFormat);
                        block = strm.ToArray();
                        md5Hash.TransformFinalBlock(block, 0, block.Length);
                    }
                }
                return ImageInfoToSig(md5Hash.Hash, qualité, coin, alpahCoul, btmp.Width, btmp.Height, fileName);
            }
        }

        public Image CheckDicoImage(Bitmap img, Guid gformat, byte qualité, ushort coin, uint alpahCoul, string file, int x, int y)
        {
            string fileSig = GetImageSig(file, qualité, coin, alpahCoul, img, x, y);
            lock (DImgs)
            {
                if (DImgs.ContainsKey(fileSig)) return DImgs[fileSig].Item1;
                else
                {
                    img.Tag = fileSig;
                    DImgs.Add(fileSig, (img, gformat, qualité, coin, alpahCoul));
                    return img;
                }
            }
        }

        public Image ChargerImage(string path, string file)
        {
            try
            {
                if (file != null && file != "")
                {
                    Bitmap btmp = Bitmap.FromFile(path + file) as Bitmap;

                    byte qualité;
                    ushort coin;
                    uint alphaCoul;
                    (qualité, coin, alphaCoul) = ExtraireQualitéArrondiCoinAlphaCoul(btmp);
                    Guid gformat = btmp.RawFormat.Guid;
                    btmp = BibliothèqueImage.AppliquerTransparence(btmp as Bitmap, btmp.RawFormat.Guid, coin, ref alphaCoul);
                    return CheckDicoImage(btmp, gformat, qualité, coin, alphaCoul, file, 0, 0);
                }
                else return null;
            }
            catch
            {
                return null;
            }
        }

        public Image ChargerSImage(string path, string file, XmlNode paq, string kx = null, string ky = null, string kw = null, string kh = null)
        {
            Int64 qualité;
            ushort coin;
            uint alphaCoul;

            //Image img = LoadImage(path, file);
            Bitmap btmp = Bitmap.FromFile(path + file) as Bitmap;

            XmlAttributeCollection att = paq.Attributes;

            string strX = kx != null ? att?.GetNamedItem(kx)?.Value : null;
            string strY = ky != null ? att?.GetNamedItem(ky)?.Value : null;
            string strW = kw != null ? att?.GetNamedItem(kw)?.Value : null;
            string strH = kh != null ? att?.GetNamedItem(kh)?.Value : null;

            int x;
            if (strX == null || int.TryParse(strX, out x) == false) x = 0;
            int y;
            if (strY == null || int.TryParse(strY, out y) == false) y = 0;
            int w;
            if (strW == null || int.TryParse(strW, out w) == false || w > btmp.Width) w = btmp.Width;
            int h;
            if (strH == null || int.TryParse(strH, out h) == false || h > btmp.Height) h = btmp.Height;

            (qualité, coin, alphaCoul) = ExtraireQualitéArrondiCoinAlphaCoul(btmp, paq);

            if (x >= btmp.Width || y >= btmp.Height || w <= 0 || h <= 0) return default;
            else return RecadreCheckDicoImage(btmp, qualité, alphaCoul, coin, file, x, y, w, h);
        }

        static public (byte, ushort, uint) ExtraireQualitéArrondiCoinAlphaCoul(Bitmap btmap)
        {
            if (btmap.RawFormat.Guid == ImageFormat.Jpeg.Guid)
            {
                PropertyItem prop = btmap.PropertyItems.FirstOrDefault(p => p.Id == 0);
                if (prop != null && prop.Type == 1 && prop.Len == 7)
                {
                    byte q = prop.Value[0];
                    ushort c = BitConverter.ToUInt16(prop.Value, 1);
                    uint ac = BitConverter.ToUInt32(prop.Value, 1 + 2);
                    return (q, c, ac);
                }
            }
            return (100, 0, 0);
        }

        static public (byte, ushort, uint) ExtraireQualitéArrondiCoinAlphaCoul(XmlNode paq)
        {
            byte qualité;
            ushort coin;
            uint couleurTrans;

            XmlAttributeCollection att = paq.Attributes;
            string strQual = att?.GetNamedItem("qualité")?.Value;
            if (strQual == null || byte.TryParse(strQual, out qualité) == false) qualité = 100;
            string strCoin = att?.GetNamedItem("coin")?.Value;
            if (strCoin == null || ushort.TryParse(strCoin, out coin) == false) coin = 0;
            string strColT = att?.GetNamedItem("CouleurAlpha")?.Value;
            if (strColT != null) couleurTrans = Convert.ToUInt32(strColT, 16);
            else couleurTrans = 0;

            return (qualité, coin, couleurTrans);
        }

        static public (byte, ushort, uint) ExtraireQualitéArrondiCoinAlphaCoul(Bitmap btmap, XmlNode paq)
        {
            byte qualité;
            ushort coin;
            uint alphaCoul;
            (qualité, coin, alphaCoul) = ExtraireQualitéArrondiCoinAlphaCoul(paq);
            var qcac = ExtraireQualitéArrondiCoinAlphaCoul(btmap);
            if (qcac.Item1<qualité) qualité = qcac.Item1;
            if (qcac.Item2 > coin) coin = qcac.Item2;
            if (alphaCoul == 0) alphaCoul = qcac.Item3;
            if (paq.Attributes?.GetNamedItem("opaque") != null) alphaCoul = 0;
            else alphaCoul = 0x00FFFFFF;
            return (qualité, coin, alphaCoul);
        }

        public KeyValuePair<Image, Image> ChargerSImageRV(string path, string file, XmlNode paq, string kx = null, string ky = null, string kw = null, string kh = null, string kdx = null, string kdy = null)
        {
            Int64 qualité;
            ushort coin;
            uint alphaCoul;

            //Image img = LoadImage(path, file);
            Bitmap btmp = Bitmap.FromFile(path + file) as Bitmap;

            XmlAttributeCollection att = paq.Attributes;

            string strX = kx != null ? att?.GetNamedItem(kx)?.Value : null;
            string strY = ky != null ? att?.GetNamedItem(ky)?.Value : null;
            string strW = kw != null ? att?.GetNamedItem(kw)?.Value : null;
            string strH = kh != null ? att?.GetNamedItem(kh)?.Value : null;

            int x;
            if (strX == null || int.TryParse(strX, out x) == false) x = 0;
            int y;
            if (strY == null || int.TryParse(strY, out y) == false) y = 0;
            int w;
            if (strW == null || int.TryParse(strW, out w) == false || w > btmp.Width) w = btmp.Width / 2;
            int h;
            if (strH == null || int.TryParse(strH, out h) == false || h > btmp.Height) h = btmp.Height;

            strX = kdx != null ? att?.GetNamedItem(kdx)?.Value : null;
            strY = kdy != null ? att?.GetNamedItem(kdy)?.Value : null;

            int dx;
            if (strX == null || int.TryParse(strX, out dx) == false) dx = btmp.Width / 2;
            int dy;
            if (strY == null || int.TryParse(strY, out dy) == false) dy = 0;

            (qualité, coin, alphaCoul) = ExtraireQualitéArrondiCoinAlphaCoul(btmp, paq);

            if (x >= btmp.Width || y >= btmp.Height || w <= 0 || h <= 0) return default;
            else if (x >= btmp.Width || y >= btmp.Height || w <= 0 || h <= 0) return default;
            else return new KeyValuePair<Image, Image>(RecadreCheckDicoImage(btmp, qualité, alphaCoul, coin, file, dx, dy, w, h), RecadreCheckDicoImage(btmp, qualité, alphaCoul, coin, file, x, y, w, h));
        }

        public Bitmap Recadrer(Bitmap btmp, int x, int y, int w, int h, Int64 qualité, uint coulTransp, ushort coin)
        {
            if (x > 0 || y > 0 || w < btmp.Width || h < btmp.Height)
            {
                Bitmap nbtmap = new Bitmap(w, h);
                Graphics.FromImage(nbtmap).DrawImage(btmp, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                if (coulTransp != 0 || coin > 0)
                {
                    return BibliothèqueImage.AppliquerTransparence(nbtmap as Bitmap, btmp.RawFormat.Guid, coin, ref coulTransp);
                    /*
                    if(coulTransp != 0) nbtmap = FaireTransparent(nbtmap, btmp.RawFormat.Guid, coulTransp);
                    if (coin > 0)
                    {
                        //nbtmap = ImageVersMemoryBitmap(nbtmap);
                        ArrondirCoins(nbtmap, coin, coulTransp);
                    }
                    return nbtmap;*/
                }
                else return Rencoder(nbtmap, btmp.RawFormat.Guid, qualité);
            }
            else
            {
                if (coulTransp != 0 || coin > 0)
                {
                    return BibliothèqueImage.AppliquerTransparence(btmp as Bitmap, btmp.RawFormat.Guid, coin, ref coulTransp);
                    /*if (coulTransp != 0) btmp = FaireTransparent(btmp, btmp.RawFormat.Guid, coulTransp);
                    if (coin > 0)
                    {
                        btmp = ImageVersMemoryBitmap(btmp);
                        ArrondirCoins(btmp, coin, coulTransp);
                    }
                    return btmp;*/
                }
                else return btmp;
            }
        }



        public Image RecadreCheckDicoImage(Bitmap btmp, Int64 qualité, uint coulTransp, ushort coin, string file, int x, int y, int w, int h)
        {
            Guid gformat = btmp.RawFormat.Guid;
            return CheckDicoImage(Recadrer(btmp, x, y, w, h, qualité, coulTransp, coin), gformat, (byte)qualité, coin, coulTransp, file, x, y);
        }

        public List<Image> LoadSImages(string path, string file, XmlNode paq)
        {
            XmlAttributeCollection att = paq.Attributes;

            string strNb = att?.GetNamedItem("nb")?.Value;
            string strNbc = att?.GetNamedItem("nbc")?.Value;
            string strNbl = att?.GetNamedItem("nbl")?.Value;

            /*Color? trans;
            if (att?.GetNamedItem("opaque") != null) trans = null;
            else trans = Color.Transparent;*/

            int nbc;
            if (strNbc == null || int.TryParse(strNbc, out nbc) == false) nbc = 1;
            int nbl;
            if (strNbl == null || int.TryParse(strNbl, out nbl) == false) nbl = 1;
            int nb;
            if (strNb == null || int.TryParse(strNb, out nb) == false) nb = nbc * nbl;

            if (nb == 0 || nbc == 0 || nbl == 0) return null;
            else if (nbc == 1 && nbl == 1)
            {
                Image simg = ChargerSImage(path, file, paq, "ix", "iy", "iw", "ih");
                if (simg != null)
                {
                    List<Image> res = new List<Image>();
                    res.Add(simg);
                    return res;
                }
                else return null;
            }
            else
            {
                Bitmap img = Bitmap.FromFile(path + file) as Bitmap; // LoadImage(path, file);

                Int64 qualité;
                ushort coin;
                uint alphaCoul;
                (qualité, coin, alphaCoul) = ExtraireQualitéArrondiCoinAlphaCoul(img, paq);
                if (att?.GetNamedItem("opaque") != null) alphaCoul = 0;

                List<Image> res = new List<Image>();

                float w = ((float)img.Width) / nbc;
                float h = ((float)img.Height) / nbl;
                float x = 0.0f;
                float y = 0.0f;
                for (int i = 0; i < nb; ++i)
                {
                    res.Add(RecadreCheckDicoImage(img, qualité, alphaCoul, coin, file, (int)x, (int)y, (int)w, (int)h));

                    x += w;
                    if (((int)(x + 0.5f)) >= img.Width)
                    {
                        x = 0.0f;
                        y += h;
                        if (((int)(y + 0.5f)) >= img.Height) y = 0.0f;
                    }
                }

                return res;
            }
        }

        public List<KeyValuePair<Image, Image>> ChargerCartesImage(string path, XmlNodeList lpaq, Image PileVide)
        {
            Image dos = null;
            List<KeyValuePair<Image, Image>> imgs = new List<KeyValuePair<Image, Image>>();

            foreach (XmlNode n in lpaq)
            {
                string nomItem = n.Name.ToUpper().Trim();
                if (nomItem == "DOS_CARTE")
                {
                    string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                    if (String.IsNullOrWhiteSpace(fileImg) == false)
                    {
                        dos = ChargerSImage(path, fileImg, n, "dx", "dy", "w", "h");
                        if (dos == PileVide) dos = null;
                    }
                }
                else if (nomItem == "CARTE")
                {
                    string fileDos = n.Attributes?.GetNamedItem("dos")?.Value;
                    Image localDos;
                    if (fileDos != null)
                    {
                        if (String.IsNullOrWhiteSpace(fileDos) == false)
                        {
                            localDos = ChargerSImage(path, fileDos, n, "dx", "dy", "w", "h");
                            //if (localDos == PileVide) localDos = null;
                        }
                        else localDos = null;
                    }
                    else localDos = dos;

                    string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                    if (String.IsNullOrWhiteSpace(fileImg) == false)
                    {
                        Image img = ChargerSImage(path, fileImg, n, "ix", "iy", "w", "h");
                        if (img != null)
                        {
                            int qt = 1;
                            string strQt = n.Attributes?.GetNamedItem("quantite")?.Value;
                            if (strQt == null || !int.TryParse(strQt, out qt)) qt = 1;
                            for (int i = 0; i < qt; ++i) imgs.Add(new KeyValuePair<Image, Image>((PileVide == null ? localDos ?? img : localDos), img));
                        }
                    }
                }
                else if (nomItem == "CARTE_RV")
                {
                    string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                    if (String.IsNullOrWhiteSpace(fileImg) == false)
                    {
                        KeyValuePair<Image, Image> kvImgs = ChargerSImageRV(path, fileImg, n, "ix", "iy", "w", "h");
                        if (kvImgs.Key != null && kvImgs.Value != null)
                        {
                            int qt = 1;
                            string strQt = n.Attributes?.GetNamedItem("quantite")?.Value;
                            if (strQt == null || !int.TryParse(strQt, out qt)) qt = 1;
                            for (int i = 0; i < qt; ++i) imgs.Add(kvImgs);
                        }
                    }
                }
                else
                {
                    List<KeyValuePair<Image, Image>> simgs;

                    if (nomItem == "CARTES")
                    {
                        string fileDos = n.Attributes?.GetNamedItem("dos")?.Value;
                        Image localDos;
                        if (fileDos != null)
                        {
                            if (String.IsNullOrWhiteSpace(fileDos) == false)
                            {
                                localDos = ChargerSImage(path, fileDos, n, "dx", "dy", "w", "h");
                                if (localDos == PileVide) localDos = null;
                            }
                            else localDos = null;
                        }
                        else localDos = dos;

                        string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                        if (String.IsNullOrWhiteSpace(fileImg) == false)
                        {
                            simgs = LoadSImages(path, fileImg, n).Select(img => new KeyValuePair<Image, Image>((PileVide == null ? localDos ?? img : localDos), img)).ToList();
                        }
                        else simgs = null;
                    }
                    else if (nomItem == "CARTES_RV")
                    {
                        string fileImgVerso = n.Attributes?.GetNamedItem("verso")?.Value; // Dos
                        string fileImgRecto = n.Attributes?.GetNamedItem("recto")?.Value;
                        if (String.IsNullOrWhiteSpace(fileImgRecto) == false && String.IsNullOrWhiteSpace(fileImgVerso) == false)
                        {
                            List<Image> lstImgsV = LoadSImages(path, fileImgVerso, n); // Dos
                            List<Image> lstImgsR = LoadSImages(path, fileImgRecto, n);
                            simgs = lstImgsR.Zip(lstImgsV, (R, V) => new KeyValuePair<Image, Image>(R, V)).ToList();
                        }
                        else simgs = null;
                    }
                    else simgs = null;

                    if (simgs != null && simgs.Any())
                    {
                        XmlNodeList quantNd = n.ChildNodes;
                        if (quantNd != null && quantNd.Count > 0)
                        {
                            int[] quantiteImages = new int[simgs.Count];
                            for (int i = 0; i < quantiteImages.Length; ++i)
                                quantiteImages[i] = 1;
                            foreach (XmlNode qn in quantNd)
                            {
                                if (qn.Name.ToUpper().Trim() == "QUANTITE")
                                {
                                    int id = int.Parse(qn.Attributes?.GetNamedItem("id").Value);
                                    int qt = int.Parse(qn.Attributes?.GetNamedItem("qt").Value);
                                    if (0 <= id && id < quantiteImages.Length && qt < 1000)
                                        quantiteImages[id] = qt;
                                    else throw new Exception("Erreur de quantification des cartes.");
                                }
                            }
                            for (int i = 0; i < quantiteImages.Length; ++i)
                            {
                                for (int j = 0; j < quantiteImages[i]; ++j)
                                    imgs.Add(simgs[i]);
                            }
                        }
                        else imgs.AddRange(simgs);
                    }
                }
            }


            return imgs;
        }

        public List<Image> ChargerFacesImage(string path, XmlNodeList lpaq)
        {
            Image dos = null;
            List<Image> imgs = new List<Image>();

            foreach (XmlNode n in lpaq)
            {
                string nomItem = n.Name.ToUpper().Trim();
                if (nomItem == "FACE")
                {
                    string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                    if (String.IsNullOrWhiteSpace(fileImg) == false)
                    {
                        Image img = ChargerSImage(path, fileImg, n, "ix", "iy", "w", "h");
                        if (img != null)
                        {
                            int qt = 1;
                            string strQt = n.Attributes?.GetNamedItem("quantite")?.Value;
                            if (strQt == null || !int.TryParse(strQt, out qt)) qt = 1;
                            for (int i = 0; i < qt; ++i) imgs.Add(img);
                        }
                    }
                }
                else
                {
                    List<Image> simgs;

                    if (nomItem == "FACES")
                    {
                        string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                        if (String.IsNullOrWhiteSpace(fileImg) == false)
                        {
                            simgs = LoadSImages(path, fileImg, n);
                        }
                        else simgs = null;
                    }
                    else simgs = null;

                    if (simgs != null && simgs.Any())
                    {
                        XmlNodeList quantNd = n.ChildNodes;
                        if (quantNd != null && quantNd.Count > 0)
                        {
                            int[] quantiteImages = new int[simgs.Count];
                            for (int i = 0; i < quantiteImages.Length; ++i)
                                quantiteImages[i] = 1;
                            foreach (XmlNode qn in quantNd)
                            {
                                if (qn.Name.ToUpper().Trim() == "QUANTITE")
                                {
                                    int id = int.Parse(qn.Attributes?.GetNamedItem("id").Value);
                                    int qt = int.Parse(qn.Attributes?.GetNamedItem("qt").Value);
                                    if (0 <= id && id < quantiteImages.Length && qt < 1000)
                                        quantiteImages[id] = qt;
                                    else throw new Exception("Erreur de quantification des cartes.");
                                }
                            }
                            for (int i = 0; i < quantiteImages.Length; ++i)
                            {
                                for (int j = 0; j < quantiteImages[i]; ++j)
                                    imgs.Add(simgs[i]);
                            }
                        }
                        else imgs.AddRange(simgs);
                    }
                }
            }


            return imgs;
        }

        public Image RécupérerOuCréerImage(string sig)
        {
            if (sig.Length < 28) return null;

            (Image, Guid, byte, ushort, uint) imgFrm;
            lock (DImgs)
            {
                if (DImgs.TryGetValue(sig, out imgFrm)) return imgFrm.Item1;
            }

            Image image;
            // Sinon, on génère une image de la taille w, h extrait de la signature
            int w, h;
            SigExImageInfo(sig, out _, out w, out h);
            if (0 < w && w < OutilsRéseau.NB_WPixel_MAX && 0 < h && h < OutilsRéseau.NB_HPixel_MAX) // Image trop grande ?
                image = InitImageVide(w, h);
            else image = InitErrorImage(20, 20);
            image.Tag = sig;
            lock (LstImgsInco) { LstImgsInco.Add(sig);}
            lock (DImgs) { DImgs[sig] = (image, ImageFormat.MemoryBmp.Guid, 100, 0, 0); }
            return image;
        }

        public bool NouvelleVersion(Image img, Guid gformat, byte qualité, ushort coin, uint alpahCoul)
        {
            if (img == null) return false;
            string sig = img.Tag as string;
            if (sig == null || sig.Length < 28) return false;

            int w, h;
            SigExImageInfo(sig, out _, out w, out h);
            if (img.Width != w || img.Height != h) return false;

            (Image, Guid, byte, ushort, uint) orImg;

            lock (LstImgsInco) { LstImgsInco.Remove(sig);}

            lock (DImgs)
            {
                if (DImgs.TryGetValue(sig, out orImg))
                {
                    if (img.Width == orImg.Item1.Width && img.Height == orImg.Item1.Height)
                    {
                        //lock (orImg) { Graphics.FromImage(orImg).DrawImage(img, img.Rect(), img.Rect(), GraphicsUnit.Pixel); }
                        DImgs[sig] = (img, gformat, qualité, coin, alpahCoul);
                        return true;
                    }
                    else return false;
                }
                else
                {
                    DImgs[sig] = (img, gformat, qualité, coin, alpahCoul);
                    return false;
                }
            }
        }

        //public string PremierModelInconnue { get { lock (DImgsInco) { return DImgsInco.Keys.FirstOrDefault(); } } }
        public SortedSet<string> ImageInconnues
        {
            get
            {
                SortedSet<string> res;
                lock (LstImgsInco){ res = new SortedSet<string>(LstImgsInco); }
                return res;
            }
        }

        public void Netoyer()
        {
            lock (DImgs)
            {
                DImgs.Clear();
            }
            lock (LstImgsInco)
            {
                LstImgsInco.Clear();
            }
        }
    }
}
