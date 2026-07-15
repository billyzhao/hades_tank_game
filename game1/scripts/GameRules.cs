using Godot;

public static class GameRules
{
    public static int NextScore(int currentScore)
    {
        return currentScore + 1;
    }

    public static Vector2 TargetPositionForScore(int score, Vector2 playfield, float margin)
    {
        float usableWidth = Mathf.Max(playfield.X - margin * 2.0f, 1.0f);
        float usableHeight = Mathf.Max(playfield.Y - margin * 2.0f, 1.0f);
        float xSeed = Mathf.PosMod(score * 137.0f + 211.0f, usableWidth);
        float ySeed = Mathf.PosMod(score * 83.0f + 97.0f, usableHeight);

        return new Vector2(margin + xSeed, margin + ySeed);
    }
}
