using System;
using System.IO;
using System.Net;
//using System.IO.
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

using zdouble = System.Numerics.Complex;

namespace ExtendedMathUtils
{
    public sealed class ExternalLinalgHandler
    {
        public delegate void OnErrorDelegate(string s);
        public delegate void OnProcessDataReceived(string s);
        public delegate void OnProcessExit(int exitCode);

        public OnErrorDelegate OnErrorDetected { get;  set; }

        public OnProcessDataReceived OnDataReceived
        {
            get
            {
                if (ext != null)
                {
                    return ext.OnDataReceived;
                }
                return null;
            }
            set
            {
                if (ext != null)
                {
                    ext.OnDataReceived = value;
                }
            }
        }

        public OnProcessExit OnExit
        {
            get
            {
                if (ext != null)
                {
                    return ext.OnExit;
                }
                return null;
            }
            set
            {
                if (ext != null)
                {
                    ext.OnExit = value;
                }
            }
        }

        private static ExternalLinalgHandler instance;
        private static readonly object padlock = new object();

        private const string PYTHON_PATH 
            = "/Users/SophusOlsen/miniconda3/bin/python";
        private const string PYTHON_SCRIPT_PATH
            = "/Users/SophusOlsen/Desktop/PyLinAlgServer/extlin_main.py";

        private const int PORT          = 9009;
        private const int BUFSIZE       = 8192;
        private const int FLOAT64BDEPTH = 8;
        private const int INT32BDEPTH   = 4;
        private const float TIMEOUT     = 5f;
        
        private readonly ExtLinAlgProcess ext;
        
        private readonly IPAddress ipAddr; 
        private readonly IPEndPoint ipEndPoint;

        private static MatrixBuilder<double>  MB = Matrix<double>.Build;
        private static VectorBuilder<zdouble> VB = Vector<zdouble>.Build;

        private ExternalLinalgHandler()
        {
            ipAddr = IPAddress.Loopback;
            ipEndPoint = new IPEndPoint(ipAddr, PORT);

            string APP_ARGS 
                = $"{PYTHON_SCRIPT_PATH} {ipAddr.ToString()} {PORT} {TIMEOUT}";

            ext = new ExtLinAlgProcess(PYTHON_PATH, APP_ARGS);
            if (!ext.Running)
            {
                ext.Run();
            }
        }

        public static ExternalLinalgHandler Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                        instance = new ExternalLinalgHandler();

                    return instance;
                }
            }
        }

        private Socket GetSocket()
        {
            return new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
        }

        private byte[] SerializeArray(double[] src)
        {
            byte[] mem = new byte[src.Length * FLOAT64BDEPTH + INT32BDEPTH];
            Buffer.BlockCopy(src, 0, mem, INT32BDEPTH, mem.Length - INT32BDEPTH);
            byte[] len = BitConverter.GetBytes(mem.Length - INT32BDEPTH);
            Buffer.BlockCopy(len, 0, mem, 0, INT32BDEPTH);
            return mem;
        }

        private double[] DeserializeArray(byte[] src)
        {
            return DeserializeArray(src, 0, src.Length);           
        }

        private double[] DeserializeArray(byte[] src, int offset, int count)
        {
            int sz = count / FLOAT64BDEPTH;
            double[] arr = new double[sz];
            Buffer.BlockCopy(src, offset, arr, 0, count);
            return arr;
        }

        private byte[] TrySerializeMatrix(Matrix<double> m)
        {
            double[] _struct = m.ToRowMajorArray();
            return SerializeArray(_struct);
        }

        private bool SendMatrixBytes(Socket sock, byte[] bytes)
        {
            byte[] buffer = new byte[BUFSIZE];
            int bytesToSend = bytes.Length;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                do
                {
                    try
                    {
                        int packet = ms.Read(buffer, 0, BUFSIZE);
                        sock.Send(buffer);
                        bytesToSend -= packet;
                    }
                    catch (Exception e)
                    {
                        if (OnErrorDetected != null)
                            OnErrorDetected.Invoke(e.ToString());
                        return false;
                    }

                } while (bytesToSend > 0);
            }

            return true;
        }

        private async Task<bool> SendMatrixBytesAsync(Socket sock, byte[] bytes)
        {
            return await Task.Run<bool>(() =>
            {
                return SendMatrixBytes(sock, bytes);
            });
        }

        private byte[] ReceiveMatrixBytes(Socket sock)
        {
            int bytesToReceive;
            byte[] recv = new byte[BUFSIZE];

            try
            {
                byte[] dataLen = new byte[INT32BDEPTH];
                int numbytes = sock.Receive(dataLen);

                if (numbytes != INT32BDEPTH)
                    return new byte[0];

                bytesToReceive = BitConverter.ToInt32(dataLen, 0);

                if (bytesToReceive == 0)
                    return new byte[0];
            }
            catch (Exception e)
            {
                if (OnErrorDetected != null)
                    OnErrorDetected.Invoke(e.ToString());
                return new byte[0];
            }

            byte[] bytes = new byte[bytesToReceive];
            using(MemoryStream ms = new MemoryStream(bytes))
            {
                while (bytesToReceive > 0)
                {
                    try
                    {
                        int packet = sock.Receive(recv);
                        ms.Write(recv, 0, packet);
                        bytesToReceive -= packet;
                    }
                    catch (Exception e)
                    {
                        if (OnErrorDetected != null)
                            OnErrorDetected.Invoke(e.ToString());
                        bytes = new byte[0];
                        break;
                    }
                }
            }
            return bytes;
        }

        private async Task<byte[]> ReceiveMatrixBytesAsync(Socket sock)
        {
            return await Task.Run<byte[]>(() =>
            {
                return ReceiveMatrixBytes(sock);
            });
        }

        private bool TryConnect(Socket sock)
        {
            try
            {
                sock.Connect(ipEndPoint);
                return true;
            }
            catch (Exception e)
            {
                if (OnErrorDetected != null)
                    OnErrorDetected.Invoke(e.ToString());
                sock.Disconnect(true);
                return false;
            }
        }

        private bool SpinAttemptConnect(Socket sock, int iters = 1000)
        {
            for (int i = 0; i < iters; i++)
            {
                if (TryConnect(sock)) return true;
            }
            return false;
        }

        private async Task<bool> SpinAttemptConnectAsync(Socket sock, int iters = 1000, int millis = 10)
        {
            await Task.Delay(millis);
            return await Task.Run<bool>(() =>
            {
                return SpinAttemptConnect(sock, iters);
            });
        }

        private void DisposeSocket(Socket sock)
        {
            sock.Disconnect(false);
            sock.Close();
            sock.Dispose();
        }

        private async Task<byte[]> FetchBytesExternalAsync(Matrix<double> m)
        {
            Socket sock = GetSocket();
            bool conn = await SpinAttemptConnectAsync(sock);

            if (!conn)
            {
                DisposeSocket(sock);
                throw new Exception("Failed to connect to server");
            }

            byte[] matrix_bytes = TrySerializeMatrix(m);
            bool send_succes = await SendMatrixBytesAsync(sock, matrix_bytes);

            if (!send_succes)
            {
                DisposeSocket(sock);
                throw new Exception("Send 0 bytes");
            }

            byte[] recv_bytes = await ReceiveMatrixBytesAsync(sock);
            if (recv_bytes.Length == 0)
            {
                DisposeSocket(sock);
                throw new Exception("Received 0 bytes");
            }

            DisposeSocket(sock);
            return recv_bytes;
        }

        private Matrix<double> GetMatrixFromBytes(byte[] src, int offset, int count, int msize)
        {
            double[] matrixRows = DeserializeArray(src, offset, count);
            return MB.DenseOfRowMajor(msize, msize, matrixRows);
        }

        private Vector<zdouble> GetComplexVectorFromBytes(byte[] src, int offset, int count, int msize)
        {
            double[] compValues = DeserializeArray(src, offset, count);
            Vector<zdouble> ev = VB.Dense(msize);
            for (int i = 0; i < msize; i++)
            {
                double re = compValues[i];
                double im = compValues[msize + i];
                ev[i] = new zdouble(re, im);
            }
            return ev;
        }

        public async Task<ExternalResult> ComputeExternalAsync(Matrix<double> m)
        {
            ExternalResult res = new ExternalResult();
            byte[] bytes = await FetchBytesExternalAsync(m);

            int sz = m.RowCount;
            int origByteSize = sz * sz * FLOAT64BDEPTH;
            int remaining = bytes.Length - origByteSize;

            res.G = GetMatrixFromBytes(bytes, 0, origByteSize, sz);
            res.D = GetComplexVectorFromBytes(bytes, origByteSize, remaining, sz);
            return res;
        }

        public struct ExternalResult
        {
            public Matrix<double>  G;
            public Vector<zdouble> D;
        }

        private sealed class ExtLinAlgProcess
        {
            private Process ext;
            public OnProcessDataReceived OnDataReceived { get; set; }
            public OnProcessExit OnExit                 { get; set; }

            public bool Running { get; private set; } = false;

            public ExtLinAlgProcess(string processPath, string args)
            {
                ext = new Process();
                ext.StartInfo.FileName = processPath;
                ext.StartInfo.Arguments = args;
                ext.StartInfo.UseShellExecute = false;
                ext.EnableRaisingEvents = true;
                ext.StartInfo.RedirectStandardOutput = true;
                ext.StartInfo.RedirectStandardError = true;

                ext.OutputDataReceived += OnOutputHandler;
                ext.ErrorDataReceived += OnErrorReceivedHandler;
                ext.Exited += OnExitedHandler;
            }

            public void Run()
            {
                if (ext != null)
                {
                    ext.Start();
                    ext.BeginOutputReadLine();
                    Running = true;
                }
            }

            private void OnExitedHandler(object s, EventArgs e)
            {
                if (OnExit != null)
                {
                    Running = false;
                    OnExit.Invoke(ext.ExitCode);
                }
            }

            private void OnOutputHandler(object s, DataReceivedEventArgs outLine)
            {
                if (!string.IsNullOrEmpty(outLine.Data))
                {
                    if (OnDataReceived != null)
                    {
                        OnDataReceived.Invoke(outLine.Data);
                    }
                }
            }

            private void OnErrorReceivedHandler(object s, DataReceivedEventArgs outLine)
            {
                return;
            }

        }
    }

}