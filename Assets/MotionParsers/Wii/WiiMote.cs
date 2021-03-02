using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    [RequireComponent(typeof(WiiReceiver))]
    public class WiiMote : MonoBehaviour
    {
        protected WiiReceiver recv;
        protected WiiReceiver.WiiReceiverTypes recvType 
            = WiiReceiver.WiiReceiverTypes.None;

        protected void Init(WiiReceiver.WiiReceiverTypes recvType)
        {
            recv = GetComponent<WiiReceiver>();
            this.recvType = recvType;
            recv.AttachedRemotes[recvType] = this;
        }

    }
}