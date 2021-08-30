using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoardGameFabrique
{
    public partial class VisualiseurCartes : Form
    {
        private int idxCartes;
        private List<(ETransformation, Image, int)> lstCartes;

        public VisualiseurCartes(List<(ETransformation, Image, int)> _lstCartes)
        {
            idxCartes = 0;
            lstCartes = _lstCartes;
            InitializeComponent();
        }

        private void RefreshTitre()
        {
            if (idxCartes >= 0 && lstCartes!=null && lstCartes.Any())
            {
                if(lstCartes[idxCartes].Item3 > 0)
                    this.Text = "Visualiseur de cartes n°" + idxCartes + " quantité " + lstCartes[idxCartes].Item3;
                else this.Text = "Visualiseur de cartes n°" + idxCartes + " Dos du paquet";
            }
            else this.Text = "Visualiseur de cartes";
        }

        public void AjoutSuppresCarte()
        {
            idxCartes = (lstCartes?.Count ?? 0) - 1;
            RefreshTitre();
            Refresh();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //Redessiner(e.Graphics);
            Graphics g = e.Graphics;
            //if(image != null)g.DrawImage(image, p.X, p.Y, new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            if(lstCartes != null && lstCartes.Any())
            {
                if (idxCartes < 0) idxCartes = 0;
                else if (idxCartes >= lstCartes.Count) idxCartes = lstCartes.Count - 1;

                ETransformation etrans;
                Image img;
                int qt;
                (etrans, img, qt) = lstCartes[idxCartes];

                if (img != null)
                {
                    int x = 0;
                    int y = 0;

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

                    g.DrawImage(img, x, y);
                }
            }
        }

        private void VisualiseurCartes_MouseClick(object sender, MouseEventArgs e)
        {
            /*if(e.Button == MouseButtons.Right)
            {
                ContextMenu cm = new ContextMenu();
                cm.MenuItems.Add(new MenuItem("Décomposer ligne colonne", (o, eArg) => this.DécomposerLigneColonne()));
                cm.MenuItems.Add(new MenuItem("-"));
                cm.MenuItems.Add(new MenuItem("Afficher le visualiseur de cartes", (o, eArg) => visualiseur.Show()));
                cm.Show(this, e.Location);
            }*/
        }

        private void VisualiseurCartes_KeyUp(object sender, KeyEventArgs e)
        {
            if (lstCartes != null && lstCartes.Any())
            {
                if (e.KeyCode == Keys.Right)
                {
                    int oldIdx = idxCartes;
                    ++idxCartes;
                    if (idxCartes < 0) idxCartes = 0;
                    else if (idxCartes >= lstCartes.Count) idxCartes = lstCartes.Count - 1;
                    RefreshTitre();
                    if(e.Control)
                    {
                        var swap = lstCartes[oldIdx];
                        lstCartes[oldIdx] = lstCartes[idxCartes];
                        lstCartes[idxCartes] = swap;
                    }
                    else Refresh();
                }
                else if (e.KeyCode == Keys.Left)
                {
                    int oldIdx = idxCartes;
                    --idxCartes;
                    if (idxCartes < 0) idxCartes = 0;
                    else if (idxCartes >= lstCartes.Count) idxCartes = lstCartes.Count - 1;
                    RefreshTitre();
                    if (e.Control)
                    {
                        var swap = lstCartes[oldIdx];
                        lstCartes[oldIdx] = lstCartes[idxCartes];
                        lstCartes[idxCartes] = swap;
                    }
                    else Refresh();
                }
                else if(e.KeyCode == Keys.Delete)
                {
                    if (idxCartes < 0) idxCartes = 0;
                    else if (idxCartes >= lstCartes.Count) idxCartes = lstCartes.Count - 1;
                    lstCartes.RemoveAt(idxCartes);
                    if (idxCartes < 0) idxCartes = 0;
                    else if (idxCartes >= lstCartes.Count) idxCartes = lstCartes.Count - 1;
                    RefreshTitre();
                    Refresh();
                }
            }
        }

        private void VisualiseurCartes_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (lstCartes != null && lstCartes.Any())
            {
                if (e.KeyChar == 'r' || e.KeyChar == 'R')
                {
                    if (idxCartes < 0) idxCartes = 0;
                    else if (idxCartes >= lstCartes.Count) idxCartes = lstCartes.Count - 1;
                    lstCartes[idxCartes] = (lstCartes[idxCartes].Item1 ^ ETransformation.Retourner, lstCartes[idxCartes].Item2, lstCartes[idxCartes].Item3);
                    Refresh();
                }
                else if (e.KeyChar == 'x' || e.KeyChar == 'X')
                {
                    if (idxCartes < 0) idxCartes = 0;
                    else if (idxCartes >= lstCartes.Count) idxCartes = lstCartes.Count - 1;
                    lstCartes[idxCartes] = (lstCartes[idxCartes].Item1 ^ ETransformation.MiroirX, lstCartes[idxCartes].Item2, lstCartes[idxCartes].Item3);
                    Refresh();
                }
                else if (e.KeyChar == 'y' || e.KeyChar == 'Y')
                {
                    if (idxCartes < 0) idxCartes = 0;
                    else if (idxCartes >= lstCartes.Count) idxCartes = lstCartes.Count - 1;
                    lstCartes[idxCartes] = (lstCartes[idxCartes].Item1 ^ ETransformation.MiroirY, lstCartes[idxCartes].Item2, lstCartes[idxCartes].Item3);
                    Refresh();
                }
                else if(e.KeyChar == '=' || e.KeyChar == '+')
                {
                    lstCartes[idxCartes] = (lstCartes[idxCartes].Item1, lstCartes[idxCartes].Item2, lstCartes[idxCartes].Item3 + 1);
                    RefreshTitre();
                }
                else if (e.KeyChar == '6' || e.KeyChar == '-')
                {
                    if(lstCartes[idxCartes].Item3 > 0) lstCartes[idxCartes] = (lstCartes[idxCartes].Item1, lstCartes[idxCartes].Item2, lstCartes[idxCartes].Item3 - 1);
                    RefreshTitre();
                }
            }
        }

        private void VisualiseurCartes_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
