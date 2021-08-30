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
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace ModuleBOARD.Elements.Lots.Piles
{
    public class Pile : Element
    {
        static private float CEpaisseur = 0.5f;
        static private float CroixEpaisseur = 5.0f;

        public Color CouleurTranche = Color.Black;
        public Image PileVide = default;
        //Dessous (visible) 0 ... n-1 Dessus (caché)
        public List<KeyValuePair<Image, Image>> Images = null;
        public int Courrante = -1;

        //public override bool EstParent { get => true; }
        public override EType ElmType { get => EType.Pile; }

        private float Epaisseur
        {
            get
            {
                if (Images != null && Images.Count > 0)
                    return CEpaisseur * Images.Count;
                else return 0.0f;
            }
        }

        public override PointF Size
        {
            get
            {
                float ep = Epaisseur;

                /*if (Images != null)
                {
                    if (0 <= Courrante && Courrante < Images.Count && Images[Courrante].Value != null)
                        imgref = Images[Courrante].Value;
                    else imgref = PileVide;
                }
                else imgref = PileVide;*/
                Image imgref;
                if (Images != null && Images.Any())
                {
                    KeyValuePair<Image, Image> kv = Images.FirstOrDefault(x => x.Key != null || x.Value != null);
                    imgref = kv.Key ?? kv.Value ?? PileVide;
                }
                else imgref = PileVide;

                PointF sz;
                if (imgref != null) sz = GC.ProjSize(imgref.Rect());
                else sz = GC.ProjSize(new Rectangle(0, 0, 20, 20));

                sz.X += ep;
                sz.Y += ep;
                return sz;
            }
        }

        protected Pile() { }

        protected Pile(int idREz) : base(idREz) { }

        public Pile(Element elm) : base(elm) { }

        public Pile(Element2D element2D) : base(element2D)
        {
            Parent = null;
            PileVide = element2D.ElmImage;
            Images = new List<KeyValuePair<Image, Image>>() { new KeyValuePair<Image, Image>(null, element2D.ElmImage) };
        }

        public Pile(Element2D2F element2D2F) : base(element2D2F)
        {
            Parent = null;
            PileVide = element2D2F.Dos ?? element2D2F.ElmImage;
            Images = new List<KeyValuePair<Image, Image>>() { new KeyValuePair<Image, Image>(null, element2D2F.ElmImage) };
        }

        public Pile(Pile elm)
            :base(elm)
        {
            CouleurTranche = elm.CouleurTranche;
            PileVide = elm.PileVide;
            Images = (elm.Images != null ? new List<KeyValuePair<Image, Image>>(elm.Images) : null);
            Courrante = elm.Courrante;
        }

        public Pile(string path, XmlNode paq, PointF p, BibliothèqueImage bibliothèqueImage)
        {
            base.Load(paq);
            GC.P.X += p.X;
            GC.P.Y += p.Y;
            //Parent = parent;
            string filePV = paq.Attributes?.GetNamedItem("vide")?.Value;
            if(filePV != null) PileVide = bibliothèqueImage.ChargerSImage(path, filePV, paq, "dx", "dy", "w", "h");

            Images = bibliothèqueImage.ChargerCartesImage(path, paq.ChildNodes, PileVide);
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

            if (paq.Attributes?.GetNamedItem("mélanger") != null)
                Mélanger();

            if (paq.Attributes?.GetNamedItem("caché") != null)
                Courrante = -1;
            else if (paq.Attributes?.GetNamedItem("montré") != null)
                Courrante = 0;
            else Courrante = -1;
        }

        override public object MettreAJour(object obj)
        {
            if (obj is Image)
            {
                Image img = obj as Image;
                if (PileVide != null && String.Equals(PileVide.Tag, img.Tag))
                    PileVide = img;
                if (Images != null)
                {
                    for (int i = 0; i < Images.Count; ++i)
                    {
                        if (Images[i].Key != null && String.Equals(Images[i].Key.Tag, img.Tag))
                            Images[i] = new KeyValuePair<Image, Image>(img, Images[i].Value);
                        if (Images[i].Value != null && String.Equals(Images[i].Value.Tag, img.Tag))
                            Images[i] = new KeyValuePair<Image, Image>(Images[i].Key, img);
                    }
                }
                return base.MettreAJour(obj);
            }
            else return base.MettreAJour(obj);
        }

        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {
            GeoCoord2D gc = GC;
            gc.P.X += p.X;
            gc.P.Y += p.Y;
            /*p.X += GC.P.X;
            p.Y += GC.P.Y;*/

            RectangleF rect;
            Matrix m = g.Transform;
            if (Images != null && Images.Count > 0)
            {
                Matrix drawMat;
                float ep = Epaisseur;

                gc.P.X -= ep;
                gc.P.Y -= ep;

                if (Courrante >= 0)
                {
                    if (Courrante >= Images.Count) Courrante = 0;

                    if (Images[Courrante].Value != null)
                    {
                        drawMat = Images[Courrante].Value.Dessiner(vue, g, gc);
                        rect = Images[Courrante].Value.RectF();
                    }
                    else if(PileVide != null)
                    {
                        drawMat = PileVide.DessinerVide(vue, g, gc);
                        rect = PileVide.RectF();
                    }
                    else
                    {
                        drawMat = new Point(20, 20).DessinerVide(vue, g, gc);
                        rect = new RectangleF(0,0,20,20);
                    }
                }
                else
                {
                    /*drawMat = Dos.Dessiner(vue, g, gc);
                    rect = Dos.RectF();*/
                    if (Courrante < -Images.Count) Courrante = -1;

                    int idx = Images.Count + Courrante;
                    if (Images[idx].Key != null)
                    {
                        drawMat = Images[idx].Key.Dessiner(vue, g, gc);
                        rect = Images[idx].Value.RectF();
                    }
                    else if (PileVide != null)
                    {
                        drawMat = PileVide.Dessiner(vue, g, gc);
                        rect = PileVide.RectF();
                    }
                    else
                    {
                        drawMat = new Point(20, 20).DessinerVide(vue, g, gc);
                        rect = new RectangleF(0, 0, 20, 20);
                    }
                }

                if (rect.IsEmpty == false)
                {
                    /*float ep = Epaisseur * echelle;
                      g.FillPolygon(new SolidBrush(CouleurTranche),
                        new PointF[]
                        {
                        new PointF(rect.X+rect.Width, rect.Y),
                        new PointF(rect.X+rect.Width+ep, rect.Y+ep),
                        new PointF(rect.X+rect.Width+ep, rect.Y+rect.Height+ep),
                        new PointF(rect.X+ep, rect.Y+rect.Height+ep),
                        new PointF(rect.X, rect.Y+rect.Height),
                        new PointF(rect.X+rect.Width, rect.Y+rect.Height)
                        }
                    );*/
                    g.Transform = drawMat;

                    float ang = (GC.A + angle) % 360.0f;
                    if (45.0f< ang && ang <= (360.0f-45.0f))
                    {
                        if (ang < (90.0f + 45.0f))
                            g.ScaleTransform(1.0f, -1.0f);
                        else if (ang <= (180.0f + 45.0f))
                            g.ScaleTransform(-1.0f, -1.0f);
                        else//if (GC.A <= (360.0f-45.0f))
                            g.ScaleTransform(-1.0f, 1.0f);
                    }
                    float sclRatio = Math.Min(rect.Width, rect.Height) / GC.E;
                    ep *= sclRatio;
                    rect.Width /= 2.0f; rect.Height /= 2.0f;
                    g.FillPolygon(new SolidBrush(CouleurTranche),
                        new PointF[]
                        {
                            new PointF(rect.Width, -rect.Height),
                            new PointF(rect.Width+ep, -rect.Height+ep),
                            new PointF(rect.Width+ep, rect.Height+ep),
                            new PointF(-rect.Width+ep, rect.Height+ep),
                            new PointF(-rect.Width, rect.Height),
                            new PointF(rect.Width, rect.Height)
                        }
                    );
                }
            }
            else
            {
                Matrix drawMat;
                if(PileVide != null) drawMat = PileVide.Dessiner(vue, g, gc);
                else drawMat = (new Point(20, 20)).DessinerVide(vue, g, gc);
                g.Transform = drawMat;
                if (PileVide != null) rect = PileVide.RectF();
                else rect = new RectangleF(0, 0, 20, 20);
                Pen pn = new Pen(CouleurTranche, CroixEpaisseur);
                /*g.DrawLine(pn, new PointF(rect.X, rect.Y), new PointF(rect.X + rect.Width, rect.Y + rect.Height));
                g.DrawLine(pn, new PointF(rect.X + rect.Width, rect.Y), new PointF(rect.X, rect.Y + rect.Height));*/
                g.DrawLine(pn, new PointF(-rect.Width/2, -rect.Height/2), new PointF(rect.Width/2, rect.Height/2));
                g.DrawLine(pn, new PointF(rect.Width/2, -rect.Height/2), new PointF(-rect.Width/2, rect.Height/2));
            }
            g.Transform = m;
        }

        private bool EstRectEquivalent(Image a, Image b)
        {
            if (a == b) return true;
            else if(a == null) return false;
            else return a.Width * b.Height == a.Height * b.Width;
        }

        public Image TrouverUneImage()
        {
            if (Images != null)
            {
                KeyValuePair<Image, Image> kv = Images.FirstOrDefault(x => x.Key != null || x.Value != null);
                return kv.Key ?? kv.Value ?? PileVide;
            }
            else return PileVide;
        }

        private bool EstCompatible(Element elm)
        {
            if (elm != null && elm.GC.E == GC.E)
            {
                Image imgCmp = TrouverUneImage();

                if (elm is Pile)
                {
                    if (imgCmp == null) return true;
                    Pile ep = elm as Pile;

                    //Même echelle ?
                    return EstRectEquivalent(ep.TrouverUneImage(), imgCmp);
                    /*{
                        KeyValuePair<Image, Image> kvEp = Images.FirstOrDefault(x => x.Key != null || x.Value != null);
                        Image imgCmpEp = kvEp.Key ?? kvEp.Value ?? ep.PileVide;

                        return EstRectEquivalent(imgCmpEp, imgCmp);
                    }*/
                    //else return false;
                }
                else if (elm is Element2D)
                {
                    if (imgCmp == null) return true;

                    Element2D e2d = elm as Element2D;
                    if (EstRectEquivalent(imgCmp, e2d.ElmImage))
                    {
                        if (e2d is Element2D2F)
                        {
                            Element2D2F e2d2d = e2d as Element2D2F;
                            if (e2d2d.Dos != null && e2d2d.ElmImage != null)
                                return EstRectEquivalent(e2d2d.Dos, e2d2d.ElmImage);
                            else return true;
                        }
                        else return true;
                    }
                    else return false;
                }
                else return false;
            }
            else return false;
        }

        public override Element ElementLaché(Element elm)
        {
            if (this != elm && elm.GC.E == GC.E && EstCompatible(elm))
            {
                if (elm is Pile)
                {
                    Pile pl = (elm as Pile);
                    List<KeyValuePair<Image, Image>> imgs = pl.Images;
                    if (imgs != null)
                    {
                        pl.Images = null;
                        if (pl.PileVide != PileVide)
                        {
                            for (int i = 0; i < imgs.Count; ++i)
                            {
                                if (imgs[i].Key == PileVide) imgs[i] = new KeyValuePair<Image, Image>(null, imgs[i].Value);
                                else if (imgs[i].Key == pl.PileVide) imgs[i] = new KeyValuePair<Image, Image>(pl.PileVide, imgs[i].Value);
                            }
                        }
                        AddRange(imgs, false);
                    }
                    return elm;
                }
                else if(elm is Element2D2F)
                {
                    Add(new KeyValuePair<Image, Image>(((elm as Element2D2F).Dos != PileVide ? (elm as Element2D2F).Dos : null), (elm as Element2D2F).ElmImage), false);
                    return null;
                }
                else if (elm is Element2D)
                {
                    Add(new KeyValuePair<Image, Image>(((elm as Element2D).ElmImage != PileVide ? (elm as Element2D).ElmImage : null), (elm as Element2D).ElmImage), false);
                    return null;
                }
                else return elm;
            }
            else return elm;
        }

        override public bool IsAt(PointF mp, float angle)
        {
            float ep = Epaisseur;
            mp.X += ep * 0.5f;
            mp.Y += ep * 0.5f;
            return base.IsAt(mp, angle);
        }

        /*public override Element MousePickAt(PointF mp, float angle)
        {
            if (IsAt(mp, angle)) return this;
            else return null;
        }*/

        public override (Element, Element) MousePickAvecContAt(PointF mp, float angle, EPickUpAction action = 0)
        {
            if (action.HasFlag(EPickUpAction.Attraper))
            {
                action |= EPickUpAction.Piocher;
            }
            else if (action.HasFlag(EPickUpAction.Piocher) && Images != null && Images.Count > 0)
            {
                action &= ~EPickUpAction.Piocher;
            }
            return base.MousePickAvecContAt(mp, angle, action);
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

        //public override Element MousePiocheAt(PointF mp, float angle)
        public override Element MousePioche(int index = int.MaxValue)
        {
            if (Images != null && Images.Count > 0 && 0 <= index && (index< Images.Count || index == int.MaxValue))
            {
                KeyValuePair<Image, Image> kvImg;
                if (index == int.MaxValue) index = GetPiocheIndex();

                kvImg = Images[index];
                Images.RemoveAt(index);

                /*if (0 <= Courrante)
                {
                    if (Courrante >= Images.Count) Courrante = 0;
                    kvImg = Images[Courrante];
                    Images.RemoveAt(Courrante);
                    if (Courrante >= Images.Count)
                    {
                        Courrante = 0;
                        if (Images.Count == 0)
                            Images = null;
                    }
                }
                else
                {
                    Courrante = -1;
                    
                    int pioch = Images.Count - 1;
                    kvImg = Images[pioch];
                    Images.RemoveAt(pioch);
                }*/
                /*GC.P.X += CEpaisseur;
                GC.P.Y += CEpaisseur;*/

                Element2D elm2D;
                if (((kvImg.Key ?? PileVide) != null) && kvImg.Key != kvImg.Value)
                {
                    elm2D = new Element2D2F();
                    (elm2D as Element2D2F).Dos = kvImg.Key ?? PileVide;
                    elm2D.ElmImage = kvImg.Value;
                    (elm2D as Element2D2F).Caché = (Courrante < 0);
                }
                else
                {
                    elm2D = new Element2D();
                    elm2D.ElmImage = kvImg.Value;
                }

                elm2D.GC = GC;
                if (Images != null)
                {
                    elm2D.GC.P.X -= (Images.Count + 1) * CEpaisseur;
                    elm2D.GC.P.Y -= (Images.Count + 1) * CEpaisseur;
                }
                elm2D.Parent = this;

                VérifierCourrante();
                return elm2D;
            }
            else return base.MousePioche();
        }

        public override int GetPiocheIndex()
        {
            if (Images != null && Images.Any())
            {
                VérifierCourrante();
                if (Courrante < 0) return (Images.Count + Courrante);
                else return Courrante;
            }
            else return int.MaxValue;
        }

        public void Mélanger(Random rnd = null)
        {
            if (Images != null)
            {
                if(rnd == null) rnd = new Random();
                for (int i = 0; i < Images.Count; ++i)
                {
                    int swi = rnd.Next(Images.Count);
                    KeyValuePair<Image, Image> simg = Images[i];
                    Images[i] = Images[swi];
                    Images[swi] = simg;
                }
                for (int i = Images.Count-1; i >= 0; --i)
                {
                    int swi = rnd.Next(Images.Count);
                    KeyValuePair<Image, Image> simg = Images[i];
                    Images[i] = Images[swi];
                    Images[swi] = simg;
                }
            }
        }

        protected KeyValuePair<Image, Image> Add(KeyValuePair<Image, Image> elm, bool atBack = true)
        {
            if (elm.Value != null)
            {
                if (Images == null) Images = new List<KeyValuePair<Image, Image>>();

                if (0 <= Courrante && Courrante < Images.Count)
                    Images.Insert(Courrante, elm);
                else
                {
                    if (Images.Count > 0)
                    {
                        if(atBack) Images.Insert(0, elm);
                        else Images.Add(elm);
                    }
                    else Images.Add(elm);
                }
                /*GC.P.X -= CEpaisseur;
                GC.P.Y -= CEpaisseur;*/
                return default;
            }
            else return elm;
        }

        protected KeyValuePair<Image, Image> AddTop(KeyValuePair<Image, Image> elm)
        {
            if (elm.Value != null)
            {
                if (Images == null) Images = new List<KeyValuePair<Image, Image>>();
                Images.Add(elm);
                /*GC.P.X -= CEpaisseur;
                GC.P.Y -= CEpaisseur;*/
                return default;
            }
            else return elm;
        }

        public KeyValuePair<Image, Image> AddBack(KeyValuePair<Image, Image> elm)
        {
            if (elm.Value != null)
            {
                if (Images == null) Images = new List<KeyValuePair<Image, Image>>();
                if (Images.Count > 0) Images.Insert(0, elm);
                else Images.Add(elm);
                /*GC.P.X -= CEpaisseur;
                GC.P.Y -= CEpaisseur;*/
                return default;
            }
            else return elm;
        }

        public List<KeyValuePair<Image, Image>> AddRange(List<KeyValuePair<Image, Image>> elms, bool atBack = true)
        {
            if (elms != null)
            {
                if (Images == null) Images = new List<KeyValuePair<Image, Image>>();

                if (0 <= Courrante && Courrante < Images.Count)
                    Images.InsertRange(Courrante, elms);
                else
                {
                    if (Images.Count > 0)
                    {
                        if(atBack) Images.InsertRange(0, elms);
                        else Images.AddRange(elms);
                    }
                    else Images.AddRange(elms);
                }
                /*GC.P.X -= CEpaisseur;
                GC.P.Y -= CEpaisseur;*/
                return default;
            }
            else return elms;
        }

        protected List<KeyValuePair<Image, Image>> AddTopRange(List<KeyValuePair<Image, Image>> elms)
        {
            if (elms != null)
            {
                if (Images == null) Images = new List<KeyValuePair<Image, Image>>();
                Images.AddRange(elms);
                /*GC.P.X -= CEpaisseur;
                GC.P.Y -= CEpaisseur;*/
                return default;
            }
            else return elms;
        }

        protected List<KeyValuePair<Image, Image>> AddBackRange(List<KeyValuePair<Image, Image>> elms)
        {
            if (elms != null)
            {
                if (Images == null) Images = new List<KeyValuePair<Image, Image>>();
                if (Images.Count > 0) Images.InsertRange(0, elms);
                else Images.AddRange(elms);
                /*GC.P.X -= CEpaisseur;
                GC.P.Y -= CEpaisseur;*/
                return default;
            }
            else return elms;
        }

        /*private void PutAt(int ielem, int at)
        {
            if (Images != null && ielem < Images.Count)
            {
                if (at < 0) at = 0;
                else if (Images.Count <= at) at = Images.Count - 1;

                if (ielem != at)
                {
                    SImage elm = Images[ielem];

                    if (at < ielem)
                        for (int i = ielem; i > at; --i)
                            Images[i] = Images[i - 1];
                    else if (at > ielem)
                        for (int i = ielem; i < at; ++i)
                            Images[i] = Images[i - 1];

                    Images[at] = elm;
                }
            }
        }*/

        /*public bool PutAt(SImage elm, int idxD)
        {
            if (Images != null)
            {
                int idx = Images.IndexOf(elm);
                if (idx >= 0)
                {
                    PutAt(idx, idxD);
                    return true;
                }
                else return false;
            }
            else return false;
        }*/

        /*private void PutTop(int idx)
        {
            PutAt(idx, 0);
        }*/

        /*public bool PutTop(SImage elm)
        {
            return PutAt(elm, 0);
        }*/

        protected void MettreAJourEtat()
        {
            if(EstDansEtat(EEtat.À_l_envers))
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
            if (Images != null && Images.Count>0)
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
                        Courrante = Images.Count-1;
                }
            }
            return true;
        }

        override public ContextMenu Menu(IBoard ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();

            MenuItem nbc = new MenuItem((Images?.Count ?? 0) + " carte(s)");
            int idx;
            for (idx = 0; idx < cm.MenuItems.Count && cm.MenuItems[idx].Text != "État"; ++idx) ;
            if (idx < cm.MenuItems.Count) cm.MenuItems[idx].MenuItems.Add(0, nbc);
            else cm.MenuItems.Add(new MenuItem("État", new MenuItem[1] { nbc }));

            cm.MenuItems.AddRange(new MenuItem []
                    {
                        new MenuItem("Mélanger", new EventHandler((o,e) => { ctrl.Mélanger(this);/*Mélanger(); ctrl.Refresh();*/ })),
                        new MenuItem("Ranger", new EventHandler((o,e) => {ctrl.RangerVersParent(this); }))
                    });
            return cm;
        }

        public override bool PutOnTop(Element elm)
        {
            return (this == elm);
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            return true;
        }

        override public object Clone()
        {
            return new Pile(this);
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj)) return true;
            else if (obj is Pile)
            {
                Pile elm = obj as Pile;
                if((Images == elm.Images || (Images!=null && elm.Images!=null && Images.Count == elm.Images.Count)) && CouleurTranche == elm.CouleurTranche && BibliothèqueImage.Equals(PileVide, elm.PileVide))
                {
                    for(int i = 0;i < Images.Count; ++i)
                        if(Images[i].Key != elm.Images[i].Key || Images[i].Value != elm.Images[i].Value) return false;
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public Pile(Stream stream, IRessourcesDésérialiseur resscDes)
            : base(stream, resscDes)
        {
            Courrante = stream.ReadInt();
            PileVide = resscDes.RécupérerImage(stream);
            ushort nbc = BitConverter.ToUInt16(stream.GetBytes(2), 0);
            if (nbc > 0)
            {
                Images = new List<KeyValuePair<Image, Image>>(nbc);
                for (ushort i = 0; i < nbc; ++i)
                    Images.Add(new KeyValuePair<Image, Image> (resscDes.RécupérerImage(stream), resscDes.RécupérerImage(stream)));
            }
            else Images = null;
        }

        override public void Serialiser(Stream stream, ref int gidr)
        {
            base.Serialiser(stream, ref gidr);
            stream.WriteBytes(BitConverter.GetBytes(Courrante));
            stream.SerialiserObject(PileVide?.Tag as string ?? "", typeof(string));
            ushort nbc = (ushort)(Images?.Count ?? 0);
            stream.WriteBytes(BitConverter.GetBytes(nbc));
            for(ushort i = 0; i < nbc; ++i)
            {
                stream.SerialiserObject(Images[i].Key?.Tag as string ?? "", typeof(string));
                stream.SerialiserObject(Images[i].Value?.Tag as string ?? "", typeof(string));
            }
        }

        /*override public void SerialiserTout(Stream stream, ref int gidr, ISet<int> setIdRéseau)
        {
            base.SerialiserTout(stream, ref gidr, setIdRéseau);
        }*/
    }
}
