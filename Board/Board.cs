using Board.Properties;
using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots;
using ModuleBOARD.Elements.Lots.Dés;
using ModuleBOARD.Elements.Lots.Piles;
using ModuleBOARD.Elements.Pieces;
using ModuleBOARD.Réseau;
using NAudio.Wave;
using NAudioDemo.NetworkChatDemo;
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
 * Bug* On ne peut pas feuilleter un paquet qui est bloqué au niveau rotation
 * Bug* On peut déplacer un objet vérouillé en déplacement via la touche shift...
 * Afficher les cartes attrapées par les joueurs
 * Débuguer les actions spéciales comme le rangement auto etc
 * Songer à un système de compression basé sur une carte maitresse et par différences d'éléments
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

        static public bool PARAM_FLAG = false;
        static public string PARAM_SERVEUR = "localhost";
        static public string PARAM_PORT = "8080";
        static public string PARAM_LOGIN = "";
        static public string PARAM_MOTDEPASSE = "";

        public BibliothèqueImage bibliothèqueImage = new BibliothèqueImage();
        public BibliothèqueModel bibliothèqueModel = new BibliothèqueModel();

        private ClientThreadBoard connexion = null;
        private int entréeAudio = -1;
        private WaveIn waveIn = null;
        private int sortieAudio = -1;
        private INetworkChatCodec AudioChatCodec = null;

        public GérerSessions jSession { get; private set; }

        /*private PointF P;
        private float Echelle;*/
        int GIdElémentRéseau = -1;
        //ulong NetworkElementCounter = 0;

        GeoVue GV;
        //private DateTime dateRendu;
        private Groupe root;
        private Joueur joueur;
        private Dictionary<ushort, Joueur> DicoJoueurs;

        //private PointF SelectedStartP;
        //private Board ElmOnBoard_LEFT;
        private PointF ClickP_LEFT;
        private PointF SelectedP_LEFT;
        //private Element SelectedElm_LEFT;
        //private int idAttrapeEnCours;
        private List<int> lstIdAttrapeEnCours;

        //private Board ElmOnBoard_RIGHT;
        private PointF ClickP_RIGHT;
        private PointF SelectedP_RIGHT;
        private MouseButtons MouseBt;
        //private Element SelectedElm_RIGHT;

        private bool Ctrl_Down;
        private bool Shift_Down;

        private Image[] textureDuBoard;
        private Size textureTaille;

        public Board(string name = "Board principale")
        {
            GV = new GeoVue(0.0f, 0.0f, 1.0f, 0.0f, this.Width, this.Height);
            //dateRendu = DateTime.Now.AddSeconds(-1);
            root = new Groupe();
            joueur = new Joueur();
            DicoJoueurs = null;
            //root.LstElements = new List<Element>();
            //SelectedElm_LEFT = null;
            //idAttrapeEnCours = 0;
            lstIdAttrapeEnCours = null;
            MouseBt = MouseButtons.None;

            entréeAudio = -1;
            waveIn = null;
            sortieAudio = -1;
            AudioChatCodec = null;
            //SelectedElm_RIGHT = null;
            //ElmOnBoard_LEFT = null;
            //ElmOnBoard_RIGHT = null;
            Ctrl_Down = false;
            Shift_Down = false;
            InitializeComponent();//Win size 1936x1056
            this.MouseWheel += new MouseEventHandler(this.Board_MouseWheel);
            this.Text = name;

            ImageList imageListLarge = new ImageList();
            imageListLarge.Images.Add(Resources.PJ);//0
            imageListLarge.Images.Add(Resources.PJMaitre); //1
            imageListLarge.Images.Add(Resources.PJMoi); //2
            imageListLarge.Images.Add(Resources.PJMoiMaitre); //3
            lvConsGJrs.LargeImageList = imageListLarge;
            lvGJrs.LargeImageList = imageListLarge;
            lvTableJr.LargeImageList = imageListLarge;

            /*byte[] bts = new byte[16];
            new Random().NextBytes(bts);
            string str = bts.Aggregate("", (a, b) => a + ", "+ b);*/
            RécupérerTextureBoard();
            this.ActiveControl = null;
            /*this.Activate();*/

            if (Board.PARAM_FLAG)
                Connecter(new Point(Left + Width / 2, Top + Height / 2));
        }

        #region ChatAudio
        private void FermerChatAudioMicro()
        {
            WaveIn wvin = waveIn;
            waveIn = null;
            if (wvin != null)
            {
                wvin.DataAvailable -= OnAudioCaptured;
                wvin.StopRecording();
                wvin.Dispose();
            }
        }

        private void FermerChatAudioHP()
        {
            if(DicoJoueurs != null && DicoJoueurs.Any())
            {
                foreach (KeyValuePair<ushort, Joueur> dj in DicoJoueurs)
                    if (dj.Value != null) dj.Value.FermerChatAudioHP();
            }
        }

        private void FermerChatAudio()
        {
            FermerChatAudioMicro();
            FermerChatAudioHP();
            AudioChatCodec = null;
        }

        private void ActivierChatAudioMicro(INetworkChatCodec codec)
        {
            if (codec != null)
            {
                if (WaveIn.DeviceCount > 0)
                {
                    if (entréeAudio < 0 || WaveIn.DeviceCount <= entréeAudio) entréeAudio = 0;
                }
                else entréeAudio = -1;
                if (entréeAudio >= 0)
                {
                    waveIn = new WaveIn(this.Handle);
                    waveIn.BufferMilliseconds = 200;
                    waveIn.DeviceNumber = entréeAudio;
                    waveIn.WaveFormat = codec.RecordFormat;
                    waveIn.DataAvailable += OnAudioCaptured;
                    waveIn.StartRecording();
                }
            }
        }

        private void ActivierChatAudioHP(INetworkChatCodec codec)
        {
            if (codec != null)
            {
                if (WaveOut.DeviceCount > 0)
                {
                    if (sortieAudio < 0 || WaveOut.DeviceCount <= sortieAudio) sortieAudio = 0;
                }
                else sortieAudio = -1;
                if (sortieAudio >= 0)
                {
                    if (DicoJoueurs != null && DicoJoueurs.Any())
                    {
                        foreach (KeyValuePair<ushort, Joueur> dj in DicoJoueurs)
                            if (dj.Value != null) dj.Value.ActivierChatAudioHP(codec, sortieAudio);
                    }
                }
            }
        }

        public void IVK_ChatAudioHP(ushort idJr, byte[] compressed)
        {
            if(idJr>0 && DicoJoueurs != null && compressed != null)
            {
                Joueur jr;
                if (DicoJoueurs.TryGetValue(idJr, out jr) && jr != null)
                {
                    try
                    {
                        byte[] decoded = AudioChatCodec.Decode(compressed, 1 + 2, compressed.Length - 3);
                        jr.ChatAudioHP(decoded);
                    }
                    catch { }
                }
                /*else if(joueur != null)
                {
                    try
                    {
                        byte[] decoded = AudioChatCodec.Decode(compressed, 1 + 2, compressed.Length - 3);
                        joueur.ChatAudioHP(decoded);
                    }
                    catch { }
                }*/
            }
        }

        /*private void ActivierChatAudio(INetworkChatCodec codec)
        {
            AudioChatCodec = codec;
            ActivierChatAudioMicro(codec);
            ActivierChatAudioHP(codec);
        }*/

        int hyster = 0;
        void OnAudioCaptured(object sender, WaveInEventArgs e)
        {
            if (EstDansSession)
            {
                if (AudioChatCodec != null && joueur != null && joueur.IdSessionJoueur > 0)
                {
                    byte[] bff = e.Buffer;
                    int len = e.BytesRecorded;
                    short hmax = 0;
                    for (int i = 0; i < len; i += 2)
                    {
                        short h = BitConverter.ToInt16(bff, i);
                        if (h < 0)
                        {
                            if (h == short.MinValue)
                            {
                                hmax = short.MaxValue;
                                break;
                            }
                            else if (-h > hmax) hmax = (short)-h;
                        }
                        else if (h > hmax) hmax = h;
                    }
                    if (hmax > 2000)
                    {
                        if (hmax > 6000) hyster = 10;
                        if (hyster > 0)
                        {
                            --hyster;
                            byte[] encoded = AudioChatCodec.Encode(bff, 0, e.BytesRecorded);
                            encoded[0] = (byte)ClientThread.ServeurCodeCommande.ChatAudio;
                            encoded[1] = (byte)((joueur.IdSessionJoueur >> 0) & 0xFF);
                            encoded[2] = (byte)((joueur.IdSessionJoueur >> 8) & 0xFF);
                            connexion.EnqueueCommande(encoded);
                        }
                    }
                    else if (hyster > 0) --hyster;
                }
            }
            else
            {
                FermerChatAudio();
                AudioChatCodec = null;
            }
        }
        #endregion ChatAudio

        /// <summary>
        /// Niveau de zoom et de dézoom 1.2
        ///-12	0,1121566548
        ///-11	0,1345879857
        ///-10	0,1615055829
        ///-09	0,1938066995
        ///-08	0,2325680394
        ///-07	0,2790816472
        ///-06	0,3348979767
        ///-05	0,401877572
        ///-04	0,4822530864
        ///-03	0,5787037037
        ///-02	0,6944444444
        ///-01	0,8333333333
        ///+00	1
        ///+01	1,2
        ///+02	1,44
        ///+03	1,728
        ///+04	2,0736
        ///+05	2,48832
        ///+06	2,985984
        ///+07	3,5831808
        ///+08	4,29981696
        ///+09	5,159780352
        ///+10	6,1917364224
        ///+11	7,4300837069
        ///+12	8,9161004483
        /// </summary>
        private void RécupérerTextureBoard()
        {
            string fName = Directory.GetFiles("./", "texture_board*.jpg").FirstOrDefault();
            if (fName != null)
            {
                Image imgTexBoard = Image.FromFile(fName);
                if (imgTexBoard != null)
                {
                    int idx = fName.LastIndexOf('_') + 1;
                    int ech;
                    if (Char.IsDigit(fName[idx]))
                    {
                        int nbFin;
                        for (nbFin = idx; Char.IsDigit(fName[nbFin]); ++nbFin) ;
                        ech = int.Parse(fName.Substring(idx, nbFin - idx));
                    }
                    else ech = 100;

                    textureDuBoard = new Image[5];
                    textureDuBoard[0] = imgTexBoard;
                    float div = 1;
                    for (int i = 1; i < 5; ++i)
                    {
                        div *= 1.6f;
                        int nw = (int)((imgTexBoard.Width / div) + 0.5f);
                        int nh = (int)((imgTexBoard.Height / div) + 0.5f);
                        Bitmap btmap = new Bitmap(nw, nh);
                        Graphics g = Graphics.FromImage(btmap);
                        g.DrawImage(imgTexBoard, 0, 0, nw, nh);
                        textureDuBoard[i] = BibliothèqueImage.Rencoder(btmap, System.Drawing.Imaging.ImageFormat.Jpeg.Guid, 100L);
                    }

                    int mind = Math.Min(imgTexBoard.Width, imgTexBoard.Height);
                    float textureEchel = ((float)ech / (float)mind)*5.0f;
                    textureTaille = new Size((int)(imgTexBoard.Width * textureEchel + 0.5f), (int)(imgTexBoard.Height * textureEchel + 0.5f));
                }
            }
            else textureDuBoard = null;
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
                        if(connexion != null && connexion.NomSession != null && joueur != null && joueur.ADroits(Joueur.EDroits.Importation, connexion.DroitsSession))
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
            this.ActiveControl = null;
            /*this.Activate();*/
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
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            RectangleF vue = new RectangleF(GV.GC.P.X, GV.GC.P.Y, this.Width / GV.GC.E, this.Height / GV.GC.E);
            g.TranslateTransform(GV.DimentionD2.X, GV.DimentionD2.Y);
            g.RotateTransform(GV.GC.A);
            g.ScaleTransform(GV.GC.E, GV.GC.E);
            g.TranslateTransform(-GV.GC.P.X, -GV.GC.P.Y);

            if (textureDuBoard != null)
            {
                float f = (vue.X + vue.Width) / textureTaille.Width;
                int basX = (int)f;
                if (f - basX > 0.0000001f) ++basX;
                //++basX;
                f = (vue.Y + vue.Height) / textureTaille.Height;
                int basY = (int)f;
                if (f - basY > 0.0000001f) ++basY;
                //++basY;

                f = (vue.X - vue.Width) / textureTaille.Width;
                int hautX = (int)f;
                if (f - hautX < -0.0000001f) --hautX;
                //--hautX;
                f = (vue.Y - vue.Height) / textureTaille.Height;
                int hautY = (int)f;
                if (f - hautY < -0.0000001f) --hautY;

                int select;
                if (GV.GC.E >= 1.0f) select = (int)((6.0f - GV.GC.E) / 3.0f); // (6 - e)/3
                else select = (int)(4 - (6.0f - (1.0f / GV.GC.E)) / 2.5f); //4-(6-(1/e))/3
                if (select < 0) select = 0; else if (select >= textureDuBoard.Length) select = textureDuBoard.Length - 1;
                Image hitmap = textureDuBoard[select];

                int dlt = 6 + (int)(0.6f / GV.GC.E);
                for (int y = hautY; y < basY; ++y)
                {
                    for (int x = hautX; x < basX; ++x)
                    {
                        g.DrawImage(hitmap, x * textureTaille.Width - dlt, y * textureTaille.Height - dlt, textureTaille.Width + 2 * dlt, textureTaille.Height + 2 * dlt);
                    }
                }
            }
            //root.GC.A = 45.0f;
            //root.GC.P.X = -1000.0f;
            //root.GC.P.Y = 0.0f;
            root.Dessiner(vue, GV.GC.A, g, new PointF(0.0f, 0.0f));
            //root.GC.P.X = 0.0f;
            //root.GC.P.Y = 0.0f;
            //root.GC.A = 0.0f;

            //if (SelectedElm_LEFT != null /*&& ElmOnBoard_LEFT == this*/)
            //    SelectedElm_LEFT.Dessiner(vue, GV.GC.A, g, SelectedP_LEFT);
            if (joueur != null) joueur.Dessiner(vue, GV.GC.A, g);
        }

        private void Board_MouseDown(object sender, MouseEventArgs e)
        {
            this.ActiveControl = null;
            /*this.Activate();*/
            MouseBt |= e.Button;
            if (e.Button.HasFlag(MouseButtons.Left) || e.Button.HasFlag(MouseButtons.Right))
            {
                //PointF pt = new PointF((e.Location.X / GC.E), (e.Location.Y / GC.E));
                PointF pt = GV.Projection(e.Location);
                if (e.Button.HasFlag(MouseButtons.Left) && (Ctrl_Down == true || !(joueur != null && joueur.AElementAttrapé)))
                {
                    //ElmOnBoard_LEFT = this;
                    ClickP_LEFT = e.Location;
                    SelectedP_LEFT = pt;
                    //joueur.P = pt;
                    //if (/*SelectedElm_RIGHT == null &&*/ Ctrl_Down == false && Shift_Down == false)
                    //{
                    //Element elm = root.MousePiocheAt(pt, GV.GC.A);
                    Element elm, conteneur;
                    if(Shift_Down) (elm, conteneur) = root.MousePickAvecContAt(pt, GV.GC.A, Element.EPickUpAction.Attraper);
                    else (elm, conteneur) = root.MousePickAvecContAt(pt, GV.GC.A, Element.EPickUpAction.Piocher);
                    if (elm != null)
                    {
                        if (Shift_Down) AttraperElément(conteneur, elm, pt, elm.GC.A);//Si shift est done alors on prent l'élément !
                        else if(elm.ElmType == EType.Figurine) PiocherElément(conteneur, elm, pt, elm.GC.A);// SelectedElm_LEFT = elm?.MousePioche();
                        else PiocherElément(conteneur, elm, pt, (360.0f - GV.GC.A) % 360.0f);// SelectedElm_LEFT = elm?.MousePioche();
                    }
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

            if (e.Button.HasFlag(MouseButtons.XButton1))
            {
                int oldA = (int)(GV.GC.A + 0.5f);
                int newA = oldA + 23;
                GV.GC.A = GeoCoord2D.AngleFromToAimant45(oldA, newA);
                Refresh();
            }
            else if (e.Button.HasFlag(MouseButtons.XButton2))
            {
                int oldA = (int)(GV.GC.A + 0.5f);
                int newA = oldA - 23;
                GV.GC.A = GeoCoord2D.AngleFromToAimant45(oldA, newA);
                Refresh();
            }
        }

        private void Board_MouseUp(object sender, MouseEventArgs e)
        {
            this.ActiveControl = null;
            /*this.Activate();*/
            MouseBt &= ~e.Button;
            if (e.Button.HasFlag(MouseButtons.Left) && Ctrl_Down == false)
            {
                if (joueur != null /*&& joueur.AElementAttrapé*/)
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
            /*if (e.Button.HasFlag(MouseButtons.Right))
            {
                if (SelectedElm_RIGHT != null)
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
                }

                //SelectedElm_RIGHT = null;
                //ElmOnBoard_RIGHT = null;
            }*/
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
            else if (e.Button.HasFlag(MouseButtons.Left) && lstIdAttrapeEnCours == null && (e.Button.HasFlag(MouseButtons.Right) == false /*|| SelectedElm_RIGHT != null*/) /*&& ElmOnBoard_LEFT == this*/)
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
                PointF pt = GV.Projection(e.Location);
                //PointF pt = new PointF((e.Location.X / GC.E), (e.Location.Y / GC.E));
                if (MouseBt.HasFlag(MouseButtons.Right) == false && (Shift_Down || Ctrl_Down))
                {
                    if (joueur != null && joueur.AElementAttrapé)
                    {
                        //joueur.P = GV.Projection(new Point(this.Left + e.X - /*ElmOnBoard_LEFT.*/Left, this.Top + e.Y - /*ElmOnBoard_LEFT.*/Top));
                        if (Shift_Down) TournnerElémentAttrapé(5.0f * delta);
                        else TournnerElémentAttrapé(45.0f * delta);
                    }
                    else
                    {
                        Element elm = root.MousePickAt(pt, GV.GC.A, (Shift_Down ? Element.EPickUpAction.Tourner : Element.EPickUpAction.Roulette));
                        if (elm != null && !elm.EstDansEtat(Element.EEtat.RotationFixe))
                        {
                            if (Shift_Down) TournnerElément(elm, 45 * delta);//.Tourner(e.Delta);
                            else RouletteElément(elm, 5 * delta); //.Roulette(e.Delta);
                        }
                    }
                    /*else
                    {
                        int oldA = (int)(GV.GC.A + 0.5f);
                        int newA = oldA + 22 * delta;
                        GV.GC.A = GeoCoord2D.AngleFromToAimant45(oldA, newA);
                    }*/
                }
                //else if (elm != null) RouletteElément(elm, delta); //.Roulette(e.Delta);
                else
                {
                    if (e.Delta < 0)
                    {
                        if(GV.GC.E > 0.13) GV.GC.E /= 1.2f;
                    }
                    else if (e.Delta > 0)
                    {
                        if (GV.GC.E < 6.19f) GV.GC.E *= 1.2f;
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
                ConnectForm cf = new ConnectForm(this, p, PARAM_SERVEUR, PARAM_PORT, PARAM_LOGIN, PARAM_MOTDEPASSE, connexion);
                if(cf.ShowDialog(this) == DialogResult.Yes) connexion = cf.connexion;
            }
            else
            {
                ConnectForm cf = new ConnectForm(this, p, PARAM_SERVEUR, PARAM_PORT, PARAM_LOGIN, PARAM_MOTDEPASSE, connexion);
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
            this.ActiveControl = null;
            /*this.Activate();*/
            if (e.Button.HasFlag(MouseButtons.Left) && Ctrl_Down == false && Shift_Down == false)
            {
                if (joueur != null && joueur.AElementAttrapé)
                {
                    PointF dX = new PointF(e.X - ClickP_LEFT.X, e.Y - ClickP_LEFT.Y);
                    float dCarrée = dX.X * dX.X + dX.Y * dX.Y;
                    if (dCarrée < CLICK_SEUIL)
                    {
                        joueur.ElémentAttrapés.ForEach(elm => { if (elm is Dés) Mélanger(elm); else RetournerElément(elm); });
                        this.Refresh();
                    }
                }
                else
                {
                    PointF pt = GV.Projection(e.Location);
                    Element elm = root.MousePickAt(pt, GV.GC.A);
                    if (elm != null)
                    {
                        if (elm is Dés) Mélanger(elm);
                        else RetournerElément(elm);//elm.Retourner();
                        this.Refresh();
                    }
                }
            }
        }

        private void Board_MouseClick(object sender, MouseEventArgs e)
        {
            this.ActiveControl = null;
            /*this.Activate();*/
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
                        if(elm.EstDansEtat(Element.EEtat.RotationFixe) == false)
                        {
                            cm.MenuItems.Add(0, new MenuItem("Rotation", new MenuItem[]
                            {
                                new MenuItem("-135", new EventHandler((o,ev) => { ChangerAngle(elm, 360.0f-135.0f); })),
                                new MenuItem(" -90", new EventHandler((o,ev) => { ChangerAngle(elm, 360.0f-90.0f); })),
                                new MenuItem(" -45", new EventHandler((o,ev) => { ChangerAngle(elm, 360.0f-45.0f); })),
                                new MenuItem("   0", new EventHandler((o,ev) => { ChangerAngle(elm, 0.0f); })),
                                new MenuItem(" +45", new EventHandler((o,ev) => { ChangerAngle(elm, 45.0f); })),
                                new MenuItem(" +90", new EventHandler((o,ev) => { ChangerAngle(elm, 90.0f); })),
                                new MenuItem("+135", new EventHandler((o,ev) => { ChangerAngle(elm, 135); })),
                                new MenuItem("+180", new EventHandler((o,ev) => { ChangerAngle(elm, 180.0f); }))
                            }));
                        }

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
                        //ctxm.MenuItems.Add("-");
                        //if (connexion == null) ctxm.MenuItems.Add("Connecter", (o, eArg) => Connecter(new Point(this.Left + e.X, this.Top + e.Y)));
                        //else
                        //{
                        //    List<MenuItem> lstMenu = new List<MenuItem>();
                        //    if (connexion != null && connexion.EstIdentifié)
                        //    {
                        //        lstMenu.Add(new MenuItem("Créer une session", new EventHandler((o, eArg) => { new CréerSession(connexion, new Point(this.Left + e.X, this.Top + e.Y)).ShowDialog(this); }))); //CréerSession cs = new CréerSession(connection); if(cs.ShowDialog() == DialogResult.Yes) { NomSessionCréée = cs.NomSession; SessionCrééeHashPwd = cs.SessionHashPwd }
                        //        lstMenu.Add(new MenuItem("Sessions", new EventHandler((o, eArg) => { jSession = new GérerSessions(connexion, new Point(this.Left + e.X, this.Top + e.Y)); if (jSession.ShowDialog() == DialogResult.Yes) ; jSession = null; })));
                        //        lstMenu.Add(new MenuItem("-"));
                        //    }
                        //    lstMenu.Add(new MenuItem("Déconnecter", new EventHandler((o, eArg) => { Connecter(new Point(this.Left + e.X, this.Top + e.Y)); })));
                        //    ctxm.MenuItems.Add("Connexion", lstMenu.ToArray());
                        //}
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
                string message;
                if (connexion != null && connexion.AEnvoiEnCours) message = "Des envois sont en cours, vos tranfères seront intérrompus...\r\n";
                else message = "";
                message += "Êtes-vous sûr de vouloir vous déconnecter ?";
                if (MessageBox.Show(message, "Déconnecter ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
            if (this.ActiveControl == null || !(this.ActiveControl is RichTextBox))
            {
                if (e.KeyCode == Keys.Left)
                {
                    int oldA = (int)(GV.GC.A + 0.5f);
                    int newA = oldA - 23;
                    GV.GC.A = GeoCoord2D.AngleFromToAimant45(oldA, newA);
                    Refresh();

                    ///*this.Activate();*/
                    //this.ActiveControl = lvConsGJrs;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Right)
                {
                    int oldA = (int)(GV.GC.A + 0.5f);
                    int newA = oldA + 23;
                    GV.GC.A = GeoCoord2D.AngleFromToAimant45(oldA, newA);
                    Refresh();

                    ///*this.Activate();*/
                    //this.ActiveControl = lvConsGJrs;
                    e.Handled = true;
                }
            }
            /*if (e.KeyCode == Keys.F2)
            {
                if (tabControle.Visible)
                {
                    if (tabControle.SelectedIndex == 0) tabControle.Visible = false;
                    else tabControle.SelectedIndex = 0;
                }
                else tabControle.Visible = true;
            }
            else if (e.KeyCode == Keys.F3)
            {
                if (tabControle.Visible)
                {
                    if (tabControle.SelectedIndex == 1) tabControle.Visible = false;
                    else tabControle.SelectedIndex = 1;
                }
                else tabControle.Visible = true;
            }
            else if (e.KeyCode == Keys.F4)
            {
                if (tabControle.Visible)
                {
                    if (tabControle.SelectedIndex == 2) tabControle.Visible = false;
                    else tabControle.SelectedIndex = 2;
                }
                else tabControle.Visible = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                tabControle.Visible = !tabControle.Visible;
            }*/
        }

        private void Board_KeyUp(object sender, KeyEventArgs e)
        {
            Ctrl_Down = e.Control;
            Shift_Down = e.Shift;
        }

        private void Board_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.ActiveControl == null || !(this.ActiveControl is RichTextBox))
            {
                if (e.KeyChar == ' ' && joueur != null && joueur.AElementAttrapé)
                {
                    joueur.RetournerAttrapées();
                    Refresh();
                }
                else if (e.KeyChar == 'r' || e.KeyChar == 'R')
                {
                    GV.GC.P = new PointF();
                    Refresh();
                }
            }
        }

        private void Board_Resize(object sender, EventArgs e)
        {
            GV.Dimention = new PointF(this.Width, this.Height);
            /*tabControle.Left = this.Width - tabControle.Width;
            tabControle.Top = 0;*/
            //tabControle.Height = this.Height-50;
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
            FermerChatAudio();
            if (root != null) root.Nétoyer(); else root = new Groupe();
            if (joueur != null) joueur.Remettre(); else joueur = new Joueur();
            DicoJoueurs = null;
            lvTableJr.Items.Clear();
            rTxtMessageSession.Clear();
            MouseBt = MouseButtons.None;
            lstIdAttrapeEnCours = null;
            bibliothèqueImage.Netoyer();
            bibliothèqueModel.Netoyer();
            this.Refresh();
        }

        private bool EstIdentifié { get => connexion != null && connexion.EstIdentifié; }
        private bool EstDansSession { get => connexion != null && joueur != null && connexion.EstIdentifié && !String.IsNullOrWhiteSpace(connexion.NomSession); }

        #region Méthodes d'intéraction avec le board
        private void ChangerDroitsSession(Joueur.EDroits droitsSess)
        {
            if(EstDansSession)
            {
                connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.MajDroitsSession, droitsSess);
            }
        }

        private void AjouterDroitsJoueur(List<Joueur> ljr, Joueur.EDroits droitsJr)
        {
            if (EstDansSession && ljr != null && ljr.Any())
            {
                foreach(Joueur jr in ljr)
                    if(jr.IdSessionJoueur > 0)
                        connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.MajDroitsJoueur, jr.IdSessionJoueur, jr.Droits | droitsJr);
            }
        }

        private void RetirerDroitsJoueur(List<Joueur> ljr, Joueur.EDroits droitsJr)
        {
            if (EstDansSession && ljr != null && ljr.Any())
            {
                foreach (Joueur jr in ljr)
                    if (jr.IdSessionJoueur > 0)
                        connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.MajDroitsJoueur, jr.IdSessionJoueur, jr.Droits & ~droitsJr);
            }
        }

        private void ChangerDroitsJoueur(Joueur jr, Joueur.EDroits droitsJr)
        {
            if (EstDansSession && jr != null && jr.IdSessionJoueur > 0)
            {
                connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.MajDroitsJoueur, jr.IdSessionJoueur, droitsJr);
            }
        }
        private void PasserMainJoueur(Joueur jr)
        {
            if (EstDansSession && jr != null && jr.IdSessionJoueur > 0)
            {
                connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.PasserMainJoueur, jr.IdSessionJoueur);
            }
        }
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

        public void ChangerAngle(Element elm, float ang)
        {
            if (elm != null)
            {
                if (EstDansSession && elm.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.ChangerAngle, elm.IdentifiantRéseau, ang);
                else
                {
                    elm.GC.A = ang;
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
                else
                {
                    elm.Roulette(delta);
                    Refresh();
                }
            }
        }

        private void TournnerElémentAttrapé(float delta)
        {
            if(joueur != null && joueur.AElementAttrapé)
            {
                if (EstDansSession && joueur.IdSessionJoueur > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.TournerElémentAttrapé, joueur.P, delta);
                else
                {
                    joueur.TournerElémentAttrapé(delta);
                    Refresh();
                }
            }
        }

        private void TournnerElément(Element elm, int delta)
        {
            if (elm != null)
            {
                if (EstDansSession && elm.IdentifiantRéseau > 0)
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.TournerElément, elm.IdentifiantRéseau, delta);
                else
                {
                    elm.Tourner(delta);
                    Refresh();
                }
            }
        }

        public void RangerVersParent(Element parent)
        {
            if (parent != null)
            {
                if (EstDansSession && parent.IdentifiantRéseau > 0)
                    ;//connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.RangerVersParent, parent.IdentifiantRéseau);
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
                    else if (elm is Dés)
                    {
                        (elm as Dés).Mélanger();
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
                    ;// connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.DéfausserElement, relem.IdentifiantRéseau);
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
                    ;//connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.ReMettreDansPioche, défss.IdentifiantRéseau);
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

        public void AttraperElément(Element elmSource, Element elmCible, PointF pt, float angle)
        {
            if (elmSource == null) elmSource = root;

            if (EstDansSession)
            {
                if (elmCible != null)
                {
                    if (lstIdAttrapeEnCours == null) lstIdAttrapeEnCours = new List<int>();
                    lstIdAttrapeEnCours.Add(elmCible.IdentifiantRéseau);
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.AttraperElement, pt, angle, elmSource.IdentifiantRéseau, elmCible.IdentifiantRéseau);
                }
            }
            else
            {
                if (elmCible != null)
                {
                    elmCible = elmSource.DétacherElement(elmCible);
                    if(elmCible != null)
                    {
                        if (elmCible.ElmType != EType.Figurine /*&& elmCible.GC.A != angle*/)
                        {
                            /*float difAng = angle - elmCible.GC.A;
                            double cosa = Math.Cos((difAng * Math.PI) / 180.0);
                            double sina = Math.Sin((difAng * Math.PI) / 180.0);
                            PointF dltp = new PointF(pt.X - elmCible.GC.P.X, pt.Y - elmCible.GC.P.Y);
                            PointF rdltp = new PointF(
                                    (float)(dltp.X * cosa - dltp.Y * sina),
                                    (float)(dltp.X * sina + dltp.Y * cosa)
                                );
                            elmCible.GC.P.X += dltp.X - rdltp.X;
                            elmCible.GC.P.Y += dltp.Y - rdltp.Y;
                            elmCible.GC.A = angle;*/
                            elmCible.GC.ChangerAngleSuivantPoint(angle, pt);
                        }
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


        public void PiocherElément(Element elmSource, Element elmCible, PointF pt, float angle)
        {
            if (elmCible != null)
            {
                int index = elmCible.GetPiocheIndex();
                if (index == int.MaxValue) AttraperElément(elmSource, elmCible, pt, angle);
                else
                {
                    if (elmSource == null) elmSource = root;
                    if (EstDansSession)
                    {
                        if (lstIdAttrapeEnCours == null) lstIdAttrapeEnCours = new List<int>();
                        lstIdAttrapeEnCours.Add(elmCible.IdentifiantRéseau);
                        connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.PiocherElement, pt, angle, elmCible.IdentifiantRéseau, index);
                    }
                    else
                    {
                        elmCible = elmCible.MousePioche(index);
                        if (elmCible != null)
                        {
                            /*elmCible.GC.P.X += elmSource.GC.P.X;
                            elmCible.GC.P.Y += elmSource.GC.P.Y;*/
                            elmCible.GC.ProjectionInv(elmSource.GC);
                            if (elmCible.ElmType != EType.Figurine /*&& elmCible.GC.A != angle*/)
                            {
                                /*float difAng = angle - elmCible.GC.A;
                                double cosa = Math.Cos((difAng * Math.PI) / 180.0);
                                double sina = Math.Sin((difAng * Math.PI) / 180.0);
                                PointF dltp = new PointF(pt .X - elmCible.GC.P.X, pt.Y - elmCible.GC.P.Y);
                                PointF rdltp = new PointF(
                                        (float)(dltp.X * cosa - dltp.Y * sina),
                                        (float)(dltp.X * sina + dltp.Y * cosa)
                                    );
                                elmCible.GC.P.X += dltp.X - rdltp.X;
                                elmCible.GC.P.Y += dltp.Y - rdltp.Y;
                                elmCible.GC.A = angle;*/
                                elmCible.GC.ChangerAngleSuivantPoint(angle, pt);
                            }
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
            lstIdAttrapeEnCours = null;
            if (joueur != null /*&& joueur.AElementAttrapé*/)
            {
                if (elmDestinataire == null) elmDestinataire = root;
                if (EstDansSession)
                {
                    connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.LacherElement, pt, elmDestinataire.IdentifiantRéseau);
                }
                else
                {
                    joueur.P = pt;
                    List<Element> aMélang = joueur.RécupérerAMélanger();
                    if (aMélang != null) aMélang.ForEach(elm => Mélanger(elm));
                    List<Element> lstElementLaché = joueur.ToutRécupérer();
                    if (lstElementLaché != null)
                        lstElementLaché.ForEach(elm =>
                        {
                            elm = elmDestinataire.ElementLaché(elm);
                            if (elm != null)
                            {
                                if (elm is Pile && !(elm is Défausse)) root.Suppression(elm);//destruction !
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

        public void IVK_SyncroGénéralJoueurs(List<string> gjoueurs)
        {
            lvConsGJrs.Items.Clear();
            lvGJrs.Items.Clear();
            if(gjoueurs != null)
                gjoueurs.ForEach(gj => { lvGJrs.Items.Add(gj, 0); lvConsGJrs.Items.Add(gj, 0); });
        }

        public void IVK_JoinSession(string session, ushort idJoueur, INetworkChatCodec codec)
        {
            Nétoyer();
            if (joueur == null) joueur = new Joueur();
            joueur.IdSessionJoueur = idJoueur;
            FermerChatAudio();
            IVK_MessageServeur(OutilsRéseau.EMessage.JoinSession, session);
            if (codec != null)
            {
                AudioChatCodec = codec;
                ActivierChatAudioMicro(codec);

                if (WaveOut.DeviceCount > 0)
                {
                    if (sortieAudio < 0 || WaveOut.DeviceCount <= sortieAudio) sortieAudio = 0;
                }
                else sortieAudio = -1;
                //joueur.ActivierChatAudioHP(AudioChatCodec, sortieAudio);
            }
        }

        public void IVK_MessageServeur(OutilsRéseau.EMessage type, string message)
        {
            //string caption;
            //MessageBoxIcon ico;
            Color couleur;
            string session = null;

            switch(type)
            {
                case OutilsRéseau.EMessage.Information:
                    //caption = "Information du serveur";
                    //ico = MessageBoxIcon.Information;
                    couleur = Color.Blue;
                    break;
                case OutilsRéseau.EMessage.Attention:
                    //caption = "Avertissement du serveur";
                    //ico = MessageBoxIcon.Warning;
                    couleur = Color.Orange;
                    break;
                case OutilsRéseau.EMessage.Erreur:
                    //caption = "Erreur du serveur";
                    //ico = MessageBoxIcon.Error;
                    couleur = Color.Red;
                    break;
                case OutilsRéseau.EMessage.IdentifiantRefusée:
                    if (MessageBox.Show("Votre identifiant est refusé.\r\nL'identifiant automatique \"" + message + "\" vous est proposé.\r\nVoulez-vous poursuivre ?", "Identifiant refusé", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        joueur = new Joueur(0, message);
                        this.Text = "Board connecté";
                        message = "Connexion réussie !";
                        //caption = "Connexion";
                        //ico = MessageBoxIcon.Information;
                        couleur = Color.Blue;
                        type = OutilsRéseau.EMessage.ConnexionRéussie;
                        session = "";
                        connexion.ActualiserLstGJoueur();
                        tabControle.SelectedIndex = 1;
                    }
                    else
                    {
                        Déconnecter();
                        return;
                    }
                    break;
                case OutilsRéseau.EMessage.ConnexionRéussie:
                    joueur = new Joueur(0, message);
                    this.Text = "Board connecté";
                    message = "Connexion réussie !";
                    //caption = "Connexion";
                    //ico = MessageBoxIcon.Information;
                    couleur = Color.Blue;
                    session = "";
                    connexion.ActualiserLstGJoueur();
                    tabControle.SelectedIndex = 1;
                    break;
                case OutilsRéseau.EMessage.CréaSession:
                    //caption = "Création de session réussie";
                    //ico = MessageBoxIcon.Information;
                    couleur = Color.Blue;
                    session = message;
                    message = "Votre session \"" + message + "\" a bien été crée";
                    break;
                case OutilsRéseau.EMessage.RefuSession:
                    //caption = "Refus de création de session";
                    //ico = MessageBoxIcon.Error;
                    couleur = Color.Red;
                    message = "Création de votre session \"" + message + "\" refusée.";
                    break;
                case OutilsRéseau.EMessage.JoinSession:
                    //caption = "Entrée en session";
                    //ico = MessageBoxIcon.Information;
                    couleur = Color.Blue;
                    this.Text = "Board (Session en cours : " + message + ")";
                    message = "Vous rejoignez la session \"" + message + "\".";
                    tabControle.SelectedIndex = 2;
                    break;
                case OutilsRéseau.EMessage.QuitSession:
                    //caption = "Sortie de session";
                    this.Text = "Board connecté";
                    //ico = MessageBoxIcon.Information;
                    couleur = Color.Blue;
                    Nétoyer();
                    if (joueur != null) joueur.Remettre(); else joueur = new Joueur();
                    session = message;
                    message = "Vous quitez la session \"" + session + "\".";
                    tabControle.SelectedIndex = 1;
                    break;
                case OutilsRéseau.EMessage.Déconnexion:
                    this.Text = "Board";
                    //caption = "Perte de connexion";
                    //ico = MessageBoxIcon.Error;
                    couleur = Color.Orange;
                    Nétoyer();
                    lvConsGJrs.Items.Clear();
                    lvGJrs.Items.Clear();
                    rTxtMessageGénéral.Clear();
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
                    //caption = "Message du serveur";
                    //ico = MessageBoxIcon.Asterisk;
                    couleur = Color.Black;
                    break;
            }
            if(message!=null)
            {
                //MessageBox.Show(message, caption, MessageBoxButtons.OK, ico);
                rTxtMessageConsole.SelectionColor = couleur;
                rTxtMessageConsole.AppendText(message + "\r");
                VérifierLongueurrTxtMessageConsole();
            }
            if (session != null && connexion!=null && !connexion.EstDansSession)
            {
                jSession = new GérerSessions(connexion, new Point(this.Left + this.Width / 2, this.Top + this.Height / 2), session);
                if (jSession.ShowDialog() == DialogResult.Yes);
                jSession = null;
            }
        }

        public void IVK_SynchroniserSession(Groupe grp, ushort IdSessionJoueur, Dictionary<ushort, Joueur> jrs/*, List<ElementRéseau> élémentRésiduel*/)
        {
            if (grp != null && EstDansSession)
            {
                //SelectedElm_RIGHT = null;
                root = grp;
                lvTableJr.Clear();
                //if(joueur != null) lvTableJr.Items.Add(joueur.Nom, 0);
                if (jrs != null && jrs.Any())
                {
                    Joueur jr;
                    if (jrs.TryGetValue(IdSessionJoueur, out jr))
                    {
                        joueur = jr;
                        //joueur.ActivierChatAudioHP(AudioChatCodec, sortieAudio);
                    }
                    else
                    {
                        if (joueur == null) joueur = new Joueur(IdSessionJoueur, connexion.ObtenirIdentifiant());
                        else joueur.IdSessionJoueur = IdSessionJoueur;
                        jr = joueur;
                    }

                    IOrderedEnumerable<KeyValuePair<ushort, Joueur>> lstIdJr = jrs.AsEnumerable().OrderBy(j => j.Key);
                    foreach (var kv in lstIdJr)
                    {
                        ListViewItem lvi;
                        if (kv.Value == jr) lvi = new ListViewItem(kv.Value.Nom, (kv.Value.EstMaitre ? 3 : 2));
                        else lvi = new ListViewItem(kv.Value.Nom, (kv.Value.EstMaitre ? 1 : 0));
                        lvi.Tag = kv.Value.IdSessionJoueur;
                        lvTableJr.Items.Add(lvi);
                    }
                    jrs.Remove(jr.IdSessionJoueur);
                    FermerChatAudioHP();
                    DicoJoueurs = jrs;
                    ActivierChatAudioHP(AudioChatCodec);
                }
                else DicoJoueurs = null;
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

        public void IVK_ArrivéeJoueur(Joueur jr)
        {
            if (jr != null && EstDansSession)
            {
                if (joueur != null && joueur.IdSessionJoueur == jr.IdSessionJoueur) ;
                else
                {
                    if (DicoJoueurs == null) DicoJoueurs = new Dictionary<ushort, Joueur>();
                    if (DicoJoueurs.ContainsKey(jr.IdSessionJoueur))
                        connexion.EnqueueCommande(ClientThread.ServeurCodeCommande.DemandeSynchro);
                    else
                    {
                        jr.ActivierChatAudioHP(AudioChatCodec, sortieAudio);
                        DicoJoueurs.Add(jr.IdSessionJoueur, jr);
                        ListViewItem lvi = new ListViewItem(jr.Nom, (jr.EstMaitre ? 1 : 0));
                        lvi.Tag = jr.IdSessionJoueur;
                        lvTableJr.Items.Add(lvi);
                    }
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
                    for (int i = 0; i < lvTableJr.Items.Count; ++i)
                    {
                        if (lvTableJr.Items[i].Text == jr.Nom)
                        {
                            lvTableJr.Items.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        public void IVK_MessageGénéral(string message)
        {
            if (EstIdentifié && message.Length <= (OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 3 + OutilsRéseau.TAILLE_MAX_MESSAGE) && OutilsRéseau.EstMessageSecurisée(message))
            {
                rTxtMessageGénéral.SelectionColor = Color.Black;
                rTxtMessageGénéral.AppendText(message + "\r");
                VérifierLongueurrTxtMessageGénéral();
            }
        }

        public void IVK_MessageSession(string message)
        {
            if (EstDansSession && message.Length <= (OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 3 + OutilsRéseau.TAILLE_MAX_MESSAGE) && OutilsRéseau.EstMessageSecurisée(message))
            {
                rTxtMessageGénéral.SelectionColor = Color.Black;
                rTxtMessageSession.AppendText(message + "\r");
                VérifierLongueurrTxtMessageSession();
            }
        }

        /*public void IVK_MajDroitsSession(string nomSession, Joueur.EDroits droits)
        {
            if (EstDansSession && nomSession != null && nomSession == connexion.NomSession)
            {
            }
        }*/

        public void IVK_MajDroitsJoueur(ushort idJr, Joueur.EDroits droits)
        {
            if (EstDansSession && idJr > 0)
            {
                if (joueur != null && joueur.IdSessionJoueur == idJr)
                    joueur.Droits = droits;
                else if(DicoJoueurs != null)
                {
                    Joueur jr;
                    if (DicoJoueurs.TryGetValue(idJr, out jr))
                        jr.Droits = droits;
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

        public void IVK_RéceptionImage(byte qualité, ushort coin, uint alphaCoul, Image img)
        {
            if (img !=null && EstDansSession)
            {
                Guid gformat = img.RawFormat.Guid;
                img = BibliothèqueImage.AppliquerTransparence(img as Bitmap, gformat, coin, ref alphaCoul);

                if (bibliothèqueImage.NouvelleVersion(img, gformat, qualité, coin, alphaCoul))
                {
                    root.MettreAJour(img);
                    MajImageJoueur(img);
                    //if (SelectedElm_LEFT != null) SelectedElm_LEFT.MettreAJour(img);
                    //if (SelectedElm_RIGHT != null) SelectedElm_RIGHT.MettreAJour(img);
                    bibliothèqueModel.MettreAJour(img);
                    if (!connexion.AReceptionEnCours) this.Refresh();
                }
            }
        }

        public void IVK_RéceptionModel(Model2_5D mld)
        {
            if (mld != null && EstDansSession)
            {
                if(bibliothèqueModel.NouvelleVersion(mld) && !connexion.AReceptionEnCours)
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

        public void IVK_ChangerAngle(int idElm, float ang)
        {
            if (EstDansSession)
            {
                Element elm = TrouverElementRéseau(idElm);
                if (elm != null)
                {
                    elm.GC.A = ang;
                    Refresh();
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
                        if (lstIdAttrapeEnCours == null || !lstIdAttrapeEnCours.Contains(elmAtrp.IdentifiantRéseau)) // on attrape autre chose ou alors on a déjà relaché...
                        {
                            Element elmRescp = root.MousePickAt(pj, GV.GC.A);
                            if (elmRescp == null) elmRescp = root;
                            LacherElément(elmRescp, pj);
                        }
                        else
                        {
                            lstIdAttrapeEnCours.Remove(elmAtrp.IdentifiantRéseau);
                            if (lstIdAttrapeEnCours.Count == 0)
                                lstIdAttrapeEnCours = null;
                        }
                    }
                    jr.P = pj;
                    jr.DonnerElément(elmAtrp);
                    this.Refresh();
                }
                return jr;
            }
            else return null;
        }

        public void IVK_AttraperElement(ushort idJr, PointF pj, float angle, int idElmPSrc, int idElmAtrp) //J'attrape l'élément idElmAtrp depuis idElmPSrc
        {
            if (EstDansSession && idElmPSrc > 0 && idElmAtrp > 0)
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
                        DonnerElementAJoueur(idJr, pj, elmAtrp);
                    }
                }
            }
        }

        public void IVK_PiocherElement(ushort idJr, PointF pj, float angle, int idElmPSrc, int index, int idElm)
        {
            if (EstDansSession && idElmPSrc > 0)
            {
                Element elmPSrc = TrouverElementRéseau(idElmPSrc);
                if (elmPSrc != null)
                {
                    Element elmAtrp = elmPSrc.MousePioche(index);
                    if (elmAtrp != null)
                    {
                        elmAtrp.IdentifiantRéseau = idElm;
                        if (lstIdAttrapeEnCours != null && lstIdAttrapeEnCours.Contains(idElmPSrc))
                        {
                            lstIdAttrapeEnCours.Remove(idElmPSrc);
                            lstIdAttrapeEnCours.Add(idElm);
                        }
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
                //if (jr == joueur) lstIdAttrapeEnCours = null;
                jr.P = pj;
                List<Element> aMélang = jr.RécupérerAMélanger();
                if(aMélang != null) aMélang.ForEach(elm => Mélanger(elm));
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
                    /*if (idAttrapeEnCours != 0 && lelm.Any(e => e.IdentifiantRéseau == idAttrapeEnCours))
                        idAttrapeEnCours = 0;*/
                    lelm.ForEach(elmLach =>
                    {
                        //if(lstIdAttrapeEnCours != null) lstIdAttrapeEnCours.Remove(elmLach.IdentifiantRéseau);
                        elmLach = elmRecep.ElementLaché(elmLach);
                        if (elmRecep != null)
                        {
                            //if (idAttrapeEnCours == elmLach.IdentifiantRéseau) idAttrapeEnCours = 0;
                            if (elmLach is Pile && !(elmLach is Défausse)) root.Suppression(elmLach);//destruction !
                            else root.ElementLaché(elmLach);
                            //DonnerElementAJoueur(idJr, pj, elmRecep);
                        }
                    });
                    //if (lstIdAttrapeEnCours.Count == 0) lstIdAttrapeEnCours = null;
                    this.Refresh();
                }
            }
        }

        public void IVK_TournerElémentAttrapé(ushort idJr, PointF pj, float delta)
        {
            Joueur jr = TrouverJoueur(idJr);
            if (jr != null)
            {
                jr.P = pj;
                jr.TournerElémentAttrapé(delta);
                Refresh();
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
                else if (elm is Dés)
                {
                    (elm as Dés).Mélanger(new Random(seed));
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
                if (root != null) root.Nétoyer(); else root = new Groupe();
                bibliothèqueImage.Netoyer();
                bibliothèqueModel.Netoyer();
            }
        }
        #endregion

        private void AJouterMenuConnexion(ContextMenu ctxm)
        {
            if (connexion == null) ctxm.MenuItems.Add("Connecter", (o, eArg) => Connecter(new Point(Left + Width / 2, Top + Height / 2)));
            else
            {
                List<MenuItem> lstMenu = new List<MenuItem>();
                if (connexion != null && connexion.EstIdentifié)
                {
                    lstMenu.Add(new MenuItem("Créer une session", new EventHandler((o, eArg) => { new CréerSession(connexion, new Point(Left + Width / 2, Top + Height / 2)).ShowDialog(this); }))); //CréerSession cs = new CréerSession(connection); if(cs.ShowDialog() == DialogResult.Yes) { NomSessionCréée = cs.NomSession; SessionCrééeHashPwd = cs.SessionHashPwd }
                    lstMenu.Add(new MenuItem("Sessions", new EventHandler((o, eArg) => { jSession = new GérerSessions(connexion, new Point(Left + Width / 2, Top + Height / 2)); if (jSession.ShowDialog() == DialogResult.Yes) ; jSession = null; })));
                    lstMenu.Add(new MenuItem("-"));
                }
                lstMenu.Add(new MenuItem("Déconnecter", new EventHandler((o, eArg) => { Connecter(new Point(Left + Width / 2, Top + Height / 2)); })));
                ctxm.MenuItems.Add("Connexion", lstMenu.ToArray());
            }
        }

        private ContextMenu AfficherMenuContextuelConsole(Point location)
        {
            ContextMenu ctxm = new ContextMenu();
            AJouterMenuConnexion(ctxm);
            if (EstIdentifié)
            {
                ctxm.MenuItems.Add(new MenuItem("Actualiser", new EventHandler((o, ev) => { connexion.ActualiserLstGJoueur(); })));
            }
            return ctxm;
        }

        private void lvGJrs_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ContextMenu ctxm = AfficherMenuContextuelConsole(e.Location);
                ctxm.Show(this, new Point(tabControle.Left + tabGénérale.Left + lvGJrs.Left + e.X, tabControle.Top + tabGénérale.Top + lvGJrs.Top + e.Y));
            }
        }

        private void lvConsGJrs_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ContextMenu ctxm = AfficherMenuContextuelConsole(e.Location);
                ctxm.Show(this, new Point(tabControle.Left + tabConsole.Left + lvConsGJrs.Left + e.X, tabControle.Top + tabConsole.Top + lvConsGJrs.Top + e.Y));
            }
        }

        private void lvTableJr_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                ContextMenu ctxm = AfficherMenuContextuelConsole(e.Location);
                if (EstIdentifié && joueur != null)
                {
                    Joueur.EDroits droisess = connexion.DroitsSession;
                    if (lvTableJr.SelectedItems.Count > 0)
                    {
                        if (lvTableJr.SelectedItems.Count > 1 && joueur.Droits.HasFlag(Joueur.EDroits.GestDroits))
                        {
                            List<Joueur> ljrs = new List<Joueur>();
                            for (int i = 0; i < lvTableJr.SelectedItems.Count; ++i)
                            {
                                Joueur jr = RetrouverJoueur((ushort)lvTableJr.SelectedItems[i].Tag);
                                if (jr != null) ljrs.Add(jr);
                            }

                            List<MenuItem> lstMenuAjt = new List<MenuItem>();
                            List<MenuItem> lstMenuRet = new List<MenuItem>();
                            for (int i = 0; i < 16 && Enum.GetName(typeof(Joueur.EDroits), (Joueur.EDroits)(1 << i)) != null; ++i)
                            {
                                Joueur.EDroits dr = (Joueur.EDroits)(1 << i);
                                string droitNm = Enum.GetName(typeof(Joueur.EDroits), dr);
                                lstMenuAjt.Add(new MenuItem("(+)" + droitNm, new EventHandler((o, ev) => { AjouterDroitsJoueur(ljrs, dr); })));
                                lstMenuRet.Add(new MenuItem("(-)" + droitNm, new EventHandler((o, ev) => { RetirerDroitsJoueur(ljrs, dr); })));
                            }
                            ctxm.MenuItems.Add(new MenuItem("Ajouter", lstMenuAjt.ToArray()));
                            ctxm.MenuItems.Add(new MenuItem("Retirer", lstMenuRet.ToArray()));
                        }
                        if (lvTableJr.SelectedItems.Count == 1)
                        {
                            Joueur jr = RetrouverJoueur((ushort)lvTableJr.SelectedItems[0].Tag);

                            if (jr != null)
                            {
                                if (joueur.Droits.HasFlag(Joueur.EDroits.GestDroits) && DicoJoueurs != null)
                                {
                                    List<MenuItem> lstMenu = new List<MenuItem>();
                                    for (int i = 0; i < 16 && Enum.GetName(typeof(Joueur.EDroits), (Joueur.EDroits)(1 << i)) != null; ++i)
                                    {
                                        Joueur.EDroits dr = (Joueur.EDroits)(1 << i);
                                        string droitNm = Enum.GetName(typeof(Joueur.EDroits), dr);
                                        if (jr.Droits.HasFlag((Joueur.EDroits)(1 << i))) lstMenu.Add(new MenuItem("(-)" + droitNm, new EventHandler((o, ev) => { ChangerDroitsJoueur(jr, jr.Droits & ~dr); })));
                                        else lstMenu.Add(new MenuItem("(+)" + droitNm, new EventHandler((o, ev) => { ChangerDroitsJoueur(jr, jr.Droits | dr); })));
                                    }
                                    ctxm.MenuItems.Add(new MenuItem("Joueur", lstMenu.ToArray()));
                                }
                                if (jr != joueur && (droisess | joueur.Droits).HasFlag(Joueur.EDroits.PasseMain))
                                {
                                    Joueur.EDroits droitpass = joueur.Droits & Joueur.EDroits.Main;
                                    if (droitpass != Joueur.EDroits.Néant)
                                    {
                                        ctxm.MenuItems.Add(new MenuItem("Passer la main", new EventHandler((o, ev) => { PasserMainJoueur(jr); })));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (joueur.Droits.HasFlag(Joueur.EDroits.GestDroits))
                        {
                            List<MenuItem> lstMenu = new List<MenuItem>();
                            for (int i = 0; i < 16 && Enum.GetName(typeof(Joueur.EDroits), (Joueur.EDroits)(1 << i)) != null; ++i)
                            {
                                if ((Joueur.EDroits)(1 << i) != Joueur.EDroits.Bloqué)
                                {
                                    Joueur.EDroits dr = (Joueur.EDroits)(1 << i);
                                    string droitNm = Enum.GetName(typeof(Joueur.EDroits), dr);
                                    if (droisess.HasFlag((Joueur.EDroits)(1 << i))) lstMenu.Add(new MenuItem("(-)" + droitNm, new EventHandler((o, ev) => { ChangerDroitsSession(droisess & ~dr); })));
                                    else lstMenu.Add(new MenuItem("(+)" + droitNm, new EventHandler((o, ev) => { ChangerDroitsSession(droisess | dr); })));
                                }
                            }
                            ctxm.MenuItems.Add(new MenuItem("Session", lstMenu.ToArray()));
                        }
                    }
                }
                if(ctxm.MenuItems.Count > 0) ctxm.Show(this, new Point(tabControle.Left + tabSession.Left + lvTableJr.Left + e.X, tabControle.Top + tabSession.Top + lvTableJr.Top + e.Y));
            }
        }

        private void VérifierLongueurrTxtMessageConsole()
        {
            int txtLen = rTxtMessageConsole.TextLength;
            int limit = 1024 * 1024;
            if (txtLen > limit)
            {
                string[] mssgs = rTxtMessageConsole.Lines;
                int idx;
                for (idx = 0; idx < mssgs.Length && txtLen > limit; ++idx)
                {
                    txtLen -= mssgs[idx].Length;
                }
                Array.Copy(mssgs, idx, mssgs, 0, mssgs.Length - idx);
                Array.Resize(ref mssgs, mssgs.Length - idx);
                rTxtMessageConsole.Lines = mssgs;
            }
            rTxtMessageConsole.SelectionStart = rTxtMessageConsole.TextLength;
            rTxtMessageConsole.ScrollToCaret();
        }

        private void VérifierLongueurrTxtMessageGénéral()
        {
            int txtLen = rTxtMessageGénéral.TextLength;
            int limit = 1024 * 1024;
            if (txtLen > limit)
            {
                string[] mssgs = rTxtMessageGénéral.Lines;
                int idx;
                for (idx = 0; idx < mssgs.Length && txtLen > limit; ++idx)
                {
                    txtLen -= mssgs[idx].Length;
                }
                Array.Copy(mssgs, idx, mssgs, 0, mssgs.Length - idx);
                Array.Resize(ref mssgs, mssgs.Length - idx);
                rTxtMessageGénéral.Lines = mssgs;
            }
            rTxtMessageGénéral.SelectionStart = rTxtMessageGénéral.TextLength;
            rTxtMessageGénéral.ScrollToCaret();
        }

        private bool rTxtShiftDown = false;

        private void rTxtSaisieGénéral_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && !rTxtShiftDown)
            {
                if (EstIdentifié)
                {
                    string message = rTxtSaisieGénéral.Text.Trim();
                    while (message.EndsWith("\r"))
                    {
                        message = message.Substring(0, message.Length - 1).TrimEnd();
                    }
                    rTxtSaisieGénéral.Text = message;
                    if (rTxtSaisieGénéral.TextLength <= OutilsRéseau.TAILLE_MAX_MESSAGE)
                    {
                        if (OutilsRéseau.EstMessageSecurisée(rTxtSaisieGénéral.Text))
                        {
                            connexion.EnqueueCommande(ClientThread.BoardCodeCommande.MessageGénéral, rTxtSaisieGénéral.Text);
                            rTxtMessageGénéral.SelectionColor = Color.Black;
                            rTxtMessageGénéral.AppendText(connexion.ObtenirIdentifiant() + " : " + rTxtSaisieGénéral.Text + "\r");
                            rTxtSaisieGénéral.Clear();
                            VérifierLongueurrTxtMessageGénéral();
                        }
                        else
                        {
                            rTxtMessageGénéral.SelectionColor = Color.Red;
                            rTxtMessageGénéral.AppendText("Certains caractères de votre message sont interdis !\r");
                            VérifierLongueurrTxtMessageGénéral();
                        }
                    }
                    else
                    {
                        rTxtMessageGénéral.SelectionColor = Color.Red;
                        rTxtMessageGénéral.AppendText("Votre message est trop long !\r");
                        VérifierLongueurrTxtMessageGénéral();
                    }
                }
                else
                {
                    rTxtMessageGénéral.SelectionColor = Color.Red;
                    rTxtMessageGénéral.AppendText("Vous êtes déconnecté !\r");
                    VérifierLongueurrTxtMessageGénéral();
                }
                e.Handled = true;
            }
            else if (e.KeyChar == 27) { this.ActiveControl = null; /*this.Activate();*/}
        }

        private void VérifierLongueurrTxtMessageSession()
        {
            int txtLen = rTxtMessageSession.TextLength;
            int limit = 1024 * 1024;
            if (txtLen > limit)
            {
                string[] mssgs = rTxtMessageSession.Lines;
                int idx;
                for(idx = 0; idx < mssgs.Length && txtLen > limit; ++idx)
                {
                    txtLen -= mssgs[idx].Length;
                }
                Array.Copy(mssgs, idx, mssgs, 0, mssgs.Length - idx);
                Array.Resize(ref mssgs, mssgs.Length - idx);
                rTxtMessageSession.Lines = mssgs;
            }
            rTxtMessageSession.SelectionStart = rTxtMessageSession.TextLength;
            rTxtMessageSession.ScrollToCaret();
        }

        private void rTxtSaisieSession_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && !rTxtShiftDown)
            {
                if (EstDansSession)
                {
                    string message = rTxtSaisieSession.Text.Trim();
                    while (message.EndsWith("\r"))
                    {
                        message = message.Substring(0, message.Length - 1).TrimEnd();
                    }
                    rTxtSaisieSession.Text = message;
                    if (rTxtSaisieSession.TextLength <= OutilsRéseau.TAILLE_MAX_MESSAGE)
                    {
                        if (OutilsRéseau.EstMessageSecurisée(rTxtSaisieSession.Text))
                        {
                            connexion.EnqueueCommande(ClientThread.BoardCodeCommande.MessageSession, rTxtSaisieSession.Text);
                            rTxtMessageSession.SelectionColor = Color.Black;
                            rTxtMessageSession.AppendText(connexion.ObtenirIdentifiant() + " : " + rTxtSaisieSession.Text + "\r\n");
                            rTxtSaisieSession.Clear();
                            VérifierLongueurrTxtMessageSession();
                        }
                        else
                        {
                            rTxtMessageSession.SelectionColor = Color.Red;
                            rTxtMessageSession.AppendText("Certains caractères de votre message sont interdis !\r\n");
                            VérifierLongueurrTxtMessageSession();
                        }
                    }
                    else
                    {
                        rTxtMessageSession.SelectionColor = Color.Red;
                        rTxtMessageSession.AppendText("Votre message est trop long !\r\n");
                        VérifierLongueurrTxtMessageSession();
                    }
                }
                else
                {
                    rTxtMessageSession.SelectionColor = Color.Red;
                    rTxtMessageSession.AppendText("Vous n'êtes pas dans une session !\r\n");
                    VérifierLongueurrTxtMessageSession();

                }
                e.Handled = true;
            }
            else if (e.KeyChar == 27) { this.ActiveControl = null; /*this.Activate();*/}
        }

        private void rTxt_KeyDown(object sender, KeyEventArgs e)
        {
            rTxtShiftDown = e.Shift;
            Ctrl_Down = e.Control;
            Shift_Down = e.Shift;
        }

        private void rTxt_KeyUp(object sender, KeyEventArgs e)
        {
            rTxtShiftDown = e.Shift;
            Ctrl_Down = e.Control;
            Shift_Down = e.Shift;
        }
    }
}
