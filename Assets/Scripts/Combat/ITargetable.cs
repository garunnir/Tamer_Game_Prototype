using UnityEngine;

namespace WildTamer
{
    /// <summary>Contract for objects that can be targeted (position, alive, faction for hostility).</summary>
    public interface ITargetable : IHasFaction
    {
        Transform Transform { get; }
        bool      IsAlive   { get; }
    }
}
