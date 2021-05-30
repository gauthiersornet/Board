using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots;
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

namespace ModuleBOARD.Elements.Pieces
{
    public interface IFigurine
    {
        float Z { get; set; }
        void DessinerFigurine(RectangleF vue, float angle, Graphics g, PointF p);
    }
    public class FigurineZComparer : IComparer<IFigurine>
    {
        public int Compare(IFigurine x, IFigurine y)
        {
            return (int)(y.Z - x.Z);
        }
    }
    public class Figurine : Element, IFigurine
    {
        public Model2_5D model2_5D;

        public bool à_l_endroit { get => !EstDansEtat(EEtat.À_l_envers); set => AssignerEtat(EEtat.À_l_envers, !value); }

        public bool Couchée { get => EstDansEtat(EEtat.Couché); set => AssignerEtat(EEtat.Couché, value); }
        public override EType ElmType { get => EType.Figurine; }

        public float Z { get; set; }

        public Figurine(Model2_5D _model2_5D)
        {
            model2_5D = _model2_5D;
            à_l_endroit = true;
            Couchée = false;
        }

        public Figurine(Figurine fig)
            :base(fig)
        {
            model2_5D = fig.model2_5D;
            à_l_endroit = fig.à_l_endroit;
        }

        public Figurine(string path, XmlNode paq, PointF p, BibliothèqueImage bibliothèqueImage, BibliothèqueModel bibliothèqueModel)
        {
            à_l_endroit = true;
            Couchée = false;
            base.Load(paq);

            GC.P.X += p.X;
            GC.P.Y += p.Y;

            model2_5D = bibliothèqueModel.ChargerModel2_5D(path, paq, bibliothèqueImage);
        }

        public override PointF Size => GC.ProjSize(model2_5D.Size(GC.A, à_l_endroit));

        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {
            GeoCoord2D gc = GC;
            gc.P.X += p.X;
            gc.P.Y += p.Y;

            Matrix m = g.Transform;
            bool mirrorX = false, mirrorY = false;
            Image img;
            RectangleF rec = new RectangleF();
            if (Couchée)
            {
                //gc.A = (gc.A + angle + 180.0f) % 360.0f;
                gc.A = (gc.A + 180.0f) % 360.0f;

                //img = model2_5D?.ObtenirImage(0.0f , à_l_endroit, out mirrorX, out mirrorY);
                img = model2_5D?.ObtenirImage(gc.A, à_l_endroit, out mirrorX, out mirrorY);
                if (img != null)
                {
                    g.TranslateTransform(gc.P.X, gc.P.Y);
                    gc.P.X = 0.0f;
                    gc.P.Y = 0.0f;
                    //g.RotateTransform(gc.A);
                    g.RotateTransform(-angle);

                    gc.A = 90.0f;
                    if (mirrorX)
                    {
                        rec.X = -img.Width / 2;
                        rec.Width = img.Width;
                    }
                    else
                    {
                        rec.X = img.Width / 2;
                        rec.Width = -img.Width;
                    }
                    if (mirrorY)
                    {
                        rec.Y = img.Height / 2;
                        rec.Height = -img.Height;
                    }
                    else
                    {
                        rec.Y = -img.Height / 2;
                        rec.Height = img.Height;
                    }
                }
                img.Dessiner(vue, g, gc, rec);
            }
            else
            {
                gc.A = (gc.A + angle) % 360.0f;

                img = model2_5D?.ObtenirImage(gc.A, à_l_endroit, out mirrorX, out mirrorY);
                if (img != null)
                {
                    g.TranslateTransform(gc.P.X, gc.P.Y);
                    gc.P.X = 0.0f;
                    gc.P.Y = 0.0f;
                    g.RotateTransform(-angle);

                    gc.A = 0.0f;
                    if (mirrorX)
                    {
                        rec.X = img.Width / 2;
                        rec.Width = -img.Width;
                    }
                    else
                    {
                        rec.X = -img.Width / 2;
                        rec.Width = img.Width;
                    }
                    if (mirrorY)
                    {
                        rec.Y = ((1 * img.Height) / 4);
                        rec.Height = -img.Height;
                    }
                    else
                    {
                        rec.Y = -((3 * img.Height) / 4);
                        rec.Height = img.Height;
                    }
                }
                img.Dessiner(vue, g, gc, rec);
            }
            g.Transform = m;
        }

        public void DessinerFigurine(RectangleF vue, float angle, Graphics g, PointF p)
        {
            Dessiner(vue, angle, g, p);
        }

        override public bool IsAt(PointF mp, float angle)
        {
            return IsAt(mp, GC.ProjSize(model2_5D.Size((Couchée ? (GC.A + 180.0f) % 360.0f : GC.A), à_l_endroit)), angle);
        }

        override public bool IsAt(PointF mp, PointF psz, float angle)
        {
            psz.X /= 2.0f;

            mp.X -= GC.P.X/* - (psz.X / 2.0f)*/;
            mp.Y -= GC.P.Y/* - (psz.Y / 2.0f)*/;

            Matrix m = new Matrix();
            //m.Rotate(Couchée ? GC.A : - angle);
            if(Couchée) m.Rotate(90.0f - angle);
            else m.Rotate(- angle);
            PointF nmp = new PointF
                (
                    mp.X * m.Elements[0] + mp.Y * m.Elements[1],
                    mp.X * m.Elements[2] + mp.Y * m.Elements[3]
                );

            if (Couchée)
            {
                psz.Y /= 2.0f;
                return (-psz.X <= nmp.X && -psz.Y <= nmp.Y && nmp.X <= psz.X && nmp.Y <= psz.Y);
            }
            else
            {
                float y = -((3 * psz.Y) / 4);
                return (-psz.X <= nmp.X && y <= nmp.Y && nmp.X <= psz.X && nmp.Y <= (y + psz.Y));
            }
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            return true;
        }
        
        public override bool PutOnTop(Element elm)
        {
            return (this == elm);
        }

        public override bool Roulette(int delta)
        {
            int ia = (int)(((GC.A % 360.0f) / 45.0f) + 0.5f);

            if (delta > 0) ia = (ia + delta) % 8;
            else if(delta < 0) ia = (ia + (1 - 8) * delta) % 8;

            GC.A = 45.0f * ia;
            return true;
        }

        public override (Element, Element) MousePickAvecContAt(PointF mp, float angle, EPickUpAction action)
        {
            if (action.HasFlag(EPickUpAction.Roulette)) action |= EPickUpAction.Tourner;
            return base.MousePickAvecContAt(mp, angle, action);
        }

        public override Element MousePickAt(PointF mp, float angle, EPickUpAction action)
        {
            if (action.HasFlag(EPickUpAction.Roulette)) action |= EPickUpAction.Tourner;
            return base.MousePickAt(mp, angle, action);
        }

        override public object Clone()
        {
            return new Figurine(this);
        }

        /*override public ContextMenu Menu(Control ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();
            //cm.MenuItems.Add("Ranger", new EventHandler((o, e) => { Board.RangerElement(this); }));
            if(this.Couchée) cm.MenuItems.Add("Relever", new EventHandler((o, e) => { this.Couchée = false; ctrl.Refresh(); }));
            else cm.MenuItems.Add("Coucher", new EventHandler((o, e) => { this.Couchée = true; ctrl.Refresh(); }));
            return cm;
        }*/
        override public ContextMenu Menu(IBoard ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();

            cm.MenuItems.AddRange(new MenuItem[]
                    {
                        new MenuItem("Mettre en paquet", new EventHandler((o,e) => {ctrl.MettreEnPaquet(this); ctrl.Refresh(); })),
                    });
            return cm;
        }

        public Figurine(Stream stream, IRessourcesDésérialiseur resscDes)
            : base(stream, resscDes)
        {
            model2_5D = resscDes.RécupérerModel(stream);
        }

        override public void Serialiser(Stream stream, ref int gidr)
        {
            base.Serialiser(stream, ref gidr);
            stream.SerialiserObject(model2_5D?.Tag as string ?? "", typeof(string));
        }

        override public void SerialiserTout(Stream stream, ref int gidr, ISet<int> setIdRéseau)
        {
            base.SerialiserTout(stream, ref gidr, setIdRéseau);
        }
    }
}
