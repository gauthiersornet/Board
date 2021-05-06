using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoardServer
{
    public class BoardSession
    {
        static public Dictionary<string, BoardSession> LstBoardSessions = new Dictionary<string, BoardSession>();

        public string NomSession { get; private set; }

        //Génération des identifiants locaux
        private int _IdElemReseau = 0;
        protected int NewIdElementBoard { get => (_IdElemReseau == int.MaxValue ? 1 : (++_IdElemReseau)); }
        private ulong VersionDuPartage = 0; //Identifiant de l'état courrant du board

        private BibliothèqueImage bibliothèqueImage = new BibliothèqueImage();
        private BibliothèqueModel bibliothèqueModel = new BibliothèqueModel();

        //private Thread thread
        private ClientThreadServer maître = null;
        private List<ClientThreadServer> clientThreadServers = new List<ClientThreadServer>();

        private BigInteger hashMotDePasseCréateur = 0;
        private BigInteger hashMotDePasseSession = 0;
        private bool demanderMaître; //Demander au créateur lors de la connex d'un joueur

        private Groupe root;

        public BoardSession(string nomSession, ClientThreadServer _maître, BigInteger _hashMotDePasseCréateur, BigInteger _hashMotDePasseSession, bool _demanderMaître)
        {
            maître = _maître;
            NomSession = nomSession;
            hashMotDePasseCréateur = _hashMotDePasseCréateur;
            hashMotDePasseSession = _hashMotDePasseSession;
            demanderMaître = _demanderMaître;
            root = new Groupe();
            lock (LstBoardSessions)
            {
                LstBoardSessions.Add(nomSession, this);
            }
            //thread = new Thread(new ThreadStart(fonctionnement));
            //thread.Start();
        }

        public bool Supprimer(ClientThreadServer demandeur)
        {
            if (demandeur == maître)
            {
                lock (this)
                {
                    Close();
                }
                return true;
            }
            else return false;
        }

        private void Close()
        {
            lock (LstBoardSessions)
            {
                LstBoardSessions.Remove(NomSession);
            }
            if (clientThreadServers != null)
            {
                lock (clientThreadServers)
                {
                    clientThreadServers.ForEach(c => c.QuiterSession(this));
                    clientThreadServers.Clear();
                }
                clientThreadServers = null;
            }
            if (root != null)
            {
                lock (root)
                {
                    root.Netoyer();
                }
                root = null;
            }
            if(bibliothèqueModel != null)
            {
                lock (bibliothèqueModel)
                {
                    bibliothèqueModel.Netoyer();
                }
                bibliothèqueModel = null;
            }
            if (bibliothèqueImage != null)
            {
                lock (bibliothèqueImage)
                {
                    bibliothèqueImage.Netoyer();
                }
                bibliothèqueImage = null;
            }
        }
    }
}
