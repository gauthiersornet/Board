using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ModuleBOARD.Réseau
{
    public abstract class ClientThread
    {
        public enum BoardCodeCommande : byte
        {
            AjouterSession = 0
        }

        public enum ServeurCodeCommande : byte
        {
            ActualiserSession = 0
        }

        public enum EClientThreadEtat : byte
        {
            NonConnecté = 0,
            Connecté = 128,
        }

        private Random rnd = new Random();

        public string ObtenirIdentifiant() { return Identifiant; }
        protected string Identifiant = null;
        protected ulong IdentifiantUL = 0;
        protected TcpClient tcpClient;
        protected NetworkStream stream;
        protected Thread thread;
        protected bool fonctionne;
        protected EClientThreadEtat etat = EClientThreadEtat.NonConnecté;

        protected BigInteger chivK;
        protected BigInteger dechifK;

        static public Guid GVersion { get => new Guid("867dc535-5473-46dd-ab60-f61e6deaf3f3"); }

        public ClientThread(TcpClient _tcpClient, string identifiant = null)
        {
            //_tcpClient.ReceiveTimeout = 1000;
            //_tcpClient.SendTimeout = 10000;
            if (identifiant != null)
            {
                if (identifiant =="" || (identifiant.Length < 30 && !string.IsNullOrWhiteSpace(identifiant) && identifiant.EstChaineSecurisée()))
                {
                    Identifiant = identifiant;
                }
                else throw new Exception("L'identifiant fait plus de 30 caractères ou contient des caractères interdis.");
            }
            thread = new Thread(new ThreadStart(fonctionnement));
            tcpClient = _tcpClient;
            stream = tcpClient.GetStream();
            fonctionne = true;
            thread.Start();
        }

        protected void InitChiffrage()
        {
            Random rnd = new Random();
            BigInteger g = OutilsRéseau.SecuredRandomBigInteger256(rnd);
            WriteUBigInteger(g, 32);
            g ^= ReadUBigInteger(32);
            Thread.Sleep(rnd.Next(256));
            rnd = new Random();
            BigInteger p = OutilsRéseau.SecuredRandomBigInteger256(rnd);
            WriteUBigInteger(OutilsRéseau.FastPowMod(g, p, OutilsRéseau.Prime256Bits), 32);
            chivK = dechifK = OutilsRéseau.FastPowMod(ReadUBigInteger(32), p, OutilsRéseau.Prime256Bits);
        }

        protected byte[] ReadBytes(int nbOctets)
        {
            if (nbOctets <= OutilsRéseau.NB_OCTET_MAX)
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
                catch (IOException ex)
                {
                    return null;
                }
            }
            else return null;
        }

        /*protected byte[] chiffrer256(BoardCodeCommande code, byte[] m, int nbBlock = -1)
        {
            return chiffrer256((byte)code, m, nbBlock);
        }

        protected byte[] chiffrer256(ServeurCodeCommande code, byte[] m, int nbBlock = -1)
        {
            return chiffrer256((byte)code, m, nbBlock);
        }*/

        /*protected byte[] chiffrer256(byte code, byte[] m, int nbBlock = -1)
        {
            byte[] nb = new byte[m.Length + 1];
            nb[0] = code;
            Array.Copy(m, 0, nb, 1, m.Length);
            return chiffrer256(nb);
        }*/

        protected byte[] chiffrer256(byte[] m, int nbBlock = -1)
        {
            int nbBlockM = ((m.Length + 30) / 31);
            if (nbBlock <= 0) nbBlock = nbBlockM;
            byte[] res = new byte[nbBlock * 32];
            byte[] buff = new byte[31];
            for (int i = 0; i < nbBlock; ++i)
            {
                if (i < nbBlockM)
                {
                    int restant = m.Length - i * 31;
                    if (restant < 31)
                    {
                        Array.Copy(m, i * 31, buff, 0, restant);
                        /*++restant;//De façon à laisser un zéro
                        if (restant < 31)
                        {*/
                            byte[] rndBuff = new byte[31 - restant];
                            rnd.NextBytes(rndBuff);
                            Array.Copy(rndBuff, 0, buff, restant, 31 - restant);
                        //}
                    }
                    else Array.Copy(m, i * 31, buff, 0, 31);
                }
                else
                {
                    rnd.NextBytes(buff);
                    //if (i == nbBlockM && m.Length % 31 == 0) buff[0] = 0;//De façon à laisser un zéro
                }
                BigInteger bint = chiffrer256(new BigInteger(buff));
                byte[] bintB = bint.ToByteArray();
                Array.Copy(bintB, 0, res, i * 32, bintB.Length);
            }
            return res;
        }

        protected BigInteger chiffrer256(BigInteger bint)
        {
            return bint ^ chivK;
        }

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
            if(m != null && m.Length>0)Array.Copy(m, 0, nb, 1, m.Length);

            byte[] cyf = chiffrer256(nb, nbBlock);
            WriteBytes(cyf);
            return cyf.Length;
        }

        protected int WriteChiffrer256(ulong val)
        {
            byte[] nb = new byte[Marshal.SizeOf(val)];
            nb.ULongToBytes(val);
            byte[] cyf = chiffrer256(nb, ((Marshal.SizeOf(val)+30)/31));
            WriteBytes(cyf);
            return cyf.Length;
        }

        protected int WriteChiffrer256(byte[] m, int nbBlock = -1)
        {
            byte[] cyf = chiffrer256(m, nbBlock);
            WriteBytes(cyf);
            return cyf.Length;
        }

        protected byte[] déchiffrer256(byte[] m)
        {
            if (m == null || m.Length % 32 != 0) return null;

            int nbBlock = (m.Length / 32);
            byte[] res = new byte[nbBlock * 31];
            byte[] buff = new byte[32];
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

        protected byte[] ReadDéChiffrer256(int nbBlock = 1)
        {
            return déchiffrer256(ReadBytes(32 * nbBlock));
        }

        /// <summary>
        /// Saute le code commande et concatène avec les blocs suivants
        /// </summary>
        /// <param name="nbBlock"></param>
        /// <returns></returns>
        protected byte[] ReadDéChiffrer256Data(byte[] commandBlock, int nbDataBlock = 1)
        {
            byte[] dataBlock = ReadDéChiffrer256(nbDataBlock);
            byte[] fullDataBlocks = new byte[commandBlock.Length - OutilsRéseau.NB_OCTET_COMMANDE + dataBlock.Length];
            Array.Copy(commandBlock, OutilsRéseau.NB_OCTET_COMMANDE, fullDataBlocks, 0, commandBlock.Length - OutilsRéseau.NB_OCTET_COMMANDE);
            Array.Copy(dataBlock, 0, fullDataBlocks, commandBlock.Length - OutilsRéseau.NB_OCTET_COMMANDE, dataBlock.Length);
            return fullDataBlocks;
        }

        protected ulong ReadULChiffrer256()
        {
            byte[] bts = ReadDéChiffrer256(1);
            return (bts != null && bts.Length == 32 ? bts.BytesToULong() : 0);
        }

        protected Guid ReadGuid()
        {
            byte[] bts = ReadBytes(16);
            return (bts != null && bts.Length == 16 ? new Guid(bts) : Guid.Empty);
        }

        protected BigInteger ReadBigInteger(int nbOctets)
        {
            return new BigInteger(ReadBytes(nbOctets));
        }

        protected BigInteger ReadUBigInteger(int nbOctets)
        {
            try
            {
                byte[] res = new byte[nbOctets + 1];
                int nbrb;
                for (int i = 0; i < nbOctets && fonctionne; i += nbrb)
                {
                    nbrb = stream.Read(res, i, nbOctets - i);
                }
                return new BigInteger(res);
            }
            catch(IOException ex)
            {
                return BigInteger.Zero;
            }
        }

        protected void WriteGuid(Guid guid)
        {
            /*lock (stream) {*/ WriteBytes(guid.ToByteArray(), 16); /*}*/
        }

        protected void WriteBytes(byte[] octets)
        {
            /*lock (stream) {*/ stream.Write(octets, 0, octets.Length); /*}*/
        }

        protected int WriteBytes(byte[] octets, int nbOctets)
        {
            if (nbOctets < 0 || nbOctets == octets.Length)
            {
                WriteBytes(octets);
                return octets.Length;
            }
            else
            {
                byte[] wB = new byte[nbOctets];
                Array.Copy(octets, wB, Math.Min(nbOctets, octets.Length));
                WriteBytes(wB);
                return nbOctets;
            }
        }

        private int WriteBigInteger(BigInteger bint, int nbOctets = -1)
        {
            return WriteBytes(bint.ToByteArray(), nbOctets);
        }

        protected bool WriteUBigInteger(BigInteger bint, int nbOctets = -1)
        {
            byte[] bIntArr = bint.ToByteArray();
            if(bIntArr.Length > 0 && bIntArr[bIntArr.Length-1] == 0)
            {
                byte[] wB = new byte[bIntArr.Length - 1];
                Array.Copy(bIntArr, wB, wB.Length);
                bIntArr = wB;
            }
            if (nbOctets < 0 || nbOctets == bIntArr.Length)
            {
                WriteBytes(bIntArr);
                return true;
            }
            else if (nbOctets < bIntArr.Length) return false;
            else
            {
                byte[] wB = new byte[nbOctets];
                Array.Copy(bIntArr, wB, bIntArr.Length);
                WriteBytes(wB);
                return true;
            }
        }

        protected abstract void fonctionnement();

        public void Close()
        {
            if(stream != null)
            {
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
    }
}
