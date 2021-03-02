using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtendedMathUtils
{
    public class Kalman
    {
        // Process and measurement covariance matrices (assumed constant)
        private Matrix Q, R;

        // State matrices (assumed constant)
        private Matrix A, A_t, H, H_t, B;
        private Vector u;

        // "a priori" and "a posteriori" state estimate vectors
        private Vector x_apriori, x_aposteriori;

        // "a priori" and "a posteriori" error covariance matrices
        private Matrix P_apriori, P_aposteriori;

        // Variable to store the Kalman Gain
        private Matrix K;

        // Identity matrix same size as P
        private Matrix I;

        public Kalman()
        {

        }

        public Vector Predict()
        {
            x_apriori = A * x_aposteriori + B * u;
            P_apriori = A * P_aposteriori * A_t + Q;
            return x_apriori;
        }

        public Vector Correct(Vector z)
        {
            K = P_apriori * H_t * Matrix.Inverse(H * P_apriori * H_t + R);
            x_aposteriori = x_apriori + K * (z - H * x_apriori);
            P_aposteriori = (I - K * H) * P_apriori;
            return x_aposteriori;
        }

    }
}



