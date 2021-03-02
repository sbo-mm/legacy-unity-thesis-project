using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    public class AcclaimHandler
    {
        private const int BUFFERSIZE = 120;
        private const string ROOTKEY = "root";

        private readonly ASFReader asfReader;
        private readonly AMCReader amcReader;
        private readonly AMCFrameBuffer frameBuffer;
        private readonly IMocapConverter mocapConverter;

        public Dictionary<string, SkeletonJoint> skeletonJointDict;

        public int JointCount
        {
            get
            {
                if (skeletonJointDict == null)
                    return 0;

                return skeletonJointDict.Count;
            }
        }

        public AcclaimHandler(ASFReader asfReader, AMCReader amcReader)
        {
            this.asfReader = asfReader;
            this.amcReader = amcReader;
            frameBuffer = new AMCFrameBuffer(BUFFERSIZE, amcReader);
            CreateSkeletonJointDict();
            SetSkeletonHierarchy();
        }

        public AcclaimHandler(ASFReader asfReader, AMCReader amcReader, IMocapConverter cvt) :
            this(asfReader, amcReader)
        {
            mocapConverter = cvt;
        }


        private DictionaryEntry CreateRootJoint()
        {
            AcclaimSkeleton root = new AcclaimSkeleton
            {
                Id = 0,
                Name = ROOTKEY
            };

            return new DictionaryEntry(ROOTKEY, new SkeletonJoint(root));
        }

        private void CreateSkeletonJointDict()
        {
            if (!asfReader.SkeletonState)
                asfReader.ParseSkeleton();

            skeletonJointDict
                = new Dictionary<string, SkeletonJoint>();

            DictionaryEntry entry = CreateRootJoint();
            skeletonJointDict.Add((string)entry.Key, (SkeletonJoint)entry.Value);

            foreach (var item in asfReader.SkeletonBuffer)
            {
                SkeletonJoint jt = new SkeletonJoint(item.Value);
                skeletonJointDict.Add(item.Key, jt);
            }
        }

        private void SetSkeletonHierarchy()
        {
            if (!asfReader.HierarchyState)
                asfReader.ParseHierarchy();

            foreach (var item in asfReader.ASFHierarchy)
            {
                SkeletonJoint parent = skeletonJointDict[item.Key];
                parent.Children = new SkeletonJoint[item.Value.Length];
                for (int i = 0; i < item.Value.Length; i++)
                {
                    SkeletonJoint child
                        = skeletonJointDict[item.Value[i]];
                    parent.Children[i] = child;
                    child.Parent = parent;
                }
            }
        }

        private void SetRootTransform(AMCFrame frame)
        {
            Vector4 point = MotionUtil.LastOne;
            SkeletonJoint root = skeletonJointDict[ROOTKEY];

            float[] vals = frame.GetValue(ROOTKEY).Data;
            float px = vals[0] * AcclaimSkeleton.METER_SCALE;
            float py = vals[1] * AcclaimSkeleton.METER_SCALE;
            float pz = vals[2] * AcclaimSkeleton.METER_SCALE;
            Vector3 transOffset = new Vector3(px, py, pz);

            Vector3 rot3x1 = new Vector3(vals[3], vals[4], vals[5]);
            Matrix4x4 M = (rot3x1 * Mathf.Deg2Rad).Euler2Mat();
            //root.LocalRotation = root.C * M * root.CInv;
            root.L = root.B * root.C * M * root.CInv;
            root.G = root.L * Matrix4x4.Translate(transOffset);

            root.Coordinate = root.G * point;
            foreach (SkeletonJoint child in root.Children)
            {
                SetChildTransform(child, frame, point);
            }

        }

        private void SetChildTransform(SkeletonJoint child, AMCFrame frame, Vector4 p)
        {
            float[] data = frame.GetValue(child.Name).Data;
            Vector3 rot3x1 = Vector3.zero;

            int idx = 0;
            for (int i = 0; data.Length != 0 && i < child.Dof.Length; i++)
            {
                if (asfReader.DOFMap.TryGetValue(child.Dof[i], out int dim))
                {
                    rot3x1[dim] = data[idx];
                    idx++;
                }
            }

            Matrix4x4 M = (rot3x1 * Mathf.Deg2Rad).Euler2Mat();
            child.L = child.B * child.C * M * child.CInv;
            child.G = child.Parent.G * child.L;
            child.Coordinate = child.G * p;

            if (child.Children == null)
                return;

            foreach (SkeletonJoint childOfChild in child.Children)
            {
                SetChildTransform(childOfChild, frame, p);
            }
        }

        public void ReadFrame()
        {
            frameBuffer.ReadFrame();
        }

        public Dictionary<string, SkeletonJoint> SetSkeleton()
        {
            AMCFrame frame = frameBuffer.CurrentFrame;
            if (frame == null)
                return null;

            SetRootTransform(frame);
            return skeletonJointDict;
        }

        public void SetSkeleton<TRes>(out TRes data)
        {
            AMCFrame frame = frameBuffer.CurrentFrame;
            if (frame == null)
            {
                data = default;
                return;
            }

            SetRootTransform(frame);

            if (!typeof(TRes).IsAssignableFrom(mocapConverter.GetConvertReturnType()))
            {
                Debug.LogWarning("Wrong format for Convert return type");
                data = default;
                return;
            }

            data = (TRes)mocapConverter.Convert(skeletonJointDict);
        }

    }

}


