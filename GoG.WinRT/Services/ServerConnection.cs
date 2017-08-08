using System;
using System.Threading.Tasks;
using GoG.Infrastructure;
using GoG.Infrastructure.Services.Multiplayer;
using Microsoft.AspNet.SignalR.Client;

namespace GoG.WinRT.Services
{
    public sealed class ServerConnection : IServerConnection
    {
        private HubConnection _hubConnection;
        private IHubProxy _goHub;


        public event Action<UserPrefs> UserConnected;
        public event Action<string> UserDisconnected;

        public event Action<Post> LobbyPost;
        public event Action<Post> GamePost;

        public event Action<string> GameRequested;
        public event Action<string> GameRequestDeclined;
        public event Action<string> GameRequestCancelled;
        public event Action<string> GameRequestAccepted;
        public event Action<string> GameAborted;

        private bool _isInitialized;

        public void Init()
        {
            if (_isInitialized)
                return;
            _isInitialized = true;

            var url = Secrets.GoHubUrl;
            _hubConnection = new HubConnection(url);
            _goHub = _hubConnection.CreateHubProxy("GoHub");

            _goHub.On(nameof(UserConnected), (UserPrefs user) => UserConnected?.Invoke(user));
            _goHub.On(nameof(UserDisconnected), (string user) => UserDisconnected?.Invoke(user));

            _goHub.On(nameof(LobbyPost), (Post post) => LobbyPost?.Invoke(post));
            _goHub.On(nameof(GamePost), (Post post) => GamePost?.Invoke(post));

            _goHub.On(nameof(GameRequested), (string name) => GameRequested?.Invoke(name));
            _goHub.On(nameof(GameRequestDeclined), (string name) => GameRequestDeclined?.Invoke(name));
            _goHub.On(nameof(GameRequestCancelled), (string name) => GameRequestCancelled?.Invoke(name));
            _goHub.On(nameof(GameRequestAccepted), (string name) => GameRequestAccepted?.Invoke(name));

            _goHub.On(nameof(GameAborted), (string name) => GameAborted?.Invoke(name));
        }
        
        public async Task Connect(UserPrefs userPrefs)
        {
            if (_hubConnection.State == ConnectionState.Connected)
                _hubConnection.Stop();
            while (_hubConnection.State == ConnectionState.Connected)
                await Task.Delay(200);
            await _hubConnection.Start();
            await _goHub.Invoke("EnterLobby", userPrefs);
        }

        public async Task Disconnect()
        {
            if (_hubConnection.State != ConnectionState.Connected)
                return;
            _hubConnection.Stop();
        }

        public async Task PostToLobby(string msg)
        {
            if (_hubConnection.State != ConnectionState.Connected)
                return;

        }
        
        public async Task PostToCurrentGame(string msg)
        {
            if (_hubConnection.State != ConnectionState.Connected)
                return;

        }

        public async Task SendGameRequest(string name)
        {
            if (_hubConnection.State != ConnectionState.Connected)
                return;

        }

        public async Task DeclineGameRequest(string name)
        {
            if (_hubConnection.State != ConnectionState.Connected)
                return;

        }

        public async Task CancelGameRequest(string name)
        {
            if (_hubConnection.State != ConnectionState.Connected)
                return;

        }

        public async Task AcceptGameRequest(string name)
        {
            if (_hubConnection.State != ConnectionState.Connected)
                return;

        }

        
        public async Task AbortGame(string name)
        {
            if (_hubConnection.State != ConnectionState.Connected)
                return;

        }
    }
}
