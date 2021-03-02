using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MonoAudioHandler : MonoBehaviour
{
    private static ConcurrentBuffer _ccbuffer = null; // new ConcurrentBuffer();
    private static long s_bufferbytes = 0; // _ccbuffer.GetBufferSize() * 4;

    private List<float[]> _sampleBufferList;
    private List<ConcurrentQueue<float[]>> _audioThreadSampleQueueList;
    private ConcurrentBag<ConcurrentQueue<float[]>> _sampleBufferBag;

    protected bool AudioObjReady { get; set; }

    protected int Buffersize
    {
        get { return _ccbuffer.GetBufferSize(); }
    }

    private void Awake()
    {
        if (_ccbuffer == null)
        {
            _ccbuffer = new ConcurrentBuffer();
            s_bufferbytes = _ccbuffer.GetBufferSize() * 4;
        }

        _sampleBufferList = new List<float[]>();
        _audioThreadSampleQueueList = new List<ConcurrentQueue<float[]>>();
        _sampleBufferBag = new ConcurrentBag<ConcurrentQueue<float[]>>();
    }

    private void TransferBuffers()
    {
        if (_sampleBufferBag.Count > 0)
        {
            for (int i = 0; i < _sampleBufferBag.Count; i++)
            {
                if (_sampleBufferBag.TryTake(out ConcurrentQueue<float[]> q))
                    _audioThreadSampleQueueList.Add(q);
            }
        }
    }

    private void DequeueBuffers()
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
                    _sampleBufferList.Add(ss);
                }
            }
        }
    }

    private void RecycleBuffers()
    {
        foreach (float[] buffer in _sampleBufferList.ToList())
        {
            _ccbuffer.AddBuffer(buffer);
            _sampleBufferList.Remove(buffer);
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!AudioObjReady)
            return;

        TransferBuffers();
        DequeueBuffers();

        if (_sampleBufferList == null || _sampleBufferList.Count == 0)
            return;
            
        for (int n = 0; n < Buffersize; n++)
        {
            float sample = 0;
            for (int i = 0; i < _sampleBufferList.Count; i++)
            {
                sample += _sampleBufferList[i][n];
            }

            sample = AudioCompressionFunc(sample);
            for (int i = 0; i < channels; i++)
            {
                data[n * channels + i] += sample;
            }
        }

        RecycleBuffers();
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
                dstbuffer = _ccbuffer.GetBufferBlocking();

                unsafe
                {
                    fixed(float *srcarray = &srcbuffer[0])
                    fixed(float *dstarray = &dstbuffer[0])
                    {
                        Buffer.MemoryCopy(srcarray, dstarray, s_bufferbytes, s_bufferbytes);
                    }
                }

                sampleQueue.Enqueue(dstbuffer);
            }
            sampleQueue.Enqueue(null);
            return bufferIterator;
        });
    }

    protected virtual float AudioCompressionFunc(float sample) { return sample; }

    private class ConcurrentBuffer
    {
        private const int BUFFERPOOLSIZE = 200;
        private ConcurrentBag<float[]> _bufferPool;
        private readonly int _buffersize;

        public ConcurrentBuffer()
        {
            AudioSettings.GetDSPBufferSize(out _buffersize, out int _d);
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
            while(_bufferPool.IsEmpty) { }
            while(!_bufferPool.TryTake(out _outbuf)) { }
            return _outbuf;
        }

        public void AddBuffer(float[] _inbuf)
        {
            _bufferPool.Add(_inbuf);
        }
    }
}

public abstract class BufferedSampleGenerator : IEnumerable<float[]>
{
    private bool _release;
    private readonly object _releaseLock = new object();
    private readonly object _deltaTimeObj = new object();

    private readonly int _bufferSize;
    private readonly float[] _internalBuffer;

    protected float SampleRate { get; }
    protected double StartTick { get; set; }

    protected abstract void OnBufferRead(float[] buffer, int buffersize, int position);
    protected abstract int GetTotalSamples();

    protected BufferedSampleGenerator()
    {
        SampleRate = AudioSettings.outputSampleRate;

        //AudioSettings.GetDSPBufferSize(out this._bufferSize, out int _d);
        _bufferSize = 128;
        this._internalBuffer = new float[this._bufferSize];
        this._release = false;
    }

    public void Release()
    {
        lock (_releaseLock)
        {
            _release = true;
        }
    }

    public IEnumerator<float[]> GetEnumerator()
    {
        int _totalSamples = GetTotalSamples();

        if (_totalSamples == 0)
            yield break;

        for (int n = 0; n < _totalSamples; n += _bufferSize)
        {
            /*
            lock (_releaseLock)
            {
                if (_release)
                {
                    _release = false;
                    yield break;
                }
            }
            */

            OnBufferRead(_internalBuffer, _bufferSize, n);
            yield return _internalBuffer;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

