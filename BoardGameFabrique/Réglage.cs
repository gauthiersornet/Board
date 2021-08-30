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
    public enum DetectAlgo : byte
    {
        Vide = 0,
        Basique = 1,
        Avancé = 2
    }

    public partial class Réglage : Form
    {
        public const int SeuilMax = 195075;
        public const int PourXXX = 1000;

        public int SeuilDep = 195075/2;
        public int Seuil = 58522;
        public int Marge = 19507;
        public int MinDim = 50;
        public DetectAlgo ActiverDétect;
        public DetectAlgo ActiverDoubleDétect;
        public uint? PCref = null;
        public bool ActiverTransparence;
        public int SeuilTransparence = 10;
        public int ArrondiCoins;
        public uint? TransCref;

        public bool Redétecter;

        public Réglage(int seuilDep, int seuil, int marge, int minDim, DetectAlgo activerDétect, DetectAlgo activerDoubleDétect, uint? pcref, bool activerTransparence, int seuilTransparence, int arrondiCoins, uint? transCref)
        {
            InitializeComponent();
            SeuilDep = seuilDep;
            tbSeuilDep.Value = (PourXXX * seuilDep + SeuilMax / 2) / SeuilMax;

            Seuil = seuil;
            tbSeuil.Value = (PourXXX * seuil + SeuilMax / 2) / SeuilMax;

            tbMarge.Value = Marge = marge;
            tbDMin.Value = MinDim = minDim;
            cbBxActDetect.SelectedIndex = (byte)activerDétect; ActiverDétect = activerDétect;
            cbBxActDbDetect.SelectedIndex = (byte)activerDoubleDétect; ActiverDoubleDétect = activerDoubleDétect;

            SeuilTransparence = seuilTransparence;
            tbSeuilTransparence.Value = (PourXXX * seuilTransparence + SeuilMax / 2) / SeuilMax;

            tbArrondCoin.Value = ArrondiCoins = arrondiCoins;
            PCref = pcref;
            if (pcref != null)
            {
                pnColor.BackColor = Color.FromArgb((int)pcref);
                pnColor.Visible = true;
            }
            else pnColor.Visible = false;
            chkBxActiverTransparence.Checked = activerTransparence;
            TransCref = transCref;
            if (transCref != null)
            {
                pnTrColor.BackColor = Color.FromArgb((int)transCref);
                pnTrColor.Visible = true;
            }
            else pnTrColor.Visible = false;

            lbSeuilDep.Text = tbSeuilDep.Value.ToString();
            lbMarge.Text = tbMarge.Value.ToString();
            lbDMin.Text = tbDMin.Value.ToString();
            lbSeuil.Text = tbSeuil.Value.ToString();
            lbSeuilTrans.Text = tbSeuilTransparence.Value.ToString();
            lbArrondCoin.Text = tbArrondCoin.Value.ToString();
        }

        private void BtValider_Click(object sender, EventArgs e)
        {
            int seuilDep = (tbSeuilDep.Value * SeuilMax + PourXXX / 2) / PourXXX;
            int seuil = (tbSeuil.Value * SeuilMax + PourXXX / 2) / PourXXX;
            int seuilTransparence = (tbSeuilTransparence.Value * SeuilMax + PourXXX / 2) / PourXXX;

            if (SeuilDep != seuilDep)
            {
                Redétecter = true;
                SeuilDep = seuilDep;
            }
            if (Seuil != seuil)
            {
                Redétecter = true;
                Seuil = seuil;
            }
            if (Marge != tbMarge.Value)
            {
                Redétecter = true;
                Marge = tbMarge.Value;
            }
            if (MinDim != tbDMin.Value)
            {
                Redétecter = true;
                MinDim = tbDMin.Value;
            }
            if ((int)ActiverDétect != cbBxActDetect.SelectedIndex)
            {
                Redétecter = true;
                ActiverDétect = (DetectAlgo)cbBxActDetect.SelectedIndex;
            }
            ActiverDoubleDétect = (DetectAlgo)cbBxActDbDetect.SelectedIndex;
            uint? vref;
            if (pnColor.Visible) vref = (uint)pnColor.BackColor.ToArgb();
            else vref = null;
            if ((PCref != null && vref != null && PCref.Value != vref.Value) || (PCref != null ^ vref != null))
            {
                Redétecter = true;
                PCref = vref;
            }
            ActiverTransparence = chkBxActiverTransparence.Checked;
            SeuilTransparence = seuilTransparence;
            if(ArrondiCoins != tbArrondCoin.Value)
            {
                ArrondiCoins = tbArrondCoin.Value;
                if(ActiverDétect == DetectAlgo.Avancé) Redétecter = true;
            }
            if (pnTrColor.Visible) TransCref = (uint)pnTrColor.BackColor.ToArgb();
            else TransCref = null;
            DialogResult = DialogResult.Yes;
        }

        private void tbSeuilDep_ValueChanged(object sender, EventArgs e)
        {
            lbSeuilDep.Text = tbSeuilDep.Value.ToString();
        }

        private void tbMarge_ValueChanged(object sender, EventArgs e)
        {
            lbMarge.Text = tbMarge.Value.ToString();
        }

        private void tbDMin_ValueChanged(object sender, EventArgs e)
        {
            lbDMin.Text = tbDMin.Value.ToString();
        }

        private void btDialogColor_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                pnColor.BackColor = colorDialog.Color;
                pnColor.Visible = true;
            }
        }

        private void btSuppColor_Click(object sender, EventArgs e)
        {
            pnColor.Visible = false;
        }

        private void tbSeuil_ValueChanged(object sender, EventArgs e)
        {
            lbSeuil.Text = tbSeuil.Value.ToString();
        }

        private void btDialogColorTrans_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                pnTrColor.BackColor = colorDialog.Color;
                pnTrColor.Visible = true;
            }
        }

        private void btSuppColorTrans_Click(object sender, EventArgs e)
        {
            pnTrColor.Visible = false;
        }

        private void tbSeuilTransparence_ValueChanged(object sender, EventArgs e)
        {
            lbSeuilTrans.Text = tbSeuilTransparence.Value.ToString();
        }

        private void tbArrondCoin_Scroll(object sender, EventArgs e)
        {
            lbArrondCoin.Text = tbArrondCoin.Value.ToString();
        }

        private void btValidSansDetect_Click(object sender, EventArgs e)
        {
            BtValider_Click(sender, e);
            Redétecter = false;
        }
    }
}
