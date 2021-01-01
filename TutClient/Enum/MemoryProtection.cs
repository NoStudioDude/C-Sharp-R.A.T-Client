namespace TutClient.Enum
{
    /// <summary>
    /// Memory protection actions
    /// </summary>
    public enum MemoryProtection
    {
        /// <summary>
        /// Execute Only
        /// </summary>
        Execute = 0x10,

        /// <summary>
        /// Execute and Read
        /// </summary>
        ExecuteRead = 0x20,

        /// <summary>
        /// Execute, Read and Write
        /// </summary>
        ExecuteReadWrite = 0x40,

        /// <summary>
        /// Execute, Write and Copy
        /// </summary>
        ExecuteWriteCopy = 0x80,

        /// <summary>
        /// No access to memory
        /// </summary>
        NoAccess = 0x01,

        /// <summary>
        /// Read Only
        /// </summary>
        ReadOnly = 0x02,

        /// <summary>
        /// Read and Write
        /// </summary>
        ReadWrite = 0x04,

        /// <summary>
        /// Write and Copy
        /// </summary>
        WriteCopy = 0x08,

        /// <summary>
        /// Modify the guard
        /// </summary>
        GuardModifierflag = 0x100,

        /// <summary>
        /// Modify the caching
        /// </summary>
        NoCacheModifierflag = 0x200,

        /// <summary>
        /// Modify the combined writing
        /// </summary>
        WriteCombineModifierflag = 0x400
    }
}