using TutClient.Enum;

namespace TutClient.Handlers
{
    public delegate bool OnEventHandler(CtrlType sig);

    public interface ISignalHandler
    {
        bool Handler(CtrlType sig);
    }

    public class SignalHandler : ISignalHandler
    {
        private readonly IIpcClientHandler _ipcClientHandler;
        private readonly ICommandHandler _commandHandler;

        public SignalHandler(IIpcClientHandler ipcClientHandler,
                             ICommandHandler commandHandler)
        {
            _ipcClientHandler = ipcClientHandler;
            _commandHandler = commandHandler;
        }
        public bool Handler(CtrlType sig)
        {
            //In every case shutdown existing IPC connections and notify the server of the disconnect

            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                    _ipcClientHandler.StopIpcHandler();
                    _commandHandler.SendCommand("dclient");
                    return true;
                case CtrlType.CTRL_LOGOFF_EVENT:
                    _ipcClientHandler.StopIpcHandler();
                    _commandHandler.SendCommand("dclient");
                    return true;
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                    _ipcClientHandler.StopIpcHandler();
                    _commandHandler.SendCommand("dclient");
                    return true;
                case CtrlType.CTRL_CLOSE_EVENT:
                    _ipcClientHandler.StopIpcHandler();
                    _commandHandler.SendCommand("dclient");
                    return true;
                default:
                    return false;
            }
        }
    }
}
