using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MotionParsers {

    public class AMCReader : BaseReader {

        private const string METATOKEN = ":DEGREES";

        private readonly long initStreamPosition;
        private StreamReader amcReader;
        public Dictionary<string, float[]> templateValueDict;

        public bool IsAMCStreamOpen { get; private set; }
        public int ValuesPerFrame   { get; private set; }

        private bool loopEnded;

        public AMCReader(string amc_path) 
        {
            initStreamPosition = 0;
            templateValueDict = new Dictionary<string, float[]>(); 
            using (StreamReader reader = File.OpenText(amc_path))
            {
                if (SkipUntilOccurenceOf(METATOKEN, reader) >= 0) {
                    initStreamPosition = reader.BaseStream.Position;
                    InitAMCReader(reader);
                }
            }

            // TODO: Appropriate error handling

            amcReader = File.OpenText(amc_path);
            amcReader.BaseStream.Position = initStreamPosition;
            amcReader.DiscardBufferedData();
            IsAMCStreamOpen = true;
        }

        ~AMCReader()
        {
            if (IsAMCStreamOpen)
            {
                amcReader.Close();
            }
        }

        private void InitAMCReader(StreamReader reader)
        {
            int nvalues = 0;
            string[] tokens = GetLineTokens(reader);
            while (!(tokens = GetLineTokens(reader))[0].IsNumeric())
            {
                float[] valueBuffer = new float[tokens.Length - 1];
                templateValueDict.Add(tokens[0], valueBuffer);
                nvalues++;
            }

            ValuesPerFrame = nvalues;
        }

        public void CloseReader()
        {
            if (IsAMCStreamOpen)
            {
                amcReader.Close();
                IsAMCStreamOpen = false;
                amcReader = null;
            }
        }

        public void CheckForLoopEnd()
        {
            if (loopEnded)
            {
                amcReader.BaseStream.Position = initStreamPosition;
                amcReader.DiscardBufferedData();
                loopEnded = false;
            }
        }

        public void ReadAMCFrame(AMCFrame frame)
        {
            if (!IsAMCStreamOpen || loopEnded)
                return;

            int valuesRead = 0;
            string[] tokens = GetLineTokens(amcReader);
            for (int i = 0; i < ValuesPerFrame; i++)
            {
                if (tokens == null)
                    break;

                tokens = GetLineTokens(amcReader);
                for (int j = 1; j < tokens.Length; j++)
                {
                    float val = float.Parse(tokens[j], CultureInfo.InvariantCulture);
                    frame.SetValue(tokens[0], j - 1, val);
                }

                valuesRead++;
            }

            loopEnded = valuesRead != ValuesPerFrame;
        }
    }

    public class FrameData : IOperable<FrameData>
    {
        public float[] Data { get; private set; }

        public FrameData(int sz)
        {
            Data = new float[sz];
        }

        public void Set(float[] vals)
        {
            for (int i = 0; i < vals.Length; i++)
                Data[i] = vals[i];
        }

        public void SetValue(int idx, float value)
        {
            Data[idx] = value;
        }

        public FrameData Add(FrameData other)
        {
            for (int i = 0; i < Data.Length; i++)
                Data[i] += other.Data[i];
            return this;
        }

        public FrameData Div(float divident)
        {
            for (int i = 0; i < Data.Length; i++)
                Data[i] /= divident;
            return this;
        }
    }

    public class AMCFrame
    {
        private static readonly int NULLSZ = 0;
        private static readonly FrameData emptyData = new FrameData(NULLSZ);

        private Dictionary<string, FrameData> frameData;

        public AMCFrame(Dictionary<string, float[]> template)
        {
            frameData = new Dictionary<string, FrameData>();
            foreach (var item in template)
            {
                FrameData data = new FrameData(item.Value.Length);
                frameData.Add(item.Key, data);
            }
        }

        public void SetValue(string key, int idx, float value)
        {
            frameData[key].SetValue(idx, value);
        }

        public FrameData GetValue(string key)
        {
            if (frameData.TryGetValue(key, out FrameData data))
                return data;

            return emptyData;
        }

        public Dictionary<string, FrameData> Get()
        {
            return frameData;
        }

    }

    public class AMCFrameBuffer
    {
        private const int filterOrder = 15;

        private readonly long bufferSize;
        private readonly AMCReader amcReader;
        private LinkedList<AMCFrame> frameBuffer;

        public AMCFrame SmoothedFrame
        {
            get { return Smooth(filterOrder); }
        }

        public AMCFrame CurrentFrame
        {
            get { return frameBuffer.Last.Value; }
        }

        public AMCFrameBuffer(long size, AMCReader amcReader)
        {
            bufferSize = size;
            this.amcReader = amcReader;
            frameBuffer = new LinkedList<AMCFrame>();

            var template = amcReader.templateValueDict;
            for (int i = 0; i < bufferSize; i++)
            {
                AMCFrame frame = new AMCFrame(template);
                frameBuffer.AddLast(frame);
            }
        }

        private AMCFrame Smooth(int order)
        {
            AMCFrame res = new AMCFrame(amcReader.templateValueDict);
            var frameData = res.Get();
            var node = frameBuffer.Last;
            for (int i = 0; i < order; i++)
            {
                AMCFrame cur = node.Value;
                var newData = cur.Get();
                frameData.Add(newData);
                node = node.Previous;
            }
            frameData.Div(order);
            return res;
        }

        private void ShiftRegister()
        {
            var node = frameBuffer.Pop();
            frameBuffer.AddLast(node);
        }

        public void ReadFrame()
        {
            ShiftRegister();
            var node = frameBuffer.Last;
            var currentFrame = node.Value;
            amcReader.ReadAMCFrame(currentFrame);
        }
    }

}


