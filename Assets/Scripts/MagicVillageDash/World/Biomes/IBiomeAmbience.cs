namespace MagicVillageDash.World.Biomes
{
    /// <summary>An ambient effect shared by one or more biomes that can be toggled on/off.</summary>
    public interface IBiomeAmbience
    {
        /// <summary>True if this effect belongs to the given biome.</summary>
        bool Handles(BiomeDefinition biome);
        void Activate();
        void Deactivate();
    }
}
