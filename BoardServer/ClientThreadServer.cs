using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoardServer
{
    public class ClientThreadServer : ClientThread
    {
        private static ulong GenerateurJoueurNetworkId = 0; //Identifiant des joueurs réseaux
        public static Dictionary<string, ClientThreadServer> lstClientThread = new Dictionary<string, ClientThreadServer>();
        
        private BoardSession sessionEnCours;

        public ClientThreadServer(TcpClient _tcpClient)
            :base(_tcpClient, null)
        {
            sessionEnCours = null;
            ++GenerateurJoueurNetworkId;
            IdentifiantUL = GenerateurJoueurNetworkId;
            lock (lstClientThread)
            {
                if (Identifiant == null ||
                    OutilsRéseau.EstChaineSecurisée(Identifiant) == false ||
                    Identifiant.Length > OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX ||
                    UTF8Encoding.UTF8.GetBytes(Identifiant).Length > OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX ||
                    lstClientThread.ContainsKey(Identifiant))
                {
                    Identifiant = "?" + Convert.ToBase64String(new byte[8].ULongToBytes(IdentifiantUL)) + "?";
                }
                lstClientThread.Add(Identifiant, this);
            }
        }

        private bool Identifie()
        {
            BigInteger code = OutilsRéseau.SecuredRandomBigInteger128();
            WriteUBigInteger(code, 16);
            Guid attendu = OutilsRéseau.GuidEncode(GVersion, code);
            Guid ret = ReadGuid();
            bool res = (attendu == ret);
            stream.WriteByte(res ? byte.MaxValue : byte.MinValue);
            return res;
        }

        private void echangerIdentifiant()
        {
            WriteChiffrer256(IdentifiantUL);
            string id = ReadDéChiffrer256((OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31).bytesToString();
            if (string.IsNullOrWhiteSpace(id) == false && OutilsRéseau.EstChaineSecurisée(id))
            {
                lock (lstClientThread)
                {
                    if (lstClientThread.ContainsKey(id) == false)
                    {
                        lstClientThread.Remove(this.Identifiant);
                        Identifiant = id;
                        lstClientThread.Add(this.Identifiant, this);
                    }
                }
            }
            WriteChiffrer256(Identifiant.stringToBytes(), (OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31);
        }

        private void envoyerSessions()
        {
            lock(BoardSession.LstBoardSessions)
            {
                foreach(KeyValuePair<string, BoardSession> kv in BoardSession.LstBoardSessions)
                {
                    WriteChiffrer256(BoardCodeCommande.AjouterSession, kv.Value.NomSession.stringToBytes(), ((OutilsRéseau.NB_OCTET_NOM_SESSION_MAX + 30) / 31));
                }
            }
        }

        protected override void fonctionnement()
        {
            if (Identifie())
            {
                InitChiffrage();
                echangerIdentifiant();
                envoyerSessions();

                // Tant que le thread n'est pas tué, on travaille
                while (Thread.CurrentThread.IsAlive && fonctionne && tcpClient.Connected)
                {
                    while (stream.DataAvailable)
                    {
                        byte[] block = ReadDéChiffrer256();
                        if (block != null)
                        {
                            switch ((ServeurCodeCommande)block[0])
                            {
                                case ServeurCodeCommande.ActualiserSession:
                                    envoyerSessions();
                                    break;
                                default:
                                    Close();
                                    fonctionne = false;
                                    break;
                            }
                        }
                    }
                    // Attente de 100 ms
                    Thread.Sleep(100);
                }
            }
            Close();
        }

        protected void Close()
        {
            lock (lstClientThread)
            {
                lstClientThread.Remove(this.Identifiant);
            }
            base.Close();
        }
    }
}
