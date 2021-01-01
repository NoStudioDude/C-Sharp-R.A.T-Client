namespace TutClient.Enum
{
    /// <summary>
    /// Memory allocation types
    /// </summary>
    public enum AllocationType
    {
        /// <summary>
        /// Commit memory
        /// </summary>
        Commit = 0x1000,
        /// <summary>
        /// Reserver the space
        /// </summary>
        Reserve = 0x2000,
        /// <summary>
        /// Decommit memory
        /// </summary>
        Decommit = 0x4000,
        /// <summary>
        /// Release the space
        /// </summary>
        Release = 0x8000,
        /// <summary>
        /// Reset memory space
        /// </summary>
        Reset = 0x80000,
        /// <summary>
        /// Physical allocation
        /// </summary>
        Physical = 0x400000,
        /// <summary>
        /// Top Down allocation
        /// </summary>
        TopDown = 0x100000,
        /// <summary>
        /// Write Watch Allocation
        /// </summary>
        WriteWatch = 0x200000,
        /// <summary>
        /// Large Pages allocation
        /// </summary>
        LargePages = 0x20000000
    }
}