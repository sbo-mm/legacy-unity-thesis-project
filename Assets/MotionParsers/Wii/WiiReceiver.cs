using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    public class WiiReceiver : MonoBehaviour
    {
        public enum WiiReceiverTypes
        {
            None, BalanceBoard, Remote
        }

        public Dictionary<WiiReceiverTypes, WiiMote> AttachedRemotes;

        private void OnEnable()
        {
            AttachedRemotes = new Dictionary<WiiReceiverTypes, WiiMote>
            {
                [WiiReceiverTypes.None] = null,
                [WiiReceiverTypes.BalanceBoard] = null,
                [WiiReceiverTypes.Remote] = null
            };
        }

        public void WiiPrint(object obj)
        {
            Debug.Log(obj.ToString());
        }
    }
}
