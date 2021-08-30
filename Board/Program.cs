using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Board
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string prm;
            prm = args.FirstOrDefault(s => s.StartsWith("serveur="));
            if(prm != null)
            {
                Board.PARAM_SERVEUR = prm.Substring("serveur=".Length).TrimEnd();
                Board.PARAM_FLAG = true;
            }
            prm = args.FirstOrDefault(s => s.StartsWith("port="));
            if (prm != null)
            {
                Board.PARAM_PORT = prm.Substring("port=".Length).TrimEnd();
                Board.PARAM_FLAG = true;
            }
            prm = args.FirstOrDefault(s => s.StartsWith("login="));
            if (prm != null)
            {
                Board.PARAM_LOGIN = prm.Substring("login=".Length).TrimEnd();
                Board.PARAM_FLAG = true;
            }
            prm = args.FirstOrDefault(s => s.StartsWith("mdp="));
            if (prm != null)
            {
                Board.PARAM_MOTDEPASSE = prm.Substring("mdp=".Length).TrimEnd();
                Board.PARAM_FLAG = true;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Board());
        }
    }
}
