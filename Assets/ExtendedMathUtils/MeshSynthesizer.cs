using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Data.Text;
using UnityEngine;

using zdouble = System.Numerics.Complex;

namespace ExtendedMathUtils
{
    [RequireComponent(typeof(ModalModel), typeof(Rigidbody))]
    public class MeshSynthesizer : MonoAudioHandler
    {
        private Func<zdouble, zdouble> exp
            => System.Numerics.Complex.Exp;

        private Func<zdouble, zdouble> conj
            => System.Numerics.Complex.Conjugate;
            
        [Range(0, 100)]
        public float gain = 1;

        private Matrix<double> massMatrix 
        { 
            get { return model?.MassMatrix; } 
        }

        private Matrix<double> gainMatrixT
        {
            get { return model?.GainMatrixT; } 
        }

        private Vector<zdouble> omegaPlus   
        { 
            get { return model?.OmegaPlus; } 
        }

        private Vector<zdouble> omegaMinus  
        { 
            get { return model?.OmegaMinus; } 
        }

        private Mesh modalMesh 
        { 
            get { return model?.ModalMesh; } 
        }

        private Vector<double> forceVector;
        private Vector<zdouble> modeGainsP;
        private Vector<zdouble> modeGainsM;
        //private Vector<double> modeGains;

        private zdouble[] modeGains;

        private ModalModel model;
        private MeshAABBSearch maab;
        private ContactPoint[] contacts;

        private int[] lastVertices;
        private const int MAXCP = 50;

        private zdouble[] auxAudioBuffer;

        private int audioBufferSize;
        private float sampleRate;

        private double timeOffset = 0;
        private double startTime;
        private double dspStartTime; 

        private bool objReady;

        zdouble prev;

        ImpactResonatorGenerator imp;

        private void Start()
        {
            model = GetComponent<ModalModel>();

            forceVector = Vector<double>.Build
                .Dense(gainMatrixT.ColumnCount);
            modeGainsP = Vector<zdouble>.Build
                .Dense(model.ModesUsed, new zdouble(0, 0));
            modeGainsM = Vector<zdouble>.Build
                .Dense(model.ModesUsed, new zdouble(0, 0));
            //modeGains  = Vector<double>.Build
            //    .Dense(model.ModesUsed);

            modeGains = new zdouble[model.ModesUsed];

            maab  = new MeshAABBSearch(modalMesh);

            contacts = new ContactPoint[MAXCP];
            lastVertices = new int[] { 0 };

            AudioSettings.GetDSPBufferSize(out audioBufferSize, out int dummy);
            sampleRate = AudioSettings.outputSampleRate;

            auxAudioBuffer = new zdouble[audioBufferSize];

            dspStartTime = AudioSettings.dspTime;
            startTime = Time.time;

            imp = new ImpactResonatorGenerator(model);

            // Flag to signal the audiothread that it can 
            // attempt to process samples (derived field)
            AudioObjReady = true;
        }

        private void Update()
        {

        }

        private Vector3 GetQueryPoint(ContactPoint cp)
        {
            Vector3 WorldCoordPointOnBounds
                = cp.thisCollider.ClosestPoint(cp.point);
            return transform.InverseTransformPoint(WorldCoordPointOnBounds);
        }

        private int[] GetImpactVertices(Vector3 query)
        {
            int[] inds = maab.FindTriangle(query);
            if (inds == null)
                return lastVertices;

            lastVertices = inds;
            return inds;
        }

        private void FillForceVector(int vertIdx, float mag)
        {
            int offset = vertIdx * 3;
            for (int i = 0; i < 3; i++)
                forceVector[offset + i] += mag;
        }

        private void UpdateForceVector(Collision collision)
        {
            //Vector3 impulse = collision.impulse / Time.fixedDeltaTime;

            float mag = collision.relativeVelocity.magnitude * collision.relativeVelocity.magnitude;

            int numPoints = collision.GetContacts(contacts);
            for (int i = 0; i < numPoints; i++)
            {
                Vector3 query = GetQueryPoint(contacts[i]);
                int[] affectedVerts = GetImpactVertices(query);
                for (int j = 0; j < affectedVerts.Length; j++)
                    FillForceVector(affectedVerts[j], mag);
            }
        }

        private void UpdateGains(Collision collision, double t0)
        {
            forceVector.Clear();
            UpdateForceVector(collision);
            
            Vector<double> g = gainMatrixT * forceVector;

            for (int i = 0; i < model.ModesUsed; i++)
            {
                double m = massMatrix[i, i];
                zdouble wp = omegaPlus[i];
                zdouble wm = omegaMinus[i];
                //modeGainsP[i] = g[i] / (m * (wp - wm));
                //modeGainsM[i] = g[i] / (m * (wm - wp));
                modeGains[i] = modeGains[i] + (g[i] / (m * (wp - wm) * exp(wp * t0)));
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (model == null)
                return;

            double t0 = 0.0; //Time.time - startTime;
            UpdateGains(collision, t0);

            imp.SetImpact(modeGains, 0.5f, (float)t0);
            BufferedAudioWrite(imp);

            timeOffset = 0;
        }

        private double GetModeSample(int mode, double t)
        {
            zdouble cp = modeGainsP[mode];
            zdouble cm = modeGainsM[mode];
            zdouble wp = omegaPlus[mode];
            zdouble wm = omegaMinus[mode];

            zdouble pos = cp * wp * exp(wp * t);
            zdouble neg = cm * wm * exp(wm * t);
            double samp = (pos + neg).Real;

            return samp;
        }

        private float LogisticFunc(float x)
        {
            const float x0 = 0f;
            const float L = 2f;
            const float k = 0.065f;
            return L / (1.0f + Mathf.Exp(-k * (x - x0))) - 1.0f;
        }

        protected override float AudioCompressionFunc(float sample)
        {
            return LogisticFunc(sample);
        }
    }

    public class ImpactResonatorGenerator : BufferedSampleGenerator
    {
        private static Func<zdouble, zdouble> exp
            => System.Numerics.Complex.Exp;

        private static Func<zdouble, zdouble> conj
            => System.Numerics.Complex.Conjugate;

        private ModalModel model;
        public int MaxNumSamples { get; set; }

        private zdouble[] gains;
        private zdouble[,] prevBuffer;
        private float startTick;

        private double dt;

        public ImpactResonatorGenerator(ModalModel model)
        {
            this.model = model;
            dt = 1.0 / SampleRate;
            prevBuffer = new zdouble[model.ModesUsed, 2];
        }

        public void SetImpact(zdouble[] gains, float duration, float t0)
        {
            this.gains = gains;
            startTick = t0;
            MaxNumSamples = Mathf.FloorToInt(SampleRate * duration);
            for (int n = 0; n < model.ModesUsed; n++)
            {
                prevBuffer[n, 0] = gains[n] * model.OmegaPlus[n]; // * exp(model.OmegaPlus[n] * t0);
                prevBuffer[n, 1] = conj(gains[n]) * model.OmegaMinus[n]; // * exp(model.OmegaMinus[n] * t0);
            }
        }

        protected override int GetTotalSamples()
        {
            return MaxNumSamples;
        }

        protected override void OnBufferRead(float[] buffer, int buffersize, int position)
        {
            for (int i = 0; i < buffersize; i++)
            {
                buffer[i] = 0;
            }

            for (int n = 0; n < model.ModesUsed; n++)
            {
                for (int i = 0; i < buffersize; i++)
                {
                    //float t = startTick + ((position + i) / SampleRate);
                    //float dt = t - startTick;

                    zdouble u = prevBuffer[n, 0] * exp(model.OmegaPlus[n]  * dt);
                    zdouble v = prevBuffer[n, 1] * exp(model.OmegaMinus[n] * dt);

                    buffer[i] += (float)(u + v).Real;

                    prevBuffer[n, 0] = u;
                    prevBuffer[n, 1] = v;
                    //startTick = t;
                }
            }
        }
    }

}