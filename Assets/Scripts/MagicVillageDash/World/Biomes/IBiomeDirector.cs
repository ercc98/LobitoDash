namespace MagicVillageDash.World.Biomes
{
    public interface IBiomeDirector
    {
        void ResetRun();
        ChunkFactory GetNextFactory(out bool spawnObstacles);
        BiomeDefinition CurrentBiome { get; }
    }
}