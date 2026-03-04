using System;

namespace WildTamer
{
    public static class GlobalEvents
    {
        public static event Action<ITargetable> OnUnitDied;
        public static event Action<ITameable>   OnTamingSucceeded;

        internal static void FireUnitDied(ITargetable unit)      => OnUnitDied?.Invoke(unit);
        internal static void FireTamingSucceeded(ITameable unit) => OnTamingSucceeded?.Invoke(unit);
    }
}
