using ModuleBOARD.Elements.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ModuleBOARD.Elements.Lots.Piles
{

    public class Pioche : Pile
    {
        public Défausse Défausse = null;

        Pioche() { }

        public Pioche(Pioche elm)
            : base(elm)
        {
            Défausse = elm.Défausse;
        }

        public Pioche(string path, XmlNode paq, PointF p, BibliothèqueImage bibliothèqueImage)
            :base(path, paq, p, bibliothèqueImage)
        {
        }

        /*public override Element MousePiocheAt(PointF mp, float angle)
        {
            Element elm = base.MousePiocheAt(mp, angle);

            if (elm != null && elm.Parent == this)
                elm.Parent = this;

            return elm;
        }*/

        public void RécupérerDeLaDéfausse()
        {
            if (Défausse != null)
            {
                List<KeyValuePair<Image, Image>> imgs = Défausse.Images;
                if (imgs != null)
                {
                    Défausse.Images = null;
                    Images.AddRange(imgs);
                }
            }
        }

        override public ContextMenu Menu(Control ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();
            if(Défausse != null)cm.MenuItems.Add("Récupérer de la défausse", new EventHandler((o, e) => { RécupérerDeLaDéfausse(); ctrl.Refresh(); }));
            return cm;
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            XmlAttributeCollection atts = paq.Attributes;
            if (atts != null)
            {
                string nomDéfausse = atts.GetNamedItem("défausse")?.Value;
                if (dElements != null && dElements.ContainsKey(nomDéfausse))
                {
                    Défausse = dElements[nomDéfausse] as Défausse;
                }
            }
            return true;
        }

        public void SuppressionDéfausse(Défausse d)
        {
            if (Défausse == d) Défausse = null;
        }

        public override Element Suppression(Element elm)
        {
            if (elm == this)
            {
                if (Défausse != null) Défausse.SuppressionPioche(this);
                return this;
            }
            else
            {
                if (elm == Défausse) Défausse = null;
                return base.Suppression(elm);
            }
        }

        override public object Clone()
        {
            return new Pioche(this);
        }

    }

    //public class DéfaussePioche : Pile
    //{
    //    public Pioche Pioche = null;

    //    DéfaussePioche() { }

    //    public DéfaussePioche(DéfaussePioche elm)
    //        :base(elm)
    //    {
    //        Pioche = elm.Pioche;
    //    }

    //    public DéfaussePioche(string path, XmlNode paq, PointF p)
    //        : base(path, paq, p)
    //    {
    //    }

    //    public override Element MousePiocheAt(PointF mp, float angle)
    //    {
    //        Element elm = base.MousePiocheAt(mp, angle);
    //        if (Pioche != null) elm.Parent = Pioche;
    //        return elm;
    //    }

    //    public void MettreDansPioche()
    //    {
    //        if(Pioche != null)
    //        {
    //            List<Image> imgs = Images;
    //            Images = null;
    //            Pioche.AddRange(imgs);
    //        }
    //    }

    //    override public ContextMenu Menu(Control ctrl)
    //    {
    //        ContextMenu cm = base.Menu(ctrl);
    //        if (cm == null) cm = new ContextMenu();
    //        if(Pioche != null) cm.MenuItems.Add("Remettre dans la pioche", new EventHandler((o, e) => { MettreDansPioche(); ctrl.Refresh(); }));
    //        return cm;
    //    }

    //    public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
    //    {
    //        XmlAttributeCollection atts = paq.Attributes;
    //        if (atts != null)
    //        {
    //            string nomPioche = atts.GetNamedItem("pioche")?.Value;
    //            if (dElements != null && dElements.ContainsKey(nomPioche))
    //            {
    //                Pioche = dElements[nomPioche] as Pioche;
    //            }
    //        }
    //        return true;
    //    }

    //    public override Element Suppression(Element elm)
    //    {
    //        if (elm == this)
    //        {
    //            if (Pioche != null) Pioche.SuppressionDéfausse(this);
    //            return this;
    //        }
    //        else
    //        {
    //            if (elm == Pioche) Pioche = null;
    //            return base.Suppression(elm);
    //        }
    //    }

    //    override public object Clone()
    //    {
    //        return new DéfaussePioche(this);
    //    }

    //}
}
