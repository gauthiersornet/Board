using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Pieces;
using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        public override EType ElmType { get => EType.Pioche; }

        Pioche() { }

        public Pioche(int idREz) : base(idREz) { }

        public Pioche(Element2D element2D) : base(element2D) { MettreAJourEtat(); }

        public Pioche(Element2D2F element2D2F) : base(element2D2F) { MettreAJourEtat(); }

        public Pioche(Pioche elm)
            : base(elm)
        {
            Défausse = elm.Défausse;
            MettreAJourEtat();
        }

        public Pioche(string path, XmlNode paq, PointF p, BibliothèqueImage bibliothèqueImage)
            :base(path, paq, p, bibliothèqueImage)
        {
        }

        override public object MettreAJour(object obj)
        {
            if(Défausse != null && obj is Défausse && (obj as Défausse).IdentifiantRéseau == Défausse.IdentifiantRéseau)
                Défausse = obj as Défausse;
            return base.MettreAJour(obj);
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

        override public ContextMenu Menu(IBoard ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();
            if(Défausse != null)cm.MenuItems.Add("Récupérer de la défausse", new EventHandler((o, e) => { RécupérerDeLaDéfausse(); ctrl.Refresh(); }));
            else cm.MenuItems.Add("Créer la défausse", new EventHandler((o, e) => { ctrl.CréerLaDéfausse(this); }));
            return cm;
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            XmlAttributeCollection atts = paq.Attributes;
            if (atts != null)
            {
                string nomDéfausse = atts.GetNamedItem("défausse")?.Value;
                if (dElements != null && nomDéfausse != null && dElements.ContainsKey(nomDéfausse))
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
                if (Défausse != null)
                {
                    Défausse.SuppressionPioche(this);
                    Défausse = null;
                }
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

        public Pioche(Stream stream, IRessourcesDésérialiseur resscDes)
            : base(stream, resscDes)
        {
            //Défausse = resscDes.Rechercher(BitConverter.ToInt32(stream.GetBytes(4), 0)) as Défausse;
            Défausse = resscDes.RetrouverDéfausse(stream);
        }

        override public void Serialiser(Stream stream, ref int gidr)
        {
            base.Serialiser(stream, ref gidr);
            stream.SerialiserRefElement(Défausse, ref gidr);
        }

        override public void SerialiserTout(Stream stream, ref int gidr, ISet<int> setIdRéseau)
        {
            stream.SerialiserTout(Défausse, ref gidr, setIdRéseau);
            base.SerialiserTout(stream, ref gidr, setIdRéseau);
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
