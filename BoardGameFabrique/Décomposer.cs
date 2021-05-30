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
    public partial class Décomposer : Form
    {
        public int nbColonnes = 1;
        public int nbLignes = 1;
        public int nbCartes = 1;

        public Décomposer()
        {
            InitializeComponent();
        }

        private void btValid_Click(object sender, EventArgs e)
        {
            nbColonnes = int.Parse(txtNbCol.Text);
            nbLignes = int.Parse(txtNbLig.Text);
            nbCartes = int.Parse(txtNbCartes.Text);
            DialogResult = DialogResult.OK;
        }
    }
}
