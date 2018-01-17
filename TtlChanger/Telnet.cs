using System;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace TtlChanger
{
    static class Telnet
    {

        private static TcpClient Connection;
        private static int TimeOutMs = 500;

        public static string Ip { get; set; } = "192.168.1.1";
        public static int Port { get; set; } = 23;
        public static string Login { get; set; } = "admin";
        public static string Password { get; set; } = "admin";

        public static void Connect()
        {
            Connection = new TcpClient(Ip,Port);
            System.Threading.Thread.Sleep(TimeOutMs);
            Send(Login);
            Send(Password);
            string s = Read();
            if (s.TrimEnd().EndsWith(":"))
                throw new Exception("Failed to connect : Login incorrect");

        }

        public static void Close()
        {
            if (Connection != null) Connection.Close();
        }

        public static bool IsConnected
        {
            get { return Connection.Connected; }
        }

        public static void Send(string cmd)
        {
            if (Connection==null || !Connection.Connected) Connect();
            byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(cmd+"\r\n");
            Connection.GetStream().Write(buf, 0, buf.Length);
            System.Threading.Thread.Sleep(TimeOutMs);
        }


        public static string Read()
        {
            if (!Connection.Connected) return null;
            StringBuilder sb = new StringBuilder();
            do
            {
                ParseTelnet(sb);
                System.Threading.Thread.Sleep(TimeOutMs);
            } while (Connection.Available > 0);
            return sb.ToString();
        }

        enum Verbs
        {
            WILL = 251,
            WONT = 252,
            DO = 253,
            DONT = 254,
            IAC = 255
        }

        enum Options
        {
            SGA = 3
        }



        private static void ParseTelnet(StringBuilder sb)
        {
            while (Connection.Available > 0)
            {
                int input = Connection.GetStream().ReadByte();
                switch (input)
                {
                    case -1:
                        break;
                    case (int)Verbs.IAC:
                        // interpret as command
                        int inputverb = Connection.GetStream().ReadByte();
                        if (inputverb == -1) break;
                        switch (inputverb)
                        {
                            case (int)Verbs.IAC:
                                //literal IAC = 255 escaped, so append char 255 to string
                                sb.Append(inputverb);
                                break;
                            case (int)Verbs.DO:
                            case (int)Verbs.DONT:
                            case (int)Verbs.WILL:
                            case (int)Verbs.WONT:
                                // reply to all commands with "WONT", unless it is SGA (suppres go ahead)
                                int inputoption = Connection.GetStream().ReadByte();
                                if (inputoption == -1) break;
                                Connection.GetStream().WriteByte((byte)Verbs.IAC);
                                if (inputoption == (int)Options.SGA)
                                    Connection.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
                                else
                                    Connection.GetStream().WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
                                Connection.GetStream().WriteByte((byte)inputoption);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        sb.Append((char)input);
                        break;
                }
            }
        }


    }
}
