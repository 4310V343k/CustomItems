using System.Collections.Generic;
using CustomItems.API;

namespace CustomItems.ItemConfigs
{
    public class SniperRifleConfig
    {
        public float DamageMultiplier { get; set; } = 7.5f;
        public int ClipSize { get; set; } = 1;
        public ItemType ItemType { get; set; } = ItemType.GunE11SR;
        public Dictionary<SpawnLocation, float> SpawnLocations { get; set; } = new Dictionary<SpawnLocation, float>();
    }
}