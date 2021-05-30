using System;
using System.Collections.Generic;
using System.Drawing;
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
using ModuleBOARD.Elements.Lots;
using ModuleBOARD.Réseau;

namespace Board
{
    public class ClientThreadBoard : ClientThread
    {
        private delegate void dlgVoid();
        private delegate void dlgVoidLstInt(List<int> ids);
        private delegate void dlgVoidString(string str);
        private delegate void dlgVoidLstString(List<string> strs);
        private delegate void dlgVoidBoolString(bool ok, string str);
        private delegate void dlgVoidCharString(Char type, string str);
        private delegate void dlgVoidTypeMessString(OutilsRéseau.EMessage type, string str);
        //private delegate void dlgVoidLstElmLstElmRes(List<Element> elems, List<ElementRéseau> élémentRésiduel);
        private delegate void dlgVoidElm(Element elem);
        private delegate void dlgVoidGrp(Groupe grp);
        private delegate void dlgVoidLstElm(List<Element> elems);
        private delegate void dlgVoidImg(Image img);
        private delegate void dlgVoidMod(Model2_5D mld);
        private delegate void dlgVoidtabInt(int[] ids);
        private delegate void dlgVoidIntElmEEtat(int idElm, Element.EEtat etat);
        private delegate void dlgVoidInt(int idElm);
        private delegate void dlgVoidIntInt(int idElm, int delta);
        private delegate void dlgVoidUShortIntInt(ushort idJrt, int idElm, int delta);
        private delegate void dlgVoidUShortPointFIntInt(ushort idJrt, PointF pj, int idElm, int delta);
        private delegate void dlgVoidUShortPointFInt(ushort idJrt, PointF pj, int idElm);
        private delegate void dlgVoidUShortStringPointF(ushort idJrt, string nomJrt, PointF pJr);
        private delegate void dlgVoidUShortPointF(ushort idJrt, PointF pJr);
        private delegate void dlgVoidStringUShortPointF(string nomSession, ushort idJrt, PointF pJr);
        private delegate void dlgVoidGrpJrs(Groupe grp, Dictionary<ushort, Joueur> jrs);
        private delegate void dlgVoidUShortPointFIntIntInt(ushort idJrt, PointF pj, int idSrc, int index, int idElm);

        public ushort IdJoueurSession { get; private set; } = 0;
        public string NomSession { get; private set; } = null;
        public bool EstDansSession { get => EstIdentifié && NomSession != null; }
        private BigInteger SessionHashPwd = 0;
        private Board board;
        private IAsyncResult boardAsyncResult = null;

        private uint VersionDuPartage;

        /*
            FinPaquet = 0, //Vide
            MessageServeur = 1, //bool ok, Nom de la session
            AjouterSession = 2, //Nom de la session
            SynchroSession = 3
        */
        static private MethodInfo[] InitBoardMethods()
        {
            ClientThreadBoard _ct = new ClientThreadBoard();
            return new MethodInfo[]
            {
                null,
                null,//déco
                GetMethodInfo<OutilsRéseau.EMessage, string>(_ct.MessageServeur),
                GetMethodInfo<List<string>>(_ct.ActualiserSessions),
                GetMethodInfo<string, ushort, PointF>(_ct.RejointSession),
                GetMethodInfo<uint, List<IBinSerialisable>>(_ct.SynchroSession),
                GetMethodInfo<int[]>(_ct.RéidentifierElément),
                GetMethodInfo<uint, ushort, string, PointF>(_ct.ArrivéeJoueur),
                GetMethodInfo<uint, ushort, PointF>(_ct.SortieJoueur),
                null,
                null,
                null,
                null,
                null,
                null,
                GetMethodInfo<List<int>>(_ct.DemandeElement),
                GetMethodInfo<List<Element>>(_ct.RéceptionElement),
                GetMethodInfo<List<string>>(_ct.DemandeImage),
                GetMethodInfo<List<string>>(_ct.DemandeModel),
                GetMethodInfo(_ct.RéceptionImage),
                GetMethodInfo(_ct.RéceptionModel),
                GetMethodInfo<uint, List<Element>>(_ct.ChargerElement),
                GetMethodInfo<uint, int, Element.EEtat>(_ct.ChangerEtatElément),
                GetMethodInfo<uint, int, int>(_ct.RouletteElément),
                GetMethodInfo<uint, int, int>(_ct.TournerElément),
                GetMethodInfo<uint, ushort, PointF, int, int>(_ct.AttraperElement), // On reçoi un élément déjà en jeu
                GetMethodInfo<uint, ushort, PointF, int, int, int>(_ct.PiocherElement), // On pioche un nouvelle élément
                GetMethodInfo<uint, ushort, PointF, int>(_ct.LacherElement), // On lache tout les élément sur un autre

                GetMethodInfo<uint, int>(_ct.RangerVersParent),
                GetMethodInfo<uint, int, int>(_ct.Mélanger),
                GetMethodInfo<uint, int>(_ct.DéfausserElement),
                GetMethodInfo<uint, int>(_ct.ReMettreDansPioche),
                GetMethodInfo<uint, int, int>(_ct.MettreEnPioche),
                GetMethodInfo<uint, int, int>(_ct.CréerLaDéfausse),
                GetMethodInfo<uint, int, int>(_ct.MettreEnPaquet),

                GetMethodInfo<uint, int>(_ct.Supprimer),
                GetMethodInfo<uint>(_ct.SupprimerTout)
            };
        }
        static private readonly MethodInfo[] BoardMethods = InitBoardMethods();

        private ClientThreadBoard() { }

        public ClientThreadBoard(string _identifiant, BigInteger _pwhash, TcpClient _tcpClient, Board _board)
            :base(_tcpClient, _pwhash, null, _identifiant)
        {
            board = _board;
            méthodesRéseau = BoardMethods;
            bibImg = _board.bibliothèqueImage;
            bibMod = _board.bibliothèqueModel;
            VersionDuPartage = 0;
        }

        protected override bool Identifie()
        {
            BigInteger code = ReadUBigInteger(16);
            if (code < BigInteger.Zero) return false;
            Guid attendu = OutilsRéseau.GuidEncode(GVersion, code);
            if(!WriteGuid(attendu))return false;
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
            if (code == BigInteger.MinusOne) return false;
            byte[] idt_bts = Identifiant.stringToBytes();
            if (WriteChiffrer256(idt_bts, (OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31)< idt_bts.Length) return false;
            if(!EcrireMotDePasseHash(code)) return false;
            string id = ReadDéChiffrer256((OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31).bytesToString();
            if (id != null && id != "")
            {
                //NomSession = "";
                if (id != Identifiant)
                {
                    InvoquerBoard(OutilsRéseau.EMessage.IdentifiantRefusée, id);
                    Identifiant = id;
                }
                else InvoquerBoard(OutilsRéseau.EMessage.ConnexionRéussie, "");
                return true;
            }
            else
            {
                //NomSession = null;
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

        #region Méthodes appelées via le thread réseau
        private bool MessageServeur(OutilsRéseau.EMessage type, string message)
        {
            bool ok = true;
            switch (type)
            {
                case OutilsRéseau.EMessage.ConnexionRéussie:
                    //NomSession = "";
                    break;
                case OutilsRéseau.EMessage.IdentifiantRefusée:
                    //NomSession = "";
                    break;
                case OutilsRéseau.EMessage.Déconnexion:
                    NomSession = null;
                    ok = false;
                    break;
                case OutilsRéseau.EMessage.QuitSession:
                    if (NomSession == message)
                    {
                        NomSession = null;
                        break;
                    }
                    else return true;
            }
            Board brd = ObtenirBoard();
            if(brd != null) InvoquerBoard((dlgVoidTypeMessString)(brd.IVK_MessageServeur), type, message);
            return ok;
        }

        private bool ActualiserSessions(List<string> sessions)
        {
            GérerSessions js = ObtenirBoard()?.jSession;
            if (js != null)
            {
                InvoquerBoard((dlgVoidLstString)(js.ActualiserSessions), sessions);
            }
            return true;
        }

        private bool RejointSession(string session, ushort idJoueurSession, PointF p)
        {
            NomSession = session;
            IdJoueurSession = idJoueurSession;
            Board brd = ObtenirBoard();
            if (brd != null) InvoquerBoard((dlgVoidStringUShortPointF)(brd.IVK_JoinSession), NomSession, idJoueurSession, p);
            return true;
            //return MessageServeur(OutilsRéseau.EMessage.JoinSession, session);
        }

        private bool SynchroSession(uint versionDuPartage, List<IBinSerialisable> elems)
        {
            VersionDuPartage = versionDuPartage;
            while(FileOrdres.TryDequeue(out _)); // vide les demandes
            //Groupe grp = elems.LastOrDefault() as Groupe;
            Groupe grp = elems.LastOrDefault( e => e.ElmType == EType.Groupe ) as Groupe;
            Dictionary<ushort, Joueur> dicoJr = new Dictionary<ushort, Joueur>();
            elems.Where(e => e.ElmType == EType.Joueur).ToList().ForEach(e => dicoJr.Add((e as Joueur).IdSessionJoueur, (e as Joueur)));
            if (grp != null)
            {
                VérifierLesBibliothèques();
                Board brd = ObtenirBoard();
                if (brd != null)
                {
                    InvoquerBoard((dlgVoidGrpJrs)(brd.IVK_SynchroniserSession), grp, dicoJr);
                    //SyncroBoard();
                }
            }
            //dicoElement.Clear();
            return true;
        }

        private bool RéidentifierElément(int[] elems)
        {
            Board brd = ObtenirBoard();
            if (brd != null) InvoquerBoard((dlgVoidtabInt)(brd.IVK_RéidentifierElément), elems);
            return true;
        }


        private bool ArrivéeJoueur(uint versionDuPartage, ushort idJr, string nomJr, PointF pJr)
        {
            Board brd = ObtenirBoard();
            if (brd != null) InvoquerBoard((dlgVoidUShortStringPointF)(brd.IVK_ArrivéeJoueur), idJr, nomJr, pJr);
            GérerVersionDuPartage(versionDuPartage);
            return true;
        }

        private bool SortieJoueur(uint versionDuPartage, ushort idJr, PointF pJr)
        {
            Board brd = ObtenirBoard();
            if (brd != null) InvoquerBoard((dlgVoidUShortPointF)(brd.IVK_SortieJoueur), idJr, pJr);
            GérerVersionDuPartage(versionDuPartage);
            return true;
        }

        private bool DemandeElement(List<int> idElms)
        {
            Board brd = ObtenirBoard();
            if (brd != null) return InvoquerBoard((dlgVoidLstInt)(brd.IVK_DemandeElement), idElms);
            else return false;
        }

        private bool RéceptionElement(List<Element> element)
        {
            Board brd = ObtenirBoard();
            if (brd != null) return InvoquerBoard((dlgVoidLstElm)(brd.IVK_RéceptionElement), element);
            else return false;
        }

        private bool DemandeImage(List<string> idImgs)
        {
            Board brd = ObtenirBoard();
            if (brd != null) return InvoquerBoard((dlgVoidLstString)(brd.IVK_DemandeImage), idImgs);
            else return false;
        }

        private bool DemandeModel(List<string> idMods)
        {
            Board brd = ObtenirBoard();
            if (brd != null) return InvoquerBoard((dlgVoidLstString)(brd.IVK_DemandeModel), idMods);
            else return false;
        }

        private bool RéceptionImage()
        {
            Image img = BibliothèqueImage.ChargerImage(ref fluxEntrant);
            Board brd = ObtenirBoard();
            if (brd != null) return InvoquerBoard((dlgVoidImg)(brd.IVK_RéceptionImage), img);
            else return false;
        }

        private bool RéceptionModel()
        {
            Model2_5D mld = BibliothèqueModel.ChargerModel(fluxEntrant, bibImg);
            VérifierBibliothèqueImages();
            Board brd = ObtenirBoard();
            if (brd != null) return InvoquerBoard((dlgVoidMod)(brd.IVK_RéceptionModel), mld);
            else return false;
        }

        private bool GérerVersionDuPartage(uint version)
        {
            if (version != 0)
            {
                if (version != VersionDuPartage)
                {
                    Journaliser("Désynchro : Client=" + VersionDuPartage + " Serveur=" + version);
                    VersionDuPartage = 0;
                    WriteCommande(ServeurCodeCommande.DemandeSynchro);
                    return false;
                }
                else
                { 
                    if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                    return true;
                }
            }
            return false;
        }

        private bool ChargerElement(uint versionDuPartage, List<Element> elements)
        {
            if (elements != null && elements.Any())
            {
                Board brd = ObtenirBoard();
                if (brd != null && NomSession != null)
                {
                    InvoquerBoard((dlgVoidElm)(brd.IVK_ChargerElement), elements.Last());
                    //SyncroBoard();
                    VérifierLesBibliothèques();
                }
            }
            GérerVersionDuPartage(versionDuPartage);
            return true;
        }

        private bool ChangerEtatElément(uint versionDuPartage, int idElm, Element.EEtat etat)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidIntElmEEtat)(brd.IVK_ChangerEtatElément), idElm, etat);
            }
            GérerVersionDuPartage(versionDuPartage);
            return true;
        }

        private bool RouletteElément(uint versionDuPartage, int idElm, int delta)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidIntInt)(brd.IVK_RouletteElément), idElm, delta);
            }
            GérerVersionDuPartage(versionDuPartage);
            return true;
        }

        private bool TournerElément(uint versionDuPartage, int idElm, int delta)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidIntInt)(brd.IVK_TournerElément), idElm, delta);
            }
            GérerVersionDuPartage(versionDuPartage);
            return true;
        }











        private bool AttraperElement(uint versionDuPartage, ushort idJr, PointF pj, int idElmPSrc, int idElmAtrp) // On reçoi un élément déjà en jeu
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                //Journaliser("Attrape Version partage = " + versionDuPartage + " attendu " + VersionDuPartage);
                InvoquerBoard((dlgVoidUShortPointFIntInt)(brd.IVK_AttraperElement), idJr, pj, idElmPSrc, idElmAtrp);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool PiocherElement(uint versionDuPartage, ushort idJr, PointF pj, int idElmPSrc, int index,  int idElm) // On pioche un nouvelle élément
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                //Journaliser("Piocher Version partage = " + versionDuPartage + " attendu " + VersionDuPartage);
                InvoquerBoard((dlgVoidUShortPointFIntIntInt)(brd.IVK_PiocherElement), idJr, pj, idElmPSrc, index, idElm);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool LacherElement(uint versionDuPartage, ushort idJr, PointF pj, int idElmRecep) // On lache tout les éléments sur un autre
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                //Journaliser("Lacher Version partage = " + versionDuPartage + " attendu " + VersionDuPartage);
                InvoquerBoard((dlgVoidUShortPointFInt)(brd.IVK_LacherElement), idJr, pj, idElmRecep);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool RangerVersParent(uint versionDuPartage, int idElm)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidInt)(brd.IVK_RangerVersParent), idElm);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool Mélanger(uint versionDuPartage, int idElm, int seed)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidIntInt)(brd.IVK_Mélanger), idElm, seed);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool DéfausserElement(uint versionDuPartage, int idElm)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidInt)(brd.IVK_DéfausserElement), idElm);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool ReMettreDansPioche(uint versionDuPartage, int idElm)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidInt)(brd.IVK_ReMettreDansPioche), idElm);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool MettreEnPioche(uint versionDuPartage, int idElm, int idPioch)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidIntInt)(brd.IVK_MettreEnPioche), idElm, idPioch);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool CréerLaDéfausse(uint versionDuPartage, int idElm, int idDefss)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidIntInt)(brd.IVK_CréerLaDéfausse), idElm, idDefss);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool MettreEnPaquet(uint versionDuPartage, int idElm, int idPaq)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidIntInt)(brd.IVK_MettreEnPaquet), idElm, idPaq);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool Supprimer(uint versionDuPartage, int idElm)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoidInt)(brd.IVK_Supprimer), idElm);
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }

        private bool SupprimerTout(uint versionDuPartage)
        {
            Board brd = ObtenirBoard();
            if (brd != null && NomSession != null)
            {
                InvoquerBoard((dlgVoid)(brd.IVK_SupprimerTout));
                GérerVersionDuPartage(versionDuPartage);
            }
            return true;
        }
        #endregion

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
            try
            {
                if (brd != null)
                {
                    if (brd.InvokeRequired)
                    {
                        SyncroBoard();
                        try
                        {
                            boardAsyncResult = brd.BeginInvoke((dlgVoidTypeMessString)(board.IVK_MessageServeur), type, message);
                        }
                        catch
                        {
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        board.IVK_MessageServeur(type, message);
                        return true;
                    }
                }
                else return false;
            }
            catch
            {
                return false;
            }
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
            if (EstEnFile(ServeurCodeCommande.ActualiserSessions) == false)
                EnqueueCommande(ServeurCodeCommande.ActualiserSessions);
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

        public bool ChangerEtat(Element elm, Element.EEtat etat)
        {
            if (NomSession != null && elm != null && elm.IdentifiantRéseau > 0)
            {
                EnqueueCommande(ServeurCodeCommande.ChangerEtatElément, elm.IdentifiantRéseau, etat);
                return true;
            }
            else return false;
        }
    }
}
