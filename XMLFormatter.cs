using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

/*
    Привожу полученные от контроллера данные к требуемому по спецификации формату, для ответа АРМ(у) весов.
*/

namespace GetUserDataServiceGUI
{
    public static class XMLFormatter
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Получить результат статического взвешивания.
        public static byte[] GetPcDomainName() 
        {
            try { 
                Dictionary<string, string> preparedAnswer = RawToXML(Environment.MachineName);

                XmlDocument xmlDoc = new XmlDocument();                                                     
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            
                XmlElement rootResponse = xmlDoc.CreateElement("Response");                                 
                xmlDoc.AppendChild(rootResponse);                                                           

                    XmlElement ch1_State = xmlDoc.CreateElement("State");                                   
                    ch1_State.InnerText = "Success";
                    rootResponse.AppendChild(ch1_State);

                        XmlElement ch2_StaticData = xmlDoc.CreateElement("StaticData");
                        rootResponse.AppendChild(ch2_StaticData);

                        // Доменное имя ПК
                        XmlElement ch3_MachineName = xmlDoc.CreateElement("MachineName");
                        ch3_MachineName.InnerText = preparedAnswer["MachineName"];
                        ch2_StaticData.AppendChild(ch3_MachineName);

                return Encoding.GetEncoding(1251).GetBytes(xmlDoc.OuterXml);
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                throw new Exception("Error getting pc name");
            }
        }

        // Получаю стандартный Exception и код по спецификации, возвращаю ошибку в установленном спецификацией формате XML.
        public static byte[] GetError(Exception ex, int code) 
        {
            XmlDocument xmlDoc = new XmlDocument();                                                         
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);

            XmlElement rootResponse = xmlDoc.CreateElement("Response");                                     
            xmlDoc.AppendChild(rootResponse);

            XmlElement ch1_State = xmlDoc.CreateElement("State");                                           
            ch1_State.InnerText = "Error";
            rootResponse.AppendChild(ch1_State);

                XmlElement ch2_ErrorDescription = xmlDoc.CreateElement("ErrorDescription");                 
                rootResponse.AppendChild(ch2_ErrorDescription);

                XmlElement ch3_ErrorCode = xmlDoc.CreateElement("ErrorCode");                               
                ch3_ErrorCode.InnerText = code.ToString();
                ch2_ErrorDescription.AppendChild(ch3_ErrorCode);

                XmlElement ch3_ErrorText = xmlDoc.CreateElement("ErrorText");
                ch3_ErrorText.InnerText = ex.Message;
                ch2_ErrorDescription.AppendChild(ch3_ErrorText);

            return Encoding.GetEncoding(1251).GetBytes(xmlDoc.OuterXml);
        }

        // Разбор входной строки и приведение данных к формату в соответствии со спецификацией.
        private static Dictionary<string, string> RawToXML(string input)
        {
            Dictionary<string, string> XMLtmp = new Dictionary<string, string>();
            string workInput = input;

            XMLtmp.Add("MachineName", input); // добавил имя ПК

            return XMLtmp;
        }

       
    }
}
