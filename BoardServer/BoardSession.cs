using ModuleBOARD.Elements.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoardServer
{
    public class BoardSession
    {
        static public Dictionary<string, BoardSession> LstBoardSessions = new Dictionary<string, BoardSession>();

        public string NomSession { get; private set; }

        private ulong GenerateurNetworkId = 0; //Identifiant des éléments réseaux
        private ulong VersionDuPartage = 0; //Identifiant de l'état courrant du board

        private BibliothèqueImage bibliothèqueImage = new BibliothèqueImage();
        private BibliothèqueModel bibliothèqueModel = new BibliothèqueModel();

        //private Thread thread;
        private ClientThreadServer créateur = null;
        private List<ClientThreadServer> clientThreadServers = new List<ClientThreadServer>();

        private string motDePasseCréateur = null;
        private string motDePasseSession = null;
        private bool demanderCréateur; // demander au créateur lors de la connex d'un joueur

        public BoardSession(string nomSession, ClientThreadServer _créateur)
        {
            créateur = _créateur;
            NomSession = nomSession;
            lock(LstBoardSessions)
            {
                LstBoardSessions.Add(nomSession, this);
            }
            //thread = new Thread(new ThreadStart(fonctionnement));
            //thread.Start();
        }

        public void Close()
        {
            clientThreadServers.ForEach(c => c.SafeStop(1));
            clientThreadServers.ForEach(c => c.SafeStop());
            clientThreadServers.ForEach(c => c.Abort());
            clientThreadServers.ForEach(c => c.Close());
            clientThreadServers.Clear();
            clientThreadServers = null;
            //if (thread.Join(10000) == false)
            //thread.Abort();
        }
    }
}
