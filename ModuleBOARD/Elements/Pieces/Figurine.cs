﻿using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        public bool à_l_endroit;
        public bool Couchée;

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

        public override void Cacher()
        {
            à_l_endroit = false;
        }

        public override void Révéler()
        {
            à_l_endroit = true;
        }

        public override void Retourner()
        {
            à_l_endroit = !à_l_endroit;
        }

        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {
            GeoCoord2D gc = GC;
            gc.P.X += p.X;
            gc.P.Y += p.Y;
            gc.A = (gc.A + 360.0f - angle + (Couchée ? 180.0f : 0)) % 360.0f;

            bool mirrorX = false, mirrorY = false;
            Image img = model2_5D?.ObtenirImage(gc.A, à_l_endroit, out mirrorX, out mirrorY);
            if(img != null)
            {
                Matrix m = g.Transform;
                g.TranslateTransform(gc.P.X, gc.P.Y);
                gc.P.X = 0.0f;
                gc.P.Y = 0.0f;
                g.RotateTransform(-angle);
                RectangleF rec = new RectangleF();

                if (Couchée)
                {
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
                else
                {
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
                g.Transform = m;
            }
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
            m.Rotate(-angle);
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

            delta /= 120;
            if (delta > 0) ia = (ia + delta) % 8;
            else if(delta < 0) ia = (ia + (1 - 8) * delta) % 8;

            GC.A = 45.0f * ia;
            return true;
        }

        override public object Clone()
        {
            return new Figurine(this);
        }

        override public ContextMenu Menu(Control ctrl)
        {
            ContextMenu cm = base.Menu(ctrl);
            if (cm == null) cm = new ContextMenu();
            /*cm.MenuItems.Add("Ranger", new EventHandler((o, e) => { Board.RangerElement(this); }));*/
            if(this.Couchée) cm.MenuItems.Add("Relever", new EventHandler((o, e) => { this.Couchée = false; ctrl.Refresh(); }));
            else cm.MenuItems.Add("Coucher", new EventHandler((o, e) => { this.Couchée = true; ctrl.Refresh(); }));
            return cm;
        }
    }
}
