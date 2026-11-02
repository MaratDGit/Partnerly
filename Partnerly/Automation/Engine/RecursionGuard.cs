namespace Partnerly.Automation.Engine
{
    public static class RecursionGuard
    {
        public const int MaxDepth = 3;
        private static readonly AsyncLocal<int> Depth = new();

        public static int Current => Depth.Value;

        public static IDisposable Enter()
        {
            Depth.Value++;
            return new Pop();
        }

        public static bool CanEnter => Depth.Value < MaxDepth;

        private sealed class Pop : IDisposable
        {
            public void Dispose()
            {
                if (Depth.Value > 0)
                {
                    Depth.Value--;
                }
            }
        }
    }
}
