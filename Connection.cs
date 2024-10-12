using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GetUserDataServiceGUI
{
    public class Connection
    {
        private CrossThreadComm.TraceCb _conInfoCallback;
        private CrossThreadComm.UpdateState _updState;
        private CrossThreadComm.UpdateRXTX _updRxTx;
        public Socket ARMsocket;
        private bool _isfree = true;
        private Dictionary<string, string> _d;
        private bool _keepOpen = true;
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Connection()
        {
        }

        public Connection(
            Socket soc,
            Dictionary<string, string> d,
            CrossThreadComm.TraceCb conInfoCb,
            CrossThreadComm.UpdateState updState)
        {
            StartConnection(soc, d, conInfoCb, updState);
        }

        public Connection(
            Socket soc,
            Dictionary<string, string> d,
            CrossThreadComm.TraceCb conInfoCb,
            CrossThreadComm.UpdateState updState,
            CrossThreadComm.UpdateRXTX updRxTx)
        {
            StartConnection(soc, d, conInfoCb, updState, updRxTx);
        }

        public void SetConnInfoTraceCallback(CrossThreadComm.TraceCb conInfoCb) => _conInfoCallback = conInfoCb;

        public bool IsFree() => _isfree;

        public void TraceLine(string s)
        {
            if (_conInfoCallback == null)
                return;
            _conInfoCallback((object)s);
        }

        public void StopRequest()
        {
            _keepOpen = false;
        }

        public bool StartConnection(
          Socket soc,
          Dictionary<string, string> d,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState)
        {
            return StartConnection(soc, d, conInfoCb, updState, (CrossThreadComm.UpdateRXTX)null);
        }

        public bool StartConnection(
          Socket soc,
          Dictionary<string, string> d,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState,
          CrossThreadComm.UpdateRXTX updRxTx)
        {
            ARMsocket = soc;
            _d = d;
            _updState = updState;
            _updRxTx = updRxTx;
            SetConnInfoTraceCallback(conInfoCb);

            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.start);
            _isfree = false;

            new Thread(new ThreadStart(Tranceiver)).Start();
            return true;
        }

        private void Tranceiver()
        {
            byte[] buffer = new byte[8192];
            _keepOpen = true;
            string cliCmd = string.Empty;

            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.connect);
            
            while (_keepOpen)
            {
                bool transferOccured = false; // флаг свершения передачи данных клиент <-> контроллер
                
                // ------------------------------ от клиента пришёл запрос begin -----------------------------------------------------------------   
                int colByteARM = LimitTo(ARMsocket.Available, 8192);
                if (colByteARM > 0) // 
                {
                    if (_updRxTx != null)
                        _updRxTx((object)this, 0, colByteARM);

                    ARMsocket.Receive(buffer, colByteARM, SocketFlags.None);
                    byte[] ARMbyteArr = new byte[colByteARM];                                               // установил размерность нового массива
                    Buffer.BlockCopy(buffer, 0, ARMbyteArr, 0, colByteARM);
                    logger.Info("ARM query string: " + Encoding.GetEncoding(1251).GetString(ARMbyteArr));   // строка запроса прикладного ПО драйверу


                    byte[] controllerCommand = DecodeClientRequestToControllerCommand(buffer, colByteARM);  // Верну null если команда не корректная, иначе - команду для контроллера
                    if (controllerCommand != null)                                                          // команда корректна -> отправляем устройству
                    {
                        // Сейчас будет работать только этот блок.
                        byte[] btArr = null;
                        try
                        {
                            // Сделать ветвление
                            btArr = XMLFormatter.GetPcDomainName();
                            logger.Info("pc name: " + Encoding.GetEncoding(1251).GetString(btArr));
                            _conInfoCallback((object)"pc name: " + Encoding.GetEncoding(1251).GetString(btArr));
                            // Если возникла ошибка при разборе сообщения - кидаю исключение.
                        }
                        catch (Exception ex)
                        {
                            btArr = XMLFormatter.GetError(ex, 100);                                         // Отформатировал ошибку в XML формат, перевёл в byte[]
                        }

                        ARMsocket.Send(btArr, btArr.Length, SocketFlags.None);
                        transferOccured = true;
                    }
                    else                                                                        // формирование для клиента сообщения об ошибке в его запросе.
                    {
                        Exception ex = new Exception("Custom request format error");               // Согласно спецификации.
                        byte[] errorByteArr = XMLFormatter.GetError(ex, 2);                     // Отформатировал ошибку в XML формат. 
                        ARMsocket.Send(errorByteArr, errorByteArr.Length, SocketFlags.None);    // Отправляем в АРМ весов XML в виде byte[].
                        logger.Error(Encoding.GetEncoding(1251).GetString(errorByteArr));       // пишу ответ для арма весов.
                    }
                }
                // ------------------------------ от клиента пришёл запрос end --------------------------------------------------------------
                
                if (ARMsocket.Poll(3000, SelectMode.SelectRead) & ARMsocket.Available == 0)
                {
                    TraceLine("Lost connection to weighter ARM " + ARMsocket.RemoteEndPoint.ToString());
                    _keepOpen = false;
                }

                if (!transferOccured)
                    Thread.Sleep(1);
            }
            
            if (_updState != null)
                _updState(this, CrossThreadComm.State.disconnect);

            logger.Info("Connect to weighter ARM is closed " + ARMsocket.RemoteEndPoint.ToString()); // отключение от ARM весов
            ARMsocket.Close();
            _isfree = true;
        }

        private int LimitTo(int i, int limit) => i > limit ? limit : i;

        // Перевожу команду полученную от клиента в команду для исполнения контроллером
        private byte[] DecodeClientRequestToControllerCommand(byte[] cliBuffer, int dataLength)
        {
            string controllerCommand = string.Empty;
            byte[] clientComandArr = new byte[dataLength];                     // установил размерность массива для команды клиента
            Buffer.BlockCopy(cliBuffer, 0, clientComandArr, 0, dataLength);    // копирую данные в промежуточный массив 

            string str = Encoding.GetEncoding(1251).GetString(clientComandArr);

            switch (Encoding.GetEncoding(1251).GetString(clientComandArr))
            {
                case "<Request method='set_mode' parameter='Static'/>":     // 1)
                    controllerCommand = null;
                    break;

                case "<Request method='getPcName'/>":                        // 2)
                    controllerCommand = "getPcName";
                    break;

                case "<Request method='9'/>":                      // 3) получить вес, взвешивание в статике
                    controllerCommand = null;
                    break;

                case "<Request method='set_zero' parameter='0'/>":          // 4)
                    controllerCommand = null;
                    break;

                case "<Request method='j'/>":                  // 5)
                    controllerCommand = null;
                    break;

                default:                                                    // 6) Команда полученная от клиента - не распознана.
                    controllerCommand = null;
                    break;
            }

            // Если команда распознана - перекодирую в байтовый массив и возвращаю, иначе верну null.
            if (!string.IsNullOrEmpty(controllerCommand) && controllerCommand.Length > 0)
            {
                return Encoding.GetEncoding(1251).GetBytes(controllerCommand);
            }
            else 
            { 
                return null;
            }
        }

    }
}
