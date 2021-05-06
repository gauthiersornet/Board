using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BoardServer
{
    public class Program
    {
        static void Main(string[] args)
        {
            //new BoardSession("Kosmopolite", null, 0, 0, false);

            Int32 port;
            string sPort = args.FirstOrDefault(s => s.StartsWith("port="));
            if (sPort != null) port = Int32.Parse(sPort.Substring("port=".Length).Trim());
            else port = 8080;

            TcpListener server = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");
                server = new TcpListener(localAddr, port);

                // Lancer l'écoute
                server.Start();

                // Boucle d'écoute.
                while (true)
                {
                    Console.Write("En attente de connexion... ");

                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connexion!");

                    new ClientThreadServer(client, OutilsRéseau.BIntHashPassword256("")).Lancer();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Connexion exception: {0}", e);
            }
            finally
            {
                // Arrête de l'écoute
                server.Stop();
            }
        }
    }
}
