using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModuleBOARD.Elements.Base
{
    public class Model2_5D
    {
        public string Tag = null;
        private Image[] imagesDessus;
        private Image[] imagesDessous;

        public static Model2_5D CréerModelVide(string sig)
        {
            Model2_5D mod = new Model2_5D();
            mod.Tag = sig;
            mod.imagesDessus = mod.imagesDessous = new Image[8];
            for (int i = 0; i < mod.imagesDessus.Length; ++i)
                mod.imagesDessus[i] = BibliothèqueImage.EmptyImage;
            return mod;
        }

        static public Model2_5D ChargerModel2_5D(string path, XmlNode paq, BibliothèqueImage bibliothèqueImage)
        {
            string fileImg = paq.Attributes?.GetNamedItem("img")?.Value;
            if (fileImg != null)
            {
                MD5 md5Hash = MD5.Create();
                Model2_5D modl = new Model2_5D();
                string strNbl = paq.Attributes?.GetNamedItem("nbl")?.Value;
                List<Image> Images = bibliothèqueImage.LoadSImages(path, fileImg, paq);
                int accw = 0;
                int acch = 0;
                foreach (Image img in Images)
                {
                    Bitmap btmap = img as Bitmap;
                    string sig = btmap.Tag as string;
                    if (sig != null && sig.Length >= 28)
                    {
                        byte[] bs = Convert.FromBase64String(sig.Substring(0, 28));
                        md5Hash.TransformBlock(bs, 0, 21, bs, 0);
                    }
                    if (img != null)
                    {
                        accw += img.Width;
                        acch += img.Height;
                        if (img.Width > 0 && img.Height > 0) btmap.MakeTransparent();//btmap.GetPixel(0, 0)
                    }
                }
                md5Hash.TransformFinalBlock(new byte[0], 0, 0);
                if (strNbl == "1")
                {
                    modl.imagesDessus = Images.Select(img => img).ToArray();
                }
                else if (strNbl == "2" && (Images.Count % 2) == 0)
                {
                    accw /= 2;
                    int nbi = Images.Count / 2;
                    modl.imagesDessus = Images.Take(nbi).Select(img => img).ToArray();
                    modl.imagesDessous = Images.Skip(nbi).Select(img => img).Take(nbi).ToArray();
                }
                modl.Tag = BibliothèqueImage.ImageInfoToSig(md5Hash.Hash, accw, acch, fileImg);
                return modl;
            }
            else return null;
        }

        public Model2_5D()
        {
        }

        public Model2_5D(string path, XmlNode paq, BibliothèqueImage bibliothèqueImage)
        {
            string fileImg = paq.Attributes?.GetNamedItem("img")?.Value;
            if (fileImg != null)
            {
                string strNbl = paq.Attributes?.GetNamedItem("nbl")?.Value;
                List<Image> Images = bibliothèqueImage.LoadSImages(path, fileImg, paq);
                foreach (Image img in Images)
                {
                    if(img != null && img.Width>0 && img.Height>0)
                    {
                        Bitmap btmap = img as Bitmap;
                        btmap.MakeTransparent();//btmap.GetPixel(0, 0)
                    }
                }
                if (strNbl == "1")
                {
                    imagesDessus = Images.Select(img => img).ToArray();
                }
                else if(strNbl == "2" && (Images.Count % 2) == 0)
                {
                    int nbi = Images.Count / 2;
                    imagesDessus = Images.Take(nbi).Select(img => img).ToArray();
                    imagesDessous = Images.Skip(nbi).Select(img => img).Take(nbi).ToArray();
                }
            }
        }

        public void Copier(Model2_5D mod)
        {
            Tag = mod.Tag;
            imagesDessus = mod.imagesDessus;
            imagesDessous = mod.imagesDessous;
        }

        static public int AngToIdx(float ang) { return (8 + (int)((ang - 45.0f / 2) / 45.0f + 0.5f)) % 8; }

        public bool AImageDessus => imagesDessus != null && imagesDessus.Length > 0;
        public bool AImageDessous => imagesDessous != null && imagesDessous.Length > 0;

        private PointF SizeStrict(int idx, bool à_l_endroit)
        {
            Image[] images;
            if (à_l_endroit) images = imagesDessus ?? imagesDessous;
            else images = imagesDessous ?? imagesDessus;
            if (images == null) return PointF.Empty;
            else
            {
                if(idx < images.Length && images[idx] != null) return new PointF(images[idx].Width, images[idx].Height);
                else return PointF.Empty;
            }
        }

        private PointF SizeSAng(int idx, bool à_l_endroit)
        {
            PointF res = SizeStrict(idx, à_l_endroit);
            if(res.IsEmpty)
            {
                int idxSym = (0 <= idx && idx <= 4) ? 2 : 6;
                int nidx = idxSym - (idx - idxSym);
                res = SizeStrict(nidx, à_l_endroit);
            }
            return res;
        }

        public PointF Size(float ang, bool à_l_endroit)
        {
            int idx = AngToIdx(ang);
            PointF res = SizeSAng(idx, à_l_endroit);
            if (res.IsEmpty)res = SizeSAng(idx, !à_l_endroit);
            return res;
        }



        private Image ObtenirImageStrict(int idx, bool à_l_endroit)
        {
            Image[] images;
            if (à_l_endroit) images = imagesDessus;
            else images = imagesDessous;
            if (images == null) return null;
            else
            {
                if (idx < images.Length && images[idx] != null) return images[idx];
                else return null;
            }
        }

        private Image ObtenirImageSAng(int idx, bool à_l_endroit, out bool mirrorX)
        {
            Image res = ObtenirImageStrict(idx, à_l_endroit);
            if (res == null)
            {
                int idxSym = (0 <= idx && idx <= 4) ? 2 : 6;
                int nidx = idxSym - (idx - idxSym);
                res = ObtenirImageStrict(nidx, à_l_endroit);
                mirrorX = true;
            }
            else mirrorX = false;
            return res;
        }

        public Image ObtenirImage(float ang, bool à_l_endroit, out bool mirrorX, out bool mirrorY)
        {
            int idx = AngToIdx(ang);
            Image res = ObtenirImageSAng(idx, à_l_endroit, out mirrorX);
            if (res == null)
            {
                res = ObtenirImageSAng(idx, !à_l_endroit, out mirrorX);
                mirrorY = true;
            }
            else mirrorY = false;
            return res;
        }

        public override bool Equals(object obj)
        {
            if (base.Equals(obj)) return true;
            else if (obj is Model2_5D)
            {
                Model2_5D mod = obj as Model2_5D;
                if (imagesDessus != mod.imagesDessus)
                {
                    if (imagesDessus != null && mod.imagesDessus != null && imagesDessus.Length == mod.imagesDessus.Length)
                    {
                        for (int i = 0; i < imagesDessus.Length; ++i)
                            if (imagesDessus[i] != mod.imagesDessus[i]) return false;
                    }
                    else return false;
                }
                if (imagesDessous != mod.imagesDessous)
                {
                    if (imagesDessous != null && mod.imagesDessous != null && imagesDessous.Length == mod.imagesDessous.Length)
                    {
                        for (int i = 0; i < imagesDessous.Length; ++i)
                            if (imagesDessous[i] != mod.imagesDessous[i]) return false;
                    }
                    else return false;
                }
                return true;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            long res = 0;
            if(imagesDessus!=null)
            {
                for(int i=0;i< imagesDessus.Length; ++i)
                {
                    if(imagesDessus[i] != null) res += (1+i)*(long)imagesDessus[i].GetHashCode();
                }
            }
            if (imagesDessus != null)
            {
                for (int i = 0; i < imagesDessus.Length; ++i)
                {
                    if (imagesDessus[i] != null) res -= (1 + i) * (long)imagesDessus[i].GetHashCode();
                }
            }
            if (res >= 0) return (int)(res % ((long)int.MaxValue+1));
            else return -(int)(-res % (-(long)int.MinValue + (long)1));
        }
    }
}
