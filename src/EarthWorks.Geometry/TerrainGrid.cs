namespace OstrixMods.EarthWorks.Geometry
{
    public static class TerrainGrid
    {
        public static bool TryGetIndex(int width, int x, int z, out int index)
        {
            if (width < 0 || x < 0 || z < 0 || x > width || z > width)
            {
                index = -1;
                return false;
            }

            index = z * (width + 1) + x;
            return true;
        }
    }
}
