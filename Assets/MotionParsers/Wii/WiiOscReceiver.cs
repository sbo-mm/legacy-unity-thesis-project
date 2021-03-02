using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    public sealed class WiiOscReceiver : WiiReceiver
    {
        #region Wii Balance Board Fields

        private const float wbb_EPS = 1e-30f;

        private float wbb_Fbr, wbb_Fbl, wbb_Ftr, wbb_Ftl;
        private float wbb_vx, wbb_vy;
        private float wbb_sum;

        #endregion

        #region internal unity
        #endregion

        #region Wii Balance Board methods

        public void WBBReceive(float[] data)
        {
            if (Mathf.Abs(data[0] - wbb_Fbl) > wbb_EPS)
                wbb_Fbl = data[0];

            if (Mathf.Abs(data[1] - wbb_Fbr) > wbb_EPS)
                wbb_Fbr = data[1];

            if (Mathf.Abs(data[2] - wbb_Ftl) > wbb_EPS)
                wbb_Ftl = data[2];

            if (Mathf.Abs(data[3] - wbb_Ftr) > wbb_EPS)
                wbb_Ftr = data[3];

            if (Mathf.Abs(data[4] - wbb_sum) > wbb_EPS)
                wbb_sum = data[4];

            if (Mathf.Abs(data[5] - wbb_vx) > wbb_EPS)
                wbb_vx = data[5];

            if (Mathf.Abs(data[6] - wbb_vy) > wbb_EPS)
                wbb_vy = data[6];
        }

        #endregion
    }

}