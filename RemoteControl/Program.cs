using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace RemoteControl
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                xmlPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\RemoteControl.xml";

                switch (args[0].ToLower())
                {
                    case "shutdown":
                        string host = null;
                        for (var i = 1; i != args.Length - 1; ++i)
                        {
                            switch (args[i].ToLower())
                            {
                                case "-a":
                                    host = args[i + 1];
                                    break;
                                default:
                                    break;
                            }
                        }
                        Shutdown(host);
                        break;

                    case "startup":
                        string mac = null;
                        string broadcastAddress = null;
                        string address = null;
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
                                case "-a":
                                    address = args[i + 1];
                                    break;
                                default:
                                    break;
                            }
                        }
                        WakeOnLan(mac, address, broadcastAddress);
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

            Console.WriteLine("操作完成");
        }

        static string xmlPath;

        static void WakeOnLan(string mac, string address, string broadcastAddress = null)
        {
            // 参数检查
            if (mac == null && address == null)
                throw new ArgumentNullException("必须提供目标主机的mac地址或IP地址");

            if (address != null)
            {
                XElement xml = XElement.Load(xmlPath);
                var arpList = xml.Element("ARP").Elements("Map");

                XElement map = null;
                foreach(var m in arpList)
                {
                    if (m.Element("IP").Value == address)
                    {
                        map = m;
                        break;
                    }
                }

                if (map == null && mac == null)
                {
                    throw new ArgumentException("找不到该IP对应的mac地址");
                }
                else if (map != null && mac != null)
                {
                    map.SetElementValue("Mac", mac);
                }
                else if (map != null && mac == null)
                {
                    mac = map.Element("Mac").Value;
                }
                else //if(map == null && mac != null)
                {
                    var ipElement = new XElement("IP", address);
                    var macElement = new XElement("Mac", mac);
                    var mapElement = new XElement("Map", ipElement, macElement);
                    xml.Element("ARP").Add(mapElement);
                }

                xml.Save(xmlPath);
            }

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

        static void Shutdown(string host)
        {
            if (host == null)
                throw new ArgumentNullException("没有指定要操作的主机");

            Process p = new Process();

            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
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

            p.StandardInput.WriteLine($"shutdown -s -t 10 -m \\{host}");
            p.StandardInput.WriteLine("exit");

            p.WaitForExit();
            p.Close();
        }
    }
}
