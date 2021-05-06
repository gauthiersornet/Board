using ModuleBOARD.Elements.Base;
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

namespace ModuleBOARD.Elements.Lots
{
    class Paquet : Element, IFigurine
    {
        public struct SElemQty
        {
            public Element Elm;
            public int Qty;
        }

        public Image ImgVisible;
        public Image ImgCaché;
        //public Paquet Parent = null;
        public List<SElemQty> LstElements = null;
        public int ElementSélectionné = -1;//Caché par défaut

        public float Z { get; set; }

        public override bool EstParent { get => true; }

        public override PointF Size
        {
            get
            {
                Image imgref;

                if(0<=ElementSélectionné)//visible ?
                {
                    imgref = ImgVisible ?? ImgCaché ?? BibliothèqueImage.EmptyImage;
                }
                else //Caché
                {
                    imgref = ImgCaché ?? ImgVisible ?? BibliothèqueImage.EmptyImage;
                }

                PointF sz = GC.ProjSize(imgref.Rect());
                return sz;
            }
        }

        public Paquet(Paquet elm)
            : base(elm)
        {
            LstElements = (elm.LstElements != null ? new List<SElemQty>(elm.LstElements) : null);
            if (LstElements != null && LstElements.Any())
            {
                ElementSélectionné = 0;
                CorrigerSelection();
            }
        }

        public Paquet(string path, XmlNode paq, PointF p, Dictionary<string, Element> _dElements, BibliothèqueImage bibliothèqueImage, BibliothèqueModel bibliothèqueModel)
        {
            if(_dElements == null) _dElements = new Dictionary<string, Element>();
            base.Load(paq);
            GC.P.X += p.X;
            GC.P.Y += p.Y;

            //PointF pZero = new PointF(0.0f, 0.0f);
            LstElements = new List<SElemQty>();
            foreach (XmlNode xmln in paq.ChildNodes)
            {
                Element elm;
                if (xmln.Name.ToUpper().Trim() != "GROUPE")
                    elm = Charger(path, xmln, _dElements, bibliothèqueImage, bibliothèqueModel);
                else elm = null;
                /*switch (xmln.Name.ToUpper().Trim())
                {
                    //case "GROUPE": elm = new Groupe(path, xmln, pZero, _dElements); break;
                    case "PILE": elm = new Pile(path, xmln, pZero, bibliothèqueImage); break;
                    case "PIOCHE": elm = new Pioche(path, xmln, pZero, bibliothèqueImage); break;
                    case "DEFFAUSE": elm = new Défausse(path, xmln, pZero, bibliothèqueImage); break;
                    case "PAQUET": elm = new Paquet(path, xmln, pZero, _dElements, bibliothèqueImage, bibliothèqueModel); break;
                    case "FIGURINE": elm = new Figurine(path, xmln, pZero, bibliothèqueImage, bibliothèqueModel); break;
                    default: elm = null; break;
                }*/
                if (elm != null && !(elm is Groupe))
                {
                    elm.Parent = this;
                    string nom = xmln.Attributes?.GetNamedItem("nom")?.Value;
                    if (nom != null) _dElements.Add(nom, elm);
                    string qty = xmln.Attributes?.GetNamedItem("qty")?.Value;
                    int iqty;
                    if (qty != null && int.TryParse(qty, out iqty))
                        LstElements.Add(new SElemQty() { Elm = elm, Qty = iqty });
                    else LstElements.Add(new SElemQty() { Elm = elm, Qty = -1 });
                }
            }
            _lier(paq.ChildNodes, _dElements);
            LstElements.Sort();

            if (LstElements != null && LstElements.Any())
            {
                if (paq.Attributes?.GetNamedItem("caché") != null)
                    ElementSélectionné = -1;
                else if (paq.Attributes?.GetNamedItem("montré") != null)
                    ElementSélectionné = 0;
                else ElementSélectionné = 0;
                CorrigerSelection();
            }
        }

        private bool _lier(XmlNodeList paqL, Dictionary<string, Element> dElements)
        {
            if (paqL.Count == LstElements.Count)
            {
                int i = 0;
                foreach (XmlNode xmln in paqL)
                    LstElements[i++].Elm.Lier(xmln, dElements);

                return true;
            }
            else return false;
        }

        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {

            if (LstElements != null)
            {
                GeoCoord2D gc = GC;
                gc.P.X += p.X;
                gc.P.Y += p.Y;
                /*for (int i = LstElements.Count - 1; i >= 0; --i)
                {
                    if (LstElements[i].Elm != null)
                        LstElements[i].Elm.Dessiner(vue, angle, g, p);
                }*/
                //Matrix m = g.Transform;
                if (0 <= ElementSélectionné)//visible ?
                {
                    if(ImgVisible!=null) ImgVisible.Dessiner(vue, g, gc);
                    else if (ImgCaché != null)
                    {
                        new Point(ImgCaché.Width, ImgCaché.Height).DessinerVide(vue, g, gc);
                    }
                    else new Point(20, 20).DessinerVide(vue, g, gc);
                    if (LstElements!=null &&
                        ElementSélectionné < LstElements.Count &&
                        LstElements[ElementSélectionné].Qty != 0 &&
                        !(LstElements[ElementSélectionné].Elm is Figurine))
                    {
                        //g.Transform = m;
                        //g.TranslateTransform(gc.P.X, gc.P.Y);
                        //g.RotateTransform(GC.A);
                        //angle = (angle + 360.0f - GC.A) % 360.0f;
                        LstElements[ElementSélectionné].Elm.GC.A = GC.A;
                        LstElements[ElementSélectionné].Elm.Dessiner(vue, angle, g, gc.P);
                    }
                }
                else if(ImgCaché != null)//caché ?
                {
                    (ImgCaché ?? ImgVisible).Dessiner(vue, g, gc);
                }
                else if(ImgVisible != null)
                {
                    new Point(ImgVisible.Width, ImgVisible.Height).DessinerVide(vue, g, gc);
                }
                else new Point(20, 20).DessinerVide(vue, g, gc);
                //g.Transform = m;
            }
        }

        public void DessinerFigurine(RectangleF vue, float angle, Graphics g, PointF p)
        {
            if (LstElements != null)
            {
                GeoCoord2D gc = GC;
                gc.P.X += p.X;
                gc.P.Y += p.Y;
                if (0 <= ElementSélectionné)//visible ?
                {
                    if (LstElements != null &&
                        ElementSélectionné < LstElements.Count &&
                        LstElements[ElementSélectionné].Qty != 0)
                    {
                        if (LstElements[ElementSélectionné].Elm is Figurine)
                        {
                            LstElements[ElementSélectionné].Elm.GC.A = GC.A;
                            LstElements[ElementSélectionné].Elm.Dessiner(vue, angle, g, gc.P);
                        }
                        else if (LstElements[ElementSélectionné].Elm is Paquet)
                        {
                            LstElements[ElementSélectionné].Elm.GC.A = GC.A;
                            (LstElements[ElementSélectionné].Elm as Paquet).DessinerFigurine(vue, angle, g, gc.P);
                        }
                    }
                }
            }
        }

        /*new public bool IsAt(PointF mp)
        {
            PointF sz = Size;
            return IsAt(mp, sz);
        }*/

        /*public override Element MousePickAt(PointF mp, float angle)
        {
            if (IsAt(mp, angle)) return this;
            else return null;
        }*/

        private bool ADuContenu()
        {
            return (LstElements != null
                && LstElements.Count > 0
                && LstElements.FirstOrDefault(eq => eq.Qty != 0).Qty != 0);
        }

        private int NbElements()
        {
            return (LstElements != null ? LstElements.Sum(eq => Math.Abs(eq.Qty)) : 0);
        }

        /// <summary>
        /// Si l'élément s'electionné est en 0 exemplaires alors changer d'objets selectionnés.
        /// </summary>
        private void CorrigerSelection()
        {
            if (0 <= ElementSélectionné)
            {
                if (LstElements != null && LstElements.Count>0)
                {
                    if (ElementSélectionné >= LstElements.Count)
                        ElementSélectionné = 0;
                    if(LstElements[ElementSélectionné].Qty == 0)
                    {
                        for (int iElmHaut = (ElementSélectionné + 1) % LstElements.Count
                                , iElmBas = (ElementSélectionné + (LstElements.Count - 1)) % LstElements.Count;
                            iElmHaut != ElementSélectionné;
                            iElmHaut = (iElmHaut + 1) % LstElements.Count
                                , iElmBas = (iElmBas + (LstElements.Count - 1)) % LstElements.Count)
                        {
                            if(LstElements[iElmHaut].Qty != 0)
                            {
                                ElementSélectionné = iElmHaut;
                                return;
                            }
                            if (LstElements[iElmBas].Qty != 0)
                            {
                                ElementSélectionné = iElmBas;
                                return;
                            }
                        }
                        ElementSélectionné = 0;
                    }
                }
                else ElementSélectionné = 0;
            }
        }

        private void CorrigerSelectionHaut()
        {
            if (0 <= ElementSélectionné)
            {
                if (LstElements != null && LstElements.Count > 0)
                {
                    if (ElementSélectionné >= LstElements.Count)
                        ElementSélectionné = 0;
                    if (LstElements[ElementSélectionné].Qty == 0)
                    {
                        for (int iElm = (ElementSélectionné + 1) % LstElements.Count;
                            iElm != ElementSélectionné;
                            iElm = (iElm + 1) % LstElements.Count)
                        {
                            if (LstElements[iElm].Qty != 0)
                            {
                                ElementSélectionné = iElm;
                                return;
                            }
                        }
                        ElementSélectionné = 0;
                    }
                }
                else ElementSélectionné = 0;
            }
        }

        private void CorrigerSelectionBas()
        {
            if (0 <= ElementSélectionné)
            {
                if (LstElements != null && LstElements.Count > 0)
                {
                    if (ElementSélectionné >= LstElements.Count)
                        ElementSélectionné = 0;
                    if (LstElements[ElementSélectionné].Qty == 0)
                    {
                        for (int iElm = (ElementSélectionné + (LstElements.Count - 1)) % LstElements.Count;
                            iElm != ElementSélectionné;
                            iElm = (iElm + (LstElements.Count - 1)) % LstElements.Count)
                        {
                            if (LstElements[iElm].Qty != 0)
                            {
                                ElementSélectionné = iElm;
                                return;
                            }
                        }
                        ElementSélectionné = 0;
                    }
                }
                else ElementSélectionné = 0;
            }
        }

        public override Element MousePioche()
        {
            int elmPioché = -1;//Pas de pioche
            if (ADuContenu())
            {
                if (0 <= ElementSélectionné)//visible ?
                {
                    CorrigerSelection();
                    elmPioché = ElementSélectionné;
                }
                else //caché ? //Tirer au hazard !
                {
                    Random rnd = new Random();
                    int rndNmb = rnd.Next(NbElements());
                    elmPioché = 0;
                    foreach (SElemQty eq in LstElements)
                    {
                        rndNmb -= Math.Abs(eq.Qty);
                        if (rndNmb < 0) break;
                        else ++elmPioché;
                    }
                }
            }

            if (0 <= elmPioché)
            {
                SElemQty elmQt = LstElements[elmPioché];
                Element felm = elmQt.Elm.Clone() as Element;
                elmQt.Qty--;
                LstElements[elmPioché] = elmQt;
                if (elmQt.Qty == 0)
                {
                    LstElements.RemoveAt(elmPioché);
                    CorrigerSelection();
                }
                felm.GC = GC;
                felm.Parent = this;
                return felm;
            }
            else return null;
        }

        /*public override Element MousePiocheAt(PointF mp, float angle)
        {
            if (IsAt(mp, angle))
            {

                int elmPioché = -1;//Pas de pioche
                if (ADuContenu())
                {
                    if (0 <= ElementSélectionné)//visible ?
                    {
                        CorrigerSelection();
                        elmPioché = ElementSélectionné;
                    }
                    else //caché ? //Tirer au hazard !
                    {
                        Random rnd = new Random();
                        int rndNmb = rnd.Next(NbElements());
                        elmPioché = 0;
                        foreach (SElemQty eq in LstElements)
                        {
                            rndNmb -= Math.Abs(eq.Qty);
                            if (rndNmb < 0) break;
                            else ++elmPioché;
                        }
                    }
                }

                if (0 <= elmPioché)
                {
                    SElemQty elmQt = LstElements[elmPioché];
                    Element felm = elmQt.Elm.Clone() as Element;
                    elmQt.Qty--;
                    LstElements[elmPioché] = elmQt;
                    if (elmQt.Qty == 0)
                    {
                        LstElements.RemoveAt(elmPioché);
                        CorrigerSelection();
                    }
                    felm.GC = GC;
                    felm.Parent = this;
                    return felm;
                }
                else return null;
            }
            else return null;
        }*/

        public override Element ElementLaché(Element elm)
        {
            if (LstElements != null)
            {
                for (int i= 0; i < LstElements.Count; ++i)
                {
                    if (LstElements[i].Elm.Equals(elm))
                    {
                        SElemQty eq = LstElements[i];
                        eq.Qty++;
                        LstElements[i] = eq;
                        return null;
                    }
                }
                elm.GC.P = new PointF(0.0f, 0.0f);
                //if (elm is Figurine) (elm as Figurine).Couchée = false;
                LstElements.Add(new SElemQty() { Elm = elm, Qty = 1 });
                return null;
            }
            else
            {
                elm.GC.P = new PointF(0.0f, 0.0f);
                //if (elm is Figurine) (elm as Figurine).Couchée = false;
                LstElements = new List<SElemQty>();
                LstElements.Add(new SElemQty() { Elm = elm, Qty = 1 });
                return null;
            }
        }

        override public object Clone()
        {
            return new Paquet(this);
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj)) return true;
            else if (obj is Paquet)
            {
                Paquet elm = obj as Paquet;
                if (LstElements == elm.LstElements) return true;
                else if (LstElements != null && elm.LstElements != null && LstElements.Count == elm.LstElements.Count)
                {
                    foreach (SElemQty eq in LstElements)
                        if (elm.LstElements.Contains(eq) == false) return false;
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public override bool PutOnTop(Element elm)
        {
            return (this == elm);
        }

        public override void MajEtat(EEtat nouvEtat)
        {
            if(AEtatChangé(EEtat.À_l_envers, nouvEtat))
            {
                if(EstDansEtat(EEtat.À_l_envers))
                {
                    ElementSélectionné = -1;
                }
                else
                {
                    ElementSélectionné = 0;
                }
            }
            base.MajEtat(nouvEtat);
        }

        public override bool Roulette(int delta)
        {
            if (LstElements != null && LstElements.Count > 0)
            {
                if (delta > 0)
                {
                    if (ElementSélectionné < 0) ElementSélectionné = 0;
                    else
                    {
                        ++ElementSélectionné;
                        if (ElementSélectionné >= LstElements.Count)
                        {
                            if (ImgCaché != null) ElementSélectionné = -1;
                            else ElementSélectionné = 0;
                        }
                    }
                }
                else if (delta < 0)
                {
                    if (ElementSélectionné < 0) ElementSélectionné = LstElements.Count - 1;
                    else
                    {
                        --ElementSélectionné;
                        if (ElementSélectionné < 0)
                        {
                            if (ImgCaché != null) ElementSélectionné = -1;
                            else ElementSélectionné = LstElements.Count - 1;
                        }
                    }
                }
            }
            CorrigerSelection();
            return true;
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            if(LstElements!=null && LstElements.Count>0)
            {
                int i = 0;
                foreach (XmlNode xmln in paq)
                    LstElements[i++].Elm.Lier(xmln, dElements);

                return true;
            }
            else return false;
        }

        public override Element Suppression(Element elm)
        {
            Element re;
            re = base.Suppression(elm);
            if (re != null) return re;
            else if (LstElements != null && LstElements.Count > 0)
            {
                SElemQty isOwn = new SElemQty() { Elm=null, Qty=0 };
                re = null;
                for(int i = 0; i < LstElements.Count; ++i)
                {
                    SElemQty e = LstElements[i];
                    if (e.Qty > 0)
                    {
                        re = e.Elm.Suppression(elm);
                        if (re != null)
                        {
                            if(Object.ReferenceEquals(re, e))
                            {
                                e.Qty--;
                                LstElements[i]=e;
                                isOwn = e;
                            }
                            break;
                        }
                    }
                }
                if (isOwn.Elm != null && isOwn.Qty==0)
                    LstElements.Remove(isOwn);
                return re;
            }
            else return null;
        }

        /// <summary>
        /// Mettre à jour l'élément numéro numElm par l'élément elm
        /// </summary>
        /// <param name="numElm"></param>
        /// <param name="elm"></param>
        /// <returns></returns>
        override public Element MettreAJour(int numElm, Element elm)
        {
            if (numElm == IdentifiantRéseau) return elm;
            else
            {
                Element res = null;
                for (int i = 0; res == null && i < LstElements.Count; ++i)
                {
                    SElemQty e = LstElements[i];
                    if (e.Elm != null)
                    {
                        res = e.Elm.MettreAJour(numElm, elm);
                        if (e.Elm == res) e.Elm = elm;
                    }
                }
                return res;
            }
        }

        /*override public ContextMenu Menu(Control ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();
            cm.MenuItems.AddRange(new MenuItem[]
                    {
                        new MenuItem("Ranger", new EventHandler((o,e) => {Board.RangerVersParent(this); ctrl.Refresh(); }))
                    });
            return cm;
        }*/
    }
}
