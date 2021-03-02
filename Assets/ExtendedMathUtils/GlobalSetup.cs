using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtendedMathUtils
{
    public class GlobalSetup : MonoBehaviour
    {
        private Queue<string> errorBuffer;
        private Queue<string> processOutputBuffer;

        private bool processExited  = false;
        private int processExitCode = 0;

        private float prevTimeScale;

        private ExternalLinalgHandler ext;

        void Awake()
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0;

            errorBuffer = new Queue<string>();
            processOutputBuffer = new Queue<string>();

            ext = ExternalLinalgHandler.Instance;
            ext.OnErrorDetected += OnErrorHandler;
            ext.OnDataReceived += OnOutputReceivedHandler;
            ext.OnExit += OnExitHandler;

            StartCoroutine(LogProcessOutputs());
        }

        public void OnOutputReceivedHandler(string s)
        {
            if (processOutputBuffer == null)
                return;

            processOutputBuffer.Enqueue(s);
        }

        public void OnErrorHandler(string e)
        {
            if (errorBuffer == null)
                return;

            errorBuffer.Enqueue(e);
        }

        public void OnExitHandler(int code)
        {
            processExitCode = code;
            processExited = true;
        }

        IEnumerator LogProcessOutputs()
        {
            while (!processExited)
            {
                if (processOutputBuffer.Count > 0)
                {
                    for (int i = 0; i < processOutputBuffer.Count; i++)
                    {
                        Debug.Log(processOutputBuffer.Dequeue());
                    }
                }

                yield return new WaitForEndOfFrame();
            }

            for (int i = 0; i < processOutputBuffer.Count; i++)
            {
                Debug.Log(processOutputBuffer.Dequeue());
            }

            Time.timeScale = prevTimeScale; 
            Debug.Log($"Process exited with: {processExitCode}");
            yield break;
        }
    }

}