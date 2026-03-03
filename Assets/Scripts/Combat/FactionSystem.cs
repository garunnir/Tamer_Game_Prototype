namespace WildTamer
{
    public enum FactionId { Enemy, Player, Neutral }

    /// <summary>
    /// Static utility for faction relationship rules.
    /// Does not require a scene object — use directly via FactionSystem.AreHostile().
    /// </summary>
    public static class FactionSystem
    {
        /// <summary>
        /// Returns true if the two factions are actively hostile to each other.
        /// Same faction and Neutral are never hostile.
        /// </summary>
        public static bool AreHostile(FactionId a, FactionId b)
        {
            if (a == b) return false;
            if (a == FactionId.Neutral || b == FactionId.Neutral) return false;
            return (a == FactionId.Enemy  && b == FactionId.Player)
                || (a == FactionId.Player && b == FactionId.Enemy);
        }

        /// <summary>Converts a FactionId to the binary CombatTeam used by CombatSystem.</summary>
        public static CombatTeam ToCombatTeam(FactionId id)
            => id == FactionId.Player ? CombatTeam.Ally : CombatTeam.Enemy;
    }
}
