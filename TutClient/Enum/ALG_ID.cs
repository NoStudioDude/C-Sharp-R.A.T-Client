namespace TutClient.Enum
{
    /// <summary>
    /// Hashing Algorithms
    /// </summary>
    public enum ALG_ID
    {
        /// <summary>
        /// MD5 Algorithm
        /// </summary>
        CALG_MD5 = 0x00008003,
        /// <summary>
        /// SHA1 Algorithm
        /// </summary>
        CALG_SHA1 = ApplicationSettings.ALG_CLASS_HASH | ApplicationSettings.ALG_SID_SHA1
    }
}
