using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace RemoteControl
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                switch (args[0].ToLower())
                {
                    case "shutdown":
                        string host = null;
                        string user = null;
                        string password = null;
                        for (var i = 1; i != args.Length - 1; ++i)
                        {
                            switch (args[i].ToLower())
                            {
                                case "-h":
                                    host = args[i + 1];
                                    break;
                                case "-u":
                                    user = args[i + 1];
                                    break;
                                case "-p":
                                    password = args[i + 1];
                                    break;
                                default:
                                    break;
                            }
                        }
                        Shutdown(host, user, password);
                        break;

                    case "startup":
                        string mac = null;
                        string broadcastAddress = null;
                        for (var i = 1; i != args.Length - 1; ++i)
                        {
                            switch (args[i])
                            {
                                case "-m":
                                    mac = args[i + 1];
                                    break;
                                case "-b":
                                    broadcastAddress = args[i + 1];
                                    break;
                            }
                        }
                        WakeOnLan(mac, broadcastAddress);
                        break;

                    case "help":
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentException();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("finish!");
            Console.ReadLine();
        }

        static void WakeOnLan(string mac, string broadcastAddress = null)
        {
            if (mac.Length != 17)
                throw new ArgumentException("提供的mac地址无效，它应该是类似FF:FF:FF:FF:FF:FF这样的格式");

            byte[] macByte = new byte[6];

            try
            {
                for (int i = 0; i < 6; ++i)
                    macByte[i] = Convert.ToByte(mac.Substring(i * 3, 2), 16);
            }
            catch (Exception)
            {
                throw new ArgumentException("提供的mac地址无效，它应该是类似FF:FF:FF:FF:FF:FF这样的格式");
            }

            if (broadcastAddress == null)
                broadcastAddress = "255.255.255.255";

            IPAddress ip = null;
            if(IPAddress.TryParse(broadcastAddress, out ip) == false)
                throw new ArgumentException("提供的广播地址无效");

            byte[] packet = new byte[6 + macByte.Length * 16];
            for (int i = 0; i != 6; ++i)
                packet[i] = 0xFF;

            for (int i = 6; i <= packet.Length - 6; i += 6)
                for (int j = 0; j != macByte.Length; ++j)
                    packet[i + j] = macByte[j];

            using(UdpClient udp = new UdpClient())
            {
                udp.Send(packet, packet.Length, new IPEndPoint(ip, 0));
            }
        }

        static void Shutdown(string host, string ipcUser, string ipcPassword)
        {
            Process p = new Process();

            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            p.Start();
            p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => 
            {
                Console.WriteLine(e.Data);
            };

            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                Console.WriteLine(e.Data);
            };

            p.StandardInput.WriteLine($"net use \\{host}\\ipc$ \"{ipcPassword}\" /user:\"{ipcUser}\"");
            p.StandardInput.WriteLine($"shutdown -s -t 60 -m \\{host}");
            p.StandardInput.WriteLine($"net use \\{host} /delete");
            p.StandardInput.WriteLine("exit");

            p.WaitForExit();
            p.Close();
        }
    }
}
