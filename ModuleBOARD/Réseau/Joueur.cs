using ModuleBOARD.Elements.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleBOARD.Réseau
{
    public class Joueur : IBinSerialisable
    {
        public ushort IdSessionJoueur;
        public string Nom;
        public PointF P;

        private bool estMainRetournée = false;
        public List<Element> ElémentAttrapés;

        public Joueur(string nom = null)
        {
            Nom = nom;
            Remettre();
        }

        public Joueur(ushort idSessionJoueur, string nom = null)
        {
            Remettre();
            IdSessionJoueur = idSessionJoueur;
            Nom = nom;
        }

        public bool AElementAttrapé { get { lock (this) { return ElémentAttrapés != null && ElémentAttrapés.Any(); } } }

        public int ElmId { get => 0; set { } }

        public EType ElmType => EType.Joueur;

        public void MajImage(Image img)
        {
            lock (this)
            {
                if (ElémentAttrapés != null)
                    ElémentAttrapés.ForEach(e => e.MettreAJour(img));
            }
        }

        public void Dessiner(RectangleF vue, float ang, Graphics g)
        {
            lock (this)
            {
                if (ElémentAttrapés != null)
                    ElémentAttrapés.ForEach(e => e.Dessiner(vue, ang, g, P));
            }
        }

        public void Remettre()
        {
            lock (this)
            {
                double ang = new Random().NextDouble();
                P = new PointF((float)(50.0 * Math.Cos(ang) + 0.5), (float)(50.0 * Math.Sin(ang) + 0.5));
                IdSessionJoueur = 0;
                ElémentAttrapés = null;
            }
        }

        public void RetournerAttrapées()//Retourne l'enssemble des éléments en main
        {
            lock (this)
            {
                estMainRetournée = !estMainRetournée;
                if (ElémentAttrapés != null)
                    ElémentAttrapés.ForEach(e => e.Retourner());
            }
        }

        public Element TrouverElementRéseau(int idRez)
        {
            if (idRez > 0)
            {
                lock (this)
                {
                    return ElémentAttrapés?.Find(e => e.IdentifiantRéseau == idRez);
                }
            }
            else return null;
        }

        public void DonnerElément(Element elm)
        {
            if (elm != null)
            {
                lock (this)
                {
                    if (ElémentAttrapés == null) ElémentAttrapés = new List<Element>();
                    elm.GC.P.X -= P.X;
                    elm.GC.P.Y -= P.Y;
                    if (estMainRetournée) elm.Retourner();
                    ElémentAttrapés.Add(elm);
                }
            }
        }

        public Element RécupérerElémentRéseau(int idRez)
        {
            if (idRez > 0)
            {
                lock (this)
                {
                    if (ElémentAttrapés != null)
                    {
                        int idx = ElémentAttrapés.FindIndex(e => e.IdentifiantRéseau == idRez);
                        if (idx >= 0)
                        {
                            Element res = ElémentAttrapés[idx];
                            ElémentAttrapés.RemoveAt(idx);
                            if (ElémentAttrapés.Any() == false) ElémentAttrapés = null;
                            res.GC.P.X += P.X;
                            res.GC.P.Y += P.Y;
                            if (estMainRetournée)
                            {
                                res.Retourner();
                                if (ElémentAttrapés == null) estMainRetournée = false;
                            }
                            return res;
                        }
                        else return null;
                    }
                    else return null;
                }
            }
            else return null;
        }

        public List<Element> ToutRécupérer()
        {
            lock (this)
            {
                if (ElémentAttrapés != null)
                {
                    List<Element> lstREs = ElémentAttrapés;
                    ElémentAttrapés = null;
                    if (estMainRetournée) lstREs.ForEach(e => { e.GC.P.X += P.X; e.GC.P.Y += P.Y; e.Retourner(); });
                    else lstREs.ForEach(e => { e.GC.P.X += P.X; e.GC.P.Y += P.Y; });
                    estMainRetournée = false;
                    return lstREs;
                }
                else return null;
            }
        }

        public void MajElement(Element elm)
        {
            if (elm != null && !(elm is ElementRéseau))
            {
                lock (this)
                {
                    if(ElémentAttrapés != null && ElémentAttrapés.RemoveAll(e => e.IdentifiantRéseau == elm.IdentifiantRéseau) > 0)
                        ElémentAttrapés.Add(elm);
                }
            }
        }

        /*
        public ushort IdSessionJoueur;
        public string Nom;
        public PointF P;

        private bool estMainRetournée = false;
        public List<Element> ElémentAttrapés;
        */
        public Joueur(Stream stream, IRessourcesDésérialiseur resscDes)
        {
            //resscDes.RetrouverObject(stream) as Element;
            IdSessionJoueur = (ushort)stream.ReadObject(typeof(ushort));
            Nom = stream.ReadString();
            P = stream.ReadPointF();
            //estMainRetournée n'pas partagée
            //ElémentAttrapés = (stream.ReadObject(typeof(Element[]), resscDes) as Element[]).ToList();
            //resscDes.RetrouverObject();
            IBinSerialisable[] tabIbins = resscDes.ReadIBinParRéférence(stream);
            if (tabIbins.Any()) ElémentAttrapés = tabIbins.Select(ib => ib as Element).ToList();
            else ElémentAttrapés = null;
        }

        public void Serialiser(Stream stream, ref int gidr)
        {
            stream.WriteByte((byte)ElmType);
            stream.WriteBytes(BitConverter.GetBytes(IdSessionJoueur));
            stream.SerialiserObject(Nom ?? "");
            stream.SerialiserObject(P);
            //stream.SerialiserObject(ElémentAttrapés?.Select(e => e.IdentifiantRéseau).ToArray(), ref gidr, typeof(int[]));
            stream.SerialiserParRéférence(ref gidr, ElémentAttrapés?.ToArray());
        }

        public void SerialiserTout(Stream stream, ref int gidr, ISet<int> setIdRéseau)
        {
            lock (this)
            {
                if (ElémentAttrapés != null)
                    foreach(Element e in ElémentAttrapés)
                        stream.SerialiserTout(e, ref gidr, setIdRéseau);
            }
            Serialiser(stream, ref gidr);
        }

        public object MettreAJour(object obj)
        {
            if (obj is Joueur)
            {
                if ((obj as Joueur).IdSessionJoueur == IdSessionJoueur)
                    return this;
                else return null;
            }
            else if(obj is Element && !(obj is ElementRéseau))
            {
                MajElement(obj as Element);
                return null;
            }
            else return null;
        }
    }
}
