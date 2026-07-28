using Godot;

namespace Game1;

public readonly record struct BalanceProfileSaveResult(bool Success, string Message);

/// <summary>
/// 正式平衡资源的唯一写入口。Release 和独立 Debug 包均只能读取，不能写回项目目录。
/// </summary>
public static class BlockadeCityBalanceProfileStore
{
    public static bool CanSaveFromCurrentEnvironment => OS.IsDebugBuild() && OS.HasFeature("editor");

    public static BlockadeCityBalanceProfile LoadValidated()
    {
        BlockadeCityBalanceProfile profile =
            GD.Load<BlockadeCityBalanceProfile>(BlockadeCityBalanceProfile.OfficialResourcePath);
        if (profile is null)
            throw new System.InvalidOperationException("缺少封锁城区正式平衡 Profile。");
        profile.Validate();
        return profile;
    }

    public static BalanceProfileSaveResult SaveFromEditorDebug(BlockadeCityBalanceSettings settings)
    {
        if (!OS.IsDebugBuild())
            return new BalanceProfileSaveResult(false, "Release 构建禁止保存正式平衡配置。");
        if (!OS.HasFeature("editor"))
            return new BalanceProfileSaveResult(false, "只能从 Godot 编辑器启动的 Debug 游戏保存正式配置。");

        return SaveAndReload(settings, BlockadeCityBalanceProfile.OfficialResourcePath);
    }

    /// <summary>仅供同程序集的 Godot 场景测试验证 ResourceSaver 往返，不触碰正式 Profile。</summary>
    internal static BalanceProfileSaveResult SaveToPathForTesting(
        BlockadeCityBalanceSettings settings,
        string path) => SaveAndReload(settings, path);

    private static BalanceProfileSaveResult SaveAndReload(BlockadeCityBalanceSettings settings, string path)
    {
        try
        {
            settings.Validate();
            BlockadeCityBalanceProfile profile = new();
            profile.Apply(settings);
            Error error = ResourceSaver.Save(profile, path);
            if (error != Error.Ok)
                return new BalanceProfileSaveResult(false, $"保存失败：{error}");

            BlockadeCityBalanceProfile reloaded = ResourceLoader.Load<BlockadeCityBalanceProfile>(
                path,
                cacheMode: ResourceLoader.CacheMode.Replace);
            BlockadeCityBalanceSettings saved = reloaded.ToSettings();
            if (!saved.ApproximatelyEquals(settings))
                return new BalanceProfileSaveResult(false, "保存后重新加载校验不一致。");

            return new BalanceProfileSaveResult(true, $"已固化为正式配置：{saved.ToCompactText()}");
        }
        catch (System.Exception exception)
        {
            return new BalanceProfileSaveResult(false, $"保存失败：{exception.Message}");
        }
    }
}
