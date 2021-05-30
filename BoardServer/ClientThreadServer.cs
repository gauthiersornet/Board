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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoardServer
{
    public class ClientThreadServer : ClientThread
    {
        public static List<ClientThreadServer> lstClientThread = new List<ClientThreadServer>();
        public BoardSession sessionEnCours;

        /*public ushort IdJoueur = 0;
        public PointF P; // position connue du joueur
        public List<Element> LstElementAttrapés;*/
        public Joueur Joueur;
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
                null,//0
                GetMethodInfo(_ct.Déconnexion),//déconnexion
                GetMethodInfo<string,BigInteger,BigInteger,bool>(_ct.créerSession),
                GetMethodInfo(_ct.EnvoyerSessions),
                GetMethodInfo<string, BigInteger>(_ct.RejoindreSession),
                GetMethodInfo<string>(_ct.SupprimerSession),
                GetMethodInfo(_ct.QuitterSession),
                GetMethodInfo(_ct.DemandeSynchro),//7
                null,
                null,
                null,
                null,//11
                null,
                null,
                null,
                GetMethodInfo<List<int>>(_ct.DemandeElement),//15
                GetMethodInfo<List<Element>>(_ct.RéceptionElement),
                GetMethodInfo<List<string>>(_ct.DemandeImage),
                GetMethodInfo<List<string>>(_ct.DemandeModel),
                GetMethodInfo(_ct.RéceptionImage),
                GetMethodInfo(_ct.RéceptionModel),//20
                GetMethodInfo<List<Element>>(_ct.ChargerElement),
                GetMethodInfo<int, Element.EEtat>(_ct.ChangerEtatElément),
                GetMethodInfo<int, int>(_ct.RouletteElément),
                GetMethodInfo<int, int>(_ct.TournerElément),//24
                GetMethodInfo<PointF, int, int>(_ct.AttraperElement), // On prend celui que l'on a ciblé !
                GetMethodInfo<PointF, int, int>(_ct.PiocherElement), // On pioche celui que l'on a cible et donc si pile non vide alors bnouv element
                GetMethodInfo<PointF, int>(_ct.LacherElement), // On lache tout les éléments sur un autre

                GetMethodInfo<int>(_ct.RangerVersParent),//28
                GetMethodInfo<int>(_ct.Mélanger),//29
                GetMethodInfo<int>(_ct.DéfausserElement),//30
                GetMethodInfo<int>(_ct.ReMettreDansPioche),//31
                GetMethodInfo<int>(_ct.MettreEnPioche),
                GetMethodInfo<int>(_ct.CréerLaDéfausse),
                GetMethodInfo<int>(_ct.MettreEnPaquet),

                GetMethodInfo<int>(_ct.Supprimer),
                GetMethodInfo(_ct.SupprimerTout)
            };
        }
        static private readonly MethodInfo[] ServMethods = InitServMethods();

        private ClientThreadServer() { }

        public ClientThreadServer(TcpClient _tcpClient, BigInteger _pwhash, ConcurrentQueue<string> journal)
            : base(_tcpClient, _pwhash, journal, null)
        {
            sessionEnCours = null;
            //LstElementAttrapés = null;
            Joueur = null;

            lock (lstClientThread)
            {
                lstClientThread.Add(this);
            }
            méthodesRéseau = ServMethods;
        }

        protected override bool Identifie()
        {
            BigInteger code = OutilsRéseau.SecuredRandomBigInteger128();
            if (!WriteUBigInteger(code, 16)) return false;
            Guid attendu = OutilsRéseau.GuidEncode(GVersion, code);
            Guid ret = ReadGuid();
            bool res = (attendu == ret);
            try { stream.WriteByte(res ? byte.MaxValue : byte.MinValue); }
            catch { res = false; }
            Thread.Sleep(1000);
            return res;
        }

        protected override bool EchangerIdentifiant()
        {
            BigInteger code = OutilsRéseau.SecuredRandomBigInteger256();
            WriteChiffrer256(code);
            string id = ReadDéChiffrer256((OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31).bytesToString();
            if (EstMotDePasseValide(code))
            {
                lock (lstClientThread)
                {
                    if (string.IsNullOrWhiteSpace(id) == false &&
                        OutilsRéseau.EstChaineSecurisée(id) &&
                        id.Length <= OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX &&
                        UTF8Encoding.UTF8.GetBytes(id).Length <= OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX &&
                        !lstClientThread.Any(c => (c != this && c.Identifiant == id))
                        ) Identifiant = id;
                    else
                    {
                        do
                        {
                            id = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 22);
                        } while (lstClientThread.Any(c => (c != this && c.Identifiant == id)));
                        Identifiant = id;
                    }
                }
            }
            else Identifiant = "";
            WriteChiffrer256(Identifiant.stringToBytes(), (OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31);

            return Identifiant != "";
        }

        #region Méthodes appelées via thread réseau
        public Element TrouverElementRéseau(int idRez)
        {
            Joueur jr = Joueur;
            if (idRez > 0 && jr != null) return jr.TrouverElementRéseau(idRez);
            else return null;
        }

        public void MajElement(Element elm)
        {
            if(elm != null && !(elm is ElementRéseau))
            {
                Joueur jr = Joueur;
                if(jr != null) jr.MajElement(elm);
            }
        }

        public bool EnvoyerSessions()
        {
            lock (BoardSession.LstBoardSessions)
            {
                /*if(WriteChiffrer256(BoardCodeCommande.AjouterSession, "".stringToBytes()) < 1) return false;
                foreach (KeyValuePair<string, BoardSession> kv in BoardSession.LstBoardSessions)
                {
                    byte[] bts = kv.Value.NomSession.stringToBytes();
                    if (WriteChiffrer256(BoardCodeCommande.AjouterSession, bts) < bts.Length) return false;
                }*/
                WriteCommande(BoardCodeCommande.ActualiserSessions, BoardSession.LstBoardSessions.Keys.ToArray());
            }
            return true;
        }

        private bool créerSession(string nomSession, BigInteger hashPwdMaître, BigInteger hashPwdJoueur, bool prévenirMaître)
        {
            lock (BoardSession.LstBoardSessions)
            {
                if (String.IsNullOrWhiteSpace(nomSession) || !OutilsRéseau.EstChaineSecurisée(nomSession) || BoardSession.LstBoardSessions.Count >= 10 || BoardSession.LstBoardSessions.ContainsKey(nomSession))
                    WriteCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.RefuSession, nomSession);
                else
                {
                    new BoardSession(nomSession, this, hashPwdMaître, hashPwdJoueur, prévenirMaître);
                    WriteCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.CréaSession, nomSession);
                }
            }
            return true;
        }

        private bool RejoindreSession(string nomSession, BigInteger hash)
        {
            Thread.Sleep(1000);
            BoardSession bs = null;
            lock (BoardSession.LstBoardSessions)
            {
                if (BoardSession.LstBoardSessions.ContainsKey(nomSession))
                    bs = BoardSession.LstBoardSessions[nomSession];
            }
            if (bs != null)
            {
                if (bs.JointLaSession(this, hash))
                {
                    bibImg = bs.bibliothèqueImage;
                    bibMod = bs.bibliothèqueModel;
                    //sessionEnCours = bs;
                }
            }
            return true;
        }

        private bool SupprimerSession(string nomSession)
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
            return true;
        }

        private bool QuitterSession()//Le joueur demande à quitter la session en cours
        {
            BoardSession bs = sessionEnCours;
            if (bs != null)
            {
                bs.QuitteLaSession(this);
                lock (this) { sessionEnCours = null; }
            }
            return true;
        }

        private bool Déconnexion()
        {
            QuitterSession();
            fonctionne = false;
            return true;
        }

        private bool DemandeSynchro()
        {
            BoardSession bs = sessionEnCours;
            if(bs != null) bs.Synchroniser(this);
            return true;
        }

        private bool DemandeElement(List<int> idElms)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null) bs.DemandeElement(this, idElms);
            return true;
        }

        private bool RéceptionElement(List<Element> element)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null) bs.RéceptionElement(this, element);
            return true;
        }

        private bool DemandeImage(List<string> idImgs)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null) bs.DemandeImage(this, idImgs);
            return true;
        }

        private bool DemandeModel(List<string> idMods)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null) bs.DemandeModel(this, idMods);
            return true;
        }

        private bool RéceptionImage()
        {
            Image img = BibliothèqueImage.ChargerImage(ref fluxEntrant);
            BoardSession bs = sessionEnCours;
            if (bs != null) bs.NouvelleVersion(this, img);
            return true;
        }

        private bool RéceptionModel()
        {
            Model2_5D mld = BibliothèqueModel.ChargerModel(fluxEntrant, bibImg);
            VérifierBibliothèqueImages();
            BoardSession bs = sessionEnCours;
            if (bs != null) bs.NouvelleVersion(this, mld);
            return true;
        }

        private bool ChargerElement(List<Element> elements)
        {
            BoardSession bs = sessionEnCours;
            if (elements != null && elements.Any() && bs != null)
            {
                VérifierLesBibliothèques();
                bs.ChargerElement(this, elements);
            }
            return true;
        }

        private bool ChangerEtatElément(int idElm, Element.EEtat etat)
        {
            BoardSession bs = sessionEnCours;
            if(bs != null && idElm>0) bs.ChangerEtatElément(this, idElm, etat);
            return true;
        }

        private bool RouletteElément(int idElm, int delta)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.RouletteElément(this, idElm, delta);
            return true;
        }

        private bool TournerElément(int idElm, int delta)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.TournerElément(this, idElm, delta);
            return true;
        }

        private bool AttraperElement(PointF pj, int idElmPSrc, int idElmAtrp) // On reçoi un élément déjà en jeu
        {
            BoardSession bs = sessionEnCours;
            //Journaliser("Attraper Version partage = " + bs.VersionDuPartage);
            if (bs != null && idElmPSrc > 0 && idElmAtrp > 0) bs.AttraperElement(this, pj, idElmPSrc, idElmAtrp);
            return true;
        }

        private bool PiocherElement(PointF pj, int idElmPSrc, int index) // On pioche un nouvelle élément
        {
            BoardSession bs = sessionEnCours;
            //Journaliser("Piocher Version partage = " + bs.VersionDuPartage);
            if (bs != null && idElmPSrc > 0) bs.PiocherElement(this, pj, idElmPSrc, index);
            return true;
        }

        private bool LacherElement(PointF pj, int idElmRecep) // On lache tout les élément sur un autre
        {
            BoardSession bs = sessionEnCours;
            //Journaliser("Lacher Version partage = " + bs.VersionDuPartage);
            if (bs != null && idElmRecep > 0) bs.LacherElément(this, pj, idElmRecep);
            return true;
        }





        private bool RangerVersParent(int idElm)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.RangerVersParent(this, idElm);
            return true;
        }

        private bool Mélanger(int idElm)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.Mélanger(this, idElm);
            return true;
        }

        private bool DéfausserElement(int idElm)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.DéfausserElement(this, idElm);
            return true;
        }

        private bool ReMettreDansPioche(int idElm)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.ReMettreDansPioche(this, idElm);
            return true;
        }

        private bool MettreEnPioche(int idElm)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.MettreEnPioche(this, idElm);
            return true;
        }

        private bool CréerLaDéfausse(int idElm)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.CréerLaDéfausse(this, idElm);
            return true;
        }

        private bool MettreEnPaquet(int idElm)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.MettreEnPaquet(this, idElm);
            return true;
        }

        private bool Supprimer(int idElm)
        {
            BoardSession bs = sessionEnCours;
            if (bs != null && idElm > 0) bs.Supprimer(this, idElm);
            return true;
        }

        private bool SupprimerTout()
        {
            BoardSession bs = sessionEnCours;
            if (bs != null) bs.SupprimerTout(this);
            return true;
        }
        #endregion

        public override void Close()
        {
            BoardSession bs = sessionEnCours;
            if(bs != null) bs.QuitteLaSession(this);
            lock (this)
            {
                sessionEnCours = null;
                bibImg = null;
                bibMod = null;
            }
            lock (lstClientThread)
            {
                lstClientThread.Remove(this);
            }
            base.Close();
        }

        public void QuiterSession(BoardSession bs)
        {
            if (sessionEnCours != null)
            {
                lock (this)
                {
                    if (bs != sessionEnCours)
                        return;
                    bibImg = null;
                    bibMod = null;
                    sessionEnCours = null;
                }
                EnqueueCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.QuitSession, bs.NomSession);
            }
        }

        /*override protected void fonctionnement()
        {
            try
            {
                if (Identifie())
                {
                    if (InitChiffrage() && EchangerIdentifiant())
                    {
                        // Tant que le thread n'est pas tué, on travaille
                        while ((thread?.IsAlive ?? false) && fonctionne && (tcpClient?.Connected ?? false))
                        {
                            while (stream.DataAvailable)
                            {
                                //lectureBlock = ReadDéChiffrer256();
                                fluxEntrant = ReadMemStreamDéchiffrer256();
                                if (fluxEntrant != null)
                                {
                                    for (int cmd = fluxEntrant.ReadByte(); 0 < cmd && cmd <= 255; cmd = fluxEntrant.ReadByte())
                                    {
                                        if (!fonctionne ||
                                            méthodesRéseau[cmd] == null ||
                                            false.Equals(méthodesRéseau[cmd].Invoke(this, fluxEntrant.DécodeCommande(this, méthodesRéseau[cmd].GetParameters()))))
                                        {
                                            Close();
                                            fonctionne = false;
                                        }
                                    }
                                }
                            }

                            WriteQueue();

                            // Attente de 10 ms
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
                }
                //else; //Déco
            }
            catch (Exception ex)
            {
                //Log...
            }
            catch
            {
                //Attraper les tous !
            }
            Close();
        }*/
    }
}
