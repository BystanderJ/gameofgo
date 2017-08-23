namespace GoG.Shared.Engine
{
    
    public class GoMoveHistoryItem
    {
        public int Id { get; set; }

        public GoMove Move { get; set; }
        
        public int Sequence { get; set; }
        
        public GoMoveResult Result { get; set; }
    }
}
