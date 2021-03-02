using System;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace ExtendedMathUtils
{
    public class SpringMesh
    {
        public List<SM_Edge> edges;
        public SortedDictionary<int, SM_Vertex> lookup;

        private Mesh mesh;
        private SM_Material props;

        public SpringMesh(Mesh mesh, SM_Material props)
        {
            this.mesh = mesh;
            this.props = props;
            edges  = new List<SM_Edge>();
            lookup = new SortedDictionary<int, SM_Vertex>();
            ConvertMesh();
        }

        private void SetTriangles()
        {
            Vector3[] verts = mesh.vertices;
            int[] tri = mesh.triangles;
            for (int i = 0; i < tri.Length; i+=3)
            {
                int p = tri[i];
                int q = tri[i + 1];
                int r = tri[i + 2];
                SM_Triangle T = new SM_Triangle(p, q, r, verts);
                T.SetEdges(edges, lookup);
            }
        }

        private void SetMasses()
        {
            foreach (var vertexEntry in lookup)
            {
                vertexEntry.Value.ComputeMass(props.Density, props.Thickness);
            }
        }

        private void ConvertMesh()
        {
            SM_Triangle.InjectMaterialProperties(props);
            SetTriangles();
            SetMasses();
            SM_Triangle.DejectMaterialProperties();
        }

        public Matrix<double> GetGlobalStiffnessMatrix()
        {
            int of = SM_Vertex.dof;
            int n = mesh.vertexCount * of;
            Matrix<double> K = Matrix<double>.Build.Sparse(n, n, 0);

            for (int i = 0; i < edges.Count; i++)
            {
                SM_Edge e = edges[i];
                double[,] local = e.GetLocalStiffnessMatrix();
                for (int j = 0; j < SM_Vertex.dof; j++)
                {
                    K[of * e.row + j, of * e.row + j] += local[j, j];
                    K[of * e.col + j, of * e.col + j] += local[of + j, of + j];
                    K[of * e.row + j, of * e.col + j] += local[j, of + j];
                    K[of * e.col + j, of * e.row + j] += local[of + j, j];
                }            
            }

            return K;
        }

        public Matrix<double> GetMassMatrix()
        {
            int of = SM_Vertex.dof;
            int n = mesh.vertexCount * of; 
            Matrix<double> M = Matrix<double>.Build.Sparse(n, n, 0);
            foreach (var vertexEntry in lookup)
            {
                SM_Vertex v = vertexEntry.Value;
                for (int i = 0; i < of; i++)
                {
                    M[of * v.idx + i, of * v.idx + i] = v.mass;
                }
            }

            return M;
        }
    }

    public class SM_Triangle
    {
        private static SM_Material _props { get; set; } = SM_Material.Default;
        public static void InjectMaterialProperties(SM_Material properties)
        {
            _props = properties;
        }

        public static void DejectMaterialProperties()
        {
            _props = SM_Material.Default;
        }

        private int p, q, r;
        private Vector3 v0, v1, v2;
        private double la, lb, lc;

        public double kc { get; private set; }
        public double ae { get; private set; }

        public SM_Triangle(int p, int q, int r, Vector3[] points)
        {
            this.p = p; 
            this.q = q; 
            this.r = r;
            v0 = points[p]; 
            v1 = points[q]; 
            v2 = points[r];
            Vector3 pq = v1 - v0; 
            lc = pq.magnitude;
            Vector3 qr = v2 - v1; 
            la = qr.magnitude;
            Vector3 rp = v0 - v2; 
            lb = rp.magnitude;
            ae = TeArea();
            kc = ComputeK();
        }

        private SM_Vertex TryGetVertex(int key, SortedDictionary<int, SM_Vertex> lookup)
        {
            SM_Vertex cur;
            if (!lookup.ContainsKey(key))
            {
                cur = new SM_Vertex(key);
                lookup.Add(key, cur);
            }
            else
            {
                cur = lookup[key];
            }

            return cur;
        }

        public void SetEdges(List<SM_Edge> edges, SortedDictionary<int, SM_Vertex> lookup)
        {
            SM_Vertex pv = TryGetVertex(p, lookup); 
            pv.AddParent(this);
            SM_Vertex qv = TryGetVertex(q, lookup); 
            qv.AddParent(this);
            SM_Vertex rv = TryGetVertex(r, lookup); 
            rv.AddParent(this);
            SM_Edge e0 = new SM_Edge(pv, qv, kc); 
            edges.Add(e0);
            SM_Edge e1 = new SM_Edge(qv, rv, kc); 
            edges.Add(e1);
            SM_Edge e2 = new SM_Edge(rv, pv, kc); 
            edges.Add(e2);
        }

        private double ComputeK()
        {
            double E = _props.Youngs * _props.Thickness;
            double v = _props.Poisson;
            return 1.0;//E;//(E / (1.0 + v)) * ae / (lc * lc) 
                //+ (E * v / (1.0 - v * v)) * (la * la + lb * lb - lc * lc) / 8.0 * ae;
        }

        private double TeArea()
        {
            double f0 =  la + lb + lc;
            double f1 =  la + lb - lc;
            double f2 =  la - lb + lc;
            double f3 = -la + lb + lc;
            return 0.25 * Math.Sqrt(f0*f1*f2*f3);
        }
    }

    public class SM_Vertex
    {
        public static int dof { get; set; } = 3;
        public int idx { get; private set; }
        public double mass { get; private set; }

        private List<SM_Triangle> constituents;

        public SM_Vertex(int idx)
        {
            this.idx = idx;
            constituents = new List<SM_Triangle>();
        }

        public void AddParent(SM_Triangle parent)
        {
            constituents.Add(parent);
        }

        public void ComputeMass(double density, double thickness)
        {
            double p = density;
            foreach (var item in constituents)
            {
                mass += p * thickness * item.ae;
            }
        }

    }

    public class SM_Edge
    {
        private const int LMAT_MULT = 2;

        public double k;
        public int row, col;
        private SM_Vertex v0, v1;

        public SM_Edge(SM_Vertex v0, SM_Vertex v1, double k)
        {
            this.k = k;
            this.v0 = v0; this.v1 = v1;
            row = this.v0.idx;
            col = this.v1.idx;
        }

        public double[,] GetLocalStiffnessMatrix()
        {
            int dof = SM_Vertex.dof * LMAT_MULT;
            double[,] K = new double[dof, dof];
            for (int x = 0; x < SM_Vertex.dof; x++)
            {
                for (int y = 0; y < SM_Vertex.dof; y++)
                {
                    K[x, y] = k;
                    K[x + SM_Vertex.dof, y + SM_Vertex.dof] = k;
                    K[x, y + SM_Vertex.dof] = -k;
                    K[x + SM_Vertex.dof, y] = -k;
                }
            }
            return K;
        }
    }
}