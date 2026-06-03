namespace MagicVillageDash.World
{
    /// <summary>Read-only spatial query over the currently streamed chunks.</summary>
    public interface IActiveChunkQuery
    {
        /// <summary>The active chunk whose Z-span contains <paramref name="worldZ"/>, or null.</summary>
        ChunkRoot GetChunkContaining(float worldZ);
    }
}
