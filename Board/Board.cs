using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots;
using ModuleBOARD.Elements.Pieces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;


/* TODO
 * Faire un système de cadrillage carré/hexa
 * Permettre à des éléments cadrillable de réceptionner d'autres éléments pouvant être du même cadrillage oiu pas avec des propriétés ou pas
 * Proposer un système d'IA simple pour déplacer des pions par l'ordinateur (avance chemin le plus court à porté etc)
 * Mettre en réseau
 * Lorsque l'on veut reposer dans un élément, prendre en compte celui-ci (rectangle et choisir au plus prêt de la souris)
 * Utils ??? Ajouter la possibilité d'ouvrir plusieurs vue d'un même plateau
*/

namespace Board
{
    public partial class Board : Form
    {
        //static private int NetworkBoardCounter = 0;

        static private List<Board> Boards = new List<Board>();
        static private readonly int POS_RND_AMP = 100;
        static private readonly float CLICK_SEUIL = 5.0f;

        static public BibliothèqueImage bibliothèqueImage = new BibliothèqueImage();
        static public BibliothèqueModel bibliothèqueModel = new BibliothèqueModel();

        ClientThreadBoard connection = null;

        static public Element RangerVersParent(Element parent)
        {
            lock(Boards)
            {
                foreach (Board brd in Boards)
                {
                    brd.root.RangerVersParent(parent);
                    brd.Refresh();
                }
                return null;
            }
        }
        static public Element DéfausserElement(Element relem)
        {
            lock (Boards)
            {
                foreach (Board brd in Boards)
                {
                    Element elm = brd.root.DéfausserElement(relem);
                    if (elm != null)
                    {
                        //brd.Refresh();
                        Rafraichir();
                        return elm;
                    }
                }
            }
            return null;
        }

        static public Element DétacherElement(Element relem)
        {
            lock (Boards)
            {
                foreach (Board brd in Boards)
                {
                    Element elm = brd.root.DétacherElement(relem);
                    if (elm != null)
                    {
                        brd.Refresh();
                        return elm;
                    }
                }
            }
            return null;
        }

        static public Element Rafraichir()
        {
            lock (Boards)
            {
                foreach (Board brd in Boards)
                    brd.Refresh();
            }
            return null;
        }

        /*private PointF P;
        private float Echelle;*/
        int NetworkBoardId = -1;
        //ulong NetworkElementCounter = 0;

        GeoVue GV;
        private Groupe root;

        //private PointF SelectedStartP;
        private Board ElmOnBoard_LEFT;
        private PointF ClickP_LEFT;
        private PointF SelectedP_LEFT;
        private Element SelectedElm_LEFT;

        private Board ElmOnBoard_RIGHT;
        private PointF ClickP_RIGHT;
        private PointF SelectedP_RIGHT;
        private Element SelectedElm_RIGHT;

        private bool Ctrl_Down;
        private bool Shift_Down;
        public Board(string name = "Board principale")
        {
            GV = new GeoVue(0.0f, 0.0f, 1.0f, 0.0f, this.Width, this.Height);
            root = new Groupe();
            //root.LstElements = new List<Element>();
            SelectedElm_LEFT = null;
            SelectedElm_RIGHT = null;
            ElmOnBoard_LEFT = null;
            ElmOnBoard_RIGHT = null;
            Ctrl_Down = false;
            Shift_Down = false;
            InitializeComponent();//Win size 1936x1056
            this.MouseWheel += new MouseEventHandler(this.Board_MouseWheel);
            this.Text = name;
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
                    if (fileNameEnd == ".XML")
                        newElm = LoadXML(fullName, p);
                    else if(ImageExtention.Contains(fileNameEnd)) newElm = new Element2D(path, fnm, p, bibliothèqueImage);
                    else newElm = null;

                    if (newElm != null) root.Fusionner(newElm);
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

        private Groupe LoadXML(string file, PointF p)
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

            return new Groupe(path, doc, p, null, bibliothèqueImage, bibliothèqueModel);
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

            if (SelectedElm_LEFT != null && ElmOnBoard_LEFT == this)
                SelectedElm_LEFT.Dessiner(vue, GV.GC.A, g, new PointF(0.0f, 0.0f));
        }

        private void Board_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) || e.Button.HasFlag(MouseButtons.Right))
            {
                //PointF pt = new PointF((e.Location.X / GC.E), (e.Location.Y / GC.E));
                PointF pt = GV.Projection(e.Location);
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    ElmOnBoard_LEFT = this;
                    SelectedP_LEFT = pt;
                    if (SelectedElm_RIGHT == null && Ctrl_Down == false && Shift_Down == false)
                    {
                        //Element elm = root.MousePiocheAt(pt, GV.GC.A);
                        Element elm, conteneur;
                        (elm, conteneur) = root.MousePickAvecContAt(pt, GV.GC.A);
                        SelectedElm_LEFT = elm?.MousePioche();
                        if (SelectedElm_LEFT != null)
                        {
                            if (conteneur != null)
                            {
                                if (SelectedElm_LEFT == elm) conteneur.DétacherElement(elm);
                                else
                                {
                                    SelectedElm_LEFT.GC.P.X += conteneur.GC.P.X;
                                    SelectedElm_LEFT.GC.P.Y += conteneur.GC.P.Y;
                                }
                            }
                            SelectedP_LEFT = pt;
                            this.Refresh();
                        }
                    }
                    else SelectedElm_LEFT = null;
                }
                if (e.Button.HasFlag(MouseButtons.Right))
                {
                    ElmOnBoard_RIGHT = this;
                    ClickP_RIGHT = e.Location;
                    SelectedP_RIGHT = pt;

                    if (SelectedElm_LEFT != null || Ctrl_Down == true || Shift_Down == true)
                        SelectedElm_RIGHT = null;
                    else
                    {
                        SelectedElm_RIGHT = root.MousePickAt(pt, GV.GC.A);
                        if (SelectedElm_RIGHT != null)
                        {
                            root.PutOnTop(SelectedElm_RIGHT);
                            this.Refresh();
                        }
                    }
                }
            }
        }

        private void Board_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                if (SelectedElm_LEFT != null)
                {
                    if (ElmOnBoard_LEFT == null) ElmOnBoard_LEFT = this;
                    Point abp = new Point(this.Left + e.X - ElmOnBoard_LEFT.Left, this.Top + e.Y - ElmOnBoard_LEFT.Top);
                    Element elmAt = ElmOnBoard_LEFT.root.MousePickAt(ElmOnBoard_LEFT.GV.Projection(abp), GV.GC.A);
                    if (elmAt != null)
                    {
                        SelectedElm_LEFT = elmAt.ElementLaché(SelectedElm_LEFT);
                        if(SelectedElm_LEFT != null)
                            ElmOnBoard_LEFT.root.AddTop(SelectedElm_LEFT);
                    }
                    else ElmOnBoard_LEFT.root.AddTop(SelectedElm_LEFT);
                    Board brd = ElmOnBoard_LEFT;
                    brd.SelectedElm_LEFT = null;
                    brd.ElmOnBoard_LEFT = null;
                    brd.Refresh();
                }

                SelectedElm_LEFT = null;
                ElmOnBoard_LEFT = null;
            }
            if (e.Button.HasFlag(MouseButtons.Right))
            {
                if (SelectedElm_RIGHT != null)
                {
                    if (ElmOnBoard_RIGHT == null) ElmOnBoard_RIGHT = this;
                    Point abp = new Point(this.Left + e.X - ElmOnBoard_RIGHT.Left, this.Top + e.Y - ElmOnBoard_RIGHT.Top);
                    float svE = SelectedElm_RIGHT.GC.E;
                    SelectedElm_RIGHT.GC.E = 0.0f;
                    Element elmAt = ElmOnBoard_RIGHT.root.MousePickAt(ElmOnBoard_RIGHT.GV.Projection(abp), GV.GC.A);
                    SelectedElm_RIGHT.GC.E = svE;
                    if (elmAt != null && elmAt != SelectedElm_RIGHT)
                    {
                        if (elmAt.ElementLaché(SelectedElm_RIGHT) == null)
                        {
                            Board.DétacherElement(SelectedElm_RIGHT);
                        }
                        else if(SelectedElm_RIGHT is IFigurine)
                        {
                            ElmOnBoard_RIGHT.root.MettreAJourZOrdre(SelectedElm_RIGHT as IFigurine);
                        }
                        SelectedElm_RIGHT = null;
                    }

                    Board brd = ElmOnBoard_RIGHT;
                    brd.SelectedElm_RIGHT = null;
                    brd.ElmOnBoard_RIGHT = null;
                    brd.Refresh();
                }

                SelectedElm_RIGHT = null;
                ElmOnBoard_RIGHT = null;
            }
        }

        private bool IsMouseInner(Point mp)
        {
            return this.Left<= mp.X && this.Top <= mp.Y
                && mp.X <= this.Right && mp.Y <= this.Bottom;
        }

        private void Board_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left) || e.Button.HasFlag(MouseButtons.Right))
            {
                if (e.Button.HasFlag(MouseButtons.Right))
                {
                    if (SelectedElm_RIGHT != null)
                    {
                        Point abp = new Point(this.Left + e.X, this.Top + e.Y);

                        if (ElmOnBoard_RIGHT == null) ElmOnBoard_RIGHT = this;
                        lock (Boards)
                        {
                            if (ElmOnBoard_RIGHT.IsMouseInner(abp) == false)
                            {
                                foreach(Board brd in Boards)
                                    if(brd != ElmOnBoard_RIGHT && brd.IsMouseInner(abp))
                                    {
                                        ElmOnBoard_RIGHT.root.DétacherElement(SelectedElm_RIGHT);
                                        Point DltMP = new Point(abp.X - brd.Left, abp.Y - brd.Top);
                                        PointF DlElmP = new PointF(
                                                SelectedElm_RIGHT.GC.P.X- SelectedP_RIGHT.X,
                                                SelectedElm_RIGHT.GC.P.Y- SelectedP_RIGHT.Y
                                            );
                                        SelectedP_RIGHT = brd.GV.Projection(DltMP);
                                        SelectedElm_RIGHT.GC.P.X = SelectedP_RIGHT.X + DlElmP.X;
                                        SelectedElm_RIGHT.GC.P.Y = SelectedP_RIGHT.Y + DlElmP.Y;

                                        brd.SelectedElm_RIGHT = ElmOnBoard_RIGHT.SelectedElm_RIGHT;
                                        brd.root.AddTop(SelectedElm_RIGHT);
                                        if (ElmOnBoard_RIGHT != this)
                                        {
                                            ElmOnBoard_RIGHT.ElmOnBoard_RIGHT = null;
                                            ElmOnBoard_RIGHT.SelectedElm_RIGHT = null;
                                            ElmOnBoard_RIGHT.Refresh();
                                            ElmOnBoard_RIGHT = brd;
                                            brd.ElmOnBoard_RIGHT = brd;
                                        }
                                        else
                                        {
                                            ElmOnBoard_RIGHT = brd;
                                            this.Refresh();
                                            brd.ElmOnBoard_RIGHT = brd;
                                        }
                                        break; 
                                    }
                            }
                            if(SelectedElm_RIGHT is IFigurine) ElmOnBoard_RIGHT.root.MettreAJourZOrdre(SelectedElm_RIGHT as IFigurine);
                        }
                        

                        PointF pt = ElmOnBoard_RIGHT.GV.Projection(new Point(abp.X - ElmOnBoard_RIGHT.Left, abp.Y - ElmOnBoard_RIGHT.Top));
                        SelectedElm_RIGHT.GC.P.X += (pt.X - SelectedP_RIGHT.X);
                        SelectedElm_RIGHT.GC.P.Y += (pt.Y - SelectedP_RIGHT.Y);
                        SelectedP_RIGHT = pt;
                        ElmOnBoard_RIGHT.Refresh();
                    }
                    else if(ElmOnBoard_RIGHT == this)
                    {
                        PointF pt = GV.Projection(e.Location);
                        GV.GC.P.X -= (pt.X - SelectedP_RIGHT.X);
                        GV.GC.P.Y -= (pt.Y - SelectedP_RIGHT.Y);
                        this.Refresh();
                    }
                }
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    if (SelectedElm_LEFT != null)
                    {
                        Point abp = new Point(this.Left + e.X, this.Top + e.Y);

                        if (ElmOnBoard_LEFT == null) ElmOnBoard_LEFT = this;
                        if (ElmOnBoard_LEFT.IsMouseInner(abp) == false)
                        {
                            lock (Boards)
                            {
                                foreach (Board brd in Boards)
                                    if (ElmOnBoard_LEFT != brd && brd.IsMouseInner(abp))
                                    {
                                        //ElmOnBoard_LEFT.root.DétacherElement(SelectedElm_LEFT);
                                        Point DltMP = new Point(abp.X - brd.Left, abp.Y - brd.Top);
                                        PointF DlElmP = new PointF(
                                                SelectedElm_LEFT.GC.P.X - SelectedP_LEFT.X,
                                                SelectedElm_LEFT.GC.P.Y - SelectedP_LEFT.Y
                                            );
                                        SelectedP_LEFT = brd.GV.Projection(DltMP);
                                        SelectedElm_LEFT.GC.P.X = SelectedP_LEFT.X + DlElmP.X;
                                        SelectedElm_LEFT.GC.P.Y = SelectedP_LEFT.Y + DlElmP.Y;
                                        //brd.root.AddTop(SelectedElm_LEFT);
                                        brd.SelectedElm_LEFT = ElmOnBoard_LEFT.SelectedElm_LEFT;
                                        if (ElmOnBoard_LEFT != this)
                                        {
                                            ElmOnBoard_LEFT.ElmOnBoard_LEFT = null;
                                            ElmOnBoard_LEFT.SelectedElm_LEFT = null;
                                            ElmOnBoard_LEFT.Refresh();
                                            ElmOnBoard_LEFT = brd;
                                            brd.ElmOnBoard_LEFT = brd;
                                        }
                                        else
                                        {
                                            ElmOnBoard_LEFT = brd;
                                            this.Refresh();
                                            brd.ElmOnBoard_LEFT = brd;
                                        }
                                        break;
                                    }
                            }
                        }

                        PointF pt = ElmOnBoard_LEFT.GV.Projection(new Point(abp.X - ElmOnBoard_LEFT.Left, abp.Y - ElmOnBoard_LEFT.Top));
                        SelectedElm_LEFT.GC.P.X += (pt.X - SelectedP_LEFT.X);
                        SelectedElm_LEFT.GC.P.Y += (pt.Y - SelectedP_LEFT.Y);
                        SelectedP_LEFT = pt;
                        ElmOnBoard_LEFT.Refresh();
                    }
                    else if ((e.Button.HasFlag(MouseButtons.Right) == false || SelectedElm_RIGHT != null) && ElmOnBoard_LEFT == this)
                    {
                        PointF pt = GV.Projection(e.Location);
                        GV.GC.P.X -= (pt.X - SelectedP_LEFT.X);
                        GV.GC.P.Y -= (pt.Y - SelectedP_LEFT.Y);
                        this.Refresh();
                    }
                }

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
                PointF pt = GV.Projection(e.Location);
                //PointF pt = new PointF((e.Location.X / GC.E), (e.Location.Y / GC.E));
                Element elm;
                if (SelectedElm_LEFT == null && SelectedElm_RIGHT == null && Shift_Down == false)
                    elm = root.MousePickAt(pt, GV.GC.A);
                else elm = null;

                if (elm != null)
                {
                    if(Ctrl_Down) elm.Tourner(e.Delta);
                    else elm.Roulette(e.Delta);
                }
                else if(Ctrl_Down)
                {
                    int delta = e.Delta / 120;
                    delta += 8 + (int)((GV.GC.A / 45.0f) + 0.5f);
                    delta %= 8;
                    GV.GC.A = delta * 45.0f;
                }
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

        private void NouvelleFenêtre()
        {
            Board nb = new Board("Board secondaire");
            nb.WindowState = FormWindowState.Normal;
            nb.Show();
        }

        private void Connecter()
        {
            if (connection == null || connection.GérerConnexion() == false)
            {
                if (root != null && root.EstVide == false)
                {
                    DialogResult dr = MessageBox.Show("Effacer la table ?", "Votre table sera éffacées, voulez-vous poursuivre ?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (dr != DialogResult.Yes) return;
                }
                ConnectForm cf = new ConnectForm(this);
                cf.ShowDialog(this);
                connection = cf.connection;
            }
        }

        private void Déconnecter()
        {
            if (connection != null &&
                MessageBox.Show("Déconnecter ?", "Êtes-vous sûr de vouloir vous déconnecter ?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                connection.Close();
                if(connection.SafeStop() == false)
                    connection.Abort();
                connection = null;
            }
        }

        private Element Supprimer(Element elm)
        {
            
            Element selm = root.Suppression(elm);
            if (selm != null) this.Refresh();
            return selm;
        }

        private void Board_MouseClick(object sender, MouseEventArgs e)
        {
            PointF pt = GV.Projection(e.Location);
            if (e.Button.HasFlag(MouseButtons.Middle))
            {
                Element elm = root.MousePickAt(pt, GV.GC.A);
                if (elm != null) elm.Retourner();
                this.Refresh();
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
                        if(elm.EstParent || elm.Parent != null)
                        {
                            cm.MenuItems.Add("-");
                            if (elm.EstParent) cm.MenuItems.Add(new MenuItem("Ranger", (o, eArg) => { Board.RangerVersParent(elm); this.Refresh(); }));
                            if (elm.Parent != null) cm.MenuItems.Add(new MenuItem("Défausser", (o, eArg) => { Board.DéfausserElement(elm); this.Refresh(); }));
                        }
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
                                    }),
                            new MenuItem("-"),
                            new MenuItem("Nouvelle fenêtre", (o,eArg) => NouvelleFenêtre())
                            });
                        ctxm.MenuItems.Add("-");
                        /*if (connection == null)*/ ctxm.MenuItems.Add("Connexion", (o, eArg) => Connecter());
                        //else ctxm.MenuItems.Add("Déconnecter", (o, eArg) => Déconnecter());
                        ctxm.Show(this, e.Location);
                    }
                }
            }
        }

        private void Board_FormClosing(object sender, FormClosingEventArgs e)
        {
            lock (Boards) Boards.Remove(this);
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

        private void Board_Resize(object sender, EventArgs e)
        {
            GV.Dimention = new PointF(this.Width, this.Height);
        }
    }
}
