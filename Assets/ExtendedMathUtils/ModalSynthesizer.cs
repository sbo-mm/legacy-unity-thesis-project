using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

using zdouble = System.Numerics.Complex;

namespace ExtendedMathUtils
{
    public class ModalSynthesizer
    {
        public static MonoBehaviour ParentBehaviour { get; set; }

        private int sampleRate;
        public int SampleRate
        {
            get { return sampleRate; }
        }

        private Matrix<double> mStiffness;
        public Matrix<double> StiffnessMatrix
        {
            get { return mStiffness; }
        }

        private Matrix<double> mMass;
        public Matrix<double> MassMatrix
        {
            get { return mMass; }
        }

        private Matrix<double> mGain;
        public Matrix<double> GainMatrix
        {
            get { return mGain; }
        }

        private Matrix<double> mGain_inv;
        public Matrix<double> GainMatrix_inv
        {
            get { return mGain_inv; }
        }

        private Vector<zdouble> mEig;
        public Vector<zdouble> EigenValues
        {
            get { return mEig; }
        }


        private int nmodes;
        private zdouble[] omega_plus;
        private zdouble[] omega_minus;
        private zdouble[] cGains;

        private SM_Material props;

        private ExternalLinalgHandler ext;
        private const int USE_EXT_TOL = 200;

        public ModalSynthesizer(Mesh mesh, SM_Material properties, int sampleRate = 44100)
        {
            this.sampleRate = sampleRate;
            SpringMesh fem = new SpringMesh(mesh, properties);
            mMass = fem.GetMassMatrix();
            mStiffness = fem.GetGlobalStiffnessMatrix();

            if (mStiffness.RowCount > USE_EXT_TOL)
            {
                /*
                ext = ExternalLinalgHandler.Instance;
                ParentBehaviour?.StartCoroutine(ext.UnityDebugProcessOutput());
                ext.ComputeExternal(mStiffness);
                mGain = ext.EigenVectors;
                mGain_inv = ext.EigenVectors_Inv;
                mEig = ext.EigenValues;
                */
                              
                /*
                using (ExternalLinalgHandler ext = new ExternalLinalgHandler())
                {
                    ParentBehaviour?.StartCoroutine(ext.UnityDebugProcessOutput());
                    ext.ComputeExternal(K);
                    mGain = ext.EigenVectors;
                    mGain_inv = ext.EigenVectors_Inv;
                    mEig = ext.EigenValues;
                }
                */
                //ext.Dispose();
            }
            else
            {
                Evd<double> evd = mStiffness.Evd(Symmetricity.Hermitian);
                mGain = evd.EigenVectors;
                mGain_inv = mGain.Inverse();
                mEig = evd.EigenValues;
            }

            props = properties;
            nmodes = mEig.Count;
            omega_plus  = new zdouble[nmodes];
            omega_minus = new zdouble[nmodes];
            SetModeProperties();

            cGains = new zdouble[nmodes];
            for (int i = 0; i < nmodes; i++)
                cGains[i] = new zdouble(0.0, 0.0);
        }

        private zdouble conj(zdouble z)
        {
            return System.Numerics.Complex.Conjugate(z);
        }

        private zdouble sqrt(zdouble z)
        {
            return System.Numerics.Complex.Sqrt(z);
        }

        private zdouble pow2(zdouble z)
        {
            return System.Numerics.Complex.Pow(z, 2.0);
        }

        private zdouble exp(zdouble z)
        {
            return System.Numerics.Complex.Exp(z);
        }

        private zdouble ComputeMode(double sgn, double g, double e, zdouble l)
        {
            return (-(g * l + e) + sgn * sqrt(pow2(g * l + e) - 4.0 * l)) / 2.0;
        }

        private void SetModeProperties()
        {
            double gamma = props.FluidC;
            double eta = props.ViscoC;
            for (int i = 0; i < nmodes; i++)
            {
                zdouble eig = mEig[i];
                omega_plus[i]  = ComputeMode(1.0, gamma, eta, eig);
                omega_minus[i] = ComputeMode(-1.0, gamma, eta, eig);
            }
        }

        private zdouble SampleMode(int i, double t)
        {
            zdouble t1 = cGains[i] * exp(omega_plus[i] * t);
            zdouble t2 = conj(cGains[i]) * exp(omega_minus[i] * t);
            return t1 + t2; 
        }

        public void UpdateOnCollision(Vector<double> f, double t0)
        {
            Vector<double> g = mGain.Inverse() * f;
            for (int i = 0; i < nmodes; i++)
            {
                zdouble diff = omega_plus[i] - omega_minus[i];
                zdouble den = mMass[i, i] * diff * exp(omega_plus[i] * t0);
                cGains[i] = cGains[i] + g[i] / den;
            }
        }

    }
}

