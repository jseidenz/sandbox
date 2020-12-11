using System.Collections.Generic;
public class WorldGenerator
{
    public WorldGenerator()
    {
        var elevation_seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        var moisture_seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        m_elevation_noise = new OpenSimplexNoise(elevation_seed);
        m_moisture_noise = new OpenSimplexNoise(moisture_seed);
    }

    public float[] GenerateHeightMap(int width_in_cells, int height_in_cells)
    {
        double width = (double)width_in_cells;
        double height = (double)height_in_cells;
        float[] height_map = new float[width_in_cells * height_in_cells];
        for(int int_y = 0; int_y < height_in_cells; ++int_y)
        {
            for(int int_x = 0; int_x < width_in_cells; ++int_x)
            {
                int cell_idx = int_y * width_in_cells + int_x;

                double x = (float)int_x;
                double y = (float)int_y;

                var nx = x / width - 0.5;
                var ny = y / height - 0.5;
                var elevation = (1.00 * ElevationNoise(1 * nx, 1 * ny)
                       + 0.50 * ElevationNoise(2 * nx, 2 * ny)
                       + 0.25 * ElevationNoise(4 * nx, 4 * ny)
                       + 0.13 * ElevationNoise(8 * nx, 8 * ny)
                       + 0.06 * ElevationNoise(16 * nx, 16 * ny)
                       + 0.03 * ElevationNoise(32 * nx, 32 * ny));
                elevation /= (1.00 + 0.50 + 0.25 + 0.13 + 0.06 + 0.03);
                elevation = System.Math.Pow(elevation, 5.00);

                height_map[cell_idx] = (float)elevation;
            }
        }

        return height_map;
    }

    public double ElevationNoise(double x, double y)
    {
        return m_elevation_noise.Evaluate(x, y);
    }

    public double MoistureNoise(double x, double y)
    {
        return m_moisture_noise.Evaluate(x, y);
    }

    OpenSimplexNoise m_elevation_noise;
    OpenSimplexNoise m_moisture_noise;

    /*
        var rng1 = PM_PRNG.create(seed1);
        var rng2 = PM_PRNG.create(seed2);
        var gen1 = new SimplexNoise(rng1.nextDouble.bind(rng1));
        var gen2 = new SimplexNoise(rng2.nextDouble.bind(rng2));
        function noise1(nx, ny) { return gen1.noise2D(nx, ny) / 2 + 0.5; }
        function noise2(nx, ny) { return gen2.noise2D(nx, ny) / 2 + 0.5; }

    for (var y = 0; y<height; y++) {
      for (var x = 0; x<width; x++) {      
        var nx = x / width - 0.5, ny = y / height - 0.5;
        var e = (1.00 * noise1(1 * nx, 1 * ny)
               + 0.50 * noise1(2 * nx, 2 * ny)
               + 0.25 * noise1(4 * nx, 4 * ny)
               + 0.13 * noise1(8 * nx, 8 * ny)
               + 0.06 * noise1(16 * nx, 16 * ny)
               + 0.03 * noise1(32 * nx, 32 * ny));
        e /= (1.00+0.50+0.25+0.13+0.06+0.03);
        e = Math.pow(e, 5.00);
        var m = (1.00 * noise2(1 * nx, 1 * ny)
               + 0.75 * noise2(2 * nx, 2 * ny)
               + 0.33 * noise2(4 * nx, 4 * ny)
               + 0.33 * noise2(8 * nx, 8 * ny)
               + 0.33 * noise2(16 * nx, 16 * ny)
               + 0.50 * noise2(32 * nx, 32 * ny));
        m /= (1.00+0.75+0.33+0.33+0.33+0.50);
    */
}