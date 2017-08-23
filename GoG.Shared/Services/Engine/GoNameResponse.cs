using GoG.Shared.Engine;

namespace GoG.Shared.Services.Engine
{
    /// <summary>
    /// Result of the Name command.
    /// </summary>
    public class GoNameResponse : GoResponse
    {
        // This empty constructor is so WCF's DataContractSerializer is able to build an instance of this type.
        public GoNameResponse()
        {
        }

        public GoNameResponse(GoResultCode resultCode, string name)
            : base(resultCode)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}