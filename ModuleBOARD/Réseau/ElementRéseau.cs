using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using ModuleBOARD.Elements.Base;

namespace ModuleBOARD.Réseau
{
    public class ElementRéseau : Element
    {
        public override EType ElmType { get => EType.ElémentRéseau; }
        public override PointF Size => new PointF(20.0f, 20.0f);

        public Element ARemplacerPar = null;

        public ElementRéseau()
        {
        }

        public ElementRéseau(int idRéseau)
        {
            IdentifiantRéseau = idRéseau;
        }

        public ElementRéseau(ElementRéseau elementRéseau)
            :base(elementRéseau)
        {
        }

        public ElementRéseau(Stream stream, IRessourcesDésérialiseur resscDes)
            : base(stream, resscDes)
        {
        }

        public override object Clone()
        {
            return new ElementRéseau(this);
        }

        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {
            //
        }

        public override bool Lier(XmlNode paq, Dictionary<string, Element> dElements)
        {
            return false;
        }

        public override bool PutOnTop(Element elm)
        {
            return false;
        }

        public override bool Roulette(int delta)
        {
            return false;
        }

        override public object MettreAJour(object obj)
        {
            if (obj is Element)
            {
                Element elm = obj as Element;
                if (elm == null) return null;
                else if (elm.IdentifiantRéseau == IdentifiantRéseau) return this;
                else return ARemplacerPar?.MettreAJour(elm);
            }
            else if(obj is Image)
            {
                if (ARemplacerPar != null)
                    ARemplacerPar.MettreAJour(obj);
                return base.MettreAJour(obj);
            }
            else return base.MettreAJour(obj);
        }

        virtual public void Serialiser(Stream stream, ref int gidr)
        {
            if (ARemplacerPar != null) ARemplacerPar.Serialiser(stream, ref gidr);
        }

        virtual public void SerialiserTout(Stream stream, ref int gidr, ISet<int> setIdRéseau)
        {
            if (ARemplacerPar != null) ARemplacerPar.SerialiserTout(stream, ref gidr, setIdRéseau);
        }
    }
}
