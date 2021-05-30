using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Pieces;
using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace ModuleBOARD.Elements.Lots.Piles
{
    public class Défausse : Pile
    {
        public override EType ElmType { get => EType.Défausse; }

        public Pioche Pioche = null;
        private Dictionary<KeyValuePair<Image, Image>, Pioche> dicoParents = null;

        Défausse() { }

        public Défausse(int idREz) : base(idREz) { }

        public Défausse(Pioche pioch) : base(pioch as Element)
        {
            /* if (pioch.PileVide != null) PileVide = pioch.PileVide;
             else if (pioch.Images != null)
             {
                 KeyValuePair<Image, Image> crt = pioch.Images.FirstOrDefault(x => x.Key != null || x.Value != null);
                 Image img = crt.Key ?? crt.Value;
                 if (img != null) PileVide = BibliothèqueImage.InitImageVide(img.Width, img.Height);
                 else PileVide = null;
             }
             else PileVide = null;*/
            PileVide = pioch.TrouverUneImage();
            if (PileVide == null) PileVide = BibliothèqueImage.ImageVide;

            PointF sz;
            if (PileVide == null) sz = GC.ProjSize(new PointF(20.0f, 20.0f));
            //else if (PileVide.Width > PileVide.Height) GC.P.Y += PileVide.Height + 10;
            else sz = GC.ProjSize(new PointF(PileVide.Width, PileVide.Height)); ;

            GC.P.X += sz.X + 5;
            MettreAJourEtat();
        }

        public Défausse(Défausse elm)
            : base(elm)
        {
            Pioche = elm.Pioche;
            if (elm.dicoParents != null) dicoParents = new Dictionary<KeyValuePair<Image, Image>, Pioche>(elm.dicoParents);
            else dicoParents = null;
        }

        public Défausse(string path, XmlNode paq, PointF p, BibliothèqueImage bibliothèqueImage)
            : base(path, paq, p, bibliothèqueImage)
        {
        }

        override public object MettreAJour(object obj)
        {
            if (Pioche != null && obj is Pioche && (obj as Pioche).IdentifiantRéseau == Pioche.IdentifiantRéseau)
                Pioche = obj as Pioche;
            return base.MettreAJour(obj);
        }

        public void ReMettreDansLaPioche()
        {
            if(Pioche != null)
            {
                List<KeyValuePair<Image, Image>> imgs = Images;
                Images = null;
                Pioche.AddRange(imgs);
            }
            else if (dicoParents != null)
            {
                for(int i = 0;i < Images.Count; ++i)
                {
                    if(dicoParents.ContainsKey(Images[i]))
                    {
                        Pioche prt = dicoParents[Images[i]] as Pioche;
                        if(prt != null)
                        {
                            prt.AddBack(Images[i]);
                            Images[i] = new KeyValuePair<Image, Image>(null, null);
                        }
                    }
                }
                Images = Images.Where(img => (img.Key != null || img.Value != null)).ToList();
                if (Images.Any() == false) Images = null;
                dicoParents = null;
            }
        }

        override public ContextMenu Menu(IBoard ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();
            if(Pioche!=null || dicoParents != null)
                cm.MenuItems.Add("Remettre dans la pioche", new EventHandler((o, e) => { ctrl.ReMettreDansPioche(this); }));
            return cm;
        }

        public override Element ElementLaché(Element elm)
        {
            Element res = base.ElementLaché(elm);
            if (res == null && Pioche == null && elm is Element2D && elm.Parent is Pioche)
            {
                if (dicoParents == null) dicoParents = new Dictionary<KeyValuePair<Image, Image>, Pioche>();
                if(elm is Element2D2F)
                    dicoParents.Add(new KeyValuePair<Image, Image>((elm as Element2D2F).Dos, (elm as Element2D).ElmImage), elm.Parent as Pioche);
                else dicoParents.Add(new KeyValuePair<Image, Image>((elm as Element2D).ElmImage, (elm as Element2D).ElmImage), elm.Parent as Pioche);
            }
            return res;
        }

        //public override Element MousePiocheAt(PointF mp, float angle)
        public override Element MousePioche(int index = int.MaxValue)
        {
            //Element elm = base.MousePiocheAt(mp, angle);
            Element elm = base.MousePioche(index);
            if (elm != null)
            {
                if (Pioche != null) elm.Parent = Pioche;
                if (dicoParents != null && elm is Element2D2F)
                {
                    //Element2D2F elm2D2F = elm as Element2D2F;
                    Element2D2F elm2D2F = elm as Element2D2F;
                    KeyValuePair<Image, Image>  kv = new KeyValuePair<Image, Image>(elm2D2F.Dos, elm2D2F.ElmImage);
                    if (dicoParents.ContainsKey(kv))
                    {
                        Pioche prt = dicoParents[kv] as Pioche;
                        if (prt != null) elm2D2F.Parent = prt;
                        if (Images == null || Images.Contains(kv) == false)
                            dicoParents.Remove(kv);
                    }
                }
            }
            return elm;
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            XmlAttributeCollection atts = paq.Attributes;
            if (atts != null)
            {
                string nomPioche = atts.GetNamedItem("pioche")?.Value;
                if (dElements != null && nomPioche != null && dElements.ContainsKey(nomPioche))
                {
                    Pioche = dElements[nomPioche] as Pioche;
                }
            }
            return true;
        }

        public void SuppressionPioche(Pioche p)
        {
            if (Pioche == p) Pioche = null;
            if(dicoParents!=null)
            {
                bool still;
                do
                {
                    still = false;
                    foreach (var kv in dicoParents)
                    {
                        if (Object.ReferenceEquals(kv.Value, p))
                        {
                            dicoParents.Remove(kv.Key);
                            still = true;
                            break;
                        }
                    }
                } while (still);
                if (dicoParents.Any() == false) dicoParents = null;
            }
        }

        public override Element Suppression(Element elm)
        {
            if (elm == this)
            {
                if (Pioche != null)
                {
                    Pioche.SuppressionDéfausse(this);
                    Pioche = null;
                }
                if(dicoParents != null)
                {
                    foreach (var kv in dicoParents) kv.Value.SuppressionDéfausse(this);
                    dicoParents = null;
                }
                return this;
            }
            else
            {
                if (elm is Pioche)
                {
                    if (Pioche == elm && (Images == null || Images.Any() == false))
                        return this;
                    else if(dicoParents != null)
                    {
                        bool still;
                        do
                        {
                            still = false;
                            foreach (var kv in dicoParents)
                            {
                                if (Object.ReferenceEquals(kv.Value, elm))
                                {
                                    dicoParents.Remove(kv.Key);
                                    still = true;
                                    break;
                                }
                            }
                        } while (still);
                        if (dicoParents.Any() == false) dicoParents = null;
                    }
                }
                return base.Suppression(elm);
            }
        }

        override public object Clone()
        {
            return new Défausse(this);
        }

        public Défausse(Stream stream, IRessourcesDésérialiseur resscDes)
            : base(stream, resscDes)
        {
            //Pioche = resscDes.Rechercher(BitConverter.ToInt32(stream.GetBytes(4), 0)) as Pioche;
            Pioche = resscDes.RetrouverPioche(stream);
        }

        override public void Serialiser(Stream stream, ref int gidr)
        {
            base.Serialiser(stream, ref gidr);
            stream.SerialiserRefElement(Pioche, ref gidr);
        }

        override public void SerialiserTout(Stream stream, ref int gidr, ISet<int> setIdRéseau)
        {
            stream.SerialiserTout(Pioche, ref gidr, setIdRéseau);
            base.SerialiserTout(stream, ref gidr, setIdRéseau);
        }
    }
}
