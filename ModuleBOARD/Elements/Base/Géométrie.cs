using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModuleBOARD.Elements.Base
{
    public struct GeoCoord2D
    {
        public PointF P; //Position 2D
        public float E; //Echelle
        public float A; // Angle en Degré

        public GeoCoord2D(PointF p, float e, float a)
        {
            P = p;
            E = e;
            A = a;
        }

        public GeoCoord2D(float x, float y, float e, float a)
        {
            P = new PointF(x, y);
            E = e;
            A = a;
        }

        public PointF Projection(PointF p)
        {
            Matrix m = new Matrix();
            m.Rotate(A);
            return new PointF
                (
                    P.X + (p.X * m.Elements[0] + p.Y * m.Elements[1]) / E,
                    P.Y + (p.X * m.Elements[2] + p.Y * m.Elements[3]) / E
                );
            //return new PointF(P.X + (p.X / E), P.Y + (p.Y / E));
        }

        public PointF ProjectionDelta(PointF d)
        {
            Matrix m = new Matrix();
            m.Rotate(A);
            return new PointF
                (
                    (d.X * m.Elements[0] + d.Y * m.Elements[1]) / E,
                    (d.X * m.Elements[2] + d.Y * m.Elements[3]) / E
                );
        }

        public PointF ProjectionInv(PointF p)
        {
            PointF pSclInv = new PointF(((p.X - P.X) * E), ((p.Y - P.Y) * E));
            Matrix m = new Matrix();
            m.Rotate(-A);
            return new PointF
                (
                    (pSclInv.X * m.Elements[0] + pSclInv.Y * m.Elements[1]),
                    (pSclInv.X * m.Elements[2] + pSclInv.Y * m.Elements[3])
                );
            //return new PointF( ((p.X - P.X) * E), ((p.Y - P.Y) * E) );
        }

        public PointF ProjectionDeltaInv(PointF d)
        {
            Matrix m = new Matrix();
            m.Rotate(-A);
            return new PointF
                (
                    (d.X * m.Elements[0] + d.Y * m.Elements[1]) * E,
                    (d.X * m.Elements[2] + d.Y * m.Elements[3]) * E
                );
        }

        public PointF ProjSize(Rectangle rect)
        {
            if (rect.Width > 0 && rect.Height > 0)
            {
                if (rect.Width <= rect.Height)
                    return new PointF(E, ((rect.Height * E) / rect.Width));
                else return new PointF(((rect.Width * E) / rect.Height), E);
            }
            else return default;
        }

        public PointF ProjSize(PointF sz)
        {
            if (sz.X > 0 && sz.Y > 0)
            {
                if (sz.X <= sz.Y)
                    return new PointF(E, ((sz.Y * E) / sz.X));
                else return new PointF(((sz.X * E) / sz.Y), E);
            }
            else return default;
        }

        public PointF ProjSize(Point psz)
        {
            if (psz.X > 0 && psz.Y > 0)
            {
                if (psz.X <= psz.Y)
                    return new PointF(E, ((psz.Y * E) / psz.X));
                else return new PointF(((psz.X * E) / psz.Y), E);
            }
            else return default;
        }
    }

    public struct GeoVue
    {
        public GeoCoord2D GC;
        public PointF DimentionD2;//demie dimention d'origine

        public PointF Dimention
        {
            get => new PointF(DimentionD2.X * 2.0f, DimentionD2.Y * 2.0f);
            set => DimentionD2 = new PointF(value.X / 2.0f, value.Y /2.0f);
        }

        public GeoVue(PointF p, float e, float a, PointF dimention)
        {
            GC = new GeoCoord2D(p, e, a);
            DimentionD2 = new PointF(dimention.X / 2.0f, dimention.Y / 2.0f);
        }

        public GeoVue(float x, float y, float e, float a, float dimX, float dimY)
        {
            GC = new GeoCoord2D(x, y, e, a);
            DimentionD2 = new PointF(dimX / 2.0f, dimY / 2.0f);
        }

        public PointF Projection(PointF p)
        {
            return GC.Projection(new PointF(p.X - DimentionD2.X, p.Y - DimentionD2.Y));
            //PointF dimProj = GC.Projection(Dimention);
            //return new PointF(P.X + (p.X / E), P.Y + (p.Y / E));
        }

        public PointF ProjectionDelta(PointF d)
        {
            return GC.ProjectionDelta(d);
        }

        public PointF ProjectionInv(PointF p)
        {
            PointF pTmp = GC.ProjectionInv(p);
            return new PointF(pTmp.X + DimentionD2.X, pTmp.Y+ DimentionD2.Y);
            //PointF dimProj = GC.ProjectionInv(Dimention);
            //return new PointF(((p.X - P.X) * E), ((p.Y - P.Y) * E));
        }

        public PointF ProjectionDeltaInv(PointF d)
        {
            return GC.ProjectionDeltaInv(d);
        }

        public PointF ProjSize(Rectangle rect)
        {
            return GC.ProjSize(rect);
        }

        public PointF ProjSize(Point psz)
        {
            return GC.ProjSize(psz);
        }
    }

    public class BibliothèqueImage
    {
        static public readonly Image EmptyImage = new Bitmap(20, 20);
        private Dictionary<string, Image> DImgs = new Dictionary<string, Image>();
        public string GetImageSig(string fileName, Bitmap btmp, int x, int y)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] block = new byte[4 * btmp.Width * btmp.Height];
                System.Runtime.InteropServices.Marshal.Copy(btdt.Scan0, block, 0, block.Length);
                md5Hash.TransformFinalBlock(block, 0, block.Length);
                btmp.UnlockBits(btdt);
                return Convert.ToBase64String(md5Hash.Hash).Substring(0, 22)+ fileName + "-" + x + "x" + y + "x" + btmp.Width.ToString() + "x" + btmp.Height.ToString();
            }
        }

        public Image CheckDicoImage(Bitmap img, string file, int x, int y)
        {
            string fileSig = GetImageSig(file, img, x, y);
            if (DImgs.ContainsKey(fileSig)) return DImgs[fileSig];
            else
            {
                DImgs.Add(fileSig, img);
                return img;
            }
        }

        public Image LoadImage(string path, string file)
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

        public Image LoadSImage(string path, string file, XmlNode paq, string kx = null, string ky = null, string kw = null, string kh = null)
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
            if(x>0 || y>0 || w<btmp.Width || h<btmp.Height)
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
                Image simg = LoadSImage(path, file, paq, "ix", "iy", "iw", "ih");
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

        public List<KeyValuePair<Image, Image>> LoadCartesImage(string path, XmlNodeList lpaq, Image dos = null)
        {
            List<KeyValuePair<Image, Image>> imgs = new List<KeyValuePair<Image, Image>>();

            foreach (XmlNode n in lpaq)
                if (n.Name.ToUpper().Trim() == "DOS_CARTE")
                {
                    string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                    if (String.IsNullOrWhiteSpace(fileImg) == false)
                    {
                        dos = LoadSImage(path, fileImg, n, "x", "y", "w", "h");
                    }
                }
                else if (n.Name.ToUpper().Trim() == "CARTE")
                {
                    string fileImg = n.Attributes?.GetNamedItem("img")?.Value;
                    if (String.IsNullOrWhiteSpace(fileImg) == false)
                    {
                        Image img = LoadSImage(path, fileImg, n, "x", "y", "w", "h");
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
                            localDos = LoadSImage(path, fileDos, n, "x", "y", "w", "h");
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
                            if(quantNd!=null && quantNd.Count > 0)
                            {
                                int[] quantiteImages = new int[simgs.Count];
                                for (int i = 0; i < quantiteImages.Length; ++i)
                                    quantiteImages[i] = 1;
                                foreach (XmlNode qn in quantNd)
                                {
                                    if(qn.Name.ToUpper().Trim() == "QUANTITE")
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
    }

    /*class Géométrie
    {
    }*/
}
