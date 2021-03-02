using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using UnityEngine;

using zdouble = System.Numerics.Complex;

namespace ExtendedMathUtils
{
    public static class EVD
    {
        public static int COMP_EXT_THRESH { get; set; } = 100;

        private static EVDResult GetMathnetEvd(Matrix<double> m)
        {
            Evd<double> evd = m.Evd(Symmetricity.Symmetric);
            return new EVDResult(evd.EigenVectors, evd.EigenValues);
        }

        private async static Task<EVDResult> GetMathnetEvdAsync(Matrix<double> m)
        {
            return await Task.Run<EVDResult>(() =>
            {
                return GetMathnetEvd(m);
            });
        }

        public async static Task<EVDResult> GetEVDAsync(Matrix<double> m)
        {
            if (m.RowCount < COMP_EXT_THRESH)
            {
                return await GetMathnetEvdAsync(m);
            }

            ExternalLinalgHandler.ExternalResult res
                = await ExternalLinalgHandler.Instance.ComputeExternalAsync(m);
            return new EVDResult(res.G, res.D);
        }

        public struct EVDResult
        {
            public Matrix<double> EigenVectors;
            public Vector<zdouble> EigenValues;

            public EVDResult(Matrix<double> w, Vector<zdouble> v)
            {
                EigenVectors = w;
                EigenValues = v;
            }
        }
    }
}
