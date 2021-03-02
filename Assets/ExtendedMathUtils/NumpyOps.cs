using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using MathNet.Numerics.LinearAlgebra;

using zdouble = System.Numerics.Complex;

namespace ExtendedMathUtils
{
    public sealed class NumpyOps
    {
        private static NumpyOps _instance = new NumpyOps();
        public static NumpyOps Numpy
        {
            get
            {
                return _instance;
            }
        }

        private readonly int        _port = 9008;
        private readonly IPAddress  _host = IPAddress.Loopback;

        private readonly Task<Socket> _opSocketTask;
        private Socket _opSocket
        {
            get { return _opSocketTask.Result; }
        }

        private readonly Thread _opThread;
        private readonly PythonProcess _opProcess;

        private const long MAXRECVBYTES = 0x44AA000;
        private readonly byte[] RECVBUF = new byte[MAXRECVBYTES];
        
        private NumpyOps()
        {
            UnityEngine.Application.quitting += OnApplicationExit;

            string _args = $"{_host.ToString()} {_port}";
            _opProcess = new PythonProcess(_args);
            _opProcess.Run();

            _opThread = new Thread(new ThreadStart(OpProc));
            _opThread.Start();

            _opSocketTask = CreateSocketAsync();
        }

        private void OpProc()
        {

            while (!_opProcess.HasExited)
            {

            }

            int exitcode = _opProcess.ExitCode;
        }

        private bool TryConnect(Socket sock, IPEndPoint ep)
        {
            try
            {
                sock.Connect(ep);
                return true;
            }
            catch (Exception e)
            {
                sock.Disconnect(true);
                return false;
            }
        }

        private bool SpinAttemptConnect(Socket sock, int iters = 1000)
        {
            IPEndPoint ep = new IPEndPoint(_host, _port);
            for (int i = 0; i < iters; i++)
            {
                if (TryConnect(sock, ep)) return true;
            }
            return false;
        }

        private Task<Socket> CreateSocketAsync()
        {
            return Task.Run(async () =>
            {
                Socket sock = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);

                await Task.Delay(10);
                SpinAttemptConnect(sock);
                return sock;
            });
        }

        private int ReceiveBytesBuffered(Socket sock, byte[] buffer, int BUFSIZE)
        {
            int readbytes = 0;
            int packet = 0;
            do
            {
                packet = sock.Receive(buffer, readbytes, BUFSIZE, SocketFlags.None);
                readbytes += packet;
            } while (sock.Poll(10, SelectMode.SelectRead));

            return readbytes;
        }

        private Task<byte[]> ReceiveBytesAsync(Socket sock)
        {
            return Task.Run(() =>
            {
                int packet = ReceiveBytesBuffered(sock, RECVBUF, 8192);
                byte[] bytes = new byte[packet];
                Buffer.BlockCopy(RECVBUF, 0, bytes, 0, packet);
                return bytes;
            });
        }

        private bool UnpackBytes(Task<byte[]> recvTask, out byte[] bytes)
        {
            bytes = null;

            try
            {
                bytes = recvTask.Result;
            }
            catch (AggregateException e)
            {
                UnityEngine.Debug.Log(e.ToString());
                return false;
            }

            return true;
        }

        private bool SendBytes(Socket sock, byte[] bytes)
        {
            try
            {
                sock.Send(bytes);
                return true;
            } 
            catch (SocketException e)
            {
                UnityEngine.Debug.Log(e.ToString());
                return false;
            }
            catch(InvalidOperationException e)
            {
                UnityEngine.Debug.Log(e.ToString());
                return false;
            }
        }

        public void DisconnectDisposeExposed()
        {
            _opSocket.Disconnect(false);
            _opSocket.Close();
            _opSocket.Dispose();

            _opThread.Join();

            _opProcess.WaitForExit();
            _opProcess.Dispose();
        }

        private byte[] GetSendBytes(string mod, string fun, 
            IList<ObjectInfo> operands, params string[] kwargs)
        {
            NumpyInfo info = new NumpyInfo
            {
                module = mod,
                function = fun,
                operands = operands,
                kwargs = kwargs
            };

            string infostr = JsonConvert.SerializeObject(info);
            return Encoding.ASCII.GetBytes(infostr);
        }

        public ReturnValueInfo ApplyOp(string _module, string _op,
            IList<ObjectInfo> operands, params string[] kwargs)
        {
            Task<byte[]> recvBytesTask 
                = ReceiveBytesAsync(_opSocket);

            byte[] sendbytes 
                = GetSendBytes(_module, _op, operands, kwargs);

            if (!SendBytes(_opSocket, sendbytes))
                return null;

            byte[] recvbytes = null;
            if (!UnpackBytes(recvBytesTask, out recvbytes))
                return null;
                
            string infostr = Encoding.ASCII.GetString(recvbytes);
            ReturnValueInfo info 
                = JsonConvert.DeserializeObject<ReturnValueInfo>(infostr);

            return info;
        }

        public ReturnValueInfo ApplyOp(string _module, string _op,
            ObjectInfo operand, params string[] kwargs)
        {
            var _wrap = new ObjectInfo[] { operand };
            return ApplyOp(_module, _op, _wrap, kwargs);
        }

        public ReturnValueInfo ApplyOp(string _module, string _op,
            string[] kwargs)
        {
            var empty = new ObjectInfo[0];
            return ApplyOp(_module, _op, empty, kwargs);
        }


        private void OnApplicationExit()
        {
            DisconnectDisposeExposed();
        }

        public sealed class NumpyInfo
        {
            [JsonProperty, JsonRequired]
            public string module { get; set; }

            [JsonProperty, JsonRequired]
            public string function { get; set; }

            [JsonProperty, JsonRequired]
            public IList<ObjectInfo> operands { get; set; }

            [JsonProperty]
            public IList<string> kwargs { get; set; }
        }

        public sealed class ReturnValueInfo
        {
            [JsonProperty]
            public IList<ObjectInfo> returns { get; set; }
        }

        public sealed class ObjectInfo
        {
            [JsonProperty, JsonRequired]
            public int[] shape { get; set; }

            [JsonProperty, JsonRequired]
            public string dtype { get; set; }

            [JsonProperty, JsonRequired]
            public byte[] data { get; set; }

            [JsonProperty]
            public string errmsg { get; set; }
        }


        private sealed class PythonProcess : Process
        {
            private static readonly string PYTHON_PATH
                = "/Users/SophusOlsen/miniconda3/bin/python";
            private static readonly string PYTHON_SCRIPT_PATH
                = "/Users/SophusOlsen/Desktop/PyLinAlgServer/test.py";

            public PythonProcess(string args)
            {
                string _args = $"{PYTHON_SCRIPT_PATH} {args}";
                StartInfo.FileName  = PYTHON_PATH;
                StartInfo.Arguments = _args;

                EnableRaisingEvents              = true;
                StartInfo.UseShellExecute        = false;
                StartInfo.RedirectStandardError  = true;
                StartInfo.RedirectStandardOutput = true;
            }

            public void Run()
            {
                Start();
                BeginErrorReadLine();
                BeginOutputReadLine();
            }
        }
    }

    public static class Numpy
    {
        private static readonly NumpyOps _numpy = NumpyOps.Numpy;

        private const string NUMPYBASE = "numpy";
        private const string NUMPYLINALG = "numpy.linalg";
        private const string NUMPYRANDOM = "numpy.random";

        private const int INT32BITS = 4;
        private const int INT64BITS = 8;
        private const int FLOAT32BITS = 4;
        private const int FLOAT64BITS = 8;
        private const int COMPLEX128 = 16;

        private static VectorBuilder<zdouble> zVB
            = Vector<zdouble>.Build;

        private static MatrixBuilder<double> dMB
            = Matrix<double>.Build;


        private static Dictionary<string, int> _bitSizeTable
            = new Dictionary<string, int>
        {
            ["int32"]   = INT32BITS,
            ["int64"]   = INT64BITS,
            ["float32"] = FLOAT32BITS,
            ["float64"] = FLOAT64BITS,
            ["complex128"] = COMPLEX128
        };
        /*
        public static byte[] ufunc_serializearray(Array src)
        {
            int allocsz = Buffer.ByteLength(src);
            byte[] mem  = new byte[allocsz];
            Buffer.BlockCopy(src, 0, mem, 0, allocsz);
            return mem;
        }

        public static T[] ufunc_deserializearray<T>(byte[] src)
        {
            int allocsz = src.Length / Marshal.SizeOf(typeof(T));
            T[] mem = new T[allocsz];
            Buffer.BlockCopy(src, 0, mem, 0, src.Length);
            return mem;
        }
        */
        private static byte[] _serializeArray(double[] src)
        {
            int allocsz = src.Length * FLOAT64BITS;
            byte[] mem = new byte[allocsz];
            Buffer.BlockCopy(src, 0, mem, 0, allocsz);
            return mem;
        }

        private static double[] _deserializeArray(byte[] src)
        {
            int allocsz = src.Length / FLOAT64BITS;
            double[] mem = new double[allocsz];
            Buffer.BlockCopy(src, 0, mem, 0, src.Length);
            return mem;
        }

        private static long[] _deserializeInt64Array(byte[] src)
        {
            int allocsz = src.Length / INT64BITS;
            long[] mem = new long[allocsz];
            Buffer.BlockCopy(src, 0, mem, 0, src.Length);
            return mem;
        }

        private static NumpyOps.ObjectInfo _getObjectInfo(Matrix<double> m)
        {
            double[] matrix_data = m.ToRowMajorArray();
            byte[] matrix_bytes = _serializeArray(matrix_data);

            return new NumpyOps.ObjectInfo
            {
                shape = new int[] {m.RowCount, m.ColumnCount},
                dtype = "d",
                data = matrix_bytes
            };
        }

        private static NumpyOps.ObjectInfo[] _getObjectInfos(params Matrix<double>[] mats)
        {
            NumpyOps.ObjectInfo[] infos = new NumpyOps.ObjectInfo[mats.Length];
            for (int i = 0; i < infos.Length; i++)
            {
                infos[i] = _getObjectInfo(mats[i]);
            }
            return infos;
        }

        private static void _assertIsValidReturnInfo(NumpyOps.ReturnValueInfo info)
        {
            if (info == null)
            {
                throw new NullReferenceException("Error while handling function");
            }

            string errmsg = info.returns[0].errmsg;
            if (!string.IsNullOrEmpty(errmsg))
            {
                throw new Exception(errmsg);
            }
        }

        private static Matrix<double> _constructMatrixFromObjInfo(
            NumpyOps.ObjectInfo robj)
        {
            int rows = robj.shape[0];
            int cols = robj.shape[1];
            double[] matrix_data = _deserializeArray(robj.data);
            return dMB.DenseOfRowMajor(rows, cols, matrix_data);
        }

        public static Matrix<double> MatMul(Matrix<double> lhs, Matrix<double> rhs)
        {
            string func = "matmul";
            var returnInfo = _numpy.ApplyOp(NUMPYBASE, 
                func, _getObjectInfos(lhs, rhs)
            );

            _assertIsValidReturnInfo(returnInfo);

            var robj = returnInfo.returns[0];
            return _constructMatrixFromObjInfo(robj);
        }

        public static int[] ArgSort(Matrix<double> m, params string[] kwargs)
        {
            string func = "argsort";
            var returnInfo = _numpy.ApplyOp(NUMPYBASE, 
                func, _getObjectInfo(m), kwargs
            );

            _assertIsValidReturnInfo(returnInfo);

            var robj = returnInfo.returns[0];
            long[] int_data = _deserializeInt64Array(robj.data);
            return int_data.Select(Convert.ToInt32).ToArray();
        }

        public static int[] ArgSort(IEnumerable<double> arr)
        {
            int cols = 1;
            int rows = arr.Count();
            var _arr = dMB.DenseOfRowMajor(rows, cols, arr);
            return ArgSort(_arr);
        }

        public static class LinAlg
        {
            public static Matrix<double> Inv(Matrix<double> m)
            {
                string func = "inv";
                var returnInfo = _numpy.ApplyOp(NUMPYLINALG, 
                    func, _getObjectInfo(m)
                );

                _assertIsValidReturnInfo(returnInfo);

                var robj = returnInfo.returns[0];
                return _constructMatrixFromObjInfo(robj);
            }

            public static Matrix<double> Pinv(Matrix<double> m)
            {
                string func = "pinv";
                var returnInfo = _numpy.ApplyOp(NUMPYLINALG,
                    func, _getObjectInfo(m)
                );

                _assertIsValidReturnInfo(returnInfo);

                var robj = returnInfo.returns[0];
                return _constructMatrixFromObjInfo(robj);
            }

            private static EvdResult Eig(string eigfunc, Matrix<double> m)
            {
                var returnInfo = _numpy.ApplyOp(NUMPYLINALG, 
                    eigfunc, _getObjectInfo(m)
                );

                _assertIsValidReturnInfo(returnInfo);

                var eigvalsobj   = returnInfo.returns[0];
                double[] ev_data = _deserializeArray(eigvalsobj.data);

                Vector<zdouble> eigenvalues;
                if (eigvalsobj.dtype.Contains("complex"))
                {
                    int msize = ev_data.Length / 2;
                    eigenvalues = zVB.Dense(msize);

                    for (int i = 0; i < msize; i++)
                    {
                        double re = ev_data[i];
                        double im = ev_data[msize + i];
                        eigenvalues[i] = new zdouble(re, im);
                    }
                }
                else
                {
                    eigenvalues = zVB.Dense(ev_data.Length);
                    for (int i = 0; i < ev_data.Length; i++)
                    {
                        double re = ev_data[i];
                        eigenvalues[i] = new zdouble(re, 0);
                    }
                }

                var eigvecsobj = returnInfo.returns[1];
                Matrix<double> eigenvectors = null;
                if (!eigvecsobj.dtype.Contains("complex"))
                    eigenvectors = _constructMatrixFromObjInfo(eigvecsobj);

                EvdResult result = new EvdResult
                {
                    EigenVectors = eigenvectors,
                    EigenValues  = eigenvalues
                };

                return result;
            }

            public static EvdResult Eig(Matrix<double> m)
            {
                return Eig("eig", m);
            }

            public static EvdResult Eigh(Matrix<double> m)
            {
                return Eig("eigh", m);
            }
        }

        public static class Random
        {
            public static Matrix<double> Normal(double mu, double sig, (int, int) size)
            {
                string func = "normal";

                string _mu   = $"{mu}";
                string _sig  = $"{sig}";
                string _size = $"{size.Item1},{size.Item2}";

                string[] kwargs = { 
                    "loc"  , _mu  , "d", 
                    "scale", _sig , "d", 
                    "size" , _size, "t" 
                };

                var returnInfo = _numpy.ApplyOp(NUMPYRANDOM, 
                    func, kwargs
                );

                _assertIsValidReturnInfo(returnInfo);

                var robj = returnInfo.returns[0];
                return _constructMatrixFromObjInfo(robj);
            }
        }

        public sealed class EvdResult
        {
            public Matrix<double> EigenVectors { get; set; }
            public Vector<zdouble> EigenValues { get; set; }
        }

    }

}