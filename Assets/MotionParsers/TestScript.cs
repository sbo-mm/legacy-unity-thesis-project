using System.Collections.Generic;
using MotionParsers;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    private const string ACCLAIMFILEPATH = "/MotionParsers/Acclaim/AcclaimFiles/";
    public string asfFile, amcFile;

    public float scale = 0.03f;

    private string ASFPath
    {
        get { return Application.dataPath + ACCLAIMFILEPATH + asfFile + ".asf"; }
    }

    private string AMCPath
    {
        get { return Application.dataPath + ACCLAIMFILEPATH + amcFile + ".amc"; }
    }


    public GameObject armJoint;
    public GameObject lowerArm;

    private ASFReader asfReader;
    private AMCReader amcReader;
    private AcclaimHandler acclaimHandler;

    private GameObject[] objs;
    private GameObject upperCube, lowerCube;
    private Vector3 initUp, initLow;

    public const int
      // JointType
      JointType_SpineBase = 0,
      JointType_SpineMid = 1,
      JointType_Neck = 2,
      JointType_Head = 3,
      JointType_ShoulderLeft = 4,
      JointType_ElbowLeft = 5,
      JointType_WristLeft = 6,
      JointType_HandLeft = 7,
      JointType_ShoulderRight = 8,
      JointType_ElbowRight = 9,
      JointType_WristRight = 10,
      JointType_HandRight = 11,
      JointType_HipLeft = 12,
      JointType_KneeLeft = 13,
      JointType_AnkleLeft = 14,
      JointType_FootLeft = 15,
      JointType_HipRight = 16,
      JointType_KneeRight = 17,
      JointType_AnkleRight = 18,
      JointType_FootRight = 19,
      JointType_SpineShoulder = 20,
      JointType_HandTipLeft = 21,
      JointType_ThumbLeft = 22,
      JointType_HandTipRight = 23,
      JointType_ThumbRight = 24,
      // TrackingState
      TrackingState_NotTracked = 0,
      TrackingState_Inferred = 1,
      TrackingState_Tracked = 2,
      // Number
      bodyCount = 6,
      jointCount = 25;

    private static int[] jointSegment = new int[] {
        JointType_SpineBase, JointType_SpineMid,             // Spine
        JointType_Neck, JointType_Head,                      // Neck
        // left
        JointType_ShoulderLeft, JointType_ElbowLeft,         // LeftUpperArm
        JointType_ElbowLeft, JointType_WristLeft,            // LeftLowerArm
        JointType_WristLeft, JointType_HandLeft,             // LeftHand
        JointType_HipLeft, JointType_KneeLeft,               // LeftUpperLeg
        JointType_KneeLeft, JointType_AnkleLeft,             // LeftLowerLeg6
        JointType_AnkleLeft, JointType_FootLeft,             // LeftFoot
        // right
        JointType_ShoulderRight, JointType_ElbowRight,       // RightUpperArm
        JointType_ElbowRight, JointType_WristRight,          // RightLowerArm
        JointType_WristRight, JointType_HandRight,           // RightHand
        JointType_HipRight, JointType_KneeRight,             // RightUpperLeg
        JointType_KneeRight, JointType_AnkleRight,           // RightLowerLeg
        JointType_AnkleRight, JointType_FootRight,           // RightFoot
      };


    private static string[] segment =
    {
        "root", "lhipjoint",
        "lhipjoint", "rhipjoint",
        "rhipjoint", "lowerback",
        "lhipjoint", "lfemur",
        "lfemur", "ltibia",
        "ltibia", "lfoot",
        "lfoot", "ltoes",
        "rhipjoint", "rfemur",
        "rfemur", "rtibia",
        "rtibia", "rfoot",
        "rfoot", "rtoes",
        "lowerback", "upperback",
        "upperback", "thorax",
        "thorax", "lowerneck",
        "lowerneck", "lclavicle",
        "lclavicle", "rclavicle",
        "lowerneck", "upperneck",
        "upperneck", "head",
        "lclavicle", "lhumerus",
        "lhumerus", "lradius",
        "lradius", "lwrist",
        "lwrist", "lhand",
        "lhand", "lthumb",
        "lhand", "lfingers",
        "rclavicle", "rhumerus",
        "rhumerus", "rradius",
        "rradius", "rwrist",
        "rwrist", "rhand",
        "rhand", "rthumb",
        "rhand", "rfingers"
    };

    Dictionary<string, GameObject> jointObjs;

    Vector3[] joints;

    void Start()
    {
        asfReader = new ASFReader(ASFPath);
        amcReader = new AMCReader(AMCPath);

        AcclaimToKinectConverter cvt 
            = new AcclaimToKinectConverter();

        acclaimHandler = new AcclaimHandler(asfReader, amcReader, cvt);

        jointObjs = new Dictionary<string, GameObject>();
        foreach (var item in acclaimHandler.skeletonJointDict)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = new Vector3(scale, scale, scale);
            go.name = item.Key;
            jointObjs.Add(item.Key, go);
        }

        /*
        objs = new GameObject[AcclaimToKinectConverter.AcclaimKinectMap.Count];
        for (int i = 0; i < objs.Length; i++)
        {
            objs[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            objs[i].transform.localScale = new Vector3(scale, scale, scale);
            objs[i].name = AcclaimToKinectConverter.AcclaimKinectMap[i][0];
        }

        joints = new Vector3[objs.Length];
        */

        /*
        initUp = Vector3.zero;
        float upperS = 0.3f;
        upperCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        upperCube.transform.localScale = new Vector3(upperS, upperS, upperS);
        upperCube.transform.position = initUp;

        lowerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lowerCube.transform.localScale = upperCube.transform.localScale;
        float yT = initUp.y + 1.2f*upperS;
        initLow = new Vector3(initUp.x, -yT, initUp.z);
        lowerCube.transform.position = initLow;
        */
    }

    void Update()
    {

        Dictionary<string, SkeletonJoint> pose = acclaimHandler.SetSkeleton();
        if (pose != null)
        {
            foreach (var item in pose)
            {
                GameObject g = jointObjs[item.Key];
                g.transform.position = item.Value.Coordinate;
                //item.Value.G.
            }

            for (int i = 0; i < segment.Length / 2; i++)
            {
                string s = segment[2 * i]; string e = segment[2 * i + 1];
                Vector3 vS = pose[s].Coordinate;
                Vector3 vE = pose[e].Coordinate;
                Debug.DrawLine(vS, vE, Color.red);
                //Debug.DrawRay(joints[s], Vector3.up * 0.2f, Color.green);
            }
        }

        /*
        acclaimHandler.SetSkeleton(out float[] data);
        if (data != null)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                float x = data[i * 3];
                float y = data[i * 3 + 1];
                float z = data[i * 3 + 2];
                Vector3 p = new Vector3(x, y, z);
                objs[i].transform.position = p;
                joints[i] = p;
            }


            for (int i = 0; i < jointSegment.Length/2; i++)
            {
                int s = jointSegment[2 * i]; int e = jointSegment[2 * i + 1];
                //Vector3 dir = joints[e] - joints[s];
                Debug.DrawLine(joints[s], joints[e], Color.red);
                Debug.DrawRay(joints[s], Vector3.up * 0.2f, Color.green);
            }
            */

        /*
        Vector3 ang = Vector3.forward;
        Vector3 shoulder = joints[JointType_ShoulderRight] - joints[JointType_ShoulderLeft];
        shoulder = new Vector3(shoulder.x, 0, shoulder.z);
        Vector3 sp = (joints[JointType_ShoulderRight] + joints[JointType_ShoulderLeft]) / 2f;
        Debug.DrawRay(sp, shoulder.normalized * 0.2f, Color.blue);
        Debug.DrawRay(sp, ang * 0.2f, Color.magenta);

        Quaternion srot = Quaternion.FromToRotation(ang, shoulder.normalized);

        //upperCube.transform.position = sp + 2f * shoulder + initUp;
        //lowerCube.transform.position = sp + 2f * shoulder + initLow;
        armJoint.transform.position = joints[JointType_ShoulderLeft];

        Vector3 waist = joints[JointType_HipRight] - joints[JointType_HipLeft];
        waist = new Vector3(waist.x, 0, waist.z);
        sp = (joints[JointType_HipRight] + joints[JointType_HipLeft]) / 2f;
        Debug.DrawRay(sp, shoulder.normalized * 0.2f, Color.blue);
        Debug.DrawRay(sp, ang * 0.2f, Color.magenta);

        Quaternion rot = Quaternion.FromToRotation(ang, waist.normalized);

        //lowerCube.transform.rotation = rot;

        Vector3 dir = joints[jointSegment[5]] - joints[jointSegment[4]];
        //Debug.DrawRay(joints[jointSegment[4]], dir.normalized);
        Quaternion leftArmRot = Quaternion.LookRotation(dir.normalized, Vector3.left);
        armJoint.transform.rotation = leftArmRot;

        dir = joints[jointSegment[7]] - joints[jointSegment[6]];
        Debug.DrawRay(joints[jointSegment[6]], dir.normalized);
        Quaternion lowerArmRot = Quaternion.LookRotation(dir.normalized, Vector3.left);
        lowerArm.transform.localRotation = Quaternion.Inverse(leftArmRot) * lowerArmRot;

        //Vector3 pq = upperCube.transform.position;
        //Quaternion qp = new Quaternion(pq.x, pq.y, pq.z, 0);
        //Quaternion p_prime = Quaternion.Inverse(leftArmRot) * qp * leftArmRot;
        //upperCube.transform.position = new Vector3(p_prime.x, p_prime.y, p_prime.z);
        //upperCube.transform.rotation = spineRot;
        //upperCube.transform.rotation = leftArmRot;
        */
    }

    private void LateUpdate()
    {
        acclaimHandler.ReadFrame();
        amcReader.CheckForLoopEnd();
    }

}

