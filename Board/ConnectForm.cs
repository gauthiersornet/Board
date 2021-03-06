using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Board
{
    public partial class ConnectForm : Form
    {
        private Board boardAConnecter;
        public ClientThreadBoard connexion;
        private Point p;


        public ConnectForm(Board board, Point p, string serv, string port, string login, string mdp, ClientThreadBoard _connection = null)
        {
            this.p = p;
            boardAConnecter = board;
            connexion = _connection;
            InitializeComponent();
            this.DialogResult = DialogResult.None;
            if (_connection != null)
            {
                txtIdentifiant.Text = connexion.ObtenirIdentifiant() ?? "";
                if(_connection.EstConnecté) ModeConnecté();
            }
            else
            {
                ModeNonConnecté();
                txtNomServeur.Text = serv;
                txtNumPort.Text = port;
                txtIdentifiant.Text = login;
                txtPwdConnect.Text = mdp;
            }
        }

        private void btConnecter_Click(object sender, EventArgs e)
        {
            if (btConnecter.Text == "Connecter")
            {
                if (txtIdentifiant.Text.Length > OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX ||
                    UTF8Encoding.UTF8.GetBytes(txtIdentifiant.Text).Length > OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX)
                {
                    MessageBox.Show("Votre identifiant est trop long.", "Erreur identifiant", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (txtIdentifiant.Text != "" && OutilsRéseau.EstChaineSecurisée(txtIdentifiant.Text) == false)
                {
                    MessageBox.Show("Votre identifiant n'est pas conforme.", "Erreur identifiant", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                btConnecter.Enabled = false;
                txtIdentifiant.Enabled = false;
                txtPwdConnect.Enabled = false;
                txtNomServeur.Enabled = false;
                txtNumPort.Enabled = false;

                var pwhash = OutilsRéseau.BIntHashPassword256(txtPwdConnect.Text);
                try
                {
                    connexion = new ClientThreadBoard(txtIdentifiant.Text, pwhash, new TcpClient(txtNomServeur.Text, int.Parse(txtNumPort.Text)), boardAConnecter);
                    connexion.Lancer();
                    //this.DialogResult = DialogResult.OK;
                    this.DialogResult = DialogResult.Yes;
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
                    this.DialogResult = DialogResult.No;
                }
            }
            else
            {
                string message;
                if (connexion != null && connexion.AEnvoiEnCours) message = "Des envois sont en cours, vos tranfères seront intérrompus...\r\n";
                else message = "";
                message += "Êtes-vous sûr de vouloir vous déconnecter ?";
                if (MessageBox.Show(message, "Déconnecter ?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
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
                    this.DialogResult = DialogResult.Yes;
                }
            }
        }

        public void ModeNonConnecté()
        {
            btConnecter.Text = "Connecter";
            txtIdentifiant.Enabled = true;
            txtPwdConnect.Enabled = true;
            txtNomServeur.Enabled = true;
            txtNumPort.Enabled = true;
            btConnecter.Enabled = true;
        }

        public void ModeConnecté()
        {
            btConnecter.Text = "Déconnecter";
            txtIdentifiant.Enabled = false;
            txtPwdConnect.Enabled = false;
            txtNomServeur.Enabled = false;
            txtNumPort.Enabled = false;
            btConnecter.Enabled = true;
        }

        private void Déconnecter()
        {
            if (connexion != null)
            {
                connexion.Close();
                if (connexion.SafeStop() == false)
                    connexion.Abort();
                connexion = null;
            }
            ModeNonConnecté();
        }

        private void ConnectForm_Shown(object sender, EventArgs e)
        {
            this.Left = p.X - this.Width / 2;
            this.Top = p.Y - this.Height / 2;
        }

        private void GKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r') btConnecter_Click(sender, e);
        }
    }
}
