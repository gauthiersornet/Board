using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoardGameFabrique
{
    [Flags]
    public enum ETransformation : byte
    {
        Identity = 0,
        Retourner = (1 << 0),
        MiroirX = (1 << 1),
        MiroirY = (1 << 2)
    }

    public partial class BoardGameFabrique : Form
    {
        int idxLigne = 0;
        private int[] pLignes;//xx yy

        private Point rightDownPoint;
        private Point oldPoint;
        private Point p;
        private Image image;
        private List<(ETransformation, Image, int)> imgs;

        //private Int64 qualité = 100L;
        //private Point tailleCarte;

        private int curCDIdx;
        private List<Rectangle> carteDetect;

        int angle;

        private VisualiseurCartes visualiseur;

        public BoardGameFabrique()
        {
            p = new Point(0,0);
            pLignes = new int[4];
            pLignes[0] = 100;
            pLignes[1] = 200;
            pLignes[2] = 100;
            pLignes[3] = 200;
            angle = 0;
            curCDIdx = -1;
            //tailleCarte = new Point(330, 516);
            image = null;
            imgs = new List<(ETransformation, Image, int)>();
            carteDetect = null;
            visualiseur = new VisualiseurCartes(imgs);
            InitializeComponent();
            visualiseur.Show();
        }

        private void BoardGameFabrique_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        static private string[] ImageExtention = { ".JPEG", ".JPG", ".BMP", ".PNG" };

        private void BoardGameFabrique_DragDrop(object sender, DragEventArgs e)
        {
            string[] lstFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (lstFiles != null && lstFiles.Length > 0)
            {
                string fName = lstFiles.First().Trim();
                string fileNameEnd = fName.ToUpper();
                if (fileNameEnd.Length >= 4) fileNameEnd = fileNameEnd.Substring(fileNameEnd.Length - 4, 4);
                if (ImageExtention.Contains(fileNameEnd))
                {
                    image = Bitmap.FromFile(fName);
                    Bitmap nbtmap = new Bitmap(image.Width, image.Height);
                    Graphics.FromImage(nbtmap).DrawImage(image, 0, 0);
                    image = nbtmap;
                    DétecterCartes();
                    if(carteDetect != null && carteDetect.Any())
                    {
                        curCDIdx = 0;
                        UpdateCrdDetect();
                    }
                    Refresh();
                }
            }
        }

        /*private int CalculDist(uint cref, uint ctest)
        {
            byte a = (byte)((((cref >> 16) & 0xFF) + ((cref >> 8) & 0xFF) + (cref & 0xFF)) / 3);
            byte b = (byte)((((ctest >> 16) & 0xFF) + ((ctest >> 8) & 0xFF) + (ctest & 0xFF)) / 3);
            return Math.Abs(a - b);
        }*/
        
        private int CalculDist(uint cref, uint ctest)
        {
            return Math.Max(Math.Abs((int)((cref >> 16) & 0xFF) - (int)((ctest >> 16) & 0xFF)),
                Math.Max(Math.Abs((int)((cref >> 8) & 0xFF) - (int)((ctest >> 8) & 0xFF)),
                Math.Abs((int)((cref >> 0) & 0xFF) - (int)((ctest >> 0) & 0xFF))));
        }

        private void DétecterCartes()
        {
            int marg = 10;
            int seuil = 100;
            int crtDMin = 50;
            Bitmap btmp = (image as Bitmap);
            carteDetect = new List<Rectangle>();
            BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                uint *grePtr = (uint*)btdt.Scan0.ToPointer();
                for(int y = 0; y < image.Height; ++y)
                {
                    uint* grePtrY = grePtr + image.Width * y;
                    IEnumerable<Rectangle> rectY = carteDetect.Where(r => (r.Y - marg <= y && y <= r.Y + r.Height + marg));
                    uint cref = *grePtrY;
                    for (int x = 0; x < image.Width; ++x)
                    {
                        Rectangle rect = rectY.FirstOrDefault(r => (r.X - marg <= x && x <= r.X + r.Width + marg));
                        if (rect.Width > 0 && rect.Height > 0) x = rect.X + rect.Width + marg;
                        else if (CalculDist(cref, *(grePtrY + x)) > seuil)
                        {
                            int rh;
                            for (rh = 0; y + rh < image.Height; ++rh)
                            {
                                uint* loc_grePtrY = grePtrY + image.Width * rh;
                                int scanx;
                                for (scanx = -marg; scanx <= marg; ++scanx)
                                    if (0 <= (x + scanx) && (x + scanx) < image.Width && (CalculDist(cref, *(loc_grePtrY + x + scanx)) > seuil)) break;
                                if (scanx >= marg) break;
                            }
                            if(rh >= crtDMin)
                            {
                                int rw;
                                for (rw = 0; x + rw < image.Width; ++rw)
                                {
                                    uint* loc_grePtrX = grePtrY + x + rw;
                                    int scany;
                                    for (scany = -marg; scany <= marg; ++scany)
                                        if (0 <= (y + scany) && (y + scany) < image.Height && (CalculDist(cref,*(loc_grePtrX + image.Width * scany)) > seuil)) break;
                                    if (scany >= marg) break;
                                }

                                if (rw >= crtDMin)
                                {
                                    int rx = x - 1;
                                    for (int scany = 0; scany < rh; ++scany)
                                    {
                                        uint* loc_grePtrY = grePtrY + image.Width * scany;
                                        for (; 0 < rx && (CalculDist(cref, *(loc_grePtrY + rx)) > seuil); --rx) ;
                                    }
                                    rx += 1;
                                    rw += (x - rx);

                                    for (int scany = 0; scany < rh; ++scany)
                                    {
                                        uint* loc_grePtrY = grePtrY + image.Width * scany;
                                        for (; (rx + rw) < image.Width && (CalculDist(cref, *(loc_grePtrY + (rx + rw))) > seuil); ++rw) ;
                                    }

                                    int ry = y - 1;
                                    for (int scanx = 0; scanx < rw; ++scanx)
                                    {
                                        uint* loc_grePtrX = grePtr  + rx + scanx;
                                        for (; 0 < ry && (CalculDist(cref, *(loc_grePtrX + ry * image.Width)) > seuil); --ry) ;
                                    }
                                    ry += 1;
                                    rh += (y - ry);

                                    for (int scanx = 0; scanx < rw; ++scanx)
                                    {
                                        uint* loc_grePtrX = grePtr + rx + scanx;
                                        for (; (ry + rh) < image.Height && (CalculDist(cref, *(loc_grePtrX + (ry + rh) * image.Width)) > seuil); ++rh) ;
                                    }

                                    carteDetect.Add(new Rectangle(rx, ry, rw, rh));
                                    x = rx + rw + marg;
                                }
                            }
                        }
                        //else cref = CalculGrey(*(grePtrY + x));
                    }
                }
            }
            btmp.UnlockBits(btdt);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //Redessiner(e.Graphics);
            Graphics g = e.Graphics;
            //if(image != null)g.DrawImage(image, p.X, p.Y, new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            if (image != null) g.DrawImage(image, p);
            g.DrawLine(new Pen(Color.Red), pLignes[0] + p.X, 0, pLignes[0] + p.X, this.Height);
            g.DrawLine(new Pen(Color.Red), pLignes[1] + p.X, 0, pLignes[1] + p.X, this.Height);
            g.DrawLine(new Pen(Color.Red), 0, pLignes[2] + p.Y, this.Width, pLignes[2] + p.Y);
            g.DrawLine(new Pen(Color.Red), 0, pLignes[3] + p.Y, this.Width, pLignes[3] + p.Y);
        }

        private void BoardGameFabrique_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right))
            {
                rightDownPoint = e.Location;
                oldPoint = e.Location;
            }
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                if (idxLigne < 2) pLignes[idxLigne] = e.Location.X - p.X;
                else pLignes[idxLigne] = e.Location.Y - p.Y;
                Refresh();
            }
        }

        private void BoardGameFabrique_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right))
            {
                p.X += e.Location.X - oldPoint.X;
                p.Y += e.Location.Y - oldPoint.Y;
                oldPoint = e.Location;
                Refresh();
            }
            if(e.Button.HasFlag(MouseButtons.Left))
            {
                if (idxLigne < 2) pLignes[idxLigne] = e.Location.X - p.X;
                else pLignes[idxLigne] = e.Location.Y - p.Y;
                Refresh();
            }
        }

        private void BoardGameFabrique_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '1' || e.KeyChar == '&') idxLigne = 0;
            else if (e.KeyChar == '2' || e.KeyChar == 'é') idxLigne = 1;
            else if (e.KeyChar == '3' || e.KeyChar == '"') idxLigne = 2;
            else if (e.KeyChar == '4' || e.KeyChar == '\'') idxLigne = 3;
            else if(e.KeyChar == ' ')
            {
                int x1 = Math.Min(pLignes[0], pLignes[1]);
                int x2 = Math.Max(pLignes[0], pLignes[1]);
                int y1 = Math.Min(pLignes[2], pLignes[3]);
                int y2 = Math.Max(pLignes[2], pLignes[3]);
                int w = x2 - x1 + 1;
                int h = y2 - y1 + 1;
                if (w > 0 && h > 0 && image != null)
                {
                    int x = 0;
                    int y = 0;
                    Bitmap nbtmap;
                    if(angle == 90 || angle == 270)
                    {
                        nbtmap = new Bitmap(h, w);
                        if (angle == 90) y -= h;
                        else if(angle == 270) x -= w;
                    }
                    else
                    {
                        nbtmap = new Bitmap(w, h);
                        if (angle == 0);
                        else if (angle == 180)
                        {
                            x -= w;
                            y -= h;
                        }
                    }
                    Graphics g = Graphics.FromImage(nbtmap);
                    if (angle != 0) g.RotateTransform(angle);
                    g.DrawImage(image, new Rectangle(x, y, w, h), new Rectangle(x1, y1, w, h), GraphicsUnit.Pixel);
                    imgs.Add((ETransformation.Identity, nbtmap, 1));
                    visualiseur.AjoutSuppresCarte();
                    //MessageBox.Show("Ok ! " + imgs.Count + " images.");
                }
                else MessageBox.Show("Impossible !");
            }
            else if(e.KeyChar == '\b')
            {
                if (imgs.Count > 0)
                {
                    imgs.RemoveAt(imgs.Count - 1);
                    visualiseur.AjoutSuppresCarte();
                    //MessageBox.Show("Supp ! " + imgs.Count + " images.");
                }
                else MessageBox.Show("Impossible !");
            }
            else if(e.KeyChar == '+')
            {
                angle = (angle + 90) % 360;
                MessageBox.Show("Rotation = " + angle + "°");
            }
            else if (e.KeyChar == '-')
            {
                angle = (angle + (360 - 90)) % 360;
                MessageBox.Show("Rotation = " + angle + "°");
            }
        }

        private void UpdateCrdDetect()
        {
            pLignes[0] = carteDetect[curCDIdx].X;
            pLignes[1] = carteDetect[curCDIdx].X + carteDetect[curCDIdx].Width - 1;
            pLignes[2] = carteDetect[curCDIdx].Y;
            pLignes[3] = carteDetect[curCDIdx].Y + carteDetect[curCDIdx].Height - 1;
            Refresh();
        }

        private void BoardGameFabrique_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode==(Keys.Up))
            {
                if (idxLigne >= 2)
                {
                    pLignes[idxLigne]--;
                    Refresh();
                }
            }
            else if (e.KeyCode==(Keys.Down))
            {
                if (idxLigne >= 2)
                {
                    pLignes[idxLigne]++;
                    Refresh();
                }
            }
            else if (e.KeyCode==(Keys.Left))
            {
                if (idxLigne < 2)
                {
                    pLignes[idxLigne]--;
                    Refresh();
                }
            }
            else if (e.KeyCode==(Keys.Right))
            {
                if (idxLigne < 2)
                {
                    pLignes[idxLigne]++;
                    Refresh();
                }
            }
            else if(e.KeyCode==Keys.PageUp)
            {
                if(carteDetect != null && carteDetect.Any())
                {
                    curCDIdx++;
                    if (curCDIdx >= carteDetect.Count) curCDIdx = 0;
                    UpdateCrdDetect();
                }
            }
            else if (e.KeyCode == Keys.PageDown)
            {
                if (carteDetect != null && carteDetect.Any())
                {
                    curCDIdx--;
                    if (curCDIdx < 0) curCDIdx = carteDetect.Count - 1;
                    UpdateCrdDetect();
                }
            }
        }

        private void DécomposerLigneColonne()
        {
            Décomposer décomp = new Décomposer();
            if(image != null && décomp.ShowDialog(this) == DialogResult.OK)
            {
                int w = image.Width / décomp.nbColonnes;
                int h = image.Height / décomp.nbLignes;
                for (int i = 0; i < décomp.nbCartes; ++i)
                {
                    int x = w * (i % décomp.nbColonnes);
                    int y = h * (i / décomp.nbColonnes);
                    Bitmap nbtmap = new Bitmap(w, h);
                    Graphics g = Graphics.FromImage(nbtmap);
                    g.DrawImage(image, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                    imgs.Add((ETransformation.Identity, nbtmap, 1));
                    
                }
                visualiseur.AjoutSuppresCarte();
            }
        }

        private static readonly Dictionary<string, ImageFormat> dicoFilForm = new Dictionary<string, ImageFormat>()
        {
            {"jpg", ImageFormat.Jpeg},
            {"png", ImageFormat.Png},
            {"bmp", ImageFormat.Bmp}
        };

        private void GénérerXML(string fichierImg, int nbl, int nbc, int w, int h)
        {
            string fichierXML;
            int idxLstP = fichierImg.LastIndexOf('.');
            int idxSlash = Math.Max(fichierImg.LastIndexOf('/'), fichierImg.LastIndexOf('\\'));
            if (idxLstP < idxSlash) idxLstP = -1;
            
            if(idxLstP >= 0) fichierXML = fichierImg.Substring(0, idxLstP) + ".XML";
            else fichierXML = fichierImg + ".XML";

            if (idxSlash >= 0) fichierImg = fichierImg.Substring(idxSlash + 1);
            string nom;
            idxLstP = fichierImg.LastIndexOf('.');
            if (idxLstP > 0) nom = fichierImg.Substring(0, idxLstP);
            else if (idxLstP == 0 || String.IsNullOrWhiteSpace(fichierImg)) nom = "xxx";
            else nom = fichierImg;

            int idxVide;
            for (idxVide = imgs.Count - 1; idxVide>=0 && imgs[idxVide].Item3 > 0; --idxVide) ;

            int dx = 0, dy = 0;
            if(idxVide >= 0)
            {
                dx = (idxVide % nbc) * w;
                dy = (idxVide / nbc) * h;
            }

            int nb;
            if (imgs.Last().Item3 <= 0) nb = imgs.Count - 1;
            else nb = imgs.Count;

            string XML = "";
            XML += "<GROUPE>\r\n";
            if (idxVide >= 0) XML += $"<PIOCHE nom=\"{nom}Pch\" vide=\"{fichierImg}\" défausse=\"{nom}Dfs\" mélanger=\"\" dx=\"{dx}\" dy=\"{dy}\" w=\"{w}\" h=\"{h}\" x=\"-120\" y=\"0\">\r\n";
            else XML += $"<PIOCHE nom=\"{nom}Pch\" défausse=\"{nom}Dfs\" mélanger=\"\" w=\"{w}\" h=\"{h}\" x=\"-120\" y=\"0\">\r\n";
            XML += $"\t<CARTES img=\"{fichierImg}\" nb=\"{nb}\" nbc=\"{nbc}\" nbl=\"{nbl}\">\r\n";
            for(int i = 0; i < imgs.Count; ++i)
            {
                if(imgs[i].Item3 > 1) XML += $"\t\t<QUANTITE id=\"{i}\" qt=\"{imgs[i].Item3}\"/>\r\n";
            }
            XML += "\t</CARTES>\r\n";
            XML += "</PIOCHE>\r\n";
            if (idxVide >= 0) XML += $"<DEFFAUSE nom=\"{nom}Dfs\" vide=\"{fichierImg}\" pioche=\"{nom}Pch\" dx=\"{dx}\" dy=\"{dy}\" w=\"{w}\" h=\"{h}\" x=\"120\" y=\"0\"/>\r\n";
            else XML += $"<DEFFAUSE nom=\"{nom}Dfs\" pioche=\"{nom}Pch\" w=\"{w}\" h=\"{h}\" x=\"120\" y=\"0\"/>\r\n";
            XML += "</GROUPE>\r\n";

            File.WriteAllText(fichierXML, XML, Encoding.UTF8);
        }

        private void Sauvegarder()
        {
            if(imgs == null || imgs.Any() == false)
            {
                MessageBox.Show("Impossible !");
                return;
            }

            double moyw = imgs.Average(img => img.Item2.Width);
            double moyh = imgs.Average(img => img.Item2.Height);
            Sauvegarder svg = new Sauvegarder(moyw, moyh);

            if (svg.ShowDialog(this) == DialogResult.OK)
            {
                int tailleCarteW = svg.TailleFinaleW;
                int tailleCarteH = svg.TailleFinaleH;

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                string flt = "";
                foreach (string ext in dicoFilForm.Keys)
                    flt += ext + " files (*." + ext + ")|*." + ext + "|";
                saveFileDialog.Filter = flt.Substring(0, flt.Length - 1);
                saveFileDialog.FileName = Guid.NewGuid().ToString();

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    ImageFormat selectFormat;
                    {
                        int idxlp = saveFileDialog.FileName.LastIndexOf(".");
                        if(idxlp<0 || !dicoFilForm.TryGetValue(saveFileDialog.FileName.Substring(idxlp+1).ToLower(), out selectFormat))
                            selectFormat = ImageFormat.Jpeg;
                    }

                    int nbCartes = imgs.Count;
                    int sq = (int)(Math.Sqrt(nbCartes) + 0.5);
                    int rest = int.MaxValue;
                    int nbL = 1;
                    int nbC = 1;
                    for (int i = sq; i > 1; --i)
                    {
                        int nc = nbCartes / i;
                        if (nbCartes % i > 0) ++nc;
                        int r = nc * i - nbCartes;
                        if (r < rest)
                        {
                            rest = r;
                            nbL = i;
                            nbC = nc;
                        }
                    }
                    if (rest > 2)
                    {
                        nbL = 1;
                        nbC = nbCartes;
                    }
                    Bitmap btmap = new Bitmap(tailleCarteW * nbC, tailleCarteH * nbL);
                    Graphics g = Graphics.FromImage(btmap);
                    for (int i = 0; i < nbCartes; ++i)
                    {
                        ETransformation etrans;
                        Image img;
                        int qt;
                        (etrans, img, qt) = imgs[i];

                        int x = (i % nbC) * tailleCarteW;
                        int y = (i / nbC) * tailleCarteH;

                        g.ResetTransform();
                        bool mirrorX = false;
                        bool mirrorY = false;

                        if (etrans.HasFlag(ETransformation.Retourner))
                        {
                            mirrorX ^= true;
                            mirrorY ^= true;
                        }
                        if (etrans.HasFlag(ETransformation.MiroirX))
                        {
                            mirrorX ^= true;
                        }
                        if (etrans.HasFlag(ETransformation.MiroirY))
                        {
                            mirrorY ^= true;
                        }

                        if (mirrorX)
                        {
                            g.ScaleTransform(1.0f, -1.0f);
                            y -= img.Height;
                        }
                        if (mirrorY)
                        {
                            g.ScaleTransform(-1.0f, 1.0f);
                            x -= img.Width;
                        }
                        g.DrawImage(img, new Rectangle(x, y, tailleCarteW, tailleCarteH), new Rectangle(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                        //g.ScaleTransform(((float)tailleCarte.X) / ((float)imgs[i].Width), ((float)tailleCarte.Y) / ((float)imgs[i].Height));
                        //g.DrawImage(imgs[i], x, y);
                    }
                    ImageCodecInfo Encoder = ImageCodecInfo.GetImageDecoders().First(ecd => ecd.FormatID == selectFormat.Guid);
                    EncoderParameter myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, svg.Qualité);
                    EncoderParameters myEncoderParameters = new EncoderParameters(1);
                    myEncoderParameters.Param[0] = myEncoderParameter;
                    btmap.Save(saveFileDialog.FileName, Encoder, myEncoderParameters);

                    if (svg.GénérerXML) GénérerXML(saveFileDialog.FileName, nbL, nbC, tailleCarteW, tailleCarteH);
                }
            }
        }

        private void Effacer()
        {
            imgs.Clear();
            visualiseur.AjoutSuppresCarte();
        }

        private void BoardGameFabrique_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                PointF dX = new PointF(e.X - rightDownPoint.X, e.Y - rightDownPoint.Y);
                float dCarrée = dX.X * dX.X + dX.Y * dX.Y;

                if (dCarrée <= 5.0f)
                {
                    ContextMenu cm = new ContextMenu();
                    cm.MenuItems.Add(new MenuItem("Décomposer ligne colonne", (o, eArg) => this.DécomposerLigneColonne()));
                    cm.MenuItems.Add(new MenuItem("-"));
                    cm.MenuItems.Add(new MenuItem("Afficher le visualiseur de cartes", (o, eArg) => { visualiseur.Show(); visualiseur.BringToFront(); }));;
                    cm.MenuItems.Add(new MenuItem("-"));
                    cm.MenuItems.Add(new MenuItem("Sauvegarder", (o, eArg) => this.Sauvegarder()));
                    cm.MenuItems.Add(new MenuItem("-"));
                    cm.MenuItems.Add(new MenuItem("-"));
                    cm.MenuItems.Add(new MenuItem("Tout éffacer", (o, eArg) => this.Effacer()));
                    cm.Show(this, e.Location);
                }
            }
        }
    }
}
