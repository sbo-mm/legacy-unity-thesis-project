using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    public class AcclaimToKinectConverter : IMocapConverter
    {

        private static Type GetReturnType
        {
            get { return typeof(float[]); }
        }


        public static readonly Dictionary<int, string[]> AcclaimKinectMap
            = new Dictionary<int, string[]>
        {
            [0]  = new string[] { "root" },
            [1]  = new string[] { "upperback" },
            [2]  = new string[] { "lowerneck" },
            [3]  = new string[] { "head" },
            [4]  = new string[] { "lclavicle" },
            [5]  = new string[] { "lhumerus" },
            [6]  = new string[] { "lwrist" },
            [7]  = new string[] { "lhand" },
            [8]  = new string[] { "rclavicle" },
            [9]  = new string[] { "rhumerus" },
            [10] = new string[] { "rwrist" },
            [11] = new string[] { "rhand" },
            [12] = new string[] { "lhipjoint" },
            [13] = new string[] { "lfemur" },
            [14] = new string[] { "ltibia" },
            [15] = new string[] { "lfoot" },
            [16] = new string[] { "rhipjoint" },
            [17] = new string[] { "rfemur" },
            [18] = new string[] { "rtibia" },
            [19] = new string[] { "rfoot" },
            [20] = new string[] { "thorax" },
            [21] = new string[] { "lfingers" },
            [22] = new string[] { "lthumb" },
            [23] = new string[] { "rfingers" },
            [24] = new string[] { "rthumb" }
        };


            /*
        public static readonly Dictionary<int, string[]> AcclaimKinectMap
            = new Dictionary<int, string[]>
        {
            [0] = new string[] { "root" },
            [1] = new string[] { "upperback" },
            [2] = new string[] { "lowerneck" },
            [3] = new string[] { "head" },
            [4] = new string[] { "rclavicle" },
            [5] = new string[] { "rhumerus" },
            [6] = new string[] { "rwrist" },
            [7] = new string[] { "rhand" },
            [8] = new string[] { "lclavicle" },
            [9] = new string[] { "lhumerus" },
            [10] = new string[] { "lwrist" },
            [11] = new string[] { "lhand" },
            [12] = new string[] { "rhipjoint" },
            [13] = new string[] { "rfemur" },
            [14] = new string[] { "rtibia" },
            [15] = new string[] { "rfoot" },
            [16] = new string[] { "lhipjoint" },
            [17] = new string[] { "lfemur" },
            [18] = new string[] { "ltibia" },
            [19] = new string[] { "lfoot" },
            [20] = new string[] { "thorax" },
            [21] = new string[] { "rfingers" },
            [22] = new string[] { "rthumb" },
            [23] = new string[] { "lfingers" },
            [24] = new string[] { "lthumb" }
        };
        */

        private static readonly int DOF = 3;

        public object Convert(object arg) 
        {
            Dictionary<string, SkeletonJoint> inData
                = (Dictionary<string, SkeletonJoint>)arg;
                
            int jtidx = 0;
            float[] coords = new float[AcclaimKinectMap.Count * DOF];
            for (int i = 0; i < AcclaimKinectMap.Count; i++)
            {
                Vector3 sum = Vector3.zero;
                string[] jts = AcclaimKinectMap[i];
                foreach (string jt in jts)
                {
                    SkeletonJoint joint = inData[jt];
                    sum += joint.Coordinate;
                }
                sum /= jts.Length;
                coords[jtidx++] = sum.x;
                coords[jtidx++] = sum.y;
                coords[jtidx++] = sum.z;
            }

            return coords;
        }

        public Type GetConvertReturnType()
        {
            return GetReturnType;
        }

    }
}
