using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class TriTest : AudioHandler
{
    public float dur = 0.5f;
    public float octave = 1;
    Dictionary<KeyCode, NoteGenerator> pianoNotes;
        
    void Start()
    {
        pianoNotes = new Dictionary<KeyCode, NoteGenerator>
        {
            [KeyCode.A] = new NoteGenerator(440.00f * octave, dur),
            [KeyCode.S] = new NoteGenerator(493.88f * octave, dur),
            [KeyCode.D] = new NoteGenerator(523.25f * octave, dur),
            [KeyCode.F] = new NoteGenerator(587.33f * octave, dur),
            [KeyCode.G] = new NoteGenerator(659.26f * octave, dur),
            [KeyCode.H] = new NoteGenerator(698.43f * octave, dur),
            [KeyCode.J] = new NoteGenerator(783.99f * octave, dur),
            [KeyCode.W] = new NoteGenerator(466.16f * octave, dur),
            [KeyCode.E] = new NoteGenerator(554.36f * octave, dur),
            [KeyCode.R] = new NoteGenerator(622.25f * octave, dur),
            [KeyCode.T] = new NoteGenerator(739.98f * octave, dur),
            [KeyCode.Y] = new NoteGenerator(830.60f * octave, dur)
        };

        AudioObjectReady = true;      
    }

    private void Update()
    {
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kcode))
            {
                if (pianoNotes.TryGetValue(kcode, out NoteGenerator note))
                {

                    Task.Run(() => 
                    {
                        foreach (var buffer in note)
                        {
                            Write(buffer);
                        }
                    });


                    //BufferedAudioWrite(note);
                }
            }
        }

    }

    /*
    private float LogisticFunc(float x)
    {
        const float x0 = 0f;
        const float L = 2f;
        const float k = 0.65f;
        return L / (1.0f + Mathf.Exp(-k * (x - x0))) - 1.0f;
    }


    protected override float AudioCompressionFunc(float sample)
    {
        return LogisticFunc(sample);
    }
    */

}

class NoteGenerator : BufferedSampleGenerator
{
    const float A = 2.0f;
    const float TWO_PI = 2f * Mathf.PI;

    int nsamples;

    float freq;
    float phase;
    float k;

    public NoteGenerator(float freq, float dur)
    {
        this.freq = freq;
        nsamples = Mathf.FloorToInt(dur * SampleRate);
        phase = UnityEngine.Random.Range(0, TWO_PI);
        k = -(Mathf.Log(2) / (dur * 0.145f));
    }

    protected override int GetTotalSamples()
    {
        return nsamples;
    }

    protected override void OnBufferRead(float[] buffer, int buffersize, int position)
    {
        for (int i = 0; i < buffersize; i++)
        {
            float t = (position + i) / SampleRate;
            float val = A * Mathf.Sin(TWO_PI * freq * t + phase);
            float dec = Mathf.Exp(k * t);
            float samp = Mathf.Clamp(dec * val, -1.0f, 1.0f);
            buffer[i] = samp;
        }
    }
}
