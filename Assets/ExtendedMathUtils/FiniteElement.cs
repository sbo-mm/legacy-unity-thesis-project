using System;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace ExtendedMathUtils
{
    public class FiniteElement
    {
        private class Node : IComparable, IComparer<Node>
        {
            public int dof, id;
            public SortedDictionary<Node, List<Element>> connections;

            public Node(int dof, int id)
            {
                this.dof = dof; this.id = id;
                connections = new SortedDictionary<Node, List<Element>>();
            }

            public void ConnectNode(Node connectTo, Element byElement)
            {
                if (!connections.ContainsKey(connectTo))
                    connections[connectTo] = new List<Element> { byElement };

                if (!connections[connectTo].Contains(byElement))
                    connections[connectTo].Add(byElement);
            }

            public int CompareTo(object obj)
            {
                if (obj == null)
                    return 1;

                if (obj is Node otherNode)
                    return this.id.CompareTo(otherNode.id);

                throw new ArgumentException("Object is not a Node");
            }

            public int Compare(Node x, Node y)
            {
                return x.CompareTo(y);
            }
        }

        private class Element
        {
            public int id;
            public List<Node> nodes;

            public Element(int id)
            {
                this.id = id;
                nodes = new List<Node>();
            }

            public void AddNode(params Node[] nodes)
            {
                foreach (Node node in nodes)
                {
                    if (!this.nodes.Contains(node))
                        this.nodes.Add(node);
                }
            }
        }

        private delegate void RunCells(Matrix<double> global);

        private class Cell
        {
            int row, col;
            public List<Element> contents;

            private static Matrix<double> subM 
                = Matrix<double>.Build.Dense(3, 3, 0.5);

            public Cell(int row, int col)
            {
                this.row = row;
                this.col = col;
                contents = new List<Element>();
            }

            public void Add(List<Element> elements)
            {
                foreach (Element elm in elements)
                {
                    if (!contents.Contains(elm))
                        contents.Add(elm);
                }
            }

            public void ComputeCell(Matrix<double> global)
            {
                Matrix<double> zero
                    = Matrix<double>.Build.Dense(3, 3);

                foreach (Element elm in contents)
                    zero = zero + subM;

                if (row == col) {
                    global.SetSubMatrix(3*row, 3, 3*col, 3, zero);
                }
                else
                {
                    global.SetSubMatrix(3*row, 3, 3*col, 3, -zero);
                    global.SetSubMatrix(3*col, 3, 3*row, 3, -zero);
                }

                /*
                global[row, col] = contents.Count;

                if (col != row && row != col)
                    global[col, row] = contents.Count;
                */
            }
        }

        private class CellMatrix
        {
            public RunCells OnComputeCells;

            ~CellMatrix()
            {
                OnComputeCells = null;
            }

            private void Add(int i, int j, List<Element> value)
            {
                Cell c = new Cell(i, j); c.Add(value);
                OnComputeCells += c.ComputeCell;
            }

            public void Assemble(SortedDictionary<int, Node> nodeEntries)
            {
                int nodeIdx;
                int num = nodeEntries.Count;
                for (nodeIdx = 0; nodeIdx < num; nodeIdx++)
                {
                    Node sel = nodeEntries[nodeIdx];
                    foreach (Node connection in sel.connections.Keys)
                    {
                        if (connection.id < nodeIdx)
                            continue;

                        Add(sel.id, connection.id, sel.connections[connection]);
                    }
                }
            }

            public void ComputeCells(Matrix<double> global)
            {
                OnComputeCells?.Invoke(global);
            }
        }

        private const int DOF          = 3;
        private const int VERT_PER_TRI = 3;

        private Mesh mesh;
        private SortedDictionary<int, Node> nodes;

        public FiniteElement(Mesh mesh)
        {
            this.mesh = mesh;
            nodes = new SortedDictionary<int, Node>();
            ConvertMeshToNodes();
        }

        private void CheckNodes(int[] tri, int n, out Element e)
        {
            e = new Element(n / VERT_PER_TRI);
            for (int i = 0; i < VERT_PER_TRI; i++)
            {
                Node tmp;
                int idx = tri[n + i];
                if (!nodes.ContainsKey(idx))
                {
                    tmp = new Node(DOF, idx);
                    tmp.ConnectNode(tmp, e);
                    nodes.Add(idx, tmp);
                }
                else
                {
                    tmp = nodes[idx];
                    tmp.ConnectNode(tmp, e);
                }
            }
        }

        private void ConvertMeshToNodes()
        {
            Element e;
            Node n0, n1, n2;
            int[] tri = mesh.triangles;
            for (int i = 0; i < tri.Length; i += VERT_PER_TRI)
            {
                CheckNodes(tri, i, out e);

                Debug.Log($"Tri Order: {tri[i]}, {tri[i + 1]}, {tri[i + 2]}");

                n0 = nodes[tri[i]];
                n1 = nodes[tri[i + 1]];
                n2 = nodes[tri[i + 2]];
                e.AddNode(n0, n1, n2);

                n0.ConnectNode(n1, e);
                n1.ConnectNode(n0, e);
                n1.ConnectNode(n2, e);
                n2.ConnectNode(n1, e);
                n0.ConnectNode(n2, e);
                n2.ConnectNode(n0, e);
            }
        }

        public Matrix<double> GlobalStiffnessMatrix()
        {
            int nverts = mesh.vertexCount;
            int ndof = DOF * nverts;

            CellMatrix cellMatrix = new CellMatrix();
            Matrix<double> K = Matrix<double>.Build.Sparse(ndof, ndof);
            cellMatrix.Assemble(nodes);
            cellMatrix.ComputeCells(K);

            return K;
        }
    }
}


