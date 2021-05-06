using ModuleBOARD.Elements.Lots;
using ModuleBOARD.Elements.Lots.Piles;
using ModuleBOARD.Elements.Pieces;
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
        /// <summary>
        /// Flag d'état
        /// </summary>
        [Flags]
        public enum EEtat : byte
        {
            Couché = (1 << 0),
            À_l_envers = (1 << 1),
            PositionFixe = (1 << 2),
            RotationFixe = (1 << 3),
        };

        [Flags]
        public enum EPickUpAction
        {
            Déplacer = (1 << 2),
            Tourner = (1 << 3),
            Roulette = (1 << 8),
        };

        public int IdentifiantRéseau = 0; //Inférieur à zéro alors élément local, sinon élément global
        public GeoCoord2D GC = new GeoCoord2D(0.0f, 0.0f, 180.0f, 0.0f);
        public sbyte Ordre = 0; // bottom < top
        private EEtat Etat;
        //public PointF P = default;
        //public float ElemEchelle = 180.0f;//Basé sur le plus petit côté
        public abstract PointF Size { get; }
        public Element Parent;
        public virtual bool EstParent { get => false; }

        protected Element() { /*Etat = (EEtat)0;*/ }
        protected Element(Element elm)
        {
            GC = elm.GC;
            Ordre = elm.Ordre;
            Etat = elm.Etat;
            Parent = elm.Parent;
        }

        static public Element Charger(string path, XmlNode xmln, Dictionary<string, Element> _dElements, BibliothèqueImage bibliothèqueImage, BibliothèqueModel bibliothèqueModel)
        {
            PointF pZero = new PointF(0.0f, 0.0f);
            Element elm;
            switch (xmln.Name.ToUpper().Trim())
            {
                case "ELEMENT2D":
                    if (xmln.Attributes?.GetNamedItem("img") != null)
                    {
                        if (xmln.Attributes?.GetNamedItem("dos") != null)
                            elm = new Element2D2F(path, xmln, pZero, bibliothèqueImage);
                        else elm = new Element2D(path, xmln, pZero, bibliothèqueImage);
                    }
                    else elm = null;
                    break;
                case "GROUPE": elm = new Groupe(path, xmln, pZero, _dElements, bibliothèqueImage, bibliothèqueModel); break;
                case "PILE": elm = new Pile(path, xmln, pZero, bibliothèqueImage); break;
                case "PIOCHE": elm = new Pioche(path, xmln, pZero, bibliothèqueImage); break;
                case "DEFFAUSE": elm = new Défausse(path, xmln, pZero, bibliothèqueImage); break;
                case "PAQUET": elm = new Paquet(path, xmln, pZero, _dElements, bibliothèqueImage, bibliothèqueModel); break;
                case "FIGURINE": elm = new Figurine(path, xmln, pZero, bibliothèqueImage, bibliothèqueModel); break;
                default: elm = null; break;
            }
            return elm;
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

            string strO = att?.GetNamedItem("ord")?.Value;
            sbyte ordre;
            if (strO == null || sbyte.TryParse(strO, out ordre) == false) ordre = 0;
            Ordre = ordre;

            EEtat nvEtat = Etat;

            if (att?.GetNamedItem("caché") != null) nvEtat |= EEtat.À_l_envers;
            else if (att?.GetNamedItem("montré") != null) nvEtat &= ~EEtat.À_l_envers;
            else if (this is Figurine) nvEtat &= ~EEtat.À_l_envers;
            else nvEtat |= EEtat.À_l_envers;

            if (att?.GetNamedItem("couché") != null) nvEtat |= EEtat.Couché;
            else if (att?.GetNamedItem("debout") != null) nvEtat &= ~EEtat.Couché;
            else nvEtat &= ~EEtat.Couché;

            string strPostype = att?.GetNamedItem("position")?.Value;
            if(strPostype!=null)
            {
                if (strPostype == "fixe") nvEtat |= EEtat.PositionFixe;
                else if (strPostype == "mobile") nvEtat &= ~EEtat.PositionFixe;
            }

            string strRottype = att?.GetNamedItem("rotation")?.Value;
            if (strRottype != null)
            {
                if (strRottype == "fixe") nvEtat |= EEtat.RotationFixe;
                else if (strRottype == "mobile") nvEtat &= ~EEtat.RotationFixe;
            }

            MajEtat(nvEtat);
        }

        //public Paquet Parent;
        public abstract void Dessiner(RectangleF vue, float angle, Graphics g, PointF p);

        public virtual (Element, Element) MousePickAvecContAt(int netId)
        {
            if (netId == IdentifiantRéseau) return (this, null);
            else return (null, null);
        }

        /// <summary>
        /// Méthode chargée de trouver un élément qui serrait sous le coordonnées mp
        /// si et seulement si les état permet l'action
        /// </summary>
        /// <param name="mp"></param>
        /// <param name="angle"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual (Element, Element) MousePickAvecContAt(PointF mp, float angle, EPickUpAction action = 0)
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
            if (action.HasFlag(EPickUpAction.Roulette)) action |= EPickUpAction.Tourner;
            if (!((EEtat)action != 0 && Etat.HasFlag((EEtat)action)) && IsAt(mp, angle)) return (this, null);
            else return (null, null);
        }

        public virtual Element MousePickAt(int netId)
        {
            if (netId == IdentifiantRéseau) return this;
            else return null;
        }

        /// <summary>
        /// Méthode chargée de trouver un élément qui serrait sous le coordonnées mp
        /// si et seulement si les état permet l'action
        /// </summary>
        /// <param name="mp"></param>
        /// <param name="angle"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public virtual Element MousePickAt(PointF mp, float angle, EPickUpAction action = 0)
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
            if (action.HasFlag(EPickUpAction.Roulette)) action |= EPickUpAction.Tourner;
            if (!((EEtat)action != 0 && Etat.HasFlag((EEtat)action)) && IsAt(mp, angle)) return this;
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
        virtual public void MajEtat(EEtat nouvEtat) { Etat = nouvEtat; }
        public void MettreEtat(EEtat etat)
        {
            MajEtat(Etat | etat);
        }
        public void RetirerEtat(EEtat etat)
        {
            MajEtat(Etat & ~etat);
        }
        public void InverserEtat(EEtat etat)
        {
            MajEtat(Etat ^ etat);
        }
        public bool EstDansEtat(EEtat etat)
        {
            return Etat.HasFlag(etat);
        }

        public bool AEtatChangé(EEtat cible, EEtat nouvEtat)
        {
            return (Etat ^ nouvEtat).HasFlag(cible);
        }

        public void AssignerEtat(EEtat etat, bool mettre)
        {
            if (mettre) MettreEtat(etat);
            else RetirerEtat(etat);
        }

        public void Retourner() { MajEtat(Etat ^ EEtat.À_l_envers); }
        //Cacher l'élément
        public void Cacher() { MajEtat(Etat | EEtat.À_l_envers); }
        //Montrer l'élément
        public void Révéler() { MajEtat(Etat & ~EEtat.À_l_envers); }
        //Actionner la roulette sur cette élément
        public virtual bool Roulette(int delta) { Tourner(delta); return true; }
        public virtual void Tourner(int delta)
        {
            if (Etat.HasFlag(EEtat.RotationFixe) == false)
            {
                delta /= 120;
                delta += 8 + (int)((GC.A / 45.0f) + 0.5f);
                delta %= 8;
                GC.A = delta * 45.0f;
            }
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

        /// <summary>
        /// Mettre à jour l'élément numéro numElm par l'élément elm.
        /// </summary>
        /// <param name="numElm"></param>
        /// <param name="elm"></param>
        /// <returns></returns>
        virtual public Element MettreAJour(int numElm, Element elm)
        {
            if (numElm == IdentifiantRéseau) return this;
            else return null;
        }

        static private readonly string[][] libellé_état = new string[][]
            {
                new string[2]{ "Debout", "Couché" },
                new string[2]{ "À l'endroit", "À l'envers" },
                new string[2]{ "Position mobile", "Position fixe" },
                new string[2]{ "Rotation mobile", "Rotation fixe" }
            };

        virtual public ContextMenu Menu(Control ctrl)
        {
            MenuItem[] metat = new MenuItem[libellé_état.Length];
            for(int i=0;i< libellé_état.Length;++i)
            {
                EEtat eta = (EEtat)(1 << i);
                if (Etat.HasFlag((EEtat)(1 << i))) metat[i] = new MenuItem(libellé_état[i][1], new EventHandler((o, e) => { Etat ^= eta; ctrl.Refresh(); }));
                else metat[i] = new MenuItem(libellé_état[i][0], new EventHandler((o, e) => { Etat ^= eta; ctrl.Refresh(); }));
            }

            ContextMenu cm = new ContextMenu();
            if (Etat.HasFlag(EEtat.RotationFixe) == false)
            {
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
            }
            cm.MenuItems.Add("État", metat);
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
