using OscJack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MotionParsers
{
    [AddComponentMenu("OSC/WBB Event Receiver")]
    public sealed class WBBEventReceiver : MonoBehaviour
    {
        const int MAX_OSC_PARAMS = 7;

        [System.Serializable]
        class FloatArrayEvent : UnityEvent<float[]> { }

        [SerializeField] int _udpPort = 9000;
        [SerializeField] string _oscAddress = "/wii/1/balance";
        [SerializeField] FloatArrayEvent _event;

        int _currentPort;
        string _currentAddress;

        float[] _floatBuffer;
        Queue<float> _floatQueue;


        float[] DequeueFloatArray()
        {
            lock (_floatQueue)
            {
                for (int i = 0; i < MAX_OSC_PARAMS; i++)
                    _floatBuffer[i] = _floatQueue.Dequeue();
            }

            return _floatBuffer;
        }

        void OnEnable()
        {
            if (string.IsNullOrEmpty(_oscAddress))
            {
                _currentAddress = null;
                return;
            }

            var server = OscMaster.GetSharedServer(_udpPort);
            server.MessageDispatcher.AddCallback(_oscAddress, OnDataReceive);

            _currentPort = _udpPort;
            _currentAddress = _oscAddress;

            if (_floatQueue == null)
                _floatQueue = new Queue<float>(MAX_OSC_PARAMS);

            if (_floatBuffer == null)
                _floatBuffer = new float[MAX_OSC_PARAMS];

        }

        void OnDisable()
        {
            if (string.IsNullOrEmpty(_currentAddress)) return;

            var server = OscMaster.GetSharedServer(_currentPort);
            server.MessageDispatcher.RemoveCallback(_currentAddress, OnDataReceive);

            _currentAddress = null;
        }

        void OnValidate()
        {
            if (Application.isPlaying)
            {
                OnDisable();
                OnEnable();
            }
        }

        void Update()
        {
            while (_floatQueue.Count > 0)
                _event.Invoke(DequeueFloatArray());
        }
        
        void OnDataReceive(string address, OscDataHandle data)
        {
            lock (_floatQueue)
            {
                for (int i = 0; i < MAX_OSC_PARAMS; i++)
                    _floatQueue.Enqueue(data.GetElementAsFloat(i));
            }
        }

    }
}