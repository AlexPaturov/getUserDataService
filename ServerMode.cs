using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace GetUserDataServiceGUI
{
    public class ServerMode
    {
        private volatile bool _run = true;
        private Socket socket = null;   
        private Connection conn;
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public int Run(
            Dictionary<string, string> d,
            CrossThreadComm.TraceCb traceFunc,
            CrossThreadComm.UpdateState updState)
        {
            return this.Run(d, traceFunc, updState, (CrossThreadComm.UpdateRXTX)null);
        }

        public int Run(
            Dictionary<string, string> d,
            CrossThreadComm.TraceCb traceFunc,
            CrossThreadComm.UpdateState updState,
            CrossThreadComm.UpdateRXTX updRxTx)
        {
            //if (traceFunc != null)
            //    traceFunc((object)"SOCKET SERVER MODE");
            DateTime dtPrevious = DateTime.Now;
            _run = true;

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind((EndPoint)new IPEndPoint(IPAddress.Any, int.Parse(d["clientPort"].Trim())));
            socket.Listen(1);
            socket.ReceiveTimeout = 10;
            //socket.Blocking = false;

            while (_run)
            {
                Socket soc = null;

                try
                {
                    if (socket.Poll(1000, SelectMode.SelectRead))
                        soc = socket.Accept(); // летит исключение, если убрать задержку из StopRequest(), после _run = false;
                }
                catch (Exception ex) // для удобства - пишу исключение и пробрасываю наверх, как предусмотрено первоначальной логикой
                {
                    logger.Error(ex);
                    throw;
                }

                if (soc != null)
                {
                    traceFunc("ARM weighter connected");
                    conn = new Connection();
                    try
                    {
                        conn.StartConnection(soc, d, traceFunc, updState, updRxTx);
                    }
                    catch (Exception ex)
                    {
                        traceFunc("ARM to moxa connection initialization failed");
                        traceFunc(ex.Message);
                        logger.Error(ex);
                        conn = null;
                    }
                }
                else
                {
                    if (DateTime.Now.Subtract(dtPrevious).TotalSeconds > 10.0)
                    {
                        traceFunc("Server active and idle");
                        logger.Info("Server active and idle");
                        dtPrevious = DateTime.Now;
                    }
                    Thread.Sleep(1);
                }
            }

            traceFunc("Server shutting down");
            

            //socket.Shutdown(SocketShutdown.Both);
            //socket.Disconnect(true);
            //socket.Close();
            socket = null;
            
            conn = null;

            if (updState != null)
                updState((object)this, CrossThreadComm.State.terminate);
            return 0;
        }

        public void StopRequest()
        {
            if (conn != null)
                conn.StopRequest();
            _run = false;
            Thread.Sleep(1000);  // даю время обновиться состоянию флага _run (и завершиться начатому запросу)  

            if (socket != null)
            {
                if (socket.Connected) 
                {
                    socket.Disconnect(true);
                    logger.Info("Server shutting down " + socket.RemoteEndPoint.ToString());
                }
                socket.Close();
            }
            socket = null;
        }
    }
}
