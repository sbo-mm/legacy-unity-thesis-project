using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtendedMathUtils
{
    public class MeshAABBSearch
    {
        private static Func<float, float, float, float> min3 = (p, q, r) => {
            return Math.Min(p, Math.Min(q, r));
        };

        private static Func<float, float, float, float> max3 = (p, q, r) => {
            return Math.Max(p, Math.Max(q, r));
        };

        private static Func<Vector2, Vector2, float> cross2d = (lhs, rhs) => {
            return lhs.x * rhs.y - lhs.y * rhs.x;
        };

        private Vector3[] vertices3d;
        private Vector2[] vertices2d;
        private int[] triangles;

        private Rect aabb;
        private List<int>[,] bins;

        private readonly int gridSize;
        private readonly float cellwidth;

        private const int ksize = 5;
        private const float width = 1.0f;

        private static Vector3 PNORM = Vector3.up;

        public MeshAABBSearch(Mesh mesh)
        {
            triangles  = mesh.triangles;
            vertices3d = mesh.vertices;
            vertices2d = Vertices3DTo2D(vertices3d);
            aabb       = Get2DAABB(vertices2d);

            NormalizeVertices2D(aabb, vertices2d);

            float trianglesSqr = Mathf.Sqrt(mesh.triangles.Length / 3);
            float subDivisions = Mathf.Ceil(trianglesSqr);

            cellwidth = width / subDivisions;
            gridSize  = (int)(width / cellwidth);

            int k = ksize - 1;
            bins = new List<int>[gridSize + k, gridSize + k];
            for (int i = 0; i < gridSize + k; i++)
                for (int j = 0; j < gridSize + k; j++)
                    bins[i, j] = new List<int>();

            InitBins();
        }

        private int[] SearchPossibleTris(Vector3 query, List<int> possibleTris)
        {
            foreach (int triIdx in possibleTris)
            {
                Vector3 p = vertices3d[triangles[triIdx]];
                Vector3 q = vertices3d[triangles[triIdx + 1]];
                Vector3 r = vertices3d[triangles[triIdx + 2]];

                GetBarycentricWeights(query, p, q, r,
                    out float wa, out float wb, out float wc
                );

                Vector3 prime = wa * p + wb * q + wc * r;
                float d = (prime - query).magnitude;

                if (d  <= 0.05f           &&
                    wa >= 0 && wa <= 1.0f &&
                    wb >= 0 && wb <= 1.0f &&
                    wc >= 0 && wc <= 1.0f)
                {
                    return new int[]
                    {
                        triangles[triIdx],
                        triangles[triIdx + 1],
                        triangles[triIdx + 2]
                    };
                }
            }
            return null;
        }

        public int[] FindTriangle(Vector3 query)
        {
            Vector2 query2d = ProjectToPlane(query);
            NormalizeVertices2D(aabb, query2d);

            CvtPoint(query2d, out int x, out int y);
            List<int> possibleTris = bins[x, y];

            int[] ret;
            if (possibleTris.Count > 0)
            {
                ret = SearchPossibleTris(query, possibleTris);
                if (ret != null)
                    return ret;
            }
             
            int k = ksize / 2;
            for (int i = y - k; i <= y + k; i++)
            {
                for (int j = x - k; j <= x + k; j++)
                {
                    possibleTris = bins[i, j];
                    ret = SearchPossibleTris(query, possibleTris);
                    if (ret != null)
                        return ret;
                }
            }

            return null;
        }

        private void InitBins()
        {
            Vector2[] tri = new Vector2[3];
            for (int t = 0; t < triangles.Length; t += 3)
            {
                FillTriArray(triangles, t, tri);
                Rect bb = BoundingBox2D(tri[0], tri[1], tri[2]);

                if (CheckIsTriangle(tri))
                    BinTriangle(bb, tri, t);
                else
                    BinLine(bb, t);
            }
        }

        private void FillTriArray(int[] src, int offset, Vector2[] dst)
        {
            const int TRISTEP = 3;
            for (int i = 0; i < TRISTEP; i++)
                dst[i] = vertices2d[src[offset + i]];
        }

        private void BinLine(Rect bb, int triIdx)
        {
            Vector2 s = bb.position;
            Vector2 e = bb.position + bb.size;

            int currentHash = -1, lastHash = -1;
            List<int> hashes = new List<int>();

            const float step = 0.01f;
            for (float t = 0; t <= 1.0f; t+=step)
            {
                Vector2 query = Vector2.Lerp(s, e, t);
                currentHash = HashFunc(query);
                if (hashes.Contains(currentHash))
                    continue;

                lastHash = PlaceInBin(query, triIdx);
                hashes.Add(lastHash);
            }
        }

        private void BinTriangle(Rect bb, Vector2[] tri, int triIdx)
        {
            const float stepSize = 10.0f;
            float stepY = bb.height / stepSize;
            float stepX = bb.width  / stepSize;

            int currentHash = -1, lastHash = -1;
            List<int> hashes = new List<int>();

            Vector2 query;
            for (query.y = bb.yMin; query.y <= bb.yMax; query.y += stepY)
            {
                for (query.x = bb.xMin; query.x <= bb.xMax; query.x += stepX)
                {
                    currentHash = HashFunc(query);
                    if (hashes.Contains(currentHash))
                        break;

                    if (IsPointInTriangle(query, tri))
                    {
                        lastHash = PlaceInBin(query, triIdx);
                        hashes.Add(lastHash);
                    }
                }
            }
        }

        private void CvtPoint(Vector2 p, out int x, out int y)
        {
            int _x = (int)((p.x + 0.5f) / cellwidth);
            int _y = (int)((p.y + 0.5f) / cellwidth);
            x = Math.Min(_x, gridSize - 1) + (ksize / 2);
            y = Math.Min(_y, gridSize - 1) + (ksize / 2);
        }

        private int HashFunc(Vector2 point)
        {
            CvtPoint(point, out int x, out int y);
            return x + y * gridSize;
        }

        private int PlaceInBin(Vector2 query, int triIdx)
        {       
            CvtPoint(query, out int x, out int y);
            if (bins[x, y].Contains(triIdx))
            {
                throw new Exception(
                    "Bin cannot contain a " +
                    "multiple of the same element"
                );
            }

            bins[x, y].Add(triIdx);
            return HashFunc(query);
        }

        private bool IsPointInTriangle(Vector2 query, params Vector2[] tri)
        {
            Vector2 A = tri[0];
            Vector2 B = tri[1];
            Vector2 C = tri[2];

            double xd = cross2d(A, B) + cross2d(B, C) + cross2d(C, A);

            if (Math.Abs(xd) < 1e-13)
                return false;

            bool p, q, r;
            double xa, xb, xc, wa, wb, wc;
            xa = cross2d(B, C) + cross2d(query, B - C);
            xb = cross2d(C, A) + cross2d(query, C - A);
            xc = cross2d(A, B) + cross2d(query, A - B);
            wa = xa / xd;
            wb = xb / xd;
            wc = xc / xd;

            p = wa >= 0 && wa <= 1.0;
            q = wb >= 0 && wb <= 1.0;
            r = wc >= 0 && wc <= 1.0;
            return p && q && r;
        }

        private void GetBarycentricWeights(Vector3 query,
            Vector3 p, Vector3 q, Vector3 r, 
            out float wa, out float wb, out float wc)
        {
            Vector3 u = q - p;
            Vector3 v = r - p;
            Vector3 w = query - p;
            Vector3 n = Vector3.Cross(u, v);

            float oneOverNormSqr = 1.0f / Vector3.Dot(n, n);
            wc = Vector3.Dot(Vector3.Cross(u, w), n) * oneOverNormSqr;
            wb = Vector3.Dot(Vector3.Cross(w, v), n) * oneOverNormSqr;
            wa = 1.0f - wc - wb;
        }

        private bool CheckIsTriangle(params Vector2[] tri)
        {
            Vector2 a = tri[0];
            Vector2 b = tri[1];
            Vector2 c = tri[2];

            float area = a.x * (b.y - c.y)
                + b.x * (c.y - a.y)
                + c.x * (a.y - b.y);
                
            return area > 1e-15f;
        }

        private void NormalizeVertices2D(Rect AABB, params Vector2[] verts)
        {
            float oneOverWidth  = 1.0f / AABB.width;
            float oneOverHeight = 1.0f / AABB.height;
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i].x = verts[i].x * oneOverWidth;
                verts[i].y = verts[i].y * oneOverHeight; 
            }
        }

        private Rect Get2DAABB(Vector2[] verts2d)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < vertices2d.Length; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    if (verts2d[i][j] < min[j]) min[j] = verts2d[i][j];
                    if (verts2d[i][j] > max[j]) max[j] = verts2d[i][j];
                }
            }
            return new Rect(min, max - min);
        }

        private Vector2 ProjectToPlane(Vector3 v)
        {
            Vector3 _p = Vector3.ProjectOnPlane(v, PNORM);
            return new Vector2(_p.x, _p.z);
        }

        private Vector2[] Vertices3DTo2D(Vector3[] verts3d)
        {
            Vector2[] verts2d = new Vector2[verts3d.Length];
            for (int i = 0; i < verts2d.Length; i++)
                verts2d[i] = ProjectToPlane(verts3d[i]);

            return verts2d;
        }

        private Rect BoundingBox2D(Vector2 a, Vector2 b, Vector2 c)
        {
            return new Rect
            {
                xMin = min3(a.x, b.x, c.x),
                yMin = min3(a.y, b.y, c.y),
                xMax = max3(a.x, b.x, c.x),
                yMax = max3(a.y, b.y, c.y)
            };
        }
    }

    public static class AABBUtils
    {
        public static float halfWidth(this Rect self)
        {
            return self.width * 0.5f;
        }

        public static float halfHeight(this Rect self)
        {
            return self.height * 0.5f;
        }
    }
}