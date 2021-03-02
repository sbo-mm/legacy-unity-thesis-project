using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MotionParsers 
{
    public class ASFReader : BaseReader 
    {
        private readonly string asf_data;
        private AcclaimSkeleton currentSkeleton;

        public Dictionary<string, AcclaimSkeleton> SkeletonBuffer { get; private set; }
        public Dictionary<string, string[]> ASFHierarchy          { get; private set; }

        public bool SkeletonState   { get; private set; }
        public bool HierarchyState  { get; private set; }

        public Dictionary<string, int> DOFMap { get; }

        public ASFReader(string asf_path) 
        {
            asf_data = ReadLines(asf_path);

            currentSkeleton = new AcclaimSkeleton();
            SkeletonBuffer = new Dictionary<string, AcclaimSkeleton>();
            ASFHierarchy = new Dictionary<string, string[]>();

            DOFMap = new Dictionary<string, int>
            {
                ["rx"] = 0,
                ["ry"] = 1,
                ["rz"] = 2
            };
        }

        private void addSkeletonToBuffer()
        {
            if (currentSkeleton != null) 
            {
                SkeletonBuffer.Add(currentSkeleton.Name, currentSkeleton);
            }

            currentSkeleton = new AcclaimSkeleton();
        }

        private void parseId(string[] tokens)
        {
            int id = int.Parse(tokens[0], NumberStyles.Integer);
            currentSkeleton.setId(id);
        }

        private void parseName(string[] tokens)
        {
            currentSkeleton.setName(tokens[0]);
        }

        private void parseDirection(string[] tokens)
        {
            float x = float.Parse(tokens[0], CultureInfo.InvariantCulture);
            float y = float.Parse(tokens[1], CultureInfo.InvariantCulture);
            float z = float.Parse(tokens[2], CultureInfo.InvariantCulture);
            currentSkeleton.setDirection(x, y, z);
        }

        private void parseLength(string[] tokens)
        {
            float len = float.Parse(tokens[0], CultureInfo.InvariantCulture);
            currentSkeleton.setLength(len);
        }

        private void parseAxis(string[] tokens)
        {
            float x = float.Parse(tokens[0], CultureInfo.InvariantCulture);
            float y = float.Parse(tokens[1], CultureInfo.InvariantCulture);
            float z = float.Parse(tokens[2], CultureInfo.InvariantCulture);
            currentSkeleton.setAxis(x, y, z);
        }

        private void parseDof(string[] tokens, StringReader reader)
        {
            string rx = "", ry = "", rz = "";
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] == "rx") rx = tokens[i];
                if (tokens[i] == "ry") ry = tokens[i];
                if (tokens[i] == "rz") rz = tokens[i];

            }
            currentSkeleton.setDof(rx, ry, rz);
            if (tokens.Length > 0) parseLimits(tokens, reader);
        }

        private void parseLimits(string[] dof, StringReader reader)
        {
            string[] tokens;
            for (int i = 0; i < dof.Length; i++)
            {
                tokens = GetLineTokens(reader);
                if (tokens.Length > 2)
                {
                    tokens = tokens.SubArray(1, tokens.Length);
                }

                float[] mm = new float[2]; 
                for (int j = 0; j < tokens.Length; j++)
                {
                    string tr = string.Join(
                        string.Empty, 
                        Regex.Matches(tokens[j], @"-?[0-9][0-9,\.]+")
                        .OfType<Match>().Select(m => m.Value)
                    );
                    mm[j] = float.Parse(tr, CultureInfo.InvariantCulture);
                }

                currentSkeleton.setLimits(DOFMap[dof[i]], mm[0], mm[1]);
            }
        }

        private int _parseSkeleton(StringReader reader)
        {
            string[] tokens = null;
            while (true)
            {
                tokens = GetLineTokens(reader);
                if (tokens == null)
                {
                    return -1;
                }

                if (tokens[0] == ":hierarchy")
                {
                    break;
                }

                if (tokens[0] == "end")
                {
                    addSkeletonToBuffer();
                }

                if(tokens[0] == "id")
                {
                    tokens = tokens.SubArray(1, tokens.Length);
                    parseId(tokens);
                }

                if (tokens[0] == "name")
                {
                    tokens = tokens.SubArray(1, tokens.Length);
                    parseName(tokens);
                }

                if (tokens[0] == "direction")
                {
                    tokens = tokens.SubArray(1, tokens.Length);
                    parseDirection(tokens);
                }

                if (tokens[0] == "length")
                {
                    tokens = tokens.SubArray(1, tokens.Length);
                    parseLength(tokens);
                }

                if (tokens[0] == "axis")
                {
                    Debug.Assert(tokens[4] == "XYZ", "Orientation order unsupported");
                    tokens = tokens.SubArray(1, tokens.Length-1);
                    parseAxis(tokens);
                }

                if (tokens[0] == "dof")
                {
                    tokens = tokens.SubArray(1, tokens.Length);
                    parseDof(tokens, reader);
                }
            }

            return 0;
        }

        public void ParseSkeleton()
        {
            using (StringReader reader = new StringReader(asf_data))
            {
                if (SkipUntilOccurenceOf(":bonedata", reader) < 0)
                    throw new IOException();

                if (_parseSkeleton(reader) < 0)
                    throw new IOException();
            }

            SkeletonState = true;
        }

        private int _parseHierarchy(StringReader reader)
        {
            string[] tokens;
            while ((tokens = GetLineTokens(reader)) != null)
            {
                if (tokens.Length <= 1)
                {
                    continue;
                }

                string parent = tokens[0];
                string[] children = new string[tokens.Length - 1];
                for (int i = 1; i < tokens.Length; i++)
                {
                    children[i - 1] = tokens[i];
                }

                ASFHierarchy.Add(parent, children);
            }

            return 0;
        }

        public void ParseHierarchy()
        {
            using (StringReader reader = new StringReader(asf_data))
            {
                if (SkipUntilOccurenceOf(":hierarchy", reader) < 0)
                    throw new IOException();
                    
                if (_parseHierarchy(reader) < 0)
                    throw new IOException();
            }

            HierarchyState = true;
        }

    }
}



