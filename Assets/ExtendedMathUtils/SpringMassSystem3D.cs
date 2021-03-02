using System;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace ExtendedMathUtils
{
    public class SpringMassSystem3D
    {
        private const int dof = 3;
        public static int DOF
        {
            get { return dof; }
        }

        private const int elDof = 2;
        public static int ELDOF
        {
            get { return elDof; }
        }

        public Matrix<double> StiffnessMatrix { get; private set; }
        public Matrix<double> MassMatrix { get; private set; }

        private List<int[]> connectivityMap;
        //private Dictionary<int, int[]> connectivityMap;

        private List<Element> elements;
        private Dictionary<Node, List<double>> faceContribs;
        private SortedDictionary<int, Node> lookup;

        private const float VERT_THRESH = 1e-10f;
        private const float BUCKET_STEP = 10f;

        private SM_Material props;

        private int[] tri; 
        private Vector3[] mesh_verts;
        private Matrix4x4 local2World;

        private double k;
        private Matrix<double> localStiffnessMatrix;

        private static MatrixBuilder<double> MB = Matrix<double>.Build;

        public SpringMassSystem3D(Transform transform, Mesh mesh, SM_Material props)
        {
            SM_Utils.AutoWeld(mesh, VERT_THRESH, BUCKET_STEP);

            this.local2World = transform.localToWorldMatrix;
            this.mesh_verts = mesh.vertices;
            this.tri = mesh.triangles;
            this.props = props;

            elements = new List<Element>();
            lookup = new SortedDictionary<int, Node>();
            faceContribs = new Dictionary<Node, List<double>>();

            k = props.Youngs * props.Thickness;
            double[,] st = {
                { k,  k,  k, -k, -k, -k},
                { k,  k,  k, -k, -k, -k},
                { k,  k,  k, -k, -k, -k},
                {-k, -k, -k,  k,  k,  k},
                {-k, -k, -k,  k,  k,  k},
                {-k, -k, -k,  k,  k,  k}
            };

            connectivityMap = new List<int[]>();
            localStiffnessMatrix = MB.DenseOfArray(st);

            FindElements();
            FormStiffness3D();
            FormMass3D();
        }

        private Node TryCreateNode(int key, Vector3 v)
        {
            if (!lookup.ContainsKey(key))
                lookup.Add(key, new Node(key, v));

            return lookup[key];
        }

        private void _swap(ref double p, ref double q)
        {
            double aux = p;
            p = q;
            q = aux;
        }

        private void _arrangeHeron(ref double a, ref double b, ref double c)
        {
            if (a <= b)
                _swap(ref a, ref b);
            if (a <= c)
                _swap(ref a, ref c);
            if (b <= c)
                _swap(ref b, ref c);
        }

        private double HeronArea(Element ea, Element eb, Element ec)
        {
            const double oneOverFour = 1.0 / 4.0;

            double a = ea.L;
            double b = eb.L;
            double c = ec.L;

            _arrangeHeron(ref a, ref b, ref c);

            double h = (a + (b + c)) 
                * (c - (a - b)) 
                * (c + (a - b)) 
                * (a + (b - c));

            return oneOverFour * Math.Sqrt(h);
        }

        private void SetFaceContribs(Element[] triangle)
        {
            const double oneThird = 0.333;
            double A = HeronArea(triangle[0], triangle[1], triangle[2]);
            double Ae = A * oneThird;
            for (int j = 0; j < triangle.Length; j++)
            {
                Node n = triangle[j].u;
                if (!faceContribs.ContainsKey(n))
                    faceContribs.Add(n, new List<double>());

                faceContribs[n].Add(Ae);
            }
        }

        private void FindElements()
        {
            Vector3[] verts = new Vector3[mesh_verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = local2World * mesh_verts[i];
            }

            //bool[,] edgeSet = new bool[verts.Length, verts.Length];

            int TRISTEP = 3;
            Element[] triangle = new Element[TRISTEP];
            for (int i = 0; i < tri.Length; i += TRISTEP)
            {
                int triIdx = i / TRISTEP;
                for (int j = 0; j < TRISTEP; j++)
                {
                    int u = tri[i + j];
                    int v = tri[i + (j + 1) % TRISTEP];
                    //if (edgeSet[u, v])
                    //    continue;

                    int[] nodes = { u, v };
                    connectivityMap.Add(nodes);

                    if ((triIdx % 2) == 1)
                    {
                        int aux = u;
                        u = v;
                        v = aux;
                    }

                    Node p = TryCreateNode(u, verts[u]);
                    Node q = TryCreateNode(v, verts[v]);
                    Element e = new Element(p, q);
                    elements.Add(e);
                    triangle[j] = e;

                    //edgeSet[u, v] = true;
                    //edgeSet[v, u] = true;
                }
                SetFaceContribs(triangle);
            }
        }

        private Matrix<double> GetLocalT(Vector3 v1, Vector3 v2, double L)
        {
            double fac = 1.0 / L; 
            double Cx = (v2.x - v1.x) * fac;
            double Cy = (v2.y - v1.y) * fac;
            double Cz = (v2.z - v1.z) * fac;

            double[,] t =
            {
                {Cx*Cx, Cx*Cy, Cx*Cz},
                {Cy*Cx, Cy*Cy, Cy*Cz},
                {Cz*Cx, Cz*Cy, Cz*Cz}
            };

            return MB.DenseOfArray(t);
        }

        private Matrix<double> GetGlobalTransformationMatrix(Vector3 v1, Vector3 v2, double L)
        {
            int LDof = DOF * ELDOF;
            Matrix<double> t = GetLocalT(v1, v2, L);
            Matrix<double> T = MB.Dense(LDof, LDof);
            T.SetSubMatrix(0, 0, t);
            T.SetSubMatrix(DOF, DOF, t);
            T.SetSubMatrix(0, DOF, -t);
            T.SetSubMatrix(DOF, 0, -t);
            return T;
        }
        
        private void FormStiffness3D()
        {
            int GDof = lookup.Count * DOF;
            Matrix<double> K = MB.Sparse(GDof, GDof, 0);

            Matrix<double>  loc;
            foreach (int[] conn in connectivityMap)
            {
                loc = localStiffnessMatrix;
                int _u = DOF * (conn[0] + 1);
                int _v = DOF * (conn[1] + 1);

                int[] elementDof = {
                    _u - 3, _u - 2, _u - 1,
                    _v - 3, _v - 2, _v - 1
                };

                for (int i = 0; i < elementDof.Length; i++)
                {
                    for (int j = 0; j < elementDof.Length; j++)
                    {
                        K[elementDof[i], elementDof[j]] += loc[i, j];
                    }
                }
            }

            /*
            Matrix<double> T, loc;
            foreach (Element el in elements)
            {
                T = GetGlobalTransformationMatrix(el.u.Pos, el.v.Pos, el.L);
                loc = k * T;

                int _u = DOF * (el.u.Idx + 1);
                int _v = DOF * (el.v.Idx + 1);
                int[] elementDof = {
                    _u - 3, _u - 2, _u - 1,
                    _v - 3, _v - 2, _v - 1
                 };

                for (int i= 0; i < elementDof.Length; i++)
                {
                    for (int j = 0; j < elementDof.Length; j++)
                    {
                        K[elementDof[i], elementDof[j]] += loc[i, j];
                    }
                }
            }
            */

            StiffnessMatrix = K;
        }

        private double GetNodeMass(Node n)
        {
            double faceSum = 0.0;
            List<double> contribs = faceContribs[n];
            for (int i = 0; i < contribs.Count; i++)
            {
                faceSum += contribs[i];
            }
            
            return props.Density * props.Thickness * faceSum;
        }

        private void FormMass3D()
        {
            int GDof = lookup.Count * DOF;
            Matrix<double> M = MB.Sparse(GDof, GDof, 0);
            for (int i = 0; i < lookup.Count; i++)
            {
                Node n = lookup[i];
                double mass = GetNodeMass(n);
                int p = DOF * (n.Idx + 1) - 3;
                int q = DOF * (n.Idx + 1) - 2;
                int r = DOF * (n.Idx + 1) - 1;
                M[p, p] = mass;
                M[q, q] = mass;
                M[r, r] = mass;
            }

            MassMatrix = M;
        }

        private sealed class Node
        {
            public int Idx     { get; private set; }
            public Vector3 Pos { get; private set; }

            public Node(int idx, Vector3 pos)
            {
                Idx = idx;
                Pos = pos;
            }
        }

        private sealed class Element
        {
            public Node u { get; private set; }
            public Node v { get; private set; }
            public double L { get; private set; }

            public Element(Node u, Node v)
            {
                this.u = u;
                this.v = v;
                this.L = (v.Pos - u.Pos).magnitude;
            }
        }
    }

    [System.Serializable]
    public struct SM_Material
    {
        public double Poisson;
        public double Youngs;
        public double Thickness;
        public double Density;
        public double FluidC;
        public double ViscoC;

        public static SM_Material Default { get; } = new SM_Material
        {
            Poisson = 0.0,
            Youngs = 1.0,
            Thickness = 1.0,
            Density = 1.0,
            FluidC = 1.0,
            ViscoC = 1.0
        };
    }

    public static class SM_Utils
    {
        private static Func<float, int> _int => Mathf.FloorToInt;
        private static Func<float, float, float> _max => Mathf.Max;

        private const int VECDOF = 3;
        private const float FMAX = float.MaxValue;
        private const float FMIN = float.MinValue;
        public static Vector3 VecMax = new Vector3(FMAX, FMAX, FMAX);
        public static Vector3 VecMin = new Vector3(FMIN, FMIN, FMIN);

        public static void FindAABB(Vector3[] vertices, out Vector3 min, out Vector3 max)
        {
            min = VecMax;
            max = VecMin;
            for (int i = 0; i < vertices.Length; i++)
            {
                for (int j = 0; j < VECDOF; j++)
                {
                    if (vertices[i][j] < min[j]) min[j] = vertices[i][j];
                    if (vertices[i][j] > max[j]) max[j] = vertices[i][j];
                }
            }
        }

        public static List<int[]>[,,] BucketTriangles(int[] tri, 
            Vector3[] verts, out float bucketStep, out Vector3 minAABB)
        {
            Vector3 maxAABB;
            List<int[]>[,,] buckets;
            FindAABB(verts, out minAABB, out maxAABB);

            float xwidth = maxAABB.x - minAABB.x;
            float ywidth = maxAABB.y - minAABB.y;
            float zwidth = maxAABB.z - minAABB.z;
            float maxwidth = _max(xwidth, _max(ywidth, zwidth));
            bucketStep = 1.0f / Mathf.Sqrt(maxwidth);

            int sizeX = _int(xwidth * bucketStep) + 1;
            int sizeY = _int(ywidth * bucketStep) + 1;
            int sizeZ = _int(zwidth * bucketStep) + 1;

            buckets = new List<int[]>[sizeX, sizeY, sizeZ];

            int x, y, z;
            for (int i = 0; i < tri.Length; i+=3)
            {
                x = _int((verts[tri[i]].x - minAABB.x) * bucketStep);
                y = _int((verts[tri[i]].y - minAABB.y) * bucketStep);
                z = _int((verts[tri[i]].z - minAABB.z) * bucketStep);

                if (buckets[x, y, z] == null)
                    buckets[x, y, z] = new List<int[]>();

                buckets[x, y, z].Add(new int[] {tri[i], tri[i+1], tri[i+2]});
            }

            return buckets;
        }

        public static void AutoWeld(Mesh mesh, float threshold, float bucketStep)
        {
            Vector3[] oldVertices = mesh.vertices;
            Vector3[] newVertices = new Vector3[oldVertices.Length];
            int[] old2new = new int[oldVertices.Length];
            int newSize = 0;

            // Find AABB
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < oldVertices.Length; i++)
            {
                if (oldVertices[i].x < min.x) min.x = oldVertices[i].x;
                if (oldVertices[i].y < min.y) min.y = oldVertices[i].y;
                if (oldVertices[i].z < min.z) min.z = oldVertices[i].z;
                if (oldVertices[i].x > max.x) max.x = oldVertices[i].x;
                if (oldVertices[i].y > max.y) max.y = oldVertices[i].y;
                if (oldVertices[i].z > max.z) max.z = oldVertices[i].z;
            }

            // Make cubic buckets, each with dimensions "bucketStep"
            int bucketSizeX = Mathf.FloorToInt((max.x - min.x) / bucketStep) + 1;
            int bucketSizeY = Mathf.FloorToInt((max.y - min.y) / bucketStep) + 1;
            int bucketSizeZ = Mathf.FloorToInt((max.z - min.z) / bucketStep) + 1;

            List<int>[,,] buckets = new List<int>[bucketSizeX, bucketSizeY, bucketSizeZ];

            // Make new vertices
            for (int i = 0; i < oldVertices.Length; i++)
            {
                // Determine which bucket it belongs to
                int x = Mathf.FloorToInt((oldVertices[i].x - min.x) / bucketStep);
                int y = Mathf.FloorToInt((oldVertices[i].y - min.y) / bucketStep);
                int z = Mathf.FloorToInt((oldVertices[i].z - min.z) / bucketStep);

                // Check to see if it's already been added
                if (buckets[x, y, z] == null)
                    buckets[x, y, z] = new List<int>(); // Make buckets lazily

                for (int j = 0; j < buckets[x, y, z].Count; j++)
                {
                    Vector3 to = newVertices[buckets[x, y, z][j]] - oldVertices[i];
                    if (Vector3.SqrMagnitude(to) < threshold)
                    {
                        old2new[i] = buckets[x, y, z][j];
                        goto skip; // Skip to next old vertex if this one is already there
                    }
                }

                // Add new vertex
                newVertices[newSize] = oldVertices[i];
                buckets[x, y, z].Add(newSize);
                old2new[i] = newSize;
                newSize++;

            skip:;
            }

            // Make new triangles
            int[] oldTris = mesh.triangles;
            int[] newTris = new int[oldTris.Length];
            for (int i = 0; i < oldTris.Length; i++)
            {
                newTris[i] = old2new[oldTris[i]];
            }

            Vector3[] finalVertices = new Vector3[newSize];
            for (int i = 0; i < newSize; i++)
                finalVertices[i] = newVertices[i];

            mesh.Clear();
            mesh.vertices = finalVertices;
            mesh.triangles = newTris;
            mesh.RecalculateNormals();
            mesh.Optimize();
        }
    }
}


