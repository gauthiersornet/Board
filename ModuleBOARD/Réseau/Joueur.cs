using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots.Dés;
using NAudio.Wave;
using NAudioDemo.NetworkChatDemo;
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
        [Flags]
        public enum EDroits : ushort
        {
            Néant = 0,
            PasseMain = (1 << 0), //peut t'il transmettre sa main ? (et donc la perdre)
            GestDroits = (1 << 1), //peut t'il dupliquer ses droits à quelqu'un d'autre
            Importation = (1 << 2), //Peut importer des nouveaux éléments
            Bloqué = (1 << 3), //Lui est'il interdit de faire quoi que ce soit ? Même le maitre !
            Manipuler = (1 << 4), //A t'il la main pour faire quelquechose avec les éléments
            ManipulerLots = (1 << 5), //Peut t'il toucher à une pile ou un paquet
            Retourner = (1 << 6), //Peut retourner les éléments
            RetournerLots = (1 << 7), //Peut retourner une pile ou un paquet
            ChangerEtat = (1 << 8), //Changer les états autres que retourner, coucher/redresser
            AttraperLots = (1 << 9), //Peut attraper (shift + clique gauche) une pile ou un paquet
            Attraper = (1 << 10),//Peut attraper (shift + clique gauche) un élément
            Piocher = (1 << 11), //Peut piocher (clique gauche) un élément
            ActionSpéciale = (1 << 12), //
            Supprimer = (1 << 13), //
            SupprimerTout = (1 << 14), //
            ChatAudio = (1 << 15), //

            Main = (ushort)(Manipuler | ManipulerLots),
            ToutLesDroits = (ushort)(0xFFFF & ~(Bloqué | GestDroits | PasseMain)),
            Maître = (ushort)(0xFFFF & ~Bloqué)
        };

        public ushort IdSessionJoueur;
        public string Nom;
        public Color Couleur;
        public PointF P;

        private WaveOut waveOut = null;
        private BufferedWaveProvider waveProvider;

        public EDroits Droits;

        private bool estMainRetournée = false;
        public List<Element> ElémentAttrapés;

        public Joueur(string nom = null)
        {
            Nom = nom;
            Couleur = Color.Black;
            Remettre();
        }

        public Joueur(ushort idSessionJoueur, string nom = null)
        {
            Remettre();
            IdSessionJoueur = idSessionJoueur;
            Nom = nom;
            Couleur = Color.Black;
        }

        public bool EstMaitre { get => Droits == EDroits.Maître; }

        public bool AElementAttrapé { get { lock (this) { return ElémentAttrapés != null && ElémentAttrapés.Any(); } } }

        public int ElmId { get => 0; set { } }

        public EType ElmType => EType.Joueur;

        public bool ADroits(Joueur.EDroits droits, Joueur.EDroits droitsSession = EDroits.Néant)
        {
            return !droits.HasFlag(Joueur.EDroits.Bloqué) && (droitsSession | Droits).HasFlag(droits);
        }

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
                Droits = EDroits.Néant;
                double ang = new Random().NextDouble();
                P = new PointF((float)(50.0 * Math.Cos(ang) + 0.5), (float)(50.0 * Math.Sin(ang) + 0.5));
                IdSessionJoueur = 0;
                ElémentAttrapés = null;
                FermerChatAudioHP();
            }
        }

        public void FermerChatAudioHP()
        {
            WaveOut wvout = waveOut;
            waveOut = null;
            if (wvout != null)
            {
                wvout.Stop();
                wvout.Dispose();
            }
            waveProvider = null;
        }

        public void ActivierChatAudioHP(INetworkChatCodec codec, int sortieAudio)
        {
            if (codec != null && sortieAudio >= 0)
            {
                waveOut = new WaveOut();
                waveProvider = new BufferedWaveProvider(codec.RecordFormat);
                waveOut.Init(waveProvider);
                waveOut.Play();
            }
        }

        public bool ChatAudioHP(byte[] decoded)
        {
            if (decoded != null && waveProvider != null)
                waveProvider.AddSamples(decoded, 0, decoded.Length);
            return true;
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

        public void TournerElémentAttrapé(float delta)
        {
            lock (this)
            {
                if (ElémentAttrapés != null)
                    ElémentAttrapés.ForEach(e => e.GC.ChangerDeltaAngleAimantSuivantPoint(delta, new PointF(0.0f, 0.0f)));
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

        public List<Element> RécupérerAMélanger()
        {
            lock (this)
            {
                if (estMainRetournée && ElémentAttrapés != null && ElémentAttrapés.Any(e => e is Dés))
                    return ElémentAttrapés.Where(e => e is Dés).ToList();
                else return null;
            }
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
                    lstREs.Reverse();
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
            IdSessionJoueur = (ushort)stream.DésérialiserObject(typeof(ushort));
            Nom = stream.ReadString();
            Couleur = Color.FromArgb(stream.ReadInt());
            P = stream.ReadPointF();
            Droits = (EDroits)BitConverter.ToUInt16(stream.GetBytes(2), 0);
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
            stream.WriteBytes(BitConverter.GetBytes((Int32)(Couleur.ToArgb())));
            stream.SerialiserObject(P);
            stream.WriteBytes(BitConverter.GetBytes((ushort)Droits));
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
