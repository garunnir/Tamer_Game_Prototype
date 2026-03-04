using System;
using System.Collections.Generic;

namespace WildTamer
{
    [Serializable] public class Vec3S { public float x, y, z; }

    [Serializable]
    public class FogData
    {
        public int    width;
        public int    height;
        public string pixelsBase64;    // Base64(byte[width*height]) — one R8 byte per pixel
    }

    [Serializable]
    public class SquadMemberData
    {
        public string monsterId;       // MonsterData SO asset name, e.g. "MonsterData_NormalA"
        public float  hpFraction;      // currentHP / data.MaxHP, clamped [0,1]
        public Vec3S  position;
    }

    [Serializable]
    public class SaveData
    {
        public Vec3S                 playerPosition;
        public FogData               fog;
        public List<SquadMemberData> squad;
        public List<string>          globalUnlocks;   // SO names tamed >= once
        public string                version = "1";
    }
}
