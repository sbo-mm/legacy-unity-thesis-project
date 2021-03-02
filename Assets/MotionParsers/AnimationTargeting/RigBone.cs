using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    public class RigBone
    {
        public GameObject gameObject;
        public HumanBodyBones bone;
        public bool isValid;

        public Transform transform
        {
            get { return animator.GetBoneTransform(bone); }
        }

        public Vector3 right
        {
            get { return initRight; }
        }

        public Vector3 up
        {
            get { return initUp; }
        }

        public Vector3 forward
        {
            get { return initForward; }
        }

        Animator animator;
        Quaternion savedValue;

        Vector3 initRight, initUp, initForward;

        public RigBone(GameObject g, HumanBodyBones b)
        {
            gameObject = g;
            bone = b;
            isValid = false;
            animator = gameObject.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.Log("no Animator Component");
                return;
            }
            Avatar avatar = animator.avatar;
            if (avatar == null || !avatar.isHuman || !avatar.isValid)
            {
                Debug.Log("Avatar is not Humanoid or it is not valid");
                return;
            }
            isValid = true;
            savedValue = animator.GetBoneTransform(bone).localRotation;

            initUp = up;
            initRight = right;
            initForward = forward;
        }

        public Quaternion GetRotationXYZ(Vector3 dir)
        {
            Quaternion q = Quaternion.identity;
            q *= Quaternion.FromToRotation(initRight, dir);
            q *= Quaternion.FromToRotation(initForward, dir);
            q *= Quaternion.FromToRotation(initUp, dir);
            return q;
        }

        public void DebugAxes()
        {
            Debug.DrawRay(transform.position, initRight * 0.2f, Color.red);
            Debug.DrawRay(transform.position, initUp * 0.2f, Color.green);
            Debug.DrawRay(transform.position, initForward * 0.2f, Color.blue);
        }

        public void set(float a, float x, float y, float z)
        {
            set(Quaternion.AngleAxis(a, new Vector3(x, y, z)));
        }

        public void set(Quaternion q)
        {
            animator.GetBoneTransform(bone).localRotation = q;
            savedValue = q;
        }

        public void mul(float a, float x, float y, float z)
        {
            mul(Quaternion.AngleAxis(a, new Vector3(x, y, z)));
        }

        public void mul(Quaternion q)
        {
            Transform tr = animator.GetBoneTransform(bone);
            tr.localRotation = q * tr.localRotation;
        }

        public void offset(float a, float x, float y, float z)
        {
            offset(Quaternion.AngleAxis(a, new Vector3(x, y, z)));
        }

        public void offset(Quaternion q)
        {
            Debug.Log((q * savedValue).eulerAngles.ToString());
            animator.GetBoneTransform(bone).localRotation = q * savedValue;
        }

        public void changeBone(HumanBodyBones b)
        {
            bone = b;
            savedValue = animator.GetBoneTransform(bone).localRotation;
        }
    }
}


