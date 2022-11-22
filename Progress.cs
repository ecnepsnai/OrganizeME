namespace OrganizeME
{
    internal class Progress
    {
        public int Current;
        public int Total;
        public string LogLine;

        public Progress(int current, int total, string logLine = null)
        {
            Current = current;
            Total = total;
            LogLine = logLine;
        }
    }
}
