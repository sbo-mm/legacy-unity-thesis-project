using System;
using System.Globalization;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Data.Text;
using UnityEngine;

using EVDResult = ExtendedMathUtils.EVD.EVDResult;
using zdouble = System.Numerics.Complex;

namespace ExtendedMathUtils
{
    [RequireComponent(typeof(MeshFilter))]
    public class ModalModel : MonoBehaviour
    {
        private Func<zdouble, zdouble> sqrt 
            => System.Numerics.Complex.Sqrt;

        private Func<zdouble, zdouble> conj
            => System.Numerics.Complex.Conjugate;

        private Func<zdouble, zdouble> pow2 = (p) 
            => { return System.Numerics.Complex.Pow(p, 2.0); };

        [SerializeField]
        private bool Create, Load, Save;

        public SM_Material materialProperties;

        private Matrix<double> _gainMatrix;
        public Matrix<double> GainMatrix
        {
            get { return _gainMatrix; }
        }

        private Matrix<double> _gainMatrixT;
        public Matrix<double> GainMatrixT
        {
            get { return _gainMatrixT; }
        }

        private Matrix<double> _massMatrix;
        public Matrix<double> MassMatrix
        {
            get { return _massMatrix; }
        }

        private Vector<zdouble> _omegaPlus;
        public Vector<zdouble> OmegaPlus
        {
            get { return _omegaPlus; }
        }

        private Vector<zdouble> _omegaMinus;
        public Vector<zdouble> OmegaMinus
        {
            get { return _omegaMinus; }
        }

        private Mesh _mesh;
        public Mesh ModalMesh
        {
            get { return _mesh; }
        }

        public SpringMassSystem3D SpringMassSystem;
        public int ModesUsed   { get; private set; } = 0;

        private const double TWO_PI   = 2.0 * Math.PI;
        private const double MIN_FREQ = 20.0;
        private const double MAX_FREQ = 22000.0;
        private const double
            X0 = 15,    X1 = 2000,  X2 = 8000,
            Y0 = 1,     Y1 = 4,     Y2 = 90; 
            
        private static readonly TwoPieceLinearFunction DLC = 
            new TwoPieceLinearFunction (X0, X1, X2, Y0, Y1, Y2);

        private static readonly MatrixBuilder<double>  MB = Matrix<double>.Build;
        private static readonly VectorBuilder<double> dVB = Vector<double>.Build;
        private static readonly VectorBuilder<zdouble> VB = Vector<zdouble>.Build;

        private void Awake()
        {
            _mesh = GetComponent<MeshFilter>().mesh;
            SpringMassSystem = new SpringMassSystem3D(
                transform, _mesh, materialProperties);

            /*
            string path = "/Users/SophusOlsen/Desktop/stiff_cyl.csv";
            DelimitedWriter.Write(path, SpringMassSystem.StiffnessMatrix, ",", null, null, CultureInfo.InvariantCulture);

            path = "/Users/SophusOlsen/Desktop/mass_cyl.csv";
            DelimitedWriter.Write(path, SpringMassSystem.MassMatrix, ",", null, null, CultureInfo.InvariantCulture);
            */

            //Debug.Log(SpringMassSystem.StiffnessMatrix);

            if (Create)
            {
                CreateModel();
            }
        }

        private zdouble GetOmega(zdouble lambda, double sgn)
        {
            // Declare aux variables for easier readability
            double eta   = materialProperties.ViscoC;
            double gamma = materialProperties.FluidC;

            // Compute omega +/- (dpn. on sgn)
            zdouble omegaSqr = 4.0 * lambda;
            zdouble delta    = gamma * lambda + eta;
            zdouble rho      = sqrt(pow2(delta) - omegaSqr);

            return ((-delta) + sgn * rho) * 0.5;
        }

        private double GetFreqFrom(zdouble omega)
        {
            return omega.Imaginary / TWO_PI;
        }

        private int SumColumnsUntilDlc(int currentIdx, double currentFreq,
            Matrix<double> gains, Vector<zdouble> lambdas, List<Vector<double>> outputVector)
        {
            Vector<double> offsetColumn = gains.Column(currentIdx);
            double delta = DLC.EvaluateAt(currentFreq);

            int j = currentIdx + 1;
            while (j < gains.ColumnCount)
            {
                zdouble probe = GetOmega(lambdas[j], 1);
                double nextFreq = GetFreqFrom(probe);
                double centerFreqDiff = nextFreq - currentFreq;

                if (centerFreqDiff > delta)
                    break;

                offsetColumn += gains.Column(j);
                j++;
            }
            outputVector.Add(offsetColumn);
            return j;
        }

        private void PrecomputeAuralProperties(Matrix<double> K, Matrix<double> M)
        {
            // Call to the numpy server to compute evd
            Numpy.EvdResult evd = Numpy.LinAlg.Eigh(K);
            var lambdas = evd.EigenValues;
            var mgains = evd.EigenVectors;

            // Initialize output structurs
            List<zdouble> wp = new List<zdouble>();
            List<zdouble> wm = new List<zdouble>();
            List<double> mass = new List<double>();
            List<Vector<double>> aggregatedColumns 
                = new List<Vector<double>>();
                
            for (int i = 0; i < lambdas.Count;)
            {
                zdouble probe = GetOmega(lambdas[i], 1);
                double _freq = GetFreqFrom(probe);

                if (_freq < MIN_FREQ || _freq > MAX_FREQ)
                {
                    i++;
                    continue;
                }

                wp.Add(probe);
                wm.Add(GetOmega(lambdas[i], -1));
                mass.Add(M[i, i]);
                i = SumColumnsUntilDlc(i, _freq, mgains, lambdas, aggregatedColumns);
            }

            _omegaPlus  = VB.DenseOfEnumerable(wp);
            _omegaMinus = VB.DenseOfEnumerable(wm);

            _massMatrix = MB.DenseOfDiagonalArray(mass.ToArray());
            _gainMatrix = MB.DenseOfColumns(aggregatedColumns);
            
            // call numpy server to compute pseudo-inverse
            _gainMatrixT = Numpy.LinAlg.Pinv(_gainMatrix);
        }

        private void CreateModel()
        {
            PrecomputeAuralProperties(SpringMassSystem.StiffnessMatrix,
                SpringMassSystem.MassMatrix);
                
            if (_gainMatrix.ColumnCount != _gainMatrixT.RowCount)
                throw new Exception("Pseudoinverse failed");

            ModesUsed = _omegaPlus.Count;
        }

        private void LoadModel()
        {
            // TODO: Load an existing model
        }

        private void SaveModel()
        {
            // TODO: Save current model
        }
    }

    public sealed class TwoPieceLinearFunction
    {
        private double t0, t1, y0, y1;
        private readonly double m0, m1;

        public TwoPieceLinearFunction(double x0, double x1, double x2,
            double y0, double y1, double y2)
        {
            t0 = x0;
            t1 = x1;
            this.y0 = y0;
            this.y1 = y1;
            m0 = (y1 - y0) / (x1 - x0);
            m1 = (y2 - y1) / (x2 - x1);
        }

        public double EvaluateAt(double x)
        {
            return (y0 + m0 * (x - t0)) * (x >= 0 ? 1.0 : 0.0) * (x <= t1 ? 1.0 : 0.0)
                + (y1 + m1 * (x - t1)) * (x > t1 ? 1.0 : 0.0);        
        }
    }
}


