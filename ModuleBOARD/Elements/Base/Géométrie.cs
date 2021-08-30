using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModuleBOARD.Elements.Base
{
    public struct GeoCoord2D
    {
        public PointF P; //Position 2D
        public float E; //Echelle
        public float A; // Angle en Degré

        public GeoCoord2D(PointF p, float e, float a)
        {
            P = p;
            E = e;
            A = a;
        }

        public GeoCoord2D(float x, float y, float e, float a)
        {
            P = new PointF(x, y);
            E = e;
            A = a;
        }

        public PointF Projection(PointF p)
        {
            Matrix m = new Matrix();
            m.Rotate(A);
            return new PointF
                (
                    P.X + (p.X * m.Elements[0] + p.Y * m.Elements[1]) / E,
                    P.Y + (p.X * m.Elements[2] + p.Y * m.Elements[3]) / E
                );
            //return new PointF(P.X + (p.X / E), P.Y + (p.Y / E));
        }

        public PointF ProjectionDelta(PointF d)
        {
            Matrix m = new Matrix();
            m.Rotate(A);
            return new PointF
                (
                    (d.X * m.Elements[0] + d.Y * m.Elements[1]) / E,
                    (d.X * m.Elements[2] + d.Y * m.Elements[3]) / E
                );
        }

        public PointF ProjectionInv(PointF p)
        {
            PointF pSclInv = new PointF(((p.X - P.X) * E), ((p.Y - P.Y) * E));
            Matrix m = new Matrix();
            m.Rotate(-A);
            return new PointF
                (
                    (pSclInv.X * m.Elements[0] + pSclInv.Y * m.Elements[1]),
                    (pSclInv.X * m.Elements[2] + pSclInv.Y * m.Elements[3])
                );
            //return new PointF( ((p.X - P.X) * E), ((p.Y - P.Y) * E) );
        }

        public PointF ProjectionDeltaInv(PointF d)
        {
            Matrix m = new Matrix();
            m.Rotate(-A);
            return new PointF
                (
                    (d.X * m.Elements[0] + d.Y * m.Elements[1]) * E,
                    (d.X * m.Elements[2] + d.Y * m.Elements[3]) * E
                );
        }

        public PointF ProjSize(Rectangle rect)
        {
            if (rect.Width > 0 && rect.Height > 0)
            {
                if (rect.Width <= rect.Height)
                    return new PointF(E, ((rect.Height * E) / rect.Width));
                else return new PointF(((rect.Width * E) / rect.Height), E);
            }
            else return default;
        }

        public PointF ProjSize(PointF sz)
        {
            if (sz.X > 0 && sz.Y > 0)
            {
                if (sz.X <= sz.Y)
                    return new PointF(E, ((sz.Y * E) / sz.X));
                else return new PointF(((sz.X * E) / sz.Y), E);
            }
            else return default;
        }

        public PointF ProjSize(Point psz)
        {
            if (psz.X > 0 && psz.Y > 0)
            {
                if (psz.X <= psz.Y)
                    return new PointF(E, ((psz.Y * E) / psz.X));
                else return new PointF(((psz.X * E) / psz.Y), E);
            }
            else return default;
        }

        public void Projection(GeoCoord2D dans_geoc)
        {
            P.X -= dans_geoc.P.X;
            P.Y -= dans_geoc.P.Y;
            if (dans_geoc.A != 0.0f)
            {
                Matrix m = new Matrix();
                m.Rotate(dans_geoc.A);
                P = new PointF
                    (
                        P.X * m.Elements[0] + P.Y * m.Elements[1],
                        P.X * m.Elements[2] + P.Y * m.Elements[3]
                    );
                A -= dans_geoc.A;
                while (A < 0.0f) A += 360.0f;
                A %= 360.0f;
            }
        }

        public void ProjectionInv(GeoCoord2D depuis_geoc)
        {
            if (depuis_geoc.A != 0.0f)
            {
                Matrix m = new Matrix();
                m.Rotate(-depuis_geoc.A);
                P = new PointF
                    (
                        P.X * m.Elements[0] + P.Y * m.Elements[1],
                        P.X * m.Elements[2] + P.Y * m.Elements[3]
                    );
                A += depuis_geoc.A;
                while (A < 0.0f) A += 360.0f;
                A %= 360.0f;
            }
            P.X += depuis_geoc.P.X;
            P.Y += depuis_geoc.P.Y;
        }

        /// <summary>
        /// Permet de changer l'angle de l'objet en considérant un point de rotation 
        /// </summary>
        public void ChangerAngleSuivantPoint(float nAngle, PointF rotPt)
        {
            if(nAngle != A) ChangerDeltaAngleSuivantPoint((nAngle - A), rotPt);
        }

        public void ChangerDeltaAngleSuivantPoint(float delta, PointF rotPt)
        {
            if (delta != 0.0f)
            {
                double cosa = Math.Cos((delta * Math.PI) / 180.0);
                double sina = Math.Sin((delta * Math.PI) / 180.0);
                PointF dltp = new PointF(rotPt.X - P.X, rotPt.Y - P.Y);
                PointF rdltp = new PointF(
                        (float)(dltp.X * cosa - dltp.Y * sina),
                        (float)(dltp.X * sina + dltp.Y * cosa)
                    );
                P.X += dltp.X - rdltp.X;
                P.Y += dltp.Y - rdltp.Y;
                A += delta;
            }
        }

        public void ChangerDeltaAngleAimantSuivantPoint(float delta, PointF rotPt)
        {
            float oldA = A;
            float newA = oldA + delta;
            ChangerAngleSuivantPoint(GeoCoord2D.AngleFromToAimant45(oldA, newA), rotPt);
        }

        static public bool EstDansIntervalStricte(int c, int iA, int iB)
        {
            int a = Math.Min(iA, iB);
            int b = Math.Max(iA, iB);
            return (a < c && c < b);
        }

        static public int AngleFromToAimant45(int angFrom, int angTo)
        {
            int a = Math.Min(angFrom, angTo);
            int b = Math.Max(angFrom, angTo);

            for (int ang = -360; ang <= 360; ang += 45)
                if (a < ang && ang < b)
                {
                    ang %= 360;
                    if (ang < 0) ang = 360 + ang;
                    return ang;
                }

            angTo %= 360;
            if (angTo < 0) angTo = 360 + angTo;
            return angTo;
        }

        static public float AngleFromToAimant45(float angFrom, float angTo)
        {
            float a = Math.Min(angFrom, angTo);
            float b = Math.Max(angFrom, angTo);

            for (float ang = -360.0f; ang <= 360.0f; ang += 45.0f)
                if (a < ang && ang < b)
                {
                    ang %= 360;
                    if (ang < 0) ang = 360 + ang;
                    return ang;
                }

            angTo %= 360;
            if (angTo < 0) angTo = 360 + angTo;
            return angTo;
        }
    }

    public struct GeoVue
    {
        public GeoCoord2D GC;
        public PointF DimentionD2;//demie dimention d'origine

        public PointF Dimention
        {
            get => new PointF(DimentionD2.X * 2.0f, DimentionD2.Y * 2.0f);
            set => DimentionD2 = new PointF(value.X / 2.0f, value.Y /2.0f);
        }

        public GeoVue(PointF p, float e, float a, PointF dimention)
        {
            GC = new GeoCoord2D(p, e, a);
            DimentionD2 = new PointF(dimention.X / 2.0f, dimention.Y / 2.0f);
        }

        public GeoVue(float x, float y, float e, float a, float dimX, float dimY)
        {
            GC = new GeoCoord2D(x, y, e, a);
            DimentionD2 = new PointF(dimX / 2.0f, dimY / 2.0f);
        }

        public PointF Projection(PointF p)
        {
            return GC.Projection(new PointF(p.X - DimentionD2.X, p.Y - DimentionD2.Y));
            //PointF dimProj = GC.Projection(Dimention);
            //return new PointF(P.X + (p.X / E), P.Y + (p.Y / E));
        }

        public PointF ProjectionDelta(PointF d)
        {
            return GC.ProjectionDelta(d);
        }

        public PointF ProjectionInv(PointF p)
        {
            PointF pTmp = GC.ProjectionInv(p);
            return new PointF(pTmp.X + DimentionD2.X, pTmp.Y+ DimentionD2.Y);
            //PointF dimProj = GC.ProjectionInv(Dimention);
            //return new PointF(((p.X - P.X) * E), ((p.Y - P.Y) * E));
        }

        public PointF ProjectionDeltaInv(PointF d)
        {
            return GC.ProjectionDeltaInv(d);
        }

        public PointF ProjSize(Rectangle rect)
        {
            return GC.ProjSize(rect);
        }

        public PointF ProjSize(Point psz)
        {
            return GC.ProjSize(psz);
        }
    }

    /*class Géométrie
    {
    }*/
}
