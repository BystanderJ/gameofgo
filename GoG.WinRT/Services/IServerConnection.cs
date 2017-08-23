using System;
using System.Threading.Tasks;
using GoG.Shared.Services.Multiplayer;


namespace GoG.WinRT.Services
{
    public interface IServerConnection
    {
        void Init();

        Task Connect(UserPrefs user);
        Task Disconnect();

        Task PostToLobby(string msg);
        event Action<Post> LobbyPost;

        Task PostToCurrentGame(string msg);
        event Action<Post> GamePost;

        Task SendGameRequest(string name);
        event Action<string> GameRequested;

        Task DeclineGameRequest(string name);
        event Action<string> GameRequestDeclined;

        Task CancelGameRequest(string name);
        event Action<string> GameRequestCancelled;

        Task AcceptGameRequest(string name);
        event Action<string> GameRequestAccepted;

        Task AbortGame(string name);
        event Action<string> GameAborted;
        
        event Action<UserPrefs> UserConnected;
        event Action<string> UserDisconnected;
    }
}