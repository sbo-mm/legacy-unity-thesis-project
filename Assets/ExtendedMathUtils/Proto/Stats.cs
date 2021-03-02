using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtendedMathUtils
{
    public static class Stats
    {
        private static readonly int RAND_SEED = 404;
        private static readonly int GAUSS_ITER = 100;
        private static readonly System.Random RAND_MAX
            = new System.Random(RAND_SEED);

        public static double RandGaussian()
        {
            double msum = 0;
            for (int i = 0; i < GAUSS_ITER; i++)
                msum += Convert.ToSingle(RAND_MAX.NextDouble());

            return (msum - GAUSS_ITER / 2f) / Math.Sqrt(GAUSS_ITER / 12f);
        }

        public static double[] IdentityRandMultivariteGaussian(int n)
        {
            double[] rnd = new double[n];
            for (int i = 0; i < n; i++)
                rnd[i] = RandGaussian();

            return rnd;
        }

        public static Matrix MultivariateGaussian(Matrix mu, Matrix sig)
        {
            double[] rnd = IdentityRandMultivariteGaussian(mu.Rows);
            Matrix x = new Matrix(mu.Rows, 1);
            for (int i = 0; i < mu.Rows; i++)
                x[i, 0] = rnd[i];

            Matrix[] eig = LinAlg.EigVals(sig);
            eig[0].ApplyOp(Math.Sqrt);

            Matrix yp = eig[0] * eig[1] * x;
            return yp + mu;
        }

    }
    
    public static class LinAlg
    {

        public static double EPS = 1e-30f;

        public static double[] Norm(Matrix A)
        {
            Vector[] cols = A.GetColumns();
            double[] norms = new double[cols.Length];
            for (int i = 0; i < cols.Length; i++)
                norms[i] = Vector.Magnitude(cols[i]);

            return norms;
        }

        public static double FrobeniusNorm(Matrix A)
        {
            double p = 2.0f;
            double norm = 0.0f;
            for (int i = 0; i < A.Rows; i++)
                for (int j = 0; j < A.Cols; j++)
                    norm += Math.Pow(Math.Abs(A[i, j]), p);

            return Math.Pow(norm, 1.0f/p);
        }

        public static double Norm1D(Matrix A)
        {
            double sumSqr = 0.0f;
            for (int i = 0; i < A.Rows; i++)
                sumSqr += A[i, 0] * A[i, 0];

            return Math.Sqrt(sumSqr);
        }

        public static double Norm1D(double a, double p = 2f)
        {
            return Math.Abs(Math.Pow(a, p));
        }

        private static void Givens(double a, double b, out double c, out double s)
        {
            double t, _c, _s;
            if (Math.Abs(b) > Math.Abs(a))
            {
                t = -a / b; 
                _s = 1f / Math.Sqrt(1f + (t * t)); 
                _c = _s * t;
            }
            else
            {
                t = -b / a; 
                _c = 1f / Math.Sqrt(1f + (t * t));
                _s = _c * t;
            }
            c = _c; s = _s;
        }

        public static Matrix[] GivensRotation(Matrix x, Matrix y, Matrix u, Matrix v)
        {
            double a = x[0, 0];
            double b = y[0, 0];

            if (Math.Abs(b) < EPS)
                return null;

            Givens(a, b, out double c, out double s);
            Matrix x0 = Matrix.Copy(x); Matrix u0 = Matrix.Copy(u);

            x = c * x0 - s * y; u = c * u0 - s * v;
            y = s * x0 + c * y; v = s * u0 + c * v;
            return new Matrix[] {x, y, u, v};
        }

        public static void GivensQR(Matrix A, out Matrix Q, out Matrix R)
        {
            int[] sz = A.Size;
            Q = Matrix.IdentityNxN(sz[0]);
            R = Matrix.Copy(A);
            Matrix r0, r1, q0, q1;
            for (int j = 0; j < sz[1]; j++)
            {
                for (int i = j+1; i < sz[0]; i++)
                {
                    r0 = R[j, j + 1, j, R.Cols];
                    r1 = R[i, i + 1, j, R.Cols];
                    q0 = Q[0, Q.Rows, j, j + 1];
                    q1 = Q[0, Q.Rows, i, i + 1];
                    Matrix[] ret = GivensRotation(r0, r1, q0, q1);
                    if (ret != null)
                    {
                        R[j, j + 1, j, R.Cols] = ret[0];
                        R[i, i + 1, j, R.Cols] = ret[1];
                        Q[0, Q.Rows, j, j + 1] = ret[2];
                        Q[0, Q.Rows, i, i + 1] = ret[3];
                    }
                }
            }
        }

        public static void GramSchmidthQR(Matrix A, out Matrix Q, out Matrix R)
        {
            Vector[] cols = A.GetColumns();
            int K = cols.Length;
            Vector[] U = new Vector[K];
            Vector[] UNorm = new Vector[K];
            for (int k = 0; k < K; k++)
            {
                Vector projSum = new Vector(cols[0].Length);
                for (int j = 0; j < k; j++)
                    projSum += Vector.Project(cols[k], U[j]);
                    
                U[k] = cols[k] - projSum;
                double mag = Vector.Magnitude(U[k]);
                if (Math.Abs(mag) < EPS)
                {
                    U[k][k] = 1.0f;
                    UNorm[k] = U[k];
                }
                else
                {
                    UNorm[k] = U[k] / Vector.Magnitude(U[k]);
                }
            }

            Q = Matrix.FromColumnVector(UNorm);
            R = Q.T * A;
        }

        public static Matrix Housv(Matrix x)
        {
            if (!x.Any())
                return x;

            double m = MatrixUtil.Max(MatrixUtil.Abs(x));
            Matrix u = x / m;

            double su = Math.Sign(u[0, 0]);
            u[0, 0] = u[0, 0] + su * Norm1D(u);
            return u / Norm1D(u);
        }

        public static Matrix HessenbergReduction(Matrix A, out Matrix Q)
        {
            int n = A.Rows;
            Q = Matrix.IdentityNxN(n);
            Matrix H = Matrix.Copy(A).TransposeAntidiagInPlace();

            Matrix v, W;
            for (int j = 0; j < n - 2; j++)
            {
                v = Housv(H[j + 1, H.Rows, j, j + 1]);

                W = H[j + 1, H.Rows, 0, H.Cols];
                H[j + 1, H.Rows, 0, H.Cols] = W - 2f * v * (v.T * W);

                W = H[0, H.Rows, j + 1, H.Cols];
                H[0, H.Rows, j + 1, H.Cols] = W - (W * (2f * v)) * v.T;

                W = Q[0, Q.Rows, j + 1, Q.Cols];
                Q[0, Q.Rows, j + 1, Q.Cols] = W - (W * (2f * v)) * v.T;
            }
            Q.TransposeAntidiagInPlace().TransposeInPlace();
            return H.TransposeAntidiagInPlace();
        }

        public static Matrix HessQR(Matrix A, out Matrix V, double tolScale = 1e-8f)
        {
            int k = 0;
            int M = A.Size[0];

            Matrix Q, R;
            List<Matrix> Qis = new List<Matrix>();
            Matrix H = HessenbergReduction(A, out Matrix U);
            V = Matrix.IdentityNxN(M);
            double fro = FrobeniusNorm(H);
            double tol = fro * tolScale;

            for (int i = M; i >= 2; i--)
            {
                int m = i - 1;
                Matrix I = Matrix.IdentityNxN(i);
                Matrix Qcum = Matrix.IdentityNxN(i);
                do
                {
                    Matrix shift = H[m, m] * I;
                    GivensQR(H[0, i, 0, i] - shift, out Q, out R);
                    H[0, i, 0, i] = R * Q + shift;
                    Qcum = Qcum * Q;
                    k++;
                } while (Math.Abs(H[m, m - 1]) > tol);
                Qis.Add(Qcum);
            }

            foreach (Matrix Qi in Qis)
            {
                Matrix QPad = Matrix.IdentityNxN(M);
                int[] sz = Qi.Size;
                QPad[0, sz[0], 0, sz[1]] = Qi;
                V = V * QPad;
            }
            V = U * V;
            return H;
        }

        public static Matrix[] EigVals(Matrix A, double tolScale = 1e-8f)
        {
            Matrix D;
            Matrix E = HessQR(A, out Matrix V, tolScale);
            double[] diag = E.Diag;

            int n = diag.Length;
            Vector[] evecCols = V.GetColumns();
            do
            {
                int newn = 0;
                for (int i = 0; i < n - 1; i++)
                {
                    if (diag[i] > diag[i + 1])
                    {
                        double tmpf = diag[i];
                        diag[i] = diag[i + 1];
                        diag[i + 1] = tmpf;

                        Vector tmpv = evecCols[i];
                        evecCols[i] = evecCols[i + 1];
                        evecCols[i + 1] = tmpv;

                        newn = i + 1;
                    }
                }
                n = newn;
            } while (n > 1);
            D = Matrix.FromDiag(diag);
            V = Matrix.FromColumnVector(evecCols);
            return new Matrix[] { D, V };
        }
    }
}


