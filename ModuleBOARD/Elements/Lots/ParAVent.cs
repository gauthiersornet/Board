using ModuleBOARD.Elements.Base;
using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;

namespace ModuleBOARD.Elements.Lots
{
    public class ParAVent : Groupe
    {
        [Flags]
        public enum EConfig : byte
        {
            Néan = 0,

            Visible = (1 << 0), // Le contenu du par à vent est visible pour les autres joueurs
            EstUneMain = (1 << 1), // Le joueur voit ses pièces de la face opposée (Si le joueur voient ses cartes face visible, alors, les autres joueurs voient le dos et inversement)
            AttraperRetourne = (1 << 2), // Une pièce attrapée est retournée. // ça permet de jouer direct face visible
            DéposerRetourne = (1 << 3), // Une pièce déposée est retournée. // ça permet par exemple de ne jamais voir la carte comme dans Hanabis

            AuthoriserAPrendre = (1 << 4), // Permet à un autre joueur d'attraper le par à vent
            AuthoriserAPiocher = (1 << 5), // Permet à un autre joueur d'attraper une pièce
            AuthoriserAPiocherAuHazard = (1 << 6), // Permet à un autre joueur d'attraper une pièce au hazard
            AuthoriserADéposer = (1 << 7), // Permet à un autre joueur de déposer une pièce
        }

        static public readonly int Marge = 20;
        static public readonly int TailleMinimale = 2 * Marge;

        public ushort IdJoueur; // Identifiant dans la session du joueur auquel appartient ce par à vent
        public string NomJoueur; // Nom du joueur auquel le par à vent appartient
        public Color CouleurJoueur;
        private EConfig droits;
        private PointF size;

        override public int ElmId { get; set; }
        public override EType ElmType { get => EType.Groupe; }

        public override PointF Size { get => size; }

        public override bool IsAt(PointF mp, float angle)
        {
            return IsAt(mp, size, angle);
        }

        public override bool IsAt(PointF mp, PointF psz, float angle)
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

        public ParAVent()
        {
            size = new PointF(TailleMinimale, TailleMinimale);
        }

        public ParAVent(Joueur jr)
        {
            IdJoueur = jr.IdSessionJoueur;
            NomJoueur = jr.Nom;
            CouleurJoueur = jr.Couleur;
            size = new PointF(TailleMinimale, TailleMinimale);
        }

        public ParAVent(ParAVent elm)
            :base(elm)
        {
            droits = elm.droits;
            size = elm.size;
        }








        public override void Dessiner(RectangleF vue, float angle, Graphics g, PointF p)
        {
            base.Dessiner(vue, angle, g, p);
            /*p.X += GC.P.X;
            p.Y += GC.P.Y;
            if (LstElements != null)
            {
                for (int i = LstElements.Count - 1; i >= 0; --i)
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
            }*/
        }



        /*public override (Element, Element) MousePickAvecContAt(PointF mp, float angle, EPickUpAction action = 0)
        {
        }

        public override List<(Element, Element)> MousePickAllAvecContAt(PointF mp, float angle, EPickUpAction action = 0)
        {
        }

        public override Element MousePickAt(PointF mp, float angle, EPickUpAction action = 0)
        {
        }

        public override List<Element> MousePickAllAt(PointF mp, float angle, EPickUpAction action = 0)
        {
        }*/


        /*public override void Tourner(int delta)
        {
            if (!EstDansEtat(EEtat.RotationFixe))
            {
                int oldA = (int)(GC.A + 0.5f);
                int newA = oldA + delta;
                GC.A = GeoCoord2D.AngleFromToAimant45(oldA, newA); // delta * 45.0f;
            }
        }*/

        public override Element RangerVersParent(Element parent)
        {
            //xxx;
            return base.RangerVersParent(parent);
        }

        public override Element DéfausserElement(Element relem)
        {
            //xxx;
            return base.DéfausserElement(relem);
        }

        public override Element DétacherElement(Element relem)
        {
            //xxx;
            return base.DétacherElement(relem);
        }

        override public object MettreAJour(object obj)
        {
            //xxx;
            return base.MettreAJour(obj);
        }

        public override Element Suppression(Element elm)
        {
            //xxx;
            return base.Suppression(elm);
        }

        new public void Nétoyer()
        {
            base.Nétoyer();
            size = new PointF(TailleMinimale, TailleMinimale);
        }















        override public object Clone()
        {
            return new ParAVent(this);
        }

        public override bool Equals(object obj)
        {
            if (Object.ReferenceEquals(this, obj)) return true;
            else if (obj is ParAVent)
            {
                ParAVent prv = obj as ParAVent;
                if (prv.size.X == size.X && prv.size.Y == size.Y) return base.Equals(obj);
                else return false;
            }
            else return false;
        }





        public ParAVent(Stream stream, IRessourcesDésérialiseur resscDes)
            : base(stream, resscDes)
        {
            droits = (EConfig)stream.DésérialiserObject(typeof(EConfig));
            NomJoueur = stream.ReadString();
            CouleurJoueur = Color.FromArgb(stream.ReadInt());
            size = stream.ReadPointF();
        }

        override public void Serialiser(Stream stream, ref int gidr)
        {
            base.Serialiser(stream, ref gidr);
            stream.SerialiserObject(NomJoueur ?? "");
            stream.WriteBytes(BitConverter.GetBytes((Int32)(CouleurJoueur.ToArgb())));
            stream.SerialiserObject(size, typeof(PointF));
        }

        /*override public void SerialiserTout(Stream stream, ref int gidr, ISet<int> setIdRéseau)
        {
            base.SerialiserTout(stream, ref gidr, setIdRéseau);
        }*/
    }
}
