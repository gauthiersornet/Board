/*using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        private PointF oldRPoint;
        private PointF p;
        private float echel;
        private float angle;
        private Image image;
        private Image imageDetect;
        private uint? gcref;
        private List<(ETransformation, Image, int)> imgs;

        //private Int64 qualité = 100L;
        //private Point tailleCarte;

        private int curCDIdx;
        private List<(Rectangle, float)> carteDetect;

        private VisualiseurCartes visualiseur;

        public bool shift;
        public bool ctrl;

        private uint? pcref;
        int seuilDepar = (int)(195075 * 0.285), seuil = (int)(195075 * 0.06),/* seuilEntrant = 80, seuilSortant = 50,*/ marg = 15, crtDMin = 100;
        int seuilTransparence = (int)(195075 * 0.1);
        int arrondiCoins = 120;
        private uint? transCref;
        DetectAlgo activerDétect = DetectAlgo.Avancé;
        DetectAlgo activerDoubleDétect = DetectAlgo.Vide;
        bool activerTransparence = true;
        Task carteDétectTask;
        CancellationTokenSource carteDétectTaskCT;

        public BoardGameFabrique()
        {
            carteDétectTask = null;
            carteDétectTaskCT = null;

            p = new Point(0, 0);
            pLignes = new int[4];
            pLignes[0] = -100;
            pLignes[1] = 100;
            pLignes[2] = -100;
            pLignes[3] = 100;
            angle = 0.0f;
            curCDIdx = -1;
            echel = 1.0f;
            //tailleCarte = new Point(330, 516);
            image = null;
            gcref = null;
            imgs = new List<(ETransformation, Image, int)>();
            carteDetect = null;
            pcref = null;
            transCref = null;
            visualiseur = new VisualiseurCartes(imgs);
            InitializeComponent();
            this.MouseWheel += new MouseEventHandler(this.BoardGameFabrique_MouseWheel);
            //visualiseur.Show();
        }

        private void BoardGameFabrique_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        static private string[] ImageExtention = { ".JPEG", ".JPG", ".BMP", ".PNG", ".GIF", ".TIFF" };

        static private bool EstFormatConnue(string fName)
        {
            string fileNameEnd = fName.ToUpper();
            if (fileNameEnd.Length >= 4) fileNameEnd = fileNameEnd.Substring(fileNameEnd.Length - 4, 4);
            return ImageExtention.Contains(fileNameEnd);
        }

        static unsafe private void SoustraireFond(Bitmap bA, Bitmap bB, int seuil, int seuilDelta)
        {
            BitmapData btdtA = bA.LockBits(new Rectangle(0, 0, bA.Width, bA.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData btdtB = bB.LockBits(new Rectangle(0, 0, bB.Width, bB.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                uint va, vb, v;
                uint a, r, g, b;
                uint* ptrA = (uint*)btdtA.Scan0.ToPointer();
                uint* ptrB = (uint*)btdtB.Scan0.ToPointer();
                uint lg = (uint)bA.Width; lg *= (uint)bB.Height;
                for (uint i = 0; i < lg; ++i)
                {
                    va = *ptrA;
                    vb = *ptrB;
                    a = ((va >> 24) & 0xFF);
                    r = ((va >> 16) & 0xFF);
                    g = ((va >> 8) & 0xFF);
                    b = ((va >> 0) & 0xFF);
                    a += ((vb >> 24) & 0xFF);
                    r += ((vb >> 16) & 0xFF);
                    g += ((vb >> 8) & 0xFF);
                    b += ((vb >> 0) & 0xFF);
                    v = ((a / 2) << 24) | ((r / 2) << 16) | ((g / 2) << 8) | ((b / 2) << 0);
                    *ptrA = v;
                    *ptrB = va;
                    int dist = CalculDist(va, vb) - seuil;
                    if (dist > 0)
                    {
                        *ptrB &= (uint)0xFFFFFF;
                        if (dist < seuilDelta) *ptrB |= (uint)(0xFE - ((dist * 0xFE) / seuilDelta)) << 24;
                    }
                    //*ptrB &= (uint)0xFFFFFF;
                    //if (dist <= seuil) *ptrB |= (uint)(0xFF - ((dist * 0xFE) / seuil)) << 24;
                    /*if (dist >= seuil) 
                    {
                        *ptrB &= (uint)0xFFFFFF;
                        //*ptrB |= (uint)(0xFF - ((dist * 0xFE) / seuil)) << 24;
                        //if (dist < seuilDelta) *ptrB |= (uint)(0xFE - ((dist * 0xFF) / seuilDelta)) << 24;
                        //*ptrA &= (uint)0xFFFFFF;
                    }*/
                    ++ptrA;
                    ++ptrB;
                }
            }
            catch { }
            finally
            {
                bB.UnlockBits(btdtB);
                bA.UnlockBits(btdtA);
            }
        }

        private void BoardGameFabrique_DragDrop(object sender, DragEventArgs e)
        {
            string[] lstFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (lstFiles != null && lstFiles.Length > 0)
            {
                pcref = null;
                string fName = lstFiles.FirstOrDefault(f => EstFormatConnue(f));
                if (fName != null)
                {
                    stopDétecterCartes();

                    imageDetect = image = Bitmap.FromFile(fName);
                    Bitmap nbtmap = new Bitmap(image.Width, image.Height);
                    using (Graphics g = Graphics.FromImage(nbtmap))
                    {
                        g.DrawImage(image, 0, 0, nbtmap.Width, nbtmap.Height);
                    }
                    image = nbtmap;

                    string fName2 = lstFiles.LastOrDefault(f => EstFormatConnue(f));
                    if (fName2 != null && fName2 != fName)
                    {
                        //Si on a 2 images, alors il faut les comparer pour extraire les éléments du font
                        Image img2 = Bitmap.FromFile(fName2);
                        if (img2.Width == image.Width && img2.Height == image.Height)
                        {
                            nbtmap = new Bitmap(img2.Width, img2.Height);
                            using (Graphics g = Graphics.FromImage(nbtmap))
                            {
                                g.DrawImage(img2, 0, 0, nbtmap.Width, nbtmap.Height);
                            }
                            SoustraireFond(image as Bitmap, nbtmap, (int)(195075 * 0.06), (int)(195075 * 0.06));
                            imageDetect = nbtmap;
                            //image = imageDetect;
                            pcref = 0xFFFFFF;
                        }
                    }

                    if (imageDetect == image)
                    {
                        imageDetect = new Bitmap(image.Width, image.Height);
                        using (Graphics g = Graphics.FromImage(imageDetect))
                        {
                            g.DrawImage(image, 0, 0, image.Width, image.Height);
                        }
                    }

                    /*p.X = image.Width / 2.0f;
                    p.Y = image.Height / 3.0f;*/
                    p.X = 0.0f; p.Y = 0.0f;
                    Refresh();
                    //DétecterCartes();
                    calculDétecterCartes();
                    //Refresh();
                }
            }
        }

        private delegate void dlgVoidDTCCrts(List<(Rectangle, float)> rects);
        private void AsyncDétectCartes()
        {
            List<(Rectangle, float)> rects;
            if (activerDétect == DetectAlgo.Avancé) rects = DétecterCartesAngle((imageDetect as Bitmap), ref gcref, seuilDepar, seuil, marg, crtDMin, arrondiCoins);
            else if (activerDétect == DetectAlgo.Basique) rects = DétecterCartes((imageDetect as Bitmap), ref gcref, seuilDepar, marg, crtDMin);
            else rects = new List<(Rectangle, float)>();
            BeginInvoke((dlgVoidDTCCrts)IVK_calculDétecterCartes, rects);
        }

        private void stopDétecterCartes()
        {
            if (carteDétectTask != null && carteDétectTask.IsCompleted == false)
            {
                if (carteDétectTaskCT != null)
                {
                    carteDétectTaskCT.Cancel();
                }
                try
                {
                    carteDétectTask.Wait();
                }
                catch (OperationCanceledException e)
                {
                    Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
                }
                finally
                {
                    carteDétectTask.Dispose();
                }
            }
            carteDétectTask = null;
            carteDétectTaskCT = null;
            this.Text = "BoardGameFabrique";
        }

        private void calculDétecterCartes()
        {
            stopDétecterCartes();
            if (imageDetect != null)
            {
                this.Text = "BoardGameFabrique (détection de cartes...)";
                gcref = pcref;
                carteDétectTaskCT = new CancellationTokenSource();
                carteDétectTask = Task.Run(() => AsyncDétectCartes(), carteDétectTaskCT.Token);
            }
        }

        private void IVK_calculDétecterCartes(List<(Rectangle, float)> rects)
        {
            carteDétectTask = null;
            carteDétectTaskCT = null;
            carteDetect = rects;
            //if (carteDetect == null) carteDetect = new List<(Rectangle, float)>();
            if (carteDetect.Any() == false && activerDétect != DetectAlgo.Vide) carteDetect.Add((new Rectangle(0, 0, image.Width, image.Height), 0.0f));
            //if (carteDetect != null && carteDetect.Any())
            {
                curCDIdx = 0;
                UpdateCrdDetect();
            }
            this.Text = "BoardGameFabrique";
        }

        /*static private int CalculDist(uint cref, uint ctest)
        {
            byte a = (byte)((((cref >> 16) & 0xFF) + ((cref >> 8) & 0xFF) + (cref & 0xFF)) / 3);
            byte b = (byte)((((ctest >> 16) & 0xFF) + ((ctest >> 8) & 0xFF) + (ctest & 0xFF)) / 3);
            return Math.Abs(a - b);
        }*/

        static private int CalculDist(uint cref, uint ctest)
        {
            byte ctestA = (byte)((ctest >> 24) & 0xFF);
            if (ctestA == 0) return 0;
            byte crefA = (byte)((cref >> 24) & 0xFF);
            if (crefA == 0) return int.MaxValue;
            else
            {
                int dr = ((int)(cref >> 16) & 0xFF) - (int)((ctest >> 16) & 0xFF);
                int dg = ((int)(cref >> 8) & 0xFF) - (int)((ctest >> 8) & 0xFF);
                int db = ((int)(cref >> 0) & 0xFF) - (int)((ctest >> 0) & 0xFF);

                return (int)(dr * dr + dg * dg + db * db);
            }
        }

        private uint colorPond(uint ca, uint cb/*, byte po = 10*/)
        {
            /*byte r = (byte)((((ca >> 16) & 0xFF) * (255 - po) + ((cb >> 16) & 0xFF) * po) / 255);
            byte g = (byte)((((ca >> 8) & 0xFF) * (255 - po) + ((cb >> 8) & 0xFF) * po) / 255);
            byte b = (byte)((((ca >> 0) & 0xFF) * (255 - po) + ((cb >> 0) & 0xFF) * po) / 255);
            return ((uint)(r << 16) | (uint)(g << 8) | (uint)(b << 0));*/
            return ca;
        }

        //depSeuil, int seuilEntrant, int seuilSortant
        unsafe private PointF LimitDelta(uint* grePtr, Size taille, int seuil, int delta, PointF départ, uint cref, PointF n)
        {
            if (carteDétectTaskCT != null && carteDétectTaskCT.IsCancellationRequested) carteDétectTaskCT.Token.ThrowIfCancellationRequested();

            int da, db;
            PointF a, b;
            Point ia, ib;
            a = b = new PointF(départ.X, départ.Y);

            bool aDebCRef;
            bool bDebCRef;

            ia = new Point((int)(a.X - n.X * 0 + 0.5f), (int)(a.Y - n.Y * 0 + 0.5f));
            if (0 <= (ia.X) && ia.X < taille.Width && 0 <= ia.Y && ia.Y < taille.Height)
            {
                uint aref = grePtr[ia.Y * taille.Width + ia.X];
                aDebCRef = (CalculDist(cref, aref) < seuil);
                da = 0;
            }
            else
            {
                da = 0;
                aDebCRef = true;
            }
            a.X += n.X;
            a.Y += n.Y;

            ib = new Point((int)(b.X + n.X * 0 + 0.5f), (int)(b.Y + n.Y * 0 + 0.5f));
            if (0 <= ib.X && ib.X < taille.Width && 0 <= ib.Y && ib.Y < taille.Height)
            {
                uint bref = grePtr[ib.Y * taille.Width + ib.X];
                bDebCRef = (CalculDist(cref, bref) < seuil);
                db = 0;
            }
            else
            {
                db = 0;
                bDebCRef = true;
            }
            b.X -= n.X;
            b.Y -= n.Y;

            for (int i = 1; i < delta; ++i)
            {
                ia.X = (int)(a.X + 0.5f);
                ia.Y = (int)(a.Y + 0.5f);

                ib.X = (int)(b.X + 0.5f);
                ib.Y = (int)(b.Y + 0.5f);

                //if (da >= 0)
                {
                    if (0 <= (ia.X) && ia.X < taille.Width && 0 <= ia.Y && ia.Y < taille.Height)
                    {
                        uint r = grePtr[ia.Y * taille.Width + ia.X];
                        /*if (((r >> 24) & 0xFF) < 128)
                        {
                            if (aDebCRef) da = 0;
                            else da = int.MaxValue;
                        }
                        else*/ da = CalculDist(cref, r);
                        //aref = colorPond(aref, r);
                    }
                    else da = 0;

                    a.X += n.X;
                    a.Y += n.Y;
                }

                //if (db >= 0)
                {
                    if (0 <= ib.X && ib.X < taille.Width && 0 <= ib.Y && ib.Y < taille.Height)
                    {
                        uint r = grePtr[ib.Y * taille.Width + ib.X];
                        /*if (((r >> 24) & 0xFF) < 128)
                        {
                            if (bDebCRef) db = 0;
                            else db = int.MaxValue;
                        }
                        else */db = CalculDist(cref, r);
                        //bref = colorPond(bref, r);
                    }
                    else db = 0;

                    b.X -= n.X;
                    b.Y -= n.Y;
                }

                if (aDebCRef ^ (da < seuil))
                {
                    if (aDebCRef)return a;
                    else return new PointF(a.X - n.X, a.Y - n.Y);
                }
                else if (bDebCRef ^ (db < seuil))
                {
                    if (bDebCRef) return b;
                    else return new PointF(b.X + n.X, b.Y + n.Y);
                }
            }
            return new PointF(float.MinValue, float.MinValue);
        }

        static private float CalculPond(uint cref, uint val, int seuilMin, int seuilDelta = (int)(195075 * 0.3))
        {
            byte valA = (byte)((val >> 24) & 0xFF);
            byte crefA = (byte)((cref >> 24) & 0xFF);
            if(crefA == 0)
            {
                if (valA == 0.0f) return 0.0f;
                else //return 1.0f;
                {
                    float r = 0.001f + 0.999f * (((float)(valA)) / 255.0f);
                    return r * r;
                }
            }
            else
            {
                int dr = ((int)(cref >> 16) & 0xFF) - (int)((val >> 16) & 0xFF);
                int dg = ((int)(cref >> 8) & 0xFF) - (int)((val >> 8) & 0xFF);
                int db = ((int)(cref >> 0) & 0xFF) - (int)((val >> 0) & 0xFF);

                int calS = (int)(dr * dr + dg * dg + db * db) - seuilMin;
                if (calS <= 0.0f) return 0.0f;
                else if (calS >= seuilDelta) return 1.0f;
                else
                {
                    float r = 0.001f + 0.999f * (((float)valA) / 255.0f) * (((float)calS) / ((float)(seuilDelta)));
                    return r * r;
                }
            }
        }

        unsafe private (PointF, float) LimitDeltaPond(uint* grePtr, Size taille, int seuil, int delta, PointF départ, uint cref, PointF n)
        {
            if (carteDétectTaskCT != null && carteDétectTaskCT.IsCancellationRequested) carteDétectTaskCT.Token.ThrowIfCancellationRequested();

            int da, db;
            PointF a, b;
            Point ia, ib;
            a = b = new PointF(départ.X, départ.Y);

            bool aDebCRef;
            uint aR = 0;
            uint aref;
            bool bDebCRef;
            uint bR = 0;
            uint bref;

            ia = new Point((int)(a.X - n.X * 0 + 0.5f), (int)(a.Y - n.Y * 0 + 0.5f));
            if (0 <= (ia.X) && ia.X < taille.Width && 0 <= ia.Y && ia.Y < taille.Height)
            {
                aref = grePtr[ia.Y * taille.Width + ia.X];
                aDebCRef = (CalculDist(cref, aref) < seuil);
                da = 0;
            }
            else
            {
                da = 0;
                aref = 0;
                aDebCRef = true;
            }
            a.X += n.X;
            a.Y += n.Y;

            ib = new Point((int)(b.X + n.X * 0 + 0.5f), (int)(b.Y + n.Y * 0 + 0.5f));
            if (0 <= ib.X && ib.X < taille.Width && 0 <= ib.Y && ib.Y < taille.Height)
            {
                bref = grePtr[ib.Y * taille.Width + ib.X];
                bDebCRef = (CalculDist(cref, bref) < seuil);
                db = 0;
            }
            else
            {
                db = 0;
                bref = 0;
                bDebCRef = true;
            }
            b.X -= n.X;
            b.Y -= n.Y;

            for (int i = 1; i < delta; ++i)
            {
                ia.X = (int)(a.X + 0.5f);
                ia.Y = (int)(a.Y + 0.5f);

                ib.X = (int)(b.X + 0.5f);
                ib.Y = (int)(b.Y + 0.5f);

                //if (da >= 0)
                {
                    aR = aref;

                    if (0 <= (ia.X) && ia.X < taille.Width && 0 <= ia.Y && ia.Y < taille.Height)
                    {
                        aref = grePtr[ia.Y * taille.Width + ia.X];
                        /*if (((r >> 24) & 0xFF) < 128)
                        {
                            if (aDebCRef) da = 0;
                            else da = int.MaxValue;
                        }
                        else*/
                        da = CalculDist(cref, aref);
                        //aref = colorPond(aref, r);
                    }
                    else
                    {
                        aref = 0;
                        da = 0;
                    }

                    a.X += n.X;
                    a.Y += n.Y;
                }

                //if (db >= 0)
                {
                    bR = bref;

                    if (0 <= ib.X && ib.X < taille.Width && 0 <= ib.Y && ib.Y < taille.Height)
                    {
                        bref = grePtr[ib.Y * taille.Width + ib.X];
                        /*if (((r >> 24) & 0xFF) < 128)
                        {
                            if (bDebCRef) db = 0;
                            else db = int.MaxValue;
                        }
                        else */
                        db = CalculDist(cref, bref);
                        //bref = colorPond(bref, r);
                    }
                    else
                    {
                        bref = 0;
                        db = 0;
                    }

                    b.X -= n.X;
                    b.Y -= n.Y;
                }

                if (aDebCRef ^ (da < seuil))
                {
                    if (aDebCRef) return (a, CalculPond(cref, aref, seuil));
                    else return (new PointF(a.X - n.X, a.Y - n.Y), CalculPond(cref, aR, seuil));
                }
                else if (bDebCRef ^ (db < seuil))
                {
                    if (bDebCRef) return (b, CalculPond(cref, bref, seuil));
                    else return (new PointF(b.X + n.X, b.Y + n.Y), CalculPond(cref, bR, seuil));
                }
            }
            return (new PointF(float.MinValue, float.MinValue), -1.0f);
        }

        struct SSobel
        {
            public float x, y, h;
            public byte a, r, g, b;
        }

        static unsafe private SSobel[] CalculerSobel(uint* grePtr, int w, int h)
        {
            SSobel[] sbl = new SSobel[w*h];

            for(int j = 1; j < h-1; ++j)
            {
                int idxj = j * w;
                for (int i = 1; i < w-1; ++i)
                {
                    int idx = idxj + i;

                    uint c = *(grePtr + idx);
                    SSobel sb = new SSobel();
                    sb.a = (byte)((c >> 24) & 0xFF);
                    sb.r = (byte)((c >> 16) & 0xFF);
                    sb.g = (byte)((c >>  8) & 0xFF);
                    sb.b = (byte)((c >>  0) & 0xFF);

                    uint hg = *(grePtr + idx - w - 1);
                    uint hd = *(grePtr + idx - w + 1);
                    uint bg = *(grePtr + idx + w - 1);
                    uint bd = *(grePtr + idx + w + 1);
                    sb.x = 0.0f;
                }
            }

            return sbl;
        }

        //depSeuil, int seuilEntrant, int seuilSortant
        unsafe private LinkedList<Point> LimitCarteUni(uint* grePtr, Size taille, int seuil, int marg, Point départ, uint cref, PointF d)
        {
            LinkedList<Point> res = new LinkedList<Point>();
            res.AddLast(départ);

            PointF scanP = new PointF(départ.X + d.X, départ.Y + d.Y);
            Point iscanP = new Point((int)(scanP.X + 0.5f), (int)(scanP.Y + 0.5f));

            int nbP;
            for(nbP = 1; (0 <= iscanP.X && iscanP.X < taille.Width && 0 <= iscanP.Y && iscanP.Y < taille.Height); ++nbP)
            {
                PointF nsp = LimitDelta(grePtr, taille, seuil, marg, scanP, cref, new PointF(-d.Y, d.X));
                if(nsp.X == float.MinValue)
                {
                    /*scanP.X -= d.X;
                    scanP.Y -= d.Y;
                    iscanP.X = (int)(scanP.X + 0.5f);
                    iscanP.Y = (int)(scanP.Y + 0.5f);*/
                    break;
                }
                else
                {
                    res.AddLast(iscanP);
                    /*PointF newD = new PointF(scanP.X - départ.X, scanP.Y - départ.Y);
                    if (Math.Abs(newD.X) >= Math.Abs(newD.Y))
                    {
                        if (newD.X > 0.0f)
                        {
                            newD.Y /= newD.X;
                            newD.X = 1.0f;
                        }
                        else if (newD.X < 0.0f)
                        {
                            newD.Y /= -newD.X;
                            newD.X = -1.0f;
                        }
                    }
                    else
                    {
                        if (newD.Y > 0.0f)
                        {
                            newD.X /= newD.Y;
                            newD.Y = 1.0f;
                        }
                        else if (newD.Y < 0.0f)
                        {
                            newD.X /= -newD.Y;
                            newD.Y = -1.0f;
                        }
                    }

                    d.X = (d.X * nbP + newD.X) / (nbP + 1);
                    d.Y = (d.Y * nbP + newD.Y) / (nbP + 1);*/

                    scanP.X += d.X;
                    scanP.Y += d.Y;
                    iscanP.X = (int)(scanP.X + 0.5f);
                    iscanP.Y = (int)(scanP.Y + 0.5f);
                }
            }

            return res;
        }

        //depSeuil, int seuilEntrant, int seuilSortant
        unsafe private LinkedList<(Point, float)> LimitCarteBil(uint* grePtr, Size taille, int seuil, int marg, Point départ, uint cref, PointF d)
        {
            LinkedList<(Point, float)> res = new LinkedList<(Point, float)>();
            
            float poid = ((0 <= (départ.X) && départ.X < taille.Width && 0 <= départ.Y && départ.Y < taille.Height) ? CalculPond(cref, grePtr[départ.Y * taille.Width + départ.X], seuil) : 0.0f);
            res.AddLast((départ, poid));

            PointF scanPA = new PointF(départ.X + d.X, départ.Y + d.Y);
            Point iscanPA = new Point((int)(scanPA.X + 0.5f), (int)(scanPA.Y + 0.5f));
            float pondA = ((0 <= (iscanPA.X) && iscanPA.X < taille.Width && 0 <= iscanPA.Y && iscanPA.Y < taille.Height) ? CalculPond(cref, grePtr[iscanPA.Y * taille.Width + iscanPA.X], seuil) : 0.0f);
            float ipondA = pondA;

            PointF scanPB = new PointF(départ.X - d.X, départ.Y - d.Y);
            Point iscanPB = new Point((int)(scanPB.X + 0.5f), (int)(scanPB.Y + 0.5f));
            float ipondB = ((0 <= (iscanPB.X) && iscanPB.X < taille.Width && 0 <= iscanPB.Y && iscanPB.Y < taille.Height) ? CalculPond(cref, grePtr[iscanPB.Y * taille.Width + iscanPB.X], seuil) : 0.0f);
            float pondB = ipondB;

            int nbP;
            bool okA, okB;
            okA = okB = true;
            for (nbP = 1; okA || okB; ++nbP)
            {
                if (okA)
                {
                    //if(0 <= iscanPA.X && iscanPA.X < taille.Width && 0 <= iscanPA.Y && iscanPA.Y < taille.Height)
                    {
                        (PointF, float) nsp = LimitDeltaPond(grePtr, taille, seuil, marg, scanPA, cref, new PointF(-d.Y, d.X));
                        if (nsp.Item2 < 0.0f)
                        {
                            /*scanPA.X -= d.X;
                            scanPA.Y -= d.Y;
                            iscanPA.X = (int)(scanPA.X + 0.5f);
                            iscanPA.Y = (int)(scanPA.Y + 0.5f);*/
                            okA = false;
                        }
                        else
                        {
                            scanPA = nsp.Item1;
                            ipondA = nsp.Item2;
                        }
                    }
                    //else okA = false;
                }

                if (okB)
                {
                    //if (0 <= iscanPB.X && iscanPB.X < taille.Width && 0 <= iscanPB.Y && iscanPB.Y < taille.Height)
                    {
                        (PointF, float) nsp = LimitDeltaPond(grePtr, taille, seuil, marg, scanPB, cref, new PointF(-d.Y, d.X));
                        if (nsp.Item2 < 0.0f)
                        {
                            /*scanPB.X -= d.X;
                            scanPB.Y -= d.Y;
                            iscanPB.X = (int)(scanPB.X + 0.5f);
                            iscanPB.Y = (int)(scanPB.Y + 0.5f);*/
                            okB = false;
                        }
                        else
                        {
                            scanPB = nsp.Item1;
                            pondB = nsp.Item2;
                        }
                    }
                    //else okB = false;
                }

                /*PointF newD = new PointF(scanPA.X - scanPB.X, scanPA.Y - scanPB.Y);
                if (Math.Abs(newD.X) >= Math.Abs(newD.Y))
                {
                    if (newD.X > 0.0f)
                    {
                        newD.Y /= newD.X;
                        newD.X = 1.0f;
                    }
                    else if (newD.X < 0.0f)
                    {
                        newD.Y /= -newD.X;
                        newD.X = -1.0f;
                    }
                }
                else
                {
                    if (newD.Y > 0.0f)
                    {
                        newD.X /= newD.Y;
                        newD.Y = 1.0f;
                    }
                    else if (newD.Y < 0.0f)
                    {
                        newD.X /= -newD.Y;
                        newD.Y = -1.0f;
                    }
                }*/

                /*d.X = (d.X * nbP * nbP + newD.X) / (nbP * nbP + 1);
                d.Y = (d.Y * nbP * nbP + newD.Y) / (nbP * nbP + 1);*/

                if (okA)
                {
                    res.AddLast((iscanPA , ipondA));

                    scanPA.X += d.X;
                    scanPA.Y += d.Y;
                    iscanPA.X = (int)(scanPA.X + 0.5f);
                    iscanPA.Y = (int)(scanPA.Y + 0.5f);
                    ipondA = pondA;
                }

                if (okB)
                {
                    res.AddFirst((iscanPB, ipondB));

                    scanPB.X -= d.X;
                    scanPB.Y -= d.Y;
                    iscanPB.X = (int)(scanPB.X + 0.5f);
                    iscanPB.Y = (int)(scanPB.Y + 0.5f);
                    ipondB = pondB;
                }

            }

            return res;
            /*if (nbP == 1) return (départ, départ);
            else
            {
                //
                return (iscanPB, iscanPA);
            }*/
        }
        private Point Diff(Point a, Point b)
        {
            return new Point(a.X - b.X, a.Y - b.Y);
        }

        private int SqDist(Point a, Point b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }

        //private (PointF, PointF) PosDirMed(LinkedList<Point> points, int coin, bool sens, float coefH = 0.75f, float coefD = 1.0f, int nbEch = -1)
        //{
        //    IEnumerable<Point> pts = SéléctionPoints(points, coin, nbEch);
        //    PointF p = new PointF(float.MinValue, float.MinValue), v = new PointF(float.MinValue, float.MinValue), n;
        //    /*for (int i = 0; i < 3; ++i)
        //    {
        //        (p, v) = MoindreCarré(pts);
        //        if (sens) n = new PointF(v.Y, -v.X);
        //        else n = new PointF(v.Y, -v.X);
        //        p = calculPointLimit(pts, n, p, 0.5f);
        //        pts = SélectionPointsProches(pts, n, p, coefD);
        //    }*/
        //
        //    (p, v) = MoindreCarré(pts);
        //    if (sens) n = new PointF(v.Y, -v.X);
        //    else n = new PointF(v.Y, -v.X);
        //    p = calculPointLimit(pts, n, p, coefH);
        //    pts = SélectionPointsProchesInf(pts, n, p, coefD);
        //
        //    /*(p, v) = MoindreCarré(pts);
        //    if (sens) n = new PointF(v.Y, -v.X);
        //    else n = new PointF(-v.Y, v.X);
        //    p = calculPointLimit(pts, n, p, coefH);
        //    pts = SélectionPointsProchesInf(pts, n, p, coefD);*/
        //
        //    (p, v) = MoindreCarré(pts);
        //    if (sens) n = new PointF(v.Y, -v.X);
        //    else n = new PointF(-v.Y, v.X);
        //    p = calculPointLimit(pts, n, p, coefH);
        //
        //    return (p, v);
        //}

        /*
            (p, v) = MoindreCarré(pts, MoindreCarré(pts));
            if (sens) n = new PointF(v.Y, -v.X);
            else n = new PointF(v.Y, -v.X);
            p = calculPointLimit(pts, n, p, coefH);
            pts = SélectionPointsProchesInf(pts, n, p, coefD);

            (p, v) = MoindreCarré(pts, (p, v));
            if (sens) n = new PointF(v.Y, -v.X);
            else n = new PointF(-v.Y, v.X);
            p = calculPointLimit(pts, n, p, coefH);
         */

        private (PointF, PointF) PosDirMed(LinkedList<(Point, float)> points, int coin, bool sens, float coefH = 0.75f, float coefD = 0.75f, int nbEch = -1)
        {
            IEnumerable<(Point, float)> pts = SéléctionPoints(points, coin, nbEch);
            PointF p = new PointF(float.MinValue, float.MinValue), v = new PointF(float.MinValue, float.MinValue), n;

            (p, v) = MoindreCarré(pts);
            if (sens) n = new PointF(v.Y, -v.X);
            else n = new PointF(-v.Y, v.X);
            p = calculPointLimit(pts, n, p, coefH);
            /*pts = SélectionPointsProchesInf(pts, n, p, coefD);

            (p, v) = MoindreCarré(pts);
            if (sens) n = new PointF(v.Y, -v.X);
            else n = new PointF(-v.Y, v.X);
            p = calculPointLimit(pts, n, p, coefH);*/

            return (p, v);
        }

        static double GetCoinCoef(int coin)
        {
            if (coin <= 0) return 1.0;
            else if (coin > 1000) return 0.0f;
            else return ((1000 - coin) / 1000.0);
        }

        static private IEnumerable<(Point, float)> SéléctionPoints(LinkedList<(Point, float)> points, int coin, int nbEch = -1)
        {
            int mid = (points.Count() - 1) / 2;
            if (nbEch < 0)
            {
                if (coin <= 0) nbEch = (int)(mid * 0.8 + 0.5);
                else
                {
                    double coef = ((1000 - coin) / 1000.0);
                    nbEch = (int)(mid * coef * 0.98f + 0.5f);
                }
            }
            nbEch = Math.Min(nbEch, mid);
            if (nbEch > 0)
            {
                (Point, float)[] pts = new (Point, float)[1 + 2 * nbEch];
                LinkedList<(Point, float)>.Enumerator iter = points.GetEnumerator();
                for (int i = (mid - nbEch); i > 0 && iter.MoveNext(); --i) ;
                for (int i = 0; i < pts.Length && iter.MoveNext(); ++i) pts[i] = iter.Current;
                return pts;
            }
            else return null;
        }

        static private IEnumerable<Point> SéléctionPoints(LinkedList<Point> points, int coin, int nbEch = -1)
        {
            int mid = (points.Count() - 1) / 2;
            if (nbEch < 0)
            {
                if (coin <= 0) nbEch = (int)(mid * 0.8 + 0.5);
                else
                {
                    double coef = ((1000 - coin) / 1000.0);
                    nbEch = (int)(mid * coef * 0.98f + 0.5f);
                }
            }
            nbEch = Math.Min(nbEch, mid);
            if (nbEch > 0)
            {
                Point[] pts = new Point[1 + 2 * nbEch];
                LinkedList<Point>.Enumerator iter = points.GetEnumerator();
                for (int i = (mid - nbEch); i > 0 && iter.MoveNext(); --i) ;
                for (int i = 0; i < pts.Length && iter.MoveNext(); ++i) pts[i] = iter.Current;
                return pts;
            }
            else return null;
        }

        static private PointF transfoV(PointF v, (PointF, PointF) lBase)
        {
            return new PointF(v.X * lBase.Item2.X + v.Y * lBase.Item2.Y, -v.X * lBase.Item2.Y + v.Y * lBase.Item2.X);
        }

        static private PointF transfoP(PointF p, (PointF, PointF) lBase)
        {
            p.X -= lBase.Item1.X;
            p.Y -= lBase.Item1.Y;
            return transfoV(p, lBase);
        }

        static private PointF transfoVInv(PointF v, (PointF, PointF) lBase)
        {
            return new PointF(v.X * lBase.Item2.X - v.Y * lBase.Item2.Y, v.X * lBase.Item2.Y + v.Y * lBase.Item2.X);
        }

        static private PointF transfoPInv(PointF p, (PointF, PointF) lBase)
        {
            p = transfoVInv(p, lBase);
            return new PointF(p.X + lBase.Item1.X, p.Y + lBase.Item1.Y);
        }

        static private (PointF, PointF) MoindreCarré(IEnumerable<(Point, float)> pts)
        {
            double sum = pts.Sum(p => p.Item2);
            double mx = pts.Sum(p => p.Item1.X * p.Item2) / sum;
            double my = pts.Sum(p => p.Item1.Y * p.Item2) / sum;
            double varx = pts.Sum(p => p.Item2 * (p.Item1.X - mx) * (p.Item1.X - mx));
            double vary = pts.Sum(p => p.Item2 * (p.Item1.Y - my) * (p.Item1.Y - my));
            double covarxy = pts.Sum(p => p.Item2 * (p.Item1.X - mx) * (p.Item1.Y - my));

            if (Math.Abs(covarxy) < 0.000000000001)
            {
                if (varx >= vary) return (new PointF((float)mx, (float)my), new PointF(1.0f, 0.0f));
                else return (new PointF((float)mx, (float)my), new PointF(0.0f, 1.0f));
            }
            else
            {
                double C = (varx - vary) / (2 * covarxy);

                double CSQC = Math.Sign(covarxy) * Math.Sqrt(C * C + 1);
                double B1H = CSQC - C;
                double B1V = CSQC + C; // inversion abscisses ordonnées

                //double plx, ply;
                double vlx, vly;

                if (Math.Abs(B1H) <= Math.Abs(B1V))//On prend les abscisses ?
                {
                    //double B0 = my - B1H * mx;
                    //plx = 0.0; ply = B0;
                    vlx = 1.0; vly = B1H;
                }
                else //On prend les ordonnées
                {
                    //double B0 = mx - B1V * my;
                    //plx = B0; ply = 0.0;
                    vlx = B1V; vly = 1.0;
                }

                double hypo = Math.Sqrt(vlx * vlx + vly * vly);
                vlx /= hypo;
                vly /= hypo;

                /*double d = (mx - plx) * -vly + (my - ply) * vlx;
                plx = mx + d * -vly;
                ply = my + d * vlx;*/

                if (Math.Abs(vlx) >= Math.Abs(vly))
                {
                    if (vlx >= 0.0f) return (new PointF((float)mx, (float)my), new PointF((float)vlx, (float)vly));
                    else return (new PointF((float)mx, (float)my), new PointF((float)-vlx, (float)-vly));
                }
                else
                {
                    if (vly >= 0.0f) return (new PointF((float)mx, (float)my), new PointF((float)vlx, (float)vly));
                    else return (new PointF((float)mx, (float)my), new PointF((float)-vlx, (float)-vly));
                }
            }
        }

        static private (PointF, PointF) MoindreCarré(IEnumerable<Point> pts)
        {
            double mx = pts.Average(p => p.X);
            double my = pts.Average(p => p.Y);
            double varx = pts.Sum(p => (p.X - mx) * (p.X - mx));
            double vary = pts.Sum(p => (p.Y - my) * (p.Y - my));
            double covarxy = pts.Sum(p => (p.X - mx) * (p.Y - my));

            if(Math.Abs(covarxy) < 0.000000000001)
            {
                if(varx >= vary) return (new PointF((float)mx, (float)my), new PointF(1.0f, 0.0f));
                else return (new PointF((float)mx, (float)my), new PointF(0.0f, 1.0f));
            }
            else
            {
                double C = (varx - vary) / (2 * covarxy);

                double CSQC = Math.Sign(covarxy) * Math.Sqrt(C * C + 1);
                double B1H = CSQC - C;
                double B1V = CSQC + C; // inversion abscisses ordonnées

                //double plx, ply;
                double vlx, vly;

                if(Math.Abs(B1H) <= Math.Abs(B1V))//On prend les abscisses ?
                {
                    //double B0 = my - B1H * mx;
                    //plx = 0.0; ply = B0;
                    vlx = 1.0; vly = B1H;
                }
                else //On prend les ordonnées
                {
                    //double B0 = mx - B1V * my;
                    //plx = B0; ply = 0.0;
                    vlx = B1V; vly = 1.0;
                }

                double hypo = Math.Sqrt(vlx * vlx + vly * vly);
                vlx /= hypo;
                vly /= hypo;

                /*double d = (mx - plx) * -vly + (my - ply) * vlx;
                plx = mx + d * -vly;
                ply = my + d * vlx;*/

                if (Math.Abs(vlx) >= Math.Abs(vly))
                {
                    if (vlx >= 0.0f) return (new PointF((float)mx, (float)my), new PointF((float)vlx, (float)vly));
                    else return (new PointF((float)mx, (float)my), new PointF((float)-vlx, (float)-vly));
                }
                else
                {
                    if (vly >= 0.0f) return (new PointF((float)mx, (float)my), new PointF((float)vlx, (float)vly));
                    else return (new PointF((float)mx, (float)my), new PointF((float)-vlx, (float)-vly));
                }
            }
        }

        //static private(PointF, PointF) MoindreCarré(IEnumerable<Point> pts, (PointF, PointF) lBase)
        //{
        //    if (pts != null && pts.Count() > 0)
        //    {
        //        List<PointF> fpts = pts.Select(p => transfoP(p, lBase)).ToList();

        //        double sumX = fpts.Sum(p => p.X);
        //        double sumY = fpts.Sum(p => p.Y);

        //        double[,] mat = new double[2, 2];
        //        mat[0, 0] = fpts.Sum(p => p.X * p.X);
        //        mat[0, 1] = sumX;
        //        mat[1, 0] = sumX;
        //        mat[1, 1] = fpts.Count();
        //        Matrix<double> A = DenseMatrix.OfArray(mat).Inverse();

        //        double[] vec = new double[2];
        //        vec[0] = fpts.Sum(p => p.X * p.Y);
        //        vec[1] = sumY;
        //        Vector<double> V = DenseVector.OfArray(vec);

        //        Vector<double> Ry = A * V;

        //        /*mat[0, 0] = fpts.Sum(p => p.Y * p.Y);
        //        mat[0, 1] = sumY;
        //        mat[1, 0] = sumY;

        //        A = DenseMatrix.OfArray(mat).Inverse();
        //        vec[1] = sumX;
        //        V = DenseVector.OfArray(vec);

        //        Vector<double> Rx = A * V;*/

        //        if (/*double.IsNaN(Rx[0]) && */double.IsNaN(Ry[0]))
        //        {
        //            return (PointF.Empty, PointF.Empty);
        //        }

        //        PointF pline, vline;

        //        PointF mp = new PointF((float)(sumX / fpts.Count()), (float)(sumY / fpts.Count()));

        //        /*double RxDelta = Math.Abs(Rx[0]);
        //        double RyDelta = Math.Abs(Ry[0]);

        //        if (double.IsNaN(Rx[0]) || RxDelta > 100.0f)
        //        {*/
        //            double hypo = Math.Sqrt(Ry[0] * Ry[0] + 1.0);
        //            vline = new PointF((float)(1.0 / hypo), (float)(Ry[0] / hypo));
        //            pline = new PointF(0.0f, (float)(Ry[1]));

        //            PointF plmp = new PointF(mp.X - pline.X, mp.Y - pline.Y);
        //            float dot = -vline.Y * plmp.X + vline.X * plmp.Y;
        //            pline.X = mp.X - dot * vline.Y;
        //            pline.Y = mp.Y + dot * vline.X;
        //        /*}
        //        else if (double.IsNaN(Ry[0]) || RyDelta > 100.0f)
        //        {
        //            double hypo = Math.Sqrt(Rx[0] * Rx[0] + 1.0);
        //            vline = new PointF((float)(Rx[0] / hypo), (float)(1.0 / hypo));
        //            pline = new PointF((float)(Rx[1]), 0.0f);

        //            PointF plmp = new PointF(mp.X - pline.X, mp.Y - pline.Y);
        //            float dot = -vline.Y * plmp.X + vline.X * plmp.Y;
        //            pline.X = mp.X - dot * vline.Y;
        //            pline.Y = mp.Y + dot * vline.X;
        //        }
        //        else
        //        {
        //            double hypo;
        //            PointF plmp;
        //            float dot;

        //            PointF vl;
        //            PointF pl;

        //            if (RxDelta <= RyDelta)
        //            {
        //                hypo = Math.Sqrt(Rx[0] * Rx[0] + 1.0);
        //                vl = new PointF((float)(Rx[0] / hypo), (float)(1.0 / hypo));
        //                pl = new PointF((float)(Rx[1]), 0.0f);
        //            }
        //            else
        //            {
        //                hypo = Math.Sqrt(Ry[0] * Ry[0] + 1.0);
        //                vl = new PointF((float)(1.0 / hypo), (float)(Ry[0] / hypo));
        //                pl = new PointF(0.0f, (float)(Ry[1]));
        //            }

        //            plmp = new PointF(mp.X - pl.X, mp.Y - pl.Y);
        //            dot = -vl.Y * plmp.X + vl.X * plmp.Y;
        //            pl.X = mp.X - dot * vl.Y;
        //            pl.Y = mp.Y + dot * vl.X;

        //            pline = pl;
        //            vline = vl;
        //            hypo = Math.Sqrt(vline.X * vline.X + vline.Y * vline.Y);
        //            vline.X = (float)(vline.X / hypo);
        //            vline.Y = (float)(vline.Y / hypo);
        //        }*/

        //        pline = transfoPInv(pline, lBase);
        //        vline = transfoVInv(vline, lBase);

        //        if (Math.Abs(vline.X) >= Math.Abs(vline.Y))
        //        {
        //            if (vline.X < 0.0f)
        //            {
        //                vline.X = -vline.X;
        //                vline.Y = -vline.Y;
        //            }
        //        }
        //        else
        //        {
        //            if (vline.Y < 0.0f)
        //            {
        //                vline.X = -vline.X;
        //                vline.Y = -vline.Y;
        //            }
        //        }

        //        return (pline, vline);
        //    }
        //    else return (new PointF(), new PointF());
        //}

        //P et Dir
        //private (PointF, PointF) MoindreCarré(IEnumerable<Point> pts)
        //{
        //    if (pts != null && pts.Count() > 0)
        //    {
        //        double sumX = pts.Sum(p => p.X);
        //        double sumY = pts.Sum(p => p.Y);

        //        double[,] mat = new double[2, 2];
        //        mat[0, 0] = pts.Sum(p => p.X * p.X);
        //        mat[0, 1] = sumX;
        //        mat[1, 0] = sumX;
        //        mat[1, 1] = pts.Count();
        //        Matrix<double> A = DenseMatrix.OfArray(mat).Inverse();

        //        double[] vec = new double[2];
        //        vec[0] = pts.Sum(p => p.X * p.Y);
        //        vec[1] = sumY;
        //        Vector<double> V = DenseVector.OfArray(vec);

        //        Vector<double> Ry = A * V;

        //        mat[0, 0] = pts.Sum(p => p.Y * p.Y);
        //        mat[0, 1] = sumY;
        //        mat[1, 0] = sumY;

        //        A = DenseMatrix.OfArray(mat).Inverse();
        //        vec[1] = sumX;
        //        V = DenseVector.OfArray(vec);

        //        Vector<double> Rx = A * V;

        //        if (double.IsNaN(Rx[0]) && double.IsNaN(Ry[0]))
        //        {
        //            return (PointF.Empty, PointF.Empty);
        //        }

        //        PointF pline, vline;

        //        PointF mp = new PointF((float)(sumX / pts.Count()), (float)(sumY / pts.Count()));

        //        double RxDelta = Math.Abs(Rx[0]);
        //        double RyDelta = Math.Abs(Ry[0]);

        //        if (double.IsNaN(Rx[0]) || RxDelta > 100.0f)
        //        {
        //            double hypo = Math.Sqrt(Ry[0] * Ry[0] + 1.0);
        //            vline = new PointF((float)(1.0 / hypo), (float)(Ry[0] / hypo));
        //            pline = new PointF(0.0f, (float)(Ry[1]));

        //            PointF plmp = new PointF(mp.X - pline.X, mp.Y - pline.Y);
        //            float dot = -vline.Y * plmp.X + vline.X * plmp.Y;
        //            pline.X = mp.X - dot * vline.Y;
        //            pline.Y = mp.Y + dot * vline.X;
        //        }
        //        else if (double.IsNaN(Ry[0]) || RyDelta > 100.0f)
        //        {
        //            double hypo = Math.Sqrt(Rx[0] * Rx[0] + 1.0);
        //            vline = new PointF((float)(Rx[0] / hypo), (float)(1.0 / hypo));
        //            pline = new PointF((float)(Rx[1]), 0.0f);

        //            PointF plmp = new PointF(mp.X - pline.X, mp.Y - pline.Y);
        //            float dot = -vline.Y * plmp.X + vline.X * plmp.Y;
        //            pline.X = mp.X - dot * vline.Y;
        //            pline.Y = mp.Y + dot * vline.X;
        //        }
        //        else
        //        {
        //            double hypo;
        //            PointF plmp;
        //            float dot;

        //            PointF vl;
        //            PointF pl;

        //            if(RxDelta <= RyDelta)
        //            {
        //                hypo = Math.Sqrt(Rx[0] * Rx[0] + 1.0);
        //                vl = new PointF((float)(Rx[0] / hypo), (float)(1.0 / hypo));
        //                pl = new PointF((float)(Rx[1]), 0.0f);
        //            }
        //            else
        //            {
        //                hypo = Math.Sqrt(Ry[0] * Ry[0] + 1.0);
        //                vl = new PointF((float)(1.0 / hypo), (float)(Ry[0] / hypo));
        //                pl = new PointF(0.0f, (float)(Ry[1]));
        //            }

        //            plmp = new PointF(mp.X - pl.X, mp.Y - pl.Y);
        //            dot = -vl.Y * plmp.X + vl.X * plmp.Y;
        //            pl.X = mp.X - dot * vl.Y;
        //            pl.Y = mp.Y + dot * vl.X;

        //            pline = pl;
        //            vline = vl;
        //            hypo = Math.Sqrt(vline.X * vline.X + vline.Y * vline.Y);
        //            vline.X = (float)(vline.X / hypo);
        //            vline.Y = (float)(vline.Y / hypo);
        //        }

        //        if(Math.Abs(vline.X) >= Math.Abs(vline.Y))
        //        {
        //            if(vline.X < 0.0f)
        //            {
        //                vline.X = -vline.X;
        //                vline.Y = -vline.Y;
        //            }
        //        }
        //        else
        //        {
        //            if (vline.Y < 0.0f)
        //            {
        //                vline.X = -vline.X;
        //                vline.Y = -vline.Y;
        //            }
        //        }

        //        return (pline, vline);
        //    }
        //    else return (new PointF(), new PointF());
        //}

        private PointF DirMoy(LinkedList<Point> points, int coin, int nbEch = -1)
        {
            int mid = points.Count() / 2;
            if (nbEch < 0)
            {
                if(coin <= 0) nbEch = (int)(mid * 0.8 + 0.5);
                else
                {
                    double coef = ((1000 - coin) / 1000.0);
                    nbEch = (int)(mid * coef + 0.5f);
                }
            }
            nbEch = Math.Min(nbEch, mid);
            if (nbEch >= 2)
            {
                PointF accp = new Point();
                for (int i = 1; i < nbEch; ++i)
                {
                    Point v = Diff(points.ElementAt(mid), points.ElementAt(mid - i));
                    //double dsq = Math.Sqrt(v.X * v.X + v.Y * v.Y);
                    accp.X += (float)(v.X);
                    accp.Y += (float)(v.Y);
                    v = Diff(points.ElementAt(mid + i), points.ElementAt(mid));
                    //dsq = v.X * v.X + v.Y * v.Y;
                    accp.X += (float)(v.X);
                    accp.Y += (float)(v.Y);
                }
                double hypo = Math.Sqrt(accp.X * accp.X + accp.Y * accp.Y);
                accp.X = (float)(accp.X / hypo);
                accp.Y = (float)(accp.Y / hypo);

                return accp;
            }
            else return new PointF();
        }

        /*private (PointF, PointF) calculPointsLimit(LinkedList<Point> points, PointF v)
        {
            Point rp = points.ElementAt(points.Count / 2);
            float hmax = points.Max(p => ((p.X - rp.X) * v.Y + (p.Y - rp.Y) * -v.X));
            PointF mp = new PointF(rp.X + v.Y * hmax, rp.Y - v.X * hmax);

            float cmin = 0.0f; //(points.First().X - mp.X) * v.X + (points.First().Y - mp.Y) * v.Y;
            float cmax = 0.0f; //(points.Last().X - mp.X) * v.X + (points.Last().Y - mp.Y) * v.Y;
            foreach (Point p in points)
            {
                float proj = (p.X - mp.X) * v.X + (p.Y - mp.Y) * v.Y;
                if (cmax < proj) cmax = proj;
                else if (proj < cmin) cmin = proj;
            }

            return (new PointF(mp.X + v.X * cmin, mp.Y + v.Y * cmin), new PointF(mp.X + v.X * cmax, mp.Y + v.Y * cmax));
        }*/

        private PointF calculPointLimit(LinkedList<Point> points, PointF v)
        {
            Point rp = points.ElementAt(points.Count / 2);
            return calculPointLimit(points, v, rp);
        }

        private PointF calculPointLimit(LinkedList<(Point, float)> points, PointF v)
        {
            Point rp = points.ElementAt(points.Count / 2).Item1;
            return calculPointLimit(points, v, rp);
        }

        static private int Comparer((float, float) a, (float, float) b)
        {
            if (a.Item1 == b.Item1) return 0;
            else if (a.Item1 < b.Item1) return -1;
            else return 1;
        }

        static private int Comparer((Point, float) a, (Point, float) b)
        {
            if (a.Item2 == b.Item2) return 0;
            else if (a.Item2 < b.Item2) return -1;
            else return 1;
        }

        static private int Comparer((Point, float, float) a, (Point, float, float) b)
        {
            if (a.Item3 == b.Item3) return 0;
            else if (a.Item3 < b.Item3) return -1;
            else return 1;
        }

        static private IEnumerable<Point> SélectionPointsProchesInf(IEnumerable<Point> points, PointF v, PointF rp, float coef = 0.5f)
        {
            List<(Point, float)> ph = points.Select(p => (p, -((p.X - rp.X) * v.X + (p.Y - rp.Y) * v.Y))).Where(cp => cp.Item2 >= 0.0f).ToList();
            ph.Sort(Comparer);
            int nbp = (int)(ph.Count * coef + 0.5f);
            List<Point> res = new List<Point>(nbp);
            for (int i = 0; i < nbp; ++i) res.Add(ph[i].Item1);
            return res;
        }

        /*static private IEnumerable<(Point, float)> SélectionPointsProchesInf(IEnumerable<(Point, float)> points, PointF v, PointF rp, float coef = 0.5f)
        {
            float limit = points.Sum(p => p.Item2) * coef;
            List<(Point, float, float)> ph = points.Select(p => (p.Item1, p.Item2, -((p.Item1.X - rp.X) * v.X + (p.Item1.Y - rp.Y) * v.Y))).Where(cp => cp.Item3 >= 0.0f).ToList();
            ph.Sort(Comparer);
            List<(Point, float)> res = new List<(Point, float)>();
            for (int i = 0; i < ph.Count && limit > 0.0f; ++i)
            {
                res.Add((ph[i].Item1, ph[i].Item2));
                limit -= ph[i].Item2;
            }
            return res;
        }*/

        static private IEnumerable<Point> SélectionPointsProchesABS(IEnumerable<Point> points, PointF v, PointF rp, float coef = 0.5f)
        {
            List<(Point, float)> ph = points.Select(p => (p, Math.Abs((p.X - rp.X) * v.X + (p.Y - rp.Y) * v.Y))).ToList();
            ph.Sort(Comparer);
            int nbp = (int)(ph.Count * coef + 0.5f);
            List<Point> res = new List<Point>(nbp);
            for (int i = 0; i < nbp; ++i) res.Add(ph[i].Item1);
            return res;
        }

        static private PointF calculPointLimit(IEnumerable<(Point, float)> points, PointF v, PointF rp, float coef = 0.75f)
        {
            /*float hmax = points.Max(p => ((p.X - rp.X) * v.X + (p.Y - rp.Y) * v.Y));
            return new PointF(rp.X + v.X * hmax, rp.Y + v.Y * hmax);*/
            if (points != null && points.Any())
            {
                float limit =  points.Sum(p => p.Item2) * coef;
                List<(float, float)> h = points.Select(p => (((p.Item1.X - rp.X) * v.X + (p.Item1.Y - rp.Y) * v.Y), p.Item2)).ToList();
                h.Sort(Comparer);
                float hr = h.Last().Item1;
                foreach((float, float) fp in h)
                {
                    limit -= fp.Item2;
                    if(limit < 0.000001)
                    {
                        hr = fp.Item1;
                        break;
                    }
                }
                return new PointF(rp.X + v.X * hr, rp.Y + v.Y * hr);
            }
            else return new PointF(float.MinValue, float.MinValue);
            /*if (points != null && points.Any())
            {
                List<float> h = points.Select(p => ((p.Item1.X - rp.X) * v.X + (p.Item1.Y - rp.Y) * v.Y)).ToList();
                h.Sort();
                float hr = h[(int)((h.Count - 1) * coef)];
                return new PointF(rp.X + v.X * hr, rp.Y + v.Y * hr);
            }
            else return new PointF(float.MinValue, float.MinValue);*/
        }

        static private PointF calculPointLimit(IEnumerable<Point> points, PointF v, PointF rp, float coef = 0.75f)
        {
            /*float hmax = points.Max(p => ((p.X - rp.X) * v.X + (p.Y - rp.Y) * v.Y));
            return new PointF(rp.X + v.X * hmax, rp.Y + v.Y * hmax);*/
            if (points != null && points.Any())
            {
                List<float> h = points.Select(p => ((p.X - rp.X) * v.X + (p.Y - rp.Y) * v.Y)).ToList();
                h.Sort();
                float hr = h[(int)((h.Count - 1) * coef)];
                return new PointF(rp.X + v.X * hr, rp.Y + v.Y * hr);
            }
            else return new PointF(float.MinValue, float.MinValue);
        }

        static private PointF intersection(PointF Pa, PointF Va, PointF Pb, PointF Vb)
        {
            float dproj = -Va.Y * Vb.X + Va.X * Vb.Y;
            if (dproj != 0.0f)
            {
                PointF P = new PointF(Pb.X - Pa.X, Pb.Y - Pa.Y);
                float B = -Va.Y * P.X + Va.X * P.Y;
                float c = -B / dproj;
                return new PointF(Pb.X + Vb.X * c, Pb.Y + Vb.Y * c);
            }
            else return new PointF(float.MinValue, float.MinValue);
        }

        //private List<(Point, Color)> pts;

        //private PointF svp = new PointF();
        //depSeuil, int seuilEntrant, int seuilSortant
        unsafe private (PointF, PointF, PointF, PointF, PointF, PointF) ContourerCarte(uint* grePtr, Size taille, int seuil, int seuil2, int marg, Point départ, uint cref, int crtDMin, int coin)
        {
            //pts = new List<(Point, Color)>();

            int crtDSqMin = crtDMin * crtDMin;
            LinkedList<(Point , float)> points;

            //obtenirGraphics().DrawEllipse(new Pen(Color.Red), départ.X, départ.Y, 1, 1);
            points = LimitCarteBil(grePtr, taille, seuil, marg, départ, cref, new PointF(1.0f, 0.0f));
            if (SqDist(points.First().Item1, points.Last().Item1) >= crtDSqMin)
            {
                PointF tv;// = DirMoy(points, coin);
                (_, tv) = PosDirMed(points, coin, true);
                points.Clear();
                points = LimitCarteBil(grePtr, taille, seuil2, marg, départ, cref, tv);
                //pts.AddRange(points.Select(pt => (pt, Color.Blue)));

                if (SqDist(points.First().Item1, points.Last().Item1) >= crtDSqMin)
                {
                    PointF hv, hLmt;// = DirMoy(points, coin);
                    (hLmt, hv) = PosDirMed(points, coin, true);
                    //hLmt = calculPointLimit(points, new PointF(hv.Y, -hv.X), hLmt);
                    //obtenirGraphics().DrawLine(new Pen(Color.Red), new PointF(hLmt.X - hv.X * 200, hLmt.Y - hv.Y * 200), new PointF(hLmt.X + hv.X * 200, hLmt.Y + hv.Y * 200));

                    Point hg = points.First().Item1;
                    Point hd = points.Last().Item1;
                    points.Clear();

                    points = LimitCarteBil(grePtr, taille, seuil2, marg, hg, cref, new PointF(-hv.Y, hv.X));
                    //points = LimitCarteBil(grePtr, taille, seuil, marg, hg, cref, new PointF(0.0f, 1.0f));
                    //pts.AddRange(points.Select(pt => (pt, Color.Yellow)));

                    if (SqDist(points.First().Item1, points.Last().Item1) >= crtDSqMin)
                    {
                        PointF gv, gLmt;// = DirMoy(points, coin);
                        (gLmt, gv) = PosDirMed(points, coin, false);
                        //gLmt = calculPointLimit(points, new PointF(-gv.Y, gv.X), gLmt);
                        //obtenirGraphics().DrawLine(new Pen(Color.Red), new PointF(gLmt.X - gv.X * 250, gLmt.Y - gv.Y * 250), new PointF(gLmt.X + gv.X * 250, gLmt.Y + gv.Y * 250));

                        Point bg = points.Last().Item1;
                        points.Clear();

                        points = LimitCarteBil(grePtr, taille, seuil2, marg, hd, cref, new PointF(gv.X, gv.Y));
                        //pts.AddRange(points.Select(pt => (pt, Color.Green)));
                        //points = LimitCarteBil(grePtr, taille, seuil, marg, hd, cref, new PointF(0.0f, 1.0f));
                        if (SqDist(points.First().Item1, points.Last().Item1) >= crtDSqMin)
                        {
                            PointF dv, dLmt;// = DirMoy(points, coin);
                            (dLmt, dv) = PosDirMed(points, coin, true);
                            //dLmt = calculPointLimit(points, new PointF(dv.Y, -dv.X), dLmt);
                            //obtenirGraphics().DrawLine(new Pen(Color.Red), new PointF(dLmt.X - dv.X * 250, dLmt.Y - dv.Y * 250), new PointF(dLmt.X + dv.X * 250, dLmt.Y + dv.Y * 250));

                            Point bd = points.Last().Item1;
                            points.Clear();

                            if(SqDist(hd, bd) <= SqDist(hg, bg)) points = LimitCarteBil(grePtr, taille, seuil2, marg, bg, cref, hv);
                            else points = LimitCarteBil(grePtr, taille, seuil2, marg, bd, cref, hv);
                            //pts.AddRange(points.Select(pt => (pt, Color.Orange)));
                            //points = LimitCarteBil(grePtr, taille, seuil, marg, bg, cref, new PointF(1.0f, 0.0f));
                            if (SqDist(points.First().Item1, points.Last().Item1) >= crtDSqMin)
                            {
                                PointF bv, bLmt;// = DirMoy(points, coin);
                                (bLmt, bv) = PosDirMed(points, coin, false);
                                //bLmt = calculPointLimit(points, new PointF(-bv.Y, bv.X), bLmt);
                                //obtenirGraphics().DrawLine(new Pen(Color.Red), new PointF(bLmt.X - bv.X * 200, bLmt.Y - bv.Y * 200), new PointF(bLmt.X + bv.X * 200, bLmt.Y + bv.Y * 200));

                                PointF hgp = intersection(hLmt, hv, gLmt, gv);
                                //obtenirGraphics().DrawEllipse(new Pen(Color.Blue), hgp.X - 2, hgp.Y - 2, 4, 4);

                                PointF hdp = intersection(hLmt, hv, dLmt, dv);
                                //obtenirGraphics().DrawEllipse(new Pen(Color.Blue), hdp.X - 2, hdp.Y - 2, 4, 4);

                                PointF bgp = intersection(gLmt, gv, bLmt, bv);
                                //obtenirGraphics().DrawEllipse(new Pen(Color.Blue), bgp.X - 2, bgp.Y - 2, 4, 4);

                                PointF bdp = intersection(dLmt, dv, bLmt, bv);
                                //obtenirGraphics().DrawEllipse(new Pen(Color.Blue), bdp.X - 2, bdp.Y - 2, 4, 4);
                                //svp = bdp;

                                return (new PointF((int)(hgp.X + 0.5f), (int)(hgp.Y + 0.5f)),
                                    new PointF((int)(hdp.X - hgp.X + 0.5f), (int)(hdp.Y - hgp.Y + 0.5f)),
                                    new PointF((int)(bgp.X - hgp.X + 0.5f), (int)(bgp.Y - hgp.Y + 0.5f)),
                                    new PointF((int)(hdp.X - bdp.X + 0.5f), (int)(hdp.Y - bdp.Y + 0.5f)),
                                    new PointF((int)(bgp.X - bdp.X + 0.5f), (int)(bgp.Y - bdp.Y + 0.5f)),
                                    new PointF((int)(bdp.X + 0.5f), (int)(bdp.Y + 0.5f)));
                            }
                        }
                    }
                }
            }

            return (new Point(int.MinValue, int.MinValue), new Point(int.MinValue, int.MinValue), new Point(int.MinValue, int.MinValue), new Point(int.MinValue, int.MinValue), new Point(int.MinValue, int.MinValue), new Point(int.MinValue, int.MinValue));
        }

        private uint CouleurTransparenceRéduite(Bitmap btmp, int marge = 0)
        {
            if (btmp != null)
            {
                uint res = 0;
                BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    unsafe
                    {
                        uint* grePtr = (uint*)btdt.Scan0.ToPointer();
                        res = CouleurTransparenceRéduite(*(grePtr + marge * (1 + btmp.Width)));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { btmp.UnlockBits(btdt); }
                return res;
            }
            else return 0;
        }

        //List<List<(Point, Color)>> lstPts;

        //int seuilEntrant = 80, int seuilSortant = 60
        private List<(Rectangle, float)> DétecterCartesAngle(Bitmap btmp, ref uint? _cref, int seuilDepar = 100, int seuil = 70, int marg = 10, int crtDMin = 50, int coin = 0)
        {
            //lstPts = new List<List<(Point, Color)>>();
            List<(Rectangle, float)> lstRes = new List<(Rectangle, float)>();
            int sqCrtMin = crtDMin * crtDMin;
            List<Rectangle> rectExclu = new List<Rectangle>();
            BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            { 
                unsafe
                {
                    uint* grePtr = (uint*)btdt.Scan0.ToPointer();
                    uint cref;
                    if (_cref != null) cref = _cref.Value;
                    else
                    {
                        if(btmp.Width <= marg || btmp.Height <= marg) cref = *grePtr;
                        else cref = *(grePtr + marg * (1 + btmp.Width));
                        _cref = cref;
                    }
                    for (int y = 0; y < btmp.Height; ++y)
                    {
                        uint* grePtrY = grePtr + btmp.Width * y;
                        IEnumerable<Rectangle> rectY = rectExclu.Where(r => (r.Y - marg <= y && y <= r.Y + r.Height + marg));
                        //uint cref = *grePtrY;
                        for(int x = 0; x < btmp.Width; x+= marg)
                        {
                            if (carteDétectTaskCT != null && carteDétectTaskCT.IsCancellationRequested) carteDétectTaskCT.Token.ThrowIfCancellationRequested();

                            Rectangle rect = rectY.FirstOrDefault(r => (r.X - marg <= x && x <= r.X + r.Width + marg));
                            if (rect.Width > 0 && rect.Height > 0) x = rect.X + rect.Width + marg;
                            else if(CalculDist(cref, *(grePtrY + x)) >= seuilDepar && (*(grePtrY + x) >> 24) != 0)
                            {
                                PointF ptc, cvx, cvy;
                                PointF ptu, uvx, uvy;
                                (ptc, cvx, cvy, uvy, uvx, ptu) = ContourerCarte(grePtr, btmp.Size, seuilDepar, seuil, marg, new Point(x, y), cref, crtDMin, coin);

                                //lstPts.Add(pts);

                                //if(cvx.X * cvx.X >= crtDMin && cvx.Y*cvy.Y >= crtDMin)
                                if (0 <= ptc.X && ptc.X < btmp.Width && 0 <= ptc.Y && ptc.Y < btmp.Height &&
                                    (cvx.X * cvx.X + cvx.Y * cvx.Y) >= sqCrtMin && (cvy.X * cvy.X + cvy.Y * cvy.Y) >= sqCrtMin)
                                {
                                    double hypoX = Math.Sqrt(cvx.X * cvx.X + cvx.Y * cvx.Y);
                                    double hypoY = Math.Sqrt(cvy.X * cvy.X + cvy.Y * cvy.Y);
                                    double hypo = hypoX * hypoY;

                                    if ((cvx.X * cvy.X + cvx.Y * cvy.Y) / hypo < 0.01f)
                                    {
                                        /*PointF ptcX = new PointF(ptc.X + cvx.X, ptc.Y + cvx.Y);
                                        PointF ptcY = new PointF(ptc.X + cvy.X, ptc.Y + cvy.Y);
                                        PointF ptcXY = new PointF(ptc.X + cvx.X + cvy.X, ptc.Y + cvx.Y + cvy.Y);*/

                                        /*if (0 <= ptcX.X && ptcX.X < btmp.Width && 0 <= ptcX.Y && ptcX.Y < btmp.Height &&
                                            0 <= ptcY.X && ptcY.X < btmp.Width && 0 <= ptcY.Y && ptcY.Y < btmp.Height &&
                                            0 <= ptcXY.X && ptcXY.X < btmp.Width && 0 <= ptcXY.Y && ptcXY.Y < btmp.Height)*/
                                        {
                                            /*if (carteDetect.Count == 3)
                                            {
                                                svp1 = ptc;
                                                svp2 = ptcX;
                                                svp3 = ptcY;
                                            }*/

                                            /*Point pMin = new Point((int)(Math.Min(ptc.X, Math.Min(ptcX.X, Math.Min(ptcY.X, ptcXY.X))) + 0.5f), (int)(Math.Min(ptc.Y, Math.Min(ptcX.Y, Math.Min(ptcY.Y, ptcXY.Y))) + 0.5f));
                                            Point pMax = new Point((int)(Math.Max(ptc.X, Math.Max(ptcX.X, Math.Max(ptcY.X, ptcXY.X))) + 0.5f), (int)(Math.Max(ptc.Y, Math.Max(ptcX.Y, Math.Max(ptcY.Y, ptcXY.Y))) + 0.5f));*/

                                            Point pMin = new Point((int)Math.Min(ptc.X, ptu.X), (int)Math.Min(ptc.Y, ptu.Y));
                                            Point pMax = new Point((int)Math.Max(ptc.X, ptu.X), (int)Math.Max(ptc.Y, ptu.Y));

                                            rectExclu.Add(new Rectangle(pMin.X, pMin.Y, pMax.X - pMin.X, pMax.Y - pMin.Y));
                                            //carteDetect.Add((new Rectangle(pMin.X, pMin.Y, pMax.X - pMin.X, pMax.Y - pMin.Y), 0.0f));

                                            ptu.X += uvx.X + uvy.X;
                                            ptu.Y += uvx.Y + uvy.Y;

                                            PointF rpx = new PointF((ptc.X + ptu.X) / 2.0f, (ptc.Y + ptu.Y) / 2.0f);
                                            PointF rvx = new PointF((cvx.X - uvx.X) / 2.0f, (cvx.Y - uvx.Y) / 2.0f);
                                            PointF rvy = new PointF((cvy.X - uvy.X) / 2.0f, (cvy.Y - uvy.Y) / 2.0f);

                                            /*PointF rpx = ptc;
                                            PointF rvx = cvx;
                                            PointF rvy = cvy;*/

                                            hypoX = Math.Sqrt(rvx.X * rvx.X + rvx.Y * rvx.Y);
                                            double angX = -Math.Sign(rvx.Y) * Math.Acos(rvx.X / hypoX);

                                            hypoY = Math.Sqrt(rvy.X * rvy.X + rvy.Y * rvy.Y);
                                            double angY = Math.Sign(rvy.X) * Math.Acos(rvy.Y / hypoY);

                                            /*double hypoCX = Math.Sqrt(cvx.X * cvx.X + cvx.Y * cvx.Y);
                                            double angCX = -Math.Sign(cvx.Y) * Math.Acos(cvx.X / hypoCX);

                                            double hypoCY = Math.Sqrt(cvy.X * cvy.X + cvy.Y * cvy.Y);
                                            double angCY = Math.Sign(cvy.X) * Math.Acos(cvy.Y / hypoCY);*/

                                            /*double hypoUX = Math.Sqrt(uvx.X * uvx.X + uvx.Y * uvx.Y);
                                            double angUX = - Math.Sign(uvx.Y) * Math.Acos(uvx.X / hypoUX);

                                            double hypoUY = Math.Sqrt(uvy.X * uvy.X + uvy.Y * uvy.Y);
                                            double angUY = Math.Sign(uvy.X) * Math.Acos(uvy.Y / hypoUY);*/

                                            //double ang = (angCX * hypoCX + angCY * hypoCY + angUX * hypoUX + angUY * hypoUY) / (hypoCX + hypoCY + hypoUX + hypoUY);

                                            double ang = (angX * hypoX + angY * hypoY) / (hypoX + hypoY);

                                            double cos = Math.Cos(ang);
                                            double sin = Math.Sin(ang);

                                            Rectangle rDect = new Rectangle();
                                            rDect.X = (int)((cos * ptc.X - sin * ptc.Y) + 0.5f);
                                            rDect.Y = (int)((sin * ptc.X + cos * ptc.Y) + 0.5f);
                                            rDect.Width = 1 + (int)((cos * cvx.X + sin * cvx.Y) + 0.5f);
                                            rDect.Height = 1 + (int)((-sin * cvy.X + cos * cvy.Y) + 0.5f);

                                            /*double angC = (angCX * hypoCX + angCY * hypoCY) / (hypoCX + hypoCY);
                                            double cos = Math.Cos(angC);
                                            double sin = Math.Sin(angC);

                                            Rectangle rDectC = new Rectangle();
                                            rDectC.X = (int)((cos * ptc.X - sin * ptc.Y) + 0.5f);
                                            rDectC.Y = (int)((sin * ptc.X + cos * ptc.Y) + 0.5f);
                                            rDectC.Width = 1 + (int)((cos * cvx.X + sin * cvx.Y) + 0.5f);
                                            rDectC.Height = 1 + (int)((-sin * cvy.X + cos * cvy.Y) + 0.5f);*/

                                            /*double angU = (angUX * hypoUX + angUY * hypoUY) / (hypoUX + hypoUY);
                                            cos = Math.Cos(angU);
                                            sin = Math.Sin(angU);

                                            Rectangle rDectU = new Rectangle();
                                            rDectU.X = (int)((cos * ptu.X - sin * ptu.Y) + 0.5f);
                                            rDectU.Y = (int)((sin * ptu.X + cos * ptu.Y) + 0.5f);
                                            rDectU.Width = 1 + (int)((cos * uvx.X + sin * uvx.Y) + 0.5f);
                                            rDectU.Height = 1 + (int)((-sin * uvy.X + cos * uvy.Y) + 0.5f);*/

                                            //double ang = angU;
                                            //Rectangle rDect = rDectU;

                                            if (rDect.Width >= crtDMin && rDect.Height >= crtDMin)
                                            {
                                                lstRes.Add((rDect, (float)((ang * 180.0) / Math.PI)));
                                            }

                                            x = pMax.X + marg;
                                        }
                                    }
                                }
                            }
                            //else cref = CalculGrey(*(grePtrY + x));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { btmp.UnlockBits(btdt); }

            return lstRes;
        }

        private List<(Rectangle, float)> DétecterCartes(Bitmap btmp, ref uint? _cref, int seuil = 100, int marg = 10, int crtDMin = 50)
        {
            List<(Rectangle, float)>  res = new List<(Rectangle, float)>();
            BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    uint *grePtr = (uint*)btdt.Scan0.ToPointer();
                    uint cref;
                    if (_cref == null)
                    {
                        if (btmp.Width <= marg || btmp.Height <= marg) cref = *grePtr;
                        else cref = *(grePtr + marg * (1 + btmp.Width));
                        _cref = cref;
                    }
                    else cref = _cref.Value;
                    for (int y = 0; y < btmp.Height; ++y)
                    {
                        uint* grePtrY = grePtr + btmp.Width * y;
                        IEnumerable<(Rectangle, float)> rectY = res.Where(r => (r.Item1.Y - marg <= y && y <= r.Item1.Y + r.Item1.Height + marg));
                        for (int x = 0; x < btmp.Width; x+= marg)
                        {
                            if (carteDétectTaskCT != null && carteDétectTaskCT.IsCancellationRequested) carteDétectTaskCT.Token.ThrowIfCancellationRequested();

                            Rectangle rect;
                            float angle;
                            (rect, angle) = rectY.FirstOrDefault(r => (r.Item1.X - marg <= x && x <= r.Item1.X + r.Item1.Width + marg));
                            if (rect.Width > 0 && rect.Height > 0) x = rect.X + rect.Width + marg;
                            else if (CalculDist(cref, *(grePtrY + x)) > seuil && (*(grePtrY + x) >> 24) != 0)
                            {
                                int rh;
                                for (rh = 0; y + rh < btmp.Height; ++rh)
                                {
                                    uint* loc_grePtrY = grePtrY + btmp.Width * rh;
                                    int scanx;
                                    for (scanx = -marg; scanx <= marg; ++scanx)
                                        if (0 <= (x + scanx) && (x + scanx) < btmp.Width && (CalculDist(cref, *(loc_grePtrY + x + scanx)) > seuil)) break;
                                    if (scanx >= marg) break;
                                }
                                if(rh >= crtDMin)
                                {
                                    int rw;
                                    for (rw = 0; x + rw < btmp.Width; ++rw)
                                    {
                                        uint* loc_grePtrX = grePtrY + x + rw;
                                        int scany;
                                        for (scany = -marg; scany <= marg; ++scany)
                                            if (0 <= (y + scany) && (y + scany) < btmp.Height && (CalculDist(cref,*(loc_grePtrX + btmp.Width * scany)) > seuil)) break;
                                        if (scany >= marg) break;
                                    }

                                    if (rw >= crtDMin)
                                    {
                                        int rx = x - 1;
                                        for (int scany = 0; scany < rh; ++scany)
                                        {
                                            uint* loc_grePtrY = grePtrY + btmp.Width * scany;
                                            for (; 0 < rx && (CalculDist(cref, *(loc_grePtrY + rx)) > seuil); --rx) ;
                                        }
                                        rx += 1;
                                        rw += (x - rx);

                                        for (int scany = 0; scany < rh; ++scany)
                                        {
                                            uint* loc_grePtrY = grePtrY + btmp.Width * scany;
                                            for (; (rx + rw) < btmp.Width && (CalculDist(cref, *(loc_grePtrY + (rx + rw))) > seuil); ++rw) ;
                                        }

                                        int ry = y - 1;
                                        for (int scanx = 0; scanx < rw; ++scanx)
                                        {
                                            uint* loc_grePtrX = grePtr  + rx + scanx;
                                            for (; 0 < ry && (CalculDist(cref, *(loc_grePtrX + ry * btmp.Width)) > seuil); --ry) ;
                                        }
                                        ry += 1;
                                        rh += (y - ry);

                                        for (int scanx = 0; scanx < rw; ++scanx)
                                        {
                                            uint* loc_grePtrX = grePtr + rx + scanx;
                                            for (; (ry + rh) < btmp.Height && (CalculDist(cref, *(loc_grePtrX + (ry + rh) * btmp.Width)) > seuil); ++rh) ;
                                        }

                                        res.Add((new Rectangle(rx, ry, rw, rh), 0.0f));
                                        x = rx + rw + marg;
                                    }
                                }
                            }
                            //else cref = CalculGrey(*(grePtrY + x));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { btmp.UnlockBits(btdt); }
            return res;
        }

        protected Graphics obtenirGraphics()
        {
            Graphics g = this.CreateGraphics();
            float w2 = Width / 2.0f;
            float h2 = Height / 2.0f;
            g.TranslateTransform(w2, h2);
            g.ScaleTransform(echel, echel);
            g.TranslateTransform(-p.X, -p.Y);
            return g;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //Redessiner(e.Graphics);
            Graphics g = e.Graphics;
            float w2 = Width / 2.0f;
            float h2 = Height / 2.0f;
            g.TranslateTransform(w2, h2);
            g.ScaleTransform(echel, echel);
            g.TranslateTransform(-p.X, -p.Y);
            //if(image != null)g.DrawImage(image, p.X, p.Y, new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            if (image != null)
            {
                System.Drawing.Drawing2D.Matrix m = g.Transform;
                if(angle != 0.0f) g.RotateTransform(angle);
                g.DrawImage(image, 0, 0);
                //g.DrawEllipse(new Pen(Color.Blue), svp.X - 2, svp.Y - 2, 4, 4);
                /*g.DrawLine(new Pen(Color.Blue), svp1, svp2);
                g.DrawLine(new Pen(Color.Blue), svp1, svp3);*/
                /*if(lstPts != null)
                {
                    lstPts.ForEach(l =>
                    {
                        l.ForEach(pt => g.DrawEllipse(new Pen(pt.Item2), pt.Item1.X - 2, pt.Item1.Y - 2, 4, 4));
                    });
                }*/
                g.Transform = m;
            }
            g.DrawLine(new Pen(Color.Red), pLignes[0], p.Y - h2 / echel, pLignes[0], p.Y + h2 / echel);
            g.DrawLine(new Pen(Color.Red), pLignes[1], p.Y - h2 / echel, pLignes[1], p.Y + h2 / echel);
            g.DrawLine(new Pen(Color.Red), p.X - w2 / echel, pLignes[2], p.X + w2 / echel, pLignes[2]);
            g.DrawLine(new Pen(Color.Red), p.X - w2 / echel, pLignes[3], p.X + w2 / echel, pLignes[3]);
        }

        private void BoardGameFabrique_MouseDown(object sender, MouseEventArgs e)
        {
            //Point np = new Point((int)(((e.Location.X - Width / 2.0f) / echel + p.X) + 0.5f), (int)(((e.Location.Y - Height / 2.0f) / echel + p.Y) + 0.5f));
            PointF np = new PointF((((e.Location.X - Width / 2.0f) / echel + p.X)), (((e.Location.Y - Height / 2.0f) / echel + p.Y)));
            if (e.Button.HasFlag(MouseButtons.Right))
            {
                rightDownPoint = e.Location;
                oldRPoint = np;
            }
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                if(idxLigne < 0)
                {
                    Point pc = new Point((int)(np.X - (pLignes[0] + pLignes[1]) / 2.0f + 0.5f), (int)(np.Y - (pLignes[2] + pLignes[3]) / 2.0f + 0.5f));
                    pLignes[0] += pc.X;
                    pLignes[1] += pc.X;
                    pLignes[2] += pc.Y;
                    pLignes[3] += pc.Y;
                }
                else if (idxLigne < 2) pLignes[idxLigne] = (int)np.X;
                else pLignes[idxLigne] = (int)np.Y;
                Refresh();
            }
        }

        private void BoardGameFabrique_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) || e.Button.HasFlag(MouseButtons.Left))
            {
                PointF np = new PointF((((e.Location.X - Width/2.0f) / echel + p.X) ), (((e.Location.Y - Height/2.0f) / echel + p.Y) ));
                if (e.Button.HasFlag(MouseButtons.Right))
                {
                    p.X -= np.X - oldRPoint.X;
                    p.Y -= np.Y - oldRPoint.Y;
                    Refresh();
                }
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    if (idxLigne < 0)
                    {
                        Point pc = new Point((int)(np.X - (pLignes[0] + pLignes[1]) / 2.0f + 0.5f), (int)(np.Y - (pLignes[2] + pLignes[3]) / 2.0f + 0.5f));
                        pLignes[0] += pc.X;
                        pLignes[1] += pc.X;
                        pLignes[2] += pc.Y;
                        pLignes[3] += pc.Y;
                    }
                    else if (idxLigne < 2) pLignes[idxLigne] = (int)np.X;
                    else pLignes[idxLigne] = (int)np.Y;
                    Refresh();
                }
            }
        }

        static public float AngleFromToAimant(float angFrom, float angTo, float delta = 45.0f)
        {
            float a = Math.Min(angFrom, angTo);
            float b = Math.Max(angFrom, angTo);

            for (float ang = -360; ang <= 360; ang += delta)
                if (a < ang && ang < b)
                {
                    ang %= 360;
                    if (ang < 0) ang = 360 + ang;
                    return ang;
                }

            angTo %= 360;
            if (angTo < 0) angTo = 360 + angTo;
            return angTo;
        }

        private void Transparence(Bitmap btmp, Bitmap nbtmapDetect, int seuil, uint? _cref, uint? coulTrans = null)
        {
            uint ctrans;
            BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData btdtDtect = nbtmapDetect.LockBits(new Rectangle(0, 0, nbtmapDetect.Width, nbtmapDetect.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    uint* grePtr = (uint*)btdt.Scan0.ToPointer();
                    uint* grePtrDtect = (uint*)btdtDtect.Scan0.ToPointer();
                    uint cref;
                    if (_cref != null) cref = _cref.Value;
                    else
                    {
                        cref = *grePtrDtect;
                        _cref = cref;
                    }

                    if (coulTrans != null)
                    {
                        ctrans = coulTrans.Value;
                        ctrans &= 0xFFFFFF;
                    }
                    else
                    {
                        /*byte gris = (byte)((((cref >> 16) & 0xFF) + ((cref >> 8) & 0xFF) + ((cref >> 0) & 0xFF) + 1) / 3);
                        if (gris < 128) ctrans = 0;
                        else ctrans = 0xFFFFFF;*/
                        ctrans = CouleurTransparenceRéduite(cref);
                    }

                    int x, y;
                    y = btmp.Height - 1;
                    for(x = 0; x < btmp.Width; ++x)
                    {
                        for (int sY = 0; sY < btmp.Height && CalculDist(cref, *(grePtrDtect + x + sY * btmp.Width)) < seuil; ++sY)
                        {
                            *(grePtr + x + sY * btmp.Width) = ctrans;
                            *(grePtrDtect + x + sY * btmp.Width) = ctrans;
                        }

                        for (int sY = y; sY >= 0 && CalculDist(cref, *(grePtrDtect + x + sY * btmp.Width)) < seuil; --sY)
                        {
                            *(grePtr + x + sY * btmp.Width) = ctrans;
                            *(grePtrDtect + x + sY * btmp.Width) = ctrans;
                        }
                    }

                    x = btmp.Width - 1;
                    for (y = 0; y < btmp.Height; ++y)
                    {
                        for (int sX = 0; sX < btmp.Width && CalculDist(cref, *(grePtrDtect + sX + y * btmp.Width)) < seuil; ++sX)
                        {
                            *(grePtr + sX + y * btmp.Width) = ctrans;
                            *(grePtrDtect + sX + y * btmp.Width) = ctrans;
                        }

                        for (int sX = x; sX >= 0 && CalculDist(cref, *(grePtrDtect + sX + y * btmp.Width)) < seuil; --sX)
                        {
                            *(grePtr + sX + y * btmp.Width) = ctrans;
                            *(grePtrDtect + sX + y * btmp.Width) = ctrans;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { btmp.UnlockBits(btdt); nbtmapDetect.UnlockBits(btdtDtect); }
        }

        private void Restauration(Bitmap btmp, Bitmap nbtmapDetect, byte seuil = 255)
        {
            uint ctrans;
            BitmapData btdt = btmp.LockBits(new Rectangle(0, 0, btmp.Width, btmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            BitmapData btdtDtect = nbtmapDetect.LockBits(new Rectangle(0, 0, nbtmapDetect.Width, nbtmapDetect.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                unsafe
                {
                    uint* grePtr = (uint*)btdt.Scan0.ToPointer();
                    uint* grePtrDtect = (uint*)btdtDtect.Scan0.ToPointer();

                    int x, y;
                    y = btmp.Height - 1;
                    for (x = 0; x < btmp.Width; ++x)
                    {
                        for (int sY = 0; sY < btmp.Height && ((*(grePtrDtect + x + sY * btmp.Width)) & 0xFF000000) < seuil; ++sY)
                        {
                            *(grePtr + x + sY * btmp.Width) = *(grePtrDtect + x + sY * btmp.Width) | 0xFF000000;
                        }

                        for (int sY = y; sY >= 0 && ((*(grePtrDtect + x + sY * btmp.Width)) & 0xFF000000) < seuil; --sY)
                        {
                            *(grePtr + x + sY * btmp.Width) = *(grePtrDtect + x + sY * btmp.Width) | 0xFF000000;
                        }
                    }

                    x = btmp.Width - 1;
                    for (y = 0; y < btmp.Height; ++y)
                    {
                        for (int sX = 0; sX < btmp.Width && ((*(grePtrDtect + sX + y * btmp.Width)) & 0xFF000000) < seuil; ++sX)
                        {
                            *(grePtr + sX + y * btmp.Width) = *(grePtrDtect + sX + y * btmp.Width) | 0xFF000000;
                        }

                        for (int sX = x; sX >= 0 && ((*(grePtrDtect + sX + y * btmp.Width)) & 0xFF000000) < seuil; --sX)
                        {
                            *(grePtr + sX + y * btmp.Width) = *(grePtrDtect + sX + y * btmp.Width) | 0xFF000000;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { btmp.UnlockBits(btdt); nbtmapDetect.UnlockBits(btdtDtect); }
        }

        private void ArrondirCoins(Bitmap nbtmap, int arrondiCoins, uint? coulTrans = null)
        {
            int r = (arrondiCoins * Math.Min(nbtmap.Width, nbtmap.Height)) / 2000;
            if (r > 1)
            {
                uint ctrans;
                BitmapData btdt = nbtmap.LockBits(new Rectangle(0, 0, nbtmap.Width, nbtmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                try
                {
                    unsafe
                    {
                        uint* grePtr = (uint*)btdt.Scan0.ToPointer();

                        if (coulTrans != null)
                        {
                            ctrans = coulTrans.Value;
                            ctrans &= 0xFFFFFF;
                        }
                        else
                        {
                            /*byte gris = (byte)((((cref >> 16) & 0xFF) + ((cref >> 8) & 0xFF) + ((cref >> 0) & 0xFF) + 1) / 3);
                            if (gris < 128) ctrans = 0;
                            else ctrans = 0xFFFFFF;*/
                            ctrans = CouleurTransparenceRéduite(*grePtr);
                        }

                        double r2 = r * r;
                        for(int j = 0; j <= r; ++j)
                        {
                            int l = r - (int)((r * Math.Sqrt(1.0 - ((r - j) * (r - j)) / r2)) + 0.5);

                            for(int i = 0; i < l; ++i)
                            {
                                *(grePtr + (i) + (j) * nbtmap.Width) = ctrans;
                                *(grePtr + (nbtmap.Width - 1 - i) + (j) * nbtmap.Width) = ctrans;
                                *(grePtr + (i) + (nbtmap.Height - 1 - j) * nbtmap.Width) = ctrans;
                                *(grePtr + (nbtmap.Width - 1 - i) + (nbtmap.Height - 1 - j) * nbtmap.Width) = ctrans;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { nbtmap.UnlockBits(btdt); }
            }
        }

        private void Capturer(int x, int y, int w, int h, float _angle)
        {
            if (w > 0 && h > 0 && image != null)
            {
                try
                {
                    uint? colTrans;
                    if (transCref != null) colTrans = transCref;
                    else if (gcref != null) colTrans = CouleurTransparenceRéduite(gcref.Value);
                    else colTrans = CouleurTransparenceRéduite(image as Bitmap);
                    if (activerDoubleDétect != DetectAlgo.Vide)
                    {
                        x -= marg; y -= marg;
                        w += 2 * marg; h += 2 * marg;
                        Bitmap nbtmap = new Bitmap(w, h);
                        using (Graphics g = Graphics.FromImage(nbtmap))
                        {
                            if(colTrans != null) g.Clear(Color.FromArgb((int)colTrans.Value));
                            else g.Clear(Color.Transparent);
                            if (_angle != 0.0f)
                            {
                                g.RotateTransform(_angle);

                                double ang = -(_angle * Math.PI) / 180.0;
                                double cos = Math.Cos(ang);
                                double sin = Math.Sin(ang);

                                float fx1, fy1;
                                fx1 = (float)(cos * x - sin * y);
                                fy1 = (float)(sin * x + cos * y);
                                g.DrawImage(image, -fx1, -fy1);
                            }
                            else g.DrawImage(image, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                        }

                        Bitmap nbtmapDetect = new Bitmap(w, h);
                        using (Graphics g = Graphics.FromImage(nbtmapDetect))
                        {
                            g.Clear(Color.Transparent);
                            if (_angle != 0.0f)
                            {
                                g.RotateTransform(_angle);

                                double ang = -(_angle * Math.PI) / 180.0;
                                double cos = Math.Cos(ang);
                                double sin = Math.Sin(ang);

                                float fx1, fy1;
                                fx1 = (float)(cos * x - sin * y);
                                fy1 = (float)(sin * x + cos * y);
                                g.DrawImage(imageDetect, -fx1, -fy1);
                            }
                            else g.DrawImage(imageDetect, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                        }

                        //if (activerTransparence) Transparence(nbtmap, nbtmapDetect, seuilTransparence, gcref, colTrans);

                        List<(Rectangle, float)> carte;
                        if (activerDoubleDétect == DetectAlgo.Avancé) carte = DétecterCartesAngle(nbtmapDetect, ref gcref, seuilDepar, seuil, marg, crtDMin, arrondiCoins);
                        else if (activerDoubleDétect == DetectAlgo.Basique) carte = DétecterCartes(nbtmapDetect, ref gcref, seuilDepar, marg, crtDMin);
                        else carte = new List<(Rectangle, float)>();
                        if (carte.Any())
                        {
                            var cdd = carte.First();
                            Bitmap ddBtmap;
                            ddBtmap = new Bitmap(cdd.Item1.Width, cdd.Item1.Height);
                            using (Graphics g = Graphics.FromImage(ddBtmap))
                            {
                                if (colTrans != null) g.Clear(Color.FromArgb((int)colTrans.Value));
                                else g.Clear(Color.Transparent);
                                if (cdd.Item2 != 0.0f)
                                {
                                    g.RotateTransform(cdd.Item2);

                                    double ang = -(cdd.Item2 * Math.PI) / 180.0;
                                    double cos = Math.Cos(ang);
                                    double sin = Math.Sin(ang);

                                    float fx1, fy1;
                                    fx1 = (float)(cos * cdd.Item1.X - sin * cdd.Item1.Y);
                                    fy1 = (float)(sin * cdd.Item1.X + cos * cdd.Item1.Y);
                                    g.DrawImage(nbtmap, -fx1, -fy1);
                                }
                                else g.DrawImage(nbtmap, new Rectangle(0, 0, cdd.Item1.Width, cdd.Item1.Height), new Rectangle(cdd.Item1.X, cdd.Item1.Y, cdd.Item1.Width, cdd.Item1.Height), GraphicsUnit.Pixel);
                            }

                            Bitmap ddBtmapDetct;
                            ddBtmapDetct = new Bitmap(cdd.Item1.Width, cdd.Item1.Height);
                            using (Graphics g = Graphics.FromImage(ddBtmapDetct))
                            {
                                if (colTrans != null) g.Clear(Color.FromArgb((int)colTrans.Value));
                                else g.Clear(Color.Transparent);
                                if (cdd.Item2 != 0.0f)
                                {
                                    g.RotateTransform(cdd.Item2);

                                    double ang = -(cdd.Item2 * Math.PI) / 180.0;
                                    double cos = Math.Cos(ang);
                                    double sin = Math.Sin(ang);

                                    float fx1, fy1;
                                    fx1 = (float)(cos * cdd.Item1.X - sin * cdd.Item1.Y);
                                    fy1 = (float)(sin * cdd.Item1.X + cos * cdd.Item1.Y);
                                    g.DrawImage(nbtmapDetect, -fx1, -fy1);
                                }
                                else g.DrawImage(nbtmapDetect, new Rectangle(0, 0, cdd.Item1.Width, cdd.Item1.Height), new Rectangle(cdd.Item1.X, cdd.Item1.Y, cdd.Item1.Width, cdd.Item1.Height), GraphicsUnit.Pixel);
                            }
                            if (activerTransparence) Transparence(ddBtmap, ddBtmapDetct, seuilTransparence, gcref, colTrans);
                            else Restauration(ddBtmap, ddBtmapDetct);
                            if (arrondiCoins > 0) ArrondirCoins(ddBtmap, arrondiCoins, colTrans);
                            imgs.Add((ETransformation.Identity, ddBtmap, 1));
                        }
                        else
                        {
                            if (arrondiCoins > 0) ArrondirCoins(nbtmap, arrondiCoins, colTrans);
                            imgs.Add((ETransformation.Identity, nbtmap, 1));
                        }
                    }
                    else
                    {
                        Bitmap nbtmap = new Bitmap(w, h);
                        using (Graphics g = Graphics.FromImage(nbtmap))
                        {
                            if (colTrans != null) g.Clear(Color.FromArgb((int)colTrans.Value));
                            else g.Clear(Color.Transparent);
                            if (_angle != 0.0f)
                            {
                                g.RotateTransform(_angle);

                                double ang = -(_angle * Math.PI) / 180.0;
                                double cos = Math.Cos(ang);
                                double sin = Math.Sin(ang);

                                float fx1, fy1;
                                fx1 = (float)(cos * x - sin * y);
                                fy1 = (float)(sin * x + cos * y);
                                g.DrawImage(image, -fx1, -fy1);
                            }
                            else g.DrawImage(image, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                        }

                        Bitmap nbtmapDetect = new Bitmap(w, h);
                        using (Graphics g = Graphics.FromImage(nbtmapDetect))
                        {
                            if (colTrans != null) g.Clear(Color.FromArgb((int)colTrans.Value));
                            else g.Clear(Color.Transparent);
                            if (_angle != 0.0f)
                            {
                                g.RotateTransform(_angle);

                                double ang = -(_angle * Math.PI) / 180.0;
                                double cos = Math.Cos(ang);
                                double sin = Math.Sin(ang);

                                float fx1, fy1;
                                fx1 = (float)(cos * x - sin * y);
                                fy1 = (float)(sin * x + cos * y);
                                g.DrawImage(imageDetect, -fx1, -fy1);
                            }
                            else g.DrawImage(imageDetect, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                        }
                        if (activerTransparence) Transparence(nbtmap, nbtmapDetect, seuilTransparence, gcref, colTrans);
                        else Restauration(nbtmap, nbtmapDetect);
                        if (arrondiCoins > 0) ArrondirCoins(nbtmap, arrondiCoins, colTrans);
                        imgs.Add((ETransformation.Identity, nbtmap, 1));
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Exception",  MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

        }

        private void BoardGameFabrique_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '1' || e.KeyChar == '&') idxLigne = 0;
            else if (e.KeyChar == '2' || e.KeyChar == 'é') idxLigne = 1;
            else if (e.KeyChar == '3' || e.KeyChar == '"') idxLigne = 2;
            else if (e.KeyChar == '4' || e.KeyChar == '\'') idxLigne = 3;
            else if (e.KeyChar == '5' || e.KeyChar == '(') idxLigne = -1;
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
                    Capturer(x1, y1, w, h, angle);
                    visualiseur.AjoutSuppresCarte();
                }
                else MessageBox.Show("Impossible !");
            }
            else if(e.KeyChar == '\r')
            {
                if (image != null && carteDetect != null && carteDetect.Any())
                {
                    foreach(var cd in carteDetect)
                    {
                        Capturer(cd.Item1.X, cd.Item1.Y, cd.Item1.Width, cd.Item1.Height, cd.Item2);
                    }
                    visualiseur.AjoutSuppresCarte();
                }
                else MessageBox.Show("Impossible !");
            }
            else if(e.KeyChar == '\b')
            {
                if (imgs.Count > 0)
                {
                    imgs.RemoveAt(imgs.Count - 1);
                    visualiseur.AjoutSuppresCarte();
                }
                else MessageBox.Show("Impossible !");
            }
            else if(e.KeyChar == '+' || e.KeyChar == '=')
            {
                if (shift) angle = AngleFromToAimant(angle, angle + 10.0f, 90.0f);
                else angle = AngleFromToAimant(angle, angle + 0.1f, 45.0f); 
                Refresh();
            }
            else if (e.KeyChar == '-' || e.KeyChar == '6')
            {
                if (shift) angle = AngleFromToAimant(angle, angle - 10.0f, 90.0f);
                else angle = AngleFromToAimant(angle, angle - 0.1f, 45.0f); 
                Refresh();
            }
        }

        private void UpdateCrdDetect()
        {
            if (carteDetect.Any())
            {
                if (carteDetect.Count <= curCDIdx) curCDIdx = carteDetect.Count - 1;
                 pLignes[0] = carteDetect[curCDIdx].Item1.X;
                pLignes[1] = carteDetect[curCDIdx].Item1.X + carteDetect[curCDIdx].Item1.Width - 1;
                pLignes[2] = carteDetect[curCDIdx].Item1.Y;
                pLignes[3] = carteDetect[curCDIdx].Item1.Y + carteDetect[curCDIdx].Item1.Height - 1;
                angle = carteDetect[curCDIdx].Item2;
                Refresh();
            }
        }

        private void BoardGameFabrique_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == (Keys.Up))
            {
                if (idxLigne < 0)
                {
                    pLignes[2]--;
                    pLignes[3]--;
                    Refresh();
                }
                else if(idxLigne >= 2)
                {
                    pLignes[idxLigne]--;
                    Refresh();
                }
            }
            else if (e.KeyCode == (Keys.Down))
            {
                if (idxLigne < 0)
                {
                    pLignes[2]++;
                    pLignes[3]++;
                    Refresh();
                }
                else if(idxLigne >= 2)
                {
                    pLignes[idxLigne]++;
                    Refresh();
                }
            }
            else if (e.KeyCode == (Keys.Left))
            {
                if (idxLigne < 0)
                {
                    pLignes[0]--;
                    pLignes[1]--;
                    Refresh();
                }
                else if(idxLigne < 2)
                {
                    pLignes[idxLigne]--;
                    Refresh();
                }
            }
            else if (e.KeyCode == (Keys.Right))
            {
                if (idxLigne < 0)
                {
                    pLignes[0]++;
                    pLignes[1]++;
                    Refresh();
                }
                else if(idxLigne < 2)
                {
                    pLignes[idxLigne]++;
                    Refresh();
                }
            }
            else if (e.KeyCode == Keys.PageUp)
            {
                if (carteDetect != null && carteDetect.Any())
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
            ctrl = e.Control;
            shift = e.Shift;
        }

        private void DécomposerLigneColonne()
        {
            Décomposer décomp = new Décomposer();
            if(image != null && décomp.ShowDialog(this) == DialogResult.OK)
            {
                uint? colTrans;
                if (transCref != null) colTrans = transCref;
                else colTrans = CouleurTransparenceRéduite(image as Bitmap);

                int w = image.Width / décomp.nbColonnes;
                int h = image.Height / décomp.nbLignes;
                for (int i = 0; i < décomp.nbCartes; ++i)
                {
                    int x = w * (i % décomp.nbColonnes);
                    int y = h * (i / décomp.nbColonnes);
                    Bitmap nbtmap = new Bitmap(w, h);
                    Graphics g = Graphics.FromImage(nbtmap);
                    g.DrawImage(image, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                    Bitmap nbtmapDtct = new Bitmap(w, h);
                    g = Graphics.FromImage(nbtmapDtct);
                    g.DrawImage(imageDetect, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
                    if (activerTransparence) Transparence(nbtmap, nbtmapDtct, seuilTransparence, gcref, colTrans);
                    else Restauration(nbtmap, nbtmapDtct);
                    if (arrondiCoins > 0) ArrondirCoins(nbtmap, arrondiCoins, colTrans);
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

        private void GénérerXMLPilePioche(string fichierImg, string fichierDos, int nbl, int nbc, int w, int h, bool pioche, bool défausse, int coin = 0, uint transColor = 0)
        {
            string baliseNm;
            if(pioche)
            {
                baliseNm = "PIOCHE";
            }
            else
            {
                baliseNm = "PILE";
                défausse = false;
            }

            string strTrans = "";
            if (transColor == 0) strTrans += " opaque=\"\"";
            if (coin > 0) strTrans += $" coin=\"{coin}\"";

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

            string strTransDos;
            int dx = 0, dy = 0;
            if (fichierDos == null && idxVide >= 0)
            {
                fichierDos = fichierImg;
                dx = (idxVide % nbc) * w;
                dy = (idxVide / nbc) * h;
            }

            int nb;
            if (imgs.Last().Item3 <= 0) nb = imgs.Count - 1;
            else nb = imgs.Count;

            bool imgQty = imgs.Any(img => (img.Item3 > 1));

            string strDéfauss;
            if (défausse) strDéfauss = $" défausse=\"{nom}Dfs\"";
            else strDéfauss = "";

            string XML = "";
            if(défausse) XML += "<GROUPE>\r\n";
            if (fichierDos != null) XML += $"<{baliseNm} nom=\"{nom}Pch\" vide=\"{fichierDos}\"{strDéfauss} mélanger=\"\" dx=\"{dx}\" dy=\"{dy}\" w=\"{w}\" h=\"{h}\"{strTrans} x=\"{(défausse?-70:0)}\" y=\"0\">\r\n";
            else XML += $"<{baliseNm} nom=\"{nom}Pch\"{strDéfauss} mélanger=\"\" w=\"{w}\" h=\"{h}\" x=\"{(défausse?-70:0)}\" y=\"0\">\r\n";
            if (imgQty)
            {
                if (imgs.Count == 1)
                {
                    if(imgs[0].Item3 > 1) XML += $"\t<CARTE img=\"{fichierImg}\" quantite=\"{imgs[0].Item3}\" w=\"{w}\" h=\"{h}\"{strTrans}/>\r\n";
                    else XML += $"\t<CARTE img=\"{fichierImg}\" w=\"{w}\" h=\"{h}\"{strTrans}/>\r\n";
                }
                else
                {
                    XML += $"\t<CARTES img=\"{fichierImg}\" nb=\"{nb}\" nbc=\"{nbc}\" nbl=\"{nbl}\"{strTrans}>\r\n";

                    for (int i = 0; i < imgs.Count; ++i)
                    {
                        if (imgs[i].Item3 > 1) XML += $"\t\t<QUANTITE id=\"{i}\" qt=\"{imgs[i].Item3}\"/>\r\n";
                    }
                    XML += "\t</CARTES>\r\n";
                }
            }
            else XML += $"\t<CARTES img=\"{fichierImg}\" nb=\"{nb}\" nbc=\"{nbc}\" nbl=\"{nbl}\"{strTrans}/>\r\n";
            XML += $"</{baliseNm}>\r\n";
            if(défausse)
            {
                if (fichierDos != null) XML += $"<DEFFAUSE nom=\"{nom}Dfs\" vide=\"{fichierDos}\" pioche=\"{nom}Pch\" dx=\"{dx}\" dy=\"{dy}\" w=\"{w}\" h=\"{h}\"{strTrans} x=\"70\" y=\"0\"/>\r\n";
                else XML += $"<DEFFAUSE nom=\"{nom}Dfs\" pioche=\"{nom}Pch\" w=\"{w}\" h=\"{h}\" x=\"70\" y=\"0\"/>\r\n";
            }
            if (défausse) XML += "</GROUPE>\r\n";

            File.WriteAllText(fichierXML, XML, Encoding.UTF8);
        }

        private void GénérerXMLDé(string fichierImg, int nbl, int nbc, int w, int h, bool modificable = false, int coin = 0, uint transColor = 0)
        {
            string baliseNm;
            if (modificable)
            {
                baliseNm = "DES";
            }
            else
            {
                baliseNm = "DES";
            }

            string strTrans = "";
            if (transColor == 0) strTrans += " opaque=\"\"";
            if (coin > 0) strTrans += $" coin=\"{coin}\"";

            string fichierXML;
            int idxLstP = fichierImg.LastIndexOf('.');
            int idxSlash = Math.Max(fichierImg.LastIndexOf('/'), fichierImg.LastIndexOf('\\'));
            if (idxLstP < idxSlash) idxLstP = -1;

            if (idxLstP >= 0) fichierXML = fichierImg.Substring(0, idxLstP) + ".XML";
            else fichierXML = fichierImg + ".XML";

            if (idxSlash >= 0) fichierImg = fichierImg.Substring(idxSlash + 1);
            string nom;
            idxLstP = fichierImg.LastIndexOf('.');
            if (idxLstP > 0) nom = fichierImg.Substring(0, idxLstP);
            else if (idxLstP == 0 || String.IsNullOrWhiteSpace(fichierImg)) nom = "xxx";
            else nom = fichierImg;

            int dx = 0, dy = 0;

            int nb;
            if (imgs.Last().Item3 <= 0) nb = imgs.Count - 1;
            else nb = imgs.Count;

            bool imgQty = imgs.Any(img => (img.Item3 > 1));

            string XML = "";
            XML += $"<{baliseNm} nom=\"{nom}\" w=\"{w}\" h=\"{h}\" x=\"0\" y=\"0\">\r\n";
            if (imgQty)
            {
                XML += $"\t<FACES img=\"{fichierImg}\" nb=\"{nb}\" nbc=\"{nbc}\" nbl=\"{nbl}\"{strTrans}>\r\n";

                for (int i = 0; i < imgs.Count; ++i)
                {
                    if (imgs[i].Item3 > 1) XML += $"\t\t<QUANTITE id=\"{i}\" qt=\"{imgs[i].Item3}\"/>\r\n";
                }
                XML += "\t</FACES>\r\n";
            }
            else XML += $"\t<FACES img=\"{fichierImg}\" nb=\"{nb}\" nbc=\"{nbc}\" nbl=\"{nbl}\"{strTrans}/>\r\n";
            XML += $"</{baliseNm}>\r\n";

            File.WriteAllText(fichierXML, XML, Encoding.UTF8);
        }

        static private uint[] TransPrimCoul =
        {
            (uint)Color.Black.ToArgb(),
            (uint)Color.White.ToArgb(),
            (uint)Color.Red.ToArgb(),
            (uint)Color.Green.ToArgb(),
            (uint)Color.Blue.ToArgb(),
            (uint)Color.Yellow.ToArgb(),
            (uint)Color.Magenta.ToArgb(),
            (uint)Color.Cyan.ToArgb()
        };

        /// <summary>
        /// Permet de trouver la couleur transparente primaire ou secondaire proche de celle passée en paramètre.
        /// </summary>
        /// <returns></returns>
        private uint CouleurTransparenceRéduite(uint cref)
        {
            int dmin = int.MaxValue;
            uint res = 0;
            cref |= (uint)0xFF << 24;
            foreach (uint col in TransPrimCoul)
            {
                int d = CalculDist(cref, col);
                if(d < dmin)
                {
                    dmin = d;
                    res = col;
                }
            }
            res &= 0xFFFFFF;
            return res;
        }

        private void Sauvegarder(uint? colTrans = null)
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
                    uint colorT;
                    if (colTrans != null) colorT = colTrans.Value;
                    else colorT = CouleurTransparenceRéduite(imgs.First().Item2 as Bitmap);
                    if (selectFormat.Guid == ImageFormat.Jpeg.Guid) colorT |= ((uint)0xFF << 24);
                    g.Clear(Color.FromArgb((int)colorT));
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
                    if(selectFormat.Guid == ImageFormat.Jpeg.Guid)
                    {
                        byte[] prop = new byte[1 + 2 + 4];
                        prop[0] = (byte)svg.Qualité;
                        Array.Copy(BitConverter.GetBytes((ushort)arrondiCoins), 0, prop, 1, 2);
                        Array.Copy(BitConverter.GetBytes((uint)(activerTransparence ? colorT : 0)), 0, prop, 1+2, 4);

                        var newItem = (PropertyItem)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(PropertyItem));
                        newItem.Id = 0;
                        newItem.Len = prop.Length;
                        newItem.Type = 1;
                        newItem.Value = prop;
                        btmap.SetPropertyItem(newItem);
                    }
                    btmap.Save(saveFileDialog.FileName, Encoder, myEncoderParameters);

                    uint transParam = (activerTransparence ? colorT : 0);
                    switch(svg.xmlGen)
                    {
                        case EXMLGen.pile:
                            GénérerXMLPilePioche(saveFileDialog.FileName, svg.ImageDos, nbL, nbC, tailleCarteW, tailleCarteH, false, false, arrondiCoins, transParam);
                            break;
                        case EXMLGen.pioche:
                            GénérerXMLPilePioche(saveFileDialog.FileName, svg.ImageDos, nbL, nbC, tailleCarteW, tailleCarteH, true, false, arrondiCoins, transParam);
                            break;
                        case EXMLGen.piocheETdéfausse:
                            GénérerXMLPilePioche(saveFileDialog.FileName, svg.ImageDos, nbL, nbC, tailleCarteW, tailleCarteH, true, true, arrondiCoins, transParam);
                            break;
                        case EXMLGen.dé:
                            GénérerXMLDé(saveFileDialog.FileName, nbL, nbC, tailleCarteW, tailleCarteH, false, arrondiCoins, transParam);
                            break;
                    }
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
                    cm.MenuItems.Add(new MenuItem("Réglages", (o, eArg) =>
                    {
                        Réglage rg = new Réglage(seuilDepar, seuil, marg, crtDMin, activerDétect, activerDoubleDétect, pcref, activerTransparence, seuilTransparence, arrondiCoins, transCref);
                        if(rg.ShowDialog(this) == DialogResult.Yes)
                        {
                            if(rg.Redétecter) stopDétecterCartes();
                            seuilDepar = rg.SeuilDep;
                            seuil = rg.Seuil;
                            marg = rg.Marge;
                            crtDMin = rg.MinDim;
                            activerDétect = rg.ActiverDétect;
                            if(pcref != 0xFFFFFF) pcref = rg.PCref; //si pas en mode transparence
                            activerDoubleDétect = rg.ActiverDoubleDétect;
                            activerTransparence = rg.ActiverTransparence;
                            seuilTransparence = rg.SeuilTransparence;
                            transCref = rg.TransCref;
                            arrondiCoins = rg.ArrondiCoins;
                            if (imageDetect != null && rg.Redétecter) calculDétecterCartes();
                        }
                    }));

                    uint? colTrans;
                    if(transCref != null) colTrans = CouleurTransparenceRéduite(transCref.Value);
                    else if (pcref != null) colTrans = CouleurTransparenceRéduite(pcref.Value);
                    else if (gcref != null) colTrans = CouleurTransparenceRéduite(gcref.Value);
                    else colTrans = null;

                    cm.MenuItems.Add(new MenuItem("-"));
                    cm.MenuItems.Add(new MenuItem("Afficher le visualiseur de cartes", (o, eArg) => { visualiseur.Show(); visualiseur.BringToFront(); }));;
                    cm.MenuItems.Add(new MenuItem("-"));
                    cm.MenuItems.Add(new MenuItem("Sauvegarder", (o, eArg) => this.Sauvegarder(colTrans)));
                    cm.MenuItems.Add(new MenuItem("-"));
                    cm.MenuItems.Add(new MenuItem("-"));
                    cm.MenuItems.Add(new MenuItem("Tout éffacer", (o, eArg) => this.Effacer()));
                    cm.Show(this, e.Location);
                }
            }
        }

        private void BoardGameFabrique_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                PointF pt = new PointF((((e.Location.X - Width / 2.0f) / echel + p.X)), (((e.Location.Y - Height / 2.0f) / echel + p.Y)));
                // = GV.Projection(e.Location);

                if (e.Delta < 0)
                {
                    echel /= 1.2f;
                }
                else if (e.Delta > 0)
                {
                    echel *= 1.2f;
                }

                PointF npt = new PointF((((e.Location.X - Width / 2.0f) / echel + p.X)), (((e.Location.Y - Height / 2.0f) / echel + p.Y)));
                PointF dp = new PointF((npt.X - pt.X), (npt.Y - pt.Y));

                //PointF npt = new PointF((e.Location.X / GC.E), (e.Location.Y / GC.E));
                p.X -= (int)(dp.X + 0.5f);
                p.Y -= (int)(dp.Y + 0.5f);

                Refresh();
            }
        }

        private void BoardGameFabrique_KeyDown(object sender, KeyEventArgs e)
        {
            ctrl = e.Control;
            shift = e.Shift;
        }
    }
}
