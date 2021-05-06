using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots.Piles;
using ModuleBOARD.Elements.Pieces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModuleBOARD.Elements.Lots
{
    public class Groupe : Element
    {
        //public Paquet Parent = null;
        private List<Element> LstElements = null;
        private List<IFigurine> LstFigurines = null;

        private float ancienAngleDessin = 0.0f;

        public override bool EstParent { get => true; }
        public bool EstVide{ get => (LstElements == null || !LstElements.Any()) && (LstFigurines == null || !LstFigurines.Any()); }

        public override PointF Size
        { 
            get => new PointF(float.MaxValue, float.MaxValue);
        }

        public override bool IsAt(PointF mp, float angle)
        {
            return true;
        }

        public override bool IsAt(PointF mp, PointF psz, float angle)
        {
            return true;
        }

        public Groupe()
        {
        }

        public Groupe(Groupe elm)
            :base(elm)
        {
            LstElements = (elm.LstElements != null ? new List<Element>(elm.LstElements) : null);
            LstFigurines = (elm.LstFigurines != null ? new List<IFigurine>(elm.LstFigurines) : null);
        }

        public Groupe(string path, XmlNode paq, PointF p, Dictionary<string, Element> _dElements, BibliothèqueImage bibliothèqueImage, BibliothèqueModel bibliothèqueModel)
        {
            if(_dElements == null) _dElements = new Dictionary<string, Element>();
            base.Load(paq);
            GC.P.X += p.X;
            GC.P.Y += p.Y;

            //PointF pZero = new PointF(0.0f, 0.0f);
            LstElements = new List<Element>();
            LstFigurines = new List<IFigurine>();
            foreach (XmlNode xmln in paq.ChildNodes)
            {
                Element elm = Charger(path, xmln, _dElements, bibliothèqueImage, bibliothèqueModel);
                /*switch (xmln.Name.ToUpper().Trim())
                {
                    case "GROUPE": elm = new Groupe(path, xmln, pZero, _dElements, bibliothèqueImage, bibliothèqueModel); break;
                    case "PILE": elm = new Pile(path, xmln, pZero, bibliothèqueImage); break;
                    case "PIOCHE": elm = new Pioche(path, xmln, pZero, bibliothèqueImage); break;
                    case "DEFFAUSE": elm = new Défausse(path, xmln, pZero, bibliothèqueImage); break;
                    case "PAQUET": elm = new Paquet(path, xmln, pZero, _dElements, bibliothèqueImage, bibliothèqueModel); break;
                    case "FIGURINE": elm = new Figurine(path, xmln, pZero, bibliothèqueImage, bibliothèqueModel); break;
                    default: elm = null; break;
                }*/
                if (elm != null)
                {
                    string nom = xmln.Attributes?.GetNamedItem("nom")?.Value;
                    if (nom != null) _dElements.Add(nom, elm);
                    if ((elm is Figurine) == false) LstElements.Add(elm);
                    if (elm is IFigurine) AddFigurine(elm as IFigurine);
                }
            }
            _lier(paq.ChildNodes, _dElements);
            if (LstElements.Any()) LstElements.Sort();
            else LstElements = null;
            if (LstFigurines.Any() == false) LstFigurines = null;
        }

        private void AddFigurine(IFigurine fig)
        {
            if (LstFigurines == null) LstFigurines = new List<IFigurine>();
            LstFigurines.Add(fig);
            MettreAJourZOrdre(fig);
        }

        public void MettreAJourOrdre(int idx)
        {
            if (LstElements != null && 0 <= idx && idx < LstElements.Count)
            {
                Element elm = LstElements[idx];
                for (; idx > 0 && (LstElements[idx - 1].Ordre > elm.Ordre); --idx)
                {
                    LstElements[idx] = LstElements[idx - 1];
                }
                for (; idx < (LstElements.Count - 1) && (LstElements[idx + 1].Ordre < elm.Ordre); ++idx)
                {
                    LstElements[idx] = LstElements[idx + 1];
                }
                LstElements[idx] = elm;
            }
        }

        public void MettreAJourZOrdre(IFigurine fig)
        {
            if (LstFigurines != null)
            {
                int fidx;
                for (fidx = 0; fidx < LstFigurines.Count && Object.ReferenceEquals(fig, LstFigurines[fidx]) == false; ++fidx) ;
                if(fidx < LstFigurines.Count)
                {
                    fig.Z = CalculerFigurineZ(fig, ancienAngleDessin);
                    for (; fidx > 0 && (LstFigurines[fidx - 1].Z < fig.Z); --fidx)
                    {
                        LstFigurines[fidx] = LstFigurines[fidx - 1];
                    }
                    for (; fidx < (LstFigurines.Count-1) && (LstFigurines[fidx + 1].Z > fig.Z); ++fidx)
                    {
                        LstFigurines[fidx] = LstFigurines[fidx + 1];
                    }
                    LstFigurines[fidx] = fig;
                }
            }
        }

        private bool _lier(XmlNodeList paqL, Dictionary<string, Element> dElements)
        {
            if (paqL.Count == (LstElements?.Count ?? 0 + LstFigurines?.Count ?? 0))
            {
                int i = 0;
                int ifig = 0;
                foreach (XmlNode xmln in paqL)
                {
                    if(xmln.Name.ToUpper().Trim() == "FIGURINE")
                        (LstFigurines[ifig++] as Element).Lier(xmln, dElements);
                    else LstElements[i++].Lier(xmln, dElements);
                }

                return true;
            }
            else return false;
        }

        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {
            p.X += GC.P.X;
            p.Y += GC.P.Y;
            if (LstElements != null)
            {
                for (int i= LstElements.Count-1; i>=0; --i)
                {
                    if (LstElements[i] != null)
                        LstElements[i].Dessiner(vue, angle, g, p);
                }
            }
            if (LstFigurines != null)
            {
                if (ancienAngleDessin != angle) TrierZFigurine(angle);
                for (int i = LstFigurines.Count - 1; i >= 0; --i)
                {
                    if (LstFigurines[i] != null)
                        LstFigurines[i].DessinerFigurine(vue, angle, g, p);
                }
            }
        }

        public void TrierZFigurine(float angle)
        {
            ancienAngleDessin = angle;
            if (LstFigurines != null)
            {
                double dAng = angle * (Math.PI / 180.0);
                double cos = Math.Cos(dAng);
                double sin = Math.Sin(dAng);
                LstFigurines.ForEach(f => f.Z = (float)(sin * (f as Element).GC.P.X + cos * (f as Element).GC.P.Y));
                LstFigurines.Sort(new FigurineZComparer());
            }
        }

        static public float CalculerFigurineZ(IFigurine fig, float angle)
        {
            if (fig != null)
            {
                double dAng = angle * (Math.PI / 180.0);
                double cos = Math.Cos(dAng);
                double sin = Math.Sin(dAng);
                return (float)(sin * (fig as Element).GC.P.X + cos * (fig as Element).GC.P.Y);
            }
            else return 0.0f;
        }

        public override (Element, Element) MousePickAvecContAt(int netId)
        {
            Element felm, conteneur;
            //(felm, conteneur) = base.MousePickAvecContAt(netId);
            /*if (felm == null)
            {*/
                if (LstFigurines != null)
                {
                    for (int i = 0; i < LstFigurines.Count; ++i)
                        if (LstFigurines[i] != null)
                        {
                            (felm, conteneur) = (LstFigurines[i] as Element).MousePickAvecContAt(netId);
                            if (felm != null)
                            {
                                if (conteneur != null) return (felm, conteneur);
                                else return (felm, this);
                            }
                        }
                }

                if (LstElements != null)
                {
                    for (int i = 0; i < LstElements.Count; ++i)
                        if (LstElements[i] != null)
                        {
                            (felm, conteneur) = LstElements[i].MousePickAvecContAt(netId);
                            if (felm != null)
                            {
                                if (conteneur != null) return (felm, conteneur);
                                else return (felm, this);
                            }
                        }
                }

                return (null, null);
            /*}
            else return (felm, conteneur);*/
        }

        public override (Element, Element) MousePickAvecContAt(PointF mp, float angle, EPickUpAction action = 0)
        {
            mp.X -= GC.P.X;
            mp.Y -= GC.P.Y;

            if (LstFigurines != null)
            {
                for (int i = 0; i < LstFigurines.Count; ++i)
                    if (LstFigurines[i] != null)
                    {
                        Element felm, conteneur;
                        (felm, conteneur) = (LstFigurines[i] as Element).MousePickAvecContAt(mp, angle, action);
                        if (felm != null)
                        {
                            if (conteneur != null) return (felm, conteneur);
                            else return (felm, this);
                        }
                    }
            }

            if (LstElements != null)
            {
                for (int i = 0; i < LstElements.Count; ++i)
                    if (LstElements[i] != null)
                    {
                        Element felm, conteneur;
                        (felm, conteneur) = LstElements[i].MousePickAvecContAt(mp, angle, action);
                        if (felm != null)
                        {
                            if (conteneur != null) return (felm, conteneur);
                            else return (felm, this);
                        }
                    }
            }
            return (null, null);
        }

        public override Element MousePickAt(int netId)
        {
            Element elm = base.MousePickAt(netId);
            if(elm == null)
            {
                if (LstFigurines != null)
                {
                    for (int i = 0; i < LstFigurines.Count; ++i)
                        if (LstFigurines[i] != null)
                        {
                            Element felm = (LstFigurines[i] as Element).MousePickAt(netId);
                            if (felm != null) return felm;
                        }
                }

                if (LstElements != null)
                {
                    for (int i = 0; i < LstElements.Count; ++i)
                        if (LstElements[i] != null)
                        {
                            Element felm = LstElements[i].MousePickAt(netId);
                            if (felm != null) return felm;
                        }
                }

                return null;
            }
            else return elm;
        }

        public override Element MousePickAt(PointF mp, float angle, EPickUpAction action = 0)
        {
            mp.X -= GC.P.X;
            mp.Y -= GC.P.Y;

            if (LstFigurines != null)
            {
                for (int i = 0; i < LstFigurines.Count; ++i)
                    if (LstFigurines[i] != null)
                    {
                        Element felm = (LstFigurines[i] as Element).MousePickAt(mp, angle, action);
                        if (felm != null) return felm;
                    }
            }

            if (LstElements != null)
            {
                for (int i=0;i< LstElements.Count; ++i)
                    if (LstElements[i] != null)
                    {
                        Element felm = LstElements[i].MousePickAt(mp, angle, action);
                        if (felm != null)return felm;
                    }
            }
            return null;
        }

        /*public override Element MousePiocheAt(PointF mp, float angle)
        {
            mp.X -= GC.P.X;
            mp.Y -= GC.P.Y;

            if (LstFigurines != null)
            {
                for (int i = 0; i < LstFigurines.Count; ++i)
                    if (LstFigurines[i] != null)
                    {
                        Element felm = (LstFigurines[i] as Element).MousePiocheAt(mp, angle);
                        if (felm != null)
                        {
                            if (LstFigurines[i] == felm)
                            {
                                LstFigurines.RemoveAt(i);
                                if (LstFigurines.Count == 0) LstFigurines = null;
                                if (felm is Paquet)
                                {
                                    LstElements.Remove(felm);
                                    if (LstElements.Count == 0) LstElements = null;
                                }
                            }
                            felm.GC.P.X += GC.P.X;
                            felm.GC.P.Y += GC.P.Y;
                            return felm;
                        }
                    }
            }

            if (LstElements != null)
            {
                for (int i = 0; i < LstElements.Count; ++i)
                    if (LstElements[i] != null)
                    {
                        Element felm = LstElements[i].MousePiocheAt(mp, angle);
                        if (felm != null)
                        {
                            if(LstElements[i] == felm)
                            {
                                LstElements.RemoveAt(i);
                                if (LstElements.Count == 0) LstElements = null;
                            }
                            felm.GC.P.X += GC.P.X;
                            felm.GC.P.Y += GC.P.Y;
                            return felm;
                        }
                    }
            }
            return null;
        }*/

        public override Element ElementLaché(Element elm)
        {
            if (elm is Figurine == false) Add(elm);
            if(elm is IFigurine) AddFigurine(elm as IFigurine);
            return null;
        }

        public Element Fusionner(Element elm)
        {
            if (elm != null)
            {
                if (elm is Groupe) Fusionner(elm as Groupe);
                else
                {
                    elm.GC.P.X -= GC.P.X;
                    elm.GC.P.Y -= GC.P.X;
                    if(elm is IFigurine)
                    {
                        if (LstFigurines == null) LstFigurines = new List<IFigurine>();
                        AddFigurine(elm as IFigurine);
                    }
                    if (elm is Figurine == false)
                    {
                        if (LstElements == null) LstElements = new List<Element>();
                        LstElements.Add(elm);
                        PutAt(LstElements.Count - 1, trouverOrdreIdx(elm.Ordre));
                    }
                }
            }
            return null;
        }

        public Groupe Fusionner(Groupe pq)
        {
            if (pq!=null)
            {
                if (pq.LstFigurines != null && pq.LstFigurines.Count > 0)
                {
                    foreach (Element elm in pq.LstFigurines)
                    {
                        elm.GC.P.X += pq.GC.P.X;
                        elm.GC.P.Y += pq.GC.P.Y;

                        elm.GC.P.X -= GC.P.X;
                        elm.GC.P.Y -= GC.P.Y;
                    }

                    if (LstFigurines != null)
                    {
                        LstFigurines.AddRange(pq.LstFigurines);
                        pq.LstFigurines.Clear();
                    }
                    else LstFigurines = pq.LstFigurines;
                    pq.LstFigurines = null;
                    if (LstFigurines != null && LstFigurines.Count == 0)
                        LstFigurines = null;
                    TrierZFigurine(ancienAngleDessin);
                }
                if (pq.LstElements != null && pq.LstElements.Count > 0)
                {
                    foreach (Element elm in pq.LstElements)
                    {
                        elm.GC.P.X += pq.GC.P.X;
                        elm.GC.P.Y += pq.GC.P.Y;

                        elm.GC.P.X -= GC.P.X;
                        elm.GC.P.Y -= GC.P.Y;
                    }

                    if (LstElements == null) LstElements = new List<Element>();
                    /*int idxToTop = LstElements.Count;
                    LstElements.AddRange(pq.LstElements);*/
                    int idxToTop = 0;
                    foreach(Element elm in pq.LstElements)
                    {
                        if (elm is Groupe) Fusionner(elm as Groupe);
                        else
                        {
                            ++idxToTop;
                            LstElements.Add(elm);
                        }
                    }
                    for (int i = 0; idxToTop + i < LstElements.Count; ++i)
                    {
                        PutAt(idxToTop + i, i);
                    }
                    pq.LstElements.Clear();

                    pq.LstElements = null;
                    if (LstElements != null && LstElements.Count == 0)
                        LstElements = null;

                    LstElements.Sort();
                }
            }
            return null;
        }

        public Element Add(Element elm)
        {
            if (elm != null && (elm is Groupe) == false)
            {
                elm.GC.P.X -= GC.P.X;
                elm.GC.P.Y -= GC.P.Y;
                if (elm is IFigurine)
                {
                    AddFigurine(elm as IFigurine);

                }
                if (elm is Figurine == false)
                {
                    if (LstElements == null) LstElements = new List<Element>();
                    LstElements.Add(elm);
                    if (LstElements.Count > 1)
                        PutAt(LstElements.Count - 1, trouverOrdreIdx(elm.Ordre));
                }
                return null;
            }
            else return elm;
        }

        public Element AddBack(Element elm)
        {
            if (elm != null && (elm is Groupe) == false)
            {
                elm.GC.P.X -= GC.P.X;
                elm.GC.P.Y -= GC.P.Y;
                if (elm is IFigurine)
                {
                    AddFigurine(elm as IFigurine);

                }
                if (elm is Figurine == false)
                {
                    if (LstElements == null) LstElements = new List<Element>();
                    LstElements.Add(elm);
                }
                return null;
            }
            else return elm;
        }

        private int trouverOrdreIdx(sbyte ordre)
        {
            int inf = 0;
            int sup = LstElements.Count;
            while(inf < sup)
            {
                int mid = (inf / sup) / 2;
                sbyte midOrdre = LstElements[mid].Ordre;
                if (midOrdre == ordre)
                    inf = sup = mid;
                else if (midOrdre > ordre)
                {
                    if (mid == inf) inf = sup;
                    else inf = mid;
                }
                else //if((ordre > midOrdre))
                {
                    if (mid == sup) sup = inf;
                    else sup = mid;
                }
            }
            return inf;
        }

        /*private int trouverZOrdreIdx(float ordre)
        {
            int inf = 0;
            int sup = LstFigurines.Count;
            while (inf < sup)
            {
                int mid = (inf / sup) / 2;
                float midOrdre = LstFigurines[mid].Z;
                if (midOrdre == ordre)
                    inf = sup = mid;
                else if (midOrdre > ordre)
                {
                    if (mid == inf) inf = sup;
                    else inf = mid;
                }
                else //if((ordre > midOrdre))
                {
                    if (mid == sup) sup = inf;
                    else sup = mid;
                }
            }
            return inf;
        }*/

        public Element AddTop(Element elm)
        {
            if (elm != null && (elm is Groupe) == false)
            {
                elm.GC.P.X -= GC.P.X;
                elm.GC.P.Y -= GC.P.Y;
                if (elm is IFigurine)
                {
                    AddFigurine(elm as IFigurine);
                }
                if (elm is Figurine == false)
                {
                    if (LstElements == null) LstElements = new List<Element>();
                    LstElements.Add(elm);
                    if (LstElements.Count > 1) PutAt(LstElements.Count - 1, 0);
                }
                return null;
            }
            else return elm;
        }

        private void PutAt(int ielem, int at)
        {
            if (LstElements != null && ielem < LstElements.Count)
            {
                if (at < 0) at = 0;
                else if (LstElements.Count <= at) at = LstElements.Count - 1;

                if (ielem != at)
                {
                    Element elm = LstElements[ielem];

                    if (at < ielem)
                        for (int i = ielem; i > at; --i)
                            LstElements[i] = LstElements[i - 1];
                    else if (at > ielem)
                        for (int i = ielem; i < at; ++i)
                            LstElements[i] = LstElements[i + 1];

                    LstElements[at] = elm;
                    MettreAJourOrdre(at);
                }
            }
        }

        private bool PutAt(Element elm, int idxD)
        {
            if (LstElements != null)
            {
                int idx = LstElements.IndexOf(elm);
                if (idx >= 0)
                {
                    PutAt(idx, idxD);
                    return true;
                }
                else return false;
            }
            else return false;
        }

        private void PutTop(int idx)
        {
            PutAt(idx, 0);
        }

        private bool PutTop(Element elm)
        {
            return PutAt(elm, 0);
        }

        private void PutFigAt(int ielem, int at)
        {
            if (LstFigurines != null && ielem < LstFigurines.Count)
            {
                if (at < 0) at = 0;
                else if (LstFigurines.Count <= at) at = LstFigurines.Count - 1;

                if (ielem != at)
                {
                    IFigurine elm = LstFigurines[ielem];

                    if (at < ielem)
                        for (int i = ielem; i > at; --i)
                            LstFigurines[i] = LstFigurines[i - 1];
                    else if (at > ielem)
                        for (int i = ielem; i < at; ++i)
                            LstFigurines[i] = LstFigurines[i + 1];

                    LstFigurines[at] = elm;
                }
            }
        }

        public override void MajEtat(EEtat nouvEtat)
        {
            if(AEtatChangé(EEtat.À_l_envers, nouvEtat))
            {
                if (LstFigurines != null)
                {
                    foreach (Element elm in LstFigurines)
                        elm.Retourner();
                }
                if (LstElements != null)
                {
                    foreach (Element elm in LstElements)
                        elm.Retourner();
                }
            }
            base.MajEtat(nouvEtat);
        }

        public override bool Roulette(int delta)
        {
            return false;
        }

        /*public override Element MouseRangerAt(PointF mp, float angle)
        {
            mp.X -= GC.P.X;
            mp.Y -= GC.P.Y;

            if (LstFigurines != null)
            {
                for (int i = 0; i < LstFigurines.Count; ++i)
                    if (LstFigurines[i] != null)
                    {
                        Element elm = (LstFigurines[i] as Element).MouseRangerAt(mp, angle);
                        if (LstFigurines[i] == elm)
                        {
                            LstFigurines.RemoveAt(i);
                            if (LstFigurines.Count == 0) LstFigurines = null;
                            if (elm is Paquet)
                            {
                                LstElements.Remove(elm);
                                if (LstElements.Count == 0) LstElements = null;
                            }
                            return elm;
                        }
                    }
            }
            if (LstElements != null)
            {
                for (int i = 0; i < LstElements.Count; ++i)
                    if (LstElements[i] != null)
                    {
                        Element elm = LstElements[i].MouseRangerAt(mp, angle);
                        if(LstElements[i] == elm)
                        {
                            LstElements.RemoveAt(i);
                            if (LstElements.Count == 0) LstElements = null;
                            return elm;
                        }
                    }
            }
            return null;
        }*/

        public override Element RangerVersParent(Element parent)
        {
            if (LstFigurines != null)
            {
                for (int i = 0; i < LstFigurines.Count; ++i)
                    if (LstFigurines[i] != null)
                    {
                        Element elm = (LstFigurines[i] as Element).RangerVersParent(parent);
                        if (LstFigurines[i] == elm)
                        {
                            LstFigurines.RemoveAt(i);
                            --i;
                        }
                    }
            }
            if (LstElements != null)
            {
                for (int i = 0; i < LstElements.Count; ++i)
                    if (LstElements[i] != null)
                    {
                        Element elm = LstElements[i].RangerVersParent(parent);
                        if (LstElements[i] == elm)
                        {
                            LstElements.RemoveAt(i);
                            --i;
                        }
                    }
            }
            return null;
        }

        public override Element DéfausserElement(Element relem)
        {
            if (LstFigurines != null)
            {
                for (int i = 0; i < LstFigurines.Count; ++i)
                    if (LstFigurines[i] != null)
                    {
                        Element elm = (LstFigurines[i] as Element).DéfausserElement(relem);
                        if (elm != null)
                        {
                            if (LstFigurines[i] == elm)
                                LstFigurines.RemoveAt(i);
                            return elm;
                        }
                        else if (LstFigurines[i] == relem)
                            return relem;
                    }
            }
            if (LstElements != null)
            {
                for (int i = 0; i < LstElements.Count; ++i)
                    if (LstElements[i] != null)
                    {
                        Element elm = LstElements[i].DéfausserElement(relem);
                        if (elm != null)
                        {
                            if (LstElements[i] == elm)
                                LstElements.RemoveAt(i);
                            return elm;
                        }
                        else if (LstElements[i] == relem)
                            return relem;
                    }
            }
            return null;
        }

        public override Element DétacherElement(Element relem)
        {
            Element belm;
            belm = base.DétacherElement(relem);
            if (belm != null) return belm;
            else 
            {
                if (LstFigurines != null)
                {
                    for (int i = 0; i < LstFigurines.Count; ++i)
                        if (LstFigurines[i] != null)
                        {
                            Element felm = (LstFigurines[i] as Element).DétacherElement(relem);
                            if (felm != null)
                            {
                                if (LstFigurines[i] == felm)
                                {
                                    LstFigurines.RemoveAt(i);
                                    if (LstFigurines.Count == 0) LstFigurines = null;
                                    if (felm is Paquet)
                                    {
                                        LstElements.Remove(felm);
                                        if (LstElements.Count == 0) LstElements = null;
                                    }
                                }
                                felm.GC.P.X += GC.P.X;
                                felm.GC.P.Y += GC.P.Y;
                                return felm;
                            }
                        }
                }

                if (LstElements != null)
                {
                    for (int i = 0; i < LstElements.Count; ++i)
                        if (LstElements[i] != null)
                        {
                            Element felm = LstElements[i].DétacherElement(relem);
                            if (felm != null)
                            {
                                if (LstElements[i] == felm)
                                {
                                    LstElements.RemoveAt(i);
                                    if (LstElements.Count == 0) LstElements = null;
                                }
                                felm.GC.P.X += GC.P.X;
                                felm.GC.P.Y += GC.P.Y;
                                return felm;
                            }
                        }
                }
                //if (LstFigurines != null)
                //{
                //    for (int i = 0; i < LstFigurines.Count; ++i)
                //        if (LstFigurines[i] != null)
                //        {
                //            elm = (LstFigurines[i] as Element).DétacherElement(relem);
                //            if (elm != null)
                //            {
                //                if (LstFigurines[i] == elm)
                //                    LstFigurines.RemoveAt(i);
                //                elm.GC.P.X += GC.P.X;
                //                elm.GC.P.Y += GC.P.Y;
                //                return elm;
                //            }
                //            else if (LstFigurines[i] == relem)
                //            {
                //                LstFigurines.RemoveAt(i);
                //                relem.GC.P.X += GC.P.X;
                //                relem.GC.P.Y += GC.P.Y;
                //                return relem;
                //            }
                //        }
                //}
                //if (LstElements != null)
                //{
                //    for (int i = 0; i < LstElements.Count; ++i)
                //        if (LstElements[i] != null)
                //        {
                //            elm = LstElements[i].DétacherElement(relem);
                //            if (elm != null)
                //            {
                //                if (LstElements[i] == elm)
                //                    LstElements.RemoveAt(i);
                //                elm.GC.P.X += GC.P.X;
                //                elm.GC.P.Y += GC.P.Y;
                //                return elm;
                //            }
                //            else if (LstElements[i] == relem)
                //            {
                //                LstElements.RemoveAt(i);
                //                relem.GC.P.X += GC.P.X;
                //                relem.GC.P.Y += GC.P.Y;
                //                return relem;
                //            }
                //        }
                //}
                return null;
            }
        }

        public override bool PutOnTop(Element elm)
        {
            if (LstElements != null)
            {
                for(int i = 0; i< LstElements.Count; ++i)
                    if (LstElements[i] != null && LstElements[i].PutOnTop(elm) && LstElements[i] == elm)
                    {
                        PutTop(i);
                        return true;
                    }
            }
            return false;
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            return _lier(paq.ChildNodes, dElements);
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
                    Element e = LstElements[i];
                    if (e != null)
                    {
                        res = e.MettreAJour(numElm, elm);
                        if (e == res) LstElements[i] = elm;
                    }
                }
                return res;
            }
        }

        public override Element Suppression(Element elm)
        {
            Element re;
            re = base.Suppression(elm);
            if (re != null) return re;
            else
            {
                if (LstFigurines != null && LstFigurines.Count > 0)
                {
                    bool isOwn = false;
                    re = null;
                    foreach (Element e in LstFigurines)
                    {
                        re = e.Suppression(elm);
                        if (re != null)
                        {
                            isOwn = Object.ReferenceEquals(re, e);
                            break;
                        }
                        /*if(re == null) re = e.Suppression(elm);
                        else e.Suppression(elm);
                        if (re != null) return re;*/
                    }
                    if (isOwn) LstFigurines.Remove(re as IFigurine);
                    return re;
                }
                if (LstElements != null && LstElements.Count > 0)
                {
                    bool isOwn = false;
                    re = null;
                    foreach (Element e in LstElements)
                    {
                        re = e.Suppression(elm);
                        if (re != null)
                        {
                            isOwn = Object.ReferenceEquals(re, e);
                            break;
                        }
                        /*if(re == null) re = e.Suppression(elm);
                        else e.Suppression(elm);
                        if (re != null) return re;*/
                    }
                    if (isOwn) LstElements.Remove(re);
                    return re;
                }
                return null;
            }
        }

        public void Netoyer()
        {
            if(LstElements != null)
            {
                LstElements.Clear();
                LstElements = null;
            }
            if (LstFigurines != null)
            {
                LstFigurines.Clear();
                LstFigurines = null;
            }
        }

        override public object Clone()
        {
            return new Groupe(this);
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj)) return true;
            else if (obj is Groupe)
            {
                Groupe elm = obj as Groupe;
                if (LstFigurines != elm.LstFigurines && LstFigurines != null && elm.LstFigurines != null && LstFigurines.Count == elm.LstFigurines.Count)
                {
                    if (LstElements != null && elm.LstElements != null && LstElements.Count == elm.LstElements.Count)
                    {
                        foreach (IFigurine e in LstFigurines)
                            if (elm.LstFigurines.Contains(e) == false) return false;
                    }
                    else return false;
                }
                if (LstElements != elm.LstElements)
                {
                    if (LstElements != null && elm.LstElements != null && LstElements.Count == elm.LstElements.Count)
                    {
                        foreach (Element e in LstElements)
                            if (elm.LstElements.Contains(e) == false) return false;
                    }
                    else return false;
                }
                return true;
            }
            else return false;
        }

    }
}
