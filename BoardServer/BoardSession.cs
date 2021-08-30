using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots;
using ModuleBOARD.Elements.Lots.Dés;
using ModuleBOARD.Elements.Lots.Piles;
using ModuleBOARD.Elements.Pieces;
using ModuleBOARD.Réseau;
using NAudioDemo.NetworkChatDemo;
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
        static public int NbSessionMax = 20;
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
        private string audioChatCodec;
        private Joueur.EDroits droitsSession;

        private ushort GIdJoueur = 1;
        private int GIdElémentRéseau = 1;//Générateur d'identifiant réseau
        private Random rnd = new Random();

        private Groupe root;

        public BoardSession(string nomSession, ClientThreadServer _maître, BigInteger _hashMotDePasseCréateur, BigInteger _hashMotDePasseSession, bool _demanderMaître, string chatCodec, Joueur.EDroits _droits)
        {
            maître = _maître;
            NomSession = nomSession;
            hashMotDePasseCréateur = _hashMotDePasseCréateur;
            hashMotDePasseSession = _hashMotDePasseSession;
            demanderMaître = _demanderMaître;
            if (String.IsNullOrWhiteSpace(chatCodec))
                audioChatCodec = null;
            else audioChatCodec = chatCodec;
            droitsSession = _droits;
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

        private bool VérifierDroits(ClientThreadServer ct, Joueur.EDroits droits)
        {
            if (ct == maître) return true;
            else
            lock (ct)
            {
                return (ct.Joueur != null && ct.Joueur.ADroits(droits, droitsSession));
            }
        }

        private bool VérifierClientEtDroits(ClientThreadServer ct, Joueur.EDroits droits)
        {
            if (clientThreadServers.Contains(ct))
            {
                if (ct == maître) return true;
                else
                lock (ct)
                {
                    return (ct.Joueur != null && ct.Joueur.ADroits(droits, droitsSession));
                }
            }
            else return false;
        }

        private Joueur.EDroits ObtenirDroitsJoueur(ClientThreadServer ct)
        {
            lock (ct)
            {
                if (ct.Joueur != null)
                {
                    return ct.Joueur.Droits;
                }
                else return Joueur.EDroits.Néant;
            }
        }

        private uint GénérerVersionPartage()
        {
            uint res = VersionDuPartage;
            if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
            return res;
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
                    lock (ct)
                    {
                        ISet<int> setIdRéseau = new SortedSet<int>();
                        return ct.WriteCommande(BoardCodeCommande.SynchroSession, setIdRéseau, ref GIdElémentRéseau,
                            VersionDuPartage,
                            ct.Joueur?.IdSessionJoueur ?? 0,
                            droitsSession,
                            root,
                            clientThreadServers.Select(c => c.Joueur).ToList());
                    }
                }
                else return false;
            }
        }

        public bool SynchroniserTous(ClientThreadServer ct)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    ISet<int> setIdRéseau = new SortedSet<int>();
                    byte[] commnd = ConstruireCommande(BoardCodeCommande.SynchroSession, setIdRéseau, ref GIdElémentRéseau,
                        VersionDuPartage,
                        ct.Joueur?.IdSessionJoueur ?? 0,
                        droitsSession,
                        root,
                        clientThreadServers.Select(c => c.Joueur).ToList());
                    clientThreadServers.ForEach(j => j.EnqueueCommande(commnd));
                    return true;
                }
                else return false;
            }
        }

        public bool MessageSession(ClientThreadServer ct, string message)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    lock (ct)
                    {
                        byte[] commnd = ConstruireCommande(BoardCodeCommande.MessageSession, message);
                        clientThreadServers.ForEach(j => { if (j != ct) j.EnqueueCommande(commnd); });
                    }
                    return true;
                }
                else return false;
            }
        }

        public bool MajDroitsSession(ClientThreadServer ct, Joueur.EDroits droits)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    lock (ct)
                    {
                        if (ct == maître || (ct.Joueur != null && ct.Joueur.Droits.HasFlag(Joueur.EDroits.GestDroits)))
                        {
                            droitsSession = droits;
                            byte[] commnd = ConstruireCommande(BoardCodeCommande.MajDroitsSession, GénérerVersionPartage()/*VersionDuPartage*/, NomSession, droits);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commnd); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                    return true;
                }
                else return false;
            }
        }

        public bool MajDroitsJoueur(ClientThreadServer ct, ushort idJr, Joueur.EDroits droits)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct) && idJr > 0)
                {
                    Joueur jr = TrouverJoueur(idJr);
                    if (jr != null)
                    {
                        lock (ct)
                        {
                            if (ct == maître || (ct.Joueur != null && !ct.Joueur.Droits.HasFlag(Joueur.EDroits.Bloqué) && ct.Joueur.Droits.HasFlag(Joueur.EDroits.GestDroits)))
                            {
                                jr.Droits = droits;
                                byte[] commnd = ConstruireCommande(BoardCodeCommande.MajDroitsJoueur, GénérerVersionPartage()/*VersionDuPartage*/, idJr, droits);
                                clientThreadServers.ForEach(j => { j.EnqueueCommande(commnd); });
                                //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                            }
                        }
                    }
                    return true;
                }
                else return false;
            }
        }

        public bool PasseMainJoueur(ClientThreadServer ct, ushort idJr)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct) && idJr > 0)
                {
                    Joueur jr = TrouverJoueur(idJr);
                    if (jr != null)
                    {
                        lock (ct)
                        {
                            if (ct.Joueur != null && !ct.Joueur.Droits.HasFlag(Joueur.EDroits.Bloqué))
                            {
                                Joueur.EDroits ctdr = ct.Joueur.Droits;
                                Joueur.EDroits droitMain = ctdr & Joueur.EDroits.Main;
                                if (droitMain != Joueur.EDroits.Néant && (droitsSession | ctdr).HasFlag(Joueur.EDroits.PasseMain))
                                {
                                    Joueur.EDroits jrdrts = jr.Droits;
                                    jrdrts |= droitMain;
                                    ctdr &= ~droitMain;

                                    ct.Joueur.Droits = ctdr;
                                    byte[] commnd = ConstruireCommande(BoardCodeCommande.MajDroitsJoueur, GénérerVersionPartage()/*VersionDuPartage*/, ct.Joueur.IdSessionJoueur, ctdr);
                                    clientThreadServers.ForEach(j => { j.EnqueueCommande(commnd); });
                                    //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;

                                    jr.Droits = jrdrts;
                                    commnd = ConstruireCommande(BoardCodeCommande.MajDroitsJoueur, GénérerVersionPartage()/*VersionDuPartage */, idJr, jrdrts);
                                    clientThreadServers.ForEach(j => { j.EnqueueCommande(commnd); });
                                    //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                                }
                            }
                        }
                    }
                    return true;
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

        public bool NouvelleVersion(ClientThreadServer ct, Image img, byte qualité, ushort coin, uint alpahCoul)
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
                            BibliothèqueImage.SauvegarderImage((img, img.RawFormat.Guid, qualité, coin, alpahCoul), strm);
                            bts = strm.ToArray();
                        }
                        clientThreadServers.ForEach(j => { if (j != ct) j.EnqueueCommande(bts); });
                    }
                    if (bibliothèqueImage != null)
                    {
                        if (bibliothèqueImage.NouvelleVersion(img, img.RawFormat.Guid, qualité, coin, alpahCoul))
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
                if (elms != null && elms.Any() && VérifierClientEtDroits(ct, Joueur.EDroits.Importation))
                {
                    Element lstElm = elms.Last();
                    List<int> renumérotation = new List<int>();
                    foreach(Element e in elms)
                    {
                        if(e.IdentifiantRéseau <= 0)
                        {
                            int ancId = e.IdentifiantRéseau;
                            e.IdentifiantRéseau = int.MaxValue;
                            if (e.IdentifiantRéseau == int.MaxValue)
                            {
                                renumérotation.Add(ancId);
                                e.IdentifiantRéseau = GIdElémentRéseau.IdRéseauSuivant();
                                renumérotation.Add(e.IdentifiantRéseau);
                            }
                        }
                    }
                    if(renumérotation.Any()) ct.EnqueueCommande(BoardCodeCommande.RéidentifierElément, renumérotation.ToArray());
                    uint vpart = GénérerVersionPartage();
                    ct.EnqueueCommande(BoardCodeCommande.ChargerElement, vpart);
                    byte[] commande = ConstruireCommande(BoardCodeCommande.ChargerElement, ref GIdElémentRéseau, vpart, elms);
                    clientThreadServers.ForEach(j => { if (j != ct) j.EnqueueCommande(commande); });
                    root.Fusionner(lstElm);
                    //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                }
            }
        }

        public bool ChangerEtatElément(ClientThreadServer ct, int idElm, Element.EEtat etat)
        {
            lock (this)
            {
                if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler))
                {
                    Element elm = TrouverElementRéseau(idElm);
                    if (elm != null)
                    {
                        elm.MajEtat(etat);
                        byte[] commande = ConstruireCommande(BoardCodeCommande.ChangerEtatElément, GénérerVersionPartage()/*VersionDuPartage*/, idElm, etat);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        return true;
                    }
                    else return false;
                }
            }
            return true;
        }

        public bool ChangerAngle(ClientThreadServer ct, int idElm, float ang)
        {
            lock (this)
            {
                if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler))
                {
                    Element elm = TrouverElementRéseau(idElm);
                    if (elm != null)
                    {
                        elm.GC.A = ang;
                        byte[] commande = ConstruireCommande(BoardCodeCommande.ChangerAngle, GénérerVersionPartage()/*VersionDuPartage*/, idElm, ang);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
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
                if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler))
                {
                    Element elm = TrouverElementRéseau(idElm);
                    if (elm != null)
                    {
                        elm.Roulette(delta);
                        byte[] commande = ConstruireCommande(BoardCodeCommande.RouletteElément, GénérerVersionPartage()/*VersionDuPartage*/, idElm, delta);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
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
                if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler))
                {
                    Element elm = TrouverElementRéseau(idElm);
                    if (elm != null)
                    {
                        elm.Tourner(delta);
                        byte[] commande = ConstruireCommande(BoardCodeCommande.TournerElément, GénérerVersionPartage()/*VersionDuPartage*/, idElm, delta);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        return true;
                    }
                    else return false;
                }
            }
            return true;
        }



        public void AttraperElement(ClientThreadServer ct, PointF pj, float angle, int idElmPSrc, int idElmAtrp) //J'attrape l'élément idElmAtrp depuis idElmPSrc
        {
            lock (this)
            {
                if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler) && idElmPSrc > 0 && idElmAtrp > 0)
                {
                    Element elmPSrc = TrouverElementRéseau(idElmPSrc);
                    if (elmPSrc == null) elmPSrc = root;
                     Element elmAtrp = elmPSrc.MousePickAt(idElmAtrp);
                    if (elmAtrp != null)
                    {
                        elmAtrp = elmPSrc.DétacherElement(elmAtrp);
                        if (elmAtrp != null)
                        {
                            if (elmAtrp.ElmType != EType.Figurine /*&& elmAtrp.GC.A != angle*/)
                            {
                                /* float difAng = angle - elmAtrp.GC.A;
                                 double cosa = Math.Cos((difAng * Math.PI) / 180.0);
                                 double sina = Math.Sin((difAng * Math.PI) / 180.0);
                                 PointF dltp = new PointF(pj.X - elmAtrp.GC.P.X, pj.Y - elmAtrp.GC.P.Y);
                                 PointF rdltp = new PointF(
                                         (float)(dltp.X * cosa - dltp.Y * sina),
                                         (float)(dltp.X * sina + dltp.Y * cosa)
                                     );
                                 elmAtrp.GC.P.X += dltp.X - rdltp.X;
                                 elmAtrp.GC.P.Y += dltp.Y - rdltp.Y;
                                 elmAtrp.GC.A = angle;*/
                                elmAtrp.GC.ChangerAngleSuivantPoint(angle, pj);
                            }
                            ct.Joueur.P = pj;
                            ct.Joueur.DonnerElément(elmAtrp);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.AttraperElement, GénérerVersionPartage()/*VersionDuPartage*/, ct.Joueur.IdSessionJoueur, pj, angle, idElmPSrc, idElmAtrp);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void PiocherElement(ClientThreadServer ct, PointF pj, float angle, int idElmPSrc, int index)
        {
            lock (this)
            {
                if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler) && idElmPSrc > 0)
                {
                    Element elmPSrc = TrouverElementRéseau(idElmPSrc);
                    if (elmPSrc != null)
                    {
                        Element elmAtrp = elmPSrc.MousePioche(index);
                        if (elmAtrp != null)
                        {
                            elmAtrp.IdentifiantRéseau = GIdElémentRéseau.IdRéseauSuivant();
                            elmAtrp.GC.ProjectionInv(elmPSrc.GC);
                            if (elmAtrp.ElmType != EType.Figurine /*&& elmAtrp.GC.A != angle*/)
                            {
                                /*float difAng = angle - elmAtrp.GC.A;
                                double cosa = Math.Cos((difAng * Math.PI) / 180.0);
                                double sina = Math.Sin((difAng * Math.PI) / 180.0);
                                PointF dltp = new PointF(pj.X - elmAtrp.GC.P.X, pj.Y - elmAtrp.GC.P.Y);
                                PointF rdltp = new PointF(
                                        (float)(dltp.X * cosa - dltp.Y * sina),
                                        (float)(dltp.X * sina + dltp.Y * cosa)
                                    );
                                elmAtrp.GC.P.X += dltp.X - rdltp.X;
                                elmAtrp.GC.P.Y += dltp.Y - rdltp.Y;
                                elmAtrp.GC.A = angle;*/
                                elmAtrp.GC.ChangerAngleSuivantPoint(angle, pj);
                            }
                            ct.Joueur.P = pj;
                            ct.Joueur.DonnerElément(elmAtrp);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.PiocherElement, GénérerVersionPartage()/*VersionDuPartage*/, ct.Joueur.IdSessionJoueur, pj, angle, idElmPSrc, index, elmAtrp.IdentifiantRéseau);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void LacherElément(ClientThreadServer ct, PointF pj, int idElmRecep)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct) && ct.Joueur != null)
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
                            if (elmLach is Pile && !(elmLach is Défausse)) root.Suppression(elmLach);
                            else root.ElementLaché(elmLach);
                            //if (elmLach != null) ct.Joueur.DonnerElément(elmLach);
                        });
                        byte[] commande = ConstruireCommande(BoardCodeCommande.LacherElement, GénérerVersionPartage()/*VersionDuPartage*/, ct.Joueur.IdSessionJoueur, pj, idElmRecep);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                    }
                }
            }
        }

        public void TournerElémentAttrapé(ClientThreadServer ct, PointF pj, float delta)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct) && ct.Joueur != null)
                {
                    ct.Joueur.P = pj;
                    ct.Joueur.TournerElémentAttrapé(delta);
                    byte[] commande = ConstruireCommande(BoardCodeCommande.TournerElémentAttrapé, GénérerVersionPartage()/*VersionDuPartage*/, ct.Joueur.IdSessionJoueur, pj, delta);
                    clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                }
            }
        }

        public void RangerVersParent(ClientThreadServer ct, int idElm)
        {
            if (idElm > 0)
            {
                lock (this)
                {
                    if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler | Joueur.EDroits.ActionSpéciale))
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
                            byte[] commande = ConstruireCommande(BoardCodeCommande.RangerVersParent, GénérerVersionPartage()/*VersionDuPartage*/, idElm);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
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
                    if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler | Joueur.EDroits.ActionSpéciale))
                    {
                        Element elm = TrouverElementRéseau(idElm);
                        if (elm != null)
                        {
                            int seed = rnd.Next();
                            if (elm is Pile) (elm as Pile).Mélanger(new Random(seed));
                            else if (elm is Dés) (elm as Dés).Mélanger(new Random(seed));
                            byte[] commande = ConstruireCommande(BoardCodeCommande.Mélanger, GénérerVersionPartage()/*VersionDuPartage*/, idElm, seed);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
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
                    if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler | Joueur.EDroits.ActionSpéciale))
                    {
                        Element elm = TrouverElementRéseau(idElm);
                        if (elm != null)
                        {
                            elm = root.DéfausserElement(elm);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.DéfausserElement, GénérerVersionPartage()/*VersionDuPartage*/, idElm);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
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
                    if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler | Joueur.EDroits.ActionSpéciale))
                    {
                        Défausse deffss = TrouverElementRéseau(idElm) as Défausse;
                        if (deffss != null)
                        {
                            deffss.ReMettreDansLaPioche();
                            byte[] commande = ConstruireCommande(BoardCodeCommande.ReMettreDansPioche, GénérerVersionPartage()/*VersionDuPartage*/, idElm);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
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
                    if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler | Joueur.EDroits.ActionSpéciale))
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
                            byte[] commande = ConstruireCommande(BoardCodeCommande.MettreEnPioche, GénérerVersionPartage()/*VersionDuPartage*/, idElm, elm.IdentifiantRéseau);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
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
                    if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler | Joueur.EDroits.ActionSpéciale))
                    {
                        Pioche pioch = TrouverElementRéseau(idElm) as Pioche;
                        if (pioch != null)
                        {
                            Défausse deff = new Défausse(pioch);
                            deff.IdentifiantRéseau = GIdElémentRéseau.IdRéseauSuivant();
                            root.ElementLaché(deff);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.CréerLaDéfausse, GénérerVersionPartage()/*VersionDuPartage*/, idElm, deff.IdentifiantRéseau);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
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
                    if (VérifierClientEtDroits(ct, Joueur.EDroits.Manipuler | Joueur.EDroits.ActionSpéciale))
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
                                byte[] commande = ConstruireCommande(BoardCodeCommande.MettreEnPaquet, GénérerVersionPartage()/*VersionDuPartage*/, idElm, elm.IdentifiantRéseau);
                                clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                                //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
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
                    if (VérifierClientEtDroits(ct, Joueur.EDroits.Supprimer))
                    {
                        Element elm = TrouverElementRéseau(idElm);
                        if (elm != null)
                        {
                            elm = root.Suppression(elm);
                            byte[] commande = ConstruireCommande(BoardCodeCommande.Supprimer, GénérerVersionPartage()/*VersionDuPartage*/, idElm);
                            clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                            //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                        }
                    }
                }
            }
        }

        public void SupprimerTout(ClientThreadServer ct)
        {
            lock (this)
            {
                if (VérifierClientEtDroits(ct, Joueur.EDroits.SupprimerTout))
                {
                    Nétoyer();
                    byte[] commande = ConstruireCommande(BoardCodeCommande.SupprimerTout, GénérerVersionPartage()/*VersionDuPartage*/);
                    clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                    //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                }
            }
        }

        public void ChatAudio(ClientThreadServer ct, byte[] cmdAudio)
        {
            lock (this)
            {
                if (clientThreadServers.Contains(ct))
                {
                    clientThreadServers.ForEach(j => { if(j != ct) j.EnqueueCommande(cmdAudio); });
                    //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;
                }
            }
        }
        #endregion

        private ushort NuméroJoueurSuivant()
        {
            ushort res;
            do
            {
                res = GIdJoueur;
                if (GIdJoueur < ushort.MaxValue) ++GIdJoueur; else GIdJoueur = 1;
            } while (clientThreadServers.Any(c => c.Joueur !=null && c.Joueur.IdSessionJoueur == res));
            return res;
        }

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
                        Joueur jr;
                        ct.sessionEnCours = this;
                        if (clientThreadServers.Contains(ct))
                        {
                            lock (ct)
                            {
                                if (ct.Joueur == null)
                                {
                                    jr = new Joueur(NuméroJoueurSuivant(), ct.ObtenirIdentifiant());
                                    ct.Joueur = jr;
                                }
                                else jr = ct.Joueur;
                            }
                        }
                        else
                        {
                            clientThreadServers.Add(ct);
                            lock (ct)
                            {
                                jr = new Joueur(NuméroJoueurSuivant(), ct.ObtenirIdentifiant());
                                ct.Joueur = jr;
                            }
                            if (GIdJoueur < ushort.MaxValue) ++GIdJoueur; else GIdJoueur = 1;
                        }
                        if (ct == maître) jr.Droits = Joueur.EDroits.Maître;
                        lock (ct) { ct.WriteCommande(BoardCodeCommande.RejointSession, NomSession, jr.IdSessionJoueur, (audioChatCodec ?? "")); }

                        byte[] commnd = ConstruireCommande(BoardCodeCommande.ArrivéeJoueur, new SortedSet<int>(), ref GIdElémentRéseau,
                            GénérerVersionPartage()/*VersionDuPartage*/,
                            jr);
                        clientThreadServers.ForEach(j => { if(j != ct)j.EnqueueCommande(commnd); });
                        //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;

                        Synchroniser(ct);
                    }
                    else ct.WriteCommande(BoardCodeCommande.MessageServeur, OutilsRéseau.EMessage.Erreur, "L'accès à la session \"" + NomSession + "\" vous est refusé en raison d'un mot de passe invalide.");
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
                        byte[] commande = ConstruireCommande(BoardCodeCommande.SortieJoueur, GénérerVersionPartage()/*VersionDuPartage*/, ct.Joueur.IdSessionJoueur, jr.P);
                        clientThreadServers.ForEach(j => { j.EnqueueCommande(commande); });
                        //if (VersionDuPartage < int.MaxValue) ++VersionDuPartage; else VersionDuPartage = 1;

                        List<Element> lstElemt = jr.ToutRécupérer();
                        if (lstElemt != null) lstElemt.ForEach(e => root.ElementLaché(e));
                    }
                }
            }
            if(estDansSession) ct.QuitterSession(this);
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
