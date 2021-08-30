using ModuleBOARD.Elements.Base;
using ModuleBOARD.Réseau;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ModuleBOARD.Réseau
{
    public abstract class ClientThread : IRessourcesDésérialiseur
    {
        public enum BoardCodeCommande : byte
        {
            FinPaquet = 0, //Vide
            Déconnexion = 1,

            MessageServeur = 2, //bool ok, Nom de la session
            ActualiserSessions = 3, //Nom des la sessions
            ReçoisListeGJoueur = 4,
            RejointSession = 5,
            SynchroSession = 6, //Sérialisation intégrale du groupe racine
            RéidentifierElément = 7,
            ArrivéeJoueur = 8,
            SortieJoueur = 9,
            MessageGénéral = 10,
            MessageSession = 11,
            MajDroitsSession = 12,
            MajDroitsJoueur = 13,
            //PasserMainJoueur = 14,

            DemandeElement = 15,
            RéceptionElement = 16,
            DemandeImage = 17,
            DemandeModel = 18,
            RéceptionImage = 19,
            RéceptionModel = 20,

            ChargerElement = 21,
            ChangerEtatElément = 22,
            ChangerAngle = 23,
            RouletteElément = 24,
            TournerElément = 25,

            AttraperElement = 26, // On reçoi un élément déjà en jeu
            PiocherElement = 27, // On pioche un nouvelle élément
            LacherElement = 28, // On lache un élément sur un autre
            TournerElémentAttrapé = 29,

            RangerVersParent = 30,
            Mélanger = 31,
            DéfausserElement = 32,
            ReMettreDansPioche = 33,
            MettreEnPioche = 34,
            CréerLaDéfausse = 35,
            MettreEnPaquet = 36,

            Supprimer = 37,
            SupprimerTout = 38,

            ChatAudio = 50
        }

        public enum ServeurCodeCommande : byte
        {
            FinPaquet = 0,
            Déconnexion = 1,

            CréerSession = 2,//Nom de la session à créer et autres paramètres
            ActualiserSessions = 3, //Vide
            ActualiserListeGJoueur = 4,
            RejoindreSession = 5, //Mot de passe en sha256
            SupprimerSession = 6, //Nom de la session, l'émétteur doit être le maître de session
            QuitterSession = 7,
            DemandeSynchro = 8,
            //9
            MessageGénéral = 10,
            MessageSession = 11,
            MajDroitsSession = 12,
            MajDroitsJoueur = 13,
            PasserMainJoueur = 14,

            DemandeElement = 15,
            RéceptionElement = 16,
            DemandeImage = 17,
            DemandeModel = 18,
            RéceptionImage = 19,
            RéceptionModel = 20,

            ChargerElement = 21,
            ChangerEtatElément = 22,
            ChangerAngle = 23,
            RouletteElément = 24,
            TournerElément = 25,

            AttraperElement = 26, // On prend celui que l'on a ciblé !
            PiocherElement = 27, // On pioche celui que l'on a cible et donc si pile non vide alors bnouv element
            LacherElement = 28, // On lache un élément sur un autre
            TournerElémentAttrapé = 29,

            RangerVersParent = 30,
            Mélanger = 31,
            DéfausserElement = 32,
            ReMettreDansPioche = 33,
            MettreEnPioche = 34,
            CréerLaDéfausse = 35,
            MettreEnPaquet = 36,

            Supprimer = 37,
            SupprimerTout = 38,

            ChatAudio = 50
        }

        /*public enum EÉvènement : byte
        {
            ConnexionRéussie,
            PerteDeConnexion,
            IdentifiantInvalide,
            VersionIncompatible
        }*/

        /*protected static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression)
        {
            var member = expression.Body as MethodCallExpression;

            if (member != null)
                return member.Method;

            throw new ArgumentException("Expression is not a method", "expression");
        }*/

        #region GetMethodInfo
        static protected MethodInfo GetMethodInfo(Func<bool> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1>(Func<T1, bool> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2, bool> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3, bool> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2, T3, T4>(Func<T1, T2, T3, T4, bool> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, bool> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, bool> a) { return a.Method; }

        static protected MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, bool> a) { return a.Method; }

        /*static protected MethodInfo GetMethodInfo(Action a){return a.Method;}
        static protected MethodInfo GetMethodInfo<T1>(Action<T1> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2>(Action<T1, T2> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2, T3>(Action<T1, T2, T3> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2, T3, T4>(Action<T1, T2, T3, T4> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> a) { return a.Method; }
        static protected MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> a) { return a.Method; }*/
        #endregion GetMethodInfo

        static public Guid GVersion { get => new Guid(new byte[16] { 187, 197, 212, 164, 206, 71, 96, 23, 94, 134, 195, 168, 147, 101, 150, 202 }); }

        private Random rnd = new Random();

        public string ObtenirIdentifiant() { return Identifiant; }
        protected string Identifiant = null;
        private BigInteger pwhash = 0;

        protected TcpClient tcpClient;
        protected NetworkStream stream;
        protected Thread thread;
        public DateTime DateDernierFonctionnement;
        public bool fonctionne { get => (méthodesRéseau != null && stream != null); set { if (!value) méthodesRéseau = null; } }

        protected BigInteger chivK;
        protected BigInteger dechifK;

        protected MemoryStream fluxEntrant;
        protected MethodInfo[] méthodesRéseau;

        protected BibliothèqueImage bibImg;
        protected SortedSet<string> ImagesDemandés;
        protected BibliothèqueModel bibMod;
        protected SortedSet<string> modelsDemandés;

        protected Dictionary<int, IBinSerialisable> dicoElement = new Dictionary<int, IBinSerialisable>(); //indexer temporairement les éléments

        protected ConcurrentQueue<byte[]> FileOrdres;

        protected ConcurrentQueue<string> Journal;

        protected ClientThread() { }

        public ClientThread(TcpClient _tcpClient, BigInteger _pwhash, ConcurrentQueue<string> journal, string identifiant = null)
        {
            _tcpClient.ReceiveTimeout = 10000;
            _tcpClient.SendTimeout = 10000;
            ImagesDemandés = null;
            modelsDemandés = null;
            DateDernierFonctionnement = DateTime.Now;
            if (journal != null) Journal = journal;
            else Journal = new ConcurrentQueue<string>();
            pwhash = _pwhash;
            if (identifiant != null)
            {
                if (identifiant == "" || (identifiant.Length < 30 && !string.IsNullOrWhiteSpace(identifiant) && identifiant.EstChaineSecurisée()))
                {
                    Identifiant = identifiant;
                }
                else throw new Exception("L'identifiant fait plus de 30 caractères ou contient des caractères interdis.");
            }
            FileOrdres = new ConcurrentQueue<byte[]>();
            thread = new Thread(new ThreadStart(fonctionnement));
            tcpClient = _tcpClient;
            stream = tcpClient.GetStream();
        }

        public void Lancer()
        {
            thread.Start();
        }

        protected bool InitChiffrage()
        {
            Random rnd = new Random();
            BigInteger bigInt;

            BigInteger g = OutilsRéseau.SecuredRandomBigInteger256(rnd);
            if (!WriteUBigInteger(g, 32)) return false;
            bigInt = ReadUBigInteger(32);
            if (bigInt < BigInteger.Zero) return false;
            if (g != bigInt) g ^= bigInt;

            Thread.Sleep(rnd.Next(256));
            rnd = new Random();
            BigInteger p = OutilsRéseau.SecuredRandomBigInteger256(rnd);
            WriteUBigInteger(OutilsRéseau.FastPowMod(g, p, OutilsRéseau.Prime256Bits), 32);
            Thread.Sleep(rnd.Next(256));
            bigInt = ReadUBigInteger(32);
            if (bigInt < BigInteger.Zero) return false;
            chivK = dechifK = OutilsRéseau.FastPowMod(bigInt, p, OutilsRéseau.Prime256Bits);

            //chivK = dechifK = 0;
            return true;
        }

        /*protected bool InitChiffrage()
        {
            Random rnd = new Random();
            BigInteger g = OutilsRéseau.SecuredRandomBigInteger256(rnd);
            if(!WriteUBigInteger(g, 32))return false;
            BigInteger bigInt = ReadUBigInteger(32);
            if (bigInt < BigInteger.Zero) return false;
            g ^= bigInt;
            Thread.Sleep(rnd.Next(256));
            rnd = new Random();
            BigInteger p = OutilsRéseau.SecuredRandomBigInteger256(rnd);
            WriteUBigInteger(OutilsRéseau.FastPowMod(g, p, OutilsRéseau.Prime256Bits), 32);
            Thread.Sleep(rnd.Next(256));
            bigInt = ReadUBigInteger(32);
            if (bigInt < BigInteger.Zero) return false;
            chivK = dechifK = OutilsRéseau.FastPowMod(bigInt, p, OutilsRéseau.Prime256Bits);
            return true;
        }*/

        protected abstract bool Identifie();
        protected abstract bool EchangerIdentifiant();

        public bool EstConnecté { get => tcpClient != null && tcpClient.Connected; }
        public bool EstIdentifié { get => tcpClient != null && tcpClient.Connected && Identifiant != null; }

        public bool AEnvoiEnCours { get => FileOrdres != null && !FileOrdres.IsEmpty; }
        public bool AReceptionEnCours { get
            {
                TcpClient tcp = tcpClient;
                if(tcp != null)
                {
                    try
                    {
                        return tcpClient.Available > 0;
                    }
                    catch { return false; }
                }
                else return false;
            } }

        public BibliothèqueImage BibImage => bibImg;
        public BibliothèqueModel BibModel => bibMod;

        /*
        protected bool DemandeImage(List<string> idImgs)
        {
            if(bibImg != null)
            {
                foreach(string sig in idImgs)
                {
                    MemoryStream strm = new MemoryStream();
                    strm.WriteByte((byte)ServeurCodeCommande.RéceptionImage);
                    if(bibImg.RécupérerImage(sig, strm)) WriteChiffrer256(strm);
                }
            }
            return true;
        }

        protected bool DemandeModel(List<string> idMods)
        {
            if (bibMod != null)
            {
                foreach (string sig in idMods)
                {
                    MemoryStream strm = new MemoryStream();
                    strm.WriteByte((byte)ServeurCodeCommande.RéceptionModel);
                    if(bibMod.RécupérerModel(sig, strm)) WriteChiffrer256(strm);
                }
            }
            return true;
        }

        protected bool RéceptionImage()
        {
            if (bibImg != null)
            {
                Image img = BibliothèqueImage.ChargerImage(fluxEntrant);
                return bibImg.NouvelleVersion(img);
            }
            else return false;
        }

        protected bool RéceptionModel()
        {
            if (bibMod != null && bibImg != null)
            {
                Model2_5D mld = BibliothèqueModel.ChargerModel(fluxEntrant, bibImg);
                return bibMod.NouvelleVersion(mld);
            }
            else return false;
        }
        */

        protected byte[] ReadBytes(int nbOctets)
        {
            if (nbOctets < OutilsRéseau.NB_OCTET_MAX)
            {
                try
                {
                    byte[] res = new byte[nbOctets];
                    int nbrb;
                    for (int i = 0; i < nbOctets && fonctionne; i += nbrb)
                    {
                        nbrb = stream.Read(res, i, nbOctets - i);
                    }
                    return res;
                }
                catch
                {
                    //return null;
                    throw;
                }
            }
            else return null;
        }

        protected byte[] chiffrer256(byte[] m, int nbBlock = -1)
        {
            if (m == null) return null;
            int nbBlockM = ((m.Length + 30) / 31);
            if (nbBlock <= 0) nbBlock = nbBlockM;
            byte[] res = new byte[nbBlock * 32];
            byte[] buff = new byte[32];
            for (int i = 0; i < nbBlock; ++i)
            {
                int nbBytes = Math.Min(31, m.Length - i * 31);

                Array.Copy(m, i * 31, buff, 0, nbBytes);
                if (nbBytes < 31)
                {
                    byte[] rndBuff = new byte[31 - nbBytes];
                    rnd.NextBytes(rndBuff);
                    Array.Copy(rndBuff, 0, buff, nbBytes, 31 - nbBytes);
                    buff[31] = (byte)nbBytes;
                }
                else buff[31] = (byte)(rnd.Next(0b00100000) | 0b00100000);
                //buff[31] = 0;
                //buff[31] = CalculerCheckCode31(buff, (i == nbBlock - 1));
                if (i == nbBlock - 1) buff[31] |= 0b01000000;// si on est au dernier block, stop bit à 1
                BigInteger bint = chiffrer256(new BigInteger(buff));
                byte[] bintB = bint.ToByteArray();
                Array.Copy(bintB, 0, res, i * 32, Math.Min(32, bintB.Length));
            }
            return res;
        }

        protected BigInteger chiffrer256(BigInteger bint)
        {
            return bint ^ chivK;
        }

        /*protected bool WriteQueue(int limit = 1)
        {
            if(FileOrdres.IsEmpty == false)
            {
                byte[] bts;
                int idx = 0;
                byte[] block = new byte[32];

                for (; limit != 0 && FileOrdres.TryDequeue(out bts); --limit)
                {
                    int nbBytes = 0;
                    for (int i = 0; i < bts.Length; i += nbBytes)
                    {
                        nbBytes = Math.Min(31 - idx, bts.Length - i);
                        Array.Copy(bts, i, block, idx, nbBytes);
                        idx += nbBytes;
                        if (idx == 31)
                        {
                            //block[31] = CalculerCheckCode31(block, ((bksIdx+32) == writeBks.Length));
                            block[31] = (byte)(rnd.Next(0b00100000) | 0b00100000);
                            if ((i + nbBytes) == bts.Length) block[31] |= 0b01000000;// si on est au dernier block, stop bit à 1

                            BigInteger bint = chiffrer256(new BigInteger(block));
                            byte[] bintB = bint.ToByteArray();
                            Array.Copy(bintB, 0, block, 0, Math.Min(32, bintB.Length));
                            if (bintB.Length < 32) Array.Clear(block, bintB.Length, 32 - bintB.Length);
                            try { stream.Write(block, 0, 32); }
                            catch { throw; }

                            idx = 0;
                        }
                    }
                    if (idx != 0)//si ça ne tombe pas juste, on doit rajouter un zéro
                    {
                        block[idx] = 0;
                        idx += 1;
                        if (idx == 31)
                        {
                            //block[31] = CalculerCheckCode31(block, ((bksIdx+32) == writeBks.Length));
                            block[31] = (byte)(rnd.Next(0b00100000) | 0b00100000);
                            block[31] |= 0b01000000;//on est au dernier block, stop bit à 1

                            BigInteger bint = chiffrer256(new BigInteger(block));
                            byte[] bintB = bint.ToByteArray();
                            Array.Copy(bintB, 0, block, 0, Math.Min(32, bintB.Length));
                            if (bintB.Length < 32) Array.Clear(block, bintB.Length, 32 - bintB.Length);
                            try { stream.Write(block, 0, 32); }
                            catch { throw; }
                            idx = 0;
                        }
                    }
                }

                if (idx > 0)
                {
                    Random rnd = new Random();
                    bts = new byte[31 - idx];
                    rnd.NextBytes(bts);
                    Array.Copy(bts, 0, block, idx, bts.Length);
                    block[31] = (byte)idx;

                    //block[31] = CalculerCheckCode31(block, true);
                    block[31] |= 0b01000000;// on est au dernier block, stop bit à 1

                    BigInteger bint = chiffrer256(new BigInteger(block));
                    byte[] bintB = bint.ToByteArray();
                    Array.Copy(bintB, 0, block, 0, Math.Min(32, bintB.Length));
                    if (bintB.Length < 32) Array.Clear(block, bintB.Length, 32 - bintB.Length);
                    try { stream.Write(block, 0, 32); }
                    catch { throw; }
                }
            }
            return true;
        }*/

        private byte[][] writeBks = new byte[100][];
        private byte[] block = new byte[32];

        protected bool WriteQueue(int tailleMini = 5*1024)
        {
            while (FileOrdres.IsEmpty == false)
            {
                int nbBuff = 0;
                int tailleTotal;
                byte[] bts;

                for (tailleTotal = 0; tailleTotal < tailleMini && nbBuff < writeBks.Length && FileOrdres.TryDequeue(out bts); tailleTotal += ((bts.Length+30)/31) * 32 )
                {
                    if(bts.Length > 0) writeBks[nbBuff++] = bts;
                }

                if(nbBuff > 0)
                {
                    int odx = 0;
                    byte[] outBuff = new byte[tailleTotal];
                    for(int bi = 0; bi < nbBuff; ++bi)
                    {
                        bts = writeBks[bi];

                        int nbFBk = ((bts.Length-1) / 31) * 31;
                        for (int i = 0; i < nbFBk; i+=31, odx+=32)
                        {
                            //Array.Copy(bts, i, block, 0, 31);
                            Buffer.BlockCopy(bts, i, block, 0, 31);

                            //block[31] = CalculerCheckCode31(block, ((bksIdx+32) == writeBks.Length));
                            block[31] = (byte)(rnd.Next(0b00100000) | 0b00100000);
                            //if ((i + nbBytes) == bts.Length) block[31] |= 0b01000000;// si on est au dernier block, stop bit à 1

                            BigInteger bint = chiffrer256(new BigInteger(block));
                            byte[] bintB = bint.ToByteArray();
                            Buffer.BlockCopy(bintB, 0, outBuff, odx, Math.Min(bintB.Length, 32));
                        }

                        int nbByte = bts.Length - nbFBk;
                        if(nbByte < 31)
                        {
                            Buffer.BlockCopy(bts, nbFBk, block, 0, nbByte);
                            Array.Clear(block, nbByte, 31 - nbByte);

                            //block[31] = CalculerCheckCode31(block, ((bksIdx+32) == writeBks.Length));
                            block[31] = (byte)(nbByte | 0b01000000);// si on est au dernier block, stop bit à 1

                            BigInteger bint = chiffrer256(new BigInteger(block));
                            byte[] bintB = bint.ToByteArray();
                            Buffer.BlockCopy(bintB, 0, outBuff, odx, Math.Min(bintB.Length, 32));
                        }
                        else
                        {
                            Buffer.BlockCopy(bts, nbFBk, block, 0, 31);

                            //block[31] = CalculerCheckCode31(block, ((bksIdx+32) == writeBks.Length));
                            block[31] = (byte)(rnd.Next(0b00100000) | 0b01100000);// si on est au dernier block, stop bit à 1

                            BigInteger bint = chiffrer256(new BigInteger(block));
                            byte[] bintB = bint.ToByteArray();
                            Buffer.BlockCopy(bintB, 0, outBuff, odx, Math.Min(bintB.Length, 32));
                        }
                        odx += 32;
                    }
                    stream.Write(outBuff, 0, outBuff.Length);
                }
            }
            stream.Flush();
            return true;
        }

        /*private byte[] writeBks = new byte[32 * 100];
        protected bool WriteQueue(int limit = 1)
        {
            if (FileOrdres.IsEmpty == false)
            {
                byte[] bts;
                int idx = 0;
                byte[] block = new byte[32];

                int bksIdx = 0;

                for (; limit != 0 && FileOrdres.TryDequeue(out bts); --limit)
                {
                    int nbBytes = 0;
                    for (int i = 0; i < bts.Length; i += nbBytes)
                    {
                        nbBytes = Math.Min(31 - idx, bts.Length - i);
                        Array.Copy(bts, i, block, idx, nbBytes);
                        idx += nbBytes;
                        if (idx == 31)
                        {
                            //block[31] = CalculerCheckCode31(block, ((bksIdx+32) == writeBks.Length));
                            block[31] = (byte)(rnd.Next(0b00100000) | 0b00100000);
                            if ((i + nbBytes) == bts.Length) block[31] |= 0b01000000;// si on est au dernier block, stop bit à 1

                            BigInteger bint = chiffrer256(new BigInteger(block));
                            byte[] bintB = bint.ToByteArray();

                            if (bintB.Length < 32)
                            {
                                Array.Copy(bintB, 0, writeBks, bksIdx, bintB.Length);
                                Array.Clear(writeBks, bksIdx + bintB.Length, 32 - bintB.Length);
                            }
                            else Array.Copy(bintB, 0, writeBks, bksIdx, 32);

                            bksIdx += 32;
                            idx = 0;
                            if (bksIdx == writeBks.Length)
                            {
                                try { stream.Write(writeBks, 0, writeBks.Length); }
                                catch { throw; }
                                bksIdx = 0;
                            }
                        }
                    }
                    if (idx != 0)//si ça ne tombe pas juste, on doit rajouter un zéro
                    {
                        block[idx] = 0;
                        idx += 1;
                        if (idx == 31)
                        {
                            //block[31] = CalculerCheckCode31(block, ((bksIdx+32) == writeBks.Length));
                            block[31] = (byte)(rnd.Next(0b00100000) | 0b00100000);
                            block[31] |= 0b01000000;//dernier block, stop bit à 1

                            BigInteger bint = chiffrer256(new BigInteger(block));
                            byte[] bintB = bint.ToByteArray();
                            
                            if (bintB.Length < 32)
                            {
                                Array.Copy(bintB, 0, writeBks, bksIdx, bintB.Length);
                                Array.Clear(writeBks, bksIdx + bintB.Length, 32 - bintB.Length);
                            }
                            else Array.Copy(bintB, 0, writeBks, bksIdx, 32);

                            bksIdx += 32;
                            idx = 0;
                            if (bksIdx == writeBks.Length)
                            {
                                try { stream.Write(writeBks, 0, writeBks.Length); }
                                catch { throw; }
                                bksIdx = 0;
                            }
                        }
                    }
                }

                if (idx > 0)
                {
                    if (idx < 31)
                    {
                        Random rnd = new Random();
                        bts = new byte[31 - idx];
                        rnd.NextBytes(bts);
                        Array.Copy(bts, 0, block, idx, bts.Length);
                        block[31] = (byte)idx;
                    }
                    else block[31] = (byte)(rnd.Next(0b00100000) | 0b00100000);
                    //block[31] = CalculerCheckCode31(block, true);
                    block[31] |= 0b01000000;// on est au dernier block, stop bit à 1

                    BigInteger bint = chiffrer256(new BigInteger(block));
                    byte[] bintB = bint.ToByteArray();
                    
                    if (bintB.Length < 32)
                    {
                        Array.Copy(bintB, 0, writeBks, bksIdx, bintB.Length);
                        Array.Clear(writeBks, bksIdx + bintB.Length, 32 - bintB.Length);
                    }
                    else Array.Copy(bintB, 0, writeBks, bksIdx, 32);
                    bksIdx += 32;
                }

                if (bksIdx > 0)
                {
                    try { stream.Write(writeBks, 0, bksIdx); }
                    catch { throw; }
                    //bksIdx = 0;
                }

                stream.Flush();
            }
            return true;
        }*/

        protected int WriteChiffrer256(byte[] m, int nbBlock = -1)
        {
            byte[] cyf = chiffrer256(m, nbBlock);
            if (WriteBytes(cyf)) return cyf.Length;
            else return -1;
        }

        protected byte[] déchiffrer256(byte[] m)
        {
            if (m == null || m.Length % 32 != 0) return null;

            int nbBlock = (m.Length / 32);
            byte[] res = new byte[nbBlock * 31];
            byte[] buff = new byte[33];
            for (int i = 0; i < nbBlock; ++i)
            {
                Array.Copy(m, i * 32, buff, 0, 32);
                BigInteger bint = déchiffrer256(new BigInteger(buff));
                byte[] bintB = bint.ToByteArray();
                Array.Copy(bintB, 0, res, i * 31, Math.Min(bintB.Length, 31));
            }
            return res;
        }

        protected BigInteger déchiffrer256(BigInteger bint)
        {
            return bint ^ chivK;
        }

        protected byte[] ReadDéChiffrer256()
        {
            List<KeyValuePair<byte, byte[]>> lres = new List<KeyValuePair<byte, byte[]>>();
            int totalSize = 0;
            bool stop;
            do
            {
                byte[] bintB = ReadBytes(32);
                if (bintB != null && bintB.Length == 32)
                {
                    if ((bintB[31] & 0b10000000) != 0) bintB = bintB.AddLast(0);
                     BigInteger bint = déchiffrer256(new BigInteger(bintB));
                    if (bint >= BigInteger.Zero)
                    {
                        bintB = bint.ToByteArray();

                        byte checkByte = (bintB.Length >= 32 ? bintB[31] : (byte)0);
                        stop = ((checkByte & 0b01000000) != 0);
                        byte octetDispo;
                        if ((checkByte & 0b00100000) == 0)//si le paquet n'est pas plein
                            octetDispo = (byte)(checkByte & 0b00011111);
                        else octetDispo = 31;
                        lres.Add(new KeyValuePair<byte, byte[]>(octetDispo, bintB));
                        totalSize += octetDispo;
                    }
                    else return null;
                }
                else return null;
                /*if (checkByte != CalculerCheckCode31(bintB, stop))
                {
                    //Err
                }*/
            }
            while(stop == false && totalSize < OutilsRéseau.NB_OCTET_MAX);//Tant que le bit de stop est à 0, il y a un autre block
            byte[] res = new byte[totalSize];
            int idx = 0;
            foreach(KeyValuePair<byte, byte[]> kv in lres)
            {
                Array.Copy(kv.Value, 0, res, idx, Math.Min(kv.Key, kv.Value.Length));
                idx += kv.Key;
            }
            return res;
        }

        protected byte[] ReadDéChiffrer256(int nbBlock)
        {
            byte[] bts = ReadBytes(32 * nbBlock);
            if (bts != null && bts.Length == 32 * nbBlock) return déchiffrer256(bts);
            else return null;
        }

        /* /// <summary>
        /// Saute le code commande et concatène avec les blocs suivants
        /// </summary>
        /// <param name="nbBlock"></param>
        /// <returns></returns>
        protected byte[] ReadDéChiffrer256Data(byte[] commandBlock, int nbDataBlock = 1)
        {
            byte[] dataBlock = ReadDéChiffrer256(nbDataBlock);
            if (dataBlock == null) return null;
            byte[] fullDataBlocks = new byte[commandBlock.Length - OutilsRéseau.NB_OCTET_COMMANDE + dataBlock.Length];
            Array.Copy(commandBlock, OutilsRéseau.NB_OCTET_COMMANDE, fullDataBlocks, 0, commandBlock.Length - OutilsRéseau.NB_OCTET_COMMANDE);
            Array.Copy(dataBlock, 0, fullDataBlocks, commandBlock.Length - OutilsRéseau.NB_OCTET_COMMANDE, dataBlock.Length);
            return fullDataBlocks;
        }*/

        /*protected void CompléterLectureBlock(int jusqua)
        {
            int nbBlks = (30 + jusqua - (lectureBlock.Length - idxLecture)) / 31;
            if (nbBlks > 0)
            {
                byte[] blks = ReadDéChiffrer256(nbBlks);
                lectureBlock = lectureBlock.SubFuse(idxLecture, (lectureBlock.Length - idxLecture), blks, 0, blks.Length);
            }
            else if (idxLecture > 0) lectureBlock = lectureBlock.SubBytes(idxLecture);
            idxLecture = 0;
        }*/

        protected int WriteChiffrer256(MemoryStream memStream)
        {
            return WriteChiffrer256(memStream.ToArray());
        }

        protected MemoryStream ReadMemStreamDéchiffrer256()
        {
            byte[] bts = ReadDéChiffrer256();
            if (bts != null) return new MemoryStream(bts);
            else return null;
        }

        /*protected string LectureString()
        {
            return fluxEntrant.ReadObject(typeof(string)) as string;
            //if (lectureBlock != null)
            //{
            //    int idx;
            //    for (idx = idxLecture; idx < lectureBlock.Length && lectureBlock[idx] != 0; ++idx) ;//On cherche le dernier 0
            //    string strR = Encoding.UTF8.GetString(lectureBlock, idxLecture, idx - idxLecture);
            //    idxLecture = idx;
            //    return strR;
            //}
            //else return null;
        }*/

        /*protected void RecalerLecture()
        {
            if (idxLecture >= lectureBlock.Length) lectureBlock = null;
            else if (lectureBlock[idxLecture] == 0) lectureBlock = null;
            else lectureBlock = lectureBlock.SubBytes(idxLecture);
            idxLecture = 0;
        }*/

        protected ulong ReadULChiffrer256()
        {
            byte[] bts = ReadDéChiffrer256(1);
            return (bts != null && bts.Length == 32 ? BitConverter.ToUInt64(bts, 0) : 0);
        }

        protected Guid ReadGuid()
        {
            byte[] bts = ReadBytes(16);
            return (bts != null && bts.Length == 16 ? new Guid(bts) : Guid.Empty);
        }

        protected BigInteger ReadDéchiffrer256()
        {
            BigInteger bigint = ReadUBigInteger(32);
            if (bigint >= BigInteger.Zero) return déchiffrer256(bigint);
            else return BigInteger.MinusOne;
        }

        protected int WriteChiffrer256(BigInteger bint)
        {
            if (WriteUBigInteger(chiffrer256(bint), 32)) return 32;
            else return -1;
        }

        /*protected BigInteger ReadBigInteger(int nbOctets)
        {
            return new BigInteger(ReadBytes(nbOctets));
        }*/

        protected BigInteger ReadUBigInteger(int nbOctets)
        {
            try
            {
                byte[] res = new byte[nbOctets + 1];
                res[nbOctets] = 0;
                int nbrb;
                for (int i = 0; i < nbOctets && fonctionne; i += nbrb)
                {
                    nbrb = stream.Read(res, i, nbOctets - i);
                }
                return new BigInteger(res);
            }
            catch
            {
                throw; /*return BigInteger.MinusOne;*/
            }
        }

        protected bool WriteGuid(Guid guid)
        {
            try
            {
                /*lock (stream) {*/
                return (WriteBytes(guid.ToByteArray(), 16) == 16); /*}*/
            }
            catch
            {
                return false;
            }
            return true;
        }

        protected bool WriteBytes(byte[] octets)
        {
            try
            {
                /*lock (stream) {*/
                stream.Write(octets, 0, octets.Length); /*}*/
                stream.Flush();
            }
            catch
            {
                throw; /*return false;*/
            }
            return true;
        }

        protected int WriteBytes(byte[] octets, int nbOctets)
        {
            if (nbOctets < 0 || nbOctets == octets.Length)
            {
                if (WriteBytes(octets)) return octets.Length;
                else return -1;
            }
            else
            {
                byte[] wB = new byte[nbOctets];
                Array.Copy(octets, wB, Math.Min(nbOctets, octets.Length));
                if (WriteBytes(wB)) return nbOctets;
                else return -1;
            }
        }

        /*private int WriteBigInteger(BigInteger bint, int nbOctets = -1)
        {
            return WriteBytes(bint.ToByteArray(), nbOctets);
        }*/

        protected bool WriteUBigInteger(BigInteger bint, int nbOctets = -1)
        {
            byte[] bIntArr = bint.ToByteArray();
            int len;
            for (len = bIntArr.Length; len > 0 && bIntArr[len - 1] == 0; --len) ;
            if (nbOctets < 0 || nbOctets == len)
            {
                if (len > 0)
                {
                    Array.Resize(ref bIntArr, len);
                    return WriteBytes(bIntArr);
                }
                else return true;
            }
            else if (nbOctets < len) return false;
            else
            {
                Array.Resize(ref bIntArr, nbOctets);
                Array.Clear(bIntArr, len, nbOctets - len);
                return WriteBytes(bIntArr);
            }
        }

        protected void Journaliser(string message)
        {
            if(Journal != null)
            {
                Journal.Enqueue(DateTime.Now + " [" + (Identifiant ?? "") + "] " + message);
            }
        }

        public void EcrireJournal(string file)
        {
            if (Journal != null)
            {
                using (StreamWriter sw = new StreamWriter(File.OpenWrite(file)))
                {
                    string str;
                    while (Journal.TryDequeue(out str))
                    {
                        sw.WriteLine(str);
                    }
                }
            }
        }

        abstract protected bool GérerException(Exception exp);

        protected void fonctionnement()
        {
            try
            {
                if (Identifie())
                {
                    if (InitChiffrage() && EchangerIdentifiant())
                    {
                        Journaliser("Connexion réussie.");
                        tcpClient.ReceiveTimeout = 30000;
                        tcpClient.SendTimeout = 30000;
                        DateDernierFonctionnement = DateTime.Now;

                        // Tant que le thread n'est pas tué, on travaille
                        while ((thread?.IsAlive ?? false) && fonctionne && (tcpClient?.Connected ?? false))
                        {
                            while(tcpClient.Available > 0)
                            {
                                //lectureBlock = ReadDéChiffrer256();
                                fluxEntrant = ReadMemStreamDéchiffrer256();
                                if (fluxEntrant != null)
                                {
                                    for (int cmd = fluxEntrant.ReadByte(); 0 < cmd && cmd <= 255; cmd = fluxEntrant.ReadByte())
                                    {
                                        MethodInfo[] mets = méthodesRéseau;
                                        if (fonctionne && mets[cmd] != null)
                                        {
                                            object[] prms = fluxEntrant.DécodeCommande(this, mets[cmd].GetParameters());
                                            //VérifierLesEléments();
                                            dicoElement.Clear();
                                            try
                                            {
                                                if (false.Equals(mets[cmd].Invoke(this, prms)))
                                                {
                                                    /*Close();
                                                    fonctionne = false;
                                                    return;*/
                                                    GérerException(null);
                                                }
                                            }
                                            catch(Exception ex)
                                            {
                                                Journaliser("ex = " + ex);
                                                if (!GérerException(null))
                                                    throw ex;
                                            }
                                            DateDernierFonctionnement = DateTime.Now;
                                        }
                                        else
                                        {
                                            Close();
                                            fonctionne = false;
                                            return;
                                        }
                                    }
                                }
                            }

                            /*while(FileOrdres.IsEmpty == false)*/ WriteQueue();

                            //Attente de 10 ms
                            Thread.Sleep(10);
                        }
                    }
                }
            }
            catch (IOException ioex) //Déco...
            {
                if (tcpClient?.Connected ?? false)
                {
                    //encore connecté ? Alors log...
                    Journaliser("ioex = " + ioex);
                }
                //else; //Déco
            }
            catch (Exception ex)
            {
                //Log...
                Journaliser("ex = " + ex);
            }
            catch
            {
                //Attraper les tous !
                Journaliser("err...");
            }
            Close();
        }

        //protected abstract void Évènement(EÉvènement éve);

        public virtual void Close()
        {
            Journaliser("Déconnexion.");
            lock (this)
            {
                Identifiant = null;
                if (stream != null)
                {
                    try { WriteCommande(ServeurCodeCommande.Déconnexion); } catch { }
                    var s = stream;
                    stream = null;
                    s.Close();
                }
                if (tcpClient != null)
                {
                    var c = tcpClient;
                    tcpClient = null;
                    c.Close();
                }
            }
        }

        public void Abort()
        {
            if (thread != null && thread.IsAlive)
            {
                fonctionne = false;
                var t = thread;
                thread = null;
                t.Abort();
            }
        }

        public bool SafeStop(int tm = 2000)
        {
            fonctionne = false;
            if (thread != null)
            {
                thread.Join(tm);
                return !thread.IsAlive;
            }
            else return true;
        }

        #region WriteCommande
        protected int WriteChiffrer256(BoardCodeCommande code, byte[] m, int nbBlock = -1)
        {
            return WriteChiffrer256((byte)code, m, nbBlock);
        }

        protected int WriteChiffrer256(ServeurCodeCommande code, byte[] m, int nbBlock = -1)
        {
            return WriteChiffrer256((byte)code, m, nbBlock);
        }

        protected int WriteChiffrer256(byte code, byte[] m, int nbBlock = -1)
        {
            byte[] nb = new byte[(m?.Length ?? 0) + 1];
            nb[0] = code;
            if (m != null && m.Length > 0) Array.Copy(m, 0, nb, 1, m.Length);

            byte[] cyf = chiffrer256(nb, nbBlock);
            if (WriteBytes(cyf)) return cyf.Length;
            else return -1;

        }

        /*protected int WriteChiffrer256(ulong val)
        {
            byte[] nb = BitConverter.GetBytes(val);
            byte[] cyf = chiffrer256(nb, ((nb.Length + 30) / 31));
            if (WriteBytes(cyf)) return cyf.Length;
            else return -1;
        }*/

        public bool WriteCommande(ServeurCodeCommande code, params object[] objs)
        {
            return WriteCommande((byte)code, objs);
        }

        public bool WriteCommande(BoardCodeCommande code, params object[] objs)
        {
            return WriteCommande((byte)code, objs);
        }

        public bool WriteCommande(byte code, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o);

                byte[] bts = memStream.ToArray();
                return (WriteChiffrer256(bts) >= bts.Length);
            }
        }

        public bool WriteCommande(ServeurCodeCommande code, ref int gidr, params object[] objs)
        {
            return WriteCommande((byte)code, ref gidr, objs);
        }

        public bool WriteCommande(BoardCodeCommande code, ref int gidr, params object[] objs)
        {
            return WriteCommande((byte)code, ref gidr, objs);
        }

        public bool WriteCommande(byte code, ref int gidr, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o, ref gidr);

                byte[] bts = memStream.ToArray();
                return (WriteChiffrer256(bts) >= bts.Length);
            }
        }

        public bool WriteCommande(ServeurCodeCommande code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            return WriteCommande((byte)code, setIdRéseau, ref gidr, objs);
        }

        public bool WriteCommande(BoardCodeCommande code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            return WriteCommande((byte)code, setIdRéseau, ref gidr, objs);
        }

        public bool WriteCommande(byte code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o, ref gidr, setIdRéseau);

                byte[] bts = memStream.ToArray();
                return (WriteChiffrer256(bts) >= bts.Length);
            }
        }
        #endregion WriteCommande

        #region EnqueueCommande
        public void EnqueueCommande(byte[] bts)
        {
            FileOrdres.Enqueue(bts);
        }

        public void EnqueueCommande(MemoryStream memStrm)
        {
            FileOrdres.Enqueue(memStrm.ToArray());
        }

        public void EnqueueCommande(BoardCodeCommande code, params object[] objs)
        {
            EnqueueCommande((byte)code, objs);
        }

        public void EnqueueCommande(BoardCodeCommande code, ref int gidr, params object[] objs)
        {
            EnqueueCommande((byte)code, ref gidr, objs);
        }

        public void EnqueueCommande(BoardCodeCommande code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            EnqueueCommande((byte)code, setIdRéseau, ref gidr, objs);
        }

        /*public void EnqueueCommande(BoardCodeCommande code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            EnqueueCommande((byte)code, setIdRéseau, ref gidr, objs);
        }*/

        /*public void EnqueueCommande(BoardCodeCommande code, byte[] m)
        {
            EnqueueCommande((byte)code, m);
        }*/

        public void EnqueueCommande(ServeurCodeCommande code, params object[] objs)
        {
            EnqueueCommande((byte)code, objs);
        }

        public void EnqueueCommande(ServeurCodeCommande code, ref int gidr, params object[] objs)
        {
            EnqueueCommande((byte)code, ref gidr, objs);
        }

        public void EnqueueCommande(ServeurCodeCommande code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            EnqueueCommande((byte)code, setIdRéseau, ref gidr, objs);
        }

        /*public void EnqueueCommande(ServeurCodeCommande code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            EnqueueCommande((byte)code, setIdRéseau, ref gidr, objs);
        }*/

        /*public void EnqueueCommande(ServeurCodeCommande code, byte[] m)
        {
            EnqueueCommande((byte)code, m);
        }*/

        /*protected void EnqueueCommande(byte code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o, ref gidr);
                FileOrdres.Enqueue(memStream.ToArray());
            }
        }*/

        protected void EnqueueCommande(byte code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o, ref gidr, setIdRéseau);
                FileOrdres.Enqueue(memStream.ToArray());
            }
        }

        protected void EnqueueCommande(byte code, ref int gidr, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o, ref gidr);
                FileOrdres.Enqueue(memStream.ToArray());
            }
        }

        protected void EnqueueCommande(byte code, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o);
                FileOrdres.Enqueue(memStream.ToArray());
            }
        }

        protected void EnqueueCommande(byte code, byte[] m = null)
        {
            FileOrdres.Enqueue(m.AddFirst(code));
        }
        #endregion EnqueueCommande

        #region Construire commande
        static public byte[] ConstruireCommande(ServeurCodeCommande code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            return ConstruireCommande((byte)code, setIdRéseau, ref gidr, objs);
        }
        static public byte[] ConstruireCommande(BoardCodeCommande code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            return ConstruireCommande((byte)code, setIdRéseau, ref gidr, objs);
        }

        static public byte[] ConstruireCommande(byte code, ISet<int> setIdRéseau, ref int gidr, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o, ref gidr, setIdRéseau);
                return memStream.ToArray();
            }
        }


        static public byte[] ConstruireCommande(ServeurCodeCommande code, ref int gidr, params object[] objs)
        {
            return ConstruireCommande((byte)code, ref gidr, objs);
        }
        static public byte[] ConstruireCommande(BoardCodeCommande code, ref int gidr, params object[] objs)
        {
            return ConstruireCommande((byte)code, ref gidr, objs);
        }

        static public byte[] ConstruireCommande(byte code, ref int gidr, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o, ref gidr);
                return memStream.ToArray();
            }
        }


        static public byte[] ConstruireCommande(ServeurCodeCommande code, params object[] objs)
        {
            return ConstruireCommande((byte)code, objs);
        }
        static public byte[] ConstruireCommande(BoardCodeCommande code, params object[] objs)
        {
            return ConstruireCommande((byte)code, objs);
        }

        static public byte[] ConstruireCommande(byte code, params object[] objs)
        {
            using (MemoryStream memStream = new MemoryStream())
            {
                memStream.WriteByte(code);
                foreach (object o in objs)
                    memStream.SerialiserObject(o);
                return memStream.ToArray();
            }
        }
        #endregion Construire commande

        /*public void EnqueuePaquet(byte[] paquet)
        {
            FileOrdres.Enqueue(paquet);
        }*/

        public delegate bool EstCommandeEquivalente(byte[] paquetA, byte[] paquetB);
        public void EnvoyerUnique(byte[] paquet, EstCommandeEquivalente estEq = null)
        {
            byte[] bts;
            LinkedList<byte[]> svQ = new LinkedList<byte[]>();
            if (estEq != null)
            {
                while (FileOrdres.TryDequeue(out bts))
                {
                    if (estEq(paquet, bts)) break;
                    else svQ.AddLast(bts);
                }
                svQ.AddLast(paquet);
                while (FileOrdres.TryDequeue(out bts)) svQ.AddLast(bts);
            }
            else
            {
                while (FileOrdres.TryDequeue(out bts))
                {
                    if (paquet[0] == bts[0])break;
                    else svQ.AddLast(bts);
                }
                svQ.AddLast(paquet);
                while (FileOrdres.TryDequeue(out bts)) svQ.AddLast(bts);
            }
            foreach (byte[] b in svQ) FileOrdres.Enqueue(b);
        }

        public bool EstEnFile(BoardCodeCommande code)
        {
            return EstEnFile((byte)code);
        }

        public bool EstEnFile(ServeurCodeCommande code)
        {
            return EstEnFile((byte)code);
        }

        public bool EstEnFile(byte code)
        {
            foreach (byte[] b in FileOrdres)
            {
                if (b[0] == code) return true;
            }
            return false;
        }

        public bool EstEnFile(byte[] data, EstCommandeEquivalente estEq = null)
        {
            if (estEq != null)
            {
                foreach (byte[] b in FileOrdres)
                {
                    if (estEq(data, b)) return true;
                }
            }
            else
            {
                foreach (byte[] b in FileOrdres)
                {
                    if (b[0] == data[0]) return true;
                }
            }
            return false;
        }

        public bool EcrireMotDePasseHash(BigInteger code)
        {
            return WriteUBigInteger(chiffrer256(OutilsRéseau.FastPowMod256(pwhash, code)));
        }

        public bool EstMotDePasseValide(BigInteger code)
        {
            return EstMotDePasseValide(ReadDéchiffrer256(), code);//LireMotDePasseHash
        }

        public bool EstMotDePasseValide(BigInteger hashPwd, BigInteger code)
        {
            BigInteger hashCode = OutilsRéseau.FastPowMod256(pwhash, code);
            return hashCode == hashPwd;
        }

        virtual public void NouvelleElement(IBinSerialisable elm)
        {
            if (elm != null && elm.ElmId != 0)
            {
                IBinSerialisable e;
                if (dicoElement.TryGetValue(elm.ElmId, out e)) //dicoElement.ContainsKey(elm.IdentifiantRéseau)
                {
                    ElementRéseau relm = e as ElementRéseau;
                    if (relm != null)
                    {
                        //relm.MettreAJour(elm);// relm.ARemplacerPar = elm;
                        foreach(KeyValuePair<int, IBinSerialisable> kv in dicoElement)
                        {
                            if (kv.Value != null)
                                kv.Value.MettreAJour(elm);
                        }
                        dicoElement[elm.ElmId] = elm;
                    }
                    else throw new Exception("Double élément détecté.");
                }
                else dicoElement.Add(elm.ElmId, elm);
            }
        }

        virtual public object Rechercher(int idElement)
        {
            if (idElement != 0)
            {
                IBinSerialisable elm;
                if (dicoElement.TryGetValue(idElement, out elm)) //dicoElement.ContainsKey(idElement)
                    return elm;//dicoElement[idElement];
                else
                {
                    Element res = new ElementRéseau(idElement);
                    dicoElement.Add(idElement, res);
                    return res;
                }
            }
            else return null;
        }

        virtual public object RetrouverObject(Stream stream)
        {
            byte[] bts = stream.GetBytes(4);
            if (bts != null) return Rechercher(BitConverter.ToInt32(bts, 0));
            else return null;
        }

        //Cela résoud le cas des dépendances circulaires (et il y en a...) !
        /*protected void VérifierLesEléments()
        {
            if (dicoElement.Any())
            {
                foreach (KeyValuePair<int, IBinSerialisable> kv in dicoElement)
                {
                    ElementRéseau elmRes = kv.Value as ElementRéseau;
                    if (elmRes != null && elmRes.ARemplacerPar != null)
                    {
                        foreach (KeyValuePair<int, IBinSerialisable> kvmaj in dicoElement)
                                kvmaj.Value.MettreAJour(elmRes.ARemplacerPar);
                    }
                }
                dicoElement.Clear();
            }
        }*/

        protected void VérifierBibliothèqueImages()
        {
            SortedSet<string> req = bibImg.ImageInconnues;
            if (req != null && req.Any())
            {
                if (ImagesDemandés != null) req.RemoveWhere(s => ImagesDemandés.Contains(s));
                else ImagesDemandés = new SortedSet<string>();
                foreach (string s in req) ImagesDemandés.Add(s);
                if (req.Any()) WriteCommande(ServeurCodeCommande.DemandeImage, req.ToList());
            }
        }

        protected void VérifierBibliothèqueModels()
        {
            SortedSet<string> req = BibModel.ModelInconnues;
            if (req != null && req.Any())
            {
                if (modelsDemandés != null) req.RemoveWhere(s => modelsDemandés.Contains(s));
                else modelsDemandés = new SortedSet<string>();
                foreach (string s in req) modelsDemandés.Add(s);
                if (req.Any()) WriteCommande(ServeurCodeCommande.DemandeModel, req.ToList());
            }
        }

        protected void VérifierLesBibliothèques()
        {
            VérifierBibliothèqueModels();
            VérifierBibliothèqueImages();
        }
    }
}
