namespace Game1;

public sealed class RunState
{
    public required int Seed { get; init; }

    public int RelayIntegrity { get; set; }

    public int PlayerArmor { get; set; }

    public int RebootsRemaining { get; set; }

    public int RoomIndex { get; set; }

    public static RunState CreateNew(
        int seed,
        int relayIntegrity = 100,
        int armor = 100,
        int reboots = 1)
    {
        return new RunState
        {
            Seed = seed,
            RelayIntegrity = relayIntegrity,
            PlayerArmor = armor,
            RebootsRemaining = reboots,
            RoomIndex = 0
        };
    }
}
