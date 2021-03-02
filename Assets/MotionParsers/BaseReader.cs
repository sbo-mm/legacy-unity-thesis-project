using UnityEngine;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace MotionParsers 
{

    public abstract class BaseReader
    {
        protected void ReadLines(string path, int nlines, out StringBuilder lineBuffer)
        {
            int lineCounter = 0;
            int limit = nlines;
            if (nlines == -1)
            {
                limit = int.MaxValue;
            }

            lineBuffer = new StringBuilder();
            StringBuilder lineReader = new StringBuilder();
            using (StreamReader sr = File.OpenText(path))
            {
                while (lineReader.Append(sr.ReadLine()).Length != 0)
                {
                    if (lineCounter > limit)
                    {
                        break;
                    }

                    lineBuffer.AppendLine(lineReader.ToString());
                    lineReader.Length = 0;
                    lineCounter++;
                }
            }
        }

        protected string ReadLines(string path, int nlines)
        {
            ReadLines(path, nlines, out StringBuilder lineBuffer);
            return lineBuffer.ToString();
        }

        protected string ReadLines(string path)
        {
            return ReadLines(path, -1);
        }

        protected string[] GetLineTokens(TextReader reader)
        {
            string line = reader.ReadLine();
            if (line == null)
            {
                return null;
            }

            line = line.Trim();
            line = Regex.Replace(line, @"\s+", " ");
            return line.Split(' ');
        }

        protected int SkipUntilOccurenceOf(string occ, TextReader reader)
        {
            while (true)
            {
                string line = reader.ReadLine();
                if (line == null)
                {
                    return -1;
                }

                if (line.Contains(occ))
                {
                    break;
                }
            }

            return 0;
        }
    }

}