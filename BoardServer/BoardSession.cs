using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots;
using ModuleBOARD.Elements.Lots.Piles;
using ModuleBOARD.Elements.Pieces;
using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ModuleBOARD.Réseau.ClientThread;

namespace BoardServer
{
    public class BoardSession
    {
        static public Dictionary<string, BoardSession> LstBoardSessions = new Dictionary<string, BoardSession>();

        public string NomSession { get; private set; }

        private uint VersionDuPartage = 1; //Identifiant de l'état courrant du board

        public BibliothèqueImage bibliothèqueImage = new BibliothèqueImage();
        public BibliothèqueModel bibliothèqueModel = new BibliothèqueModel();

        //private Thread thread
        private ClientThreadServer maître = null;
        private List<ClientThreadServer> clientThreadServers = new List<ClientThreadServer>();

        private BigInteger hashMotDePasseCréateur = 0;
        private BigInteger hashMotDePasseSession = 0;
        private bool demanderMaître; //Demander au créateur lors de la connex d'un joueur

        private ushort GIdJoueur = 1;
        private int GIdElémentRéseau = 1;//Générateur d'identifiant réseau

        private Groupe root;

        public BoardSession(string nomSession, ClientThreadServer _maître, BigInteger _hashMotDePasseCréateur, BigInteger _hashMotDePasseSession, bool _demanderMaître)
        {
            maître = _maître;
            NomSession = nomSession;
            hashMotDePasseCréateur = _hashMotDePasseCréateur;
            hashMotDePasseSession = _hashMotDePasseSession;
            demanderMaître = _demanderMaître;
            root = new Groupe();
            //root.IdentifiantRéseau = GIdRéseau.IdRéseauSuivant();
            lock (LstBoardSessions)
            {
                LstBoardSessions.Add(nomSession, this);
            }
            //thread = new Thread(new ThreadStart(fonctionnement));
            //thread.Start();
        }

        #region Intéraction avec le board serveur
        #region Outils
        private Element TrouverElementRéseau(int idRez)
        {
            if (idRez > 0)
            {
                Element elm = root.MousePickAt(idRez);
                if(elm == null)
                {
                    foreach(ClientThreadServer j in clientThreadServers)
                    {
                        elm = j.TrouverElementRéseau(idRez);
                        if (elm != null) break;
                    }
                }
                return elm;
            }
            else return null;
        }

        private object MajElement(Element elm)
        {
            if (elm != null && !(elm is ElementRéseau))
            {
                clientThreadServers.ForEach(j => j.MajElement(elm)); ;
                return root.MettreAJour(elm);
            }
            else return null;
        }

        private Joueur TrouverJoueur(ushort idJoueur)
        {
            foreach(ClientThreadServer ct in clientThreadServers)
            {
                Joueur jr = ct.Joueur;
                if(jr.IdSessionJoueur == idJoueur) return jr;
            }
            return null;
        }

        private void Nétoyer()
        {
            if (root != null) root.Nétoyer(); else root = new Groupe();
            bibliothèqueImage.Netoyer();
            bibliothèqueModel.Netoyer();
        }
        #endregion

        public bool Synchroniser(ClientThreadServer ct)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    ISet<int> setIdRéseau = new SortedSet<int>();
                    return ct.WriteCommande(BoardCodeCommande.SynchroSession, setIdRéseau, ref GIdElémentRéseau,
                        VersionDuPartage,
                        root,
                        clientThreadServers.Where(c => ct != c).Select(c => c.Joueur).ToList());
                }
                else return false;
            }
        }

        public bool DemandeElement(ClientThreadServer ct, List<int> idElms)
        {
            try
            {
                lock (this)
                {
                    if (idElms != null && idElms.Any() && clientThreadServers.Contains(ct))
                    {
                        if (root != null)
                        {
                            List<Element> lstElms = idElms.Select(id => TrouverElementRéseau(id)).Where(e => !(e is ElementRéseau)).ToList();
                            if (lstElms.Any()) ct.WriteCommande(BoardCodeCommande.RéceptionElement, ref GIdElémentRéseau, lstElms);
                        }
                        else return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool RéceptionElement(ClientThreadServer ct, List<Element> lstElms)
        {
            try
            {
                lock (this)
                {
                    if (lstElms != null && lstElms.Any() && clientThreadServers.Contains(ct))
                    {
                        if (root != null)
                        {
                            if (lstElms.Any())
                            {
                                List<int> renumérotation = new List<int>();
                                foreach (Element e in lstElms)
                                {
                                    if (e.IdentifiantRéseau <= 0)
                                    {
                                        renumérotation.Add(e.IdentifiantRéseau);
                                        e.IdentifiantRéseau = GIdElémentRéseau.IdRéseauSuivant();
                                        renumérotation.Add(e.IdentifiantRéseau);
                                    }
                                }
                                if(renumérotation.Any()) ct.EnqueueCommande(BoardCodeCommande.RéidentifierElément, renumérotation.ToArray());
                                
                                byte[] commande = ConstruireCommande(BoardCodeCommande.RéceptionElement, ref GIdElémentRéseau, lstElms);
                                clientThreadServers.ForEach(j => { if (j != ct) j.EnqueueCommande(commande); });

                                lstElms.ForEach(e => MajElement(e));
                            }
                        }
                        else return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool DemandeImage(ClientThreadServer ct, List<string> idImgs)
        {
            try
            {
                lock (this)
                {
                    if (idImgs != null && idImgs.Any() && clientThreadServers.Contains(ct))
                    {
                        if (bibliothèqueImage != null)
                        {
                            foreach (string sig in idImgs)
                            {
                                MemoryStream strm = new MemoryStream();
                                strm.WriteByte((byte)BoardCodeCommande.RéceptionImage);
                                if (bibliothèqueImage.RécupérerImage(sig, strm)) ct.EnqueueCommande(strm);
                            }
                        }
                        else return false;
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        public bool DemandeModel(ClientThreadServer ct, List<string> idMods)
        {
            lock (this)
            {
                if (idMods != null && idMods.Any() && clientThreadServers.Contains(ct))
                {
                    if (bibliothèqueModel != null)
                    {
                        foreach (string sig in idMods)
                        {
                            MemoryStream strm = new MemoryStream();
                            strm.WriteByte((byte)BoardCodeCommande.RéceptionModel);
                            if (bibliothèqueModel.RécupérerModel(sig, strm)) ct.EnqueueCommande(strm);
                        }
                    }
                }
            }
            return true;
        }

        public bool NouvelleVersion(ClientThreadServer ct, Image img)
        {
            lock (this)
            {
                if (img != null && clientThreadServers.Contains(ct))
                {
                    if (clientThreadServers.Count > 1)
                    {
                        byte[] bts;
                        using (MemoryStream strm = new MemoryStream())
                        {
                            strm.WriteByte((byte)BoardCodeCommande.RéceptionImage);
                            BibliothèqueImage.SauvegarderImage(img, strm);
                            bts = strm.ToArray();
                        }
                        clientThreadServers.ForEach(j => { if (j != ct) j.EnqueueCommande(bts); });
                    }
                    if (bibliothèqueImage != null)
                    {
                        if (bibliothèqueImage.NouvelleVersion(img))
                        {
                            root.MettreAJour(img);
                            if (bibliothèqueModel != null)
                                bibliothèqueModel.MettreAJour(img);
                        }
                        return true;
                    }
                    else return false;
                }
                else return false;
            }
        }

        public bool NouvelleVersion(ClientThreadServer ct, Model2_5D mld)
        {
            lock (this)
            {
                if (mld != null && clientThreadServers.Contains(ct))
                {
                    if (clientThreadServers.Count > 1)
                    {
                        byte[] bts;
                        using (MemoryStream strm = new MemoryStream())
                        {
                            strm.WriteByte((byte)BoardCodeCommande.RéceptionModel);
                            mld.Sérialiser(strm);
                            bts = strm.ToArray();
                        }
                        clientThreadServers.ForEach(j => { if (j != ct) j.EnqueueCommande(bts); });
                    }
                    if (bibliothèqueModel != null)
                        return bibliothèqueModel.NouvelleVersion(mld);
                    else return false;
                }
                else return false;
            }
        }

        public void ChargerElement(ClientThreadServer ct, List<Element> elms)
        {
            lock (this)
            {
                if (elms != null && elms.Any() && clientThreadServers.Contains(ct))
                {
                    Element lstElm = elms.Last();
                    List<int> renumérotation = new List<int>();
                    foreach(Element e in elms)
                    {
                        if(e.IdentifiantRéseau <= 0)
                        {
                            renumérotation.Add(e.IdentifiantRéseau);
                            e.IdentifiantRéseau = GIdElémentRéseau.IdRéseauSuivant();
                            renumérotation.Add(e.IdentifiantRéseau);
                        }
                    }
                    if(renumérotation.Any()) ct.EnqueueCommande(BoardCodeCommande.RéidentifierElément, renumérotation.ToArray());
                    ct.EnqueueCommande(BoardCodeCommande.ChargerElement, VersionDuPartage);
                    byte[] commande = ConstruireCommande(BoardCodeCommande.ChargerElement, ref GIdElémentRéseau, VersionDuPartage, elms);
                    clientThreadServers.ForEach(j => { if (j != ct) j.EnqueueCommande(commande); });
                    root.Fusionner(lstElm);
                    if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                }
            }
        }

        public bool ChangerEtatElément(ClientThreadServer ct, int idElm, Element.EEtat etat)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    Element elm = TrouverElementRéseau(idElm);
                    if (elm != null)
                    {
                        elm.MajEtat(etat);
                        byte[] commande = ConstruireCommande(BoardCodeCommande.ChangerEtatElément, VersionDuPartage, idElm, etat);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        return true;
                    }
                    else return false;
                }
            }
            return true;
        }

        public bool RouletteElément(ClientThreadServer ct, int idElm, int delta)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    Element elm = TrouverElementRéseau(idElm);
                    if (elm != null)
                    {
                        elm.Roulette(delta);
                        byte[] commande = ConstruireCommande(BoardCodeCommande.RouletteElément, VersionDuPartage, idElm, delta);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        return true;
                    }
                    else return false;
                }
            }
            return true;
        }

        public bool TournerElément(ClientThreadServer ct, int idElm, int delta)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    Element elm = TrouverElementRéseau(idElm);
                    if (elm != null)
                    {
                        elm.Tourner(delta);
                        byte[] commande = ConstruireCommande(BoardCodeCommande.TournerElément, VersionDuPartage, idElm, delta);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        return true;
                    }
                    else return false;
                }
            }
            return true;
        }



        public void AttraperElement(ClientThreadServer ct, PointF pj, int idElmPSrc, int idElmAtrp) //J'attrape l'élément idElmAtrp depuis idElmPSrc
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct) && idElmPSrc > 0 && idElmAtrp > 0)
                {
                    Element elmPSrc = TrouverElementRéseau(idElmPSrc);
                    if (elmPSrc == null) elmPSrc = root;
                     Element elmAtrp = elmPSrc.MousePickAt(idElmAtrp);
                    if (elmAtrp != null)
                    {
                        elmAtrp = elmPSrc.DétacherElement(elmAtrp);
                        if (elmAtrp != null)
                        {
                            ct.Joueur.P = pj;
                            ct.Joueur.DonnerElément(elmAtrp);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.AttraperElement, VersionDuPartage, ct.Joueur.IdSessionJoueur, pj, idElmPSrc, idElmAtrp);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void PiocherElement(ClientThreadServer ct, PointF pj, int idElmPSrc, int index)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct) && idElmPSrc > 0)
                {
                    Element elmPSrc = TrouverElementRéseau(idElmPSrc);
                    if (elmPSrc != null)
                    {
                        Element elmAtrp = elmPSrc.MousePioche(index);
                        if (elmAtrp != null)
                        {
                            elmAtrp.IdentifiantRéseau = GIdElémentRéseau.IdRéseauSuivant();
                            ct.Joueur.P = pj;
                            ct.Joueur.DonnerElément(elmAtrp);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.PiocherElement, VersionDuPartage, ct.Joueur.IdSessionJoueur, pj, idElmPSrc, index, elmAtrp.IdentifiantRéseau);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void LacherElément(ClientThreadServer ct, PointF pj, int idElmRecep)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    Element elmRecep;
                    if (idElmRecep > 0)
                    {
                        elmRecep = TrouverElementRéseau(idElmRecep);
                        if (elmRecep == null) elmRecep = root;
                    }
                    else elmRecep = root;

                    ct.Joueur.P = pj;
                    //Element elmLach = ct.Joueur.RécupérerElémentRéseau(idElmLach);
                    List<Element> lelmLach = ct.Joueur.ToutRécupérer();
                    if (lelmLach != null)
                    {
                        lelmLach.ForEach(elmLach =>
                        {
                            elmLach = elmRecep.ElementLaché(elmLach);
                            if (elmLach is Pile) root.Suppression(elmLach);
                            else root.ElementLaché(elmLach);
                            //if (elmLach != null) ct.Joueur.DonnerElément(elmLach);
                        });
                        byte[] commande = ConstruireCommande(BoardCodeCommande.LacherElement, VersionDuPartage, ct.Joueur.IdSessionJoueur, pj, idElmRecep);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                    }
                }
            }
        }

        public void RangerVersParent(ClientThreadServer ct, int idElm)
        {
            if (idElm > 0)
            {
                lock (this)
                {
                    if (clientThreadServers.Contains(ct))
                    {
                        Element parent = TrouverElementRéseau(idElm);
                        if (parent != null)
                        {
                            Element elm = root.RangerVersParent(parent);
                            if (elm != null)
                            {
                                elm = root.ElementLaché(elm);
                                if (elm != null) root.Suppression(elm);
                            }
                            byte[] commande = ConstruireCommande(BoardCodeCommande.RangerVersParent, VersionDuPartage, idElm);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void Mélanger(ClientThreadServer ct, int idElm)
        {
            if (idElm > 0)
            {
                lock (this)
                {
                    if (clientThreadServers.Contains(ct))
                    {
                        Element elm = TrouverElementRéseau(idElm);
                        if (elm != null)
                        {
                            Random rnd = new Random();
                            int seed = rnd.Next();
                            if (elm is Pile) (elm as Pile).Mélanger(new Random(seed));
                            byte[] commande = ConstruireCommande(BoardCodeCommande.Mélanger, VersionDuPartage, idElm, seed);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void DéfausserElement(ClientThreadServer ct, int idElm)
        {
            if (idElm > 0)
            {
                lock (this)
                {
                    if (clientThreadServers.Contains(ct))
                    {
                        Element elm = TrouverElementRéseau(idElm);
                        if (elm != null)
                        {
                            elm = root.DéfausserElement(elm);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.DéfausserElement, VersionDuPartage, idElm);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void ReMettreDansPioche(ClientThreadServer ct, int idElm)
        {
            if (idElm > 0)
            {
                lock (this)
                {
                    if (clientThreadServers.Contains(ct))
                    {
                        Défausse deffss = TrouverElementRéseau(idElm) as Défausse;
                        if (deffss != null)
                        {
                            deffss.ReMettreDansLaPioche();
                            byte[] commande = ConstruireCommande(BoardCodeCommande.ReMettreDansPioche, VersionDuPartage, idElm);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void MettreEnPioche(ClientThreadServer ct, int idElm)
        {
            if (idElm > 0)
            {
                lock (this)
                {
                    if (clientThreadServers.Contains(ct))
                    {
                        Element elm = TrouverElementRéseau(idElm);
                        if (elm != null)
                        {
                            if (elm is Element2D)
                            {
                                elm = root.DétacherElement(elm);
                                if (elm is Element2D2F)
                                {
                                    elm = new Pioche(elm as Element2D2F);
                                }
                                else if (elm is Element2D)
                                {
                                    elm = new Pioche(elm as Element2D);
                                }
                                elm.IdentifiantRéseau = GIdElémentRéseau.IdRéseauSuivant();
                                root.ElementLaché(elm);
                            }
                            byte[] commande = ConstruireCommande(BoardCodeCommande.MettreEnPioche, VersionDuPartage, idElm, elm.IdentifiantRéseau);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void CréerLaDéfausse(ClientThreadServer ct, int idElm)
        {
            if (idElm > 0)
            {
                lock (this)
                {
                    if (clientThreadServers.Contains(ct))
                    {
                        Pioche pioch = TrouverElementRéseau(idElm) as Pioche;
                        if (pioch != null)
                        {
                            Défausse deff = new Défausse(pioch);
                            deff.IdentifiantRéseau = GIdElémentRéseau.IdRéseauSuivant();
                            root.ElementLaché(deff);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.CréerLaDéfausse, VersionDuPartage, idElm, deff.IdentifiantRéseau);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void MettreEnPaquet(ClientThreadServer ct, int idElm)
        {
            if (idElm > 0)
            {
                lock (this)
                {
                    if (clientThreadServers.Contains(ct))
                    {
                        Element elm = TrouverElementRéseau(idElm);
                        if (elm != null)
                        {
                            elm = root.DétacherElement(elm);
                            if (elm != null)
                            {
                                elm = new Paquet(elm);
                                elm.IdentifiantRéseau = GIdElémentRéseau.IdRéseauSuivant();
                                root.ElementLaché(elm);
                                byte[] commande = ConstruireCommande(BoardCodeCommande.MettreEnPaquet, VersionDuPartage, idElm, elm.IdentifiantRéseau);
                                clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                                if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                            }
                        }
                    }
                }
            }
        }

        public void Supprimer(ClientThreadServer ct, int idElm)
        {
            if (idElm > 0)
            {
                lock (this)
                {
                    if (clientThreadServers.Contains(ct))
                    {
                        Element elm = TrouverElementRéseau(idElm);
                        if (elm != null)
                        {
                            elm = root.Suppression(elm);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.Supprimer, VersionDuPartage, idElm);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void SupprimerTout(ClientThreadServer ct)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    Nétoyer();
                    byte[] commande = ConstruireCommande(BoardCodeCommande.SupprimerTout, VersionDuPartage);
                    clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                    if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                }
            }
        }
        #endregion

        public bool JointLaSession(ClientThreadServer ct, BigInteger hash)
        {
            if (ct.sessionEnCours == null)
            {
                bool ok = false;
                lock (this)
                {
                    if (ct == maître) ok = true;
                    else
                    {
                        if (hash == hashMotDePasseCréateur)
                        {
                            maître = ct;
                            ok = true;
                        }
                        else ok = (hash == hashMotDePasseSession);
                    }
                    if (ok)
                    {
                        ct.sessionEnCours = this;
                        if (clientThreadServers.Contains(ct))
                        {
                            lock (ct)
                            {
                                if (ct.Joueur == null)
                                {
                                    ct.Joueur = new Joueur(GIdJoueur);
                                    if (GIdJoueur < ushort.MaxValue) ++GIdJoueur; else GIdJoueur = 1;
                                }
                            }
                        }
                        else
                        {
                            clientThreadServers.Add(ct);
                            lock (ct) { ct.Joueur = new Joueur(GIdJoueur); }
                            if (GIdJoueur < ushort.MaxValue) ++GIdJoueur; else GIdJoueur = 1;
                        }
                        ct.WriteCommande(BoardCodeCommande.RejointSession, NomSession, ct.Joueur.IdSessionJoueur, ct.Joueur.P);
                        Synchroniser(ct);
                    }
                    else ct.WriteCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.Erreur, "L'accès à la session \" + NomSession + \" vous est refusé en raison d'un mot de passe invalide.");
                }
                return ok;
            }
            else return false;
        }

        public void QuitteLaSession(ClientThreadServer ct)
        {
            bool estDansSession;
            lock (this)
            {
                estDansSession = clientThreadServers.Remove(ct);
                if(estDansSession)
                {
                    Joueur jr = ct.Joueur;
                    if (jr != null)
                    {
                        byte[] commande = ConstruireCommande(BoardCodeCommande.SortieJoueur, VersionDuPartage, ct.Joueur.IdSessionJoueur, jr.P);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;

                        List<Element> lstElemt = jr.ToutRécupérer();
                        if (lstElemt != null) lstElemt.ForEach(e => root.ElementLaché(e));
                    }
                }
            }
            if(estDansSession) ct.QuiterSession(this);
        }

        public bool Supprimer(ClientThreadServer demandeur)
        {
            lock (this)
            {
                if (demandeur == maître)
                {
                    Close();
                    return true;
                }
                else return false;
            }
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
                    //clientThreadServers.ForEach(c => QuitteLaSession(c));
                    byte[] commande = ConstruireCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.QuitSession, NomSession);
                    clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); lock (j) { j.sessionEnCours = null; } });
                    clientThreadServers.Clear();
                }
                clientThreadServers = null;
            }
            if (root != null)
            {
                lock (root)
                {
                    root.Nétoyer();
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
