using UnityEngine;

namespace WildTamer
{
    public interface ITargetable
    {
        Transform Transform { get; }
        bool      IsAlive   { get; }
        FactionId Faction   { get; }
    }
}
