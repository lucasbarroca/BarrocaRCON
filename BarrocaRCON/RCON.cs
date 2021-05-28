using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BarrocaRCON
{
    public class RCON
    {
        public IPEndPoint Address;
        //public string Password { get; set; }

        UdpClient udpClient;

        public RCON(IPAddress IP, ushort Port)
        {
            Address = new IPEndPoint(IP, Port);
            //this.Password = Password;

            udpClient = new UdpClient();
            udpClient.Client.SendTimeout = 1000;
            udpClient.Client.ReceiveTimeout = 1000;
        }

        public async Task<string> ExecuteCommand(Encoding encoding, string password, string command, bool RetryOnDisconnect = false)
        {
            bool success = false;
            while (!success)
            {
                try
                {
                    byte[] sendData = encoding.GetBytes($"\xFF\xFF\xFF\xFFrcon {password} {command}");
                    await udpClient.SendAsync(sendData, sendData.Length, Address);

                    byte[] receiveData = udpClient.Receive(ref Address);
                    success = true;
                    return encoding.GetString(receiveData).Substring(10).Trim();
                }
                catch
                {
                    if (!RetryOnDisconnect)
                        return "Command failed. Probably can't connect";
                }
            }

            return "something wrong happens.";
        }
    }
}
