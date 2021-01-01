namespace TutClient.Enum
{
    /// <summary>
    /// Shutdown signal types
    /// </summary>
    public enum CtrlType
    {
        /// <summary>
        /// Control + C pressed
        /// </summary>
        CTRL_C_EVENT = 0,
        /// <summary>
        /// Break pressed
        /// </summary>
        CTRL_BREAK_EVENT = 1,
        /// <summary>
        /// Window closed
        /// </summary>
        CTRL_CLOSE_EVENT = 2,
        /// <summary>
        /// User logged off
        /// </summary>
        CTRL_LOGOFF_EVENT = 5,
        /// <summary>
        /// User stopped the OS
        /// </summary>
        CTRL_SHUTDOWN_EVENT = 6
    }
}