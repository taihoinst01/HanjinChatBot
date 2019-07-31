using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using HanjinChatBot.DB;
using HanjinChatBot.Models;
using Newtonsoft.Json.Linq;

using System.Configuration;
using System.Web.Configuration;
using HanjinChatBot.Dialogs;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.ConnectorEx;
using HanjinChatBot.SAP;
using System.Threading;

using System.Text;
using System.IO;

namespace HanjinChatBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        //MessagesController
        public static readonly string TEXTDLG = "2";
        public static readonly string CARDDLG = "3";
        public static readonly string MEDIADLG = "4";

        public static Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/");
        const string chatBotAppID = "appID";
        public static int appID = Convert.ToInt32(rootWebConfig.ConnectionStrings.ConnectionStrings[chatBotAppID].ToString());

        //config 변수 선언
        static public string[] LUIS_NM = new string[2];        //루이스 이름(일반)
        static public string[] LUIS_APP_ID = new string[2];    //루이스 app_id(일반대화셋)
        static public string[] LUIS_APINM = new string[2];        //루이스 이름(API)
        static public string[] LUIS_APPAPI_ID = new string[2];    //루이스 app_id(API)
        static public string LUIS_SUBSCRIPTION = "";            //루이스 구독키
        static public int LUIS_TIME_LIMIT;                      //루이스 타임 체크
        static public string BOT_ID = "";                       //bot id
        static public string MicrosoftAppId = "";               //app id
        static public string MicrosoftAppPassword = "";         //app password
        static public string LUIS_SCORE_LIMIT = "";             //루이스 점수 체크
        static public string LUIS_APISCORE_LIMIT = "";             //루이스 API 점수 체크
        static public string LUIS_MINSCORE_LIMIT = "";             //SORRY MESSAGE LUIS 최저 점수

        public static int chatBotID = 0;
        public static DateTime startTime;


        public static String apiFlag = "";
        public static string channelID = "";

        //API변수선언
        static public string apiUrl = "http://www.nhanjinexpress.hanjin.net/ipcc/";                 //API Url(real)
        //static public string apiUrl = "http://211.210.94.46:7777/customer/";                 //API Url(test)
        static public string DeliveryList = apiUrl + "ipcc_api.get_wbls";                 //택배목록
        static public string ReturnDeliveryResult = apiUrl + "ipcc_api.get_rtn_info";                 //반품예약가능여부
        static public string ReturnDeliveryRequest = apiUrl + "ipcc_api.req_rsv_rtn";                 //반품예약요청
        static public string DeliveryCollection = apiUrl + "ipcc_api.get_rsvs";                 //택배집하목록
        static public string bookCheck = apiUrl + "ipcc_api.get_rsv_info";                 //예약확인
        static public string bookCancelYN = apiUrl + "ipcc_api.get_rsv_cancel";                 //예약취소가능여부확인
        static public string bookCancelResult = apiUrl + "ipcc_api.req_rsv_cancel";                 //예약취소요청
        static public string goodLocation = apiUrl + "ipcc_api.get_wbl_info";                 //상품위치확인
        static public string findOrgInfo = apiUrl + "ipcc_api.get_cen_info";                 //집배점정보확인(주소)
        static public string findWayBillNm = apiUrl + "ipcc_api.get_rtn_wbl";                 //반품상품의 운송장확인

        static public string requestAuth = apiUrl + "ipcc_api.get_auth";                 //휴대폰 인증요청
        static public string responseAuth = apiUrl + "ipcc_api.cfm_auth";                 //휴대폰 인증확인

        static public string apiIntent = "None";                 //api 용 intent
        static public string apiOldIntent = "None";                 //api 용 intent(old)
        static public string invoiceNumber = "";                 //운송장 번호
        static public string bookNumber = "";                 //예약 번호
        static public string cancelNumber = "";                 //취소사유
        static public string APIResult = "";                 //api 결과-쏘리메세지 안 나오게 하기 위해서.
        static public string APILuisIntent = null;                 //API 용 루이스 INTENT
        static public string authCheck = "F";                 //인증 체크-리스트 추출용(T/F)
        static public string authNumber = "";                 //인증 번호
        static public string authName = "";                 //인증 이름
        static public string checkAuthNameCnt = "F";                 //주소로서 집배점 찾기 검토
        static public string checkFindAddressCnt = "F";                 //주소로서 집배점 찾기 검토
        static public string mobilePC = "";                 //모바일 PC 확인
        static public string requestPhone = "";                 //리스트용 전화번호
        static public int deliveryListPageNum = 1;
        static public int collectionListPageNum = 1;
        static public int pageCnt = 3;

        static public string authUrl = "";                 //모바일 인증시 적용할 redirect 항목

        HttpWebRequest wReq;
        Stream postDataStream;
        Stream respPostStream;
        StreamReader readerPost;
        HttpWebResponse wResp;
        StringBuilder postParams;


        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {

            DbConnect db = new DbConnect();
            DButil dbutil = new DButil();
            DButil.HistoryLog("db connect !! ");
            //HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            HttpResponseMessage response;

            DButil.HistoryLog("activity.CreateReply() !! ");
            Activity reply1 = activity.CreateReply();
            Activity reply2 = activity.CreateReply();
            Activity reply3 = activity.CreateReply();
            Activity reply4 = activity.CreateReply();

            DButil.HistoryLog("SetActivity!! ");
            // Activity 값 유무 확인하는 익명 메소드
            Action<Activity> SetActivity = (act) =>
            {
                if (!(reply1.Attachments.Count != 0 || reply1.Text != ""))
                {
                    reply1 = act;
                }
                else if (!(reply2.Attachments.Count != 0 || reply2.Text != ""))
                {
                    reply2 = act;
                }
                else if (!(reply3.Attachments.Count != 0 || reply3.Text != ""))
                {
                    reply3 = act;
                }
                else if (!(reply4.Attachments.Count != 0 || reply4.Text != ""))
                {
                    reply4 = act;
                }
                else
                {

                }
            };

            /*
             * USER DATA CHECK
             * */
            List<UserCheck> userCheck = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);
            Debug.WriteLine("*activity.ChannelId : " + activity.ChannelId + "* activity.Conversation.Id : " + activity.Conversation.Id);
            Debug.WriteLine("*userCheck.Count() : " + userCheck.Count());
            if (userCheck.Count() == 0)
            {
                int userDataResult = db.UserCheckDataInsert(activity.ChannelId, activity.Conversation.Id);
            }
            DButil.HistoryLog("userCheck insert end ");

            if (activity.Type == ActivityTypes.ConversationUpdate && activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
            {
                startTime = DateTime.Now;
                //파라메터 호출
                if (LUIS_NM.Count(s => s != null) > 0)
                {
                    Array.Clear(LUIS_NM, 0, LUIS_NM.Length);
                }

                //파라메터 호출
                if (LUIS_APINM.Count(s => s != null) > 0)
                {
                    Array.Clear(LUIS_APINM, 0, LUIS_APINM.Length);
                }

                if (LUIS_APP_ID.Count(s => s != null) > 0)
                {
                    Array.Clear(LUIS_APP_ID, 0, LUIS_APP_ID.Length);
                }

                if (LUIS_APPAPI_ID.Count(s => s != null) > 0)
                {
                    Array.Clear(LUIS_APPAPI_ID, 0, LUIS_APPAPI_ID.Length);
                }

                DButil.HistoryLog("db SelectConfig start !! ");
                List<ConfList> confList = db.SelectConfig();
                DButil.HistoryLog("db SelectConfig end!! ");

                for (int i = 0; i < confList.Count; i++)
                {
                    switch (confList[i].cnfType)
                    {
                        case "LUIS_APP_ID":
                            LUIS_APP_ID[LUIS_APP_ID.Count(s => s != null)] = confList[i].cnfValue;
                            LUIS_NM[LUIS_NM.Count(s => s != null)] = confList[i].cnfNm;
                            break;
                        case "LUIS_APPAPI_ID":
                            LUIS_APPAPI_ID[LUIS_APPAPI_ID.Count(s => s != null)] = confList[i].cnfValue;
                            LUIS_APINM[LUIS_APINM.Count(s => s != null)] = confList[i].cnfNm;
                            break;
                        case "LUIS_SUBSCRIPTION":
                            LUIS_SUBSCRIPTION = confList[i].cnfValue;
                            break;
                        case "BOT_ID":
                            BOT_ID = confList[i].cnfValue;
                            break;
                        case "MicrosoftAppId":
                            MicrosoftAppId = confList[i].cnfValue;
                            break;
                        case "MicrosoftAppPassword":
                            MicrosoftAppPassword = confList[i].cnfValue;
                            break;
                        case "LUIS_SCORE_LIMIT":
                            LUIS_SCORE_LIMIT = confList[i].cnfValue;
                            break;
                        case "LUIS_APISCORE_LIMIT":
                            LUIS_APISCORE_LIMIT = confList[i].cnfValue;
                            break;
                        case "LUIS_TIME_LIMIT":
                            LUIS_TIME_LIMIT = Convert.ToInt32(confList[i].cnfValue);
                            break;
                        default: //미 정의 레코드
                            Debug.WriteLine("*conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            DButil.HistoryLog("*conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            break;
                    }
                }

                //한진 API
                /*
                DButil.HistoryLog("db SelectapiConfig start !! ");
                List<APIConfList> confAPIList = db.SelectAPIConfig();
                DButil.HistoryLog("db SelectConfig end!! ");

                for (int i = 0; i < confAPIList.Count; i++)
                {
                    switch (confAPIList[i].apiName)
                    {
                        case "DELIVERYLIST":
                            DeliveryList = confAPIList[i].apiUrl;
                            break;
                        case "RETURNDELIVERYRESULT":
                            ReturnDeliveryResult = confAPIList[i].apiUrl;
                            break;
                        case "RETURNDELIVERYREQUEST":
                            ReturnDeliveryRequest = confAPIList[i].apiUrl;
                            break;
                        case "DELIVERYCOLLECTION":
                            DeliveryCollection = confAPIList[i].apiUrl;
                            break;
                        case "BOOKCHECK":
                            bookCheck = confAPIList[i].apiUrl;
                            break;
                        case "BOOKCANCELYN":
                            bookCancelYN = confAPIList[i].apiUrl;
                            break;
                        case "BOOKCANCELRESULT":
                            bookCancelResult = confAPIList[i].apiUrl;
                            break;
                        case "GOODLOCATION":
                            goodLocation = confAPIList[i].apiUrl;
                            break;
                        case "FINDORGINFO":
                            findOrgInfo = confAPIList[i].apiUrl;
                            break;
                        case "FINDWAYBILLNM":
                            findWayBillNm = confAPIList[i].apiUrl;
                            break;
                        case "REQUESTAUTH":
                            requestAuth = confAPIList[i].apiUrl;
                            break;
                        case "RESPONSEAUTH":
                            responseAuth = confAPIList[i].apiUrl;
                            break;
                        default: //미 정의 레코드
                            DButil.HistoryLog("*APIconf type : " + confAPIList[i].apiName + "* conf apiUrl : " + confAPIList[i].apiUrl);
                            break;
                    }
                }
                */
                LUIS_MINSCORE_LIMIT = "0.2";

                Debug.WriteLine("* DB conn : " + activity.Type);
                DButil.HistoryLog("* DB conn : " + activity.Type);

                //초기 다이얼로그 호출
                List<DialogList> dlg = db.SelectInitDialog(activity.ChannelId);

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                foreach (DialogList dialogs in dlg)
                {
                    Activity initReply = activity.CreateReply();
                    initReply.Recipient = activity.From;
                    initReply.Type = "message";
                    initReply.Attachments = new List<Attachment>();

                    Attachment tempAttachment;

                    if (dialogs.dlgType.Equals(CARDDLG))
                    {
                        foreach (CardList tempcard in dialogs.dialogCard)
                        {
                            tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity, "INIT");
                            initReply.Attachments.Add(tempAttachment);

                            //2018-11-26:KSO:INIT Carousel 만드는부분 추가
                            if (tempcard.card_order_no > 1)
                            {
                                initReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                            }
                        }
                    }
                    else
                    {
                        tempAttachment = dbutil.getAttachmentFromDialog(dialogs, activity);
                        initReply.Attachments.Add(tempAttachment);
                    }
                    await connector.Conversations.SendToConversationAsync(initReply);
                }

                DateTime endTime = DateTime.Now;
                Debug.WriteLine("프로그램 수행시간 : {0}/ms", ((endTime - startTime).Milliseconds));
                Debug.WriteLine("* activity.Type : " + activity.Type);
                Debug.WriteLine("* activity.Recipient.Id : " + activity.Recipient.Id);
                Debug.WriteLine("* activity.ServiceUrl : " + activity.ServiceUrl);

                DButil.HistoryLog("* activity.Type : " + activity.ChannelData);
                DButil.HistoryLog("* activity.Recipient.Id : " + activity.Recipient.Id);
                DButil.HistoryLog("* activity.ServiceUrl : " + activity.ServiceUrl);

            }
            else if (activity.Type == ActivityTypes.Message && activity.Text.Contains("tel:")) //전화번호 받아오는 부분 처리
            {
                DButil.HistoryLog("* activity.TEL text : " + activity.Text);
                /*
                 * 전화번호 처리
                 * */
                String realTelNumber = "";
                DButil.HistoryLog("start tel : ");
                String telMessage = activity.Text;
                DButil.HistoryLog("telMessage : " + telMessage);
                String mobilePc = "";
                String telNumber = telMessage.Substring(4); //tel:ABDDERFSDVD  tel:ABACHBIFACA
                DButil.HistoryLog("telNumber : " + telNumber);
                int checkTelNumber = telNumber.Length;
                DButil.HistoryLog("checkTelNumber : " + checkTelNumber);
                if (telMessage.Contains("tel:") && checkTelNumber > 5)
                {
                    String[] telNumbers = dbutil.arrayStr(telNumber);
                    DButil.HistoryLog("telNumbers : " + telNumbers.Length);
                    for (int i = 0; i < telNumbers.Length; i++)
                    {
                        realTelNumber = realTelNumber + dbutil.getTelNumber(telNumbers[i]);
                        DButil.HistoryLog("realTelNumber : " + realTelNumber);
                    }
                    mobilePc = "MOBILE";
                    DButil.HistoryLog("realTelNumber : " + realTelNumber);
                    DButil.HistoryLog("CHATBOT TYPE IS MOBILE");
                }
                else
                {
                    mobilePc = "PC";
                    DButil.HistoryLog("CHATBOT TYPE IS PC");
                }


                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "USER_PHONE", realTelNumber);
                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "MOBILEPC", mobilePc);
            }
            else if (activity.Type == ActivityTypes.Message && activity.Text.Contains("consulting:")) //챗봇상담후기
            {
                DButil.HistoryLog("* activity.consulting text : " + activity.Text);
                db.UserReportDataInsert(activity.Text);
            }
            else if (activity.Type == ActivityTypes.Message && !activity.Text.Contains("tel:"))
            {
                DButil.HistoryLog("* activity.TEL text : " + activity.Text);

                /*
                 * MOBILE PC 검토..없으면 무조건 PC로 한다.
                 * */
                List<UserCheck> uDataCheck = new List<UserCheck>();
                uDataCheck = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);
                if (uDataCheck[0].mobilePc == null || uDataCheck[0].mobilePc.Equals(""))
                {
                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "MOBILEPC", "PC");
                }

                List<UserCheck> uData = new List<UserCheck>();
                uData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);
                checkAuthNameCnt = uData[0].nameCheck;
                checkFindAddressCnt = uData[0].addressCheck;

                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                try
                {
                    Debug.WriteLine("* activity.Type == ActivityTypes.Message ");
                    channelID = activity.ChannelId;
                    string orgMent = activity.Text;
                    DButil.HistoryLog("* activity.Text : " + activity.Text);

                    List<RelationList> relationList = new List<RelationList>();
                    string luisId = "";
                    string luisIntent = "";
                    string luisEntities = "";
                    string luisIntentScore = "";
                    string luisTypeEntities = "";
                    string dlgId = "";
                    string replyresult = "";


                    //대화 시작 시간
                    startTime = DateTime.Now;
                    long unixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

                    DButil.HistoryLog("orgMent : " + orgMent);
                    //금칙어 체크
                    CardList bannedMsg = db.BannedChk(orgMent);
                    Debug.WriteLine("* bannedMsg : " + bannedMsg.cardText);//해당금칙어에 대한 답변
                    DButil.HistoryLog("* bannedMsg : " + bannedMsg.cardText);//해당금칙어에 대한 답변

                    //금칙어 처리

                    if (bannedMsg.cardText != null)
                    {
                        Activity reply_ment = activity.CreateReply();
                        reply_ment.Recipient = activity.From;
                        reply_ment.Type = "message";

                        reply_ment.Attachments = new List<Attachment>();

                        List<CardList> text = new List<CardList>();

                        UserHeroCard plCard = new UserHeroCard()
                        {
                            Text = bannedMsg.cardText
                        };

                        Attachment plAttachment = plCard.ToAttachment();
                        reply_ment.Attachments.Add(plAttachment);

                        DateTime endTime = DateTime.Now;
                        relationList = null;

                        int dbResult = db.insertUserQuery(relationList, "", "", "", "", "B", orgMent);

                        db.insertHistory(null, activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), "", "", "", "", "B", orgMent);

                        var reply_ment_info = await connector.Conversations.SendToConversationAsync(reply_ment);
                        response = Request.CreateResponse(HttpStatusCode.OK);
                        return response;
                    }
                    else
                    {

                        string queryStr = "";
                        string luisQuery = "";

                        CacheList cacheList = new CacheList();
                        //정규화
                        luisQuery = orgMent;
                        orgMent = Regex.Replace(orgMent, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);
                        orgMent = orgMent.Replace(" ", "").ToLower();
                        queryStr = orgMent;
                        cacheList = db.CacheChk(orgMent.Replace(" ", ""));                     // 캐시 체크 (TBL_QUERY_ANALYSIS_RESULT 조회..)

                        String checkText = Regex.Replace(activity.Text, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);//공백 및 특수문자 제거
                        int chectTextLength = checkText.Length;

                        if (checkText.Contains("동의") && chectTextLength < 6)
                        {
                            luisIntent = "None";
                        }

                        if (checkAuthNameCnt.Equals("T") || checkFindAddressCnt.Equals("T"))
                        {
                            luisIntent = "None";
                        }

                        //smalltalk 문자 확인  
                        DButil.HistoryLog("smalltalk 체크");
                        String smallTalkSentenceConfirm = db.SmallTalkSentenceConfirm(orgMent);

                        //smalltalk 답변이 있을경우
                        if (!string.IsNullOrEmpty(smallTalkSentenceConfirm))
                        {
                            DButil.HistoryLog("smalltalk 답변이 있을경우");
                            luisId = "";
                            cacheList.luisIntent = "None";//하단의 로직을 수행하지 않기 위해서
                        }
                        else
                        {
                            /*
                             * API LUIS 호출
                             * */
                            List<string[]> apiTextList = new List<string[]>(2);

                            for (int i = 0; i < 2; i++)
                            {
                                apiTextList.Add(new string[] { MessagesController.LUIS_NM[i], MessagesController.LUIS_APPAPI_ID[i], MessagesController.LUIS_SUBSCRIPTION, luisQuery });
                                Debug.WriteLine("GetMultiLUIS() LUIS_APINM : " + MessagesController.LUIS_APINM[i] + " | LUIS_APPAPI_ID : " + MessagesController.LUIS_APPAPI_ID[i]);
                            }
                            DButil.HistoryLog("activity.Conversation.Id : " + activity.Conversation.Id);
                            Debug.WriteLine("activity.Conversation.Id : " + activity.Conversation.Id);

                            float APIluisScoreCompare = 0.0f;
                            JObject APILuis = new JObject();

                            //루이스 처리
                            Task<JObject> t1 = Task<JObject>.Run(async () => await GetIntentFromBotLUIS(apiTextList, luisQuery, "API", activity.ChannelId, activity.Conversation.Id));

                            //결과값 받기
                            await Task.Delay(1000);
                            t1.Wait();
                            APILuis = t1.Result;

                            //entities 갯수가 0일겨우 intent를 None으로 처리

                            if (APILuis.Count != 0)
                            {
                                //if ((int)Luis["entities"].Count() != 0)
                                if (1 != 0)
                                {
                                    float luisScore = (float)APILuis["intents"][0]["score"];
                                    int luisEntityCount = (int)APILuis["entities"].Count();

                                    APILuisIntent = APILuis["topScoringIntent"]["intent"].ToString();//add
                                    luisScore = APIluisScoreCompare;
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                                    Debug.WriteLine("GetMultiLUIS() LUIS APILuisIntent : " + APILuisIntent);
                                }
                            }
                            else
                            {
                                APILuisIntent = "None";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "F");
                            }
                            apiIntent = APILuisIntent;
                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_INTENT", apiIntent);
                            Debug.WriteLine("API INTENT 첫번째 호출(루이스를 통해서)===" + apiIntent);
                        }

                        Regex r = new Regex("[0-9]");
                        bool containNum = r.IsMatch(activity.Text); //숫자포함 확인
                        String apiActiveText = Regex.Replace(activity.Text, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);//공백 및 특수문자 제거
                        bool checkNum = Regex.IsMatch(apiActiveText, @"^\d+$"); //입력값이 숫자인지 파악.
                        String onlyNumber = Regex.Replace(activity.Text, @"\D", "");
                        /*****************************************************************
                            * 문장별로 apiintent 체크하기
                            * 
                            ************************************************************** */
                        if (containNum == true && onlyNumber.Length > 8)
                        {
                            if (apiActiveText.Contains("운송장번호") && apiActiveText.Contains("반품택배예약"))
                            {
                                apiIntent = "F_예약";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("반품택배예약진행") || apiActiveText.Contains("받은주소에서반품") || apiActiveText.Contains("다른주소로변경반품"))
                            {
                                apiIntent = "F_예약";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("예약내용확인") || apiActiveText.Equals("예약번호직접입력"))
                            {
                                apiIntent = "F_예약확인";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("예약취소확인"))
                            {
                                apiIntent = "F_예약취소";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("예약번호") && apiActiveText.Contains("예약취소선택"))
                            {
                                apiIntent = "F_예약취소";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("예약취소진행") && apiActiveText.Contains("계속사용으로반품취소발송취소"))
                            {
                                apiIntent = "F_예약취소";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("예약취소진행") && apiActiveText.Contains("타택배또는다른경로로발송"))
                            {
                                apiIntent = "F_예약취소";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("예약취소진행") && apiActiveText.Contains("이중예약또는기타사유"))
                            {
                                apiIntent = "F_예약취소";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("아니오") && apiActiveText.Contains("예약취소하지않습니다"))
                            {
                                apiIntent = "F_예약취소";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("에대한배송일정조회") && apiActiveText.Contains("운송장번호"))
                            {
                                apiIntent = "F_택배배송일정조회";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else
                            {

                            }
                        }
                        else
                        {
                            if (apiActiveText.Equals("집하예정일확인") || apiActiveText.Equals("예약번호확인") || apiActiveText.Equals("나의예약확인") || apiActiveText.Equals("예약확인"))
                            {
                                apiIntent = "F_예약확인";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("주소로연락처찾기") || apiActiveText.Equals("운송장번호로연락처찾기") || apiActiveText.Equals("집배점지점조회"))
                            {
                                apiIntent = "F_집배점/기사연락처";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("주소다시입력") || apiActiveText.Equals("집배원") || apiActiveText.Equals("기사확인"))
                            {
                                apiIntent = "F_집배점/기사연락처";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("반송장번호확인"))
                            {
                                apiIntent = "F_운송장번호확인";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("예휴대폰인증") || apiActiveText.Contains("아니오휴대폰인증"))
                            {
                                apiIntent = "F_모바일인증";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("반품택배예약다음페이지") || apiActiveText.Contains("반품택배예약이전페이지"))
                            {
                                apiIntent = "F_예약";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("택배는어떻게보내나요") || apiActiveText.Contains("택배예약은어떻게하나"))
                            {
                                apiIntent = "F_예약";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("반품접수부탁드려요") || apiActiveText.Contains("반품예약해주세요") || apiActiveText.Contains("반품예약어떻게하나요"))
                            {
                                apiIntent = "F_예약";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("상품이어디있나요") || apiActiveText.Contains("배송이언제되나요") || apiActiveText.Equals("나의배송목록"))
                            {
                                apiIntent = "F_택배배송일정조회";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("예약목록다음페이지") || apiActiveText.Equals("예약목록이전페이지") || apiActiveText.Equals("접수목록"))
                            {
                                apiIntent = "F_예약확인";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("예약조회해주세요") || apiActiveText.Contains("예약확인해주세요") || apiActiveText.Contains("예약목록"))
                            {
                                apiIntent = "F_예약확인";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("예약취소목록다음페이지") || apiActiveText.Contains("예약취소목록이전페이지"))
                            {
                                apiIntent = "F_예약취소";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Contains("접수한예약을취소하고싶어요") || apiActiveText.Equals("나의예약취소"))
                            {
                                apiIntent = "F_예약취소";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("배송목록다음페이지") || apiActiveText.Contains("배송목록이전페이지"))
                            {
                                apiIntent = "F_택배배송일정조회";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("고객의말씀") || apiActiveText.Contains("온라인상담"))//이건 루이스에도 없는 것. 그냥 예외사항이다.
                            {
                                apiIntent = "F_고객의말씀";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("자주하는질문"))//이건 루이스에도 없는 것. 그냥 예외사항이다.
                            {
                                apiIntent = "F_자주하는질문";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else if (apiActiveText.Equals("위탁배송가능여부"))
                            {
                                apiIntent = "F_위탁배송";
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                            }
                            else
                            {

                            }
                        }
                        Debug.WriteLine("API INTENT 두번째 호출(문장를 통해서)===" + apiIntent);
                        /*
                         * apiintent 값이 없다면 luis 호출을 한다.
                         * */
                        String apiTFdata = "F";

                        /*
                         * smalltalk 처리
                         * smalltalk 에 잡히면..무조건으로...
                         * */
                        String checkSmallIntent1 = db.getIntentFromSmallTalk(orgMent);

                        if (checkSmallIntent1.Equals("") || checkSmallIntent1 == null)
                        {

                        }
                        else
                        {
                            luisIntent = checkSmallIntent1;
                            cacheList.luisIntent = checkSmallIntent1;
                            cacheList.luisEntities = checkSmallIntent1;
                        }


                        if (apiIntent.Equals("None"))
                        {
                            Debug.WriteLine("API INTENT 값이 없으므로 대화셋 검토");
                            /*
                             * 1. CASH DATA 검토
                             * 2. LUIS 검토
                             * */

                            if (cacheList.luisIntent == null || cacheList.luisEntities == null)
                            {
                                DButil.HistoryLog("cache none : " + orgMent);
                                Debug.WriteLine("cache none : " + orgMent);
                                int checkNumberLength = onlyNumber.Length;

                                if (containNum == true && checkNumberLength > 8) //숫자가 포함되어 있으면 대화셋의 데이터는 나오지 않는다. 운송장, 예약번호 등이라 판단
                                {
                                    luisIntent = "None";
                                }
                                else if (checkText.Contains("동의") && chectTextLength < 6)
                                {
                                    luisIntent = "None";
                                }
                                else if (checkAuthNameCnt.Equals("T") || checkFindAddressCnt.Equals("T"))
                                {
                                    luisIntent = "None";
                                }
                                else
                                {
                                    List<string[]> textList = new List<string[]>(2);

                                    for (int i = 0; i < 2; i++)
                                    {
                                        textList.Add(new string[] { MessagesController.LUIS_NM[i], MessagesController.LUIS_APP_ID[i], MessagesController.LUIS_SUBSCRIPTION, luisQuery });
                                        Debug.WriteLine("GetMultiLUIS() LUIS_NM : " + MessagesController.LUIS_NM[i] + " | LUIS_APP_ID : " + MessagesController.LUIS_APP_ID[i]);
                                    }
                                    DButil.HistoryLog("activity.Conversation.Id : " + activity.Conversation.Id);
                                    Debug.WriteLine("activity.Conversation.Id : " + activity.Conversation.Id);

                                    JObject Luis_before = new JObject();
                                    float luisScoreCompare = 0.0f;
                                    JObject Luis = new JObject();

                                    //루이스 처리
                                    Task<JObject> APIt1 = Task<JObject>.Run(async () => await GetIntentFromBotLUIS(textList, luisQuery, "LUIS", activity.ChannelId, activity.Conversation.Id));

                                    //결과값 받기
                                    await Task.Delay(1000);
                                    APIt1.Wait();
                                    Luis = APIt1.Result;

                                    //entities 갯수가 0일겨우 intent를 None으로 처리

                                    if (Luis.Count != 0)
                                    {
                                        if (1 != 0)
                                        {
                                            float luisScore = (float)Luis["intents"][0]["score"];
                                            int luisEntityCount = (int)Luis["entities"].Count();

                                            luisIntent = Luis["topScoringIntent"]["intent"].ToString();//add
                                            luisScore = luisScoreCompare;
                                            //Debug.WriteLine("GetMultiLUIS() LUIS luisIntent : " + luisIntent);
                                        }
                                        apiTFdata = db.getAPITFData(luisIntent);
                                        if (apiTFdata == null)
                                        {
                                            apiTFdata = "F";
                                        }


                                        if (apiTFdata.Equals("F"))
                                        {
                                            apiIntent = "None";
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiIntent);
                                        }

                                        db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "F");
                                    }
                                    else
                                    {
                                        luisIntent = "None";
                                    }
                                }
                            }
                            else
                            {
                                luisId = cacheList.luisId;
                                luisIntent = cacheList.luisIntent;
                                luisEntities = cacheList.luisEntities;
                                luisIntentScore = cacheList.luisScore;

                                if (checkText.Contains("동의") && chectTextLength < 6)
                                {
                                    luisIntent = "None";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                                }
                                else if (checkAuthNameCnt.Equals("T") || checkFindAddressCnt.Equals("T"))
                                {
                                    luisIntent = "None";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");
                                }
                                else
                                {
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "F");
                                }
                                apiTFdata = db.getAPITFData(luisIntent);
                                if (apiTFdata == null)
                                {
                                    apiTFdata = "F";
                                }


                                if (apiTFdata.Equals("F"))
                                {
                                    apiIntent = "None";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiIntent);
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "F");
                                }
                            }


                        }
                        else
                        {
                            //api intent 가 있다면
                            //smalltalk 부분 처리
                            if (checkSmallIntent1.Equals("") || checkSmallIntent1 == null)
                            {
                                luisIntent = "None";
                            }

                        }

                        if (activity.Text.Contains("[") && activity.Text.Contains("]"))
                        {
                            luisIntent = "None";
                        }

                        DButil.HistoryLog("luisIntent : " + luisIntent);

                        //////////////////////////////////////////////

                        string smallTalkConfirm = "";

                        if (luisIntent.Equals("smalltalk") || luisIntent.Equals("SMALLTALK"))
                        {
                            String checkSmallIntent = "";
                            if (orgMent.Length < 11) //smalltalk 10자까지만
                            {
                                /*
                                 * smalltalk intent 가 smalltalk 가 아닐 경우 relationList에 정보를 담는다.
                                 * */
                                checkSmallIntent = db.getIntentFromSmallTalk(orgMent);
                                if (checkSmallIntent.Equals("smalltalk") || checkSmallIntent.Equals("SMALLTALK"))
                                {
                                    smallTalkConfirm = db.SmallTalkConfirm(orgMent);
                                }
                                else
                                {
                                    smallTalkConfirm = "";
                                    relationList = db.DefineTypeChkSpare(checkSmallIntent, luisEntities);
                                }

                            }
                            else
                            {
                                smallTalkConfirm = "";
                            }
                        }
                        else if (!string.IsNullOrEmpty(luisIntent))
                        {
                            relationList = db.DefineTypeChkSpare(luisIntent, luisEntities);

                            if (relationList.Count == 0)
                            {
                                relationList = null;
                            }
                        }
                        else
                        {
                            relationList = null;
                        }

                        //relationList count 를 체크하여 null 처리
                        if (relationList == null)
                        {

                        }
                        else
                        {
                            if (relationList.Count == 0)
                            {
                                relationList = null;
                            }
                        }

                        if (relationList != null)
                        {
                            dlgId = "";
                            mobilePC = uData[0].mobilePc;
                            for (int m = 0; m < relationList.Count; m++)
                            {
                                DialogList dlg = db.SelectDialog(relationList[m].dlgId, mobilePC);
                                dlgId += Convert.ToString(dlg.dlgId) + ",";
                                Activity commonReply = activity.CreateReply();
                                Attachment tempAttachment = new Attachment();
                                DButil.HistoryLog("dlg.dlgType : " + dlg.dlgType);

                                string userSSO = "NONE";

                                if (dlg.dlgType.Equals(CARDDLG))
                                {
                                    foreach (CardList tempcard in dlg.dialogCard)
                                    {
                                        tempAttachment = dbutil.getAttachmentFromDialog(tempcard, activity, userSSO);

                                        if (tempAttachment != null)
                                        {
                                            commonReply.Attachments.Add(tempAttachment);
                                        }

                                        //2018-04-19:KSO:Carousel 만드는부분 추가
                                        if (tempcard.card_order_no > 1)
                                        {
                                            commonReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                                        }
                                    }
                                }
                                else
                                {
                                    //DButil.HistoryLog("* facebook dlg.dlgId : " + dlg.dlgId);
                                    DButil.HistoryLog("* activity.ChannelId : " + activity.ChannelId);

                                    tempAttachment = dbutil.getAttachmentFromDialog(dlg, activity);
                                    commonReply.Attachments.Add(tempAttachment);
                                }

                                if (commonReply.Attachments.Count > 0)
                                {
                                    SetActivity(commonReply);
                                    /*
                                    //NONE_DLG 예외처리
                                    if (luisIntent.Equals("NONE_DLG"))
                                    {
                                        replyresult = "D";
                                    }
                                    else
                                    {
                                        replyresult = "H";
                                    }
                                    */
                                }
                            }
                        }
                        //SMALLTALK 확인
                        else if (!string.IsNullOrEmpty(smallTalkConfirm))
                        {
                            Debug.WriteLine("smalltalk dialogue-------------");

                            Random rand = new Random();

                            //SMALLTALK 구분
                            string[] smallTalkConfirm_result = smallTalkConfirm.Split('$');

                            int smallTalkConfirmNum = rand.Next(0, smallTalkConfirm_result.Length);

                            Activity smallTalkReply = activity.CreateReply();
                            smallTalkReply.Recipient = activity.From;
                            smallTalkReply.Type = "message";
                            smallTalkReply.Attachments = new List<Attachment>();

                            HeroCard plCard = new HeroCard()
                            {
                                Title = "",
                                Text = smallTalkConfirm_result[smallTalkConfirmNum]
                            };

                            Attachment plAttachment = plCard.ToAttachment();
                            smallTalkReply.Attachments.Add(plAttachment);

                            SetActivity(smallTalkReply);
                            replyresult = "S";
                        }
                        else
                        {

                        }

                        /*
                             * [APIINTENT]::글자 - 대화셋에서 버튼을 클릭 했을 시에는 이것으로 진행한다.
                             */
                        if (activity.Text.Contains("[") && activity.Text.Contains("]"))
                        {
                            int apiIntentS = activity.Text.IndexOf("[");
                            int apiIntentE = activity.Text.IndexOf("]");
                            apiIntent = activity.Text.Substring(apiIntentS + 1, (apiIntentE - 1) - apiIntentS);
                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_INTENT", apiIntent);
                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_CHECK", "T");

                            Debug.WriteLine("apiIntent[]-------------" + apiIntent);
                            Debug.WriteLine("대화셋에서 버튼 클릭-------------" + apiIntent);
                        }

                        /*
                         * API 처리부분 INTENT 처리
                         * */
                        if (luisIntent.Equals("None"))
                        {
                            List<UserCheck> apiIntentData = new List<UserCheck>();
                            apiIntentData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);
                            String apiintent = apiIntentData[0].apiIntent;
                            String apiOldIntent = apiIntentData[0].apiOldIntent;
                            if (apiIntent.Equals("None") || apiIntent.Equals(""))
                            {

                            }
                            else
                            {
                                apiOldIntent = "None";
                            }

                            if (apiOldIntent.Equals("None") || apiOldIntent.Equals(""))
                            {

                            }
                            else
                            {
                                apiIntent = apiOldIntent;
                            }

                            if (checkAuthNameCnt.Equals("T"))
                            {
                                apiIntent = "F_모바일인증";
                            }
                            else if (checkFindAddressCnt.Equals("T"))
                            {
                                apiIntent = "F_집배점/기사연락처";
                            }
                            else
                            {
                                //apiIntent = "None";
                            }
                        }
                        else
                        {
                            apiIntent = "None";

                        }
                        /*
                         * 지금 들어온 데이터가 API 와 관련이 있냐 없냐 구분하기.
                         * */
                        List<UserCheck> apiIntentCheckData = new List<UserCheck>();
                        apiIntentCheckData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);
                        String checkApiintent = "";
                        String checkApiTF = apiIntentCheckData[0].apiCheck;
                        if (apiIntentCheckData[0].apiIntent.Equals("None"))
                        {
                            checkApiintent = apiIntentCheckData[0].apiOldIntent;
                        }
                        else
                        {
                            checkApiintent = apiIntentCheckData[0].apiIntent;
                        }

                        //String checkApiintent = apiIntent;
                        if (checkApiintent.Equals("F_모바일인증"))
                        {
                            if (apiActiveText.Contains("예휴대폰인증") || apiActiveText.Contains("아니오휴대폰인증"))
                            {

                            }
                            else if (apiActiveText.Equals("동의") || apiActiveText.Equals("미동의"))
                            {

                            }
                            else if (apiActiveText.Equals("인증확인"))
                            {

                            }
                            else
                            {
                                if (checkApiTF.Equals("F"))
                                {
                                    apiIntent = "None";
                                }

                            }
                        }
                        else if (checkApiintent.Equals("F_예약"))
                        {
                            if (containNum == true && onlyNumber.Length > 8)
                            {
                                if (apiActiveText.Contains("운송장번호") && apiActiveText.Contains("반품택배예약"))
                                {
                                    apiIntent = "F_예약";
                                }
                            }
                            else if (apiActiveText.Contains("반품택배예약") || apiActiveText.Contains("택배배송목록"))
                            {

                            }
                            else if (apiActiveText.Contains("택배예약"))
                            {

                            }
                            else if (apiActiveText.Contains("반품택배예약다음페이지") && activity.Text.Contains(">>"))
                            {

                            }
                            else if (apiActiveText.Contains("반품택배예약이전페이지") && activity.Text.Contains("<<"))
                            {

                            }
                            else
                            {
                                if (checkApiTF.Equals("F"))
                                {
                                    apiIntent = "None";
                                }
                            }
                        }
                        else if (checkApiintent.Equals("F_예약취소"))
                        {
                            if (apiActiveText.Contains("아니오") && apiActiveText.Contains("예약취소"))
                            {

                            }
                            else if (containNum == true && onlyNumber.Length > 8)//예약번호 9자리
                            {
                                if (apiActiveText.Contains("예약취소확인"))
                                {
                                    apiIntent = "F_예약취소";
                                }
                                else if (apiActiveText.Contains("예약번호") && apiActiveText.Contains("예약취소선택"))
                                {
                                    apiIntent = "F_예약취소";
                                }
                                else if (apiActiveText.Contains("예약취소진행") && apiActiveText.Contains("계속사용으로반품취소발송취소"))
                                {
                                    apiIntent = "F_예약취소";
                                }
                                else if (apiActiveText.Contains("예약취소진행") && apiActiveText.Contains("타택배또는다른경로로발송"))
                                {
                                    apiIntent = "F_예약취소";
                                }
                                else if (apiActiveText.Contains("예약취소진행") && apiActiveText.Contains("이중예약또는기타사유"))
                                {
                                    apiIntent = "F_예약취소";
                                }
                                else if (apiActiveText.Contains("아니오") && apiActiveText.Contains("예약취소하지않습니다"))
                                {
                                    apiIntent = "F_예약취소";
                                }
                                else
                                {

                                }
                            }
                            else if (apiActiveText.Contains("예약취소목록다음페이지") && activity.Text.Contains(">>"))
                            {

                            }
                            else if (apiActiveText.Equals("예약취소"))
                            {

                            }
                            else if (apiActiveText.Contains("예약취소목록이전페이지") && activity.Text.Contains("<<"))
                            {

                            }
                            else
                            {
                                if (checkApiTF.Equals("F"))
                                {
                                    apiIntent = "None";
                                }
                            }
                        }
                        else if (checkApiintent.Equals("F_예약확인"))
                        {
                            if (apiActiveText.Equals("예약번호확인"))
                            {

                            }
                            else if (containNum == true && onlyNumber.Length > 8)//예약번호 9자리
                            {
                                if (apiActiveText.Contains("예약내용확인"))
                                {

                                }
                            }
                            else if (apiActiveText.Contains("예약목록다음페이지") && activity.Text.Contains(">>"))
                            {

                            }
                            else if (apiActiveText.Contains("예약목록이전페이지") && activity.Text.Contains("<<"))
                            {

                            }
                            else if (apiActiveText.Equals("예약확인"))
                            {

                            }
                            else
                            {
                                if (checkApiTF.Equals("F"))
                                {
                                    apiIntent = "None";
                                }
                            }
                        }
                        else if (checkApiintent.Equals("F_운송장번호확인"))
                        {
                            if (containNum == true && onlyNumber.Length > 8)
                            {

                            }
                            else if (apiActiveText.Equals("반송장번호확인"))
                            {

                            }
                            else if (apiActiveText.Equals("운송장번호확인"))
                            {

                            }
                            else if (apiActiveText.Contains("반송장번호확인다음페이지") && activity.Text.Contains(">>"))
                            {

                            }
                            else if (apiActiveText.Contains("반송장번호확인이전페이지") && activity.Text.Contains(">>"))
                            {

                            }
                            else
                            {
                                if (checkApiTF.Equals("F"))
                                {
                                    apiIntent = "None";
                                }
                            }
                        }
                        else if (checkApiintent.Equals("F_집배점/기사연락처"))
                        {
                            if (apiActiveText.Contains("운송장번호") && apiActiveText.Contains("연락처찾기"))
                            {

                            }
                            else if (containNum == true && checkFindAddressCnt.Equals("F"))
                            {

                            }
                            else if (apiActiveText.Equals("주소로연락처찾기") || apiActiveText.Equals("운송장번호로연락처찾기"))
                            {

                            }
                            else if (apiActiveText.Equals("주소다시입력"))
                            {

                            }
                            else
                            {
                                if (checkFindAddressCnt.Equals("T"))
                                {

                                }
                                else
                                {
                                    if (checkApiTF.Equals("F"))
                                    {
                                        apiIntent = "None";
                                    }
                                }




                            }
                        }
                        else if (checkApiintent.Equals("F_택배배송일정조회"))
                        {
                            if (apiActiveText.Contains("에대한배송일정조회"))
                            {

                            }
                            else if (containNum == true && onlyNumber.Length > 8)
                            {
                                if (apiActiveText.Contains("에대한배송일정조회") && apiActiveText.Contains("운송장번호"))
                                {

                                }
                                else
                                {

                                }
                            }
                            else if (apiActiveText.Contains("배송목록다음페이지") && activity.Text.Contains(">>"))
                            {

                            }
                            else if (apiActiveText.Contains("배송목록이전페이지") && activity.Text.Contains("<<"))
                            {

                            }
                            else
                            {
                                if (checkApiTF.Equals("F"))
                                {
                                    apiIntent = "None";
                                }
                            }
                        }
                        else if (checkApiintent.Equals("F_위탁배송"))
                        {
                            
                        }
                        else
                        {
                            if (checkApiTF.Equals("F"))
                            {
                                apiIntent = "None";
                            }
                        }

                        Debug.WriteLine("API INTENT 세번째 호출===" + apiIntent);
                        if (relationList == null && apiIntent.Equals("None"))
                        //if (relationList.Count == 0 && apiIntent.Equals("None"))
                        {
                            if (!string.IsNullOrEmpty(smallTalkConfirm))
                            {
                                //smalltalk 답변 있으니, sorry message 는 그만..
                            }
                            else
                            {
                                Debug.WriteLine("no dialogue-------------");

                                Activity intentNoneReply = activity.CreateReply();

                                var message = queryStr;

                                Debug.WriteLine("NO DIALOGUE MESSAGE : " + message);

                                Activity sorryReply = activity.CreateReply();
                                sorryReply.Recipient = activity.From;
                                sorryReply.Type = "message";
                                sorryReply.Attachments = new List<Attachment>();

                                /*
                                 * luis 를 통해서 상위 4개의 답변만 나온다.
                                 * */
                                List<CardAction> cardButtons = new List<CardAction>();
                                List<TextList> textList = new List<TextList>();
                                String sorryIntentDB = db.getSorryIntent(activity.Conversation.Id);

                                if (sorryIntentDB == null || sorryIntentDB.Equals(""))
                                {
                                    textList = db.SelectSorryDialogText("5");
                                    String textListCardText = "";
                                    for (int i = 0; i < textList.Count; i++)
                                    {
                                        textListCardText = textList[i].cardText;
                                    }
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        //Text = "죄송합니다.고객님의 질문을 이해하지 못했어요.<br>\"택배예약은 어떻게 하나요?, 배송조회 해주세요, 반품예약접수 도와줘\"<br>이렇게 질문해 주시면 좋아요!<br>저는 택배와 관련된 질문에 답을 드릴 수 있어요!",
                                        Text = textListCardText,
                                    };
                                    Attachment plAttachment = plCard.ToAttachment();
                                    sorryReply.Attachments.Add(plAttachment);
                                    SetActivity(sorryReply);

                                    DateTime endTime1 = DateTime.Now;
                                    db.insertHistory(null, activity.Conversation.Id, activity.ChannelId, ((endTime1 - MessagesController.startTime).Milliseconds), "", "", "", "", "D", queryStr);
                                }
                                else
                                {
                                    MatchCollection matches = Regex.Matches(sorryIntentDB, "%");
                                    int cnt = matches.Count;

                                    if (cnt > 0)
                                    {
                                        textList = db.SelectSorryDialogText("6");
                                        String textListCardText = "";
                                        for (int i = 0; i < textList.Count; i++)
                                        {
                                            textListCardText = textList[i].cardText;
                                        }
                                        sorryIntentDB = sorryIntentDB.Remove(sorryIntentDB.Length - 1);
                                        if (sorryIntentDB.Contains("%"))
                                        {
                                            String[] sorryIntentArray;
                                            sorryIntentArray = sorryIntentDB.Split('%');
                                            for (int jj = 0; jj < sorryIntentArray.Length; jj++)
                                            {
                                                String query = db.getLuisMINData(sorryIntentArray[jj]);
                                                CardAction sorryButton = new CardAction();
                                                sorryButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = query,
                                                    Title = query
                                                };
                                                cardButtons.Add(sorryButton);
                                            }

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "문의하시려는 항목을 아래에서 선택해 주세요.<br>원하시는 항목이 없으면 이렇게 입력해 보세요.<br><br>예)택배예약은 어떻게 하나요?, 배송조회 해 주세요, 반품예약접수 도와줘",
                                                Text = textListCardText,
                                                Buttons = cardButtons
                                            };
                                            Attachment plAttachment = plCard.ToAttachment();
                                            sorryReply.Attachments.Add(plAttachment);
                                            SetActivity(sorryReply);
                                        }
                                        else
                                        {
                                            String query = db.getLuisMINData(sorryIntentDB);
                                            CardAction sorryButton = new CardAction();
                                            sorryButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = query,
                                                Title = query
                                            };
                                            cardButtons.Add(sorryButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "문의하시려는 항목을 아래에서 선택해 주세요.<br>원하시는 항목이 없으면 이렇게 입력해 보세요.<br><br>예)택배예약은 어떻게 하나요?, 배송조회 해 주세요, 반품예약접수 도와줘",
                                                Text = textListCardText,
                                                Buttons = cardButtons
                                            };
                                            Attachment plAttachment = plCard.ToAttachment();
                                            sorryReply.Attachments.Add(plAttachment);
                                            SetActivity(sorryReply);
                                        }
                                    }
                                    else
                                    {
                                        textList = db.SelectSorryDialogText("5");
                                        String textListCardText = "";
                                        for (int i = 0; i < textList.Count; i++)
                                        {
                                            textListCardText = textList[i].cardText;
                                        }
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            //Text = "죄송합니다.고객님의 질문을 이해하지 못했어요.<br>\"택배예약은 어떻게 하나요?, 배송조회 해주세요, 반품예약접수 도와줘\"<br>이렇게 질문해 주시면 좋아요!<br>저는 택배와 관련된 질문에 답을 드릴 수 있어요!",
                                            Text = textListCardText,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        sorryReply.Attachments.Add(plAttachment);
                                        SetActivity(sorryReply);
                                    }

                                    replyresult = "D";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "SORRY_INTENT", "");
                                }




                                /*
                                Debug.WriteLine("no dialogue-------------");

                                Activity intentNoneReply = activity.CreateReply();

                                var message = queryStr;

                                Debug.WriteLine("NO DIALOGUE MESSAGE : " + message);

                                Activity sorryReply = activity.CreateReply();
                                sorryReply.Recipient = activity.From;
                                sorryReply.Type = "message";
                                sorryReply.Attachments = new List<Attachment>();
                                //sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                                List<CardList> text = new List<CardList>();
                                List<CardAction> cardButtons = new List<CardAction>();

                                text = db.SelectSorryDialogText("5");
                                for (int i = 0; i < text.Count; i++)
                                {
                                    CardAction plButton = new CardAction();
                                    plButton = new CardAction()
                                    {
                                        Type = text[i].btn1Type,
                                        Value = text[i].btn1Context,
                                        Title = text[i].btn1Title
                                    };
                                    cardButtons.Add(plButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = text[i].cardTitle,
                                        Text = text[i].cardText,
                                        //Buttons = cardButtons
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    sorryReply.Attachments.Add(plAttachment);
                                }
                                
                                SetActivity(sorryReply);
                                replyresult = "D";
                                */
                            }

                        }
                        else
                        {
                            //API 관련 처리하기
                            /*
                    * API 연동부분은 다 이곳에서 처리
                    * 대화셋 APP 는 그대로 진행. API APP도 따로 진행
                    */
                            Debug.WriteLine("API INTENT 마지막 호출===" + apiIntent);
                            Debug.WriteLine("luisIntent1-------------" + luisIntent);
                            Debug.WriteLine("apiOldIntent-------------" + apiOldIntent);

                            DButil.HistoryLog("luisIntent1-------------" + luisIntent);
                            DButil.HistoryLog("apiOldIntent-------------" + apiOldIntent);

                            Activity apiMakerReply = activity.CreateReply();

                            apiMakerReply.Recipient = activity.From;
                            apiMakerReply.Type = "message";
                            apiMakerReply.Attachments = new List<Attachment>();

                            authCheck = uData[0].authCheck;//모바일 인증 체크
                            mobilePC = uData[0].mobilePc;//모바일인지 PC 인지 구분
                            requestPhone = uData[0].userPhone; //전화번호

                            authName = uData[0].userName;//모바일 인증 체크(이름)
                            authNumber = uData[0].authNumber;//모바일 인증 체크(인증번호)

                            //mobilePC = "MOBILE";//TEST 용 반드시 지울 것!!!!(에뮬레이터용)
                            //mobilePC = "PC";//TEST 용 반드시 지울 것!!!!(에뮬레이터용)
                            //requestPhone = "01022840610";//TEST 용 반드시 지울 것!!!!김은영대리
                            //requestPhone = "01075013741";//TEST 용 반드시 지울 것!!!!이채원강사
                            //db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "USER_PHONE", requestPhone);//TEST 용 반드시 지울 것!!!!
                            //db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "MOBILEPC", mobilePC);//TEST 용 반드시 지울 것!!!!
                            /*****************************************************************
                            * apiIntent F_예약
                            * 
                            ************************************************************** */
                            if (apiIntent.Equals("F_예약"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                //개인택배예약
                                if (apiActiveText.Contains("개인택배예약"))
                                {
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction bookButton = new CardAction();
                                    bookButton = new CardAction()
                                    {
                                        Type = "openUrl",
                                        Value = "https://m.hanex.hanjin.co.kr/inquiry/outcoming/reservationMain",
                                        Title = "예약접수"
                                    };
                                    cardButtons.Add(bookButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "네~ 고객님<br>예약접수 시 지정하신 날짜 혹은 접수 다음 날(접수당일, 공휴일 제외)부터 방문가능하며 일부 상품은 택배 이용에 제한이 있을 수 있으니 참고하시기 바랍니다.<br><br>택배예약을 원하시는 경우 아래 버튼을 눌러주세요.",
                                        Buttons = cardButtons,
                                    };

                                    invoiceNumber = onlyNumber;
                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("운송장번호직접입력"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "반품을 원하시는 운송장번호를 입력해 주세요.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("다른주소로변경반품"))
                                {
                                    //모바일 인증
                                    if (authCheck.Equals("F"))
                                    {
                                        db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_URL", "[F_예약]::다른주소로변경반품");
                                        db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_예약");
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction deliveryButton = new CardAction();
                                        deliveryButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "예. 휴대폰인증 하겠습니다",
                                            Title = "휴대폰 인증"
                                        };
                                        cardButtons.Add(deliveryButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "반품하시려는 주소가 받은 주소와 다를 경우 모바일앱에서 진행됩니다.<br>모바일인증을 하신 후에 모바일앱에서 반품접수를 진행해 주세요.",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        String userPhone = userCheck[0].userPhone;
                                        String dataUserPhone = "";
                                        String[] telNumbers = dbutil.arrayStr(userPhone);
                                        for (int i = 0; i < telNumbers.Length; i++)
                                        {
                                            dataUserPhone = dataUserPhone + dbutil.getTelEnglish(telNumbers[i]);
                                        }

                                        List<CardAction> addressButtons = new List<CardAction>();
                                        CardAction addressButton = new CardAction();
                                        addressButton = new CardAction()
                                        {
                                            Type = "openUrl",
                                            Value = "https://m.hanex.hanjin.co.kr/member/ssoCTI?tel_num=" + dataUserPhone + "&auth_num=" + authNumber + "&type=A&wbl=" + onlyNumber,
                                            Title = "반품접수"
                                        };
                                        addressButtons.Add(addressButton);

                                        UserHeroCard addressPlCard3 = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "모바일앱에서 반품접수를 진행해 주세요.",
                                            Buttons = addressButtons,
                                        };
                                        Attachment plAttachment = addressPlCard3.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                }
                                //반품택배예약가능여부 확인
                                else if (containNum == true) //반품택배예약 중에서 숫자만 추출한다.
                                {
                                    if (apiActiveText.Contains("반품택배예약진행") || apiActiveText.Contains("받은주소에서반품"))//반품택배예약 진행
                                    {
                                        Debug.WriteLine("onlynumber===========================================" + onlyNumber);
                                        postParams = new StringBuilder();
                                        postParams.Append("gbn_cod=CHATBOT");
                                        postParams.Append("&wbl_num=" + onlyNumber);
                                        postParams.Append("&tel_num=" + requestPhone);
                                        Encoding encoding = Encoding.UTF8;
                                        byte[] result = encoding.GetBytes(postParams.ToString());

                                        wReq = (HttpWebRequest)WebRequest.Create(ReturnDeliveryRequest);
                                        wReq.Method = "POST";
                                        wReq.ContentType = "application/x-www-form-urlencoded";
                                        wReq.ContentLength = result.Length;

                                        postDataStream = wReq.GetRequestStream();
                                        postDataStream.Write(result, 0, result.Length);
                                        postDataStream.Close();

                                        wResp = (HttpWebResponse)wReq.GetResponse();
                                        respPostStream = wResp.GetResponseStream();
                                        readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                        String ReturnDeliveryRequestJsonData = readerPost.ReadToEnd();
                                        Debug.WriteLine("반품택배예약진행===========================================" + ReturnDeliveryRequestJsonData);

                                        JArray obj = JArray.Parse(ReturnDeliveryRequestJsonData);
                                        String requestText = "";
                                        String returnCode = "";

                                        foreach (JObject jobj in obj)
                                        {
                                            if (jobj["ret_cod"].ToString().Equals("9006"))
                                            {
                                                requestText = "고객님께서 입력 하신 운송장번호 <strong><font color='#0101DF'>(" + onlyNumber + ")</font></strong>는 이미 반품택배예약이 완료되어있습니다.<br>반품택배 예약번호는 <strong><font color='#0101DF'>(" + jobj["rsv_num"].ToString() + ")</font></strong>이며, 방문일정 확인은 " + jobj["org_nam"].ToString() + " 집배점 ☎ <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a> 번(으)로 문의 부탁드립니다.";
                                                returnCode = "9006";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("1000"))
                                            {
                                                requestText = "네~ 고객님<br>상품을 받으셨던 주소지로 반품택배예약접수가 완료되었습니다.<br>예약접수 번호는 <strong><font color='#0101DF'>(" + jobj["rsv_num"].ToString() + ")</font></strong> 입니다.<br>방문 예정일은 접수 다음날부터 주말, 공휴일을 제외한 3일 이내에 순차적으로 방문예정입니다.<br>※ 반품, 교환은 따로 구분하지 않고 반품으로 접수되며, 포장은 받아보신 그대로 재포장해주시기 바랍니다.";
                                                returnCode = "1000";
                                            }
                                            else
                                            {
                                                requestText = "운송장번호 <strong><font color='#0101DF'>(" + onlyNumber + ")</font></strong>  상품은 반품하실 업체를 통해 반품접수를 하시거나 개인택배예약으로 접수가 가능합니다. 개인택배예약 접수를 원하시면 아래 버튼을 눌러주십시오.";
                                                returnCode = "1111";
                                            }
                                        }

                                        if (returnCode.Equals("9006") || returnCode.Equals("1000"))
                                        {
                                            UserHeroCard plCardOk = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = requestText,
                                            };
                                            Attachment plAttachmentOk = plCardOk.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachmentOk);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            List<CardAction> cardButtons = new List<CardAction>();
                                            CardAction priButton = new CardAction();
                                            priButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "개인택배예약",
                                                Title = "개인택배예약"
                                            };
                                            cardButtons.Add(priButton);

                                            UserHeroCard plCardNo = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = requestText,
                                                Buttons = cardButtons,
                                            };
                                            Attachment plAttachmentNo = plCardNo.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachmentNo);
                                            SetActivity(apiMakerReply);
                                        }

                                    }
                                    else//반품가능예약여부확인
                                    {
                                        postParams = new StringBuilder();
                                        postParams.Append("wbl_num=" + onlyNumber);
                                        Encoding encoding = Encoding.UTF8;
                                        byte[] result = encoding.GetBytes(postParams.ToString());

                                        wReq = (HttpWebRequest)WebRequest.Create(ReturnDeliveryResult);
                                        wReq.Method = "POST";
                                        wReq.ContentType = "application/x-www-form-urlencoded";
                                        wReq.ContentLength = result.Length;

                                        postDataStream = wReq.GetRequestStream();
                                        postDataStream.Write(result, 0, result.Length);
                                        postDataStream.Close();

                                        wResp = (HttpWebResponse)wReq.GetResponse();
                                        respPostStream = wResp.GetResponseStream();
                                        readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                        String ReturnDeliveryResultJsonData = readerPost.ReadToEnd();
                                        Debug.WriteLine("반품가능예약여부확인===========================================" + ReturnDeliveryResultJsonData);
                                        /************************************************/
                                        /*
                                        WebClient webClient = new WebClient();

                                        String sample = ReturnDeliveryResult + "?wbl_num=" + onlyNumber;
                                        Stream stream = webClient.OpenRead(sample);
                                        String ReturnDeliveryResultJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                        */
                                        JArray obj = JArray.Parse(ReturnDeliveryResultJsonData);

                                        List<CardList> text = new List<CardList>();
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        String returnYN = "";//반품가능여부
                                        String returnText = "";

                                        foreach (JObject jobj in obj)
                                        {
                                            if (jobj["ret_cod"].ToString().Equals("9001"))
                                            {
                                                returnYN = "no";
                                                returnText = "운송장 미등록";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("9002"))
                                            {
                                                returnYN = "no";
                                                returnText = "배송출발전 모든상태(배송출발부터 반품가능)";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("9003"))
                                            {
                                                returnYN = "no";
                                                returnText = "신용번호 미존재";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("9004"))
                                            {
                                                returnYN = "no";
                                                returnText = "EDI화주/멀티화주/반품계약 미 적용화주";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("9005"))
                                            {
                                                returnYN = "no";
                                                returnText = "주소 오입력";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("9006"))
                                            {
                                                returnYN = "no";
                                                returnText = "이중예약";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("9007"))
                                            {
                                                returnYN = "no";
                                                returnText = "집하집배점 할당실패";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("9999"))
                                            {
                                                returnYN = "no";
                                                returnText = "기타에러";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("1000"))
                                            {
                                                returnYN = "yes";
                                                returnText = "반품가능";
                                            }
                                            else
                                            {
                                                returnYN = "no";
                                                returnText = "기타에러";
                                            }
                                        }

                                        if (returnYN.Equals("yes"))
                                        {
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "TEMP_NUMBER", onlyNumber);
                                            CardAction yesButton = new CardAction();
                                            yesButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "운송장 번호 " + onlyNumber + " 받은 주소에서 반품",
                                                Title = "받은 주소에서 반품"
                                            };
                                            cardButtons.Add(yesButton);

                                            CardAction yes1Button = new CardAction();
                                            yes1Button = new CardAction()
                                            {
                                                //Type = "openUrl",
                                                //Value = "https://m.hanex.hanjin.co.kr/inquiry/outcoming/reservationReturn",
                                                Type = "imBack",
                                                Value = "운송장 번호 " + onlyNumber + " 다른 주소로 변경 반품",
                                                Title = "다른 주소로 변경 반품"
                                            };
                                            cardButtons.Add(yes1Button);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "운송장 번호 " + onlyNumber + "번의 반품택배 방문 주소지를 선택해 주세요.",
                                                Buttons = cardButtons,
                                            };

                                            invoiceNumber = onlyNumber;
                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                        }
                                        else
                                        {
                                            CardAction yesButton = new CardAction();
                                            yesButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "개인택배예약",
                                                Title = "개인택배예약"
                                            };
                                            cardButtons.Add(yesButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "운송장번호 <strong><font color='#0101DF'>(" + onlyNumber + ")</font></strong> 상품은 반품하실 업체를 통해 반품접수를 하시거나 개인택배예약으로 접수가 가능합니다.<br>개인택배예약 접수를 원하시면 아래 버튼을 눌러주십시오.",
                                                Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            invoiceNumber = null;
                                        }

                                        SetActivity(apiMakerReply);
                                    }

                                }

                                else if (apiActiveText.Contains("반품택배예약") || apiActiveText.Contains("택배배송목록"))
                                {
                                    if (mobilePC.Equals("PC"))
                                    {
                                        //no show
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "postBack",
                                            Value = "[F_예약]::운송장번호직접입력",
                                            Title = "운송장번호 직접입력"
                                        };
                                        cardButtons.Add(returnButton);

                                        CardAction extButton = new CardAction();
                                        extButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "반품택배예약 이용안내",
                                            Title = "반품맞교환 택배예약 이용안내"
                                        };
                                        cardButtons.Add(extButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "네~ 고객님<br>반품, 맞교환 택배 예약 접수는 구매하신 업체를 통해서 신청하시거나 당사를 통해서도 접수 가능합니다.<br><br>운송장번호를 입력해 주십시오.",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        //모바일 인증 체크
                                        if (authCheck.Equals("F"))
                                        {
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_URL", "[F_예약]::택배배송목록");
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_예약");
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction deliveryButton = new CardAction();
                                            deliveryButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "예. 휴대폰인증 하겠습니다",
                                                Title = "휴대폰 인증"
                                            };
                                            cardButtons.Add(deliveryButton);

                                            CardAction returnButton = new CardAction();
                                            returnButton = new CardAction()
                                            {
                                                Type = "postBack",
                                                Value = "[F_예약]::운송장번호직접입력",
                                                Title = "운송장번호 직접입력"
                                            };
                                            cardButtons.Add(returnButton);

                                            CardAction extButton = new CardAction();
                                            extButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "반품택배예약 이용안내",
                                                Title = "반품맞교환 택배예약 이용안내"
                                            };
                                            cardButtons.Add(extButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "네~ 고객님<br>반품, 맞교환 택배 예약 접수는 구매하신 업체를 통해서 신청하시거나 당사를 통해서도 접수 가능합니다.<br><br>고객님께서 받으셨던 택배목록에서 반품택배예약을 하시려면 휴대폰 인증이 필요합니다. <br>휴대폰 인증을 하시겠습니까?<br><br>또는 운송장번호를 입력해 주십시오.",
                                                Buttons = cardButtons,
                                            };
                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            int totalPage = 0;
                                            if (apiActiveText.Contains("반품택배예약다음페이지") && activity.Text.Contains(">>"))
                                            {
                                                deliveryListPageNum++;
                                            }
                                            else if (apiActiveText.Contains("반품택배예약이전페이지") && activity.Text.Contains("<<"))
                                            {
                                                deliveryListPageNum--;
                                            }
                                            else
                                            {
                                                deliveryListPageNum = 1;
                                            }

                                            List<CardAction> startCardButtons = new List<CardAction>();
                                            CardAction startPlButton = new CardAction();
                                            startPlButton = new CardAction()
                                            {
                                                Type = "postBack",
                                                Value = "[F_예약]::운송장번호직접입력",
                                                Title = "운송장번호 직접입력"
                                            };
                                            startCardButtons.Add(startPlButton);

                                            UserHeroCard startCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "반품택배예약을 원하시는 상품을 목록에서 선택해주세요.<br>해당 상품이 목록에 없는 경우, 운송장번호를 직접 입력해 주세요.",
                                                Buttons = startCardButtons,
                                            };

                                            Attachment plAttachment = startCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            /*
                                             * POST METHOD
                                             * */
                                            postParams = new StringBuilder();
                                            postParams.Append("auth_num=" + authNumber);
                                            postParams.Append("&pag_num=" + deliveryListPageNum);
                                            postParams.Append("&pag_cnt=" + pageCnt);

                                            Encoding encoding = Encoding.UTF8;
                                            byte[] result = encoding.GetBytes(postParams.ToString());

                                            wReq = (HttpWebRequest)WebRequest.Create(DeliveryList);
                                            wReq.Method = "POST";
                                            wReq.ContentType = "application/x-www-form-urlencoded";
                                            wReq.ContentLength = result.Length;

                                            postDataStream = wReq.GetRequestStream();
                                            postDataStream.Write(result, 0, result.Length);
                                            postDataStream.Close();

                                            wResp = (HttpWebResponse)wReq.GetResponse();
                                            respPostStream = wResp.GetResponseStream();
                                            readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                            String DeliveryListJsonData = readerPost.ReadToEnd();
                                            Debug.WriteLine("반품택배예약목록===========================================" + DeliveryListJsonData);
                                            JArray obj = JArray.Parse(DeliveryListJsonData);

                                            foreach (JObject jobj in obj)
                                            {
                                                if (jobj["ret_cod"].ToString().Equals("9041"))
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "인증번호에 오류가 발생되었습니다."
                                                    };
                                                    plAttachment = plCard.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment);
                                                }
                                                else if (jobj["ret_cod"].ToString().Equals("9042"))
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                    };
                                                    plAttachment = plCard.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment);
                                                }
                                                else if (jobj["ret_cod"].ToString().Equals("9999"))
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "시스템 오류 또는 요청항목이 누락되었습니다."
                                                    };
                                                    plAttachment = plCard.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment);
                                                }
                                                else
                                                {
                                                    List<CardAction> cardButtons = new List<CardAction>();

                                                    totalPage = Convert.ToInt32(jobj["tot_pag"].ToString());
                                                    deliveryListPageNum = Convert.ToInt32(jobj["pag_num"].ToString());

                                                    String tempDate = jobj["dlv_ymd"].ToString();
                                                    String dateText = "";
                                                    if (tempDate == "" || tempDate.Equals(""))
                                                    {
                                                        //dateText = "<div class=\"endDate\"><span class=\"dateDay\"><small>배송중</small></span><span class=\"dateWeek\"></span></div>";
                                                        dateText = "";
                                                    }
                                                    else
                                                    {
                                                        String yearText = tempDate.Substring(0, 4);
                                                        String monthText = tempDate.Substring(4, 2);
                                                        String dayText = tempDate.Substring(6, 2);
                                                        dateText = "<strong>· 배송완료일자 : </strong > " + yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["dlv_dy"].ToString() + ")<br>";
                                                        //dateText = "<div class=\"endDate\"><span class=\"dateDay\">" + monthText + "." + dayText + "</span><span class=\"dateWeek\">" + jobj["dlv_dy"].ToString() + "요일</span></div>";
                                                    }
                                                    //배송상태 처리
                                                    String deliveryStatus = jobj["wrk_nam"].ToString();
                                                    String deliveryStatusText = "상품접수";
                                                    if (deliveryStatus.Equals("10"))
                                                    {
                                                        deliveryStatusText = "상품접수";
                                                    }
                                                    else if (deliveryStatus.Equals("20"))
                                                    {
                                                        deliveryStatusText = "상품발송대기중";
                                                    }
                                                    else if (deliveryStatus.Equals("30"))
                                                    {
                                                        deliveryStatusText = "이동중";
                                                    }
                                                    else if (deliveryStatus.Equals("40"))
                                                    {
                                                        deliveryStatusText = "배송준비";
                                                    }
                                                    else if (deliveryStatus.Equals("50"))
                                                    {
                                                        deliveryStatusText = "배송중";
                                                    }
                                                    else
                                                    {
                                                        deliveryStatusText = "배송완료";
                                                    }

                                                    String goodNameTemp = jobj["god_nam"].ToString();
                                                    int goodNameLength = jobj["god_nam"].ToString().Length;
                                                    String goodName = "";
                                                    if (goodNameLength > 20)
                                                    {
                                                        goodName = goodNameTemp.Substring(0, 15) + "....";
                                                    }
                                                    else
                                                    {
                                                        goodName = goodNameTemp;
                                                    }


                                                    CardAction plButton = new CardAction();
                                                    plButton = new CardAction()
                                                    {
                                                        Type = "imBack",
                                                        Value = "운송장번호 " + jobj["wbl_num"].ToString() + " 반품택배예약",
                                                        Title = "반품택배예약",
                                                    };
                                                    cardButtons.Add(plButton);

                                                    UserHeroCard plCard = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = dateText + "<strong>· 배송상태 : <font color='#0101DF'>" + deliveryStatusText + "</font></strong><br><strong>· 상품명 : </strong><font color='#585858'>" + goodName + "</font><br><strong>· 운송장번호 : <font color='#0101DF'>(" + jobj["wbl_num"].ToString() + ")</font></strong><br><strong>· 송하인명 : </strong>" + jobj["snd_nam"].ToString(),
                                                        //Text = "<div class=\"takeBack\">" + dateText + "<div class=\"prodInfo\"><span class=\"prodName\">" + goodName + "</span><span class=\"prodNum\">" + jobj["wbl_num"].ToString() + "/" + jobj["snd_nam"].ToString() + "</span><span class=\"prodStatus\">" + deliveryStatusText + "</span></div></div>",
                                                        Tap = plButton
                                                    };
                                                    plAttachment = plCard.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment);
                                                }

                                            }
                                            if (totalPage == 1)
                                            {
                                                //페이징 없음
                                            }
                                            else
                                            {
                                                List<CardAction> pageButtons = new List<CardAction>();
                                                //전체페이지와 동일하면 다음버튼은 없다
                                                if (deliveryListPageNum == totalPage)
                                                {

                                                }
                                                else
                                                {
                                                    CardAction nextButton = new CardAction();
                                                    nextButton = new CardAction()
                                                    {
                                                        Type = "imBack",
                                                        Value = "반품택배예약 다음페이지>>",
                                                        Title = "다음페이지",
                                                    };
                                                    pageButtons.Add(nextButton);
                                                }
                                                //현재 페이지가 1이면 이전버튼은 없다.
                                                if (deliveryListPageNum < 2)
                                                {

                                                }
                                                else
                                                {
                                                    CardAction prevButton = new CardAction();
                                                    prevButton = new CardAction()
                                                    {
                                                        Type = "imBack",
                                                        Value = "반품택배예약 이전페이지<<",
                                                        Title = "이전페이지",
                                                    };
                                                    pageButtons.Add(prevButton);
                                                }


                                                UserHeroCard pageCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "",
                                                    Buttons = pageButtons,
                                                };
                                                plAttachment = pageCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                            SetActivity(apiMakerReply);
                                        }
                                    }

                                }
                                //예약초기화면
                                else
                                {
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction deliveryButton = new CardAction();
                                    deliveryButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "개인택배예약",
                                        Title = "개인택배예약"
                                    };
                                    cardButtons.Add(deliveryButton);

                                    CardAction returnButton = new CardAction();
                                    returnButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "반품택배예약",
                                        Title = "반품택배예약"
                                    };
                                    cardButtons.Add(returnButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "네~ 고객님<br>문의하시려는 항목을 아래에서 선택해<br>주세요.<br>원하시는 항목이 없으면, 이렇게 입력해<br>보세요.<br><br>예) 택배예약, 배송조회, 택배요금",
                                        Buttons = cardButtons,
                                    };
                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }

                            }
                            else
                            {

                            }

                            /*****************************************************************
                                 * apiIntent F_예약확인
                                 * 택배집하목록(송하인전화번호)
                                 ************************************************************** */
                            if (apiIntent.Equals("F_예약확인"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Equals("집하예정일확인"))
                                {

                                }
                                else if (apiActiveText.Contains("운송장예약번호직접입력"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "예약확인을 원하시는 예약번호 또는 운송장번호를 직접 입력해주세요. ",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("예약번호직접입력"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "예약확인을 원하시는 예약번호를 직접 입력해주세요. ",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("예약내용확인") || (containNum == true && onlyNumber.Length > 8))//리스트버튼 클릭이거나 직접 입력일 경우
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    /*
                                    * POST METHOD
                                    * */
                                    postParams = new StringBuilder();
                                    postParams.Append("wbl_rsv=" + bookNumber);

                                    Encoding encoding = Encoding.UTF8;
                                    byte[] result = encoding.GetBytes(postParams.ToString());

                                    wReq = (HttpWebRequest)WebRequest.Create(bookCheck);
                                    wReq.Method = "POST";
                                    wReq.ContentType = "application/x-www-form-urlencoded";
                                    wReq.ContentLength = result.Length;

                                    postDataStream = wReq.GetRequestStream();
                                    postDataStream.Write(result, 0, result.Length);
                                    postDataStream.Close();

                                    wResp = (HttpWebResponse)wReq.GetResponse();
                                    respPostStream = wResp.GetResponseStream();
                                    readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                    String bookCheckJsonData = readerPost.ReadToEnd();
                                    Debug.WriteLine("bookCheckJsonData====" + bookCheckJsonData);

                                    JArray obj = JArray.Parse(bookCheckJsonData);

                                    foreach (JObject jobj in obj)
                                    {
                                        String bookCheckText = "";

                                        if (jobj["ret_cod"].ToString().Equals("1000"))
                                        {
                                            String tempDate = jobj["acp_ymd"].ToString();
                                            String yearText = tempDate.Substring(0, 4);
                                            String monthText = tempDate.Substring(4, 2);
                                            String dayText = tempDate.Substring(6, 2);
                                            String dateText = yearText + "년 " + monthText + "월 " + dayText + "일";

                                            if (jobj["wrk_cod"].ToString().Equals("03"))
                                            {
                                                String tempDate1 = jobj["wrk_ymd"].ToString();
                                                String yearText1 = tempDate1.Substring(0, 4);
                                                String monthText1 = tempDate1.Substring(4, 2);
                                                String dayText1 = tempDate1.Substring(6, 2);
                                                String dateText1 = yearText1 + "년 " + monthText1 + "월 " + dayText1 + "일";
                                                bookCheckText = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>는 " + dateText1 + " 예약 취소 되었습니다.<br>예약문의 사항은 " + jobj["org_nam"].ToString() + " 집배점 ☎ <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a> 으로 문의 부탁드립니다.";
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("11"))
                                            {
                                                String tempDate1 = jobj["wrk_ymd"].ToString();
                                                String yearText1 = tempDate1.Substring(0, 4);
                                                String monthText1 = tempDate1.Substring(4, 2);
                                                String dayText1 = tempDate1.Substring(6, 2);
                                                String dateText1 = yearText1 + "년 " + monthText1 + "월 " + dayText1 + "일";
                                                bookCheckText = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>는 " + dateText1 + " 정상 집하 완료된 건입니다.<br>해당 운송장번호는 <strong>" + jobj["wbl_num"].ToString() + "</strong> 입니다.";
                                            }
                                            else
                                            {
                                                bookCheckText = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>는 " + dateText + " 정상 예약접수된 건입니다.<br>방문일정 또는 예약변경 사항, 문의사항은 " + jobj["org_nam"].ToString() + " 집배점 ☎ <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a> 으로 문의 부탁드립니다.";
                                            }
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9011"))
                                        {
                                            bookCheckText = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>는 예약미등록 건입니다.";
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9999"))
                                        {
                                            bookCheckText = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>는 에러발생 건입니다.";
                                        }
                                        else
                                        {

                                        }
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = bookCheckText,
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                }
                                //예약확인 초기화면
                                else
                                {
                                    if (mobilePC.Equals("PC"))
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "postBack",
                                            Value = "[F_예약확인]::예약번호직접입력",
                                            Title = "예약번호 직접입력"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "네~ 고객님<br>예약번호를 입력해 주십시오",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        //모바일 인증 체크
                                        if (authCheck.Equals("F"))
                                        {
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_URL", "[F_예약확인]::나의예약확인");
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_예약확인");
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction deliveryButton = new CardAction();
                                            deliveryButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "예. 휴대폰인증 하겠습니다",
                                                Title = "휴대폰 인증"
                                            };
                                            cardButtons.Add(deliveryButton);

                                            CardAction returnButton = new CardAction();
                                            returnButton = new CardAction()
                                            {
                                                Type = "postBack",
                                                //Value = "아니오. 휴대폰인증 취소하겠습니다",
                                                Value = "[F_예약확인]::예약번호직접입력",
                                                Title = "예약번호 직접입력"
                                            };
                                            cardButtons.Add(returnButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "네~ 고객님<br>예약 목록 확인등을 위해서 휴대폰 인증이 필요합니다.<br>휴대폰 인증을 하시겠습니까?<br>또는 예약번호를 입력해 주십시오",
                                                Buttons = cardButtons,
                                            };
                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            int totalPage = 0;
                                            if (apiActiveText.Contains("예약목록다음페이지") && activity.Text.Contains(">>"))
                                            {
                                                collectionListPageNum++;
                                            }
                                            else if (apiActiveText.Contains("예약목록이전페이지") && activity.Text.Contains("<<"))
                                            {
                                                collectionListPageNum--;
                                            }
                                            else
                                            {
                                                collectionListPageNum = 1;
                                            }

                                            /*
                                            * POST METHOD
                                            * */
                                            postParams = new StringBuilder();
                                            //postParams.Append("tel_num=" + requestPhone);
                                            postParams.Append("auth_num=" + authNumber);
                                            postParams.Append("&pag_num=" + collectionListPageNum);
                                            postParams.Append("&pag_cnt=" + pageCnt);

                                            Encoding encoding = Encoding.UTF8;
                                            byte[] result = encoding.GetBytes(postParams.ToString());

                                            wReq = (HttpWebRequest)WebRequest.Create(DeliveryCollection);
                                            wReq.Method = "POST";
                                            wReq.ContentType = "application/x-www-form-urlencoded";
                                            wReq.ContentLength = result.Length;

                                            postDataStream = wReq.GetRequestStream();
                                            postDataStream.Write(result, 0, result.Length);
                                            postDataStream.Close();

                                            wResp = (HttpWebResponse)wReq.GetResponse();
                                            respPostStream = wResp.GetResponseStream();
                                            readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                            String DeliveryCollectionJsonData = readerPost.ReadToEnd();
                                            Debug.WriteLine("post data(예약확인)====" + DeliveryCollectionJsonData);
                                            JArray obj = JArray.Parse(DeliveryCollectionJsonData);
                                            int checkInt = obj.Count;

                                            if (checkInt == 0)
                                            {
                                                totalPage = 1;
                                                UserHeroCard plCard1 = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                };

                                                Attachment plAttachment = plCard1.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                            else
                                            {
                                                List<CardAction> numberButtons = new List<CardAction>();
                                                CardAction numberButton = new CardAction();
                                                numberButton = new CardAction()
                                                {
                                                    Type = "postBack",
                                                    Value = "[F_예약확인]::운송장예약번호직접입력",
                                                    Title = "운송장/예약번호 직접입력"
                                                };
                                                numberButtons.Add(numberButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "예약번호 또는 원 운송장번호로 직접조회할 수 있습니다.",
                                                    Buttons = numberButtons,
                                                };
                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);

                                                foreach (JObject jobj in obj)
                                                {
                                                    if (jobj["ret_cod"].ToString().Equals("9051"))
                                                    {
                                                        totalPage = 1;
                                                        plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "인증번호 오류입니다. 다시 한번 시도해 주세요."
                                                        };
                                                        plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else if (jobj["ret_cod"].ToString().Equals("9052"))
                                                    {
                                                        totalPage = 1;
                                                        plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "고객님의 휴대폰 번호로 조회된 결과가 없습니다."
                                                        };
                                                        plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else if (jobj["ret_cod"].ToString().Equals("9999"))
                                                    {
                                                        totalPage = 1;
                                                        plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "기타에러가 발생되었습니다."
                                                        };
                                                        plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else
                                                    {
                                                        List<CardList> text = new List<CardList>();
                                                        List<CardAction> cardButtons = new List<CardAction>();

                                                        totalPage = Convert.ToInt32(jobj["tot_pag"].ToString());
                                                        collectionListPageNum = Convert.ToInt32(jobj["pag_num"].ToString());

                                                        String tempDate = jobj["wrk_ymd"].ToString();
                                                        String dateText = tempDate;
                                                        String cardShowText = "";

                                                        String goodNameTemp = jobj["god_nam"].ToString();
                                                        int goodNameLength = jobj["god_nam"].ToString().Length;
                                                        String goodName = "";
                                                        if (goodNameLength > 20)
                                                        {
                                                            goodName = goodNameTemp.Substring(0, 15) + "....";
                                                        }
                                                        else
                                                        {
                                                            goodName = goodNameTemp;
                                                        }

                                                        if (tempDate == "" || tempDate.Equals(""))
                                                        {
                                                            dateText = "";
                                                            cardShowText = "<strong>·예약번호 : </strong><strong><font color='#0101DF'>(" + jobj["rsv_num"].ToString() + ")</font></strong><br><strong>· 상품명 : </strong>" + goodName + " <br><strong>·수하인명 : </strong>" + jobj["rcv_nam"].ToString() + " <br><strong>·예약상태 : </strong><font color='#0101DF'>" + jobj["wrk_nam"].ToString() + "</font>";
                                                        }
                                                        else
                                                        {
                                                            String yearText = tempDate.Substring(0, 4);
                                                            String monthText = tempDate.Substring(4, 2);
                                                            String dayText = tempDate.Substring(6, 2);
                                                            dateText = yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["wrk_dy"].ToString() + ")";
                                                            cardShowText = "<strong>·예약번호 : </strong><strong><font color='#0101DF'>(" + jobj["rsv_num"].ToString() + ")</font></strong><br><strong>· 상품명 : </strong>" + goodName + " <br><strong>·수하인명 : </strong>" + jobj["rcv_nam"].ToString() + " <br><strong>·예약상태 : </strong><font color='#0101DF'>" + jobj["wrk_nam"].ToString() + "</font><br><strong>·날짜 : </strong>" + dateText;
                                                        }

                                                        CardAction bookButton = new CardAction();
                                                        bookButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = jobj["rsv_num"].ToString() + " 예약 내용 확인",
                                                            Title = "예약 내용 확인"
                                                        };
                                                        cardButtons.Add(bookButton);

                                                        plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = cardShowText,
                                                            Buttons = cardButtons,
                                                        };
                                                        plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                }

                                                if (totalPage == 1)
                                                {
                                                    //페이징 없음
                                                }
                                                else
                                                {
                                                    List<CardAction> pageButtons = new List<CardAction>();
                                                    //전체페이지와 동일하면 다음버튼은 없다
                                                    if (collectionListPageNum == totalPage)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        CardAction nextButton = new CardAction();
                                                        nextButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "예약목록 다음페이지>>",
                                                            Title = "다음페이지",
                                                        };
                                                        pageButtons.Add(nextButton);
                                                    }
                                                    //현재 페이지가 1이면 이전버튼은 없다.
                                                    if (collectionListPageNum < 2)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        CardAction prevButton = new CardAction();
                                                        prevButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "예약목록 이전페이지<<",
                                                            Title = "이전페이지",
                                                        };
                                                        pageButtons.Add(prevButton);
                                                    }


                                                    UserHeroCard pageCard = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "",
                                                        Buttons = pageButtons,
                                                    };
                                                    plAttachment = pageCard.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment);
                                                }
                                            }


                                            SetActivity(apiMakerReply);
                                        }
                                    }
                                }
                            }
                            else
                            {

                            }

                            /*****************************************************************
                             * apiIntent 예약취소
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_예약취소"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Contains("예약취소선택"))
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction cancel1Button = new CardAction();
                                    cancel1Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        //Value = "예약취소진행(" + bookNumber + ")-발송취소",
                                        Value = "예약취소진행(" + bookNumber + ")-계속사용으로 반품취소,발송취소",
                                        Title = "계속사용으로 반품취소,발송취소"
                                    };
                                    cardButtons.Add(cancel1Button);
                                    CardAction cancel2Button = new CardAction();
                                    cancel2Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        //Value = "예약취소진행(" + bookNumber + ")-기집하",
                                        Value = "예약취소진행(" + bookNumber + ")-타택배 또는 다른경로로 발송",
                                        Title = "타택배 또는 다른경로로 발송"
                                    };
                                    cardButtons.Add(cancel2Button);
                                    CardAction cancel3Button = new CardAction();
                                    cancel3Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        //Value = "예약취소진행(" + bookNumber + ")-이중예약",
                                        Value = "예약취소진행(" + bookNumber + ")-이중예약 또는 기타사유",
                                        Title = "이중예약 또는 기타사유"
                                    };
                                    cardButtons.Add(cancel3Button);

                                    CardAction noButton = new CardAction();
                                    noButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "아니오. " + bookNumber + " 예약 취소하지 않습니다.",
                                        Title = "아니오"
                                    };
                                    cardButtons.Add(noButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "예약 번호 " + bookNumber + " 예약 취소를 진행합니다.<br>취소사유를 선택해 주세요",
                                        Buttons = cardButtons,
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("예약취소진행"))
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    String whyCancelNm = "";
                                    if (apiActiveText.Contains("계속사용으로반품취소발송취소"))
                                    {
                                        whyCancelNm = "5";
                                    }
                                    else if (apiActiveText.Contains("타택배또는다른경로로발송"))
                                    {
                                        whyCancelNm = "6";
                                    }
                                    else if (apiActiveText.Contains("이중예약또는기타사유"))
                                    {
                                        whyCancelNm = "7";
                                    }
                                    else
                                    {
                                        whyCancelNm = "8";
                                    }

                                    /*
                                    * POST METHOD
                                    * */
                                    postParams = new StringBuilder();
                                    postParams.Append("gbn_cod=CHATBOT");
                                    postParams.Append("&rsv_num=" + bookNumber);
                                    postParams.Append("&can_gbn=" + whyCancelNm);
                                    postParams.Append("&tel_num=" + requestPhone);

                                    Encoding encoding = Encoding.UTF8;
                                    byte[] result = encoding.GetBytes(postParams.ToString());

                                    wReq = (HttpWebRequest)WebRequest.Create(bookCancelResult);
                                    wReq.Method = "POST";
                                    wReq.ContentType = "application/x-www-form-urlencoded";
                                    wReq.ContentLength = result.Length;

                                    postDataStream = wReq.GetRequestStream();
                                    postDataStream.Write(result, 0, result.Length);
                                    postDataStream.Close();

                                    wResp = (HttpWebResponse)wReq.GetResponse();
                                    respPostStream = wResp.GetResponseStream();
                                    readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                    String bookCancelResultJsonData = readerPost.ReadToEnd();
                                    Debug.WriteLine("post data(예약취소)====" + bookCancelResultJsonData);
                                    /*
                                    WebClient webClient = new WebClient();
                                    String sample = bookCancelResult + "?gbn_cod=CHATBOT&rsv_num=" + bookNumber+"&can_gbn="+ whyCancelNm+"&tel_num="+requestPhone;
                                    Stream stream = webClient.OpenRead(sample);
                                    String bookCancelResultJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                    */
                                    JArray obj = JArray.Parse(bookCancelResultJsonData);

                                    foreach (JObject jobj in obj)
                                    {
                                        if (jobj["ret_cod"].ToString().Equals("9012"))
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "자동 예약취소가 불가능한 상태입니다.<br>예약취소는 " + jobj["org_nam"].ToString() + " 집배점/ 전화번호 <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>으로 문의하여 주시기 바랍니다.",
                                                Text = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>번은 집하완료가 되어서<br>예약취소가 불가능한 상태입니다.<br>예약, 집하관련 문의 사항은 " + jobj["org_nam"].ToString() + " 집배점 ☎ <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>번 으로 문의하여 주시기 바랍니다.",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9013"))
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "자동 예약취소가 불가능한 상태입니다.<br>예약취소는 " + jobj["org_nam"].ToString() + " 집배점/ 전화번호 <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>으로 문의하여 주시기 바랍니다.",
                                                Text = "입력하신 예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>번은 예약상태가 확인되지 않아 예약취소가 불가능한 번호입니다.<br>예약번호를 다시 확인해주세요.",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9014"))
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "자동 예약취소가 불가능한 상태입니다.<br>예약취소는 " + jobj["org_nam"].ToString() + " 집배점/ 전화번호 <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>으로 문의하여 주시기 바랍니다.",
                                                Text = "이미 예약취소가 완료된 예약번호입니다. <br>예약관련 문의 사항은 " + jobj["org_nam"].ToString() + " 집배점 ☎ <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>번 으로 문의하여 주시기 바랍니다.",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("1000"))
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "예약번호 " + bookNumber + " 의 예약취소 처리가 완료되었습니다.<br>전산취소하였으나, 방문예정안내SMS와 집하직원의 방문 또는 사전전화를<br>받으실 수 있습니다. 이때 전산취소사항을 알려주시기 부탁드립니다.<br>감사합니다.",
                                                Text = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>번의 예약취소가 처리되었습니다.<br>예약취소 하였으나, 방문예정안내SMS와 집하 직원의 방문 또는 사전 전화를 받으실 수 있습니다.<br>이때 예약취소사항을 알려주시기 부탁드립니다.<br>감사합니다.",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "예약번호 " + bookNumber + " 의 예약취소 처리가 완료되었습니다.<br>전산취소하였으나, 방문예정안내SMS와 집하직원의 방문 또는 사전전화를<br>받으실 수 있습니다. 이때 전산취소사항을 알려주시기 부탁드립니다.<br>감사합니다.",
                                                Text = "자동 예약취소가 불가능한 상태입니다.",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }

                                    }
                                }
                                else if (apiActiveText.Contains("아니오") && apiActiveText.Contains("예약취소"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong> 예약취소작업이 종료되었습니다.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("예약번호직접입력"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "정확한 예약 번호를 입력해 주세요.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("예약취소확인") || checkNum == true)
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    /*
                                    * POST METHOD
                                    * */
                                    postParams = new StringBuilder();
                                    postParams.Append("rsv_num=" + bookNumber);

                                    Encoding encoding = Encoding.UTF8;
                                    byte[] result = encoding.GetBytes(postParams.ToString());

                                    wReq = (HttpWebRequest)WebRequest.Create(bookCancelYN);
                                    wReq.Method = "POST";
                                    wReq.ContentType = "application/x-www-form-urlencoded";
                                    wReq.ContentLength = result.Length;

                                    postDataStream = wReq.GetRequestStream();
                                    postDataStream.Write(result, 0, result.Length);
                                    postDataStream.Close();

                                    wResp = (HttpWebResponse)wReq.GetResponse();
                                    respPostStream = wResp.GetResponseStream();
                                    readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                    String bookCancelYNJsonData = readerPost.ReadToEnd();
                                    Debug.WriteLine("post data(예약취소확인)====" + bookCancelYNJsonData);
                                    /************************************************/
                                    /*
                                    WebClient webClient = new WebClient();
                                    String sample = bookCancelYN + "?rsv_num=" + bookNumber;
                                    Stream stream = webClient.OpenRead(sample);
                                    String bookCancelYNJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                    */
                                    JArray obj = JArray.Parse(bookCancelYNJsonData);
                                    foreach (JObject jobj in obj)
                                    {
                                        String tempDate = jobj["acp_ymd"].ToString();
                                        String dateText = tempDate;
                                        String correctDateText = "";
                                        if (tempDate == "" || tempDate.Equals(""))
                                        {
                                            dateText = "";
                                        }
                                        else
                                        {
                                            String yearText = tempDate.Substring(0, 4);
                                            String monthText = tempDate.Substring(4, 2);
                                            String dayText = tempDate.Substring(6, 2);
                                            dateText = "<br><strong>·예약일시: </strong >" + yearText + "년 " + monthText + "월 " + dayText + "일";
                                            correctDateText = yearText + "년 " + monthText + "월 " + dayText + "일";
                                        }

                                        String heroCardText = "";

                                        if (jobj["ret_cod"].ToString().Equals("1000"))
                                        {
                                            heroCardText = correctDateText + "에 예약하신 예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>를 취소하시려면 아래 버튼을 클릭해주세요";
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9012"))
                                        {
                                            //heroCardText = "이미 집하완료가 되어서 예약취소가 불가능한 상태입니다. 예약취소는 집배점으로 문의하여 주시기 바랍니다<br><br><strong>·예약번호 : </strong><font color='#0101DF'>" + bookNumber +"</font>"+ dateText + "<br><strong>· 집배점: </strong>" + jobj["org_nam"].ToString() + "<br><strong>·전화번호: </strong><a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>";
                                            heroCardText = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>번은 집하완료가 되어서 예약취소가 불가능한 상태입니다.<br>예약, 집하관련 문의 사항은 " + jobj["org_nam"].ToString() + " 집배점 ☎ <a href = 'tel:" + jobj["tel_num"].ToString() + "' > " + jobj["tel_num"].ToString() + " </a> 번 으로 문의하여 주시기 바랍니다";
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9013"))
                                        {
                                            heroCardText = "입력하신 예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong>번은 예약상태가 확인되지 않아 예약취소가 불가능한 번호입니다.<br>예약번호를 다시 확인해주세요.";
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9014"))
                                        {
                                            //heroCardText = "이미 예약취소가 된 예약번호이기에 예약취소가 불가능한 상태입니다. 예약취소는 집배점으로 문의하여 주시기 바랍니다<br><br><strong>·예약번호 : </strong><font color='#0101DF'>" + bookNumber + "</font>"+ dateText + "<br><strong>· 집배점: </strong>" + jobj["org_nam"].ToString() + "<br><strong>·전화번호: </strong><a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>";
                                            heroCardText = "이미 예약취소가 완료된 예약번호입니다.<br>예약관련 문의 사항은 " + jobj["org_nam"].ToString() + " 집배점 ☎ <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>번 으로 문의하여 주시기 바랍니다";
                                        }
                                        else
                                        {
                                            heroCardText = "자동 예약취소가 불가능한 상태입니다. ";
                                        }

                                        List<CardAction> cardButtons = new List<CardAction>();
                                        CardAction bookButton = new CardAction();

                                        if (jobj["ret_cod"].ToString().Equals("1000"))
                                        {
                                            bookButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "예약번호 " + bookNumber + " 예약 취소선택",
                                                Title = "예약 취소"
                                            };

                                            cardButtons.Add(bookButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = heroCardText,
                                                Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = heroCardText,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }

                                    }
                                }
                                else
                                {
                                    if (mobilePC.Equals("PC"))
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "postBack",
                                            Value = "[F_예약취소]::예약번호직접입력",
                                            Title = "예약번호 직접입력"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "네~ 고객님<br>예약번호를 입력해 주십시오",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        //모바일 인증 체크
                                        if (authCheck.Equals("F"))
                                        {
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_URL", "[F_예약취소]::나의예약취소");
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_예약취소");
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction deliveryButton = new CardAction();
                                            deliveryButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "예. 휴대폰인증 하겠습니다",
                                                Title = "휴대폰 인증"
                                            };
                                            cardButtons.Add(deliveryButton);

                                            CardAction returnButton = new CardAction();
                                            returnButton = new CardAction()
                                            {
                                                Type = "postBack",
                                                //Value = "아니오. 휴대폰인증 취소하겠습니다",
                                                Value = "[F_예약취소]::예약번호직접입력",
                                                Title = "예약번호 직접입력"
                                            };
                                            cardButtons.Add(returnButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "네~ 고객님<br>예약 목록 확인등을 위해서 휴대폰 인증이 필요합니다.<br>휴대폰 인증을 하시겠습니까?<br>또는 예약번호를 입력해 주십시오",
                                                Buttons = cardButtons,
                                            };
                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            int totalPage = 0;
                                            if (apiActiveText.Contains("예약취소목록다음페이지") && activity.Text.Contains(">>"))
                                            {
                                                collectionListPageNum++;
                                            }
                                            else if (apiActiveText.Contains("예약취소목록이전페이지") && activity.Text.Contains("<<"))
                                            {
                                                collectionListPageNum--;
                                            }
                                            else
                                            {
                                                collectionListPageNum = 1;
                                            }

                                            /*
                                            * POST METHOD
                                            * */
                                            postParams = new StringBuilder();
                                            //postParams.Append("tel_num=" + requestPhone);
                                            postParams.Append("auth_num=" + authNumber);
                                            postParams.Append("&pag_num=" + collectionListPageNum);
                                            postParams.Append("&pag_cnt=" + pageCnt);

                                            Encoding encoding = Encoding.UTF8;
                                            byte[] result = encoding.GetBytes(postParams.ToString());

                                            wReq = (HttpWebRequest)WebRequest.Create(DeliveryCollection);
                                            wReq.Method = "POST";
                                            wReq.ContentType = "application/x-www-form-urlencoded";
                                            wReq.ContentLength = result.Length;

                                            postDataStream = wReq.GetRequestStream();
                                            postDataStream.Write(result, 0, result.Length);
                                            postDataStream.Close();

                                            wResp = (HttpWebResponse)wReq.GetResponse();
                                            respPostStream = wResp.GetResponseStream();
                                            readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                            String DeliveryCollectionJsonData = readerPost.ReadToEnd();
                                            Debug.WriteLine("post data(예약취소)====" + DeliveryCollectionJsonData);
                                            /************************************************/
                                            /*
                                            WebClient webClient = new WebClient();
                                            String sample = DeliveryCollection + "?tel_num=" + requestPhone + "&pag_num=" + collectionListPageNum + "&pag_cnt=" + pageCnt;
                                            Stream stream = webClient.OpenRead(sample);
                                            String DeliveryCollectionJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                            */
                                            JArray obj = JArray.Parse(DeliveryCollectionJsonData);
                                            int checkInt = obj.Count;

                                            if (checkInt == 0)
                                            {
                                                UserHeroCard plCard1 = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                };

                                                Attachment plAttachment = plCard1.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                            else
                                            {
                                                List<CardAction> numberButtons = new List<CardAction>();
                                                CardAction numberButton = new CardAction();
                                                numberButton = new CardAction()
                                                {
                                                    Type = "postBack",
                                                    Value = "[F_예약취소]::예약번호직접입력",
                                                    Title = "예약번호 직접입력"
                                                };
                                                numberButtons.Add(numberButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "아래 목록에서 예약취소 하실 상품의 예약번호를 선택해 주세요.<br>해당 예약번호가 목록에 없는 경우, <br>예약번호를 직접 입력해주세요. ",
                                                    Buttons = numberButtons,
                                                };
                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);

                                                foreach (JObject jobj in obj)
                                                {
                                                    if (jobj["ret_cod"].ToString().Equals("9052"))
                                                    {
                                                        totalPage = 1;
                                                        plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                        };
                                                        plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else if (jobj["ret_cod"].ToString().Equals("9051"))
                                                    {
                                                        totalPage = 1;
                                                        plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "인증번호에 오류가 발생되었습니다."
                                                        };
                                                        plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else if (jobj["ret_cod"].ToString().Equals("9999"))
                                                    {
                                                        totalPage = 1;
                                                        plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "기타에러사항이 발생되었습니다."
                                                        };
                                                        plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else
                                                    {
                                                        List<CardList> text = new List<CardList>();
                                                        List<CardAction> cardButtons = new List<CardAction>();

                                                        totalPage = Convert.ToInt32(jobj["tot_pag"].ToString());
                                                        collectionListPageNum = Convert.ToInt32(jobj["pag_num"].ToString());

                                                        String tempDate = jobj["wrk_ymd"].ToString();
                                                        String dateText = tempDate;
                                                        String cardShowText = "";

                                                        String goodNameTemp = jobj["god_nam"].ToString();
                                                        int goodNameLength = jobj["god_nam"].ToString().Length;
                                                        String goodName = "";
                                                        if (goodNameLength > 20)
                                                        {
                                                            goodName = goodNameTemp.Substring(0, 15) + "....";
                                                        }
                                                        else
                                                        {
                                                            goodName = goodNameTemp;
                                                        }

                                                        if (tempDate == "" || tempDate.Equals(""))
                                                        {
                                                            dateText = "미할당";
                                                            cardShowText = "<strong>·예약번호 : </strong><strong><font color='#0101DF'>(" + jobj["rsv_num"].ToString() + ")</font></strong><br><strong>· 상품명 : </strong>" + goodName + " <br><strong>·수하인명 : </strong>" + jobj["rcv_nam"].ToString() + " <br><strong>·예약상태 : </strong><font color='#0101DF'>" + jobj["wrk_nam"].ToString() + "</font>";
                                                        }
                                                        else
                                                        {
                                                            String yearText = tempDate.Substring(0, 4);
                                                            String monthText = tempDate.Substring(4, 2);
                                                            String dayText = tempDate.Substring(6, 2);
                                                            dateText = yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["wrk_dy"].ToString() + ")";
                                                            cardShowText = "<strong>·예약번호 : </strong><strong><font color='#0101DF'>(" + jobj["rsv_num"].ToString() + ")</font></strong><br><strong>· 상품명 : </strong>" + goodName + " <br><strong>·수하인명 : </strong>" + jobj["rcv_nam"].ToString() + " <br><strong>·예약상태 : </strong><font color='#0101DF'>" + jobj["wrk_nam"].ToString() + "</font><br><strong>·날짜 : </strong>" + dateText;
                                                        }

                                                        CardAction bookButton = new CardAction();
                                                        bookButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = jobj["rsv_num"].ToString() + " 예약 취소 확인",
                                                            Title = "예약 취소"
                                                        };
                                                        cardButtons.Add(bookButton);

                                                        plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = cardShowText,
                                                            Buttons = cardButtons,
                                                        };
                                                        plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }

                                                }
                                                if (totalPage == 1)
                                                {
                                                    //페이징 없음
                                                }
                                                else
                                                {
                                                    List<CardAction> pageButtons = new List<CardAction>();
                                                    //전체페이지와 동일하면 다음버튼은 없다
                                                    if (collectionListPageNum == totalPage)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        CardAction nextButton = new CardAction();
                                                        nextButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "예약취소목록 다음페이지>>",
                                                            Title = "다음페이지",
                                                        };
                                                        pageButtons.Add(nextButton);
                                                    }
                                                    //현재 페이지가 1이면 이전버튼은 없다.
                                                    if (collectionListPageNum < 2)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        CardAction prevButton = new CardAction();
                                                        prevButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "예약취소목록 이전페이지<<",
                                                            Title = "이전페이지",
                                                        };
                                                        pageButtons.Add(prevButton);
                                                    }


                                                    UserHeroCard pageCard = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "",
                                                        Buttons = pageButtons,
                                                    };
                                                    plAttachment = pageCard.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment);
                                                }
                                            }
                                            SetActivity(apiMakerReply);
                                        }
                                    }

                                }
                            }
                            else
                            {
                                //if (apiIntent.Equals("F_예약취소"))
                            }

                            /*****************************************************************
                             * apiIntent 운송장번호확인
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_운송장번호확인"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (containNum == true)//예약번호나 원 운송장 번호라 판단함
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");//예약번호 또는 운송장번호
                                    /*
                                    * POST METHOD
                                    * */
                                    postParams = new StringBuilder();
                                    postParams.Append("wbl_rsv=" + bookNumber);

                                    Encoding encoding = Encoding.UTF8;
                                    byte[] result = encoding.GetBytes(postParams.ToString());

                                    wReq = (HttpWebRequest)WebRequest.Create(findWayBillNm);
                                    wReq.Method = "POST";
                                    wReq.ContentType = "application/x-www-form-urlencoded";
                                    wReq.ContentLength = result.Length;

                                    postDataStream = wReq.GetRequestStream();
                                    postDataStream.Write(result, 0, result.Length);
                                    postDataStream.Close();

                                    wResp = (HttpWebResponse)wReq.GetResponse();
                                    respPostStream = wResp.GetResponseStream();
                                    readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                    String findWayBillNmJsonData = readerPost.ReadToEnd();
                                    Debug.WriteLine("post data(운송장번호확인)====" + findWayBillNmJsonData);
                                    /************************************************/
                                    /*
                                    WebClient webClient = new WebClient();
                                    String sample = findWayBillNm + "?wbl_rsv=" + bookNumber;
                                    Debug.WriteLine("URL==" + sample);
                                    Stream stream = webClient.OpenRead(sample);
                                    String findWayBillNmJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                    */
                                    JArray obj = JArray.Parse(findWayBillNmJsonData);

                                    foreach (JObject jobj in obj)
                                    {
                                        if (jobj["ret_cod"].ToString().Equals("9008"))
                                        {
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction mhomeButton = new CardAction();
                                            mhomeButton = new CardAction()
                                            {
                                                Type = "postBack",
                                                Value = "[F_운송장번호확인]::반송장번호직접입력",
                                                Title = "예약번호/운송장번호 직접입력"
                                            };
                                            cardButtons.Add(mhomeButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "해당 번호로 운송장번호가 조회되지 않습니다<br>다시 한번 예약번호나 반품택배예약 접수하신 운송장번호를 확인 후 입력해 주세요.",
                                                Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                            break;
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("1000"))
                                        {
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction mhomeButton = new CardAction();
                                            mhomeButton = new CardAction()
                                            {
                                                Type = "postBack",
                                                Value = "운송장 번호 " + jobj["wbl_num"].ToString() + "에 대한 배송일정조회",
                                                Title = "배송 조회"
                                            };
                                            cardButtons.Add(mhomeButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "운송장 번호 " + jobj["wbl_num"].ToString() + " 입니다<br> 한진택배를 이용해 주셔서 감사합니다.",
                                                Text = "예약번호 <strong><font color='#0101DF'>(" + bookNumber + ")</font></strong> 번의<br>발송된 운송장 번호는 <strong><font color='#0101DF'>(" + jobj["wbl_num"].ToString() + ")</font></strong> 입니다.<br><br>반송 상품의 배송조회는 아래 버튼을 눌러주세요.",
                                                Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);

                                            UserHeroCard defaultCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "네~ 고객님<br>문의하신 지역의 담당기사 연락처입니다.<br>근무 외 시간에는 통화가 어려우니 참고 해주시기 바랍니다.<br>(*근무시간: 09시~18시)<br><br>담당기사: <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a><br>집배점: " + jobj["org_nam"].ToString() + " <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a><br><br>고객님께 작은 도움이 되었기를 바랍니다. 추가적으로 궁금한 사항은 언제든지 문의해 주세요.",
                                                Text = "언제든 필요한 게 있으면 말씀해 주세요.<br><br>예) 택배예약, 배송조회, 택배요금",
                                            };

                                            plAttachment = defaultCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);

                                            SetActivity(apiMakerReply);
                                            break;
                                        }
                                        else//기타 에러사항
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "기타 에러사항이 발생되었습니다<br>죄송하지만 다시 한번 시도해 주세요",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                            break;
                                        }

                                    }
                                }
                                else
                                {
                                    if (mobilePC.Equals("PC"))
                                    {
                                        //no show
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "postBack",
                                            Value = "[F_운송장번호확인]::반송장번호직접입력",
                                            Title = "예약번호/운송장번호 직접입력"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard1 = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "네~ 고객님<br>상품 수령하신 운송장번호로 반품택배 예약접수를 하신 경우, 운송장번호 조회가 가능하며, 예약번호를 아신다면 예약번호로 반송 운송장번호 확인이 가능합니다.<br><br>예약번호나 운송장번호를 입력해 주십시오.",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment1 = plCard1.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment1);
                                        SetActivity(apiMakerReply);
                                    }
                                    else if (apiActiveText.Contains("반송장번호직접입력"))
                                    {
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "예약번호나 운송장번호를 입력해 주십시오.",
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        //모바일 인증 체크
                                        if (authCheck.Equals("F"))
                                        {
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_URL", "[F_운송장번호확인]::반송장번호확인");
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_운송장번호확인");
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction deliveryButton = new CardAction();
                                            deliveryButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "예. 휴대폰인증 하겠습니다",
                                                Title = "휴대폰 인증"
                                            };
                                            cardButtons.Add(deliveryButton);

                                            CardAction returnButton = new CardAction();
                                            returnButton = new CardAction()
                                            {
                                                Type = "postBack",
                                                Value = "[F_운송장번호확인]::반송장번호직접입력",
                                                Title = "예약번호/운송장번호 직접입력"
                                            };
                                            cardButtons.Add(returnButton);

                                            UserHeroCard plCard1 = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "네~ 고객님<br>상품 수령하신 운송장번호로 반품택배 예약접수를 하신 경우, 운송장번호 조회가 가능하며, 예약번호를 아신다면 예약번호로 반송 운송장번호 확인이 가능합니다.<br><br>택배목록 확인등을 위해서 휴대폰 인증이 필요합니다. <br>휴대폰 인증을 하시겠습니까?<br>또는 예약번호나 운송장번호를 입력해 주십시오.",
                                                Buttons = cardButtons,
                                            };
                                            Attachment plAttachment1 = plCard1.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment1);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            int totalPage = 0;
                                            if (apiActiveText.Contains("반송장번호확인다음페이지") && activity.Text.Contains(">>"))
                                            {
                                                deliveryListPageNum++;
                                            }
                                            else if (apiActiveText.Contains("반송장번호확인이전페이지") && activity.Text.Contains("<<"))
                                            {
                                                deliveryListPageNum--;
                                            }
                                            else
                                            {
                                                deliveryListPageNum = 1;
                                            }

                                            List<CardAction> returnButtons = new List<CardAction>();
                                            CardAction returnListButton = new CardAction();
                                            returnListButton = new CardAction()
                                            {
                                                Type = "postBack",
                                                Value = "[F_운송장번호확인]::반송장번호직접입력",
                                                Title = "예약번호/운송장번호 직접입력"
                                            };
                                            returnButtons.Add(returnListButton);


                                            UserHeroCard startCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "반품상품의 운송장번호 확인입니다.<br>예약번호나 반품택배예약 접수하신<br>운송장번호를 입력해 주세요.",
                                                Buttons = returnButtons,
                                            };

                                            Attachment plAttachment1 = startCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment1);
                                            /*
                                             * POST METHOD
                                             * */
                                            postParams = new StringBuilder();
                                            postParams.Append("auth_num=" + authNumber);
                                            postParams.Append("&pag_num=" + deliveryListPageNum);
                                            postParams.Append("&pag_cnt=" + pageCnt);

                                            Encoding encoding = Encoding.UTF8;
                                            byte[] result = encoding.GetBytes(postParams.ToString());

                                            wReq = (HttpWebRequest)WebRequest.Create(DeliveryList);
                                            wReq.Method = "POST";
                                            wReq.ContentType = "application/x-www-form-urlencoded";
                                            wReq.ContentLength = result.Length;

                                            postDataStream = wReq.GetRequestStream();
                                            postDataStream.Write(result, 0, result.Length);
                                            postDataStream.Close();

                                            wResp = (HttpWebResponse)wReq.GetResponse();
                                            respPostStream = wResp.GetResponseStream();
                                            readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                            String DeliveryListJsonData = readerPost.ReadToEnd();
                                            Debug.WriteLine("반송장번호확인택배예약목록===========================================" + DeliveryListJsonData);
                                            JArray obj = JArray.Parse(DeliveryListJsonData);

                                            foreach (JObject jobj in obj)
                                            {
                                                if (jobj["ret_cod"].ToString().Equals("9041"))
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard1 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "인증번호에 오류가 발생되었습니다."
                                                    };
                                                    plAttachment1 = plCard1.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment1);
                                                }
                                                else if (jobj["ret_cod"].ToString().Equals("9042"))
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard1 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                    };
                                                    plAttachment1 = plCard1.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment1);
                                                }
                                                else if (jobj["ret_cod"].ToString().Equals("9999"))
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard1 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "시스템 오류 또는 요청항목이 누락되었습니다."
                                                    };
                                                    plAttachment1 = plCard1.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment1);
                                                }
                                                else
                                                {
                                                    List<CardAction> cardButtons = new List<CardAction>();

                                                    totalPage = Convert.ToInt32(jobj["tot_pag"].ToString());
                                                    deliveryListPageNum = Convert.ToInt32(jobj["pag_num"].ToString());

                                                    String tempDate = jobj["dlv_ymd"].ToString();
                                                    String dateText = "";
                                                    if (tempDate == "" || tempDate.Equals(""))
                                                    {
                                                        //dateText = "<div class=\"endDate\"><span class=\"dateDay\"><small>배송중</small></span><span class=\"dateWeek\"></span></div>";
                                                        dateText = "";
                                                    }
                                                    else
                                                    {
                                                        String yearText = tempDate.Substring(0, 4);
                                                        String monthText = tempDate.Substring(4, 2);
                                                        String dayText = tempDate.Substring(6, 2);
                                                        dateText = "<strong>· 배송완료일자 : </strong > " + yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["dlv_dy"].ToString() + ")<br>";
                                                        //dateText = "<div class=\"endDate\"><span class=\"dateDay\">" + monthText + "." + dayText + "</span><span class=\"dateWeek\">" + jobj["dlv_dy"].ToString() + "요일</span></div>";
                                                    }
                                                    //배송상태 처리
                                                    String deliveryStatus = jobj["wrk_nam"].ToString();
                                                    String deliveryStatusText = "상품접수";
                                                    if (deliveryStatus.Equals("10"))
                                                    {
                                                        deliveryStatusText = "상품접수";
                                                    }
                                                    else if (deliveryStatus.Equals("20"))
                                                    {
                                                        deliveryStatusText = "상품발송대기중";
                                                    }
                                                    else if (deliveryStatus.Equals("30"))
                                                    {
                                                        deliveryStatusText = "이동중";
                                                    }
                                                    else if (deliveryStatus.Equals("40"))
                                                    {
                                                        deliveryStatusText = "배송준비";
                                                    }
                                                    else if (deliveryStatus.Equals("50"))
                                                    {
                                                        deliveryStatusText = "배송중";
                                                    }
                                                    else
                                                    {
                                                        deliveryStatusText = "배송완료";
                                                    }

                                                    String goodNameTemp = jobj["god_nam"].ToString();
                                                    int goodNameLength = jobj["god_nam"].ToString().Length;
                                                    String goodName = "";
                                                    if (goodNameLength > 20)
                                                    {
                                                        goodName = goodNameTemp.Substring(0, 15) + "....";
                                                    }
                                                    else
                                                    {
                                                        goodName = goodNameTemp;
                                                    }


                                                    CardAction plButton = new CardAction();
                                                    plButton = new CardAction()
                                                    {
                                                        Type = "imBack",
                                                        Value = "운송장번호 " + jobj["wbl_num"].ToString() + " 의 반송장번호 확인",
                                                        Title = "반송장 번호 확인",
                                                    };
                                                    cardButtons.Add(plButton);

                                                    UserHeroCard plCard1 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = dateText + "<strong>· 배송상태 : <font color='#0101DF'>" + deliveryStatusText + "</font></strong><br><strong>· 상품명 : </strong><font color='#585858'>" + goodName + "</font><br><strong>· 운송장번호 : <font color='#0101DF'>(" + jobj["wbl_num"].ToString() + ")</font></strong><br><strong>· 송하인명 : </strong>" + jobj["snd_nam"].ToString(),
                                                        //Text = "<div class=\"takeBack\">" + dateText + "<div class=\"prodInfo\"><span class=\"prodName\">" + goodName + "</span><span class=\"prodNum\">" + jobj["wbl_num"].ToString() + "/" + jobj["snd_nam"].ToString() + "</span><span class=\"prodStatus\">" + deliveryStatusText + "</span></div></div>",
                                                        Tap = plButton
                                                    };
                                                    plAttachment1 = plCard1.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment1);
                                                }

                                            }
                                            if (totalPage == 1)
                                            {
                                                //페이징 없음
                                            }
                                            else
                                            {
                                                List<CardAction> pageButtons = new List<CardAction>();
                                                //전체페이지와 동일하면 다음버튼은 없다
                                                if (deliveryListPageNum == totalPage)
                                                {

                                                }
                                                else
                                                {
                                                    CardAction nextButton = new CardAction();
                                                    nextButton = new CardAction()
                                                    {
                                                        Type = "imBack",
                                                        Value = "반송장번호확인 다음페이지>>",
                                                        Title = "다음페이지",
                                                    };
                                                    pageButtons.Add(nextButton);
                                                }
                                                //현재 페이지가 1이면 이전버튼은 없다.
                                                if (deliveryListPageNum < 2)
                                                {

                                                }
                                                else
                                                {
                                                    CardAction prevButton = new CardAction();
                                                    prevButton = new CardAction()
                                                    {
                                                        Type = "imBack",
                                                        Value = "반송장번호확인 이전페이지<<",
                                                        Title = "이전페이지",
                                                    };
                                                    pageButtons.Add(prevButton);
                                                }


                                                UserHeroCard pageCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "",
                                                    Buttons = pageButtons,
                                                };
                                                plAttachment1 = pageCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment1);
                                            }
                                            SetActivity(apiMakerReply);
                                        }
                                    }
                                }
                            }
                            else
                            {

                            }

                            /*****************************************************************
                             * apiIntent 택배배송일정조회
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_택배배송일정조회"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Contains("에대한배송일정조회") || containNum == true)//직접이던 선택이던
                                {
                                    if (containNum == true) //숫자가 포함(직접이던 선택이던)
                                    {
                                        invoiceNumber = Regex.Replace(activity.Text, @"\D", "");
                                        db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "TEMP_NUMBER", invoiceNumber);//운송장번호 기억-위탁배송때문임
                                        /*
                                    * POST METHOD
                                    * */
                                        postParams = new StringBuilder();
                                        postParams.Append("wbl_num=" + invoiceNumber);

                                        Encoding encoding = Encoding.UTF8;
                                        byte[] result = encoding.GetBytes(postParams.ToString());

                                        wReq = (HttpWebRequest)WebRequest.Create(goodLocation);
                                        wReq.Method = "POST";
                                        wReq.ContentType = "application/x-www-form-urlencoded";
                                        wReq.ContentLength = result.Length;

                                        postDataStream = wReq.GetRequestStream();
                                        postDataStream.Write(result, 0, result.Length);
                                        postDataStream.Close();

                                        wResp = (HttpWebResponse)wReq.GetResponse();
                                        respPostStream = wResp.GetResponseStream();
                                        readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                        String goodLocationJsonData = readerPost.ReadToEnd();
                                        Debug.WriteLine("post data(택배배송일정조회)====" + goodLocationJsonData);
                                        /************************************************/
                                        /*
                                        WebClient webClient = new WebClient();
                                        String sample = goodLocation + "?wbl_num= " + invoiceNumber;
                                        Stream stream = webClient.OpenRead(sample);
                                        String goodLocationJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                        */
                                        JArray obj = JArray.Parse(goodLocationJsonData);

                                        String wrkCod = "";//상태코드
                                        String orgNam = ""; //집배점명
                                        String telNum = ""; //전화번호
                                        String empTel = ""; //배송직원전화번호
                                        String statusText = "";

                                        foreach (JObject jobj in obj)
                                        {
                                            if (jobj["wrk_cod"].ToString().Equals("10"))
                                            {
                                                wrkCod = "상품접수";
                                                //statusText = "고객님께서 문의하신 운송장 번호 <strong>" + invoiceNumber + "</strong>는 현재 상품 발송을 위해 운송장이 접수된 상태입니다.<br>터미널 입고 시점부터 배송 일정 조회가 가능하며 당일 출고한 상품은 발송일 오후나 다음날 다시 한번 배송조회 확인해 주시기 바랍니다.<br>1~2일이 경과하여도 상품 이동 내역이 없는 경우에는 주문업체(쇼핑몰) 또는 보내는 분께 상품 발송 일자와 한진택배로 발송된 상품인지 문의해 주시기 바랍니다.";
                                                statusText = "[상품접수]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNumber + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>접수상태</font></strong><br>현재 상품 발송을 위해 운송장이 접수된 상태입니다.<br>지역에 따라 1~2일이상 소요될 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("20"))
                                            {
                                                wrkCod = "상품발송대기중";
                                                //statusText = "고객님께서 문의하신 운송장 번호 <strong>" + invoiceNumber + "</strong>는 발송 터미널에 입고되어 상품 발송 대기 중입니다. 배송조회 하시는 다음날 다시 한번 확인해 주시기 바랍니다. 지역(현장) 사정에 따라 배송은 1~2일 소요될 수 있는 점 양해바랍니다.";
                                                statusText = "[상품발송대기중]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNumber + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>상품발송 대기중</font></strong><br>발송 터미널에 상품이 입고되어 발송대기 중입니다.<br>지역에 따라 1~2일이상 소요될 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("30"))
                                            {
                                                wrkCod = "이동중";
                                                //statusText = "고객님께서 문의하신 운송장 번호 <strong>" + invoiceNumber + "</strong>는 상품이 발송되어, 이동 중입니다. 배송예정 시간 확인은 당일 도착 물량에 따라 변동이 될 수 있으니 다시 한번 확인 해 주시기 바랍니다. 배송지역 사정에 따라 배송은 1~2일 소요될 수 있는 점 양해바랍니다.";
                                                statusText = "[이동중]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNumber + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>상품이동중</font></strong><br>상품이 발송되어 이동중입니다.<br>지역에 따라 1~2일이상 소요될 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("40"))
                                            {
                                                wrkCod = "배송준비";
                                                //statusText = "고객님께서 문의하신 운송장 번호 <strong>" + invoiceNumber + "</strong>는 현재 배송지역 터미널에 도착하여, 배송 담당자에게 인계를 위해 준비중입니다.<br>배송예정 시간 확인은 당일 도착 물량에 따라 변동이 될 수 있으며 배송은 1~2일 소요 될 수 있는 점 양해바랍니다.";
                                                statusText = "[배송준비]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNumber + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>배송준비중</font></strong><br>배송지역 터미널에 도착하여, 배송 준비중입니다.<br>지역에 따라 1~2일이상 소요될 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("50"))
                                            {
                                                wrkCod = "배송중";
                                                String tempDate = jobj["wrk_ymd"].ToString();
                                                String yearText = tempDate.Substring(0, 4);
                                                String monthText = tempDate.Substring(4, 2);
                                                String dayText = tempDate.Substring(6, 2);
                                                String dateText = yearText + "년 " + monthText + "월 " + dayText + "일";

                                                orgNam = jobj["org_nam"].ToString();
                                                //telNum = jobj["tel_num"].ToString();
                                                //empTel = jobj["emp_tel"].ToString();
                                                telNum = " <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>";
                                                empTel = " <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a>";

                                                //statusText = "<strong>·배송예정일자: </strong > " + dateText + "<br>" + "<strong>· 배송상태 : </strong><font color='#0101DF'>배송 중</font><br><strong>· 운송장번호 : </strong><font color='#0101DF'>" + invoiceNumber + "</font><br><strong>· 배송직원: </strong>☎ " + empTel + "<br><strong>· 집배점: </strong>" + orgNam + " ☎ " + telNum + "<br><br>상품 확인이 안되시거나, 자세한 문의사항은<br>담당직원 또는 집배점으로 문의해 주세요.";
                                                statusText = "[배송중]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNumber + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>배송중</font></strong><br>· 집배점 : " + orgNam + " ☎ <strong>" + telNum + "</strong><br>· 배송직원 : ☎ <strong>" + empTel + "</strong><br>자세한 문의 사항은 집배점 또는 배송직원에게 확인해주시기 바랍니다.<br>※ 운전중이거나 근무시간이 아닌 경우 통화가 어려울 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("60"))
                                            {
                                                wrkCod = "배송완료";
                                                String tempDate = jobj["wrk_ymd"].ToString();
                                                String yearText = tempDate.Substring(0, 4);
                                                String monthText = tempDate.Substring(4, 2);
                                                String dayText = tempDate.Substring(6, 2);
                                                String dateText = yearText + "년 " + monthText + "월 " + dayText + "일";

                                                orgNam = jobj["org_nam"].ToString();
                                                telNum = " <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>";
                                                empTel = " <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a>";
                                                //statusText = "<strong>· 배송완료일자 : </strong > " + dateText + "<br>" + "<strong>· 배송상태 : </strong><font color='#0101DF'>배송완료</font><br><strong>· 운송장번호 : </strong><font color='#0101DF'>" + invoiceNumber + "</font><br><strong>· 배송직원: </strong>☎ " + empTel + "<br><strong>· 집배점: </strong>" + orgNam + " ☎ " +telNum + "<br><br>상품 확인이 안되시거나, 자세한 문의사항은<br>담당직원 또는 집배점으로 문의해 주세요.<br>※ 운전 중, 근무시간이 아닌 경우 통화가 어려울 수 있습니다";
                                                statusText = "[배송완료]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNumber + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>" + dateText + " 배송완료</font></strong><br>· 집배점 : " + orgNam + " ☎ <strong>" + telNum + "</strong><br>· 배송직원 : ☎ <strong>" + empTel + "</strong><br>자세한 문의 사항은 집배점 또는 배송직원에게 확인해주시기 바랍니다.<br>※ 운전중이거나 근무시간이 아닌 경우 통화가 어려울 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("70"))
                                            {
                                                wrkCod = "오도착";
                                                //statusText = "고객님께서 문의하신 운송장 번호 <strong>" + invoiceNumber + "</strong>는 현재 배송지역이 아닌 다른 지역에 경유 중으로 배송은 1~2일 더 소요될 수 있는 점 양해부탁드립니다.<br>배송조회 하시는 다음날 다시 한번 확인해 주시기 바랍니다. ";
                                                statusText = "[오도착]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNumber + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>경유중</font></strong><br>현재 배송지역이 아닌 다른 지역에 경유 중으로 배송은 1~2일이 더 소요될 수 있는 점 양해 부탁드립니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("80"))
                                            {
                                                wrkCod = "미배송";
                                                orgNam = jobj["org_nam"].ToString();
                                                //telNum = jobj["tel_num"].ToString();
                                                telNum = " <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>";
                                                empTel = " <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a>";
                                                //empTel = jobj["emp_tel"].ToString();

                                                //statusText = "<strong>· 배송상태 : </strong><font color='#0101DF'>미배송</font><br><strong>· 운송장번호 : </strong><font color='#0101DF'>" + invoiceNumber + "</font><br><strong>· 배송직원: </strong>☎ " + empTel + "<br><strong>· 집배점: </strong>" + orgNam + " ☎ " + telNum + "<br><br>자세한 문의사항은<br>담당직원 또는 집배점으로 문의해 주세요.";
                                                statusText = "[미배송]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNumber + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>배송중</font></strong><br>· 집배점 : " + orgNam + " ☎ <strong>" + telNum + "</strong><br>· 배송직원 : ☎ <strong>" + empTel + "</strong><br>자세한 문의 사항은 집배점 또는 배송직원에게 확인해주시기 바랍니다.<br>※ 운전중이거나 근무시간이 아닌 경우 통화가 어려울 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else
                                            {
                                                wrkCod = "오류";
                                                //statusText = "오류";
                                                statusText = "고객님께서 문의하신 운송장 번호 <strong><font color='#0101DF'>(" + invoiceNumber + ")</font></strong>는 배송조회가 되지 않습니다.<br>문의 내용이 있으시면 아래 버튼을 눌러주세요.";

                                                List<CardAction> cardButtons = new List<CardAction>();

                                                CardAction returnButton = new CardAction();
                                                returnButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "고객의말씀",
                                                    Title = "온라인 상담"
                                                };
                                                cardButtons.Add(returnButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                    Buttons = cardButtons,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }


                                        }
                                    }
                                    else
                                    {
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "운송장 번호가 확인되지 않았습니다.",
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                }
                                else if (apiActiveText.Contains("운송장번호직접입력"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "정확한 운송장 번호를 입력해 주세요.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else
                                {
                                    if (mobilePC.Equals("PC"))
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "postBack",
                                            Value = "[F_택배배송일정조회]::운송장번호직접입력",
                                            Title = "운송장번호 직접입력"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "네~ 고객님<br>운송장번호를 입력해 주십시오",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        //모바일 인증 체크
                                        if (authCheck.Equals("F"))
                                        {
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_URL", "[F_택배배송일정조회]::나의배송목록");
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_택배배송일정조회");
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction deliveryButton = new CardAction();
                                            deliveryButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "예. 휴대폰인증 하겠습니다",
                                                Title = "휴대폰 인증"
                                            };
                                            cardButtons.Add(deliveryButton);

                                            CardAction returnButton = new CardAction();
                                            returnButton = new CardAction()
                                            {
                                                Type = "postBack",
                                                //Value = "아니오. 휴대폰인증 취소하겠습니다",
                                                Value = "[F_택배배송일정조회]::운송장번호직접입력",
                                                Title = "운송장번호 직접입력"
                                            };
                                            cardButtons.Add(returnButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "네~ 고객님<br>택배목록 확인등을 위해서 휴대폰 인증이 필요합니다.<br>휴대폰 인증을 하시겠습니까?<br>또는 운송장번호를 입력해 주십시오",
                                                Buttons = cardButtons,
                                            };
                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            int totalPage = 0;
                                            if (apiActiveText.Contains("배송목록다음페이지") && activity.Text.Contains(">>"))
                                            {
                                                deliveryListPageNum++;
                                            }
                                            else if (apiActiveText.Contains("배송목록이전페이지") && activity.Text.Contains("<<"))
                                            {
                                                deliveryListPageNum--;
                                            }
                                            else
                                            {
                                                deliveryListPageNum = 1;
                                            }

                                            WebClient webClient = new WebClient();
                                            /*
                                    * POST METHOD
                                    * */
                                            postParams = new StringBuilder();
                                            //postParams.Append("tel_num=" + requestPhone);
                                            postParams.Append("auth_num=" + authNumber);
                                            postParams.Append("&pag_num=" + deliveryListPageNum);
                                            postParams.Append("&pag_cnt=" + pageCnt);

                                            Encoding encoding = Encoding.UTF8;
                                            byte[] result = encoding.GetBytes(postParams.ToString());

                                            wReq = (HttpWebRequest)WebRequest.Create(DeliveryList);
                                            wReq.Method = "POST";
                                            wReq.ContentType = "application/x-www-form-urlencoded";
                                            wReq.ContentLength = result.Length;

                                            postDataStream = wReq.GetRequestStream();
                                            postDataStream.Write(result, 0, result.Length);
                                            postDataStream.Close();

                                            wResp = (HttpWebResponse)wReq.GetResponse();
                                            respPostStream = wResp.GetResponseStream();
                                            readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                            String DeliveryListJsonData = readerPost.ReadToEnd();
                                            Debug.WriteLine("post data(택배배송목록)====" + DeliveryListJsonData);
                                            /************************************************/
                                            /*
                                            String sample = DeliveryList + "?tel_num=" + requestPhone + "&pag_num=" + deliveryListPageNum + "&pag_cnt=" + pageCnt;
                                            Stream stream = webClient.OpenRead(sample);
                                            String DeliveryListJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                            */
                                            JArray obj = JArray.Parse(DeliveryListJsonData);

                                            int checkInt = obj.Count;

                                            if (checkInt == 0)
                                            {
                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                            else
                                            {
                                                foreach (JObject jobj in obj)
                                                {
                                                    if (jobj["ret_cod"].ToString().Equals("9041"))
                                                    {
                                                        totalPage = 1;
                                                        UserHeroCard plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "인증번호에 오류가 발생되었습니다."
                                                        };
                                                        Attachment plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else if (jobj["ret_cod"].ToString().Equals("9042"))
                                                    {
                                                        totalPage = 1;
                                                        UserHeroCard plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                        };
                                                        Attachment plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else if (jobj["ret_cod"].ToString().Equals("9999"))
                                                    {
                                                        totalPage = 1;
                                                        UserHeroCard plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "시스템 오류 또는 요청항목이 누락되었습니다."
                                                        };
                                                        Attachment plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else
                                                    {
                                                        List<CardAction> cardButtons = new List<CardAction>();

                                                        totalPage = Convert.ToInt32(jobj["tot_pag"].ToString());
                                                        deliveryListPageNum = Convert.ToInt32(jobj["pag_num"].ToString());

                                                        String tempDate = jobj["dlv_ymd"].ToString();
                                                        String dateText = "";
                                                        if (tempDate == "" || tempDate.Equals(""))
                                                        {
                                                            dateText = "";
                                                            //dateText = "<div class=\"endDate\"><span class=\"dateDay\"><small>배송중</small></span><span class=\"dateWeek\"></span></div>";
                                                        }
                                                        else
                                                        {
                                                            String yearText = tempDate.Substring(0, 4);
                                                            String monthText = tempDate.Substring(4, 2);
                                                            String dayText = tempDate.Substring(6, 2);
                                                            dateText = "<strong>· 배송완료일자 : </strong > " + yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["dlv_dy"].ToString() + ")<br>";
                                                            //dateText = "<div class=\"endDate\"><span class=\"dateDay\">" + monthText + "." + dayText + "</span><span class=\"dateWeek\">" + jobj["dlv_dy"].ToString() + "요일</span></div>";
                                                        }
                                                        //배송상태 처리
                                                        String deliveryStatus = jobj["wrk_nam"].ToString();
                                                        String deliveryStatusText = "상품접수";
                                                        if (deliveryStatus.Equals("10"))
                                                        {
                                                            deliveryStatusText = "상품접수";
                                                        }
                                                        else if (deliveryStatus.Equals("20"))
                                                        {
                                                            deliveryStatusText = "상품발송대기중";
                                                        }
                                                        else if (deliveryStatus.Equals("30"))
                                                        {
                                                            deliveryStatusText = "이동중";
                                                        }
                                                        else if (deliveryStatus.Equals("40"))
                                                        {
                                                            deliveryStatusText = "배송준비";
                                                        }
                                                        else if (deliveryStatus.Equals("50"))
                                                        {
                                                            deliveryStatusText = "배송중";
                                                        }
                                                        else
                                                        {
                                                            deliveryStatusText = "배송완료";
                                                        }

                                                        String goodNameTemp = jobj["god_nam"].ToString();
                                                        int goodNameLength = jobj["god_nam"].ToString().Length;
                                                        String goodName = "";
                                                        if (goodNameLength > 20)
                                                        {
                                                            goodName = goodNameTemp.Substring(0, 15) + "....";
                                                        }
                                                        else
                                                        {
                                                            goodName = goodNameTemp;
                                                        }


                                                        CardAction plButton = new CardAction();
                                                        plButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "운송장번호 " + jobj["wbl_num"].ToString() + " 에 대한 배송일정 조회",
                                                            Title = "반품택배예약",
                                                        };
                                                        cardButtons.Add(plButton);

                                                        UserHeroCard plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = dateText + "<strong>· 배송상태 : <font color='#0101DF'>" + deliveryStatusText + "</font></strong><br><strong>· 상품명 : </strong><font color='#585858'>" + goodName + "</font><br><strong>· 운송장번호 : <font color='#0101DF'>(" + jobj["wbl_num"].ToString() + ")</font></strong><br><strong>· 송하인명 : </strong>" + jobj["snd_nam"].ToString(),
                                                            //Text = "<div class=\"takeBack\">" + dateText + "<div class=\"prodInfo\"><span class=\"prodName\">" + goodName + "</span><span class=\"prodNum\">" + jobj["wbl_num"].ToString() + "/" + jobj["snd_nam"].ToString() + "</span><span class=\"prodStatus\">" + deliveryStatusText + "</span></div></div>",
                                                            Tap = plButton
                                                        };
                                                        Attachment plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }

                                                }
                                                if (totalPage == 1)
                                                {
                                                    //페이징 없음
                                                }
                                                else
                                                {
                                                    List<CardAction> pageButtons = new List<CardAction>();
                                                    //전체페이지와 동일하면 다음버튼은 없다
                                                    if (deliveryListPageNum == totalPage)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        CardAction nextButton = new CardAction();
                                                        nextButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "배송목록 다음페이지>>",
                                                            Title = "다음페이지",
                                                        };
                                                        pageButtons.Add(nextButton);
                                                    }
                                                    //현재 페이지가 1이면 이전버튼은 없다.
                                                    if (deliveryListPageNum < 2)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        CardAction prevButton = new CardAction();
                                                        prevButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "배송목록 이전페이지<<",
                                                            Title = "이전페이지",
                                                        };
                                                        pageButtons.Add(prevButton);
                                                    }


                                                    UserHeroCard pageCard = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "",
                                                        Buttons = pageButtons,
                                                    };
                                                    Attachment plAttachment = pageCard.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment);
                                                }
                                            }

                                            SetActivity(apiMakerReply);
                                        }
                                    }

                                }
                            }
                            else
                            {

                            }

                            /*****************************************************************
                             * apiIntent 집배점/기사연락처
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_집배점/기사연락처"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Contains("운송장번호") && apiActiveText.Contains("연락처찾기"))
                                {
                                    checkFindAddressCnt = "F";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "ADDRESSCHECK", checkFindAddressCnt);
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "고객님! 운송장번호를 입력해 주세요"
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);

                                }
                                else if (containNum == true && checkFindAddressCnt.Equals("F"))//주소찾기의 숫자가 아닌 운송장 번호로서 찾는다.
                                {
                                    invoiceNumber = Regex.Replace(activity.Text, @"\D", "");
                                    /*
                                    * POST METHOD
                                    * */
                                    postParams = new StringBuilder();
                                    postParams.Append("wbl_num=" + invoiceNumber);

                                    Encoding encoding = Encoding.UTF8;
                                    byte[] result = encoding.GetBytes(postParams.ToString());

                                    wReq = (HttpWebRequest)WebRequest.Create(goodLocation);
                                    wReq.Method = "POST";
                                    wReq.ContentType = "application/x-www-form-urlencoded";
                                    wReq.ContentLength = result.Length;

                                    postDataStream = wReq.GetRequestStream();
                                    postDataStream.Write(result, 0, result.Length);
                                    postDataStream.Close();

                                    wResp = (HttpWebResponse)wReq.GetResponse();
                                    respPostStream = wResp.GetResponseStream();
                                    readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                    String goodLocationJsonData = readerPost.ReadToEnd();
                                    Debug.WriteLine("post data(집배점기사연락처)====" + goodLocationJsonData);
                                    /************************************************/
                                    /*
                                    WebClient webClient = new WebClient();
                                    String sample = goodLocation + "?wbl_num= " + invoiceNumber;
                                    Stream stream = webClient.OpenRead(sample);
                                    String goodLocationJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                    */
                                    JArray obj = JArray.Parse(goodLocationJsonData);

                                    foreach (JObject jobj in obj)
                                    {
                                        if (jobj["wrk_cod"].ToString().Equals("50") || jobj["wrk_cod"].ToString().Equals("60") || jobj["wrk_cod"].ToString().Equals("70"))
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "네. 고객님<br>문의하신 지역의 담당기사 연락처입니다.<br>근무 외 시간에는 통화가 어려우니 참고 해주시기 바랍니다.<br>(*근무시간: 09시~18시)<br><br>담당기사: ☎ <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a><br>집배점: " + jobj["org_nam"].ToString() + " ☎ <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a><br><br>고객님께 작은 도움이 되었기를 바랍니다. 궁금하신 사항이 있으시면 언제든지 문의해주세요",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                //Text = "네. 고객님<br>해당 운송장 번호로는 연락처를 찾을 수 없습니다. 운송장 번호를 다시 한번 확인해 주세요.",
                                                Text = "네. 고객님<br>해당 정보로는 연락처를 찾을 수 없습니다. 다시 한번 정확한 정보를 확인해 주세요",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }

                                    }


                                }
                                else if (apiActiveText.Contains("주소") && apiActiveText.Contains("연락처찾기"))
                                {
                                    checkFindAddressCnt = "T";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "ADDRESSCHECK", checkFindAddressCnt);
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "고객님의 정확한 주소를 입력해 주세요.<br><br>예)<br>서울특별시 금천구 가산동 371-23<br>서울특별시 금천구 가산디지털2로 83",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Equals("주소다시입력"))
                                {
                                    checkFindAddressCnt = "T";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "ADDRESSCHECK", checkFindAddressCnt);
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "고객님의 정확한 주소를 입력해 주세요.<br><br>예)<br>서울특별시 금천구 가산동 371-23<br>서울특별시 금천구 가산디지털2로 83",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else
                                {
                                    if (checkFindAddressCnt.Equals("T")) //주소로서 데이터를 찾아야 한다
                                    {
                                        db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "ADDRESSCHECK", checkFindAddressCnt);
                                        /*
                                    * POST METHOD
                                    * */
                                        postParams = new StringBuilder();
                                        postParams.Append("address=" + apiActiveText);

                                        Encoding encoding = Encoding.UTF8;
                                        //byte[] result = encoding.GetBytes(postParams.ToString());
                                        byte[] result = Encoding.GetEncoding("ks_c_5601-1987").GetBytes(postParams.ToString());

                                        wReq = (HttpWebRequest)WebRequest.Create(findOrgInfo);
                                        wReq.Method = "POST";
                                        wReq.ContentType = "application/x-www-form-urlencoded";
                                        wReq.ContentLength = result.Length;

                                        postDataStream = wReq.GetRequestStream();
                                        postDataStream.Write(result, 0, result.Length);
                                        postDataStream.Close();

                                        wResp = (HttpWebResponse)wReq.GetResponse();
                                        respPostStream = wResp.GetResponseStream();
                                        readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                        String findOrgInfoJsonData = readerPost.ReadToEnd();
                                        Debug.WriteLine("post data(주소로서집배점찾기)====" + findOrgInfoJsonData);
                                        /************************************************/
                                        /*
                                        WebClient webClient = new WebClient();
                                        
                                        String sample = findOrgInfo + "?address=" + apiActiveText;
                                        Stream stream = webClient.OpenRead(sample);
                                        String findOrgInfoJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                        */
                                        JArray obj = JArray.Parse(findOrgInfoJsonData);
                                        foreach (JObject jobj in obj)
                                        {
                                            if (jobj["ret_cod"].ToString().Equals("9001"))//조회실패
                                            {
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                CardAction retryButton = new CardAction();
                                                retryButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "주소다시입력",
                                                    Title = "주소 다시 입력"
                                                };
                                                cardButtons.Add(retryButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "고객님! 죄송합니다.<br>입력하신 주소로 조회가 실패되었습니다",
                                                    Buttons = cardButtons,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("9999"))
                                            {
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                CardAction retryButton = new CardAction();
                                                retryButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "주소다시입력",
                                                    Title = "주소 다시 입력"
                                                };
                                                cardButtons.Add(retryButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "고객님! 죄송합니다.<br>에러가 발생되었습니다.",
                                                    Buttons = cardButtons,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("1000"))
                                            {
                                                checkFindAddressCnt = "F";
                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "ADDRESSCHECK", checkFindAddressCnt);
                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    //Text = "네~ 고객님<br>문의하신 지역의 담당기사 연락처입니다.<br>근무 외 시간에는 통화가 어려우니 참고 해주시기 바랍니다.<br>(*근무시간: 09시~18시)<br><br>담당기사: <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a><br>집배점: " + jobj["org_nam"].ToString() + " <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a><br><br>고객님께 작은 도움이 되었기를 바랍니다. 추가적으로 궁금한 사항은 언제든지 문의해 주세요.",
                                                    Text = "네~ 고객님 문의하신 지역의 담당직원 연락처입니다.<br>※ 운전 중, 근무시간이 아닌 경우 통화가 어려울 수 있습니다.<br>담당직원 : ☎ <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a><br>집배점 : " + jobj["org_nam"].ToString() + " ☎ <a href = 'tel:" + jobj["tel_num"].ToString() + "' > " + jobj["tel_num"].ToString() + " </a > ",
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);

                                                UserHeroCard defaultCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    //Text = "네~ 고객님<br>문의하신 지역의 담당기사 연락처입니다.<br>근무 외 시간에는 통화가 어려우니 참고 해주시기 바랍니다.<br>(*근무시간: 09시~18시)<br><br>담당기사: <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a><br>집배점: " + jobj["org_nam"].ToString() + " <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a><br><br>고객님께 작은 도움이 되었기를 바랍니다. 추가적으로 궁금한 사항은 언제든지 문의해 주세요.",
                                                    Text = "언제든 필요한 게 있으면 말씀해 주세요.<br><br>예) 택배예약, 배송조회, 택배요금",
                                                };

                                                plAttachment = defaultCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);

                                                SetActivity(apiMakerReply);
                                            }
                                            else
                                            {

                                            }
                                        }
                                    }
                                    else
                                    {
                                        List<CardList> text = new List<CardList>();
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction find1Button = new CardAction();
                                        find1Button = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "운송장번호로 연락처 찾기",
                                            Title = "운송장번호로 연락처 찾기"
                                        };
                                        cardButtons.Add(find1Button);

                                        CardAction find2Button = new CardAction();
                                        find2Button = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "주소로 연락처 찾기",
                                            Title = "주소로 연락처 찾기"
                                        };
                                        cardButtons.Add(find2Button);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "네~ 고객님<br>문의하시려는 항목을 아래에서 선택해<br>주세요.<br>원하시는 항목이 없으면, 이렇게 입력해<br>보세요.<br><br>예) 택배예약, 배송조회, 택배요금",
                                            Buttons = cardButtons,
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }

                                }
                            }
                            else
                            {

                            }
                            /*****************************************************************
                             * apiIntent 고객의 말씀
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_고객의말씀"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);

                                if (authCheck.Equals("F"))
                                {
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_URL", "[F_고객의말씀]::고객의말씀");
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_고객의말씀");
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction deliveryButton = new CardAction();
                                    deliveryButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "예. 휴대폰인증 하겠습니다",
                                        Title = "휴대폰 인증"
                                    };
                                    cardButtons.Add(deliveryButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "모바일앱에서 온라인 상담으로 진행합니다.<br>아래버튼을 눌러주세요.",
                                        Buttons = cardButtons,
                                    };
                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else
                                {
                                    String userPhone = userCheck[0].userPhone;
                                    String dataUserPhone = "";
                                    String[] telNumbers = dbutil.arrayStr(userPhone);
                                    for (int i = 0; i < telNumbers.Length; i++)
                                    {
                                        dataUserPhone = dataUserPhone + dbutil.getTelEnglish(telNumbers[i]);
                                    }

                                    List<CardAction> addressButtons = new List<CardAction>();
                                    CardAction addressButton = new CardAction();
                                    addressButton = new CardAction()
                                    {
                                        Type = "openUrl",
                                        Value = "https://m.hanex.hanjin.co.kr/member/ssoCTI?tel_num=" + dataUserPhone + "&auth_num=" + authNumber + "&type=B",
                                        Title = "온라인 상담"
                                    };
                                    addressButtons.Add(addressButton);

                                    UserHeroCard addressPlCard3 = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "모바일앱에서 온라인 상담으로 진행합니다.<br>아래버튼을 눌러주세요.",
                                        Buttons = addressButtons,
                                    };
                                    Attachment plAttachment = addressPlCard3.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                            }
                            else
                            {

                            }

                            /*****************************************************************
                             * apiIntent 자주하는질문
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_자주하는질문"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);

                                List<CardAction> cardButtons = new List<CardAction>();

                                CardAction guideButton1 = new CardAction();
                                guideButton1 = new CardAction()
                                {
                                    Type = "imBack",
                                    Value = "예약",
                                    Title = "예약"
                                };
                                cardButtons.Add(guideButton1);

                                CardAction guideButton2 = new CardAction();
                                guideButton2 = new CardAction()
                                {
                                    Type = "imBack",
                                    Value = "반품택배예약",
                                    Title = "반품택배예약"
                                };
                                cardButtons.Add(guideButton2);

                                CardAction guideButton3 = new CardAction();
                                guideButton3 = new CardAction()
                                {
                                    Type = "imBack",
                                    Value = "예약확인",
                                    Title = "예약확인"
                                };
                                cardButtons.Add(guideButton3);

                                CardAction guideButton4 = new CardAction();
                                guideButton4 = new CardAction()
                                {
                                    Type = "imBack",
                                    Value = "예약취소",
                                    Title = "예약취소"
                                };
                                cardButtons.Add(guideButton4);

                                CardAction guideButton5 = new CardAction();
                                guideButton5 = new CardAction()
                                {
                                    Type = "imBack",
                                    Value = "배송문의",
                                    Title = "배송문의"
                                };
                                cardButtons.Add(guideButton5);

                                CardAction guideButton6 = new CardAction();
                                guideButton6 = new CardAction()
                                {
                                    Type = "imBack",
                                    Value = "견적의뢰",
                                    Title = "견적의뢰"
                                };
                                cardButtons.Add(guideButton6);

                                CardAction guideButton7 = new CardAction();
                                guideButton7 = new CardAction()
                                {
                                    Type = "imBack",
                                    Value = "현금영수증",
                                    Title = "현금영수증"
                                };
                                cardButtons.Add(guideButton7);

                                CardAction guideButton8 = new CardAction();
                                guideButton8 = new CardAction()
                                {
                                    Type = "imBack",
                                    Value = "직원,집배점연락처",
                                    Title = "직원,집배점연락처"
                                };
                                cardButtons.Add(guideButton8);

                                CardAction guideButton9 = new CardAction();
                                guideButton9 = new CardAction()
                                {
                                    Type = "imBack",
                                    Value = "고객의말씀",
                                    Title = "온라인 상담"
                                };
                                cardButtons.Add(guideButton9);

                                UserHeroCard plCard = new UserHeroCard()
                                {
                                    Title = "",
                                    Text = "네~ 고객님<br>문의하시려는 항목을 아래에서 선택해<br>주세요.",
                                    Buttons = cardButtons,
                                };
                                Attachment plAttachment = plCard.ToAttachment();
                                apiMakerReply.Attachments.Add(plAttachment);
                                SetActivity(apiMakerReply);
                            }
                            else
                            {

                            }

                            /*****************************************************************
                             * apiIntent 위탁배송
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_위탁배송"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);

                                if (apiActiveText.Contains("운송장번호직접입력"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "정확한 운송장 번호를 입력해 주세요.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Equals("위탁배송가능여부"))
                                {
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction returnButton = new CardAction();
                                    returnButton = new CardAction()
                                    {
                                        Type = "postBack",
                                        Value = "[F_위탁배송]::운송장번호직접입력",
                                        Title = "운송장번호 직접입력"
                                    };
                                    cardButtons.Add(returnButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "네~ 고객님<br>운송장번호를 입력해 주십시오",
                                        Buttons = cardButtons,
                                    };
                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else
                                {
                                    Boolean goAPI = false;

                                    List<UserCheck> checkUserData = new List<UserCheck>();
                                    checkUserData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);
                                    String userPhone = userCheck[0].userPhone;
                                    String invoiceNm = userCheck[0].tempNumber;

                                    if (invoiceNm.Equals("") || string.IsNullOrEmpty(invoiceNm))
                                    {
                                        if (containNum == true)
                                        {
                                            goAPI = true;
                                            invoiceNm = Regex.Replace(activity.Text, @"\D", "");
                                        }
                                        else
                                        {
                                            goAPI = false;
                                        }
                                    }
                                    else
                                    {
                                        goAPI = true;
                                    }

                                    if (goAPI == true)
                                    {
                                        postParams = new StringBuilder();
                                        postParams.Append("wbl_num=" + invoiceNm);

                                        Encoding encoding = Encoding.UTF8;
                                        byte[] result = encoding.GetBytes(postParams.ToString());

                                        wReq = (HttpWebRequest)WebRequest.Create(goodLocation);
                                        wReq.Method = "POST";
                                        wReq.ContentType = "application/x-www-form-urlencoded";
                                        wReq.ContentLength = result.Length;

                                        postDataStream = wReq.GetRequestStream();
                                        postDataStream.Write(result, 0, result.Length);
                                        postDataStream.Close();

                                        wResp = (HttpWebResponse)wReq.GetResponse();
                                        respPostStream = wResp.GetResponseStream();
                                        readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                        String goodLocationJsonData = readerPost.ReadToEnd();
                                        Debug.WriteLine("post data(위탁배송데이터)====" + goodLocationJsonData);
                                        db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "TEMP_NUMBER", "");//정보나오고 기존 운송장번호 삭제
                                        /************************************************/
                                        /*
                                        WebClient webClient = new WebClient();
                                        String sample = goodLocation + "?wbl_num= " + invoiceNumber;
                                        Stream stream = webClient.OpenRead(sample);
                                        String goodLocationJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                        */
                                        JArray obj = JArray.Parse(goodLocationJsonData);

                                        String wrkCod = "";//상태코드
                                        String orgNam = ""; //집배점명
                                        String telNum = ""; //전화번호
                                        String empTel = ""; //배송직원전화번호
                                        String statusText = "";

                                        foreach (JObject jobj in obj)
                                        {
                                            if (jobj["wrk_cod"].ToString().Equals("10"))
                                            {
                                                wrkCod = "상품접수";
                                                statusText = "네~ 고객님<br><strong><font color='#0101DF'>" + wrkCod + "</font></strong>중으로 위탁장소 전달 가능 상태가 아닙니다.	<br>위탁장소 배송 가능여부는 배송직원에게 상품이 전달된 이후 확인이 가능합니다.<br>배송지역 상품 도착여부 확인 후 재문의 부탁드릴게요.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("20"))
                                            {
                                                wrkCod = "상품발송대기중";
                                                statusText = "네~ 고객님<br><strong><font color='#0101DF'>" + wrkCod + "</font></strong>중으로 위탁장소 전달 가능 상태가 아닙니다.	<br>위탁장소 배송 가능여부는 배송직원에게 상품이 전달된 이후 확인이 가능합니다.<br>배송지역 상품 도착여부 확인 후 재문의 부탁드릴게요.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("30"))
                                            {
                                                wrkCod = "이동중";
                                                statusText = "네~ 고객님<br><strong><font color='#0101DF'>" + wrkCod + "</font></strong>중으로 위탁장소 전달 가능 상태가 아닙니다.	<br>위탁장소 배송 가능여부는 배송직원에게 상품이 전달된 이후 확인이 가능합니다.<br>배송지역 상품 도착여부 확인 후 재문의 부탁드릴게요.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("40"))
                                            {
                                                wrkCod = "배송준비";
                                                statusText = "네~ 고객님<br>위탁장소 배송 가능여부 확인은 집배점 또는 배송직원에게 확인해주시기 바랍니다.<br>고객님의 요청하신 장소에 배송 후 분실시 상품에 대한 변상이 어렵고,<br>고객님께서 직접적인 피해를 보실수있사오니, 신중히 요청해 주시기 바랍니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("50"))
                                            {
                                                wrkCod = "배송중";
                                                String tempDate = jobj["wrk_ymd"].ToString();
                                                String yearText = tempDate.Substring(0, 4);
                                                String monthText = tempDate.Substring(4, 2);
                                                String dayText = tempDate.Substring(6, 2);
                                                String dateText = yearText + "년 " + monthText + "월 " + dayText + "일";

                                                orgNam = jobj["org_nam"].ToString();
                                                //telNum = jobj["tel_num"].ToString();
                                                //empTel = jobj["emp_tel"].ToString();
                                                telNum = " <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>";
                                                empTel = " <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a>";

                                                statusText = "네~ 고객님<br>위탁장소 배송 가능여부 확인은 집배점 또는 배송직원에게 확인해주시기 바랍니다.<br>고객님의 요청하신 장소에 배송 후 분실시 상품에 대한 변상이 어렵고,<br>고객님께서 직접적인 피해를 보실수있사오니, 신중히 요청해 주시기 바랍니다.<br><br>· 집배점 : " + orgNam + " ☎ <strong>" + telNum + "</strong><br>· 배송직원 : ☎ <strong>" + empTel + "</strong><br>※ 운전중이거나 근무시간이 아닌 경우 통화가 어려울 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("60"))
                                            {
                                                wrkCod = "배송완료";
                                                String tempDate = jobj["wrk_ymd"].ToString();
                                                String yearText = tempDate.Substring(0, 4);
                                                String monthText = tempDate.Substring(4, 2);
                                                String dayText = tempDate.Substring(6, 2);
                                                String dateText = yearText + "년 " + monthText + "월 " + dayText + "일";

                                                orgNam = jobj["org_nam"].ToString();
                                                telNum = " <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>";
                                                empTel = " <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a>";

                                                statusText = "네~ 고객님<br>문의하신 운송장 번호 <strong><font color='#0101DF'>(" + invoiceNm + ")</font></strong> 번은 <strong><font color='#0101DF'>" + dateText + " 배송완료</font></strong>되어 위탁배송이 가능 상태가 아닙니다.<br><br>상품을 받지 못하셨거나, 다른 문의 사항이 있으신 경우<br>집배점 또는 배송직원에게 확인해주시기 바랍니다.<br><br>· 집배점 : " + orgNam + " ☎ <strong>" + telNum + "</strong><br>· 배송직원 : ☎ <strong>" + empTel + "</strong><br>※ 운전중이거나 근무시간이 아닌 경우 통화가 어려울 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("70"))
                                            {
                                                wrkCod = "오도착";
                                                statusText = "[오도착]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNm + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>경유중</font></strong><br>현재 배송지역이 아닌 다른 지역에 경유 중으로 배송은 1~2일이 더 소요될 수 있는 점 양해 부탁드립니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("80"))
                                            {
                                                wrkCod = "미배송";
                                                orgNam = jobj["org_nam"].ToString();
                                                telNum = " <a href='tel:" + jobj["tel_num"].ToString() + "'>" + jobj["tel_num"].ToString() + "</a>";
                                                empTel = " <a href='tel:" + jobj["emp_tel"].ToString() + "'>" + jobj["emp_tel"].ToString() + "</a>";

                                                statusText = "[미배송]<br><strong>· 운송장번호 : <font color='#0101DF'>(" + invoiceNm + ")</font></strong><br>· 배송상태 : <strong><font color='#0101DF'>배송중</font></strong><br>· 집배점 : " + orgNam + " ☎ <strong>" + telNum + "</strong><br>· 배송직원 : ☎ <strong>" + empTel + "</strong><br>자세한 문의 사항은 집배점 또는 배송직원에게 확인해주시기 바랍니다.<br>※ 운전중이거나 근무시간이 아닌 경우 통화가 어려울 수 있습니다.";

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else
                                            {
                                                wrkCod = "오류";
                                                statusText = "고객님께서 문의하신 운송장 번호 <strong><font color='#0101DF'>(" + invoiceNm + ")</font></strong>는 배송조회가 되지 않습니다.<br>문의 내용이 있으시면 아래 버튼을 눌러주세요.";

                                                List<CardAction> cardButtons = new List<CardAction>();

                                                CardAction returnButton = new CardAction();
                                                returnButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "고객의말씀",
                                                    Title = "온라인 상담"
                                                };
                                                cardButtons.Add(returnButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = statusText,
                                                    Buttons = cardButtons,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction Button1 = new CardAction();
                                        Button1 = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "배송조회",
                                            Title = "배송조회"
                                        };
                                        cardButtons.Add(Button1);

                                        CardAction Button2 = new CardAction();
                                        Button2 = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "위탁배송가능여부",
                                            Title = "위탁배송 가능여부 확인"
                                        };
                                        cardButtons.Add(Button2);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "네~ 고객님<br>문의하시려는 항목을 아래에서 선택해<br>주세요.<br>원하시는 항목이 없으면, 이렇게 입력해<br>보세요.<br><br>예) 택배예약, 배송조회, 택배요금",
                                            Buttons = cardButtons,
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                }
                                
                            }
                            else
                            {

                            }

                            /*****************************************************************
                            * apiIntent F_모바일 인증
                            * 
                            ************************************************************** */
                            if (apiIntent.Equals("F_모바일인증"))
                            {
                                replyresult = "H";
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Contains("예휴대폰인증"))
                                {
                                    checkAuthNameCnt = "F";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "NAMECHECK", checkAuthNameCnt); //인증 이름부분
                                    List<CardList> text = new List<CardList>();
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction find1Button = new CardAction();
                                    find1Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "동의",
                                        Title = "동의"
                                    };
                                    cardButtons.Add(find1Button);

                                    CardAction find2Button = new CardAction();
                                    find2Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "미동의",
                                        Title = "미동의"
                                    };
                                    cardButtons.Add(find2Button);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "휴대폰번호 인증에 동의하시면 고객님의 택배목록을 조회/제공하기 위한 목적으로만 활용되며, 별도 보관하지 않습니다.<br><br>또한, 인증에 동의하시더라도 목록이 없는 경우 운송장번호를 직접 입력해 주셔야 합니다.<br><br>인증 절차에 동의하시겠습니까?",
                                        Buttons = cardButtons,
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("아니오휴대폰인증"))
                                {
                                    checkAuthNameCnt = "F";
                                    apiIntent = "None";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiIntent);
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "NAMECHECK", checkAuthNameCnt); //인증 이름부분
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "휴대폰 인증 진행이 취소되었습니다.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Equals("동의"))
                                {
                                    postParams = new StringBuilder();
                                    postParams.Append("gbn_cod=CHATBOT");
                                    postParams.Append("&tel_num=" + requestPhone);
                                    //postParams.Append("&req_nam=" + authName);

                                    Encoding encoding = Encoding.UTF8;
                                    //byte[] result = encoding.GetBytes(postParams.ToString());
                                    byte[] result = Encoding.GetEncoding("ks_c_5601-1987").GetBytes(postParams.ToString());

                                    wReq = (HttpWebRequest)WebRequest.Create(requestAuth);
                                    wReq.Method = "POST";
                                    wReq.ContentType = "application/x-www-form-urlencoded";
                                    wReq.ContentLength = result.Length;

                                    postDataStream = wReq.GetRequestStream();
                                    postDataStream.Write(result, 0, result.Length);
                                    postDataStream.Close();

                                    wResp = (HttpWebResponse)wReq.GetResponse();
                                    respPostStream = wResp.GetResponseStream();
                                    readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);

                                    String requestAuthJsonData = readerPost.ReadToEnd();
                                    Debug.WriteLine("post data(인증시 전화번호로 하기)====" + requestAuthJsonData);

                                    JArray obj = JArray.Parse(requestAuthJsonData);
                                    foreach (JObject jobj in obj)
                                    {
                                        if (jobj["ret_cod"].ToString().Equals("1000"))
                                        {
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_NUMBER", jobj["auth_num"].ToString()); //AUTH_NUMBER UPDATE
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "USER_NAME", authName); //AUTH_NAME UPDATE
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction authButton = new CardAction();
                                            authButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "인증확인",
                                                Title = "인증확인"
                                            };
                                            cardButtons.Add(authButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "요청하신 인증번호는 <strong>" + jobj["auth_num"].ToString() + "</strong> 입니다<br>인증확인버튼을 클릭하시면 인증이 완료됩니다.",
                                                Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9031"))
                                        {
                                            checkAuthNameCnt = "F";
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "NAMECHECK", checkAuthNameCnt); //인증 이름부분
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "최근 30일 이내 고객님의 휴대폰 번호로 조회되는 배송/집하목록이 없어 인증되지 않았습니다.<br>불편을 드려 죄송합니다",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9032"))
                                        {
                                            checkAuthNameCnt = "F";
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "NAMECHECK", checkAuthNameCnt); //인증 이름부분
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "동일한 전화번호로 3회 초과요청되었기에 인증되지 않았습니다.<br>불편을 드려 죄송합니다",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            checkAuthNameCnt = "F";
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "NAMECHECK", checkAuthNameCnt); //인증 이름부분
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "인증되지 않았습니다.<br>불편을 드려 죄송합니다.",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                    }
                                }

                                else if (apiActiveText.Equals("미동의"))
                                {
                                    checkAuthNameCnt = "F";
                                    db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "NAMECHECK", checkAuthNameCnt); //인증 이름부분
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "고객님의 휴대폰 번호를 기준으로 배송정보를 조회하기 위해 인증절차가 필요합니다.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Equals("인증확인"))
                                {
                                    postParams = new StringBuilder();
                                    postParams.Append("tel_num=" + requestPhone);
                                    //postParams.Append("&req_nam=" + authName);
                                    postParams.Append("&auth_num=" + authNumber);

                                    Encoding encoding = Encoding.UTF8;
                                    //byte[] result = encoding.GetBytes(postParams.ToString());
                                    byte[] result = Encoding.GetEncoding("ks_c_5601-1987").GetBytes(postParams.ToString());

                                    wReq = (HttpWebRequest)WebRequest.Create(responseAuth);
                                    wReq.Method = "POST";
                                    wReq.ContentType = "application/x-www-form-urlencoded";
                                    wReq.ContentLength = result.Length;

                                    postDataStream = wReq.GetRequestStream();
                                    postDataStream.Write(result, 0, result.Length);
                                    postDataStream.Close();

                                    wResp = (HttpWebResponse)wReq.GetResponse();
                                    respPostStream = wResp.GetResponseStream();
                                    readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                    String responseAuthJsonData = readerPost.ReadToEnd();
                                    Debug.WriteLine("post data(인증확인)====" + responseAuthJsonData);

                                    JArray obj = JArray.Parse(responseAuthJsonData);
                                    foreach (JObject jobj in obj)
                                    {
                                        if (jobj["ret_cod"].ToString().Equals("1000"))
                                        {
                                            authCheck = "T";//인증성공
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_CHECK", authCheck); //AUTH_CHECK UPDATE
                                            checkAuthNameCnt = "F";
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "NAMECHECK", checkAuthNameCnt); //인증 이름부분
                                            apiOldIntent = "";
                                            authUrl = uData[0].authUrl;
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction list1Button = new CardAction();
                                            list1Button = new CardAction()
                                            {
                                                Type = "postBack",
                                                Value = authUrl,
                                                Title = "택배배송목록"
                                            };
                                            cardButtons.Add(list1Button);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "인증되었습니다.",
                                                //Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            /*
                                            * 인증완료시에 리스트가 바로 나오도록 한다.
                                            * */
                                            if (authUrl.Equals("[F_예약]::택배배송목록"))
                                            {
                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_예약");
                                                int totalPage1 = 0;
                                                if (apiActiveText.Contains("반품택배예약다음페이지") && activity.Text.Contains(">>"))
                                                {
                                                    deliveryListPageNum++;
                                                }
                                                else if (apiActiveText.Contains("반품택배예약이전페이지") && activity.Text.Contains("<<"))
                                                {
                                                    deliveryListPageNum--;
                                                }
                                                else
                                                {
                                                    deliveryListPageNum = 1;
                                                }

                                                List<CardAction> startCardButtons = new List<CardAction>();
                                                CardAction startPlButton = new CardAction();
                                                startPlButton = new CardAction()
                                                {
                                                    Type = "postBack",
                                                    Value = "[F_예약]::운송장번호직접입력",
                                                    Title = "운송장번호 직접입력"
                                                };
                                                startCardButtons.Add(startPlButton);

                                                UserHeroCard startCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "반품택배예약을 원하시는 상품을 목록에서 선택해주세요.<br>해당 상품이 목록에 없는 경우, 운송장번호를 직접 입력해 주세요.",
                                                    Buttons = startCardButtons,
                                                };

                                                Attachment plAttachment1 = startCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment1);


                                                postParams = new StringBuilder();
                                                //postParams.Append("tel_num=" + requestPhone);
                                                postParams.Append("auth_num=" + authNumber);
                                                postParams.Append("&pag_num=" + deliveryListPageNum);
                                                postParams.Append("&pag_cnt=" + pageCnt);

                                                Encoding encoding1 = Encoding.UTF8;
                                                byte[] result1 = encoding1.GetBytes(postParams.ToString());

                                                wReq = (HttpWebRequest)WebRequest.Create(DeliveryList);
                                                wReq.Method = "POST";
                                                wReq.ContentType = "application/x-www-form-urlencoded";
                                                wReq.ContentLength = result1.Length;

                                                postDataStream = wReq.GetRequestStream();
                                                postDataStream.Write(result1, 0, result1.Length);
                                                postDataStream.Close();

                                                wResp = (HttpWebResponse)wReq.GetResponse();
                                                respPostStream = wResp.GetResponseStream();
                                                readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                                String DeliveryListJsonData = readerPost.ReadToEnd();
                                                Debug.WriteLine("post data(인증확인후반품택배예약목록)====" + DeliveryListJsonData);

                                                JArray obj1 = JArray.Parse(DeliveryListJsonData);

                                                foreach (JObject jobj1 in obj1)
                                                {
                                                    if (jobj1["ret_cod"].ToString().Equals("9041"))
                                                    {
                                                        totalPage1 = 1;
                                                        UserHeroCard plCard1 = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "인증번호에 오류가 발생되었습니다."
                                                        };
                                                        plAttachment = plCard1.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else if (jobj1["ret_cod"].ToString().Equals("9042"))
                                                    {
                                                        totalPage1 = 1;
                                                        UserHeroCard plCard1 = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                        };
                                                        plAttachment = plCard1.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else if (jobj1["ret_cod"].ToString().Equals("9999"))
                                                    {
                                                        totalPage1 = 1;
                                                        UserHeroCard plCard1 = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "시스템 오류 또는 요청항목이 누락되었습니다."
                                                        };
                                                        plAttachment = plCard1.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else
                                                    {
                                                        List<CardAction> cardButtons1 = new List<CardAction>();

                                                        totalPage1 = Convert.ToInt32(jobj1["tot_pag"].ToString());
                                                        deliveryListPageNum = Convert.ToInt32(jobj1["pag_num"].ToString());

                                                        String tempDate = jobj1["dlv_ymd"].ToString();
                                                        String dateText = "";
                                                        if (tempDate == "" || tempDate.Equals(""))
                                                        {
                                                            //dateText = "<div class=\"endDate\"><span class=\"dateDay\"><small>배송중</small></span><span class=\"dateWeek\"></span></div>";
                                                            dateText = "";
                                                        }
                                                        else
                                                        {
                                                            String yearText = tempDate.Substring(0, 4);
                                                            String monthText = tempDate.Substring(4, 2);
                                                            String dayText = tempDate.Substring(6, 2);
                                                            dateText = "<strong>· 배송완료일자 : </strong > " + yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj1["dlv_dy"].ToString() + ")<br>";
                                                            //dateText = "<div class=\"endDate\"><span class=\"dateDay\">" + monthText + "." + dayText + "</span><span class=\"dateWeek\">" + jobj1["dlv_dy"].ToString() + "요일</span></div>";
                                                        }
                                                        //배송상태 처리
                                                        String deliveryStatus = jobj1["wrk_nam"].ToString();
                                                        String deliveryStatusText = "상품접수";
                                                        if (deliveryStatus.Equals("10"))
                                                        {
                                                            deliveryStatusText = "상품접수";
                                                        }
                                                        else if (deliveryStatus.Equals("20"))
                                                        {
                                                            deliveryStatusText = "상품발송대기중";
                                                        }
                                                        else if (deliveryStatus.Equals("30"))
                                                        {
                                                            deliveryStatusText = "이동중";
                                                        }
                                                        else if (deliveryStatus.Equals("40"))
                                                        {
                                                            deliveryStatusText = "배송준비";
                                                        }
                                                        else if (deliveryStatus.Equals("50"))
                                                        {
                                                            deliveryStatusText = "배송중";
                                                        }
                                                        else
                                                        {
                                                            deliveryStatusText = "배송완료";
                                                        }

                                                        String goodNameTemp = jobj1["god_nam"].ToString();
                                                        int goodNameLength = jobj1["god_nam"].ToString().Length;
                                                        String goodName = "";
                                                        if (goodNameLength > 20)
                                                        {
                                                            goodName = goodNameTemp.Substring(0, 15) + "....";
                                                        }
                                                        else
                                                        {
                                                            goodName = goodNameTemp;
                                                        }


                                                        CardAction plButton = new CardAction();
                                                        plButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "운송장번호 " + jobj1["wbl_num"].ToString() + " 반품택배예약",
                                                            Title = "반품택배예약",
                                                        };
                                                        cardButtons1.Add(plButton);

                                                        UserHeroCard plCard1 = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = dateText + "<strong>· 배송상태 : <font color='#0101DF'>" + deliveryStatusText + "</font></strong><br><strong>· 상품명 : </strong><font color='#585858'>" + goodName + "</font><br><strong>· 운송장번호 : <font color='#0101DF'>(" + jobj1["wbl_num"].ToString() + ")</font></strong><br><strong>· 송하인명 : </strong>" + jobj1["snd_nam"].ToString(),
                                                            //Text = "<div class=\"takeBack\">" + dateText + "<div class=\"prodInfo\"><span class=\"prodName\">" + goodName + "</span><span class=\"prodNum\">" + jobj1["wbl_num"].ToString() + "/" + jobj1["snd_nam"].ToString() + "</span><span class=\"prodStatus\">" + deliveryStatusText + "</span></div></div>",
                                                            Tap = plButton
                                                        };
                                                        plAttachment = plCard1.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }

                                                }

                                                if (totalPage1 == 1)
                                                {
                                                    //페이징 없음
                                                }
                                                else
                                                {
                                                    List<CardAction> pageButtons = new List<CardAction>();
                                                    //전체페이지와 동일하면 다음버튼은 없다
                                                    if (deliveryListPageNum == totalPage1)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        CardAction nextButton = new CardAction();
                                                        nextButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "반품택배예약 다음페이지>>",
                                                            Title = "다음페이지",
                                                        };
                                                        pageButtons.Add(nextButton);
                                                    }
                                                    //현재 페이지가 1이면 이전버튼은 없다.
                                                    if (deliveryListPageNum < 2)
                                                    {

                                                    }
                                                    else
                                                    {
                                                        CardAction prevButton = new CardAction();
                                                        prevButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "반품택배예약 이전페이지<<",
                                                            Title = "이전페이지",
                                                        };
                                                        pageButtons.Add(prevButton);
                                                    }


                                                    UserHeroCard pageCard = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "",
                                                        Buttons = pageButtons,
                                                    };
                                                    plAttachment = pageCard.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment);
                                                }
                                            }
                                            else if (authUrl.Equals("[F_예약확인]::나의예약확인"))
                                            {
                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_예약확인");
                                                int totalPage = 0;
                                                if (apiActiveText.Contains("예약목록다음페이지") && activity.Text.Contains(">>"))
                                                {
                                                    collectionListPageNum++;
                                                }
                                                else if (apiActiveText.Contains("예약목록이전페이지") && activity.Text.Contains("<<"))
                                                {
                                                    collectionListPageNum--;
                                                }
                                                else
                                                {
                                                    collectionListPageNum = 1;
                                                }

                                                /*
                                                * POST METHOD
                                                * */
                                                postParams = new StringBuilder();
                                                //postParams.Append("tel_num=" + requestPhone);
                                                postParams.Append("auth_num=" + authNumber);
                                                postParams.Append("&pag_num=" + collectionListPageNum);
                                                postParams.Append("&pag_cnt=" + pageCnt);

                                                Encoding encoding2 = Encoding.UTF8;
                                                byte[] result2 = encoding2.GetBytes(postParams.ToString());

                                                wReq = (HttpWebRequest)WebRequest.Create(DeliveryCollection);
                                                wReq.Method = "POST";
                                                wReq.ContentType = "application/x-www-form-urlencoded";
                                                wReq.ContentLength = result2.Length;

                                                postDataStream = wReq.GetRequestStream();
                                                postDataStream.Write(result2, 0, result2.Length);
                                                postDataStream.Close();

                                                wResp = (HttpWebResponse)wReq.GetResponse();
                                                respPostStream = wResp.GetResponseStream();
                                                readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                                String DeliveryCollectionJsonData = readerPost.ReadToEnd();
                                                Debug.WriteLine("post data DeliveryCollection====" + DeliveryCollectionJsonData);

                                                JArray obj2 = JArray.Parse(DeliveryCollectionJsonData);
                                                int checkInt = obj2.Count;

                                                if (checkInt == 0)
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard1 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                    };

                                                    Attachment plAttachment2 = plCard1.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment2);
                                                }
                                                else
                                                {
                                                    List<CardAction> numberButtons = new List<CardAction>();
                                                    CardAction numberButton = new CardAction();
                                                    numberButton = new CardAction()
                                                    {
                                                        Type = "postBack",
                                                        Value = "[F_예약확인]::운송장예약번호직접입력",
                                                        Title = "운송장/예약번호 직접입력"
                                                    };
                                                    numberButtons.Add(numberButton);


                                                    UserHeroCard plCard2 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "아래의 목록에서 문의하고자 하시는 상품의 예약내용확인을 선택해 주세요.<br>해당 예약번호가 목록에 없는 경우,<br>예약번호 또는 운송장번호를 직접 입력해 주세요",
                                                        Buttons = numberButtons,
                                                    };
                                                    Attachment plAttachment2 = plCard2.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment2);

                                                    foreach (JObject jobj2 in obj2)
                                                    {
                                                        if (jobj2["ret_cod"].ToString().Equals("9051"))
                                                        {
                                                            totalPage = 1;
                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "인증번호 오류입니다. 다시 한번 시도해 주세요."
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else if (jobj2["ret_cod"].ToString().Equals("9052"))
                                                        {
                                                            totalPage = 1;
                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "고객님의 휴대폰 번호로 조회된 결과가 없습니다."
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else if (jobj2["ret_cod"].ToString().Equals("9999"))
                                                        {
                                                            totalPage = 1;
                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "기타에러가 발생되었습니다."
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else
                                                        {
                                                            List<CardAction> cardButtons2 = new List<CardAction>();

                                                            totalPage = Convert.ToInt32(jobj2["tot_pag"].ToString());
                                                            collectionListPageNum = Convert.ToInt32(jobj2["pag_num"].ToString());

                                                            String tempDate = jobj2["wrk_ymd"].ToString();
                                                            String dateText = tempDate;
                                                            String cardShowText = "";

                                                            String goodNameTemp = jobj2["god_nam"].ToString();
                                                            int goodNameLength = jobj2["god_nam"].ToString().Length;
                                                            String goodName = "";
                                                            if (goodNameLength > 20)
                                                            {
                                                                goodName = goodNameTemp.Substring(0, 15) + "....";
                                                            }
                                                            else
                                                            {
                                                                goodName = goodNameTemp;
                                                            }

                                                            if (tempDate == "" || tempDate.Equals(""))
                                                            {
                                                                dateText = "미할당";
                                                                cardShowText = "<strong>·예약번호 : </strong><strong><font color='#0101DF'>(" + jobj2["rsv_num"].ToString() + ")</font></strong><br><strong>· 상품명 : </strong>" + goodName + " <br><strong>·수하인명 : </strong>" + jobj2["rcv_nam"].ToString() + " <br><strong>·예약상태 : </strong><font color='#0101DF'>" + jobj2["wrk_nam"].ToString() + "</font>";
                                                            }
                                                            else
                                                            {
                                                                String yearText = tempDate.Substring(0, 4);
                                                                String monthText = tempDate.Substring(4, 2);
                                                                String dayText = tempDate.Substring(6, 2);
                                                                dateText = yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj2["wrk_dy"].ToString() + ")";
                                                                cardShowText = "<strong>·예약번호 : </strong><strong><font color='#0101DF'>(" + jobj2["rsv_num"].ToString() + ")</font></strong><br><strong>· 상품명 : </strong>" + goodName + " <br><strong>·수하인명 : </strong>" + jobj2["rcv_nam"].ToString() + " <br><strong>·예약상태 : </strong><font color='#0101DF'>" + jobj2["wrk_nam"].ToString() + "</font><br><strong>·날짜 : </strong>" + dateText;
                                                            }

                                                            CardAction bookButton = new CardAction();
                                                            bookButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = jobj2["rsv_num"].ToString() + " 예약 내용 확인",
                                                                Title = "예약 내용 확인"
                                                            };
                                                            cardButtons2.Add(bookButton);

                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = cardShowText,
                                                                Buttons = cardButtons2,
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        /*

                                                        */

                                                    }

                                                    if (totalPage == 1)
                                                    {
                                                        //페이징 없음
                                                    }
                                                    else
                                                    {
                                                        List<CardAction> pageButtons = new List<CardAction>();
                                                        //전체페이지와 동일하면 다음버튼은 없다
                                                        if (collectionListPageNum == totalPage)
                                                        {

                                                        }
                                                        else
                                                        {
                                                            CardAction nextButton = new CardAction();
                                                            nextButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "예약목록 다음페이지>>",
                                                                Title = "다음페이지",
                                                            };
                                                            pageButtons.Add(nextButton);
                                                        }
                                                        //현재 페이지가 1이면 이전버튼은 없다.
                                                        if (collectionListPageNum < 2)
                                                        {

                                                        }
                                                        else
                                                        {
                                                            CardAction prevButton = new CardAction();
                                                            prevButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "예약목록 이전페이지<<",
                                                                Title = "이전페이지",
                                                            };
                                                            pageButtons.Add(prevButton);
                                                        }


                                                        UserHeroCard pageCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "",
                                                            Buttons = pageButtons,
                                                        };
                                                        plAttachment = pageCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                }

                                            }
                                            else if (authUrl.Equals("[F_예약]::다른주소로변경반품"))
                                            {
                                                List<UserCheck> checkUserData = new List<UserCheck>();
                                                checkUserData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);
                                                String userPhone = userCheck[0].userPhone;
                                                String invoiceNm = userCheck[0].tempNumber;

                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "TEMP_NUMBER", "");//정보나오고 기존 운송장번호 삭제

                                                List<CardAction> addressButtons = new List<CardAction>();
                                                CardAction addressButton = new CardAction();
                                                addressButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "운송장 번호 " + invoiceNm + " 다른 주소로 변경 반품",
                                                    Title = "다른 주소로 변경 반품"
                                                };
                                                addressButtons.Add(addressButton);

                                                UserHeroCard addressPlCard3 = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "운송장 번호 " + invoiceNm + "를 다른 주소로 반품처리하시겠습니까?",
                                                    Buttons = addressButtons,
                                                };
                                                plAttachment = addressPlCard3.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                            else if (authUrl.Equals("[F_고객의말씀]::고객의말씀"))
                                            {
                                                String userPhone = userCheck[0].userPhone;
                                                String dataUserPhone = "";
                                                String[] telNumbers = dbutil.arrayStr(userPhone);
                                                for (int i = 0; i < telNumbers.Length; i++)
                                                {
                                                    dataUserPhone = dataUserPhone + dbutil.getTelEnglish(telNumbers[i]);
                                                }

                                                List<CardAction> addressButtons = new List<CardAction>();
                                                CardAction addressButton = new CardAction();
                                                addressButton = new CardAction()
                                                {
                                                    Type = "openUrl",
                                                    Value = "https://m.hanex.hanjin.co.kr/member/ssoCTI?tel_num=" + dataUserPhone + "&auth_num=" + authNumber + "&type=B",
                                                    Title = "온라인 상담"
                                                };
                                                addressButtons.Add(addressButton);

                                                UserHeroCard addressPlCard3 = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "모바일앱에서 온라인 상담으로 진행합니다.<br>아래버튼을 눌러주세요.",
                                                    Buttons = addressButtons,
                                                };
                                                plAttachment = addressPlCard3.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }

                                            else if (authUrl.Equals("[F_예약취소]::나의예약취소"))
                                            {
                                                /************************************************/
                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_예약취소");
                                                int totalPage = 0;
                                                if (apiActiveText.Contains("예약취소목록다음페이지") && activity.Text.Contains(">>"))
                                                {
                                                    collectionListPageNum++;
                                                }
                                                else if (apiActiveText.Contains("예약취소목록이전페이지") && activity.Text.Contains("<<"))
                                                {
                                                    collectionListPageNum--;
                                                }
                                                else
                                                {
                                                    collectionListPageNum = 1;
                                                }

                                                /*
                                                * POST METHOD
                                                * */
                                                postParams = new StringBuilder();
                                                //postParams.Append("tel_num=" + requestPhone);
                                                postParams.Append("auth_num=" + authNumber);
                                                postParams.Append("&pag_num=" + collectionListPageNum);
                                                postParams.Append("&pag_cnt=" + pageCnt);

                                                Encoding encoding3 = Encoding.UTF8;
                                                byte[] result3 = encoding3.GetBytes(postParams.ToString());

                                                wReq = (HttpWebRequest)WebRequest.Create(DeliveryCollection);
                                                wReq.Method = "POST";
                                                wReq.ContentType = "application/x-www-form-urlencoded";
                                                wReq.ContentLength = result3.Length;

                                                postDataStream = wReq.GetRequestStream();
                                                postDataStream.Write(result3, 0, result3.Length);
                                                postDataStream.Close();

                                                wResp = (HttpWebResponse)wReq.GetResponse();
                                                respPostStream = wResp.GetResponseStream();
                                                readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                                String DeliveryCollectionJsonData = readerPost.ReadToEnd();
                                                Debug.WriteLine("post data(인증후예약취소목록)====" + DeliveryCollectionJsonData);
                                                /************************************************/
                                                /*
                                                WebClient webClient = new WebClient();
                                                String sample = DeliveryCollection + "?tel_num=" + requestPhone + "&pag_num=" + collectionListPageNum + "&pag_cnt=" + pageCnt;
                                                Stream stream = webClient.OpenRead(sample);
                                                String DeliveryCollectionJsonData = new StreamReader(stream, Encoding.GetEncoding("ks_c_5601-1987"), true).ReadToEnd();
                                                */
                                                JArray obj3 = JArray.Parse(DeliveryCollectionJsonData);
                                                int checkInt = obj3.Count;

                                                if (checkInt == 0)
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard3 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                    };

                                                    Attachment plAttachment3 = plCard3.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment3);
                                                }
                                                else
                                                {
                                                    List<CardAction> numberButtons = new List<CardAction>();
                                                    CardAction numberButton = new CardAction();
                                                    numberButton = new CardAction()
                                                    {
                                                        Type = "postBack",
                                                        Value = "[F_예약취소]::예약번호직접입력",
                                                        Title = "예약번호 직접입력"
                                                    };
                                                    numberButtons.Add(numberButton);


                                                    UserHeroCard plCard3 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "아래 목록에서 예약취소 하실 상품의 예약번호를 선택해 주세요.<br>해당 예약번호가 목록에 없는 경우,<br>예약번호를 직접 입력해 주세요.",
                                                        Buttons = numberButtons,
                                                    };
                                                    Attachment plAttachment3 = plCard3.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment3);

                                                    foreach (JObject jobj3 in obj3)
                                                    {
                                                        if (jobj3["ret_cod"].ToString().Equals("9052"))
                                                        {
                                                            totalPage = 1;
                                                            plCard3 = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                            };
                                                            plAttachment = plCard3.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else
                                                        {
                                                            List<CardAction> cardButtons3 = new List<CardAction>();

                                                            totalPage = Convert.ToInt32(jobj3["tot_pag"].ToString());
                                                            collectionListPageNum = Convert.ToInt32(jobj3["pag_num"].ToString());

                                                            String tempDate = jobj3["wrk_ymd"].ToString();
                                                            String dateText = tempDate;
                                                            String cardShowText = "";

                                                            String goodNameTemp = jobj3["god_nam"].ToString();
                                                            int goodNameLength = jobj3["god_nam"].ToString().Length;
                                                            String goodName = "";
                                                            if (goodNameLength > 20)
                                                            {
                                                                goodName = goodNameTemp.Substring(0, 15) + "....";
                                                            }
                                                            else
                                                            {
                                                                goodName = goodNameTemp;
                                                            }

                                                            if (tempDate == "" || tempDate.Equals(""))
                                                            {
                                                                dateText = "미할당";
                                                                cardShowText = "<strong>·예약번호 : </strong><strong><font color='#0101DF'>(" + jobj3["rsv_num"].ToString() + ")</font></strong><br><strong>· 상품명 : </strong>" + goodName + " <br><strong>·수하인명 : </strong>" + jobj3["rcv_nam"].ToString() + " <br><strong>·예약상태 : </strong><font color='#0101DF'>" + jobj3["wrk_nam"].ToString() + "</font>";
                                                            }
                                                            else
                                                            {
                                                                String yearText = tempDate.Substring(0, 4);
                                                                String monthText = tempDate.Substring(4, 2);
                                                                String dayText = tempDate.Substring(6, 2);
                                                                dateText = yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj3["wrk_dy"].ToString() + ")";
                                                                cardShowText = "<strong>·예약번호 : </strong><strong><font color='#0101DF'>(" + jobj3["rsv_num"].ToString() + ")</font></strong><br><strong>· 상품명 : </strong>" + goodName + " <br><strong>·수하인명 : </strong>" + jobj3["rcv_nam"].ToString() + " <br><strong>·예약상태 : </strong><font color='#0101DF'>" + jobj3["wrk_nam"].ToString() + "</font><br><strong>·날짜 : </strong>" + dateText;
                                                            }

                                                            CardAction bookButton = new CardAction();
                                                            bookButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = jobj3["rsv_num"].ToString() + " 예약 취소 확인",
                                                                Title = "예약 취소"
                                                            };
                                                            cardButtons3.Add(bookButton);

                                                            plCard3 = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = cardShowText,
                                                                Buttons = cardButtons3,
                                                            };
                                                            plAttachment = plCard3.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }

                                                    }
                                                    if (totalPage == 1)
                                                    {
                                                        //페이징 없음
                                                    }
                                                    else
                                                    {
                                                        List<CardAction> pageButtons = new List<CardAction>();
                                                        //전체페이지와 동일하면 다음버튼은 없다
                                                        if (collectionListPageNum == totalPage)
                                                        {

                                                        }
                                                        else
                                                        {
                                                            CardAction nextButton = new CardAction();
                                                            nextButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "예약취소목록 다음페이지>>",
                                                                Title = "다음페이지",
                                                            };
                                                            pageButtons.Add(nextButton);
                                                        }
                                                        //현재 페이지가 1이면 이전버튼은 없다.
                                                        if (collectionListPageNum < 2)
                                                        {

                                                        }
                                                        else
                                                        {
                                                            CardAction prevButton = new CardAction();
                                                            prevButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "예약취소목록 이전페이지<<",
                                                                Title = "이전페이지",
                                                            };
                                                            pageButtons.Add(prevButton);
                                                        }

                                                        UserHeroCard pageCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "",
                                                            Buttons = pageButtons,
                                                        };
                                                        plAttachment = pageCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                }
                                                /**************************************************/
                                            }
                                            else if (authUrl.Equals("[F_택배배송일정조회]::나의배송목록"))
                                            {
                                                /**************************************************/
                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_택배배송일정조회");
                                                int totalPage = 0;
                                                if (apiActiveText.Contains("배송목록다음페이지") && activity.Text.Contains(">>"))
                                                {
                                                    deliveryListPageNum++;
                                                }
                                                else if (apiActiveText.Contains("배송목록이전페이지") && activity.Text.Contains("<<"))
                                                {
                                                    deliveryListPageNum--;
                                                }
                                                else
                                                {
                                                    deliveryListPageNum = 1;
                                                }

                                                WebClient webClient = new WebClient();
                                                /*
                                        * POST METHOD
                                        * */
                                                postParams = new StringBuilder();
                                                //postParams.Append("tel_num=" + requestPhone);
                                                postParams.Append("auth_num=" + authNumber);
                                                postParams.Append("&pag_num=" + deliveryListPageNum);
                                                postParams.Append("&pag_cnt=" + pageCnt);

                                                Encoding encoding4 = Encoding.UTF8;
                                                byte[] result4 = encoding4.GetBytes(postParams.ToString());

                                                wReq = (HttpWebRequest)WebRequest.Create(DeliveryList);
                                                wReq.Method = "POST";
                                                wReq.ContentType = "application/x-www-form-urlencoded";
                                                wReq.ContentLength = result4.Length;

                                                postDataStream = wReq.GetRequestStream();
                                                postDataStream.Write(result4, 0, result4.Length);
                                                postDataStream.Close();

                                                wResp = (HttpWebResponse)wReq.GetResponse();
                                                respPostStream = wResp.GetResponseStream();
                                                readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                                String DeliveryListJsonData = readerPost.ReadToEnd();
                                                Debug.WriteLine("post data(인증후택배배송목록)====" + DeliveryListJsonData);
                                                JArray obj4 = JArray.Parse(DeliveryListJsonData);

                                                int checkInt = obj4.Count;

                                                if (checkInt == 0)
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard4 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                    };

                                                    Attachment plAttachment4 = plCard4.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment4);
                                                }
                                                else
                                                {
                                                    foreach (JObject jobj4 in obj4)
                                                    {
                                                        if (jobj4["ret_cod"].ToString().Equals("9041"))
                                                        {
                                                            totalPage = 1;
                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "인증번호에 오류가 발생되었습니다."
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else if (jobj4["ret_cod"].ToString().Equals("9042"))
                                                        {
                                                            totalPage = 1;
                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else if (jobj4["ret_cod"].ToString().Equals("9999"))
                                                        {
                                                            totalPage = 1;
                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "시스템 오류 또는 요청항목이 누락되었습니다."
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else
                                                        {
                                                            List<CardAction> cardButtons4 = new List<CardAction>();

                                                            totalPage = Convert.ToInt32(jobj4["tot_pag"].ToString());
                                                            deliveryListPageNum = Convert.ToInt32(jobj4["pag_num"].ToString());

                                                            String tempDate = jobj4["dlv_ymd"].ToString();
                                                            String dateText = "";
                                                            if (tempDate == "" || tempDate.Equals(""))
                                                            {
                                                                //dateText = "<div class=\"endDate\"><span class=\"dateDay\"><small>배송중</small></span><span class=\"dateWeek\"></span></div>";
                                                                dateText = "";
                                                            }
                                                            else
                                                            {
                                                                String yearText = tempDate.Substring(0, 4);
                                                                String monthText = tempDate.Substring(4, 2);
                                                                String dayText = tempDate.Substring(6, 2);
                                                                dateText = "<strong>· 배송완료일자 : </strong > " + yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj4["dlv_dy"].ToString() + ")<br>";
                                                                //dateText = "<div class=\"endDate\"><span class=\"dateDay\">" + monthText + "." + dayText + "</span><span class=\"dateWeek\">" + jobj4["dlv_dy"].ToString() + "요일</span></div>";
                                                            }
                                                            String cardShowText = "";

                                                            //배송상태 처리
                                                            String deliveryStatus = jobj4["wrk_nam"].ToString();
                                                            String deliveryStatusText = "상품접수";
                                                            if (deliveryStatus.Equals("10"))
                                                            {
                                                                deliveryStatusText = "상품접수";
                                                            }
                                                            else if (deliveryStatus.Equals("20"))
                                                            {
                                                                deliveryStatusText = "상품발송대기중";
                                                            }
                                                            else if (deliveryStatus.Equals("30"))
                                                            {
                                                                deliveryStatusText = "이동중";
                                                            }
                                                            else if (deliveryStatus.Equals("40"))
                                                            {
                                                                deliveryStatusText = "배송준비";
                                                            }
                                                            else if (deliveryStatus.Equals("50"))
                                                            {
                                                                deliveryStatusText = "배송중";
                                                            }
                                                            else
                                                            {
                                                                deliveryStatusText = "배송완료";
                                                            }

                                                            String goodNameTemp = jobj4["god_nam"].ToString();
                                                            int goodNameLength = jobj4["god_nam"].ToString().Length;
                                                            String goodName = "";
                                                            if (goodNameLength > 20)
                                                            {
                                                                goodName = goodNameTemp.Substring(0, 15) + "....";
                                                            }
                                                            else
                                                            {
                                                                goodName = goodNameTemp;
                                                            }

                                                            CardAction bookButton = new CardAction();
                                                            bookButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "운송장 번호 " + jobj4["wbl_num"].ToString() + "에 대한 배송일정조회",
                                                                Title = "배송일정 확인"
                                                            };
                                                            cardButtons4.Add(bookButton);

                                                            UserHeroCard plCard4 = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = dateText + "<strong>· 배송상태 : <font color='#0101DF'>" + deliveryStatusText + "</font></strong><br><strong>· 상품명 : </strong><font color='#585858'>" + goodName + "</font><br><strong>· 운송장번호 : <font color='#0101DF'>(" + jobj4["wbl_num"].ToString() + ")</font></strong><br><strong>· 송하인명 : </strong>" + jobj4["snd_nam"].ToString(),
                                                                //Text = "<div class=\"takeBack\">" + dateText + "<div class=\"prodInfo\"><span class=\"prodName\">" + goodName + "</span><span class=\"prodNum\">" + jobj4["wbl_num"].ToString() + "/" + jobj4["snd_nam"].ToString() + "</span><span class=\"prodStatus\">" + deliveryStatusText + "</span></div></div>",
                                                                Tap = bookButton,
                                                            };

                                                            Attachment plAttachment4 = plCard4.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment4);
                                                        }

                                                    }
                                                    if (totalPage == 1)
                                                    {
                                                        //페이징 없음
                                                    }
                                                    else
                                                    {
                                                        List<CardAction> pageButtons = new List<CardAction>();
                                                        //전체페이지와 동일하면 다음버튼은 없다
                                                        if (deliveryListPageNum == totalPage)
                                                        {

                                                        }
                                                        else
                                                        {
                                                            CardAction nextButton = new CardAction();
                                                            nextButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "배송목록 다음페이지>>",
                                                                Title = "다음페이지",
                                                            };
                                                            pageButtons.Add(nextButton);
                                                        }
                                                        //현재 페이지가 1이면 이전버튼은 없다.
                                                        if (deliveryListPageNum < 2)
                                                        {

                                                        }
                                                        else
                                                        {
                                                            CardAction prevButton = new CardAction();
                                                            prevButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "배송목록 이전페이지<<",
                                                                Title = "이전페이지",
                                                            };
                                                            pageButtons.Add(prevButton);
                                                        }


                                                        UserHeroCard pageCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "",
                                                            Buttons = pageButtons,
                                                        };
                                                        Attachment plAttachment4 = pageCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment4);
                                                    }
                                                }
                                                /**************************************************/
                                            }
                                            else if (authUrl.Equals("[F_운송장번호확인]::반송장번호확인"))
                                            {
                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", "F_운송장번호확인");
                                                int totalPage = 0;
                                                if (apiActiveText.Contains("반송장번호확인다음페이지") && activity.Text.Contains(">>"))
                                                {
                                                    deliveryListPageNum++;
                                                }
                                                else if (apiActiveText.Contains("반송장번호확인이전페이지") && activity.Text.Contains("<<"))
                                                {
                                                    deliveryListPageNum--;
                                                }
                                                else
                                                {
                                                    deliveryListPageNum = 1;
                                                }

                                                WebClient webClient = new WebClient();
                                                /*
                                        * POST METHOD
                                        * */
                                                postParams = new StringBuilder();
                                                //postParams.Append("tel_num=" + requestPhone);
                                                postParams.Append("auth_num=" + authNumber);
                                                postParams.Append("&pag_num=" + deliveryListPageNum);
                                                postParams.Append("&pag_cnt=" + pageCnt);

                                                Encoding encoding4 = Encoding.UTF8;
                                                byte[] result4 = encoding4.GetBytes(postParams.ToString());

                                                wReq = (HttpWebRequest)WebRequest.Create(DeliveryList);
                                                wReq.Method = "POST";
                                                wReq.ContentType = "application/x-www-form-urlencoded";
                                                wReq.ContentLength = result4.Length;

                                                postDataStream = wReq.GetRequestStream();
                                                postDataStream.Write(result4, 0, result4.Length);
                                                postDataStream.Close();

                                                wResp = (HttpWebResponse)wReq.GetResponse();
                                                respPostStream = wResp.GetResponseStream();
                                                readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);
                                                String DeliveryListJsonData = readerPost.ReadToEnd();
                                                Debug.WriteLine("post data(인증후반송장번호확인택배배송목록)====" + DeliveryListJsonData);
                                                JArray obj4 = JArray.Parse(DeliveryListJsonData);

                                                int checkInt = obj4.Count;

                                                if (checkInt == 0)
                                                {
                                                    totalPage = 1;
                                                    UserHeroCard plCard4 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                    };

                                                    Attachment plAttachment4 = plCard4.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment4);
                                                }
                                                else
                                                {
                                                    List<CardAction> returnButtons = new List<CardAction>();
                                                    CardAction returnListButton = new CardAction();
                                                    returnListButton = new CardAction()
                                                    {
                                                        Type = "postBack",
                                                        Value = "[F_운송장번호확인]::반송장번호직접입력",
                                                        Title = "예약번호/운송장번호 직접입력"
                                                    };
                                                    returnButtons.Add(returnListButton);

                                                    UserHeroCard plCard4 = new UserHeroCard()
                                                    {
                                                        Title = "",
                                                        Text = "반품상품의 반송장번호를 확인합니다.<br>목록에서 선택해 주시거나 예약번호나 반품택배예약 접수하신 운송장번호를 입력해 주십시오.",
                                                        Buttons = returnButtons,
                                                    };
                                                    Attachment plAttachment4 = plCard4.ToAttachment();
                                                    apiMakerReply.Attachments.Add(plAttachment4);

                                                    foreach (JObject jobj4 in obj4)
                                                    {
                                                        if (jobj4["ret_cod"].ToString().Equals("9041"))
                                                        {
                                                            totalPage = 1;
                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "인증번호에 오류가 발생되었습니다."
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else if (jobj4["ret_cod"].ToString().Equals("9042"))
                                                        {
                                                            totalPage = 1;
                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "고객님의 휴대폰 번호로 조회되는 목록이 없습니다."
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else if (jobj4["ret_cod"].ToString().Equals("9999"))
                                                        {
                                                            totalPage = 1;
                                                            plCard = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = "시스템 오류 또는 요청항목이 누락되었습니다."
                                                            };
                                                            plAttachment = plCard.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment);
                                                        }
                                                        else
                                                        {
                                                            List<CardAction> cardButtons4 = new List<CardAction>();

                                                            totalPage = Convert.ToInt32(jobj4["tot_pag"].ToString());
                                                            deliveryListPageNum = Convert.ToInt32(jobj4["pag_num"].ToString());

                                                            String tempDate = jobj4["dlv_ymd"].ToString();
                                                            String dateText = "";
                                                            if (tempDate == "" || tempDate.Equals(""))
                                                            {
                                                                dateText = "";
                                                                //dateText = "<div class=\"endDate\"><span class=\"dateDay\"><small>배송중</small></span><span class=\"dateWeek\"></span></div>";
                                                            }
                                                            else
                                                            {
                                                                String yearText = tempDate.Substring(0, 4);
                                                                String monthText = tempDate.Substring(4, 2);
                                                                String dayText = tempDate.Substring(6, 2);
                                                                dateText = "<strong>· 배송완료일자 : </strong > " + yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj4["dlv_dy"].ToString() + ")<br>";
                                                                //dateText = "<div class=\"endDate\"><span class=\"dateDay\">" + monthText + "." + dayText + "</span><span class=\"dateWeek\">" + jobj4["dlv_dy"].ToString() + "요일</span></div>";
                                                            }
                                                            String cardShowText = "";

                                                            //배송상태 처리
                                                            String deliveryStatus = jobj4["wrk_nam"].ToString();
                                                            String deliveryStatusText = "상품접수";
                                                            if (deliveryStatus.Equals("10"))
                                                            {
                                                                deliveryStatusText = "상품접수";
                                                            }
                                                            else if (deliveryStatus.Equals("20"))
                                                            {
                                                                deliveryStatusText = "상품발송대기중";
                                                            }
                                                            else if (deliveryStatus.Equals("30"))
                                                            {
                                                                deliveryStatusText = "이동중";
                                                            }
                                                            else if (deliveryStatus.Equals("40"))
                                                            {
                                                                deliveryStatusText = "배송준비";
                                                            }
                                                            else if (deliveryStatus.Equals("50"))
                                                            {
                                                                deliveryStatusText = "배송중";
                                                            }
                                                            else
                                                            {
                                                                deliveryStatusText = "배송완료";
                                                            }

                                                            String goodNameTemp = jobj4["god_nam"].ToString();
                                                            int goodNameLength = jobj4["god_nam"].ToString().Length;
                                                            String goodName = "";
                                                            if (goodNameLength > 20)
                                                            {
                                                                goodName = goodNameTemp.Substring(0, 15) + "....";
                                                            }
                                                            else
                                                            {
                                                                goodName = goodNameTemp;
                                                            }

                                                            CardAction returnNmButton = new CardAction();
                                                            returnNmButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "운송장번호 " + jobj4["wbl_num"].ToString() + " 의 반송장번호 확인",
                                                                Title = "반송장 번호 확인"
                                                            };
                                                            cardButtons4.Add(returnNmButton);

                                                            plCard4 = new UserHeroCard()
                                                            {
                                                                Title = "",
                                                                Text = dateText + "<strong>· 배송상태 : <font color='#0101DF'>" + deliveryStatusText + "</font></strong><br><strong>· 상품명 : </strong><font color='#585858'>" + goodName + "</font><br><strong>· 운송장번호 : <font color='#0101DF'>(" + jobj4["wbl_num"].ToString() + ")</font></strong><br><strong>· 송하인명 : </strong>" + jobj4["snd_nam"].ToString(),
                                                                //Text = "<div class=\"takeBack\">" + dateText + "<div class=\"prodInfo\"><span class=\"prodName\">" + goodName + "</span><span class=\"prodNum\">" + jobj4["wbl_num"].ToString() + "/" + jobj4["snd_nam"].ToString() + "</span><span class=\"prodStatus\">" + deliveryStatusText + "</span></div></div>",
                                                                Tap = returnNmButton,
                                                            };

                                                            plAttachment4 = plCard4.ToAttachment();
                                                            apiMakerReply.Attachments.Add(plAttachment4);
                                                        }

                                                    }
                                                    if (totalPage == 1)
                                                    {
                                                        //페이징 없음
                                                    }
                                                    else
                                                    {
                                                        List<CardAction> pageButtons = new List<CardAction>();
                                                        //전체페이지와 동일하면 다음버튼은 없다
                                                        if (deliveryListPageNum == totalPage)
                                                        {

                                                        }
                                                        else
                                                        {
                                                            CardAction nextButton = new CardAction();
                                                            nextButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "반송장번호확인 다음페이지>>",
                                                                Title = "다음페이지",
                                                            };
                                                            pageButtons.Add(nextButton);
                                                        }
                                                        //현재 페이지가 1이면 이전버튼은 없다.
                                                        if (deliveryListPageNum < 2)
                                                        {

                                                        }
                                                        else
                                                        {
                                                            CardAction prevButton = new CardAction();
                                                            prevButton = new CardAction()
                                                            {
                                                                Type = "imBack",
                                                                Value = "반송장번호확인 이전페이지<<",
                                                                Title = "이전페이지",
                                                            };
                                                            pageButtons.Add(prevButton);
                                                        }


                                                        UserHeroCard pageCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "",
                                                            Buttons = pageButtons,
                                                        };
                                                        plAttachment4 = pageCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment4);
                                                    }
                                                }
                                            }
                                            else
                                            {

                                            }

                                            /**********************************************************************************************/

                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            authCheck = "F";//인증실패
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_CHECK", authCheck); //AUTH_CHECK UPDATE
                                            checkAuthNameCnt = "F";
                                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "NAMECHECK", checkAuthNameCnt); //인증 이름부분
                                            String rejectText = "";
                                            if (jobj["ret_cod"].ToString().Equals("9033"))
                                            {
                                                rejectText = "인증번호가 잘못되었습니다.";
                                            }
                                            else if (jobj["ret_cod"].ToString().Equals("9034"))
                                            {
                                                rejectText = "인증번호 발급시간 오류(3분경과)";
                                            }
                                            else
                                            {
                                                rejectText = "기타오류";
                                            }
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "다음과 같은 이유로 인증에 실패되었습니다.<hr>" + rejectText,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }

                                    }

                                }
                                else//위의 글 외에는 이제는 이름이라고 판단하고 진행합시다.
                                {
                                    authName = apiActiveText;
                                    if (checkAuthNameCnt.Equals("T"))//이제는 이름이라고 생각하자. 인증번호 받기
                                    {
                                        postParams = new StringBuilder();
                                        postParams.Append("gbn_cod=CHATBOT");
                                        postParams.Append("&tel_num=" + requestPhone);
                                        postParams.Append("&req_nam=" + authName);

                                        Encoding encoding = Encoding.UTF8;
                                        //byte[] result = encoding.GetBytes(postParams.ToString());
                                        byte[] result = Encoding.GetEncoding("ks_c_5601-1987").GetBytes(postParams.ToString());

                                        wReq = (HttpWebRequest)WebRequest.Create(requestAuth);
                                        wReq.Method = "POST";
                                        wReq.ContentType = "application/x-www-form-urlencoded";
                                        wReq.ContentLength = result.Length;

                                        postDataStream = wReq.GetRequestStream();
                                        postDataStream.Write(result, 0, result.Length);
                                        postDataStream.Close();

                                        wResp = (HttpWebResponse)wReq.GetResponse();
                                        respPostStream = wResp.GetResponseStream();
                                        readerPost = new StreamReader(respPostStream, Encoding.GetEncoding("ks_c_5601-1987"), true);

                                        String requestAuthJsonData = readerPost.ReadToEnd();
                                        Debug.WriteLine("post data(인증시 이름으로하기)====" + requestAuthJsonData);

                                        JArray obj = JArray.Parse(requestAuthJsonData);
                                        foreach (JObject jobj in obj)
                                        {
                                            if (jobj["ret_cod"].ToString().Equals("1000"))
                                            {
                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_NUMBER", jobj["auth_num"].ToString()); //AUTH_NUMBER UPDATE
                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "USER_NAME", authName); //AUTH_NAME UPDATE
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                CardAction authButton = new CardAction();
                                                authButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "인증확인",
                                                    Title = "인증확인"
                                                };
                                                cardButtons.Add(authButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "요청하신 인증번호는 <strong>" + jobj["auth_num"].ToString() + "</strong> 입니다<br>인증확인버튼을 클릭하시면 인증이 완료됩니다.",
                                                    Buttons = cardButtons,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                            else
                                            {
                                                checkAuthNameCnt = "F";
                                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "NAMECHECK", checkAuthNameCnt); //인증 이름부분
                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "인증번호 생성에 실패되었습니다.<br>불편을 드려 죄송합니다. 다시 시도해 주세요.",
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                                SetActivity(apiMakerReply);
                                            }
                                        }
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                            else
                            {

                            }

                            /********************************************/
                        }

                        DateTime endTime = DateTime.Now;

                        //analysis table insert
                        //NONE_DLG 예외처리
                        if (string.IsNullOrEmpty(luisIntent))
                        {
                            luisIntent = "";
                        }
                        if (luisIntent.Equals("NONE_DLG"))
                        {
                            replyresult = "H";
                        }

                        if (luisIntent.Equals("None"))
                        {
                            if (apiIntent.Equals("None"))
                            {
                                int dbResult = db.insertUserQuery(relationList, luisId, luisIntent, luisEntities, luisIntentScore, replyresult, orgMent);
                            }
                            else
                            {
                                //api 일때는 처리하지 않기.
                            }
                        }
                        else
                        {
                            int dbResult = db.insertUserQuery(relationList, luisId, luisIntent, luisEntities, luisIntentScore, replyresult, orgMent);
                        }



                        //history table insert
                        //NONE_DLG 예외처리
                        //db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), "", "", "", "", replyresult);
                        if (luisIntent.Equals("NONE_DLG"))
                        {
                            replyresult = "D";
                        }

                        db.insertHistory(null, activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), luisIntent, luisEntities, luisIntentScore, dlgId, replyresult, orgMent);
                        replyresult = "";
                        luisIntent = "";
                        luisTypeEntities = "";
                    }
                }
                catch (Exception e)
                {
                    Debug.Print(e.StackTrace);
                    DButil.HistoryLog("ERROR===" + e.Message);

                    Activity sorryReply = activity.CreateReply();

                    string queryStr = activity.Text;

                    queryStr = Regex.Replace(queryStr, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);
                    queryStr = queryStr.Replace(" ", "").ToLower();

                    sorryReply.Recipient = activity.From;
                    sorryReply.Type = "message";
                    sorryReply.Attachments = new List<Attachment>();
                    //sorryReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                    List<TextList> text = new List<TextList>();
                    List<CardAction> cardButtons = new List<CardAction>();

                    text = db.SelectSorryDialogText("5");
                    for (int i = 0; i < text.Count; i++)
                    {
                        UserHeroCard plCard = new UserHeroCard()
                        {
                            Title = text[i].cardTitle,
                            Text = text[i].cardText,
                            //Buttons = cardButtons
                        };

                        Attachment plAttachment = plCard.ToAttachment();
                        sorryReply.Attachments.Add(plAttachment);
                    }

                    SetActivity(sorryReply);

                    //db.InsertError(activity.Conversation.Id, e.Message);

                    DateTime endTime = DateTime.Now;
                    int dbResult = db.insertUserQuery(null, "", "", "", "", "E", queryStr);
                    //db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), "", "", "", "","E");
                    db.insertHistory(null, activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), "", "", "", "", "E", queryStr);
                }
                finally
                {
                    if (reply1.Attachments.Count != 0 || reply1.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply1);
                    }
                    if (reply2.Attachments.Count != 0 || reply2.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply2);
                    }
                    if (reply3.Attachments.Count != 0 || reply3.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply3);
                    }
                    if (reply4.Attachments.Count != 0 || reply4.Text != "")
                    {
                        await connector.Conversations.SendToConversationAsync(reply4);
                    }
                }
            }
            else
            {
                HandleSystemMessage(activity);
            }
            response = Request.CreateResponse(HttpStatusCode.OK);
            return response;

        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
            }
            else if (message.Type == ActivityTypes.Typing)
            {
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }
            return null;
        }

        public static async Task<JObject> GetIntentFromBotLUIS(List<string[]> textList, string query, string luisType, String ChannelId, String ConversationId)
        {

            JObject[] Luis_before = new JObject[2];
            JObject Luis = new JObject();
            float luisScoreCompare = 0.0f;
            query = Uri.EscapeDataString(query);

            for (int k = 0; k < textList.Count; k++)
            {
                string url = string.Format("https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{0}?subscription-key={1}&timezoneOffset=0&verbose=true&q={2}", textList[k][1], textList[k][2], query);

                Debug.WriteLine("-----LUIS URL 확인");
                Debug.WriteLine("-----LUIS URL : " + url);

                using (HttpClient client = new HttpClient())
                {
                    //취소 시간 설정
                    client.Timeout = TimeSpan.FromMilliseconds(MessagesController.LUIS_TIME_LIMIT); //3초
                    var cts = new CancellationTokenSource();
                    try
                    {
                        HttpResponseMessage msg = await client.GetAsync(url, cts.Token);

                        int currentRetry = 0;

                        Debug.WriteLine("msg.IsSuccessStatusCode1 = " + msg.IsSuccessStatusCode);
                        //HistoryLog("msg.IsSuccessStatusCode1 = " + msg.IsSuccessStatusCode);

                        if (msg.IsSuccessStatusCode)
                        {
                            var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                            Luis_before[k] = JObject.Parse(JsonDataResponse);
                            currentRetry = 0;
                        }
                        else
                        {
                            //통신장애, 구독만료, url 오류                  
                            //오류시 3번retry
                            for (currentRetry = 0; currentRetry < 3; currentRetry++)
                            {
                                //테스용 url 설정
                                //string url_re = string.Format("https://southeastasia.api.cognitive.microsoft.com/luis/v2.0/apps/{0}?subscription-key={1}&timezoneOffset=0&verbose=true&q={2}", luis_app_id, luis_subscription, query);
                                string url_re = string.Format("https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{0}?subscription-key={1}&timezoneOffset=0&verbose=true&q={2}", textList[k][1], textList[k][2], query);
                                HttpResponseMessage msg_re = await client.GetAsync(url_re, cts.Token);

                                if (msg_re.IsSuccessStatusCode)
                                {
                                    //다시 호출
                                    Debug.WriteLine("msg.IsSuccessStatusCode2 = " + msg_re.IsSuccessStatusCode);
                                    //HistoryLog("msg.IsSuccessStatusCode2 = " + msg.IsSuccessStatusCode);
                                    var JsonDataResponse = await msg_re.Content.ReadAsStringAsync();
                                    Luis_before[k] = JObject.Parse(JsonDataResponse);
                                    luisScoreCompare = (float)Luis_before[k]["intents"][0]["score"];
                                    currentRetry = 0;
                                    break;
                                }
                                else
                                {
                                    //초기화
                                    //jsonObj = JObject.Parse(@"{
                                    //    'query':'',
                                    //    'topScoringIntent':0,
                                    //    'intents':[],
                                    //    'entities':'[]'
                                    //}");
                                    Debug.WriteLine("GetIntentFromBotLUIS else print ");
                                    //HistoryLog("GetIntentFromBotLUIS else print ");
                                    Luis_before[k] = JObject.Parse(@"{
                                                                        'query': '',
                                                                        'topScoringIntent': {
                                                                        'intent': 'None',
                                                                        'score': 0.09
                                                                        },
                                                                        'intents': [
                                                                        {
                                                                            'intent': 'None',
                                                                            'score': 0.09
                                                                        }
                                                                        ],
                                                                        'entities': []
                                                                    }
                                                                    ");
                                }
                            }
                        }

                        msg.Dispose();
                    }
                    catch (TaskCanceledException e)
                    {
                        Debug.WriteLine("GetIntentFromBotLUIS error = " + e.Message);
                        //HistoryLog("GetIntentFromBotLUIS error = " + e.Message);
                        //초기화
                        //jsonObj = JObject.Parse(@"{
                        //                'query':'',
                        //                'topScoringIntent':0,
                        //                'intents':[],
                        //                'entities':'[]'
                        //            }");

                        Luis_before[k] = JObject.Parse(@"{
                                                            'query': '',
                                                            'topScoringIntent': {
                                                            'intent': 'None',
                                                            'score': 0.09
                                                            },
                                                            'intents': [
                                                            {
                                                                'intent': 'None',
                                                                'score': 0.09
                                                            }
                                                            ],
                                                            'entities': []
                                                        }
                                                        ");

                    }
                }
            }

            for (int i = 0; i < 1; i++)
            {
                //entities 0일 경우 PASS
                if (1 != 0)
                {
                    //intent None일 경우 PASS
                    if (Luis_before[i]["intents"][0]["intent"].ToString() != "None")
                    {
                        if (luisType.Equals("API"))
                        {
                            //제한점수 체크(API)
                            if ((float)Luis_before[i]["intents"][0]["score"] > Convert.ToDouble(MessagesController.LUIS_APISCORE_LIMIT))
                            {
                                if ((float)Luis_before[i]["intents"][0]["score"] > luisScoreCompare)
                                {
                                    Luis = Luis_before[i];
                                    luisScoreCompare = (float)Luis_before[i]["intents"][0]["score"];
                                }
                                else
                                {

                                }

                            }
                        }
                        else
                        {
                            //제한점수 체크(LUIS)
                            if ((float)Luis_before[i]["intents"][0]["score"] > Convert.ToDouble(MessagesController.LUIS_SCORE_LIMIT))
                            {
                                if ((float)Luis_before[i]["intents"][0]["score"] > luisScoreCompare)
                                {
                                    Luis = Luis_before[i];
                                    luisScoreCompare = (float)Luis_before[i]["intents"][0]["score"];
                                }
                                else
                                {

                                }

                            }
                            else
                            {
                                DbConnect db = new DbConnect();
                                //상위 4개 INTENT 디비 등록
                                String intentName = "";
                                float checkMinScore = 0.59f;
                                for (int ii = 0; ii < 4; ii++)
                                {
                                    if (Luis_before[i]["intents"][ii]["intent"].ToString().Equals("None") || Luis_before[i]["intents"][ii]["intent"].ToString().Equals("test intent"))
                                    {

                                    }
                                    else
                                    {
                                        //점수는 50점
                                        Debug.WriteLine("score===========================================================" + (float)Luis_before[i]["intents"][ii]["score"]);
                                        Debug.WriteLine("intent===========================================================" + Luis_before[i]["intents"][ii]["intent"].ToString());
                                        if ((float)Luis_before[i]["intents"][ii]["score"] > checkMinScore)
                                        {
                                            intentName = intentName + Luis_before[i]["intents"][ii]["intent"].ToString() + "%";
                                        }

                                    }
                                }
                                db.UserCheckUpdate(ChannelId, ConversationId, "SORRY_INTENT", intentName);
                            }
                        }

                    }
                }
            }

            return Luis;
        }

        public static async Task<JObject> GetIntentFromBotLUISMIN(List<string[]> textList, string query, string luisType)
        {

            JObject[] Luis_before = new JObject[2];
            JObject Luis = new JObject();
            float luisScoreCompare = 0.0f;
            query = Uri.EscapeDataString(query);

            for (int k = 0; k < textList.Count; k++)
            {
                string url = string.Format("https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{0}?subscription-key={1}&timezoneOffset=0&verbose=true&q={2}", textList[k][1], textList[k][2], query);

                Debug.WriteLine("-----LUIS URL 확인");
                Debug.WriteLine("-----LUIS URL : " + url);

                using (HttpClient client = new HttpClient())
                {
                    //취소 시간 설정
                    client.Timeout = TimeSpan.FromMilliseconds(MessagesController.LUIS_TIME_LIMIT); //3초
                    var cts = new CancellationTokenSource();
                    try
                    {
                        HttpResponseMessage msg = await client.GetAsync(url, cts.Token);

                        int currentRetry = 0;

                        Debug.WriteLine("msg.IsSuccessStatusCode1 = " + msg.IsSuccessStatusCode);
                        //HistoryLog("msg.IsSuccessStatusCode1 = " + msg.IsSuccessStatusCode);

                        if (msg.IsSuccessStatusCode)
                        {
                            var JsonDataResponse = await msg.Content.ReadAsStringAsync();
                            Luis_before[k] = JObject.Parse(JsonDataResponse);
                            currentRetry = 0;
                        }
                        else
                        {
                            //통신장애, 구독만료, url 오류                  
                            //오류시 3번retry
                            for (currentRetry = 0; currentRetry < 3; currentRetry++)
                            {
                                //테스용 url 설정
                                //string url_re = string.Format("https://southeastasia.api.cognitive.microsoft.com/luis/v2.0/apps/{0}?subscription-key={1}&timezoneOffset=0&verbose=true&q={2}", luis_app_id, luis_subscription, query);
                                string url_re = string.Format("https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/{0}?subscription-key={1}&timezoneOffset=0&verbose=true&q={2}", textList[k][1], textList[k][2], query);
                                HttpResponseMessage msg_re = await client.GetAsync(url_re, cts.Token);

                                if (msg_re.IsSuccessStatusCode)
                                {
                                    //다시 호출
                                    Debug.WriteLine("msg.IsSuccessStatusCode2 = " + msg_re.IsSuccessStatusCode);
                                    //HistoryLog("msg.IsSuccessStatusCode2 = " + msg.IsSuccessStatusCode);
                                    var JsonDataResponse = await msg_re.Content.ReadAsStringAsync();
                                    Luis_before[k] = JObject.Parse(JsonDataResponse);
                                    luisScoreCompare = (float)Luis_before[k]["intents"][0]["score"];
                                    currentRetry = 0;
                                    break;
                                }
                                else
                                {
                                    //초기화
                                    //jsonObj = JObject.Parse(@"{
                                    //    'query':'',
                                    //    'topScoringIntent':0,
                                    //    'intents':[],
                                    //    'entities':'[]'
                                    //}");
                                    Debug.WriteLine("GetIntentFromBotLUIS else print ");
                                    //HistoryLog("GetIntentFromBotLUIS else print ");
                                    Luis_before[k] = JObject.Parse(@"{
                                                                        'query': '',
                                                                        'topScoringIntent': {
                                                                        'intent': 'None',
                                                                        'score': 0.09
                                                                        },
                                                                        'intents': [
                                                                        {
                                                                            'intent': 'None',
                                                                            'score': 0.09
                                                                        }
                                                                        ],
                                                                        'entities': []
                                                                    }
                                                                    ");
                                }
                            }
                        }

                        msg.Dispose();
                    }
                    catch (TaskCanceledException e)
                    {
                        Debug.WriteLine("GetIntentFromBotLUIS error = " + e.Message);

                        Luis_before[k] = JObject.Parse(@"{
                                                            'query': '',
                                                            'topScoringIntent': {
                                                            'intent': 'None',
                                                            'score': 0.09
                                                            },
                                                            'intents': [
                                                            {
                                                                'intent': 'None',
                                                                'score': 0.09
                                                            }
                                                            ],
                                                            'entities': []
                                                        }
                                                        ");

                    }
                }
            }

            if (1 != 0)
            {
                //intent None일 경우 PASS
                if (Luis_before[0]["intents"][0]["intent"].ToString() != "None")
                {
                    Luis = Luis_before[0];
                    //luisScoreCompare = (float)Luis_before[i]["intents"][0]["score"];

                }
            }

            return Luis;
        }
    }
}