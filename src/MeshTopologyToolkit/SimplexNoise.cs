using System;
using System.Numerics;

namespace MeshTopologyToolkit
{

    public static class SimplexNoise
    {
        // Gradients for 2D
        private static readonly int[][] Grad2 =
        {
            new[] { 1, 1 }, new[] { -1, 1 }, new[] { 1, -1 }, new[] { -1, -1 },
            new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }
        };

        // Gradients for 3D
        private static readonly int[][] Grad3 =
        {
            new[] { 1, 1, 0 }, new[] { -1, 1, 0 }, new[] { 1, -1, 0 }, new[] { -1, -1, 0 },
            new[] { 1, 0, 1 }, new[] { -1, 0, 1 }, new[] { 1, 0, -1 }, new[] { -1, 0, -1 },
            new[] { 0, 1, 1 }, new[] { 0, -1, 1 }, new[] { 0, 1, -1 }, new[] { 0, -1, -1 }
        };

        // Gradients for 4D
        private static readonly int[][] Grad4 =
        {
        new[] { 0, 1, 1, 1 }, new[] { 0, 1, 1, -1 }, new[] { 0, 1, -1, 1 }, new[] { 0, 1, -1, -1 },
        new[] { 0, -1, 1, 1 }, new[] { 0, -1, 1, -1 }, new[] { 0, -1, -1, 1 }, new[] { 0, -1, -1, -1 },
        new[] { 1, 0, 1, 1 }, new[] { 1, 0, 1, -1 }, new[] { 1, 0, -1, 1 }, new[] { 1, 0, -1, -1 },
        new[] { -1, 0, 1, 1 }, new[] { -1, 0, 1, -1 }, new[] { -1, 0, -1, 1 }, new[] { -1, 0, -1, -1 },
        new[] { 1, 1, 0, 1 }, new[] { 1, 1, 0, -1 }, new[] { 1, -1, 0, 1 }, new[] { 1, -1, 0, -1 },
        new[] { -1, 1, 0, 1 }, new[] { -1, 1, 0, -1 }, new[] { -1, -1, 0, 1 }, new[] { -1, -1, 0, -1 },
        new[] { 1, 1, 1, 0 }, new[] { 1, 1, -1, 0 }, new[] { 1, -1, 1, 0 }, new[] { 1, -1, -1, 0 },
        new[] { -1, 1, 1, 0 }, new[] { -1, 1, -1, 0 }, new[] { -1, -1, 1, 0 }, new[] { -1, -1, -1, 0 }
    };

        // Permutation table
        private static readonly int[] Perm = new int[512];

        // Original permutation
        private static readonly int[] P =
        {
            151,160,137,91,90,15,
            131,13,201,95,96,53,194,233,7,225,140,36,
            103,30,69,142,8,99,37,240,21,10,23,
            190,6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,
            35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,
            168,68,175,74,165,71,134,139,48,27,166,77,146,158,231,83,
            111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
            102,143,54,65,25,63,161,1,216,80,73,209,76,132,187,208,
            89,18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,
            186,3,64,52,217,226,250,124,123,5,202,38,147,118,126,255,
            82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,223,
            183,170,213,119,248,152,2,44,154,163,70,221,153,101,155,167,
            43,172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,
            185,112,104,218,246,97,228,251,34,242,193,238,210,144,12,191,
            179,162,241,81,51,145,235,249,14,239,107,49,192,214,31,181,
            199,106,157,184,84,204,176,115,121,50,45,127,4,150,254,138,
            236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,
            61,156,180
        };

        static SimplexNoise()
        {
            for (int i = 0; i < 512; i++)
                Perm[i] = P[i & 255];
        }

        private static int FastFloor(float x) => x > 0 ? (int)x : (int)x - 1;

        private static float Dot(int[] g, float x, float y) => g[0] * x + g[1] * y;

        private static float Dot(int[] g, float x, float y, float z) => g[0] * x + g[1] * y + g[2] * z;

        private static float Dot(int[] g, float x, float y, float z, float w) => g[0] * x + g[1] * y + g[2] * z + g[3] * w;


        /// <summary>
        /// 2D Simplex noise
        /// </summary>
        public static float Noise(Vector2 pos)
        {
            const float F2 = 0.366025403f; // (sqrt(3) - 1) / 2
            const float G2 = 0.211324865f; // (3 - sqrt(3)) / 6

            float x = pos.X;
            float y = pos.Y;

            float s = (x + y) * F2;
            int i = FastFloor(x + s);
            int j = FastFloor(y + s);

            float t = (i + j) * G2;
            float X0 = i - t;
            float Y0 = j - t;

            float x0 = x - X0;
            float y0 = y - Y0;

            int i1, j1;
            if (x0 > y0)
            {
                i1 = 1; j1 = 0;
            }
            else
            {
                i1 = 0; j1 = 1;
            }

            float x1 = x0 - i1 + G2;
            float y1 = y0 - j1 + G2;

            float x2 = x0 - 1f + 2f * G2;
            float y2 = y0 - 1f + 2f * G2;

            int ii = i & 255;
            int jj = j & 255;

            float n0 = 0f, n1 = 0f, n2 = 0f;

            float t0 = 0.5f - x0 * x0 - y0 * y0;
            if (t0 > 0)
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(Grad2[Perm[ii + Perm[jj]] % 8], x0, y0);
            }

            float t1 = 0.5f - x1 * x1 - y1 * y1;
            if (t1 > 0)
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(Grad2[Perm[ii + i1 + Perm[jj + j1]] % 8], x1, y1);
            }

            float t2 = 0.5f - x2 * x2 - y2 * y2;
            if (t2 > 0)
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(Grad2[Perm[ii + 1 + Perm[jj + 1]] % 8], x2, y2);
            }

            // Scale result to roughly [-1, 1]
            return 70f * (n0 + n1 + n2);
        }

        public static float Turbulence(Vector2 pos, int octaves)
        {
            float sum = 0f;
            float frequency = 1f;
            float amplitude = 1f;

            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * MathF.Abs(Noise(pos * frequency));

                frequency *= 2f;
                amplitude *= 0.5f;
            }

            return sum;
        }

        public static float FractalNoise(Vector2 pos,
            int octaves, 
            float lacunarity = 2f,
            float persistence = 0.5f)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float max = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * Noise(pos * frequency);
                max += amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return sum / max; // Normalize to ~[-1, 1]
        }

        /// <summary>
        /// 3D Simplex noise
        /// </summary>
        public static float Noise(Vector3 pos)
        {
            const float F3 = 1f / 3f;
            const float G3 = 1f / 6f;

            float x = pos.X;
            float y = pos.Y;
            float z = pos.Z;

            float s = (x + y + z) * F3;
            int i = FastFloor(x + s);
            int j = FastFloor(y + s);
            int k = FastFloor(z + s);

            float t = (i + j + k) * G3;
            float X0 = i - t;
            float Y0 = j - t;
            float Z0 = k - t;

            float x0 = x - X0;
            float y0 = y - Y0;
            float z0 = z - Z0;

            int i1, j1, k1;
            int i2, j2, k2;

            if (x0 >= y0)
            {
                if (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; }
                else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; }
            }
            else
            {
                if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; }
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; }
                else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
            }

            float x1 = x0 - i1 + G3;
            float y1 = y0 - j1 + G3;
            float z1 = z0 - k1 + G3;

            float x2 = x0 - i2 + 2f * G3;
            float y2 = y0 - j2 + 2f * G3;
            float z2 = z0 - k2 + 2f * G3;

            float x3 = x0 - 1f + 3f * G3;
            float y3 = y0 - 1f + 3f * G3;
            float z3 = z0 - 1f + 3f * G3;

            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;

            float n0 = 0, n1 = 0, n2 = 0, n3 = 0;

            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 > 0)
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(Grad3[Perm[ii + Perm[jj + Perm[kk]]] % 12], x0, y0, z0);
            }

            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 > 0)
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(Grad3[Perm[ii + i1 + Perm[jj + j1 + Perm[kk + k1]]] % 12], x1, y1, z1);
            }

            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 > 0)
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(Grad3[Perm[ii + i2 + Perm[jj + j2 + Perm[kk + k2]]] % 12], x2, y2, z2);
            }

            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 > 0)
            {
                t3 *= t3;
                n3 = t3 * t3 * Dot(Grad3[Perm[ii + 1 + Perm[jj + 1 + Perm[kk + 1]]] % 12], x3, y3, z3);
            }

            // Scale to roughly [-1, 1]
            return 32f * (n0 + n1 + n2 + n3);
        }

        public static float FractalNoise(Vector3 pos,
           int octaves,
           float lacunarity = 2f,
           float persistence = 0.5f)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float max = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * Noise(pos * frequency);
                max += amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return sum / max; // Normalize to ~[-1, 1]
        }

        /// <summary>
        /// 4D Simplex noise
        /// </summary>
        public static float Noise(Vector4 pos)
        {
            float x = pos.X;
            float y = pos.Y;
            float z = pos.Z;
            float w = pos.W;

            float F4 = (MathF.Sqrt(5f) - 1f) / 4f;
            float G4 = (5f - MathF.Sqrt(5f)) / 20f;

            float s = (x + y + z + w) * F4;
            int i = FastFloor(x + s);
            int j = FastFloor(y + s);
            int k = FastFloor(z + s);
            int l = FastFloor(w + s);

            float t = (i + j + k + l) * G4;
            float X0 = i - t;
            float Y0 = j - t;
            float Z0 = k - t;
            float W0 = l - t;

            float x0 = x - X0;
            float y0 = y - Y0;
            float z0 = z - Z0;
            float w0 = w - W0;

            int rankx = 0, ranky = 0, rankz = 0, rankw = 0;

            if (x0 > y0) rankx++; else ranky++;
            if (x0 > z0) rankx++; else rankz++;
            if (x0 > w0) rankx++; else rankw++;
            if (y0 > z0) ranky++; else rankz++;
            if (y0 > w0) ranky++; else rankw++;
            if (z0 > w0) rankz++; else rankw++;

            int i1 = rankx >= 3 ? 1 : 0;
            int j1 = ranky >= 3 ? 1 : 0;
            int k1 = rankz >= 3 ? 1 : 0;
            int l1 = rankw >= 3 ? 1 : 0;

            int i2 = rankx >= 2 ? 1 : 0;
            int j2 = ranky >= 2 ? 1 : 0;
            int k2 = rankz >= 2 ? 1 : 0;
            int l2 = rankw >= 2 ? 1 : 0;

            int i3 = rankx >= 1 ? 1 : 0;
            int j3 = ranky >= 1 ? 1 : 0;
            int k3 = rankz >= 1 ? 1 : 0;
            int l3 = rankw >= 1 ? 1 : 0;

            float x1 = x0 - i1 + G4;
            float y1 = y0 - j1 + G4;
            float z1 = z0 - k1 + G4;
            float w1 = w0 - l1 + G4;

            float x2 = x0 - i2 + 2f * G4;
            float y2 = y0 - j2 + 2f * G4;
            float z2 = z0 - k2 + 2f * G4;
            float w2 = w0 - l2 + 2f * G4;

            float x3 = x0 - i3 + 3f * G4;
            float y3 = y0 - j3 + 3f * G4;
            float z3 = z0 - k3 + 3f * G4;
            float w3 = w0 - l3 + 3f * G4;

            float x4 = x0 - 1f + 4f * G4;
            float y4 = y0 - 1f + 4f * G4;
            float z4 = z0 - 1f + 4f * G4;
            float w4 = w0 - 1f + 4f * G4;

            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;
            int ll = l & 255;

            float n0 = 0, n1 = 0, n2 = 0, n3 = 0, n4 = 0;

            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0 - w0 * w0;
            if (t0 > 0)
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(Grad4[Perm[ii + Perm[jj + Perm[kk + Perm[ll]]]] % 32], x0, y0, z0, w0);
            }

            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1 - w1 * w1;
            if (t1 > 0)
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(Grad4[Perm[ii + i1 + Perm[jj + j1 + Perm[kk + k1 + Perm[ll + l1]]]] % 32], x1, y1, z1, w1);
            }

            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2 - w2 * w2;
            if (t2 > 0)
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(Grad4[Perm[ii + i2 + Perm[jj + j2 + Perm[kk + k2 + Perm[ll + l2]]]] % 32], x2, y2, z2, w2);
            }

            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3 - w3 * w3;
            if (t3 > 0)
            {
                t3 *= t3;
                n3 = t3 * t3 * Dot(Grad4[Perm[ii + i3 + Perm[jj + j3 + Perm[kk + k3 + Perm[ll + l3]]]] % 32], x3, y3, z3, w3);
            }

            float t4 = 0.6f - x4 * x4 - y4 * y4 - z4 * z4 - w4 * w4;
            if (t4 > 0)
            {
                t4 *= t4;
                n4 = t4 * t4 * Dot(Grad4[Perm[ii + 1 + Perm[jj + 1 + Perm[kk + 1 + Perm[ll + 1]]]] % 32], x4, y4, z4, w4);
            }

            // Scale to roughly [-1, 1]
            return 27f * (n0 + n1 + n2 + n3 + n4);
        }


        public static float FractalNoise(Vector4 pos,
            int octaves,
            float lacunarity = 2f,
            float persistence = 0.5f)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float max = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * Noise(pos * frequency);
                max += amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return sum / max; // Normalize to ~[-1, 1]
        }
    }
}
