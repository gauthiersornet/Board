using ModuleBOARD.Réseau;
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

namespace Board
{
    public partial class ConnectForm : Form
    {
        private Board boardAConnecter;
        public ClientThreadBoard connection;

        public bool connexEtablie;

        public ConnectForm(Board board)
        {
            boardAConnecter = board;
            connection = null;
            InitializeComponent();
            this.DialogResult = DialogResult.Abort;
            this.Height = 165;
            connexEtablie = false;
        }

        private void btConnecter_Click(object sender, EventArgs e)
        {
            if (btConnecter.Text == "Connecter")
            {
                btConnecter.Enabled = false;

                if (txtIdentifiant.Text.Length > OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX ||
                    UTF8Encoding.UTF8.GetBytes(txtIdentifiant.Text).Length > OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX)
                {
                    MessageBox.Show("Votre identifiant est trop long.",  "Erreur identifiant", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (txtIdentifiant.Text != "" &&  OutilsRéseau.EstChaineSecurisée(txtIdentifiant.Text) == false)
                {
                    MessageBox.Show("Votre identifiant n'est pas conforme.", "Erreur identifiant", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                lstBxSessions.Items.Clear();
                txtIdentifiant.Enabled = false;
                txtNomServeur.Enabled = false;
                txtNumPort.Enabled = false;
                try
                {
                    connection = new ClientThreadBoard(txtIdentifiant.Text, new TcpClient(txtNomServeur.Text, int.Parse(txtNumPort.Text)), boardAConnecter, this);
                    //this.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    Déconnecter();
                    MessageBox.Show(ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    /*connexEtablie = false;
                    if (connection != null)
                    {
                        connection.Abort();
                        connection.Close();
                        connection = null;
                    }
                    MessageBox.Show("Erreur", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btConnecter.Text = "Connecter";
                    txtIdentifiant.Enabled = true;
                    txtNomServeur.Enabled = true;
                    txtNumPort.Enabled = true;
                    btConnecter.Enabled = true;*/
                }
            }
            else if(MessageBox.Show("Êtes-vous sûr de vouloir vous déconnecter ?", "Déconnecter ?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Déconnecter();
                /*connexEtablie = false;
                this.Height = 200;
                lstBxSessions.Items.Clear();
                btConnecter.Enabled = false;
                connection.Close();
                if (connection.SafeStop() == false)
                    connection.Abort();
                connection = null;
                btConnecter.Text = "Connecter";
                btConnecter.Enabled = true;*/
            }
        }

        public void IdentifiantRefusé(string identifiant)
        {
            if(MessageBox.Show("Votre identifiant est refusé.\r\nL'identifiant automatique \"" + identifiant + "\" vous est proposé.\r\nVoulez-vous poursuivre ?", "Identifiant refusé", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
            {
                Déconnecter();
            }
        }

        public void ConnectionRéussie()
        {
            connexEtablie = true;
            lstBxSessions.Enabled = true;
            btRejoindre.Enabled = true;
            btActualiser.Enabled = true;
            btCréer.Enabled = true;
            btConnecter.Text = "Déconnecter";
            btConnecter.Enabled = true;
            this.Height = 400;
            txtIdentifiant.Text = connection.ObtenirIdentifiant();
        }

        private void Déconnecter()
        {
            connexEtablie = false;
            txtIdentifiant.Enabled = true;
            txtNomServeur.Enabled = true;
            txtNumPort.Enabled = true;
            btRejoindre.Enabled = false;
            btActualiser.Enabled = false;
            btCréer.Enabled = false;
            connexEtablie = false;
            this.Height = 165;
            lstBxSessions.Items.Clear();
            lstBxSessions.Enabled = false;
            btConnecter.Enabled = false;
            if (connection != null)
            {
                connection.Close();
                if (connection.SafeStop() == false)
                    connection.Abort();
                connection = null;
            }
            btConnecter.Text = "Connecter";
            btConnecter.Enabled = true;
        }

        public void PerteDeConnexion(string message)
        {
            Déconnecter();
            MessageBox.Show(message, "Perte de connexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void ConnectionRattée(string message)
        {
            Déconnecter();
            MessageBox.Show(message, "Echec de connexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void AjouterSession(string nomSession)
        {
            if(connexEtablie) lstBxSessions.Items.Add(nomSession);
        }

        private void btActualiser_Click(object sender, EventArgs e)
        {
            if (connexEtablie)
            {
                btRejoindre.Enabled = false;
                lstBxSessions.Items.Clear();
                connection.ActualiserLstSession();
            }
        }
    }
}
