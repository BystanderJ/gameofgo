namespace GoG.Infrastructure.Services.Multiplayer
{
    public class Post
    {
        public string ByUser { get; set; }
        public string Msg { get; set; }

        public Post()
        {
        }

        public Post(string byUser, string msg)
        {
            ByUser = byUser;
            Msg = msg;
        }
    }
}
