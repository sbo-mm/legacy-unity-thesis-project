using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtendedMathUtils
{
    public class Matrix
    {
        public int Rows { get; private set; }
        public int Cols { get; private set; }

        private double[,] matrix;
        public double this[int r, int c]
        {
            get => matrix[r, c];
            set => matrix[r, c] = value;
        }

        public Matrix this[int a, int b, int c, int d]
        {
            get
            {
                Matrix view = new Matrix(b - a, d - c);
                for (int i = a; i < b; i++)
                    for (int j = c; j < d; j++)
                        view[i - a, j - c] = this[i, j];

                return view;
            }

            set
            {
                for (int i = a; i < b; i++)
                    for (int j = c; j < d; j++)
                        this[i, j] = value[i - a, j - c];
            }
        }

        public Matrix T
        {
            get { return Transpose(this); }
        }

        public double[] Diag
        {
            get
            {
                double[] diag = new double[Rows];
                for (int i = 0; i < Rows; i++)
                    diag[i] = this[i, i];

                return diag;
            }
        }

        private readonly int[] sz = new int[2];
        public int[] Size
        {
            get
            {
                if (sz[0] != Rows)
                    sz[0] = Rows;

                if (sz[1] != Cols)
                    sz[1] = Cols;

                return sz;
            }
        }

        public Matrix (int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            matrix = new double[rows, cols];
        }

        public void SetColumns(Vector[] cols)
        {
            if ((Rows == Cols) && (Rows == 0))
            {
                Rows = cols[0].Length;
                Cols = cols.Length;
                matrix = new double[Rows, Cols];
            }

            for (int i = 0; i < Cols; i++)
            {
                for (int j = 0; j < Rows; j++)
                {
                    this[j, i] = cols[i][j];
                }
            }
        }

        public Vector[] GetColumns()
        {
            Vector[] cols = new Vector[Cols];
            for (int i = 0; i < Cols; i++)
            {
                double[] values = new double[Rows];
                for (int j = 0; j < Rows; j++)
                {
                    values[j] = this[j, i];
                }
                cols[i] = new Vector(values);
            }
            return cols;
        }

        public Vector SubVector(int r1, int r2, int col)
        {
            return SubMatrix(r1, r2, col, col).GetColumns()[0];
        }


        public Matrix SubMatrix(int f1, int f2, int t1, int t2)
        {
            int subRows = f2 - f1 + 1;
            int subCols = t2 - t1 + 1;
            Matrix ret = new Matrix(subRows, subCols);
            for (int i = f1; i <= f2; i++)
                for (int j = t1; j <= t2; j++)
                    ret[i - f1, j - t1] = this[i, j];

            return ret;
        }

        private Matrix TransposeInPlaceNxN()
        {
            int i, j;
            double tmp;
            for (i = 1; i < Rows; i++)
            {
                for (j = 0; j < i; j++)
                {
                    tmp = this[i, j];
                    this[i, j] = this[j, i];
                    this[j, i] = tmp;
                }
            }
            return this;
        }

        public Matrix TransposeInPlace()
        {
            if (Rows == Cols)
                return TransposeInPlaceNxN();

            double[,] matrixT = new double[Cols, Rows];
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    matrixT[j, i] = this[i, j];
                }
            }
            int tmp = Cols;
            Cols = Rows;
            Rows = tmp;
            matrix = matrixT;
            return this;
        }

        public Matrix TransposeAntidiagInPlace()
        {
            double tmp;
            int n = Rows;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - 1 - i; j++)
                {
                    tmp = this[i, j];
                    this[i, j] = this[n - 1 - j, n - 1 - i];
                    this[n - 1 - j, n - 1 - i] = tmp;
                }
            }
            return this;
        }

        public Matrix Subtract(Matrix rhs)
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    this[i, j] = this[i, j] - rhs[i, j];

            return this;
        }

        public Matrix Subtract(double rhs)
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    this[i, j] = this[i, j] - rhs;

            return this;
        }

        public Matrix Add(Matrix rhs)
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    this[i, j] = this[i, j] + rhs[i, j];

            return this;
        }

        public Matrix Add(double rhs)
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    this[i, j] = this[i, j] + rhs;

            return this;
        }

        public Matrix ElementMul(double rhs)
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    this[i, j] = this[i, j] * rhs;

            return this;
        }

        public Matrix ElementDiv(double rhs)
        {
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    this[i, j] = this[i, j] / rhs;

            return this;
        }

        /**********************************************************/
        /*    STATIC METHODS AND OPERATORS (and some overrides)   */
        /**********************************************************/

        public static Matrix RandomNxN(int n)
        {
            Matrix rnd = new Matrix(n, n);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    rnd[i, j] = Stats.RandGaussian();

            return rnd;
        }

        public static Matrix FromDiag(params double[] d)
        {
            Matrix ret = new Matrix(d.Length, d.Length);
            for (int i = 0; i < d.Length; i++)
                ret[i, i] = d[i];

            return ret;
        }

        public static Matrix FromDiag(Matrix d)
        {
            int M = d.Size[0];
            Matrix ret = new Matrix(M, M);
            for (int i = 0; i < M; i++)
                ret[i, i] = d[i, i];

            return ret;
        }

        public static Matrix FromColumnVector(params Vector[] cols)
        {
            int r = cols[0].Length;
            int c = cols.Length;
            Matrix ret = new Matrix(r, c);
            ret.SetColumns(cols);
            return ret;
        }

        public static Matrix Transpose(Matrix m)
        {
            Matrix ret = new Matrix(m.Cols, m.Rows);
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Cols; j++)
                {
                    ret[j, i] = m[i, j];
                }
            }

            return ret;
        }

        private static double Det2x2(Matrix m)
        {
            double a, b, c, d;
            a = m[0, 0]; b = m[0, 1];
            c = m[1, 0]; d = m[1, 1];
            return a * d - c * b;
        }

        public static double Determinant(Matrix m)
        {
            if ((m.Rows != m.Cols) || (m.Rows < 1))
            {
                // TODO: throw error
                return default;
            }

            if (m.Rows == 1)
                return m[0, 0];

            if (m.Rows == 2)
                return Det2x2(m);

            Matrix ret;
            int N = m.Rows;
            int i, j, j1, j2;
            double det = 0;

            for (j1 = 0; j1 < N; j1++)
            {
                ret = new Matrix(N - 1, N - 1);
                for(i = 1; i < N; i++)
                {
                    j2 = 0;
                    for (j = 0; j < N; j++)
                    {
                        if (j == j1)
                            continue;
                        ret[i - 1, j2] = m[i, j];
                        j2++;
                    }
                }
                double sign = Math.Pow(-1.0, j1 + 2.0);
                det += sign * m[0, j1] * Determinant(ret);
            }
            return det;
        }

        private static void Adjunct(Matrix a, Matrix c, int i, int j)
        {
            int N = a.Rows;
            int ii, jj, i1, j1;

            i1 = 0;
            for (ii = 0; ii < N; ii++)
            {
                if (ii == i)
                    continue;
                j1 = 0;
                for (jj = 0; jj < N; jj++)
                {
                    if (jj == j)
                        continue;
                    c[i1, j1] = a[ii, jj];
                    j1++;
                }
                i1++;
            }
        }

        public static Matrix CoFactor(Matrix m)
        {
            if ((m.Rows != m.Cols) || (m.Rows < 1))
            {
                // TODO: throw error
                return default;
            }

            int i, j;
            int N = m.Rows;
            double det = 0.0f;

            Matrix ret = new Matrix(N, N);
            Matrix c = new Matrix(N - 1, N - 1);
            for (j = 0; j < N; j++)
            {
                for (i = 0; i < N; i++)
                {
                    Adjunct(m, c, i, j);
                    det = Determinant(c);
                    double sign = Mathf.Pow(-1.0f, i + j + 2.0f);
                    ret[i, j] = sign * det;
                }
            }

            return ret;
        }

        public static Matrix Adjugate(Matrix m)
        {
            return CoFactor(m).TransposeInPlace();
        }

        public static Matrix Inverse(Matrix m)
        {
            double invDet = 1.0f / Determinant(m);
            return Adjugate(m) * invDet;
        }

        public static Matrix IdentityNxN(int N)
        {
            Matrix id = new Matrix(N, N);
            for (int i = 0; i < N; i++)
            {
                id[i, i] = 1.0f;
            }
            return id;
        }

        public static Matrix operator -(Matrix lhs, Matrix rhs)
        {
            Matrix ret = new Matrix(lhs.Rows, lhs.Cols);
            for (int i = 0; i < lhs.Rows; i++)
                for (int j = 0; j < lhs.Cols; j++)
                    ret[i, j] = lhs[i, j] - rhs[i, j];

            return ret;
        }

        public static Matrix operator -(Matrix lhs, double rhs)
        {
            Matrix ret = new Matrix(lhs.Rows, lhs.Cols);
            for (int i = 0; i < lhs.Rows; i++)
                for (int j = 0; j < lhs.Cols; j++)
                    ret[i, j] = lhs[i, j] - rhs;
            
            return ret;
        }

        public static Matrix operator +(Matrix lhs, Matrix rhs)
        {
            Matrix ret = new Matrix(lhs.Rows, lhs.Cols);
            for (int i = 0; i < lhs.Rows; i++)
                for (int j = 0; j < lhs.Cols; j++)
                    ret[i, j] = lhs[i, j] + rhs[i, j];

            return ret;
        }

        public static Matrix operator +(Matrix lhs, double rhs)
        {
            Matrix ret = new Matrix(lhs.Rows, lhs.Cols);
            for (int i = 0; i < lhs.Rows; i++)
                for (int j = 0; j < lhs.Cols; j++)
                    ret[i, j] = lhs[i, j] + rhs;

            return ret;
        }

        public static Matrix operator *(Matrix lhs, Matrix rhs)
        {
            int M = lhs.Rows;
            int K = rhs.Cols;
            int N = rhs.Rows;
            Matrix res = new Matrix(M, K);
            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < K; j++)
                {
                    for (int s = 0; s < N; s++)
                    {
                        double a = lhs[i, s];
                        double b = rhs[s, j];
                        double c = res[i, j];
                        res[i, j] = c + (a * b);
                    }
                }
            }

            return res;
        }

        public static Vector operator *(Matrix lhs, Vector rhs)
        {
            if (lhs.Cols != rhs.Rows)
                rhs.TransposeInPlace();

            int M = lhs.Rows;
            int N = rhs.Rows;
            Vector res = new Vector(M);
            for (int i = 0; i < M; i++)
            {
                for (int s = 0; s < N; s++)
                {
                    double a = lhs[i, s];
                    double b = rhs[s];
                    double c = res[i];
                    res[i] = c + (a * b);
                }
            }

            return res;
        }

        public static Matrix operator *(Matrix lhs, double rhs)
        {
            Matrix ret = new Matrix(lhs.Rows, lhs.Cols);
            for (int i = 0; i < lhs.Rows; i++)
                for (int j = 0; j < lhs.Cols; j++)
                    ret[i, j] = lhs[i, j] * rhs;

            return ret;
        }

        public static Matrix operator *(double lhs, Matrix rhs)
        {
            Matrix ret = new Matrix(rhs.Rows, rhs.Cols);
            for (int i = 0; i < rhs.Rows; i++)
                for (int j = 0; j < rhs.Cols; j++)
                    ret[i, j] = lhs * rhs[i, j];

            return ret;
        }

        public static Matrix operator /(Matrix lhs, double rhs)
        {
            Matrix ret = new Matrix(lhs.Rows, lhs.Cols);
            for (int i = 0; i < lhs.Rows; i++)
                for (int j = 0; j < lhs.Cols; j++)
                    ret[i, j] = lhs[i, j] / rhs;

            return ret;
        }

        public static Matrix Copy(Matrix m)
        {
            Matrix ret = new Matrix(m.Rows, m.Cols);
            for (int i = 0; i < m.Rows; i++)
                for (int j = 0; j < m.Cols; j++)
                    ret[i, j] = m[i, j];

            return ret;
        }

        public override string ToString()
        {
            const int PRINT_SIZE = 30;
            const string decformat = "{0:0.####}";

            bool RowExt = Rows > PRINT_SIZE; 
            int MaxRows = RowExt ? PRINT_SIZE : Rows;

            bool ColExt = Cols > PRINT_SIZE; 
            int MaxCols = ColExt ? PRINT_SIZE : Cols;

            string s = "";
            for (int i = 0; i < MaxRows; i++)
            {
                for (int j = 0; j < MaxCols; j++)
                {
                    double num = this[i, j];
                    s += " " + string.Format(decformat, num); 
                }

                if (RowExt)
                    s += " ...";

                s += "\n";
            }

            if (ColExt)
            {
                s += ".\n.\n.\n";
            }

            return s;
        }



        public string GetDimsFormat()
        {
            return string.Format("[{0}, {1}]", Size[0], Size[1]);   
        }
    }

    public static class MatrixUtil
    {
        public static Matrix ApplyOp(this Matrix m, Func<double, double> op)
        {
            for (int i = 0; i < m.Rows; i++)
                for (int j = 0; j < m.Cols; j++)
                    m[i, j] = op.Invoke(m[i, j]);

            return m;
        }

        public static bool Any(this Matrix m)
        {
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Cols; j++)
                {
                    if (Math.Abs(m[i, j]) > 0)
                        return true;
                }
            }

            return false;
        }

        public static Matrix Abs(Matrix m)
        {
            Matrix ret = new Matrix(m.Rows, m.Cols);
            for (int i = 0; i < m.Rows; i++)
                for (int j = 0; j < m.Cols; j++)
                    ret[i, j] = Math.Abs(m[i, j]);

            return ret;
        }

        public static double Max(Matrix m)
        {
            double max = double.MinValue;
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Cols; j++)
                {
                    if (m[i, j] > max)
                        max = m[i, j];
                }
            }
            return max;
        }
    }

}


