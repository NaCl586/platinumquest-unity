using UnityEngine;

namespace Server.Config
{
    [CreateAssetMenu(fileName = "ServerConfig", menuName = "Server/Server Config")]
    public class ServerConfig : ScriptableObject
    {
        [Header("Connection")]
        public string BaseUrl = "http://localhost:7104";

        [Header("Request")]
        public int Timeout = 30;

        [Header("Debug")]
        public bool LogRequests = true;
    }
}
