using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteControl
{
    class Program
    {
        static void Main(string[] args)
        {
            switch(args[0].ToLower())
            {
                case "shutdown":
                    string host = null;
                    string user = null;
                    string password = null;
                    for (var i = 1; i != 6; ++i)
                    {
                        switch(args[i].ToLower())
                        {
                            case "-a":
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
                    break;
                case "help":
                    break;
                default:
                    break;
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

            p.StandardInput.WriteLine($"net use \\{host}\\ipc$ \"{ipcPassword}\" /user:\"{ipcUser}\"");
            p.StandardInput.WriteLine($"shutdown -s -t 60 -m \\{host}");
            p.StandardInput.WriteLine($"net use \\{host} /delete");
            p.StandardInput.WriteLine("exit");

            p.WaitForExit();
            p.Close();
        }
    }
}
