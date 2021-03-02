using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtendedMathUtils
{
    public class Vector
    {
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        private double[,] vector;
        public double this[int i]
        {
            get => vector[Rows == 1 ? 0 : i, Cols == 1 ? 0 : i];
            set => vector[Rows == 1 ? 0 : i, Cols == 1 ? 0 : i] = value;
        }

        public double this[int i, int j]
        {
            get => vector[i, j];
            set => vector[i, j] = value; 
        }

        public int Length
        {
            get { return Rows + Cols - 1; }
        }

        public Vector() { }

        public Vector(int size)
        {
            Rows = size;
            Cols = 1;
            vector = new double[Rows, Cols];
        }

        public Vector(params double[] values) : this(values.Length)
        {
            for (int i = 0; i < values.Length; i++)
                this[i] = values[i];
        }

        public Vector Transpose()
        {
            Vector ret = new Vector()
            {
                Cols = Rows,
                Rows = Cols
            };
            ret.vector = new double[ret.Rows, ret.Cols];

            for (int i = 0; i < Length; i++)
            {
                ret[i] = this[i];
            }

            return ret;
        }

        public Vector TransposeInPlace()
        {
            int tmp = Cols;
            Cols = Rows;
            Rows = tmp;

            double[,] trans = new double[Rows, Cols];
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    trans[i, j] = vector[j, i];
                }
            }
            vector = trans;
            return this;
        }

        public static double Magnitude(Vector v)
        {
            double mag = 0;
            for (int i = 0; i < v.Length; i++)
                mag += v[i] * v[i];

            return System.Math.Sqrt(mag);
        }

        public static double Dot(Vector lhs, Vector rhs)
        {
            double res = 0.0f;
            for (int i = 0; i < lhs.Length; i++)
            {
                double a = lhs[i];
                double b = rhs[i];
                res += a * b;
            }
            return res;
        }

        public static Vector Project(Vector from, Vector to)
        {
            double scalarProjection = from * to / Magnitude(to);
            Vector toNorm = to / Magnitude(to);
            return toNorm * scalarProjection;
        }

        public static Matrix VecMulVecTranspose(Vector v1, Vector v1T)
        {
            Vector v2 = v1T.Transpose();
            int M = v1.Rows;
            int K = v2.Cols;
            int N = v2.Rows;
            Matrix res = new Matrix(M, K);
            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < K; j++)
                {
                    for (int s = 0; s < N; s++)
                    {
                        double a = v1[i, s];
                        double b = v2[s, j];
                        double c = res[i, j];
                        res[i, j] = c + (a * b);
                    }
                }
            }

            return res;
        }

        public static double operator *(Vector lhs, Vector rhs)
        {
            return Dot(lhs, rhs);
        }

        public static Vector operator *(Vector lhs, double rhs)
        {
            Vector res = new Vector(lhs.Length);
            for (int i = 0; i < lhs.Length; i++)
                res[i] = lhs[i] * rhs;

            return res;
        }

        public static Vector operator *(double lhs, Vector rhs)
        {
            Vector res = new Vector(rhs.Length);
            for (int i = 0; i < rhs.Length; i++)
                res[i] = lhs * rhs[i];

            return res;
        }

        public static Vector operator /(Vector lhs, double rhs)
        {
            Vector res = new Vector(lhs.Length);
            for (int i = 0; i < lhs.Length; i++)
                res[i] = lhs[i] / rhs;

            return res;
        }

        public static Vector operator +(Vector lhs, Vector rhs)
        {
            Vector res = new Vector(lhs.Length);
            for (int i = 0; i < lhs.Length; i++)
                res[i] = lhs[i] + rhs[i];

            return res;
        }

        public static Vector operator -(Vector lhs, Vector rhs)
        {
            Vector res = new Vector(lhs.Length);
            for (int i = 0; i < lhs.Length; i++)
                res[i] = lhs[i] - rhs[i];

            return res;
        }

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < Length; i++)
                s += this[i] + " ";
            return s;
        }

    }

}


