using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using com.miracom.transceiverx;
using com.miracom.transceiverx.session;
using com.miracom.transceiverx.message;
using System.Diagnostics;

namespace HanjinChatBot.SAP
{
    public class TestEaiCall
    {
        //static void Main(string[] args)
        public void call()
        {
            try
            {
                //H101EaiCall eai = new H101EaiCall();

                // 채널시스템 구분
                String systemID = "CJ_Feed_CJONE";
                // 인터페이스ID ==> 연계 업무마다 정의됨
                String interfaceID = "MxZ_SD_FEEDPRM_CREDIT";
                // EAI호출프로그램명 ==> 구분가능한 프로그램명
                String sessionName = "APP_PRG_NAME";

                //String data = "<?xml version='1.0' encoding='UTF-8'?>";
                //data = data + "<EAI_REQUEST>";
                //data = data + "<INTERFACE_INFO>";
                //data = data + "<REQUEST_SYSTEM>CJ_Feed_CJONE</REQUEST_SYSTEM>";
                //data = data + "<UUID>CJ_Feed_CJONE_20160226172910785</UUID>";
                //data = data + "<OPTIONAL_1/>";
                //data = data + "<OPTIONAL_2/>";
                //data = data + "<OPTIONAL_3/>";
                //data = data + "<OPTIONAL_4/>";
                //data = data + "</INTERFACE_INFO>";
                //data = data + "<INPUT_DATA>";
                //data = data + "<I_KUNNR>0000200873</I_KUNNR>";
                //data = data + "</INPUT_DATA>";
                //data = data + "</EAI_REQUEST>";

                String data = " { ";
                data = data + "   'EAI_REQUEST': { ";
                data = data + "     'INTERFACE_INFO': { ";
                data = data + "       'REQUEST_SYSTEM': 'CJ_CHATBOT', ";
                data = data + "       'UUID': 'CJ_CHATBOT_2016021812253212', ";
                //data = data + "       'OPTIONAL_1': 'CJ_SAP / CJ_BI / CJG_BI (응답시스템에 따라 선택)', ";
                data = data + "       'OPTIONAL_1': 'CJ_SAP', ";
                data = data + "       'OPTIONAL_2': 'CHATBOT' ";
                data = data + "       'OPTIONAL_3': '' ";
                data = data + "       'OPTIONAL_4': '' ";
                data = data + "     }, ";
                data = data + "     'INPUT_DATA': { ";
                data = data + "       'P_ID': 'CJ월드ID',  ";
                data = data + "       'P_NUMxP_PERNR': '사번',";
                data = data + "       'P_TEXT': '재발급사유(5글자이상)' ";
                data = data + "     } ";
                data = data + "   }";
                data = data + " } ";

                Debug.WriteLine("------START Send Request Message...\n" + data + "------END Send Request Message...\n");

                //String repData = eai.sendMessage(sessionName, systemID, interfaceID, data, 5000);

                //Console.WriteLine("------Reply Message...\n" + repData);
            }
            catch (TrxException ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
