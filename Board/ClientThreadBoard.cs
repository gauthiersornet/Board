using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModuleBOARD.Elements.Base;
using ModuleBOARD.Réseau;

namespace Board
{
    public class ClientThreadBoard : ClientThread
    {
        private delegate void dlgVoid();
        private delegate void dlgVoidString(string str);
        private delegate void dlgVoidBoolString(bool ok, string str);
        private delegate void dlgVoidCharString(Char type, string str);
        private delegate void dlgVoidTypeMessString(OutilsRéseau.EMessage type, string str);

        public string NomSession { get; private set; } = null;
        private BigInteger SessionHashPwd = 0;
        private Board board;
        private IAsyncResult boardAsyncResult = null;

        /*
            FinPaquet = 0, //Vide
            MessageServeur = 1, //bool ok, Nom de la session
            AjouterSession = 2, //Nom de la session
            SessionRejoint = 3 //Message si err
        */
        static private MethodInfo[] InitBoardMethods()
        {
            ClientThreadBoard _ct = new ClientThreadBoard();
            return new MethodInfo[]
            {
                null,
                GetMethodInfo<OutilsRéseau.EMessage, string>(_ct.MessageServeur),
                GetMethodInfo<string>(_ct.AjoutSession),
                null,
                null,
                null,
                null,
                null,
                null
            };
        }
        static private readonly MethodInfo[] BoardMethods = InitBoardMethods();

        private ClientThreadBoard() { }

        public ClientThreadBoard(string _identifiant, BigInteger _pwhash, TcpClient _tcpClient, Board _board)
            :base(_tcpClient, _pwhash, _identifiant)
        {
            board = _board;
            méthodesRéseau = BoardMethods;
        }

        protected override bool Identifie()
        {
            BigInteger code = ReadUBigInteger(16);
            Guid attendu = OutilsRéseau.GuidEncode(GVersion, code);
            WriteGuid(attendu);
            int res = stream.ReadByte();
            if (res == 0xFF)
            {
                return true;
            }
            else
            {
                InvoquerBoard(OutilsRéseau.EMessage.Erreur, "Votre version n'est pas compatible.");
                return false;
            }
        }

        protected override bool EchangerIdentifiant()
        {
            BigInteger code = ReadDéchiffrer256();
            WriteChiffrer256(Identifiant.stringToBytes(), (OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31);
            EcrireMotDePasseHash(code);
            string id = ReadDéChiffrer256((OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31).bytesToString();
            if (id != "")
            {
                if (id != Identifiant)
                {
                    InvoquerBoard(OutilsRéseau.EMessage.IdentifiantRefusée, id);
                    Identifiant = id;
                }
                else InvoquerBoard(OutilsRéseau.EMessage.Information, "Connexion réussie !");
                return true;
            }
            else
            {
                InvoquerBoard(OutilsRéseau.EMessage.Erreur, "Identifiant ou mot de passe invalide.");
                return false;
            }
        }

        public override void Close()
        {
            if (tcpClient != null) InvoquerBoard(OutilsRéseau.EMessage.Déconnexion, "Déconnexion");
            LibererBoard();
            base.Close();
        }

        private void MessageServeur(OutilsRéseau.EMessage type, string message)
        {
            switch (type)
            {
                case OutilsRéseau.EMessage.JoinSession:
                    NomSession = message;
                    break;
            }
            InvoquerBoard((dlgVoidTypeMessString)(board.MessageServeur), type, message);
        }

        private void AjoutSession(string nomSession)
        {
            GérerSessions js = ObtenirBoard()?.jSession;
            if (js != null) InvoquerBoard((dlgVoidString)(js.AjouterSession), nomSession);
        }

        /*protected override void fonctionnement()
        {
            if (Identifie())
            {
                InitChiffrage();

                if (EchangerIdentifiant())
                {
                    etat = EClientThreadEtat.Connecté;
                    InvoquerBoard((dlgVoid)(board.ConnectionRéussie));
                    SyncroBoard();

                    // Tant que le thread n'est pas tué, on travaille
                    while (Thread.CurrentThread.IsAlive && fonctionne && tcpClient.Connected)
                    {
                        while (stream.DataAvailable)
                        {
                            fluxEntrant = ReadMemStreamDéchiffrer256();
                            if (fluxEntrant != null)
                            {
                                for (int cmd = fluxEntrant.ReadByte(); 0 < cmd && cmd <= 255; cmd = fluxEntrant.ReadByte())
                                {
                                    //BoardCodeCommande cmd = (BoardCodeCommande)memStreamLecture.ReadByte();
                                    switch ((BoardCodeCommande)cmd)
                                    {
                                        case BoardCodeCommande.AjouterSession:

                                            break;
                                        case BoardCodeCommande.MessageServeur:
                                            {

                                            }
                                            break;
                                        default:
                                            Close();
                                            fonctionne = false;
                                            break;
                                    }
                                }
                            }
                        }

                        WriteQueue();

                        // Attente de 10 ms
                        if (stream.DataAvailable == false) Thread.Sleep(100);
                    }

                    InvoquerBoard((dlgVoidString)(board.PerteDeConnexion), "Déconnexion");
                }
                else InvoquerBoard((dlgVoidString)(board.ConnectionRattée), "Identifiant ou mot de passe invalide.");
            }
            else InvoquerBoard((dlgVoidString)(board.ConnectionRattée), "Votre version n'est pas compatible.");
            Close();
        }*/

        #region Board
        public void SyncroBoard()
        {
            if (boardAsyncResult != null && boardAsyncResult.IsCompleted == false)
            {
                if (boardAsyncResult.AsyncWaitHandle.WaitOne(2000)) boardAsyncResult = null;
            }
        }

        public Board ObtenirBoard()
        {
            Board brd;
            lock (this)
            {
                brd = board;
            }
            return brd;
        }

        private bool InvoquerBoard(OutilsRéseau.EMessage type, string message)
        {
            Board brd = ObtenirBoard();
            if (brd != null)
            {
                if (brd.InvokeRequired)
                {
                    SyncroBoard();
                    try
                    {
                        boardAsyncResult = brd.BeginInvoke((dlgVoidTypeMessString)(board.MessageServeur), type, message);
                    }
                    catch
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    board.MessageServeur(type, message);
                    return true;
                }
            }
            else return false;
        }

        private bool InvoquerBoard(Delegate dlg, params object[] args)
        {
            if (dlg != null)
            {
                SyncroBoard();
                Board brd = ObtenirBoard();
                if (brd != null)
                {
                    try
                    {
                        boardAsyncResult = brd.BeginInvoke(dlg, args);
                    }
                    catch
                    {
                        return false;
                    }
                    return true;
                }
                else return false;
            }
            else return false;
        }

        public void LibererBoard()
        {
            lock (this)
            {
                board = null;
            }
        }
        #endregion Board

        public void ActualiserLstSession()
        {
            if (EstEnFile(ServeurCodeCommande.ActualiserSession) == false)
                EnqueueCommande(ServeurCodeCommande.ActualiserSession);
        }

        public bool CréerSession(string nomSession, BigInteger hashMotDePasseMaitre, BigInteger hashMotDePasseJoueur, bool demanderMaitre)
        {
            if (EstEnFile(ServeurCodeCommande.CréerSession) == false)
            {
                EnqueueCommande(ServeurCodeCommande.CréerSession, nomSession, hashMotDePasseMaitre, hashMotDePasseJoueur, demanderMaitre);
                return true;
            }
            else return false;
        }

        public bool SupprimerSession(string nomSession)
        {
            if (EstEnFile(ServeurCodeCommande.SupprimerSession) == false)
            {
                EnqueueCommande(ServeurCodeCommande.SupprimerSession, nomSession);
                return true;
            }
            else return false;
        }
    }
}
