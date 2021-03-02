using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MotionParsers
{
    public class RigControl : MonoBehaviour
    {
        private const string ACCLAIMFILEPATH = "/MotionParsers/Acclaim/AcclaimFiles/";
        private const int BODYCOUNT  = 1;
        private const int JOINTCOUNT = 25;

        public string asfFile, amcFile;

        public GameObject humanoid;
        public bool mirror = true;
        public bool move = true;

        private ASFReader asfReader;
        private AMCReader amcReader;
        private AcclaimHandler acclaimHandler;

        private CharacterSkeleton skeleton;
        private int[] state;

        private string ASFPath
        {
            get { return Application.dataPath + ACCLAIMFILEPATH + asfFile + ".asf"; }
        }

        private string AMCPath
        {
            get { return Application.dataPath + ACCLAIMFILEPATH + amcFile + ".amc"; }
        }

        void Start()
        {
            skeleton = new CharacterSkeleton(humanoid);
            state = new int[BODYCOUNT * JOINTCOUNT];

            for (int i = 0; i < state.Length; i++)
                state[i] = CharacterSkeleton.TrackingState_Tracked;

            asfReader = new ASFReader(ASFPath);
            amcReader = new AMCReader(AMCPath);

            AcclaimToKinectConverter cvt
                = new AcclaimToKinectConverter();

            acclaimHandler = new AcclaimHandler(asfReader, amcReader, cvt);
        }

        void Update()
        {


        }

        void LateUpdate()
        {

            acclaimHandler.SetSkeleton(out float[] data);
            if (data != null)
            {
                skeleton.set(data, state, 0, mirror, move);
            }

            acclaimHandler.ReadFrame();
            amcReader.CheckForLoopEnd();
        }
    }
}


