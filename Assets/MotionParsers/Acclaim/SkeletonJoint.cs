using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    public class SkeletonJoint
    {
        private AcclaimSkeleton acclaimSkeleton;

        public Matrix4x4 G { get; set; }
        public Matrix4x4 L { get; set; }
        public Matrix4x4 C { get; private set; }
        public Matrix4x4 CInv { get; private set; }

        private bool BSet;
        private Matrix4x4 _B;
        public Matrix4x4 B
        {
            get
            {
                if (!BSet && Parent != null)
                {
                    Vector3 parentD = Parent.Direction;
                    float parentL = Parent.Length;
                    _B = Matrix4x4.Translate(parentD * parentL);
                    BSet = true;
                }
                return _B;
            }
        }

        public Vector3 Coordinate { get; set; }
        public Quaternion LocalRotation { get; set; }
        public SkeletonJoint Parent { get; set; }
        public SkeletonJoint[] Children { get; set; }

        public string Name
        {
            get { return acclaimSkeleton.Name; }
        }

        public float Length
        {
            get { return acclaimSkeleton.Length; }
        }

        public string[] Dof
        {
            get { return acclaimSkeleton.Dof; }
        }

        public float[] Limits
        {
            get { return acclaimSkeleton.Limits; }
        }

        private Vector3 _direction = Vector3.positiveInfinity;
        public Vector3 Direction
        {
            get
            {
                if (_direction.Equals(Vector3.positiveInfinity))
                {
                    float[] d = acclaimSkeleton.Direction;
                    _direction = new Vector3(d[0], d[1], d[2]);
                    return _direction;
                }
                else
                {
                    return _direction;
                }
            }
        }

        private Vector3 _axis = Vector3.positiveInfinity;
        public Vector3 Axis
        {
            get
            {
                if (_axis.Equals(Vector3.positiveInfinity))
                {
                    float[] a = acclaimSkeleton.Axis;
                    _axis = new Vector3(a[0], a[1], a[2]);
                    return _axis;
                }
                else
                {
                    return _axis;
                }
            }
        }

        public SkeletonJoint(AcclaimSkeleton skeleton)
        {
            acclaimSkeleton = skeleton;
            _B = Matrix4x4.Translate(Vector3.zero);
            C = (Axis * Mathf.Deg2Rad).Euler2Mat();
            CInv = Matrix4x4.Inverse(C);

            Debug.Log(Name + "\n" + C.ToString() + "\n" + CInv.ToString());
        }
    }
}


