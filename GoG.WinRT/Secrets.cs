namespace GoG.WinRT
{
    public static class Secrets
    {
#if DEBUG
        public const string GoHubUrl = "http://localhost:58388";
#else
        public const string GoHubUrl = "";
#endif
    }
}
