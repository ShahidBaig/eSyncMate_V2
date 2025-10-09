using eSyncMate.Processor.Models;
using eSyncMate.Processor.Connections;

namespace eSyncMate.Processor.Connections
{
    public interface IConnector
    {
        public static string Token ;
        
         Task  GetApiToken(string BaseURL, string ConsumerKey, string ConsumerSecret,string ApplicationID, string RefereshToken, string GrantType);
    }
}
