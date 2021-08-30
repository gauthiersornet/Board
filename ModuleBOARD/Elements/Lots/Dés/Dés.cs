using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Pieces;
using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace ModuleBOARD.Elements.Lots.Dés
{
    public class Dés : Element
    {
        static private float CroixEpaisseur = 5.0f;

        public override EType ElmType { get => EType.Dés; }
        public List<Image> Images = null;
        public int Courrante = -1;

        protected Dés() { }

        protected Dés(int idREz) : base(idREz) { }

        public Dés(Element elm) : base(elm) { }

        public Dés(Element2D element2D) : base(element2D)
        {
            Parent = null;
            Images = new List<Image>() { element2D.ElmImage };
        }

        public Dés(Element2D2F element2D2F) : base(element2D2F)
        {
            Parent = null;
            Images = new List<Image>() { element2D2F.ElmImage };
        }

        public Dés(Dés elm)
            : base(elm)
        {
            Images = (elm.Images != null ? new List<Image>(elm.Images) : null);
            Courrante = elm.Courrante;
        }

        public Dés(string path, XmlNode paq, PointF p, BibliothèqueImage bibliothèqueImage)
        {
            base.Load(paq);
            GC.P.X += p.X;
            GC.P.Y += p.Y;
            //Parent = parent;
            if (paq.Attributes?.GetNamedItem("echl") == null) GC.E = 40.0f;

            Images = bibliothèqueImage.ChargerFacesImage(path, paq.ChildNodes);
            /*if(Images != null)
            {
                GC.P.X -= Images.Count * CEpaisseur;
                GC.P.Y -= Images.Count * CEpaisseur;
            }*/

            /*string fileImg = paq.Attributes?.GetNamedItem("carte")?.Value;
            if (fileImg != null)
            {
                List<SImage> lstSImgs = SImage.LoadSImages(path, fileImg, paq);
                if(lstSImgs!=null && lstSImgs.Count > 0) Images.AddRange(lstSImgs);
            }*/

            if (paq.Attributes?.GetNamedItem("mélanger") != null) Mélanger();

            if (paq.Attributes?.GetNamedItem("caché") != null)
                Courrante = -1;
            else if (paq.Attributes?.GetNamedItem("montré") != null)
                Courrante = 0;
            else Courrante = -1;
        }

        public override PointF Size
        {
            get
            {
                Image imgref;
                if (Images != null && Images.Any()) imgref = Images.FirstOrDefault(x => x != null);
                else imgref = null;

                PointF sz;
                if (imgref != null) sz = GC.ProjSize(imgref.Rect());
                else sz = GC.ProjSize(new Rectangle(0, 0, 20, 20));
                return sz;
            }
        }

        public override object Clone(){ return new Dés(this); }

        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {
            GeoCoord2D gc = GC;
            gc.P.X += p.X;
            gc.P.Y += p.Y;

            Matrix m = g.Transform;
            if (Images != null && Images.Any())
            {
                int idx;
                if (Courrante >= 0)
                {
                    if (Courrante >= Images.Count) Courrante = 0;
                    idx = Courrante;
                }
                else
                {
                    if (Courrante < -Images.Count) Courrante = -1;
                    idx = Images.Count + Courrante;
                }
                if(Images[idx] != null) Images[idx].Dessiner(vue, g, gc);
                else new Point(20, 20).DessinerVide(vue, g, gc);
            }
            else
            {
                Matrix drawMat = (new Point(20, 20)).DessinerVide(vue, g, gc);
                g.Transform = drawMat;
                RectangleF rect = new RectangleF(0, 0, 20, 20);
                Pen pn = new Pen(Color.Black, CroixEpaisseur);
                /*g.DrawLine(pn, new PointF(rect.X, rect.Y), new PointF(rect.X + rect.Width, rect.Y + rect.Height));
                g.DrawLine(pn, new PointF(rect.X + rect.Width, rect.Y), new PointF(rect.X, rect.Y + rect.Height));*/
                g.DrawLine(pn, new PointF(-rect.Width / 2, -rect.Height / 2), new PointF(rect.Width / 2, rect.Height / 2));
                g.DrawLine(pn, new PointF(rect.Width / 2, -rect.Height / 2), new PointF(-rect.Width / 2, rect.Height / 2));
            }
            g.Transform = m;
        }

        override public object MettreAJour(object obj)
        {
            if (obj is Image)
            {
                Image img = obj as Image;
                if (Images != null)
                {
                    for (int i = 0; i < Images.Count; ++i)
                    {
                        if (Images[i] != null && String.Equals(Images[i].Tag, img.Tag))
                            Images[i] = img;
                    }
                }
                return base.MettreAJour(obj);
            }
            else return base.MettreAJour(obj);
        }

        protected void MettreAJourEtat()
        {
            if (EstDansEtat(EEtat.À_l_envers))
            {
                if (Courrante >= 0) Courrante = ~Courrante;
            }
            else
            {
                if (Courrante < 0) Courrante = ~Courrante;
            }
        }

        public override void MajEtat(EEtat nouvEtat)
        {
            if (AEtatChangé(EEtat.À_l_envers, nouvEtat))
            {
                Courrante = ~Courrante;
            }
            base.MajEtat(nouvEtat);
        }

        public override bool Roulette(int delta)
        {
            if (Images != null && Images.Count > 0)
            {
                if (Courrante < 0)
                {
                    //Courrante = ~Courrante;//On montre
                    base.Révéler();
                }
                else if (delta < 0)
                {
                    ++Courrante;
                    if (Courrante >= Images.Count)
                        Courrante = 0;
                }
                else if (delta > 0)
                {
                    --Courrante;
                    if (Courrante < 0)
                        Courrante = Images.Count - 1;
                }
            }
            return true;
        }

        override public ContextMenu Menu(IBoard ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();

            MenuItem nbc = new MenuItem((Images?.Count ?? 0) + " Faces(s)");
            int idx;
            for (idx = 0; idx < cm.MenuItems.Count && cm.MenuItems[idx].Text != "État"; ++idx) ;
            if (idx < cm.MenuItems.Count) cm.MenuItems[idx].MenuItems.Add(0, nbc);
            else cm.MenuItems.Add(new MenuItem("État", new MenuItem[1] { nbc }));

            cm.MenuItems.AddRange(new MenuItem[]
                    {
                        new MenuItem("Mélanger", new EventHandler((o,e) => { ctrl.Mélanger(this);/*Mélanger(); ctrl.Refresh();*/ }))
                    });
            return cm;
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            return true;
        }

        public override bool PutOnTop(Element elm)
        {
            return (this == elm);
        }

        private void VérifierCourrante()
        {
            if (Images != null && Images.Any())
            {
                if (Courrante < 0)
                {
                    if (Courrante < -Images.Count) Courrante = -Images.Count;
                }
                else
                {
                    if (Courrante >= Images.Count) Courrante = Images.Count - 1;
                }
            }
        }

        public void Mélanger(Random rnd = null)
        {
            if (Images != null && Images.Any())
            {
                if (rnd == null) rnd = new Random();
                Courrante = rnd.Next(Images.Count);
            }
        }

        public Dés(Stream stream, IRessourcesDésérialiseur resscDes)
            : base(stream, resscDes)
        {
            Courrante = stream.ReadInt();
            ushort nbc = BitConverter.ToUInt16(stream.GetBytes(2), 0);
            if (nbc > 0)
            {
                Images = new List<Image>(nbc);
                for (ushort i = 0; i < nbc; ++i)
                    Images.Add(resscDes.RécupérerImage(stream));
            }
            else Images = null;
        }

        override public void Serialiser(Stream stream, ref int gidr)
        {
            base.Serialiser(stream, ref gidr);
            stream.WriteBytes(BitConverter.GetBytes(Courrante));
            ushort nbc = (ushort)(Images?.Count ?? 0);
            stream.WriteBytes(BitConverter.GetBytes(nbc));
            for (ushort i = 0; i < nbc; ++i)
            {
                stream.SerialiserObject(Images[i]?.Tag as string ?? "", typeof(string));
            }
        }
    }
}
