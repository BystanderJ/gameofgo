namespace GoG.Infrastructure.Engine
{
    
    public class GoPlayer
    {
        public int Id { get; set; }

        public string Name { get; set; }
        
        public PlayerType PlayerType { get; set; }
        
        public decimal Komi { get; set; }
        
        public int Level { get; set; }
    }
}
