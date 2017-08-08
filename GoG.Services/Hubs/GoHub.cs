using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using GoG.Infrastructure.Services.Multiplayer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Infrastructure;

namespace GoG.Services.Hubs
{
    public class Game
    {
        public string WhiteConnectionId { get; set; }
        public string BlackConnectionId { get; set; }
    }
    
    public class GoHub : Hub
    {
        private readonly List<Game> _games = new List<Game>();
        private readonly Dictionary<string, UserPrefs> _users = new Dictionary<string, UserPrefs>();
        private readonly List<Post> _lobbyPosts = new List<Post>();
        private readonly IConnectionManager _connectionManager;

        public GoHub(IConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public override Task OnConnected()
        {
            if (!_users.ContainsKey(Context.ConnectionId))
                _users.Add(Context.ConnectionId, new UserPrefs());

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            if (stopCalled)
            {
                if (_users.ContainsKey(Context.ConnectionId))
                    _users.Remove(Context.ConnectionId);
            }

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            if (!_users.ContainsKey(Context.ConnectionId))
                _users.Add(Context.ConnectionId, new UserPrefs());

            return base.OnReconnected();
        }

        public void Connect(UserPrefs user)
        {
            if (_users.ContainsKey(Context.ConnectionId))
                _users[Context.ConnectionId] = user;
            else
                _users.Add(Context.ConnectionId, user);
        }

        public void AddLobbyPost(string post)
        {
            if (!_users.TryGetValue(Context.ConnectionId, out var user))
                return;
            if (string.IsNullOrWhiteSpace(user.Name))
                return;

            var newPost = new Post(user.Name, post);

            _lobbyPosts.Add(newPost);
            _connectionManager.GetHubContext<GoHub>().Clients.All.publishLobbyPost(newPost);
        }

        public void AddGamePost(string post)
        {

            if (!_users.TryGetValue(Context.ConnectionId, out var user))
                return;
            if (string.IsNullOrWhiteSpace(user.Name))
                return;

            var newPost = new Post(user.Name, post);

            _lobbyPosts.Add(newPost);
            _connectionManager.GetHubContext<GoHub>().Clients.All.publishLobbyPost(newPost);
        }

        //private void CleanGamesInvolvingUser()
        //{
        //    foreach (var g in _games)
        //    {
        //        if (g.BlackConnectionId == Context.ConnectionId)
        //        {
        //            _connectionManager.GetHubContext<GoHub>().Clients.Client(g.WhiteConnectionId).publishLobbyPost(newPost);
        //        }
        //        if (g.WhiteConnectionId == Context.ConnectionId)
        //        {
        //            _connectionManager.GetHubContext<GoHub>().Clients..All.publishLobbyPost(newPost);
        //        }
        //    }
        //}
    }
}