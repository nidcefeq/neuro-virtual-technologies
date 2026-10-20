using System;

public static class GameDifficulty
{
    public enum Level { Low, Mid, High }

    public static Level Current { get; private set; } = Level.Mid;
    public static event Action<Level> OnChanged;

    public static void Set(Level level)
    {
        if (Current == level) return;
        Current = level;
        OnChanged?.Invoke(level);
    }

    // ВАЖНО: инверсия. Уровень стресса -> сложность игры.
    public static void SetFromStressLevel(string stressLevel)
    {
        switch (stressLevel?.Trim().ToLowerInvariant())
        {
            case "low": Set(Level.High); break; // низкий стресс  -> сложнее
            case "high": Set(Level.Low); break; // высокий стресс -> легче
            case "mid":
            default: Set(Level.Mid); break;
        }
    }
}