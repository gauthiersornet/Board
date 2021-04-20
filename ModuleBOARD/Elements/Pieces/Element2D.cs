using ModuleBOARD.Elements.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ModuleBOARD.Elements.Pieces
{
    public class Element2D : Element
    {
        //public SImage SImg = default;
        public Image ElmImage = default;

        public override PointF Size { get => GC.ProjSize(ElmImage.Rect()); }

        public Element2D()
        {
        }

        public Element2D(Element2D elm)
            :base(elm)
        {
            ElmImage = elm.ElmImage;
        }

        public Element2D(string path, string file, PointF p, BibliothèqueImage bibliothèqueImage)
        {
            GC.P = p;
            ElmImage = bibliothèqueImage.LoadImage(path, file);
        }

        public Element2D(string path, XmlNode paq, PointF p, BibliothèqueImage bibliothèqueImage)
        {
            Load(paq);
            GC.P.X += p.X;
            GC.P.Y += p.Y;
            string fileImg = paq.Attributes.GetNamedItem("img")?.Value;
            ElmImage = bibliothèqueImage.LoadSImage(path, fileImg, paq, "ix", "iy", "iw", "ih");
        }

        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {
            GeoCoord2D gc = GC;
            gc.P.X += p.X;
            gc.P.Y += p.Y;
            /*p.X += GC.P.X;
            p.Y += GC.P.Y;*/
            //Matrix m = g.Transform;
            ElmImage.Dessiner(vue, g, gc);
            //g.Transform = m;
        }

        public bool IsValid() { return ElmImage != null; }

        public override void Retourner()
        {
        }

        public override void Cacher()
        {
        }

        public override void Révéler()
        {
        }

        public override bool Roulette(int delta)
        {
            return false;
        }

        public override bool PutOnTop(Element elm)
        {
            return (this == elm);
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj)) return true;
            else if (obj is Element2D)
            {
                Element2D elm = obj as Element2D;
                return GC.E == elm.GC.E && BibliothèqueImage.Equals(ElmImage, elm.ElmImage);
            }
            else return false;
        }

        public override object Clone()
        {
            return new Element2D(this);
        }

    }
}
