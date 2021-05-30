using ModuleBOARD.Elements.Base;
using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
        public override EType ElmType { get => EType.Element2D; }

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
            ElmImage = bibliothèqueImage.ChargerImage(path, file);
            /*System.Threading.Thread tt = new System.Threading.Thread(new System.Threading.ThreadStart(TEST));
            tt.Start();*/
        }

        /*private void TEST()
        {
            while(true)
            {
                MemoryStream stream = new MemoryStream();
                ElmImage.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }*/

        public Element2D(string path, XmlNode paq, PointF p, BibliothèqueImage bibliothèqueImage)
        {
            Load(paq);
            GC.P.X += p.X;
            GC.P.Y += p.Y;
            string fileImg = paq.Attributes.GetNamedItem("img")?.Value;
            ElmImage = bibliothèqueImage.ChargerSImage(path, fileImg, paq, "ix", "iy", "w", "h");
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

        override public object MettreAJour(object obj)
        {
            if (obj is Image)
            {
                Image img = obj as Image;
                if (ElmImage != null && String.Equals(ElmImage.Tag as string, img.Tag as string))
                    ElmImage = img;
            }
            return base.MettreAJour(obj);
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

        override public ContextMenu Menu(IBoard ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();

            cm.MenuItems.AddRange(new MenuItem[]
                    {
                        new MenuItem("Créer la pioche", new EventHandler((o,e) => {ctrl.MettreEnPioche(this); })),
                    });
            return cm;
        }

        public Element2D(Stream stream, IRessourcesDésérialiseur resscDes)
            : base(stream, resscDes)
        {
            ElmImage = resscDes.RécupérerImage(stream);
        }

        override public void Serialiser(Stream stream, ref int gidr)
        {
            base.Serialiser(stream, ref gidr);
            stream.SerialiserObject(ElmImage?.Tag.ToString() ?? "");
        }

        override public void SerialiserTout(Stream stream, ref int gidr, ISet<int> setIdRéseau)
        {
            base.SerialiserTout(stream, ref gidr, setIdRéseau);
        }
    }
}
