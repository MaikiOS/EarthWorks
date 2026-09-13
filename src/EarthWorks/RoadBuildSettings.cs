namespace OstrixMods.EarthWorks
{
    internal readonly struct RoadBuildSettings
    {
        public RoadBuildSettings(
            int subdivisionsPerSegment,
            float terrainDelta,
            float maximumGradeRatio,
            float shoulderWidth,
            int maximumVertices)
        {
            SubdivisionsPerSegment = subdivisionsPerSegment;
            TerrainDelta = terrainDelta;
            MaximumGradeRatio = maximumGradeRatio;
            ShoulderWidth = shoulderWidth;
            MaximumVertices = maximumVertices;
        }

        public int SubdivisionsPerSegment { get; }
        public float TerrainDelta { get; }
        public float MaximumGradeRatio { get; }
        public float ShoulderWidth { get; }
        public int MaximumVertices { get; }

        public static RoadBuildSettings FromCurrentConfig()
        {
            return new RoadBuildSettings(
                EarthWorksPlugin.EffectiveCurveSubdivisions,
                EarthWorksPlugin.EffectiveTerrainDelta,
                EarthWorksPlugin.EffectiveMaximumGradeRatio,
                EarthWorksPlugin.EffectiveShoulderWidth,
                EarthWorksPlugin.EffectiveMaximumVertices);
        }
    }
}
