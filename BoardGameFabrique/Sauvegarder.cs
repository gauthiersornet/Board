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
    public enum EXMLGen
    {
        vide = 0,
        pile = 1,
        pioche = 2,
        piocheETdéfausse = 3,
        dé = 4
    }

    public partial class Sauvegarder : Form
    {
        private double tailleMoyW;
        private double tailleMoyH;

        public int TailleFinaleW;
        public int TailleFinaleH;
        public Int64 Qualité;
        public EXMLGen xmlGen;

        public string ImageDos;

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
            cbxXMLGen.SelectedIndex = 0;
            txtFichierImgDos.Text = "";
        }

        private void btValid_Click(object sender, EventArgs e)
        {
            TailleFinaleW = int.Parse(txtTailleFinalW.Text);
            TailleFinaleH = int.Parse(txtTailleFinalH.Text);
            Qualité = scrolBarQualité.Value;
            xmlGen = (EXMLGen)cbxXMLGen.SelectedIndex;
            DialogResult = DialogResult.OK;
            ImageDos = (String.IsNullOrWhiteSpace(txtFichierImgDos.Text) ? null : txtFichierImgDos.Text);
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
