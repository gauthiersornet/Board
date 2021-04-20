using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;

namespace ModuleBOARD.Elements.Base
{
    static public class Outils
    {
        static public Rectangle Rect(this Image img) { return new Rectangle(0, 0, img.Width, img.Height); }
        static public RectangleF RectF(this Image img) { return new RectangleF(0, 0, img.Width, img.Height); }

        static public Matrix Dessiner(this Image img, RectangleF vue, Graphics g, GeoCoord2D GC, RectangleF dRect)
        {
            Matrix res;
            /*g.ResetTransform();
            g.RotateTransform(10.0f);*/

            if (img != null)
            {
                float sclRatio = GC.E / Math.Min(img.Width, img.Height);

                Matrix m = g.Transform;
                g.TranslateTransform(GC.P.X, GC.P.Y);
                g.RotateTransform(GC.A);
                g.ScaleTransform(sclRatio, sclRatio);
                g.DrawImage(img, dRect, new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                res = g.Transform;
                g.Transform = m;
                //g.FillRectangle(new SolidBrush(Color.Black), drect);
            }
            else res = null;

            return res;
        }

        static public Matrix Dessiner(this Image img, RectangleF vue, Graphics g, GeoCoord2D GC)
        {
            Matrix res;
            /*g.ResetTransform();
            g.RotateTransform(10.0f);*/

            if (img != null)
            {
                float sclRatio = GC.E / Math.Min(img.Width, img.Height);
                RectangleF dRect = new RectangleF(-img.Width / 2, -img.Height / 2, img.Width, img.Height);

                Matrix m = g.Transform;
                g.TranslateTransform(GC.P.X, GC.P.Y);
                g.RotateTransform(GC.A);
                g.ScaleTransform(sclRatio, sclRatio);
                g.DrawImage(img, dRect, new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                res = g.Transform;
                g.Transform = m;
                //g.FillRectangle(new SolidBrush(Color.Black), drect);
            }
            else res = null;

            return res;
        }

        static public Matrix DessinerVide(this Image img, RectangleF vue, Graphics g, GeoCoord2D GC)
        {
            Matrix res;
            if (img.Width > 0 && img.Height > 0)
            {
                float sclRatio = Math.Min(img.Width, img.Height);
                RectangleF dRect = new RectangleF(-img.Width / 2, -img.Height / 2, img.Width, img.Height);

                Matrix m = g.Transform;
                sclRatio = GC.E / sclRatio;
                g.TranslateTransform(GC.P.X, GC.P.Y);
                g.RotateTransform(GC.A);
                g.ScaleTransform(sclRatio, sclRatio);
                g.FillRectangle(new SolidBrush(Color.Gray), dRect);
                res = g.Transform;
                g.Transform = m;
            }
            else res = null;
            return res;
        }

        static public Matrix DessinerVide(this Point img, RectangleF vue, Graphics g, GeoCoord2D GC)
        {
            Matrix res;
            if (img.X > 0 && img.Y > 0)
            {
                float sclRatio = Math.Min(img.X, img.Y);
                RectangleF dRect = new RectangleF(-img.X / 2, -img.Y / 2, img.X, img.Y);

                Matrix m = g.Transform;
                sclRatio = GC.E / sclRatio;
                g.TranslateTransform(GC.P.X, GC.P.Y);
                g.RotateTransform(GC.A);
                g.ScaleTransform(sclRatio, sclRatio);
                g.FillRectangle(new SolidBrush(Color.Gray), dRect);
                res = g.Transform;
                g.Transform = m;
            }
            else res = null;
            return res;
        }

        static public Matrix DessinerVide(this RectangleF dRect, RectangleF vue, Graphics g, GeoCoord2D GC)
        {
            Matrix res;
            if (dRect.Width > 0 && dRect.Height > 0)
            {
                float sclRatio = Math.Min(dRect.Width, dRect.Height);
                if (sclRatio < 0.0f) sclRatio = -sclRatio;

                Matrix m = g.Transform;
                sclRatio = GC.E / sclRatio;
                g.TranslateTransform(GC.P.X, GC.P.Y);
                g.RotateTransform(GC.A);
                g.ScaleTransform(sclRatio, sclRatio);
                g.FillRectangle(new SolidBrush(Color.Gray), dRect);
                res = g.Transform;
                g.Transform = m;
            }
            else res = null;
            return res;
        }
    }
}
