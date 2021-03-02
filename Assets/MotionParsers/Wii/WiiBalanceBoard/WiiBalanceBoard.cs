using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    public sealed class WiiBalanceBoard : MonoBehaviour
    {
        private const float EXP = 3f;
        private const float PI4 = Mathf.PI / 4f;
        private const float wbb_EPS = 1e-30f;

        private float wbb_Fbr, wbb_Fbl, wbb_Ftr, wbb_Ftl;
        private float wbb_vx, wbb_vy;
        private float wbb_sum;

        private void Update()
        {
            transform.rotation = WBBTitlt();
        }

        private float InnerValue(float num)
        {
            return Mathf.Tan(PI4 * Mathf.Pow(num/wbb_sum, EXP));
        }

        private float WBBAlpha()
        {
            float t1 = wbb_Ftr + wbb_Fbr;
            float t2 = wbb_Ftl + wbb_Fbl;
            return InnerValue(t1 - t2);
        }

        private float WBBBeta()
        {
            float t1 = wbb_Fbr + wbb_Fbl;
            float t2 = wbb_Ftr + wbb_Ftl;
            return InnerValue(t1 - t2);
        }

        private Quaternion WBBTitlt()
        {
            float a = WBBAlpha() * Mathf.Rad2Deg;
            float b = WBBBeta()  * Mathf.Rad2Deg;

            if (float.IsNaN(a) || float.IsNaN(b))
            {
                return Quaternion.identity;
            }

            return Quaternion.Euler(b, 0, a);
        }

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
    }
}