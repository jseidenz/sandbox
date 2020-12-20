using System.Collections.Generic;
public class HeightMapGenerator
{
    public HeightMapGenerator()
    {
        var elevation_seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        var moisture_seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        m_elevation_noise = new OpenSimplexNoise(elevation_seed);
    }

    public float[] GenerateHeightMap(int width_in_cells, int height_in_cells, double scale)
    {
        float width = (float)width_in_cells;
        float height = (float)height_in_cells;
        float[] height_map = new float[width_in_cells * height_in_cells];
        for(int int_y = 0; int_y < height_in_cells; ++int_y)
        {
            for(int int_x = 0; int_x < width_in_cells; ++int_x)
            {
                int cell_idx = int_y * width_in_cells + int_x;

                float x = (float)int_x;
                float y = (float)int_y;

                var nx = x / width - 0.5f;
                var ny = y / height - 0.5f;
                var elevation = (1.00 * ElevationNoise(1 * nx, 1 * ny)
                       + 0.50 * ElevationNoise(2 * nx, 2 * ny)
                       + 0.25 * ElevationNoise(4 * nx, 4 * ny)
                       + 0.13 * ElevationNoise(8 * nx, 8 * ny)
                       + 0.06 * ElevationNoise(16 * nx, 16 * ny)
                       + 0.03 * ElevationNoise(32 * nx, 32 * ny));
                elevation /= (1.00 + 0.50 + 0.25 + 0.13 + 0.06 + 0.03);
                elevation = System.Math.Pow(elevation, 5.00);

                elevation = elevation * scale;

                height_map[cell_idx] = (float)elevation;
            }
        }

        return height_map;
    }

    public float ElevationNoise(float x, float y)
    {
        return m_elevation_noise.Evaluate(x, y) / 2 + 0.5f;
    }

    OpenSimplexNoise m_elevation_noise;
}