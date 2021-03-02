using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    public class AcclaimSkeleton
    {
        public static float METER_SCALE = 0.056444f;

        private const int BASEDOF = 3;
        private const int LIMSRANGE = 2; 

        public int Id;
        public string Name;
        public float Length;

        public float[] Direction;
        public float[] Axis;
        public float[] Limits;
        public string[] Dof;

        public AcclaimSkeleton()
        {
            Direction = new float[BASEDOF];
            Axis = new float[BASEDOF];
            Limits = new float[LIMSRANGE * BASEDOF];
            Dof = new string[BASEDOF];
        }

        public void setId(int id)
        {
            Id = id;
        }

        public void setName(string name)
        {
            Name = name;
        }

        public void setLength(float len)
        {
            Length = len * METER_SCALE;
        }

        public void setDirection(float x, float y, float z)
        {
            Direction[0] = x;
            Direction[1] = y;
            Direction[2] = z;
        }

        public void setAxis(float x, float y, float z)
        {
            Axis[0] = x;
            Axis[1] = y;
            Axis[2] = z;
        }

        public void setDof(string rx, string ry, string rz)
        {
            Dof[0] = rx;
            Dof[1] = ry;
            Dof[2] = rz;
        }

        public void setLimits(int d, float min, float max)
        {
            int idx = d * LIMSRANGE;
            Limits[idx] = min;
            Limits[idx + 1] = max;
        }

    }
}


