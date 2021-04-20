using ModuleBOARD.Elements.Lots.Piles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ModuleBOARD.Elements.Base
{
    /*
     * Taille standard d'une image : 224*312 pixels
    */

    public abstract class Element : IComparable, ICloneable
    {
        /*static Element Trouver(string elmName)
        {
            if (DElements.ContainsKey(elmName))
                return DElements[elmName];
            else return null;
        }

        static bool Enregistrer(string elmName, Element elm)
        {
            if (DElements.ContainsKey(elmName))
                return false;
            else
            {
                DElements.Add(elmName, elm);
                return true;
            }
        }

        static bool Supprimer(string elmName)
        {
            if (DElements.ContainsKey(elmName))
            {
                DElements.Remove(elmName);
                return true;
            }
            else return false;
        }

        static void Supprimer()
        {
            DElements.Clear();
        }*/

        public ulong NetworkID = 0;
        public GeoCoord2D GC = new GeoCoord2D(0.0f, 0.0f, 180.0f, 0.0f);
        public sbyte Ordre = 0; // bottom < top
        //public PointF P = default;
        //public float ElemEchelle = 180.0f;//Basé sur le plus petit côté
        public abstract PointF Size { get; }
        public Element Parent;
        public virtual bool EstParent { get => false; }

        protected Element() { }
        protected Element(Element elm)
        {
            GC = elm.GC;
            Ordre = elm.Ordre;
            Parent = elm.Parent;
        }

        protected void Load(XmlNode paq)
        {
            XmlAttributeCollection att = paq.Attributes;

            string strX = att?.GetNamedItem("x")?.Value;
            string strY = att?.GetNamedItem("y")?.Value;

            float x;
            if (strX == null || float.TryParse(strX, out x) == false) x = 0.0f;
            float y;
            if (strY == null || float.TryParse(strY, out y) == false) y = 0.0f;
            GC.P = new PointF(x, y);

            string strE = att?.GetNamedItem("echl")?.Value;
            float echl;
            if (strE == null || float.TryParse(strE, out echl) == false) echl = 180.0f;
            GC.E = echl;

            string strA = att?.GetNamedItem("ang")?.Value;
            float ang;
            if (strA == null || float.TryParse(strA, out ang) == false) ang = 0.0f;
            GC.A = ang;

            string strO = att?.GetNamedItem("ordre")?.Value;
            sbyte ordre;
            if (strO == null || sbyte.TryParse(strO, out ordre) == false) ordre = 0;
            Ordre = ordre;
        }

        //public Paquet Parent;
        public abstract void Dessiner(RectangleF vue, float angle, Graphics g, PointF p);

        public virtual (Element, Element) MousePickAvecContAt(ulong netId)
        {
            if (netId == NetworkID) return (this, null);
            else return (null, null);
        }
        public virtual (Element, Element) MousePickAvecContAt(PointF mp, float angle)
        {
            /*SImage SImg;
            if (Caché) SImg = Dos.IsSizeValid() ? Dos : Devant;
            else SImg = Devant.IsSizeValid() ? Devant : Dos ;

            if (SImg.IsSizeValid())
            {
                if (P.X <= mp.X && P.Y <= mp.Y && mp.X < (int)(GC.P.X + GC.E)
                    && mp.Y < (int)(P.Y + (SImg.Rect.Height * GC.E) / SImg.Rect.Width))
                    return this;
                else return null;
            }
            else return null;*/
            if (IsAt(mp, angle)) return (this, null);
            else return (null, null);
        }

        public virtual Element MousePickAt(ulong netId)
        {
            if (netId == NetworkID) return this;
            else return null;
        }
        public virtual Element MousePickAt(PointF mp, float angle)
        {
            /*SImage SImg;
            if (Caché) SImg = Dos.IsSizeValid() ? Dos : Devant;
            else SImg = Devant.IsSizeValid() ? Devant : Dos ;

            if (SImg.IsSizeValid())
            {
                if (P.X <= mp.X && P.Y <= mp.Y && mp.X < (int)(GC.P.X + GC.E)
                    && mp.Y < (int)(P.Y + (SImg.Rect.Height * GC.E) / SImg.Rect.Width))
                    return this;
                else return null;
            }
            else return null;*/
            if (IsAt(mp, angle)) return this;
            else return null;
        }
        public virtual Element MousePioche()
        {
            /*if (SImg.IsSizeValid())
            {
                if (P.X <= mp.X && P.Y <= mp.Y && mp.X < (int)(P.X + ElemEchelle)
                    && mp.Y < (int)(P.Y + (SImg.Rect.Height * ElemEchelle) / SImg.Rect.Width))
                    return this;
                else return null;
            }
            else return null;*/
            return this;
        }
        //public virtual Element MousePiocheAt(PointF mp, float angle)
        //{
        //    /*if (SImg.IsSizeValid())
        //    {
        //        if (P.X <= mp.X && P.Y <= mp.Y && mp.X < (int)(P.X + ElemEchelle)
        //            && mp.Y < (int)(P.Y + (SImg.Rect.Height * ElemEchelle) / SImg.Rect.Width))
        //            return this;
        //        else return null;
        //    }
        //    else return null;*/
        //    if (IsAt(mp, angle)) return this;
        //    else return null;
        //}
        //Demande à l'élément de mettre celui en paramètre sur le dessus. Retourne vrai si il a pu le faire
        public abstract bool PutOnTop(Element elm);
        //Propose à l'élément désigné élément de retourner vers sont parent
        public virtual Element MouseRanger()
        {
            if (Parent != null) return RangerVersParent(Parent);
            else return null;
        }
        /*public virtual Element MouseRangerAt(PointF mp, float angle)
        {
            if (Parent != null && IsAt(mp, angle))
                return RangerVersParent(Parent);
            else return null;
        }*/
        //Indique que l'élément elm est laché sur this.
        //La méthode retourne soit elm (refus) soit un autre élément à la palce soit null
        public virtual Element ElementLaché(Element elm)
        {
            return elm;
        }
        //Propose aux éléments ayant le parent d'y retourner

        public virtual Element RangerVersParent(Element parent)
        {
            if (parent != null && parent == Parent)
            {
                return (Parent.ElementLaché(this) == null) ? this : null;
            }
            else return null;
        }
        //Propose à l'élément relem de se ranger en scannant partout
        public virtual Element DéfausserElement(Element relem)
        {
            if (this == relem)
            {
                if(Parent is Pioche && (Parent as Pioche).Défausse != null)
                    return ((Parent as Pioche).Défausse.ElementLaché(this) == null) ? this : null;
                else return (Parent.ElementLaché(this) == null) ? this : null;
            }
            else return null;
        }
        //Propage dans l'arbre le fait de devoir détacher l'élément
        public virtual Element DétacherElement(Element relem)
        {
            if (relem == this) return relem;
            else return null;
        }
        //Retrouner l'élément visible/caché
        public abstract void Retourner();
        //Cacher l'élément
        public abstract void Cacher();
        //Montrer l'élément
        public abstract void Révéler();
        //Actionner la roulette sur cette élément
        public abstract bool Roulette(int delta);
        public virtual void Tourner(int delta)
        {
            delta /= 120;
            delta += 8 + (int)((GC.A / 45.0f) + 0.5f);
            delta %= 8;
            GC.A = delta * 45.0f;
        }

        public abstract bool Lier(XmlNode paq, Dictionary<string, Element> dElements);
        public virtual Element Suppression(Element elm)
        {
            Element re = (this == elm ? this : null);
            if (re != null) return re;
            else
            {
                if (elm == Parent) Parent = null;
                return null;
            }
        }

        public virtual bool IsAt(PointF mp, float angle)
        {
            return IsAt(mp, Size, angle);
        }

        public virtual bool IsAt(PointF mp, PointF psz, float angle)
        {
            psz.X /= 2.0f;
            psz.Y /= 2.0f;

            mp.X -= GC.P.X/* - (psz.X / 2.0f)*/;
            mp.Y -= GC.P.Y/* - (psz.Y / 2.0f)*/;

            Matrix m = new Matrix();
            m.Rotate(GC.A);
            PointF nmp = new PointF
                (
                    mp.X * m.Elements[0] + mp.Y * m.Elements[1],
                    mp.X * m.Elements[2] + mp.Y * m.Elements[3]
                );

            return (-psz.X <= nmp.X && -psz.Y <= nmp.Y && nmp.X <= psz.X && nmp.Y <= psz.Y);
        }

        virtual public ContextMenu Menu(Control ctrl)
        {
            ContextMenu cm = new ContextMenu();
            cm.MenuItems.Add("Rotation", new MenuItem[]
                {
                    /*new MenuItem(" -45", new EventHandler((o,e) => { GC.A=(GC.A + (360.0f-45.0f)) % 360.0f; ctrl.Refresh(); })),
                    new MenuItem(" -90", new EventHandler((o,e) => { GC.A=(GC.A + (360.0f-90.0f)) % 360.0f; ctrl.Refresh(); })),
                    new MenuItem(" +45", new EventHandler((o,e) => { GC.A=(GC.A + 45.0f) % 360.0f; ctrl.Refresh(); })),
                    new MenuItem(" +90", new EventHandler((o,e) => { GC.A=(GC.A + 90.0f) % 360.0f; ctrl.Refresh(); })),
                    new MenuItem("+180", new EventHandler((o,e) => { GC.A=(GC.A + 180.0f) % 360.0f; ctrl.Refresh(); }))*/
                    new MenuItem("-135", new EventHandler((o,e) => { GC.A=(360.0f-135.0f); ctrl.Refresh(); })),
                    new MenuItem(" -90", new EventHandler((o,e) => { GC.A=(360.0f-90.0f); ctrl.Refresh(); })),
                    new MenuItem(" -45", new EventHandler((o,e) => { GC.A=(360.0f-45.0f); ctrl.Refresh(); })),
                    new MenuItem("   0", new EventHandler((o,e) => { GC.A=(0.0f); ctrl.Refresh(); })),
                    new MenuItem(" +45", new EventHandler((o,e) => { GC.A=(45.0f); ctrl.Refresh(); })),
                    new MenuItem(" +90", new EventHandler((o,e) => { GC.A=(90.0f); ctrl.Refresh(); })),
                    new MenuItem("+135", new EventHandler((o,e) => { GC.A=(135); ctrl.Refresh(); })),
                    new MenuItem("+180", new EventHandler((o,e) => { GC.A=(180.0f); ctrl.Refresh(); }))
                });
            return cm;
        }

        public int CompareTo(object obj)
        {
            if (obj is Element)
            {
                Element e = obj as Element;
                return (Ordre == e.Ordre) ? 0 : (Ordre < e.Ordre ? 1 : -1);//Volontairement inversé au niveau de l'ordre
            }
            else return 0;
        }

        abstract public object Clone();

        /*{
get
{
MenuItem[] mnItms = new MenuItem[]
{
  new MenuItem("Coco")
};
ContextMenu cm = new ContextMenu(mnItms);
return cm;
}
}*/
        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj)) return true;
            else if (obj is Element)
            {
                Element elm = obj as Element;
                return GC.E == elm.GC.E && Size == elm.Size;
            }
            else return false;
        }

    }
}
