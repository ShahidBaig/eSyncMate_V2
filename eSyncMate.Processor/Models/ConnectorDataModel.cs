namespace eSyncMate.Processor.Models
{
    public class ConnectorDataModel
    {
        public string ConnectivityType { get; set; }
        public string ConnectionString { get; set; }
        public string CommandType { get; set; }
        public string Command { get; set; }
        public string KeyFieldName { get; set; }
        public string DataFieldName { get; set; }
        public string CustomerID { get; set; }
        public string JsonDataCollectionName { get; set; }

        public string AuthType { get; set; }
        public string Host { get; set; }
        public string BaseUrl { get; set; }
        public string Url { get; set; }
        public string Method { get; set; }
        public string BodyFormat { get; set; }
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string Token { get; set; }
        public string TokenSecret { get; set; }
        public string Realm { get; set; }
        public List<ConnectorHeader> Headers { get; set; }
        public List<Parameter> Parmeters { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ConnectorHeader
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
