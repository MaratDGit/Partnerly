namespace Partnerly.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int GamesPlayed { get; set; }
        public int Victories { get; set; }
        public int Defeats { get; set; }

        // Previous month
        public int PreviousGamesPlayed { get; set; }
        public int PreviousVictories { get; set; }
        public int PreviousDefeats { get; set; }

        public int FailedRuns { get; set; }
        public int PreviousFailedRuns { get; set; }
        public int RunsQueued { get; set; }
        public int RunsRunning { get; set; }
        public int RunsWaiting { get; set; }

        public decimal WinRate
        {
            get
            {
                var completed = Victories + FailedRuns;
                return completed == 0
                    ? 0
                    : Math.Round((decimal)Victories / completed * 100, 1);
            }
        }

        public decimal PreviousWinRate
        {
            get
            {
                var completed = PreviousVictories + PreviousFailedRuns;
                return completed == 0
                    ? 0
                    : Math.Round((decimal)PreviousVictories / completed * 100, 1);
            }
        }

        // Percentage changes
        public decimal GamesPlayedChange =>
            CalculateChange(GamesPlayed, PreviousGamesPlayed);

        public decimal VictoriesChange =>
            CalculateChange(Victories, PreviousVictories);

        public decimal DefeatsChange =>
            CalculateChange(Defeats, PreviousDefeats);

        public decimal WinRateChange =>
            CalculateChange(WinRate, PreviousWinRate);

        // Improvement status
        public bool GamesPlayedImprovement =>
            GamesPlayedChange >= 0;

        public bool VictoriesImprovement =>
            VictoriesChange >= 0;

        public bool DefeatsImprovement =>
            DefeatsChange <= 0;

        public bool WinRateImprovement =>
            WinRateChange >= 0;

        private static decimal CalculateChange(
            decimal current,
            decimal previous)
        {
            if (previous == 0)
                return current == 0 ? 0 : 100;

            return Math.Round(
                (current - previous) / previous * 100,
                1);
        }

        public List<GamesByDayViewModel> GamesByDay { get; set; } = new();

        public List<WinLossViewModel> WinLoss { get; set; } = new();

        public DashboardsViewModel Insights { get; set; } = new();
    }

    public class GamesByDayViewModel
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class WinLossViewModel
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}