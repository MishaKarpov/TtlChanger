using System;
using System.IO;
using System.Xml.XPath;

namespace TtlChanger
{
    class Program
    {
        private const string fileConfig = "config.xml";
        private const int rebootTimeOutS = 15;

        static void Main(string[] args)
        {
            if (File.Exists(fileConfig))
            {
                // Load configuration
                XPathDocument xml = new XPathDocument(fileConfig);
                XPathNavigator nav = xml.CreateNavigator();

                if (nav.SelectSingleNode("xml/config/ip") != null) Telnet.Ip = nav.SelectSingleNode("xml/config/ip").Value;
                if (nav.SelectSingleNode("xml/config/port") != null) Telnet.Port = int.Parse(nav.SelectSingleNode("xml/config/port").Value);
                if (nav.SelectSingleNode("xml/config/login") != null) Telnet.Login = nav.SelectSingleNode("xml/config/login").Value;
                if (nav.SelectSingleNode("xml/config/password") != null) Telnet.Password = nav.SelectSingleNode("xml/config/password").Value;

                Console.WriteLine("Loaded configuration from {0}", fileConfig);
            }
            else Console.WriteLine("Not found {0}, used default configuration", fileConfig);

            Console.WriteLine("Ip: {0}", Telnet.Ip);
            Console.WriteLine("Port: {0}", Telnet.Port);
            Console.WriteLine("Login: {0}", Telnet.Login);
            Console.WriteLine("Password: {0}", Telnet.Password);

            if (args.Length != 0)
            {
                if (args[0] == "reboot") Reboot();
                else ChangeTtl();
            }
            else
            {
                Console.WriteLine("Press key 'R' to reboot router or any another key to change TTL.");
                if (Console.ReadKey(true).Key == ConsoleKey.R) Reboot();
                else ChangeTtl();
            }

        }


       private static void ChangeTtl()
        {
            Console.WriteLine("TTL changing...");
            // Firewall command for set TTL
            Telnet.Send("iptables -t mangle -I POSTROUTING -o \"${WAN_IF}\" -j TTL --ttl-set 128");
            Telnet.Send("iptables -I PREROUTING -t mangle -d 8.8.8.8 -j TTL --ttl-set 128");
            Console.WriteLine("TTL changed!");
        }

        private static void Reboot()
        {
            Console.WriteLine("Wait rebooting...");

            Telnet.Send("reboot");
            Telnet.Close();
            // If success than connection true, so next exceptions will be depended on booting device

            System.Threading.Thread.Sleep(rebootTimeOutS*1000);
            while (!Telnet.IsConnected)
            {
                try
                {
                    Telnet.Connect();
                }
                catch
                {
                    // wait when router boot 
                    System.Threading.Thread.Sleep(1000);
                }

            }
            Console.WriteLine("Successful reboot!");

            ChangeTtl();

        }
    }
}
