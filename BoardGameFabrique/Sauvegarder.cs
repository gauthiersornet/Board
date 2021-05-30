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
    public partial class Sauvegarder : Form
    {
        private double tailleMoyW;
        private double tailleMoyH;

        public int TailleFinaleW;
        public int TailleFinaleH;
        public Int64 Qualité;

        public bool GénérerXML;

        public Sauvegarder(double _tailleMoyW, double _tailleMoyH)
        {
            tailleMoyW = _tailleMoyW;
            tailleMoyH = _tailleMoyH;
            TailleFinaleW = (int)(_tailleMoyW + 0.5);
            TailleFinaleH = (int)(_tailleMoyH + 0.5);
            InitializeComponent();
            lblTMoyW.Text = TailleFinaleW.ToString();
            txtTailleFinalW.Text = lblTMoyW.Text;
            lblTMoyH.Text = TailleFinaleH.ToString();
            txtTailleFinalH.Text = lblTMoyH.Text;
            GénérerXML = true;
        }

        private void btValid_Click(object sender, EventArgs e)
        {
            TailleFinaleW = int.Parse(txtTailleFinalW.Text);
            TailleFinaleH = int.Parse(txtTailleFinalH.Text);
            Qualité = scrolBarQualité.Value;
            GénérerXML = chkBxGénérerXML.Checked;
            DialogResult = DialogResult.OK;
        }

        private void scrolPourMille_ValueChanged(object sender, EventArgs e)
        {
            lblTaillePourc.Text = (scrolPourMille.Value / 10).ToString() + "." + (scrolPourMille.Value % 10).ToString() + "%";
            txtTailleFinalW.Text = ((int)(tailleMoyW * (scrolPourMille.Value / 1000.0) + 0.5)).ToString();
            txtTailleFinalH.Text = ((int)(tailleMoyH * (scrolPourMille.Value / 1000.0) + 0.5)).ToString();
        }

        private void scrolBarQualité_ValueChanged(object sender, EventArgs e)
        {
            lblQualit.Text = scrolBarQualité.Value.ToString() + "%";
        }
    }
}
