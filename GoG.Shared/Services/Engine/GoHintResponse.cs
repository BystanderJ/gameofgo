using GoG.Shared.Engine;

namespace GoG.Shared.Services.Engine
{
    /// <summary>
    /// A list of positions response.
    /// </summary>
    public class GoHintResponse : GoResponse
    {
        // This empty constructor is so WCF's DataContractSerializer is able to build an instance of this type.
        public GoHintResponse()
        {
        }

        public GoHintResponse(GoResultCode resultCode, GoMove move)
            : base(resultCode)
        {
            Move = move;
        }
        
        public GoMove Move { get; set; }
    }
}