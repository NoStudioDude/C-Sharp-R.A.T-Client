using System;
using System.Diagnostics;
using System.Threading.Tasks;

using appCom;

namespace TutClient.Handlers
{
    public interface IIpcClientHandler
    {
        event EventHandler<string> SendCommand;

        void StopIpcHandler();
        void StartIpcHandler();
        void WriteToStream(string message);
        Task LaunchIpcChild(string serverName);
    }

    public class IpcClientHandler : IIpcClientHandler
    {
        public event EventHandler<string> SendCommand;

        private Client _ipcClient;
        private static ProcessData _ipcProcess;

        /// <summary>
        /// Start handling IPC connections
        /// </summary>
        public void StartIpcHandler()
        {
            _ipcClient = new Client(); //Create a new IPC client
            _ipcClient.OnMessageReceived += ReadIpc; //Subscribe to the message receiver
        }

        /// <summary>
        /// Stop the IPC Handler and kill out client
        /// </summary>
        public void StopIpcHandler()
        {
            if (_ipcClient == null) return; //Check if the client is running
            
            _ipcClient.StopPipe(); //Stop the client
            _ipcClient = null; //Set the client to null
        }

        /// <summary>
        /// Writes to IpcClient stream
        /// </summary>
        public void WriteToStream(string message)
        {
            _ipcClient.WriteStream(message);
        }

        /// <summary>
        /// Start IPC Child processes
        /// </summary>
        /// <param name="serverName">The server to start</param>
        public async Task LaunchIpcChild(string serverName)
        {
            string filepath = ""; //The path to the server's exe file
            if (serverName == "tut_client_proxy") //If the proxy server is specified
            {
                filepath = @"proxy\proxyServer.exe"; //Set the proxy server's path
            }

            _ipcProcess = ProcessData.CheckProcessName("proxyServer", "tut_client_proxy"); //Get the process data of the proxySevrer

            if ((_ipcProcess != null && !_ipcProcess.IsPipeOnline("tut_client_proxy")) || _ipcProcess == null) //Check if the server is offline
            {
                var p = new Process //Create a new process object
                        {
                            StartInfo =
                            {
                                FileName = filepath, //Set the exe path
                                Arguments = "use_ipc" //Specify the IPC flag for the proxy
                            }
                        };

                p.Start(); //Start the proxy Server
                _ipcProcess = new ProcessData(p.Id, "tut_client_proxy"); //Get a new process data

                await Task.Delay(1500); //Wait for the server to start
            }

            _ipcClient.ConnectPipe("tut_client_proxy", 0); //Connect to the server
        }

        /// <summary>
        /// IPC Receive Messages Callback
        /// </summary>
        /// <param name="e">Message event args</param>
        private void ReadIpc(ClientMessageEventArgs e)
        {
            var msg = e.Message; //Get the message
            SendCommand?.Invoke(this, "ipc§" + "tut_client_proxy" + "§" + msg);
        }
    }
}