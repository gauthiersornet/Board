using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoardServer
{
    public class ClientThreadServer : ClientThread
    {
        public static Dictionary<string, ClientThreadServer> lstClientThread = new Dictionary<string, ClientThreadServer>();
        private BoardSession sessionEnCours;

        /*
            FinPaquet = 0,
            CréerSession = 1,//Nom de la session à créer et autres paramètres
            ActualiserSession = 2, //Vide
            RejoindreSession = 3, //Mot de passe en sha256
            SupprimerSession = 4, //Nom de la session, l'émétteur doit être le maître de session
        */

        static private MethodInfo[] InitServMethods()
        {
            ClientThreadServer _ct = new ClientThreadServer();
            return new MethodInfo[]
            {
                null,
                GetMethodInfo<string,BigInteger,BigInteger,bool>(_ct.créerSession),
                GetMethodInfo(_ct.EnvoyerSessions),
                null,//RejoindreSession
                GetMethodInfo<string>(_ct.SupprimerSession),
                null,
                null,
                null,
                null
            };
        }
        static private readonly MethodInfo[] ServMethods = InitServMethods();

        private ClientThreadServer() { }

        public ClientThreadServer(TcpClient _tcpClient, BigInteger _pwhash)
            : base(_tcpClient, _pwhash, null)
        {
            sessionEnCours = null;
            lock (lstClientThread)
            {
                if (Identifiant == null ||
                    OutilsRéseau.EstChaineSecurisée(Identifiant) == false ||
                    Identifiant.Length > OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX ||
                    UTF8Encoding.UTF8.GetBytes(Identifiant).Length > OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX ||
                    lstClientThread.ContainsKey(Identifiant))
                {
                    do
                    {
                        Identifiant = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 22);
                    } while (lstClientThread.ContainsKey(Identifiant));
                }
                lstClientThread.Add(Identifiant, this);
            }
            méthodesRéseau = ServMethods;
        }

        protected override bool Identifie()
        {
            BigInteger code = OutilsRéseau.SecuredRandomBigInteger128();
            WriteUBigInteger(code, 16);
            Guid attendu = OutilsRéseau.GuidEncode(GVersion, code);
            Guid ret = ReadGuid();
            bool res = (attendu == ret);
            stream.WriteByte(res ? byte.MaxValue : byte.MinValue);
            return res;
        }

        protected override bool EchangerIdentifiant()
        {
            BigInteger code = OutilsRéseau.SecuredRandomBigInteger256();
            WriteChiffrer256(code);
            string id = ReadDéChiffrer256((OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31).bytesToString();
            if (EstMotDePasseValide(code))
            {
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
            }
            else Identifiant = "";
            WriteChiffrer256(Identifiant.stringToBytes(), (OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31);
            return Identifiant != "";
        }

        public void EnvoyerSessions()
        {
            lock (BoardSession.LstBoardSessions)
            {
                WriteChiffrer256(BoardCodeCommande.AjouterSession, "".stringToBytes());
                foreach (KeyValuePair<string, BoardSession> kv in BoardSession.LstBoardSessions)
                {
                    WriteChiffrer256(BoardCodeCommande.AjouterSession, kv.Value.NomSession.stringToBytes());
                }
            }
        }

        private void créerSession(string nomSession, BigInteger hashPwdMaître, BigInteger hashPwdJoueur, bool prévenirMaître)
        {
            lock (BoardSession.LstBoardSessions)
            {
                if (BoardSession.LstBoardSessions.ContainsKey(nomSession))
                    WriteCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.RefuSession, nomSession);
                else
                {
                    new BoardSession(nomSession, this, hashPwdMaître, hashPwdJoueur, prévenirMaître);
                    WriteCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.CréaSession, nomSession);
                }
            }
        }

        private void SupprimerSession(string nomSession)
        {
            bool ok = false;
            BoardSession bs = null;
            lock (BoardSession.LstBoardSessions)
            {
                if (BoardSession.LstBoardSessions.ContainsKey(nomSession))
                    bs = BoardSession.LstBoardSessions[nomSession];
            }
            if (bs != null) ok = bs.Supprimer(this);

            if (ok)
            {
                WriteCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.Information, "La session \"" + nomSession + "\" a été supprimée.");
                EnvoyerSessions();
            }
            else WriteCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.Erreur, "Erreur de suppression de la session \"" + nomSession + "\".\nVous devez être maître de la session que voulez supprimer.\nSi tel est le cas, il vous faudra rejoindre celle-ci avec le mot de passe maître.");
        }

        public override void Close()
        {
            lock (lstClientThread)
            {
                lstClientThread.Remove(this.Identifiant);
            }
            base.Close();
        }

        public void QuiterSession(BoardSession bs)
        {
            lock (this)
            {
                if (bs != sessionEnCours)
                    return;
            }
            sessionEnCours = null;
            EnqueueCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.QuitSession, bs.NomSession);
        }
    }
}
