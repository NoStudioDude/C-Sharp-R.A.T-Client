namespace TutClient.Enum
{
    /// <summary>
    /// R.A.T Remote Error Codes
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// File can't be located
        /// </summary>
        FILE_NOT_FOUND = 0x00,

        /// <summary>
        /// Access to process is denied, when killing
        /// </summary>
        PROCESS_ACCESS_DENIED = 0x01,

        /// <summary>
        /// Cannot encrypt data
        /// </summary>
        ENCRYPT_DATA_CORRUPTED = 0x02,

        /// <summary>
        /// Cannot decrypt data
        /// </summary>
        DECRYPT_DATA_CORRUPTED = 0x03,

        /// <summary>
        /// Cannot find the specified directory
        /// </summary>
        DIRECTORY_NOT_FOUND = 0x04,

        /// <summary>
        /// Invalid device selected with mic or cam stream
        /// </summary>
        DEVICE_NOT_AVAILABLE = 0x05,

        /// <summary>
        /// Password recovery failed
        /// </summary>
        PASSWORD_RECOVERY_FAILED = 0x06,

        /// <summary>
        /// Error, when reading from the remote CMD module
        /// </summary>
        CMD_STREAM_READ = 0X07,

        /// <summary>
        /// Cannot find specified path
        /// </summary>
        FILE_AND_DIR_NOT_FOUND = 0x08,

        /// <summary>
        /// Specified file already exists
        /// </summary>
        FILE_EXISTS = 0x09,

        /// <summary>
        /// Elevated privileges are required to run this module
        /// </summary>
        ADMIN_REQUIRED = 0x10,

        /// <summary>
        /// Catched expection
        /// </summary>
        EXCEPTION = 0x11,

        /// <summary>
        /// Given command not found
        /// </summary>
        COMMAND_NOT_FOUND = 0x12
    }
}