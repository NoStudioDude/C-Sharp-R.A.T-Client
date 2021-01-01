namespace TutClient.Enum
{
    /// <summary>
    /// Process Desired Access Codes
    /// </summary>
    public enum ProcessAccessFlags : uint
    {
        /// <summary>
        /// Request all access
        /// </summary>
        All = 0x001F0FFF,
        /// <summary>
        /// Only terminate process
        /// </summary>
        Terminate = 0x00000001,
        /// <summary>
        /// Create a thread in the process
        /// </summary>
        CreateThread = 0x00000002,
        /// <summary>
        /// Do Memory Operations on the process
        /// </summary>
        VirtualMemoryOperation = 0x00000008,
        /// <summary>
        /// Only read memory of the process
        /// </summary>
        VirtualMemoryRead = 0x00000010,
        /// <summary>
        /// Only write memory of the process
        /// </summary>
        VirtualMemoryWrite = 0x00000020,
        /// <summary>
        /// Duplicate the handle of the process
        /// </summary>
        DuplicateHandle = 0x00000040,
        /// <summary>
        /// Create a child process
        /// </summary>
        CreateProcess = 0x000000080,
        /// <summary>
        /// Set process quota
        /// </summary>
        SetQuota = 0x00000100,
        /// <summary>
        /// Set process information
        /// </summary>
        SetInformation = 0x00000200,
        /// <summary>
        /// Query process information
        /// </summary>
        QueryInformation = 0x00000400,
        /// <summary>
        /// Query limited process infromation
        /// </summary>
        QueryLimitedInformation = 0x00001000,
        /// <summary>
        /// Synchronize the process
        /// </summary>
        Synchronize = 0x00100000
    }
}
