using System.Collections.Generic;

namespace BrowserPass
{
    public interface IPassReader
    {
        IEnumerable<CredentialModel> ReadPasswords();
        string BrowserName { get; }
    }
}