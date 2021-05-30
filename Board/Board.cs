using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots;
using ModuleBOARD.Elements.Lots.Piles;
using ModuleBOARD.Elements.Pieces;
using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;


/* TODO
 * Faire un objet spéciale qui contient des éléments et appartient à un joueur style paravent ou main... main accessible en raccourci au bas de l'écran !
 * Lorqu'un élément est relaché sur 2, le glisser entre
 * Faire un système de cadrillage carré/hexa
 * Permettre à des éléments cadrillable de réceptionner d'autres éléments pouvant être du même cadrillage oiu pas avec des propriétés ou pas
 * Proposer un système d'IA simple pour déplacer des pions par l'ordinateur (avance chemin le plus court à porté etc)
 * Mettre en réseau
 * Lorsque l'on veut reposer dans un élément, prendre en compte celui-ci (rectangle et choisir au plus prêt de la souris)
 * Utils ??? Ajouter la possibilité d'ouvrir plusieurs vue d'un même plateau
*/

namespace Board
{
    public partial class Board : Form, IBoard
    {
        //static private int NetworkBoardCounter = 0;

        static private List<Board> Boards = new List<Board>();
        static private readonly int POS_RND_AMP = 100;
        static private readonly float CLICK_SEUIL = 5.0f;

        public BibliothèqueImage bibliothèqueImage = new BibliothèqueImage();
        public BibliothèqueModel bibliothèqueModel = new BibliothèqueModel();

        private ClientThreadBoard connexion = null;

        public GérerSessions jSession { get; private set; }

        /*private PointF P;
        private float Echelle;*/
        int GIdElémentRéseau = -1;
        //ulong NetworkElementCounter = 0;

        GeoVue GV;
        private Groupe root;
        private Joueur joueur;
        private Dictionary<ushort, Joueur> DicoJoueurs;

        //private PointF SelectedStartP;
        //private Board ElmOnBoard_LEFT;
        private PointF ClickP_LEFT;
        private PointF SelectedP_LEFT;
        //private Element SelectedElm_LEFT;
        private int idAttrapeEnCours;

        //private Board ElmOnBoard_RIGHT;
        private PointF ClickP_RIGHT;
        private PointF SelectedP_RIGHT;
        private MouseButtons MouseBt;
        //private Element SelectedElm_RIGHT;

        private bool Ctrl_Down;
        private bool Shift_Down;

        public Board(string name = "Board principale")
        {
            GV = new GeoVue(0.0f, 0.0f, 1.0f, 0.0f, this.Width, this.Height);
            root = new Groupe();
            joueur = new Joueur();
            DicoJoueurs = null;
            //root.LstElements = new List<Element>();
            //SelectedElm_LEFT = null;
            idAttrapeEnCours = 0;
            MouseBt = MouseButtons.None;
            //SelectedElm_RIGHT = null;
            //ElmOnBoard_LEFT = null;
            //ElmOnBoard_RIGHT = null;
            Ctrl_Down = false;
            Shift_Down = false;
            InitializeComponent();//Win size 1936x1056
            this.MouseWheel += new MouseEventHandler(this.Board_MouseWheel);
            this.Text = name;

            /*byte[] bts = new byte[16];
            new Random().NextBytes(bts);
            string str = bts.Aggregate("", (a, b) => a + ", "+ b);*/
        }

        private void Board_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        static private string[] ImageExtention = { ".JPEG", ".JPG", ".BMP", ".PNG" };

        private void Board_DragDrop(object sender, DragEventArgs e)
        {
            string[] lstFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (lstFiles != null && lstFiles.Length > 0)
            {
                Random rnd = null;
                PointF drop_p = GV.Projection(new PointF((e.X - this.Left - 5), (e.Y - this.Top - 27)));

                if (lstFiles.Length > 1) rnd = new Random();

                foreach (string fullName in lstFiles)
                {
                    //p = new PointF(((e.X - this.Left - 7) / GC.E), ((e.Y - this.Top - 30) / GC.E));
                    PointF p = drop_p;

                    if (rnd != null)
                    {
                        p.X += rnd.Next(POS_RND_AMP) - POS_RND_AMP / 2;
                        p.Y += rnd.Next(POS_RND_AMP) - POS_RND_AMP / 2;
                    }

                    string path, fnm;
                    {
                        int lidx = fullName.LastIndexOf("\\");
                        if(lidx<0) lidx = fullName.LastIndexOf("//");
                        if(lidx > 0)
                        {
                            path = fullName.Substring(0, lidx+1);
                            fnm = fullName.Substring(lidx+1);
                        }
                        else
                        {
                            path = "";
                            fnm = fullName;
                        }
                    }

                    Element newElm;
                    string fileNameEnd = fnm.ToUpper().Trim();
                    if(fileNameEnd.Length >= 4) fileNameEnd = fileNameEnd.Substring(fileNameEnd.Length-4, 4);
                    if (fileNameEnd == ".XML") newElm = LoadXML(fullName, p);
                    else if(ImageExtention.Contains(fileNameEnd)) newElm = new Element2D(path, fnm, p, bibliothèqueImage);
                    else newElm = null;

                    if (newElm != null)
                    {
                        //On est connecté à une session ?
                        if(connexion != null && connexion.NomSession != null)
                        {
                            connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.ChargerElement, new SortedSet<int>(), ref GIdElémentRéseau, newElm);
                        }
                        root.Fusionner(newElm);
                    }
                    /*{
                        PointF size = newElm.Size;
                        newElm.GC.P.X -= size.X / 2.0f;
                        newElm.GC.P.Y -= size.Y / 2.0f;
                        root.Fusionner(newElm);
                    }*/
                }

                this.Refresh();
            }
        }

        private Element LoadXML(string file, PointF p)
        {
            XmlDocument doc = new XmlDocument();
            try { doc.Load(file); }
            catch (System.IO.FileNotFoundException ex)
            {
                MessageBox.Show(this, ex.Message, "Fichier non trouvé", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            catch (System.Xml.XmlException ex)
            {
                MessageBox.Show(this, ex.Message, "Fichier XML incorrect", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            int l = file.LastIndexOf('\\');
            if (l < 0) l = file.LastIndexOf('/');

            string path = file.Substring(0, l + 1);
            if (l >= 0) path = file.Substring(0, l + 1);
            else path = "";

            return Element.Charger(path, doc.ChildNodes.Item(0), p, null, bibliothèqueImage, bibliothèqueModel);
            //return new Groupe(path, doc.ChildNodes.Item(0), p, null, bibliothèqueImage, bibliothèqueModel);
        }

        /*private void Redessiner(Graphics g)
        {
            //g.Clear(Color.White);
            RectangleF vue = new RectangleF(GC.P.X, GC.P.Y, this.Width / GC.E, this.Height / GC.E);
            g.ScaleTransform(GC.E, GC.E);
            g.TranslateTransform(-GC.P.X, -GC.P.Y);
            root.Dessiner(vue, GC.E, g, new PointF(0.0f, 0.0f));
        }*/

        protected override void OnPaint(PaintEventArgs e)
        {
            //Redessiner(e.Graphics);
            Graphics g = e.Graphics;
            RectangleF vue = new RectangleF(GV.GC.P.X, GV.GC.P.Y, this.Width / GV.GC.E, this.Height / GV.GC.E);
            g.TranslateTransform(GV.DimentionD2.X, GV.DimentionD2.Y);
            g.RotateTransform(GV.GC.A);
            g.ScaleTransform(GV.GC.E, GV.GC.E);
            g.TranslateTransform(-GV.GC.P.X, -GV.GC.P.Y);
            root.Dessiner(vue, GV.GC.A, g, new PointF(0.0f, 0.0f));

            //if (SelectedElm_LEFT != null /*&& ElmOnBoard_LEFT == this*/)
            //    SelectedElm_LEFT.Dessiner(vue, GV.GC.A, g, SelectedP_LEFT);
            if (joueur != null) joueur.Dessiner(vue, GV.GC.A, g);
        }

        private void Board_MouseDown(object sender, MouseEventArgs e)
        {
            MouseBt |= e.Button;
            if (e.Button.HasFlag(MouseButtons.Left) || e.Button.HasFlag(MouseButtons.Right))
            {
                //PointF pt = new PointF((e.Location.X / GC.E), (e.Location.Y / GC.E));
                PointF pt = GV.Projection(e.Location);
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    //ElmOnBoard_LEFT = this;
                    ClickP_LEFT = e.Location;
                    SelectedP_LEFT = pt;
                    //joueur.P = pt;
                    //if (/*SelectedElm_RIGHT == null &&*/ Ctrl_Down == false && Shift_Down == false)
                    //{
                    //Element elm = root.MousePiocheAt(pt, GV.GC.A);
                    Element elm, conteneur;
                    (elm, conteneur) = root.MousePickAvecContAt(pt, GV.GC.A, Element.EPickUpAction.Déplacer);
                    if (Shift_Down) AttraperElément(conteneur, elm, pt);//Si shift est done alors on prent l'élément !
                    else PiocherElément(conteneur, elm, pt);// SelectedElm_LEFT = elm?.MousePioche();
                    //}
                    //else SelectedElm_LEFT = null;
                }
                if (e.Button.HasFlag(MouseButtons.Right))
                {
                    //ElmOnBoard_RIGHT = this;
                    ClickP_RIGHT = e.Location;
                    SelectedP_RIGHT = pt;

                    /*if (SelectedElm_LEFT != null || Ctrl_Down == true || Shift_Down == true)
                        SelectedElm_RIGHT = null;
                    else
                    {
                        SelectedElm_RIGHT = root.MousePickAt(pt, GV.GC.A, Element.EPickUpAction.Déplacer);
                        if (SelectedElm_RIGHT != null)
                        {
                            root.PutOnTop(SelectedElm_RIGHT);
                            this.Refresh();
                        }
                    }*/
                }
            }
        }

        private void Board_MouseUp(object sender, MouseEventArgs e)
        {
            MouseBt &= ~e.Button;
            if (e.Button.HasFlag(MouseButtons.Left) && Ctrl_Down == false)
            {
                if (joueur != null && joueur.AElementAttrapé)
                {
                    //if (ElmOnBoard_LEFT == null) ElmOnBoard_LEFT = this;
                    Point abp = new Point(this.Left + e.X - /*ElmOnBoard_LEFT.*/Left, this.Top + e.Y - /*ElmOnBoard_LEFT.*/Top);
                    PointF pt = GV.Projection(abp);
                    Element elmAt = /*ElmOnBoard_LEFT.*/root.MousePickAt(/*ElmOnBoard_LEFT.*/pt, GV.GC.A);
                    if (elmAt == null) elmAt = root;
                    LacherElément(elmAt, pt);
                    //Board brd = ElmOnBoard_LEFT;
                    /*brd.*///SelectedElm_LEFT = null;
                    //brd.ElmOnBoard_LEFT = null;
                    /*brd.*///Refresh();
                }

                //SelectedElm_LEFT = null;
                //ElmOnBoard_LEFT = null;
            }
            if (e.Button.HasFlag(MouseButtons.Right))
            {
                /*if (SelectedElm_RIGHT != null)
                {
                    //if (ElmOnBoard_RIGHT == null) ElmOnBoard_RIGHT = this;
                    //Point abp = new Point(this.Left + e.X - ElmOnBoard_RIGHT.Left, this.Top + e.Y - ElmOnBoard_RIGHT.Top);
                    //float svE = SelectedElm_RIGHT.GC.E;
                    //SelectedElm_RIGHT.GC.E = 0.0f;
                    //Element elmAt = ElmOnBoard_RIGHT.root.MousePickAt(ElmOnBoard_RIGHT.GV.Projection(abp), GV.GC.A);
                    //SelectedElm_RIGHT.GC.E = svE;
                    //if (elmAt != null && elmAt != SelectedElm_RIGHT)
                    //{
                    //    if (elmAt.ElementLaché(SelectedElm_RIGHT) == null)
                    //    {
                    //        Board.DétacherElement(SelectedElm_RIGHT);
                    //    }
                    //    else if(SelectedElm_RIGHT is IFigurine)
                    //    {
                    //        ElmOnBoard_RIGHT.root.MettreAJourZOrdre(SelectedElm_RIGHT as IFigurine);
                    //    }
                    //    SelectedElm_RIGHT = null;
                    //}

                    Board brd = ElmOnBoard_RIGHT;
                    brd.SelectedElm_RIGHT = null;
                    //brd.ElmOnBoard_RIGHT = null;
                    brd.Refresh();
                }*/

                //SelectedElm_RIGHT = null;
                //ElmOnBoard_RIGHT = null;
            }
        }

        private bool IsMouseInner(Point mp)
        {
            return this.Left<= mp.X && this.Top <= mp.Y
                && mp.X <= this.Right && mp.Y <= this.Bottom;
        }

        private void Board_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right))
            {
                PointF pt = GV.Projection(e.Location);
                GV.GC.P.X -= (pt.X - SelectedP_RIGHT.X);
                GV.GC.P.Y -= (pt.Y - SelectedP_RIGHT.Y);
                this.Refresh();
            }
            if (joueur != null && joueur.AElementAttrapé)
            {
                //if (/*e.Button.HasFlag(MouseButtons.Left) || Shift_Down || Ctrl_Down*/)
                {
                    Point abp = new Point(this.Left + e.X, this.Top + e.Y);

                    PointF pt = /*ElmOnBoard_LEFT.*/GV.Projection(new Point(abp.X - /*ElmOnBoard_LEFT.*/Left, abp.Y - /*ElmOnBoard_LEFT.*/Top));
                    // SelectedElm_LEFT.GC.P.X += (pt.X - SelectedP_LEFT.X);
                    // SelectedElm_LEFT.GC.P.Y += (pt.Y - SelectedP_LEFT.Y);
                    joueur.P = pt;
                    /*ElmOnBoard_LEFT.*/
                    Refresh();
                }
                //else
                //{
                //    Point abp = new Point(this.Left + e.X - /*ElmOnBoard_LEFT.*/Left, this.Top + e.Y - /*ElmOnBoard_LEFT.*/Top);
                //    PointF pt = GV.Projection(abp);
                //    Element elmAt = /*ElmOnBoard_LEFT.*/root.MousePickAt(/*ElmOnBoard_LEFT.*/pt, GV.GC.A);
                //    if (elmAt == null) elmAt = root;
                //    LacherElément(elmAt, pt);
                //}
            }
            else if (e.Button.HasFlag(MouseButtons.Left) && idAttrapeEnCours == 0 && (e.Button.HasFlag(MouseButtons.Right) == false /*|| SelectedElm_RIGHT != null*/) /*&& ElmOnBoard_LEFT == this*/)
            {
                PointF pt = GV.Projection(e.Location);
                GV.GC.P.X -= (pt.X - SelectedP_LEFT.X);
                GV.GC.P.Y -= (pt.Y - SelectedP_LEFT.Y);
                this.Refresh();
            }
        }

        /*private void Board_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            PointF pt = GC.Projection(e.Location);
            //PointF pt = new PointF((e.Location.X / GC.E), (e.Location.Y / GC.E));
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                if (root.MouseRangerAt(pt) == null)
                {
                    Element elm = root.MousePickAt(pt);
                    if (elm != null) root.RangerVersParent(elm);
                }
                this.Refresh();
            }
            if (e.Button.HasFlag(MouseButtons.Middle))
            {

            }
            if (e.Button.HasFlag(MouseButtons.Right))
            {

            }
        }*/

        private void Board_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                int delta = e.Delta / 120;
                delta *= 10;
                PointF pt = GV.Projection(e.Location);
                //PointF pt = new PointF((e.Location.X / GC.E), (e.Location.Y / GC.E));
                Element elm;
                if (MouseBt.HasFlag(MouseButtons.Right)) elm = null;
                else
                {
                    if (joueur != null && joueur.AElementAttrapé) elm = null;
                    else elm = root.MousePickAt(pt, GV.GC.A, (Shift_Down ? Element.EPickUpAction.Tourner : Element.EPickUpAction.Roulette));
                }

                if (Shift_Down)
                {
                    if (elm != null && !elm.EstDansEtat(Element.EEtat.RotationFixe)) TournnerElément(elm, delta);//.Tourner(e.Delta);
                    else
                    {
                        /*delta += 8 + (int)((GV.GC.A / 45.0f) + 0.5f);
                        delta %= 8;
                        GV.GC.A = delta * 45.0f;*/
                        int oldA = (int)(GV.GC.A + 0.5f);
                        int newA = oldA + delta;
                        GV.GC.A = GeoCoord2D.AngleFromToAimant45(oldA, newA);
                    }
                }
                else if (elm != null) RouletteElément(elm, delta); //.Roulette(e.Delta);
                else
                {
                    if (e.Delta < 0)
                    {
                        GV.GC.E /= 1.2f;
                    }
                    else if (e.Delta > 0)
                    {
                        GV.GC.E *= 1.2f;
                    }

                    PointF npt = GV.Projection(e.Location);
                    PointF dp = new PointF((npt.X - pt.X), (npt.Y - pt.Y));

                    //PointF npt = new PointF((e.Location.X / GC.E), (e.Location.Y / GC.E));
                    GV.GC.P.X -= dp.X;
                    GV.GC.P.Y -= dp.Y;
                }

                this.Refresh();
            }
        }

        /*private void NouvelleFenêtre()
        {
            Board nb = new Board("Board secondaire");
            nb.WindowState = FormWindowState.Normal;
            nb.Show();
        }*/

        private void Connecter(Point p)
        {
            if (connexion == null)
            {
                ConnectForm cf = new ConnectForm(this, p, connexion);
                if(cf.ShowDialog(this) == DialogResult.Yes) connexion = cf.connection;
            }
            else
            {
                ConnectForm cf = new ConnectForm(this, p, connexion);
                if (cf.ShowDialog(this) == DialogResult.Yes) Déconnecter();
            }
        }

        private bool Déconnecter()
        {
            ClientThreadBoard ct = connexion;
            connexion = null;
            if (ct != null)
            {
                ct.EcrireJournal("Journal.txt");

                //ct.Close();
                ct.fonctionne = false;
                if (ct.SafeStop() == false)
                {
                    ct.Abort();
                    ct.Close();
                }
                //connection = null;
                Nétoyer();
                return true;
            }
            else return true;
        }

        private void Board_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) && Ctrl_Down == false && Shift_Down == false)
            {
                if (joueur != null && joueur.AElementAttrapé)
                {
                    PointF dX = new PointF(e.X - ClickP_LEFT.X, e.Y - ClickP_LEFT.Y);
                    float dCarrée = dX.X * dX.X + dX.Y * dX.Y;
                    if (dCarrée < CLICK_SEUIL)
                    {
                        joueur.ElémentAttrapés.ForEach(elm => RetournerElément(elm));
                        this.Refresh();
                    }
                }
                else
                {
                    PointF pt = GV.Projection(e.Location);
                    Element elm = root.MousePickAt(pt, GV.GC.A);
                    if (elm != null)
                    {
                        RetournerElément(elm);//elm.Retourner();
                        this.Refresh();
                    }
                }
            }
        }

        private void Board_MouseClick(object sender, MouseEventArgs e)
        {
            PointF pt = GV.Projection(e.Location);
            if (e.Button.HasFlag(MouseButtons.Middle))
            {
                if (joueur != null && joueur.AElementAttrapé)
                {
                    joueur.RetournerAttrapées();
                    this.Refresh();
                }
                else
                {
                    Element elm = root.MousePickAt(pt, GV.GC.A, Element.EPickUpAction.Retourner);
                    if (elm != null) RetournerElément(elm);//.Retourner();
                }
            }
            if (e.Button.HasFlag(MouseButtons.Right))
            {
                PointF dX = new PointF(e.X-ClickP_RIGHT.X, e.Y - ClickP_RIGHT.Y);
                float dCarrée = dX.X* dX.X + dX.Y* dX.Y;
                if (dCarrée < CLICK_SEUIL)
                {
                    Element elm = root.MousePickAt(pt, GV.GC.A);
                    if (elm != null)
                    {
                        ContextMenu cm = elm.Menu(this);
                        /*
                         * Board.RangerVersParent(this); ctrl.Refresh();
                        */
                        if (cm == null) cm = new ContextMenu();
                        /*if(elm.EstParent || elm.Parent != null)
                        {
                            cm.MenuItems.Add("-");
                            if (elm.EstParent) cm.MenuItems.Add(new MenuItem("Ranger", (o, eArg) => { RangerVersParent(elm); this.Refresh(); }));
                            if (elm.Parent != null) cm.MenuItems.Add(new MenuItem("Défausser", (o, eArg) => { DéfausserElement(elm); this.Refresh(); }));
                        }*/
                        /*if(connection?.NomSession != null)
                        {
                            cm.MenuItems.Add(new MenuItem("Session", new MenuItem[]
                                {
                                    new MenuItem("Rétablir", new EventHandler((o, eArg) => { })),
                                    new MenuItem("Transmettre", new EventHandler((o, eArg) => { }))
                                }));
                        }*/
                        cm.MenuItems.Add("-");
                        cm.MenuItems.Add(new MenuItem("Supprimer", (o,eArg) => this.Supprimer(elm)));
                        if (cm!=null)cm.Show(this, e.Location);
                    }
                    else
                    {
                        ContextMenu ctxm =
                            new ContextMenu(new MenuItem[]
                            {
                                new MenuItem("Rotation", new MenuItem[]
                                    {
                                        new MenuItem("-135", new EventHandler((o,eArg) => { GV.GC.A=(135); this.Refresh(); })),
                                        new MenuItem(" -90", new EventHandler((o,eArg) => { GV.GC.A=(90.0f); this.Refresh(); })),
                                        new MenuItem(" -45", new EventHandler((o,eArg) => { GV.GC.A=(45.0f); this.Refresh(); })),
                                        new MenuItem("   0", new EventHandler((o,eArg) => { GV.GC.A=(0.0f); this.Refresh(); })),
                                        new MenuItem(" +45", new EventHandler((o,eArg) => { GV.GC.A=(360.0f-45.0f); this.Refresh(); })),
                                        new MenuItem(" +90", new EventHandler((o,eArg) => { GV.GC.A=(360.0f-90.0f); this.Refresh(); })),
                                        new MenuItem("+135", new EventHandler((o,eArg) => { GV.GC.A=(360.0f-135.0f); this.Refresh(); })),
                                        new MenuItem("+180", new EventHandler((o,eArg) => { GV.GC.A=(180.0f); this.Refresh(); }))
                                    })//,
                            //new MenuItem("-"),
                            //new MenuItem("Nouvelle fenêtre", (o,eArg) => NouvelleFenêtre())
                            });
                        ctxm.MenuItems.Add("-");
                        if (connexion == null) ctxm.MenuItems.Add("Connecter", (o, eArg) => Connecter(new Point(this.Left + e.X, this.Top + e.Y)));
                        else
                        {
                            List<MenuItem> lstMenu = new List<MenuItem>();
                            if (connexion != null && connexion.EstIdentifié)
                            {
                                lstMenu.Add(new MenuItem("Créer une session", new EventHandler((o, eArg) => { new CréerSession(connexion, new Point(this.Left + e.X, this.Top + e.Y)).ShowDialog(this); }))); //CréerSession cs = new CréerSession(connection); if(cs.ShowDialog() == DialogResult.Yes) { NomSessionCréée = cs.NomSession; SessionCrééeHashPwd = cs.SessionHashPwd }
                                lstMenu.Add(new MenuItem("Sessions", new EventHandler((o, eArg) => { jSession = new GérerSessions(connexion, new Point(this.Left + e.X, this.Top + e.Y)); if (jSession.ShowDialog() == DialogResult.Yes) ; jSession = null; })));
                                lstMenu.Add(new MenuItem("-"));
                            }
                            lstMenu.Add(new MenuItem("Déconnecter", new EventHandler((o, eArg) => { Connecter(new Point(this.Left + e.X, this.Top + e.Y)); })));
                            ctxm.MenuItems.Add("Connexion", lstMenu.ToArray());
                        }
                        ctxm.MenuItems.Add("-");
                        ctxm.MenuItems.Add(new MenuItem("Tout éffacer", (o, eArg) => this.ToutSupprimer()));
                        ctxm.Show(this, e.Location);
                    }
                }
            }
        }

        private void Board_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (connexion != null)
            {
                if (MessageBox.Show("Êtes-vous sûr de vouloir vous déconnecter ?", "Déconnecter ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Déconnecter();
                    lock (Boards) Boards.Remove(this);
                }
                else e.Cancel = true;
            }
            else if(root.EstVide == false)
            {
                if (MessageBox.Show("Êtes-vous sûr de vouloir fermer ?", "Fermer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    e.Cancel = true;
            }
        }

        private void Board_Load(object sender, EventArgs e)
        {
            lock (Boards) Boards.Add(this);
        }

        private void Board_KeyDown(object sender, KeyEventArgs e)
        {
            Ctrl_Down = e.Control;
            Shift_Down = e.Shift;
        }

        private void Board_KeyUp(object sender, KeyEventArgs e)
        {
            Ctrl_Down = e.Control;
            Shift_Down = e.Shift;
        }

        private void Board_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ' && joueur != null && joueur.AElementAttrapé)
                joueur.RetournerAttrapées();
        }

        private void Board_Resize(object sender, EventArgs e)
        {
            GV.Dimention = new PointF(this.Width, this.Height);
            this.Refresh();
        }

        /*public void PerteDeConnexion(string message)
        {
            Déconnecter();
            MessageBox.Show(message, "Perte de connexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }*/

        /*public void ConnectionRattée(string message)
        {
            Déconnecter();
            MessageBox.Show(message, "Echec de connexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }*/

        /*public void IdentifiantRefusé(string identifiant)
        {
            if (MessageBox.Show("Votre identifiant est refusé.\r\nL'identifiant automatique \"" + identifiant + "\" vous est proposé.\r\nVoulez-vous poursuivre ?", "Identifiant refusé", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                Déconnecter();
            }
        }*/

        /*public void ConnectionRéussie()
        {
            MessageBox.Show("Connexion réussie", "Connexion réussie", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }*/

        private void Nétoyer()
        {
            if (root != null) root.Nétoyer(); else root = new Groupe();
            if (joueur != null) joueur.Remettre(); else joueur = new Joueur();
            DicoJoueurs = null;
            MouseBt = MouseButtons.None;
            idAttrapeEnCours = 0;
            bibliothèqueImage.Netoyer();
            bibliothèqueModel.Netoyer();
            this.Refresh();
        }

        private bool EstDansSession { get => connexion != null && joueur != null && connexion.EstIdentifié && !String.IsNullOrWhiteSpace(connexion.NomSession); }

        #region Méthodes d'intéraction avec le board
        private void RetournerElément(Element elm)
        {
            if(elm != null)
            {
                if (EstDansSession && elm.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.ChangerEtatElément, elm.IdentifiantRéseau, elm.RetournerEtat());
                else
                {
                    elm.Retourner();
                    this.Refresh();
                }
            }
        }

        public void ChangerEtatElément(Element elm, Element.EEtat etat)
        {
            if (elm != null)
            {
                if (EstDansSession && elm.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.ChangerEtatElément, elm.IdentifiantRéseau, etat);
                else
                {
                    elm.MajEtat(etat);
                    Refresh();
                }
            }
        }

        private void RouletteElément(Element elm, int delta)
        {
            if (elm != null)
            {
                if (EstDansSession && elm.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.RouletteElément, elm.IdentifiantRéseau, delta);
                else elm.Roulette(delta);
            }
        }

        private void TournnerElément(Element elm, int delta)
        {
            if (elm != null)
            {
                if (EstDansSession && elm.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.TournerElément, elm.IdentifiantRéseau, delta);
                else elm.Tourner(delta);
            }
        }

        public void RangerVersParent(Element parent)
        {
            if (parent != null)
            {
                if (EstDansSession && parent.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.RangerVersParent, parent.IdentifiantRéseau);
                else
                {
                    Element elm = root.RangerVersParent(parent);
                    if (elm != null)
                    {
                        elm = root.ElementLaché(elm);
                        if (elm != null) root.Suppression(elm);
                    }
                    Refresh();
                }
            }
        }

        public void Mélanger(Element elm)
        {
            if (elm != null)
            {
                if (EstDansSession && elm.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.Mélanger, elm.IdentifiantRéseau);
                else
                {
                    if (elm is Pile)
                    {
                        (elm as Pile).Mélanger();
                        Refresh();
                    }
                }
            }
        }

        public void DéfausserElement(Element relem)
        {
            if (relem != null)
            {
                if (EstDansSession && relem.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.DéfausserElement, relem.IdentifiantRéseau);
                else
                {
                    Element elm = root.DéfausserElement(relem);
                    if (elm != null) Refresh();
                }
            }
        }

        public void ReMettreDansPioche(Défausse défss)
        {
            if (défss != null)
            {
                if (EstDansSession && défss.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.ReMettreDansPioche, défss.IdentifiantRéseau);
                else
                {
                    défss.ReMettreDansLaPioche();
                    Refresh();
                }
            }
        }

        public void MettreEnPioche(Element elm)
        {
            if (elm != null)
            {
                if (EstDansSession && elm.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.MettreEnPioche, elm.IdentifiantRéseau);
                else
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
                        root.ElementLaché(elm);
                        Refresh();
                    }
                }
            }
        }

        public void CréerLaDéfausse(Pioche pioch)
        {
            if (pioch != null)
            {
                if (EstDansSession && pioch.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.CréerLaDéfausse, pioch.IdentifiantRéseau);
                else
                {
                    if (pioch != null)
                    {
                        Défausse deff = new Défausse(pioch);
                        root.ElementLaché(deff);
                        Refresh();
                    }
                }
            }
        }

        public void MettreEnPaquet(Element relem)
        {
            if (relem != null)
            {
                if (EstDansSession && relem.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.MettreEnPaquet, relem.IdentifiantRéseau);
                else
                {
                    relem = root.DétacherElement(relem);
                    if (relem != null) root.ElementLaché(new Paquet(relem));
                }
            }
        }

        private void Supprimer(Element elm)
        {
            if (elm != null)
            {
                if (EstDansSession)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.Supprimer, elm.IdentifiantRéseau);
                {
                    Element selm = root.Suppression(elm);
                    if (selm != null) this.Refresh();
                }
            }
        }

        private void ToutSupprimer()
        {
            if (MessageBox.Show("Vous êtes sur le point de tout éffacer.\r\nÊtes-vous sûr de vouloir le faire ?", "Confirmation de suppression massive", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (EstDansSession)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.SupprimerTout);
                else Nétoyer();
            }
        }

        public void AttraperElément(Element elmSource, Element elmCible, PointF pt)
        {
            if (elmSource == null) elmSource = root;
            if (EstDansSession)
            {
                if (elmCible != null)
                {
                    idAttrapeEnCours = elmCible.IdentifiantRéseau;
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.AttraperElement, pt, elmSource.IdentifiantRéseau, elmCible.IdentifiantRéseau);
                }
            }
            else
            {
                if (elmCible != null)
                {
                    elmCible = elmSource.DétacherElement(elmCible);
                    if(elmCible != null)
                    {
                        /*SelectedElm_LEFT = elmCible;
                        elmCible.GC.P.X -= pt.X;
                        elmCible.GC.P.Y -= pt.Y;
                        SelectedP_LEFT = pt;*/
                        joueur.P = pt;
                        joueur.DonnerElément(elmCible);
                        this.Refresh();
                    }
                    //else SelectedElm_LEFT = null;
                }
                //else SelectedElm_LEFT = null;
            }
        }


        public void PiocherElément(Element elmSource, Element elmCible, PointF pt)
        {
            if (elmCible != null)
            {
                int index = elmCible.GetPiocheIndex();
                if (index == int.MaxValue) AttraperElément(elmSource, elmCible, pt);
                else
                {
                    if (elmSource == null) elmSource = root;
                    if (EstDansSession)
                    {
                        idAttrapeEnCours = elmCible.IdentifiantRéseau;
                        connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.PiocherElement, pt, elmCible.IdentifiantRéseau, index);
                    }
                    else
                    {
                        elmCible = elmCible.MousePioche(index);
                        if (elmCible != null)
                        {
                            elmCible.GC.P.X += elmSource.GC.P.X;
                            elmCible.GC.P.Y += elmSource.GC.P.Y;
                            //SelectedElm_LEFT = elmCible;
                            //SelectedP_LEFT = pt;
                            joueur.P = pt;
                            joueur.DonnerElément(elmCible);
                            this.Refresh();
                        }
                        //else SelectedElm_LEFT = null;
                    }
                }
            }
        }
        
        public void LacherElément(Element elmDestinataire, PointF pt)
        {
            idAttrapeEnCours = 0;
            if (joueur != null && joueur.AElementAttrapé)
            {
                if (elmDestinataire == null) elmDestinataire = root;
                if (EstDansSession)
                {
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.LacherElement, pt, elmDestinataire.IdentifiantRéseau);
                }
                else
                {
                    joueur.P = pt;
                    List<Element> lstElementLaché = joueur.ToutRécupérer();
                    if (lstElementLaché != null)
                        lstElementLaché.ForEach(elm =>
                        {
                            elm = elmDestinataire.ElementLaché(elm);
                            if (elm != null)
                            {
                                if (elm is Pile) root.Suppression(elm);//destruction !
                                else /*ElmOnBoard_LEFT.*/root.ElementLaché(elm);
                            }
                        });
                    //elmLaché.GC.P.X += pt.X;
                    //elmLaché.GC.P.Y += pt.Y;
                    //elmLaché = elmDestinataire.ElementLaché(elmLaché);
                    //if (elmLaché != null)
                    //{
                    //    //ElmOnBoard_LEFT.root.AddTop(SelectedElm_LEFT);
                    //    if (elmLaché is Pile) root.Suppression(elmLaché);//destruction !
                    //    else /*ElmOnBoard_LEFT.*/root.ElementLaché(elmLaché);
                    //}
                    //SelectedElm_LEFT = null;
                    this.Refresh();
                }
            }
        }
        #endregion

        #region Invoke du thread réseau
        private Element TrouverElementRéseau(int idRez)
        {
            if (idRez > 0)
            {
                Element elm = root.MousePickAt(idRez);
                if (elm == null) elm = TrouverElementJoueur(idRez);
                return elm;
            }
            else return null;
        }

        private object MajElement(Element elm)
        {
            if (elm != null && !(elm is ElementRéseau))
            {
                //clientThreadServers.ForEach(j => j.MajElement(elm)); ;
                return root.MettreAJour(elm);
            }
            else return null;
        }

        public void IVK_JoinSession(string session, ushort idJoueur, PointF p)
        {
            Nétoyer();
            if (joueur == null) joueur = new Joueur();
            joueur.IdSessionJoueur = idJoueur;
            joueur.P = p;
            IVK_MessageServeur(OutilsRéseau.EMessage.JoinSession, session);
        }

        public void IVK_MessageServeur(OutilsRéseau.EMessage type, string message)
        {
            string caption;
            MessageBoxIcon ico;
            string session = null;

            switch(type)
            {
                case OutilsRéseau.EMessage.Information:
                    caption = "Information du serveur";
                    ico = MessageBoxIcon.Information;
                    break;
                case OutilsRéseau.EMessage.Attention:
                    caption = "Avertissement du serveur";
                    ico = MessageBoxIcon.Warning;
                    break;
                case OutilsRéseau.EMessage.Erreur:
                    caption = "Erreur du serveur";
                    ico = MessageBoxIcon.Error;
                    break;
                case OutilsRéseau.EMessage.IdentifiantRefusée:
                    if (MessageBox.Show("Votre identifiant est refusé.\r\nL'identifiant automatique \"" + message + "\" vous est proposé.\r\nVoulez-vous poursuivre ?", "Identifiant refusé", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        joueur = new Joueur(0) { Nom = message };
                        this.Text = "Board connecté";
                        message = "Connexion réussie !";
                        caption = "Connexion";
                        ico = MessageBoxIcon.Information;
                        type = OutilsRéseau.EMessage.ConnexionRéussie;
                        session = "";
                    }
                    else
                    {
                        Déconnecter();
                        return;
                    }
                    break;
                case OutilsRéseau.EMessage.ConnexionRéussie:
                    this.Text = "Board connecté";
                    message = "Connexion réussie !";
                    caption = "Connexion";
                    ico = MessageBoxIcon.Information;
                    session = "";
                    break;
                case OutilsRéseau.EMessage.CréaSession:
                    caption = "Création de session réussie";
                    ico = MessageBoxIcon.Information;
                    session = message;
                    message = "Votre session \"" + message + "\" a bien été crée";
                    break;
                case OutilsRéseau.EMessage.RefuSession:
                    caption = "Refus de création de session";
                    ico = MessageBoxIcon.Error;
                    message = "Création de votre session \"" + message + "\" refusée.";
                    break;
                case OutilsRéseau.EMessage.JoinSession:
                    caption = "Entrée en session";
                    ico = MessageBoxIcon.Information;
                    session = message;
                    this.Text = "Board (Session en cours : " + session + ")";
                    message = "Vous rejoignez la session \"" + session + "\".";
                    break;
                case OutilsRéseau.EMessage.QuitSession:
                    caption = "Sortie de session";
                    this.Text = "Board connecté";
                    ico = MessageBoxIcon.Information;
                    Nétoyer();
                    if (joueur != null) joueur.Remettre(); else joueur = new Joueur();
                    message = "Vous quitez la session \"" + message + "\".";
                    break;
                case OutilsRéseau.EMessage.Déconnexion:
                    idAttrapeEnCours = 0;
                    this.Text = "Board";
                    caption = "Perte de connexion";
                    ico = MessageBoxIcon.Error;
                    Nétoyer();
                    if (connexion != null)
                    {
                        connexion.EcrireJournal("Journal.txt");
                        connexion = null;
                        if (root != null)
                        {
                            Nétoyer();
                            this.Refresh();
                        }
                    }
                    break;
                default:
                    caption = "Message du serveur";
                    ico = MessageBoxIcon.Asterisk;
                    break;
            }
            if(message!=null) MessageBox.Show(message, caption, MessageBoxButtons.OK, ico);
            if(session != null && (type == OutilsRéseau.EMessage.ConnexionRéussie || type == OutilsRéseau.EMessage.CréaSession))
            {
                jSession = new GérerSessions(connexion, new Point(this.Left + this.Width / 2, this.Top + this.Height / 2), session);
                if (jSession.ShowDialog() == DialogResult.Yes);
                jSession = null;
            }
        }

        public void IVK_SynchroniserSession(Groupe grp, Dictionary<ushort, Joueur> jrs/*, List<ElementRéseau> élémentRésiduel*/)
        {
            if (grp != null && EstDansSession)
            {
                //SelectedElm_RIGHT = null;
                root = grp;
                if (jrs != null && jrs.Any()) DicoJoueurs = jrs; else DicoJoueurs = null;
                this.Refresh();
            }
        }

        public void IVK_RéidentifierElément(int[] ids)
        {
            if (ids != null && ids.Any() && EstDansSession)
            {
                for (int i = 0; i < ids.Length; i += 2)
                {
                    Element e = root.MousePickAt(ids[i]);
                    if (e != null) e.IdentifiantRéseau = ids[i + 1];
                }
            }
        }

        public void IVK_ArrivéeJoueur(ushort idJr, string nomJr, PointF pJr)
        {
            if (idJr != 0 && EstDansSession)
            {
                if (joueur != null && joueur.IdSessionJoueur == idJr) ;
                else
                {
                    if (DicoJoueurs == null) DicoJoueurs = new Dictionary<ushort, Joueur>();
                    if (DicoJoueurs.ContainsKey(idJr))
                        connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.DemandeSynchro);
                    else DicoJoueurs.Add(idJr, new Joueur(idJr, nomJr) { P = pJr });
                }
            }
        }

        public void IVK_SortieJoueur(ushort idJr, PointF pJr)
        {
            if (idJr != 0 && EstDansSession)
            {
                Joueur jr = RetirerJoueur(idJr);
                if(jr != null)
                {
                    jr.P = pJr;
                    List<Element> lstElm = jr.ToutRécupérer();
                    if(lstElm != null)
                    {
                        lstElm.ForEach(e => root.ElementLaché(e));
                        Refresh();
                    }
                }
            }
        }

        public void IVK_DemandeElement(List<int> idElms)
        {
            if (idElms != null && idElms.Any() && EstDansSession)
            {
                if (root != null)
                {
                    List<Element> lstElms = idElms.Select(id => TrouverElementRéseau(id)).Where(e => !(e is ElementRéseau)).ToList();
                    if (lstElms.Any()) connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.RéceptionElement, ref GIdElémentRéseau, lstElms);
                }
            }
        }

        public void IVK_RéceptionElement(List<Element> lstElms)
        {
            if (lstElms != null && lstElms.Any() && EstDansSession)
            {
                if (root != null)
                {
                    if (lstElms.Any())
                    {
                        lstElms.ForEach(e => MajElement(e));
                        Refresh();
                    }
                }
            }
        }

        public void IVK_RéceptionImage(Image img)
        {
            if(img !=null && EstDansSession && bibliothèqueImage.NouvelleVersion(img))
            {
                root.MettreAJour(img);
                MajImageJoueur(img);
                //if (SelectedElm_LEFT != null) SelectedElm_LEFT.MettreAJour(img);
                //if (SelectedElm_RIGHT != null) SelectedElm_RIGHT.MettreAJour(img);
                bibliothèqueModel.MettreAJour(img);
                this.Refresh();
            }
        }

        public void IVK_RéceptionModel(Model2_5D mld)
        {
            if (mld != null && EstDansSession)
            {
                if(bibliothèqueModel.NouvelleVersion(mld))
                    this.Refresh();
            }
        }

        public void IVK_DemandeImage(List<string> idImgs)
        {
            if (bibliothèqueImage != null && EstDansSession)
            {
                foreach (string sig in idImgs)
                {
                    MemoryStream strm = new MemoryStream();
                    strm.WriteByte((byte)ClientThread.ServeurCodeCommande.RéceptionImage);
                    if (bibliothèqueImage.RécupérerImage(sig, strm)) connexion.EnqueueCommande(strm);
                }
            }
        }

        public void IVK_DemandeModel(List<string> idMods)
        {
            if (bibliothèqueModel != null && EstDansSession)
            {
                foreach (string sig in idMods)
                {
                    MemoryStream strm = new MemoryStream();
                    strm.WriteByte((byte)ClientThread.ServeurCodeCommande.RéceptionModel);
                    if (bibliothèqueModel.RécupérerModel(sig, strm)) connexion.EnqueueCommande(strm);
                }
            }
        }

        public void IVK_ChargerElement(Element elm)
        {
            if (elm != null && EstDansSession)
            {
                root.Fusionner(elm);
                this.Refresh();
            }
        }

        public void IVK_ChangerEtatElément(int idElm, Element.EEtat etat)
        {
            if (EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
                if (elm != null)
                {
                    elm.MajEtat(etat);
                    this.Refresh();
                }
            }
        }

        public void IVK_RouletteElément(int idElm, int delta)
        {
            if (EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
                if (elm != null)
                {
                    elm.Roulette(delta);
                    this.Refresh();
                }
            }
        }

        public void IVK_TournerElément(int idElm, int delta)
        {
            if (EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
                if (elm != null)
                {
                    elm.Tourner(delta);
                    this.Refresh();
                }
            }
        }

        private Joueur TrouverJoueur(ushort idJoueur)
        {
            if (joueur != null && idJoueur == joueur.IdSessionJoueur) //c'est nous qui attrapons ?
                return joueur;
            else
            {
                if (DicoJoueurs == null) DicoJoueurs = new Dictionary<ushort, Joueur>();
                Joueur jr;
                if (!DicoJoueurs.TryGetValue(idJoueur, out jr))
                {
                    jr = new Joueur(idJoueur);
                    DicoJoueurs.Add(idJoueur, jr);
                }
                return jr;
            }
        }

        private Element TrouverElementJoueur(int idElm)
        {
            /*if (SelectedElm_LEFT != null && SelectedElm_LEFT.IdentifiantRéseau == idElm) //c'est nous qui attrapons ?
                return SelectedElm_LEFT;*/
            if(joueur != null && joueur.AElementAttrapé)
            {
                Element elm = joueur.TrouverElementRéseau(idElm);
                if (elm != null) return elm;
            }
            if (DicoJoueurs != null)
            {
                Element elm = null;
                foreach (KeyValuePair<ushort, Joueur> kv in DicoJoueurs)
                {
                    elm = kv.Value.TrouverElementRéseau(idElm);
                    if (elm != null) break;
                }
                return elm;
            }
            else return null;
        }

        private void MajImageJoueur(Image img)
        {
            if (joueur != null) joueur.MajImage(img);
            if (DicoJoueurs != null)
            {
                foreach (KeyValuePair<ushort, Joueur> kv in DicoJoueurs)
                    kv.Value.MajImage(img);
            }
        }

        private Joueur RetrouverJoueur(ushort idJoueur)
        {
            if (joueur != null && idJoueur == joueur.IdSessionJoueur) //c'est nous qui attrapons ?
                return joueur;
            else if(DicoJoueurs != null)
            {
                Joueur jr;
                if (DicoJoueurs.TryGetValue(idJoueur, out jr)) return jr;
                else return null;
            }
            else return null;
        }

        private Joueur RetirerJoueur(ushort idJoueur)
        {
            if (joueur != null && idJoueur == joueur.IdSessionJoueur) //c'est nous qui attrapons ?
                return joueur;
            else if (DicoJoueurs != null)
            {
                Joueur jr;
                if (DicoJoueurs.TryGetValue(idJoueur, out jr))
                {
                    DicoJoueurs.Remove(idJoueur);
                    return jr;
                }
                else return null;
            }
            else return null;
        }

        private Joueur DonnerElementAJoueur(ushort idJr, PointF pj, Element elmAtrp)
        {
            if (elmAtrp != null)
            {
                Joueur jr = TrouverJoueur(idJr);
                if (jr != null)
                {
                    if(joueur == jr) //c'est nous qui attrapons ?
                    {
                        if (idAttrapeEnCours == 0 || idAttrapeEnCours != elmAtrp.IdentifiantRéseau) // on attrape autre chose ou alors on a déjà relaché...
                        {
                            Element elmRescp = root.MousePickAt(pj, GV.GC.A);
                            if (elmRescp == null) elmRescp = root;
                            LacherElément(elmRescp, pj);
                        }
                        else idAttrapeEnCours = 0;
                    }
                    jr.P = pj;
                    jr.DonnerElément(elmAtrp);
                    this.Refresh();
                }
                return jr;
            }
            else return null;
        }

        public void IVK_AttraperElement(ushort idJr, PointF pj, int idElmPSrc, int idElmAtrp) //J'attrape l'élément idElmAtrp depuis idElmPSrc
        {
            if (EstDansSession && idElmPSrc > 0 && idElmAtrp > 0)
            {
                Element elmPSrc = TrouverElementRéseau(idElmPSrc);
                if (elmPSrc == null) elmPSrc = root;
                Element elmAtrp = elmPSrc.MousePickAt(idElmAtrp);
                if (elmAtrp != null)
                {
                    elmAtrp = elmPSrc.DétacherElement(elmAtrp);
                    DonnerElementAJoueur(idJr, pj, elmAtrp);
                }
            }
        }

        public void IVK_PiocherElement(ushort idJr, PointF pj, int idElmPSrc, int index, int idElm)
        {
            if (EstDansSession && idElmPSrc > 0)
            {
                idAttrapeEnCours = 0;
                Element elmPSrc = TrouverElementRéseau(idElmPSrc);
                if (elmPSrc != null)
                {
                    Element elmAtrp = elmPSrc.MousePioche(index);
                    if (elmAtrp != null)
                    {
                        elmAtrp.IdentifiantRéseau = idElm;
                        DonnerElementAJoueur(idJr, pj, elmAtrp);
                    }
                }
            }
        }

        //private Element RécupérerElementDeJoueur(ushort idJr, PointF pj, int idElm)
        //{
        //    if (idElm > 0)
        //    {
        //        Element elm;
        //        /*if (idJr == joueur.IdSessionJoueur) //c'est nous qui attrapons ?
        //        {
        //            joueur.P = pj;
        //            elm = SelectedElm_LEFT;
        //            SelectedElm_LEFT = null;
        //            if (elm != null)
        //            {
        //                elm.GC.P.X += pj.X;
        //                elm.GC.P.Y += pj.Y;
        //            }
        //        }
        //        else
        //        {*/
        //            Joueur jr = TrouverJoueur(idJr);
        //            jr.P = pj;
        //            elm = jr.RécupérerElémentRéseau(idElm);
        //        //}
        //        return elm;
        //    }
        //    return null;
        //}

        private List<Element> RécupérerElementAttrapéDeJoueur(ushort idJr, PointF pj)
        {
            Joueur jr = TrouverJoueur(idJr);
            if (jr != null)
            {
                if (jr == joueur) idAttrapeEnCours = 0;
                jr.P = pj;
                List<Element>  lelm = jr.ToutRécupérer();
                return lelm;
            }
            else return null;
        }

        public void IVK_LacherElement(ushort idJr, PointF pj, int idElmRecep)
        {
            if (EstDansSession)
            {
                Element elmRecep;
                if (idElmRecep > 0)
                {
                    elmRecep = TrouverElementRéseau(idElmRecep);
                    if (elmRecep == null) elmRecep = root;
                }
                else elmRecep = root;

                //Element elmLach = RécupérerElementDeJoueur(idJr, pj, idElmLach);
                List<Element> lelm = RécupérerElementAttrapéDeJoueur(idJr, pj);
                if (lelm != null && elmRecep != null)
                {
                    if (idAttrapeEnCours != 0 && lelm.Any(e => e.IdentifiantRéseau == idAttrapeEnCours))
                        idAttrapeEnCours = 0;
                    lelm.ForEach(elmLach =>
                    {
                        elmLach = elmRecep.ElementLaché(elmLach);
                        if (elmRecep != null)
                        {
                            //if (idAttrapeEnCours == elmLach.IdentifiantRéseau) idAttrapeEnCours = 0;
                            if (elmLach is Pile) root.Suppression(elmLach);
                            else root.ElementLaché(elmLach);
                            //DonnerElementAJoueur(idJr, pj, elmRecep);
                        }
                    });
                    this.Refresh();
                }
            }
        }

        public void IVK_RangerVersParent(int idElm)
        {
            if (idElm > 0 && EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
                if (elm != null)
                {
                    elm = root.ElementLaché(elm);
                    if (elm != null) root.Suppression(elm);
                    Refresh();
                }
            }
        }

        public void IVK_Mélanger(int idElm, int seed)
        {
            if (idElm > 0 && EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
                if (elm is Pile)
                {
                    (elm as Pile).Mélanger(new Random(seed));
                    Refresh();
                }
            }
        }

        public void IVK_DéfausserElement(int idElm)
        {
            if (idElm > 0 && EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
                elm = root.DéfausserElement(elm);
                if (elm != null) Refresh();
            }
        }

        public void IVK_ReMettreDansPioche(int idElm)
        {
            if (idElm > 0 && EstDansSession)
            {
                Défausse déffs = TrouverElementRéseau(idElm) as Défausse;
                if (déffs != null)
                {
                    déffs.ReMettreDansLaPioche();
                    Refresh();
                }
            }
        }

        public void IVK_MettreEnPioche(int idElm, int idPch)
        {
            if (idElm > 0 && EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
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
                    elm.IdentifiantRéseau = idPch;
                    root.ElementLaché(elm);
                    Refresh();
                }
            }
        }

        public void IVK_CréerLaDéfausse(int idElm, int idDefss)
        {
            if (idElm > 0 && EstDansSession)
            {
                Pioche pioch = TrouverElementRéseau(idElm) as Pioche;
                if (pioch != null)
                {
                    Défausse deff = new Défausse(pioch);
                    deff.IdentifiantRéseau = idDefss;
                    root.ElementLaché(deff);
                    Refresh();
                }
            }
        }

        public void IVK_MettreEnPaquet(int idElm, int idPaq)
        {
            if (idElm > 0 && EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
                if (elm != null)
                {
                    elm = root.DétacherElement(elm);
                    if (elm != null)
                    {
                        elm = new Paquet(elm);
                        elm.IdentifiantRéseau = idPaq;
                        root.ElementLaché(elm);
                    }
                    Refresh();
                }
            }
        }

        public void IVK_Supprimer(int idElm)
        {
            if (idElm > 0 && EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
                elm = root.Suppression(elm);
                if(elm != null) Refresh();
            }
        }

        public void IVK_SupprimerTout()
        {
            if (EstDansSession)
            {
                Nétoyer();
            }
        }
        #endregion
    }
}
