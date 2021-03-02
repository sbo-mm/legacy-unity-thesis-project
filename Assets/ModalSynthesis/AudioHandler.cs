using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

/*
[RequireComponent(typeof(AudioSource))]
public class AudioHandler : MonoBehaviour
{
    // Const
    private const int DEFAULT_AQS = 128;

    // Protected
    protected bool AudioObjectReady { get; set; }

    protected int AudioQueueBufferSize
    {
        get
        {
            return audioQueueBufferSize;
        }

        set
        {
            audioQueueBufferSize = value;
            aqBytesLength = value * sizeof(float);
        }
    }

    //Private
    private int unityAudioBufferSize;
    private int audioQueueBufferSize;
    private long aqBytesLength;

    private List<float[]> _sampleBufferList;
    private List<ConcurrentQueue<float[]>> _audioThreadSampleQueueList;
    private ConcurrentBag<ConcurrentQueue<float[]>> _sampleBufferBag;
    private ConcurrentBuffer _concurrentBuffer;

    private void Awake()
    {
        unityAudioBufferSize = GetUnityBufferSize();
        AudioQueueBufferSize = unityAudioBufferSize; //DEFAULT_AQS;

        _sampleBufferList = new List<float[]>();
        _audioThreadSampleQueueList = new List<ConcurrentQueue<float[]>>();
        _sampleBufferBag = new ConcurrentBag<ConcurrentQueue<float[]>>();
        _concurrentBuffer = new ConcurrentBuffer(AudioQueueBufferSize);
    }

    private bool TransferBuffers()
    {
        if (_sampleBufferBag.Count > 0)
        {
            for (int i = 0; i < _sampleBufferBag.Count; i++)
            {
                if (_sampleBufferBag.TryTake(out ConcurrentQueue<float[]> q))
                    _audioThreadSampleQueueList.Add(q);
            }
            return true;
        }
        return false;
    }

    private bool DequeueBuffers()
    {
        if (_audioThreadSampleQueueList.Count > 0)
        {
            foreach (var queue in _audioThreadSampleQueueList.ToList())
            {
                if (queue.TryDequeue(out float[] ss))
                {
                    if (ss == null)
                    {
                        _audioThreadSampleQueueList.Remove(queue);
                        continue;
                    }
                    Debug.Log(ss.Length);
                    _sampleBufferList.Add(ss);
                }
            }
            return true;
        }
        return false;
    }

    private void RecycleBuffers()
    {
        foreach (float[] buffer in _sampleBufferList.ToList())
        {
            _concurrentBuffer.AddBuffer(buffer);
            _sampleBufferList.Remove(buffer);
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!AudioObjectReady)
            return;
    }

    protected Task<BufferedSampleGenerator> BufferedAudioWrite(BufferedSampleGenerator bufferIterator)
    {
        ConcurrentQueue<float[]> sampleQueue
            = new ConcurrentQueue<float[]>();
        _sampleBufferBag.Add(sampleQueue);

        return Task.Run(() =>
        {
            float[] dstbuffer;
            foreach (float[] srcbuffer in bufferIterator)
            {
                dstbuffer = _concurrentBuffer.GetBufferBlocking();
                
                unsafe
                {
                    fixed (float* srcarray = &srcbuffer[0])
                    fixed (float* dstarray = &dstbuffer[0])
                    {
                        Buffer.MemoryCopy(srcarray, dstarray, aqBytesLength, aqBytesLength);
                    }
                }

                sampleQueue.Enqueue(dstbuffer);
            }
            sampleQueue.Enqueue(null);
            return bufferIterator;
        });
    }

    private static int GetUnityBufferSize()
    {
        AudioSettings.GetDSPBufferSize(out int b, out int _);
        return b;
    }

    protected virtual float AudioCompressionFunc(float sample) { return sample; }

    private class ConcurrentBuffer
    {
        private const int BUFFERPOOLSIZE = 200;
        private ConcurrentBag<float[]> _bufferPool;
        private readonly int _buffersize;

        public ConcurrentBuffer(int buffersize)
        {
            _buffersize = buffersize;
            _bufferPool = new ConcurrentBag<float[]>();
            for (int i = 0; i < BUFFERPOOLSIZE; i++)
            {
                _bufferPool.Add(new float[_buffersize]);
            }
        }

        public int GetBufferSize()
        {
            return _buffersize;
        }

        public float[] GetBufferBlocking()
        {
            float[] _outbuf;
            while (_bufferPool.IsEmpty) { }
            while (!_bufferPool.TryTake(out _outbuf)) { }
            return _outbuf;
        }

        public void AddBuffer(float[] _inbuf)
        {
            _bufferPool.Add(_inbuf);
        }
    }
}
*/


[RequireComponent(typeof(AudioSource))]
public class AudioHandler : MonoBehaviour
{
    // Const
    private const int DEFAULT_AQS     = 128;

    private const float LF_GROWTHRATE = 0.00015f;
    private const float LF_MIDPOINT   = 0f;
    private const float LF_MAXIMUM    = 2f;
    private const float LF_HALFMAX    = LF_MAXIMUM * 0.5f;

    // Protected
    protected int AudioQueueBufferSize
    {
        get
        {
            return audioQueueBufferSize;
        }

        set
        {
            audioQueueBufferSize = value;
            aqBytesLength = audioQueueBufferSize * 4;
        }
    }

    protected bool AudioObjectReady { get; set; } = false;

    // Private
    private int UnityAudioBufferSize;
    private float[] auxillaryBuffer;
    private ConcurrentQueue<float[]> audioQueue;

    private int audioQueueBufferSize;
    private long aqBytesLength;

    void Awake()
    {
        AudioQueueBufferSize = DEFAULT_AQS;
        AudioSettings.GetDSPBufferSize(out UnityAudioBufferSize, out int _d);
        //AudioQueueBufferSize = UnityAudioBufferSize;

        auxillaryBuffer = new float[AudioQueueBufferSize];
        audioQueue = new ConcurrentQueue<float[]>();
    }

    private float LogisticFunc(float x)
    {
        const float x0 = 0f;
        const float L = 2f;
        const float k = 0.65f;
        return L / (1.0f + Mathf.Exp(-k * (x - x0))) - 1.0f;
    }

    private float CompressLogistic(float sample)
    {
        return LogisticFunc(sample);
        //return (LF_MAXIMUM / (1 + Mathf.Exp(-LF_GROWTHRATE * (sample - LF_MIDPOINT)))) - LF_HALFMAX;
    }

    private void CopyTo(float[] src, float[] dst)
    {
        unsafe
        {
            fixed (float* srcarray = &src[0])
            fixed (float* dstarray = &dst[0])
            {
                Buffer.MemoryCopy(srcarray, dstarray, aqBytesLength, aqBytesLength);
            }
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!AudioObjectReady)
            return;

        if (audioQueue.IsEmpty)
            return;

        for (int n = 0; n < UnityAudioBufferSize; n+=AudioQueueBufferSize)
        {
            if (audioQueue.IsEmpty)
                break;

            if (audioQueue.TryDequeue(out float[] samples))
                CopyTo(samples, auxillaryBuffer);

            for (int j = 0; j < AudioQueueBufferSize; j++)
            {
                float sample = auxillaryBuffer[j];
                sample = CompressLogistic(sample);
                for (int i = 0; i < channels; i++)
                {
                    data[(n + j) * channels + i] += sample;
                }
            }
        }

    }

    protected void Write(float[] samples)
    {
        if (audioQueue != null && samples != null)
        {
            if (samples.Length == AudioQueueBufferSize) 
            {
                float[] aux = new float[samples.Length];
                CopyTo(samples, aux);
                //Debug.Log("Writing samples...");
                audioQueue.Enqueue(aux);
            }
        }
    }
}

