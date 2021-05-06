﻿using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots.Piles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModuleBOARD.Elements.Pieces
{
    public class Element2D2F : Element2D
    {
        public bool Caché { get => EstDansEtat(EEtat.À_l_envers); set => AssignerEtat(EEtat.À_l_envers, value); }
        public Image Dos = default;
        //public SImage ElmImage = default;//Devant

        public override PointF Size { get => Caché ? GC.ProjSize(Dos.Rect()) : GC.ProjSize(ElmImage.Rect()); }

        public Element2D2F()
        {
        }

        public Element2D2F(Element2D2F elm)
            :base(elm)
        {
            Caché = elm.Caché;
            Dos = elm.Dos;
        }

        public Element2D2F(string path, XmlNode paq, PointF p, BibliothèqueImage bibliothèqueImage)
            : base(path, paq, p, bibliothèqueImage)
        {
            string fileDos = paq.Attributes.GetNamedItem("dos")?.Value;
            Dos = bibliothèqueImage.ChargerSImage(path, fileDos, paq, "dx", "dy", "dw", "dh");
        }

        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {
            GeoCoord2D gc = GC;
            gc.P.X += p.X;
            gc.P.Y += p.Y;
            /*p.X += GC.P.X;
            p.Y += GC.P.Y;*/
            if (Caché) Dos.Dessiner(vue, g, gc);
            else if (ElmImage == null)
            {
                Dos.DessinerVide(vue, g, gc);
            }
            else ElmImage.Dessiner(vue, g, gc);
        }

        override public object Clone()
        {
            return new Element2D2F(this);
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj)) return true;
            else if (obj is Element2D2F)
            {
                Element2D2F elm = obj as Element2D2F;
                return base.Equals(obj) && BibliothèqueImage.Equals(Dos, elm.Dos);
            }
            else return false;
        }

    }
}
