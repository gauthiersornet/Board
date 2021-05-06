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
        static private Image initEmptyImage(int w, int h)
        {
            Image img = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(img);
            Pen p = new Pen(Color.Red, 5);
            Brush b = new SolidBrush(Color.Black);
            g.FillRectangle(b, 0, 0, w, h);
            g.DrawRectangle(p, 0, 0, w, h);
            return img;
        }

        static private Image initErrorImage(int w, int h)
        {
            Image img = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(img);
            Pen p = new Pen(Color.Red, 5);
            Brush b = new SolidBrush(Color.Black);
            g.FillRectangle(b, 0, 0, w, h);
            g.DrawRectangle(p, 0, 0, w, h);
            g.DrawLine(p, 0, 0, w, h);
            g.DrawLine(p, 0, w, 0, h);
            return img;
        }

        static public readonly Image EmptyImage = initEmptyImage(20, 20);
        private Dictionary<string, Image> DImgs = new Dictionary<string, Image>();
        private Dictionary<string, Image> DImgsInco = new Dictionary<string, Image>();//images inconnues

        static public MemoryStream ImageVersJPEG(Image img)
        {
            MemoryStream memStream = new MemoryStream();
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

        static public string ImageInfoToSig(byte[] bMd5Hash, int w, int h, string fileName)
        {
            byte[] bsig = new byte[bMd5Hash.Length + 5];
            bsig[0] = (byte)((w >> 0) & 0xFF);
            bsig[1] = (byte)((w >> 8) & 0xFF);
            bsig[2] = (byte)((w >> 16) & 0x0F);
            bsig[2] |= (byte)(((h >> 16) & 0x0F) << 4);
            bsig[3] = (byte)((h >> 8) & 0xFF);
            bsig[4] = (byte)((h >> 0) & 0xFF);
            Array.Copy(bMd5Hash, 0, bsig, 5, bMd5Hash.Length);
            return Convert.ToBase64String(bsig) + fileName;
        }

        static public string GetImageSig(string fileName, Bitmap btmp, int x, int y)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] block = new byte[4 * btmp.Width * btmp.Height];
                System.Runtime.InteropServices.Marshal.Copy(btdt.Scan0, block, 0, block.Length);
                md5Hash.TransformFinalBlock(block, 0, block.Length);
                btmp.UnlockBits(btdt);
                return ImageInfoToSig(md5Hash.Hash, btmp.Width, btmp.Height, fileName);
            }
        }

        public Image CheckDicoImage(Bitmap img, string file, int x, int y)
        {
            string fileSig = GetImageSig(file, img, x, y);
            if (DImgs.ContainsKey(fileSig)) return DImgs[fileSig];
            else
            {
                img.Tag = fileSig;
                DImgs.Add(fileSig, img);
                return img;
            }
        }

        public Image ChargerImage(string path, string file)
        {
            try
            {
                if (file != null && file != "")
                {
                    return CheckDicoImage(Bitmap.FromFile(path + file) as Bitmap, file, 0, 0);
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

            if (x >= btmp.Width || y >= btmp.Height || w <= 0 || h <= 0) return default;
            else return RecadreCheckDicoImage(btmp, file, x, y, w, h);
        }

        public Bitmap Recadrer(Bitmap btmp, int x, int y, int w, int h)
        {
            if (x > 0 || y > 0 || w < btmp.Width || h < btmp.Height)
            {
                Bitmap nbtmap = new Bitmap(w, h);
                Graphics.FromImage(nbtmap).DrawImage(btmp, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                return nbtmap;
            }
            else return btmp;
        }

        public Image RecadreCheckDicoImage(Bitmap btmp, string file, int x, int y, int w, int h)
        {
            return CheckDicoImage(Recadrer(btmp, x, y, w, h), file, x, y);
        }

        public List<Image> LoadSImages(string path, string file, XmlNode paq)
        {
            XmlAttributeCollection att = paq.Attributes;

            string strNb = att?.GetNamedItem("nb")?.Value;
            string strNbc = att?.GetNamedItem("nbc")?.Value;
            string strNbl = att?.GetNamedItem("nbl")?.Value;

            int nbc;
            if (strNbc == null || int.TryParse(strNbc, out nbc) == false) nbc = 1;
            int nbl;
            if (strNbl == null || int.TryParse(strNbl, out nbl) == false) nbl = 1;
            int nb;
            if (strNb == null || int.TryParse(strNb, out nb) == false) nb = nbc * nbl;

            if (nb == 0 || nbc == 0 || nbl == 0) return null;
            else if (nb == 1)
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
                List<Image> res = new List<Image>();

                float w = ((float)img.Width) / nbc;
                float h = ((float)img.Height) / nbl;
                float x = 0.0f;
                float y = 0.0f;
                for (int i = 0; i < nb; ++i)
                {
                    res.Add(RecadreCheckDicoImage(img, file, (int)x, (int)y, (int)w, (int)h));

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

        public List<KeyValuePair<Image, Image>> ChargerCartesImage(string path, XmlNodeList lpaq, Image dos = null)
        {
            List<KeyValuePair<Image, Image>> imgs = new List<KeyValuePair<Image, Image>>();

            foreach (XmlNode n in lpaq)
                if (n.Name.ToUpper().Trim() == "DOS_CARTE")
                {
                    string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                    if (String.IsNullOrWhiteSpace(fileImg) == false)
                    {
                        dos = ChargerSImage(path, fileImg, n, "x", "y", "w", "h");
                    }
                }
                else if (n.Name.ToUpper().Trim() == "CARTE")
                {
                    string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                    if (String.IsNullOrWhiteSpace(fileImg) == false)
                    {
                        Image img = ChargerSImage(path, fileImg, n, "x", "y", "w", "h");
                        if (img != null) imgs.Add(new KeyValuePair<Image, Image>(dos, img));
                    }
                }
                else if (n.Name.ToUpper().Trim() == "CARTES")
                {
                    string fileDos = n.Attributes?.GetNamedItem("dos")?.Value;
                    Image localDos;
                    if (fileDos != null)
                    {
                        if (String.IsNullOrWhiteSpace(fileDos) == false)
                        {
                            localDos = ChargerSImage(path, fileDos, n, "x", "y", "w", "h");
                        }
                        else localDos = null;
                    }
                    else localDos = dos;

                    string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                    if (String.IsNullOrWhiteSpace(fileImg) == false)
                    {
                        List<KeyValuePair<Image, Image>> simgs = LoadSImages(path, fileImg, n).Select(img => new KeyValuePair<Image, Image>(localDos ?? img, img)).ToList();
                        if (simgs != null && simgs.Count > 0)
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
            if (DImgs.ContainsKey(sig)) return DImgs[sig];
            else if(DImgsInco.ContainsKey(sig)) return DImgsInco[sig];
            else // Sinon, on génère une image de la taille w, h extrait de la signature
            {
                int w, h;
                SigExImageInfo(sig, out _, out w, out h);
                Image img;
                if (w*h > OutilsRéseau.NB_OCTET_MAX) // Image trop grande ?
                    img = initErrorImage(20, 20);
                else img = initErrorImage(w, h);
                DImgsInco.Add(sig, img);
                return img;
            }
        }

        public bool NouvelleVersion(string sig, Image img)
        {
            Image orImg;
            if(DImgsInco.ContainsKey(sig))
            {
                orImg = DImgsInco[sig];
                DImgsInco.Remove(sig);
            }
            else if(DImgs.ContainsKey(sig)) orImg = DImgs[sig];
            else
            {
                DImgs[sig] = img;
                return true;
            }

            if (img.Width == orImg.Width && img.Height == orImg.Height)
            {
                Graphics.FromImage(orImg).DrawImage(img, img.Rect(), img.Rect(), GraphicsUnit.Pixel);
                DImgs[sig] = orImg;
                return true;
            }
            else return false;
        }

        public string PremierModelInconnue { get => DImgsInco.Keys.FirstOrDefault(); }
        public List<string> ModelInconnues { get => DImgsInco.Keys.ToList(); }

        public void Netoyer()
        {
            DImgs.Clear();
            DImgsInco.Clear();
        }
    }
}
