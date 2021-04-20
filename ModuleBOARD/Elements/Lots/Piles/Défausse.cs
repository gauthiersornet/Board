using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Pieces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace ModuleBOARD.Elements.Lots.Piles
{
    public class Défausse : Pile
    {
        public Pioche Pioche = null;
        private Dictionary<KeyValuePair<Image, Image>, Pioche> dicoParents = null;

        Défausse() { }

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

        public void MettreEnPioche()
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

        override public ContextMenu Menu(Control ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();
            if(Pioche!=null || dicoParents != null)
                cm.MenuItems.Add("Remettre en pioche", new EventHandler((o, e) => { MettreEnPioche(); ctrl.Refresh(); }));
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
        public override Element MousePioche()
        {
            //Element elm = base.MousePiocheAt(mp, angle);
            Element elm = base.MousePioche();
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
                if (dElements != null && dElements.ContainsKey(nomPioche))
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
                if (Pioche != null) Pioche.SuppressionDéfausse(this);
                else if(dicoParents != null)
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
                return base.Suppression(elm);
            }
        }

        override public object Clone()
        {
            return new Défausse(this);
        }
    }
}
