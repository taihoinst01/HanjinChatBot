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

        public static int chatBotID = 0;
        public static DateTime startTime;


        public static String apiFlag = "";
        public static string channelID = "";

        //API변수선언
        static public string DeliveryList = "http://www.jobible.co.kr/DeliveryList.data";                 //택배목록
        static public string ReturnDeliveryResult = "http://www.jobible.co.kr/ReturnDeliveryResult.data";                 //반품예약가능여부
        //static public string API2Url = "http://www.jobible.co.kr/json1.data";                 //반품예약취소
        static public string DeliveryCollection = "http://www.jobible.co.kr/DeliveryCollection.data";                 //집하예정일확인
        static public string API4Url = "http://www.jobible.co.kr/json3.data";                 //예약번호확인
        static public string bookCheck = "http://www.jobible.co.kr/bookCheck.data";                 //예약확인
        static public string bookCancelYN = "http://www.jobible.co.kr/bookCancelYN.data";                 //예약취소가능여부확인
        static public string bookCancelResult = "http://www.jobible.co.kr/bookCancelResult.data";                 //예약취소요청
        static public string goodLocation = "http://www.jobible.co.kr/goodLocation.data";                 //상품위치확인
        static public string findOrgInfo = "http://www.jobible.co.kr/findOrgInfo.data";                 //집배점정보확인(주소)
        //static public string API7Url = "http://www.jobible.co.kr/json6.data";                 //배송일정상세조회

        static public string APIDeliverListData = "";                 //택배반품리스트 json 데이터
        static public string APIReturnBookListData = "";                 //반품예약리스트 json 데이터
        static public string APIDelayListData = "";                 //택배방문지연 json 데이터
        static public string APIFindListData = "";                 //집배점기사찾기 json 데이터

        static public string apiIntent = "None";                 //api 용 intent
        static public string apiOldIntent = "None";                 //api 용 intent(old)
        static public string invoiceNumber = "";                 //운송장 번호
        static public string bookNumber = "";                 //예약 번호
        static public string cancelNumber = "";                 //취소사유
        static public string APIResult = "";                 //api 결과-쏘리메세지 안 나오게 하기 위해서.
        static public string APILuisIntent = null;                 //API 용 루이스 INTENT
        static public string authCheck = "F";                 //인증 체크-리스트 추출용(T/F)
        static public string authNumber = "";                 //인증 번호
        static public string checkFindAddressCnt = "F";                 //주소로서 집배점 찾기 검토

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
                    //string[] LUIS_NM = new string[10];
                    Array.Clear(LUIS_NM, 0, LUIS_NM.Length);
                }

                //파라메터 호출
                if (LUIS_APINM.Count(s => s != null) > 0)
                {
                    //string[] LUIS_NM = new string[10];
                    Array.Clear(LUIS_APINM, 0, LUIS_APINM.Length);
                }

                if (LUIS_APP_ID.Count(s => s != null) > 0)
                {
                    //string[] LUIS_APP_ID = new string[10];
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
                        case "LUIS_TIME_LIMIT":
                            LUIS_TIME_LIMIT = Convert.ToInt32(confList[i].cnfValue);
                            break;
                        default: //미 정의 레코드
                            Debug.WriteLine("*conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            DButil.HistoryLog("*conf type : " + confList[i].cnfType + "* conf value : " + confList[i].cnfValue);
                            break;
                    }
                }

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
                    //initReply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

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
                String telNumber = "01012341234";
                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "USER_PHONE", telNumber);
            }
            //else if (activity.Type == ActivityTypes.Message)
            else if (activity.Type == ActivityTypes.Message && !activity.Text.Contains("tel:"))
            {
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
                    //결과 플레그 H : 정상 답변,  G : 건의사항, D : 답변 실패, E : 에러, S : SMALLTALK, I : SAPINIT, Q : SAP용어, Z : SAP용어 실피, B : 금칙어 및 비속어 
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

                        //history table insert
                        //db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), "", "", "", "", replyresult);
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
                                                                                               //cacheList.luisIntent 초기화
                                                                                               //cacheList.luisIntent = null;

                        String checkText = Regex.Replace(activity.Text, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);//공백 및 특수문자 제거
                        int chectTextLength = checkText.Length;

                        if (checkText.Contains("동의") && chectTextLength < 9)
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
                        }
                        //luis 호출
                        else if (cacheList.luisIntent == null || cacheList.luisEntities == null)
                        {
                            DButil.HistoryLog("cache none : " + orgMent);
                            Debug.WriteLine("cache none : " + orgMent);
                            Regex r = new Regex("[0-9]");
                            bool containNum = r.IsMatch(activity.Text); //숫자여부 확인
                            String onlyNumber = Regex.Replace(activity.Text, @"\D", "");
                            int checkNumberLength = onlyNumber.Length;

                            if (containNum == true&&checkNumberLength > 8) //숫자가 포함되어 있으면 대화셋의 데이터는 나오지 않는다. 나중에 숫자 길이까지 체크(운송장, 예약번호, 전화번호)
                            {
                                luisIntent = "None";
                            }
                            else if (checkText.Contains("동의") && chectTextLength < 9)
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

                                //Task<JObject> t1 = Task<JObject>.Run(() => GetIntentFromBotLUIS2(textList, orgMent));
                                //루이스 처리
                                Task<JObject> APIt1 = Task<JObject>.Run(async () => await GetIntentFromBotLUIS(textList, luisQuery));

                                //결과값 받기
                                await Task.Delay(1000);
                                APIt1.Wait();
                                Luis = APIt1.Result;

                                //Debug.WriteLine("Luis : " + Luis); 
                                //entities 갯수가 0일겨우 intent를 None으로 처리

                                //if (Luis != null || Luis.Count > 0)
                                if (Luis.Count != 0)
                                {
                                    //if ((int)Luis["entities"].Count() != 0)
                                    if (1 != 0)
                                    {
                                        float luisScore = (float)Luis["intents"][0]["score"];
                                        int luisEntityCount = (int)Luis["entities"].Count();

                                        luisIntent = Luis["topScoringIntent"]["intent"].ToString();//add
                                        luisScore = luisScoreCompare;
                                        Debug.WriteLine("GetMultiLUIS() LUIS luisIntent : " + luisIntent);
                                    }
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

                            if (checkText.Contains("동의") && chectTextLength < 9)
                            {
                                luisIntent = "None";
                            }
                        }

                        DButil.HistoryLog("luisId : " + luisId);
                        DButil.HistoryLog("luisIntent : " + luisIntent);
                        DButil.HistoryLog("luisEntities : " + luisEntities);

                        /*
                         *  RELATION TABLE 검색. API TF 검색
                         *  있으면 그대로 사용
                         *  없으면 LUIS 검색
                         * */
                        String apiTFdata = "F";
                        Debug.WriteLine("luisIntentluisIntent : " + luisIntent);
                        if (luisIntent == "" || luisIntent.Equals("None"))
                        {
                            //API 용 루이스 INTENT
                            List<string[]> apiTextList = new List<string[]>(2);

                            for (int i = 0; i < 2; i++)
                            {
                                apiTextList.Add(new string[] { MessagesController.LUIS_NM[i], MessagesController.LUIS_APPAPI_ID[i], MessagesController.LUIS_SUBSCRIPTION, luisQuery });
                                Debug.WriteLine("GetMultiLUIS() LUIS_APINM : " + MessagesController.LUIS_APINM[i] + " | LUIS_APPAPI_ID : " + MessagesController.LUIS_APPAPI_ID[i]);
                            }
                            DButil.HistoryLog("activity.Conversation.Id : " + activity.Conversation.Id);
                            Debug.WriteLine("activity.Conversation.Id : " + activity.Conversation.Id);

                            //JObject Luis_before = new JObject();
                            float APIluisScoreCompare = 0.0f;
                            JObject APILuis = new JObject();

                            //Task<JObject> t1 = Task<JObject>.Run(() => GetIntentFromBotLUIS2(textList, orgMent));
                            //루이스 처리
                            Task<JObject> t1 = Task<JObject>.Run(async () => await GetIntentFromBotLUIS(apiTextList, luisQuery));

                            //결과값 받기
                            await Task.Delay(1000);
                            t1.Wait();
                            APILuis = t1.Result;
                            //결과값 받기
                            await Task.Delay(1000);
                            t1.Wait();
                            APILuis = t1.Result;

                            //Debug.WriteLine("Luis : " + Luis); 
                            //entities 갯수가 0일겨우 intent를 None으로 처리

                            //if (Luis != null || Luis.Count > 0)
                            if (APILuis.Count != 0)
                            {
                                //if ((int)Luis["entities"].Count() != 0)
                                if (1 != 0)
                                {
                                    float luisScore = (float)APILuis["intents"][0]["score"];
                                    int luisEntityCount = (int)APILuis["entities"].Count();

                                    APILuisIntent = APILuis["topScoringIntent"]["intent"].ToString();//add
                                    luisScore = APIluisScoreCompare;
                                    Debug.WriteLine("GetMultiLUIS() LUIS APILuisIntent : " + APILuisIntent);
                                }
                            }
                            else
                            {
                                APILuisIntent = "None";
                            }
                            apiIntent = APILuisIntent;
                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_INTENT", apiIntent);
                        }
                        else
                        {
                            apiTFdata = db.getAPITFData(luisIntent);
                            if (apiTFdata.Equals("T"))
                            {
                                //API 용 루이스 INTENT
                                List<string[]> apiTextList = new List<string[]>(2);

                                for (int i = 0; i < 2; i++)
                                {
                                    apiTextList.Add(new string[] { MessagesController.LUIS_NM[i], MessagesController.LUIS_APPAPI_ID[i], MessagesController.LUIS_SUBSCRIPTION, luisQuery });
                                    Debug.WriteLine("GetMultiLUIS() LUIS_APINM : " + MessagesController.LUIS_APINM[i] + " | LUIS_APPAPI_ID : " + MessagesController.LUIS_APPAPI_ID[i]);
                                }
                                DButil.HistoryLog("activity.Conversation.Id : " + activity.Conversation.Id);
                                Debug.WriteLine("activity.Conversation.Id : " + activity.Conversation.Id);

                                //JObject Luis_before = new JObject();
                                float APIluisScoreCompare = 0.0f;
                                JObject APILuis = new JObject();

                                //Task<JObject> t1 = Task<JObject>.Run(() => GetIntentFromBotLUIS2(textList, orgMent));
                                //루이스 처리
                                Task<JObject> t1 = Task<JObject>.Run(async () => await GetIntentFromBotLUIS(apiTextList, luisQuery));

                                //결과값 받기
                                await Task.Delay(1000);
                                t1.Wait();
                                APILuis = t1.Result;
                                //결과값 받기
                                await Task.Delay(1000);
                                t1.Wait();
                                APILuis = t1.Result;

                                //Debug.WriteLine("Luis : " + Luis); 
                                //entities 갯수가 0일겨우 intent를 None으로 처리

                                //if (Luis != null || Luis.Count > 0)
                                if (APILuis.Count != 0)
                                {
                                    //if ((int)Luis["entities"].Count() != 0)
                                    if (1 != 0)
                                    {
                                        float luisScore = (float)APILuis["intents"][0]["score"];
                                        int luisEntityCount = (int)APILuis["entities"].Count();

                                        APILuisIntent = APILuis["topScoringIntent"]["intent"].ToString();//add
                                        luisScore = APIluisScoreCompare;
                                        Debug.WriteLine("GetMultiLUIS() LUIS APILuisIntent : " + APILuisIntent);
                                    }
                                }
                                else
                                {
                                    APILuisIntent = "None";
                                }
                                apiIntent = APILuisIntent;
                            }
                            else
                            {
                                apiIntent = "None";
                            }
                            db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_INTENT", apiIntent);
                        }
                        Debug.WriteLine("apiIntentapiIntent : " + apiIntent);


                        //////////////////////////////////////////////

                        string smallTalkConfirm = "";

                        if (!string.IsNullOrEmpty(luisIntent))
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
                            //smalltalk 답변가져오기

                            if (orgMent.Length < 11)
                            {
                                smallTalkConfirm = db.SmallTalkConfirm(orgMent);
                            }
                            else
                            {
                                smallTalkConfirm = "";
                            }

                        }
                        
                        if (relationList != null)
                        {
                            dlgId = "";
                            //userData[0].mobileYN = P OR M NULL
                            for (int m = 0; m < relationList.Count; m++)
                            {
                                DialogList dlg = db.SelectDialog(relationList[m].dlgId, "P");
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

                                    //NONE_DLG 예외처리
                                    if (luisIntent.Equals("NONE_DLG"))
                                    {
                                        replyresult = "D";
                                    }
                                    else
                                    {
                                        replyresult = "H";
                                    }

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
                            db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "loop");
                        }
                        else
                        {

                        }

                        /*
                         * relationList 가 null 이고 apiIntent 가 null 이면 sorry message
                         * add JunHyoung Park
                         * */
                        List<UserCheck> uData = new List<UserCheck>();
                        uData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);
                        if (string.IsNullOrEmpty(uData[0].apiIntent))
                        {
                            apiIntent = "None";
                        }
                        else
                        {
                            apiIntent = uData[0].apiIntent;
                        }

                        if (string.IsNullOrEmpty(uData[0].apiOldIntent))
                        {
                            apiOldIntent = "None";
                        }
                        else
                        {
                            apiOldIntent = uData[0].apiOldIntent;
                        }

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
                        Debug.WriteLine("apiIntent3-------------" + apiIntent);

                        if (luisIntent.Equals("None"))
                        {

                        }
                        else
                        {
                            if (apiTFdata.Equals("F"))
                            {
                                apiIntent = "None";
                            }
                        }
                        Debug.WriteLine("apiIntent CHECK-------------"+ apiIntent);
                        Debug.WriteLine("relationList-------------" + relationList);
                        if (relationList == null && apiIntent.Equals("None"))
                        //if (relationList.Count == 0 && apiIntent.Equals("None"))
                        {
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
                                    //Title = text[i].cardTitle,
                                    Text = text[i].cardText,
                                    Buttons = cardButtons
                                };

                                Attachment plAttachment = plCard.ToAttachment();
                                sorryReply.Attachments.Add(plAttachment);
                            }

                            SetActivity(sorryReply);
                            replyresult = "D";
                        }
                        else
                        {
                            //API 관련 처리하기
                            /*
                    * API 연동부분은 다 이곳에서 처리
                    * 대화셋 APP 는 그대로 진행. API APP도 따로 진행
                    */
                            Debug.WriteLine("apiIntent1-------------" + apiIntent);
                            Debug.WriteLine("luisIntent1-------------" + luisIntent);
                            Debug.WriteLine("apiOldIntent-------------" + apiOldIntent);

                            Activity apiMakerReply = activity.CreateReply();

                            apiMakerReply.Recipient = activity.From;
                            apiMakerReply.Type = "message";
                            apiMakerReply.Attachments = new List<Attachment>();

                            Regex r = new Regex("[0-9]");
                            bool checkNum = Regex.IsMatch(activity.Text, @"^\d+$"); //입력값이 숫자인지 파악.
                            bool containNum = r.IsMatch(activity.Text); //숫자여부 확인
                            String apiActiveText = Regex.Replace(activity.Text, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);//공백 및 특수문자 제거
                            String onlyNumber = Regex.Replace(activity.Text, @"\D", "");
                            /*
                             * [APIINTENT]::글자
                             * */
                            if (activity.Text.Contains("[") && activity.Text.Contains("]"))
                            {
                                int apiIntentS = activity.Text.IndexOf("[");
                                int apiIntentE = activity.Text.IndexOf("]");
                                apiIntent = activity.Text.Substring(apiIntentS + 1, (apiIntentE - 1) - apiIntentS);
                                Debug.WriteLine("apiIntent[]-------------" + apiIntent);
                            }
                            Debug.WriteLine("apiIntent2-------------" + apiIntent);

                            /*
                             * API 처리부분
                             * */
                            if (apiIntent.Equals("None")|| apiIntent.Equals(""))
                            {

                            }
                            else
                            {
                                apiOldIntent = "None";
                            }

                            if (apiOldIntent.Equals("None")|| apiOldIntent.Equals(""))
                            {

                            }
                            else
                            {
                                apiIntent = apiOldIntent;
                            }
                            Debug.WriteLine("apiIntent3-------------" + apiIntent);

                            if(luisIntent.Equals("None"))
                            {

                            }
                            else
                            {
                                if (apiTFdata.Equals("F"))
                                {
                                    apiIntent = "None";
                                }
                            }
                            Debug.WriteLine("apiIntent4-------------" + apiIntent);
                            /*
                             * DB 에서 API 관련부분 처리
                             * 동의어 부분
                             * */

                            authCheck = uData[0].authCheck;
                            Debug.WriteLine("authCheck-------------" + authCheck);
                            /*****************************************************************
                            * apiIntent F_예약
                            * 
                            ************************************************************** */
                            if (apiIntent.Equals("F_예약"))
                            {
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
                                        Value = "http://www.hanjin.co.kr/Delivery_html/reserve/login1.jsp?rsr_gbn=",
                                        Title = "예약접수"
                                    };
                                    cardButtons.Add(bookButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "네. 고객님!<br>한진택배 이용해 주셔서 감사합니다.<br><br>예약접수 시 지정하신 날짜 혹은 접수 다음 날(접수당일, 공휴일 제외)부터 방문가능하며 일부 상품은 택배 이용에 제한이 있을 수 있으니 참고하시기 바랍니다.<br><br>택배예약을 원하시는 경우 아래 버튼을 눌러주세요.",
                                        Buttons = cardButtons,
                                    };

                                    invoiceNumber = onlyNumber;
                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                //반품택배예약
                                else if (containNum == true) //반품택배예약 중에서 숫자만 추출한다.
                                {
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(ReturnDeliveryResult);
                                    String ReturnDeliveryResultJsonData = new StreamReader(stream).ReadToEnd();

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
                                        }else if (jobj["ret_cod"].ToString().Equals("9002"))
                                        {
                                            returnYN = "no";
                                            returnText = "배송출발전 모든상태(배송출발부터 반품가능)";
                                        }else if (jobj["ret_cod"].ToString().Equals("9003"))
                                        {
                                            returnYN = "no";
                                            returnText = "신용번호 미존재";
                                        }else if (jobj["ret_cod"].ToString().Equals("9004"))
                                        {
                                            returnYN = "no";
                                            returnText = "EDI화주/멀티화주/반품계약 미 적용화주";
                                        }else if (jobj["ret_cod"].ToString().Equals("9005"))
                                        {
                                            returnYN = "no";
                                            returnText = "주소 오입력";
                                        }else if (jobj["ret_cod"].ToString().Equals("9006"))
                                        {
                                            returnYN = "no";
                                            returnText = "이중예약";
                                        }else if (jobj["ret_cod"].ToString().Equals("9007"))
                                        {
                                            returnYN = "no";
                                            returnText = "집하집배점 할당실패";
                                        }else if (jobj["ret_cod"].ToString().Equals("9999"))
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
                                        CardAction yesButton = new CardAction();
                                        yesButton = new CardAction()
                                        {
                                            Type = "openUrl",
                                            Value = "http://www.hanjin.co.kr/Delivery_html/reserve/return.jsp",
                                            Title = "홈페이지 이동"
                                        };
                                        cardButtons.Add(yesButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "운송장 번호 " + onlyNumber + "를 반품처리하시겠습니까?",
                                            Buttons = cardButtons,
                                        };

                                        invoiceNumber = onlyNumber;
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                    }
                                    else
                                    {
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "["+returnText +"] 운송장 번호 " + onlyNumber + " 상품은 반품 예약접수가 어렵습니다. 반품하실 업체를 통해 반품접수를 하시거나 한진택배 홈페이지 또는 모바일 고객센터(m.hanjin.co.kr)에서 개인택배로 예약접수가 가능합니다.",
                                            Buttons = cardButtons,
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        invoiceNumber = null;
                                    }

                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("반품택배예약"))
                                {
                                    //모바일 인증 체크
                                    if (authCheck.Equals("F"))
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction deliveryButton = new CardAction();
                                        deliveryButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "예. 핸드폰인증 하겠습니다",
                                            Title = "예"
                                        };
                                        cardButtons.Add(deliveryButton);

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "아니오. 핸드폰인증 취소하겠습니다",
                                            Title = "아니오"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "택배목록 확인등을 위해서 핸드폰 인증이 필요합니다. 핸드폰 인증을 하신 후에 다시 진행해 주세요<br>핸드폰 인증을 하시겠습니까?<br>또는 운송장번호를 직접 입력해 주세요",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(DeliveryList);
                                        String DeliveryListJsonData = new StreamReader(stream).ReadToEnd();

                                        JArray obj = JArray.Parse(DeliveryListJsonData);
                                        
                                        List<CardList> text = new List<CardList>();
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        int i = 1;
                                        foreach (JObject jobj in obj)
                                        {
                                            String tempDate = jobj["dlv_ymd"].ToString();
                                            String yearText = tempDate.Substring(0, 4);
                                            String monthText = tempDate.Substring(5, 2);
                                            String dayText = tempDate.Substring(8, 2);
                                            String dateText = yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["dlv_dy"].ToString() + "요일)";

                                            CardAction plButton = new CardAction();
                                            plButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "운송장번호 " + jobj["wbl_num"].ToString() + " 반품택배예약",
                                                Title = dateText + " "+ jobj["wrk_nam"].ToString() + jobj["wbl_num"].ToString(),
                                            };
                                            cardButtons.Add(plButton);
                                            i++;
                                        }

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "고객님의 택배배송 목록입니다.",
                                            Buttons = cardButtons,
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
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
                                        Text = "문의하실 항목을 선택해 주세요.",
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
                                 * 
                                 ************************************************************** */
                            if (apiIntent.Equals("F_예약확인"))
                            {
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Equals("집하예정일확인"))
                                {
                                    //모바일 인증 체크
                                    if (authCheck.Equals("F"))
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction deliveryButton = new CardAction();
                                        deliveryButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "예. 핸드폰인증 하겠습니다",
                                            Title = "예"
                                        };
                                        cardButtons.Add(deliveryButton);

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "아니오. 핸드폰인증 취소하겠습니다",
                                            Title = "아니오"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "택배목록 확인등을 위해서 핸드폰 인증이 필요합니다. 핸드폰 인증을 하신 후에 다시 진행해 주세요<br>핸드폰 인증을 하시겠습니까?",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(DeliveryCollection);
                                        String DeliveryCollectionJsonData = new StreamReader(stream).ReadToEnd();

                                        JArray obj = JArray.Parse(DeliveryCollectionJsonData);
                                        int checkInt = obj.Count;

                                        if (checkInt == 0)
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "고객님! 현재 문의하신 정보에 해당하는 예약 건을 찾을 수 없습니다."
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                        }
                                        else
                                        {
                                            foreach (JObject jobj in obj)
                                            {
                                                String tempDate = jobj["wrk_ymd"].ToString();
                                                String yearText = tempDate.Substring(0, 4);
                                                String monthText = tempDate.Substring(4,2);
                                                String dayText = tempDate.Substring(6, 2);
                                                String dateText = yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["wrk_yd"].ToString() + "요일)";
                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "고객님!<br>상품 \"" + jobj["god_nam"].ToString() + "(예약번호: "+ jobj["rsv_num"].ToString() + ")\"는 " + dateText + " " + jobj["wrk_nam"].ToString() +" 상태입니다.",
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                        }
                                        SetActivity(apiMakerReply);
                                    }


                                }
                                else if (apiActiveText.Equals("예약번호확인"))
                                {
                                    if (authCheck.Equals("F"))
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction deliveryButton = new CardAction();
                                        deliveryButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "예. 핸드폰인증 하겠습니다",
                                            Title = "예"
                                        };
                                        cardButtons.Add(deliveryButton);

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "아니오. 핸드폰인증 취소하겠습니다",
                                            Title = "아니오"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "택배목록 확인등을 위해서 핸드폰 인증이 필요합니다. 핸드폰 인증을 하신 후에 다시 진행해 주세요<br>핸드폰 인증을 하시겠습니까?<br>또는 운송장번호나 예약번호를 직접 입력해 주세요",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(DeliveryList);
                                        String DeliveryListJsonData = new StreamReader(stream).ReadToEnd();

                                        JArray obj = JArray.Parse(DeliveryListJsonData);
                                        int checkInt = obj.Count;

                                        if (checkInt == 0)
                                        {
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction bookButton = new CardAction();
                                            bookButton = new CardAction()
                                            {
                                                Type = "openUrl",
                                                Value = "http://www.hanjin.co.kr/Delivery_html/reserve/login1.jsp?rsr_gbn=",
                                                Title = "택배 예약하기"
                                            };
                                            cardButtons.Add(bookButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "고객님! 현재 문의하신 정보에 해당하는 예약 건을 찾을 수 없습니다."
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                        }
                                        else
                                        {
                                            foreach (JObject jobj in obj)
                                            {
                                                List<CardList> text = new List<CardList>();
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                String tempDate = jobj["dlv_ymd"].ToString();
                                                String yearText = tempDate.Substring(0, 4);
                                                String monthText = tempDate.Substring(5, 2);
                                                String dayText = tempDate.Substring(8, 2);
                                                String dateText = yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["dlv_dy"].ToString() + "요일)";

                                                CardAction bookButton = new CardAction();
                                                bookButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = jobj["wbl_num"].ToString() + " 예약 내용 확인",
                                                    Title = "예약 내용 확인"
                                                };
                                                cardButtons.Add(bookButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = dateText + " " + jobj["god_nam"].ToString() + jobj["wrk_nam"].ToString() + "<br>"+ jobj["wbl_num"].ToString(),
                                                    Buttons = cardButtons,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                        }

                                        SetActivity(apiMakerReply);
                                    }


                                }
                                else if (apiActiveText.Contains("예약내용확인")||containNum==true)//리스트버튼 클릭이거나 직접 입력일 경우
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(bookCheck);
                                    String bookCheckJsonData = new StreamReader(stream).ReadToEnd();

                                    JArray obj = JArray.Parse(bookCheckJsonData);

                                    foreach (JObject jobj in obj)
                                    {
                                        String bookCheckText = "";
                                        String tempDate = jobj["acp_ymd"].ToString();
                                        String yearText = tempDate.Substring(0, 4);
                                        String monthText = tempDate.Substring(4, 2);
                                        String dayText = tempDate.Substring(6, 2);
                                        String dateText = yearText + "년 " + monthText + "월 " + dayText + "일";

                                        if (jobj["ret_cod"].ToString().Equals("1000"))
                                        {
                                            bookCheckText = "번호 " + bookNumber + "는 " + dateText + " 정상 예약접수된 건입니다.";
                                        }else if (jobj["ret_cod"].ToString().Equals("9001"))
                                        {
                                            bookCheckText = "번호 " + bookNumber + "는 " + dateText + " 예약미등록 건입니다.";
                                        }else if (jobj["ret_cod"].ToString().Equals("9999"))
                                        {
                                            bookCheckText = "번호 " + bookNumber + "는 " + dateText + " 에러발생 건입니다.";
                                        }
                                        else
                                        {

                                        }
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = bookCheckText + "<br>방문일정 또는 예약변경 사항, 문의사항은 "+ jobj["org_nam"].ToString()+" 집배점 전화번호 "+ jobj["tel_num"].ToString()+" 으로 문의 부탁드립니다.",
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                }
                                //예약확인 초기화면
                                else
                                {
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction deliveryButton = new CardAction();
                                    deliveryButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "집하예정일확인",
                                        Title = "집하예정일 확인하기"
                                    };
                                    cardButtons.Add(deliveryButton);

                                    CardAction returnButton = new CardAction();
                                    returnButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "예약번호확인",
                                        Title = "예약번호 확인하기"
                                    };
                                    cardButtons.Add(returnButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "문의하실 항목을 선택해 주세요.",
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
                             * apiIntent 예약취소
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_예약취소"))
                            {
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
                                        Value = "예약취소진행1",
                                        Title = "발송취소"
                                    };
                                    cardButtons.Add(cancel1Button);
                                    CardAction cancel2Button = new CardAction();
                                    cancel2Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "예약취소진행2",
                                        Title = "기집하"
                                    };
                                    cardButtons.Add(cancel2Button);
                                    CardAction cancel3Button = new CardAction();
                                    cancel3Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "예약취소진행3",
                                        Title = "이중예약"
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
                                    cancelNumber = Regex.Replace(activity.Text, @"\D", "");
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(bookCancelResult);
                                    String bookCancelResultJsonData = new StreamReader(stream).ReadToEnd();

                                    JArray obj = JArray.Parse(bookCancelResultJsonData);

                                    foreach (JObject jobj in obj)
                                    {
                                        if (jobj["ret_cod"].ToString().Equals("9001"))
                                        {
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction serviceCenterButton = new CardAction();
                                            serviceCenterButton = new CardAction()
                                            {
                                                Type = "openUrl",
                                                Value = "http://m.hanjin.co.kr",
                                                Title = "모바일 고객센터"
                                            };
                                            cardButtons.Add(serviceCenterButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "자동 예약취소가 불가능한 상태입니다. 예약취소는 " + jobj["org_nam"].ToString() + " 집배점/ 전화번호 " + jobj["tel_num"].ToString() + "으로 문의하여 주시거나 모바일 고객센터로 접수바랍니다.",
                                                Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9999"))
                                        {
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction serviceCenterButton = new CardAction();
                                            serviceCenterButton = new CardAction()
                                            {
                                                Type = "openUrl",
                                                Value = "http://m.hanjin.co.kr",
                                                Title = "모바일 고객센터"
                                            };
                                            cardButtons.Add(serviceCenterButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "자동 예약취소가 불가능한 상태입니다. 예약취소는 " + jobj["org_nam"].ToString() + " 집배점/ 전화번호 " + jobj["tel_num"].ToString() + "으로 문의하여 주시거나 모바일 고객센터로 접수바랍니다.",
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
                                                Text = "예약번호 " + bookNumber + " 의 예약취소 처리가 완료되었습니다.",
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
                                        Text = "예약번호 " + invoiceNumber + " 예약처리 취소작업이 종료되었습니다.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("예약확인")||checkNum==true)
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(bookCancelYN);
                                    String bookCancelYNJsonData = new StreamReader(stream).ReadToEnd();

                                    JArray obj = JArray.Parse(bookCancelYNJsonData);
                                    foreach (JObject jobj in obj)
                                    {
                                        String tempDate = jobj["apc_ymd"].ToString();
                                        String yearText = tempDate.Substring(0, 4);
                                        String monthText = tempDate.Substring(4, 2);
                                        String dayText = tempDate.Substring(6, 2);
                                        String dateText = yearText + "년 " + monthText + "월 " + dayText + "일";
                                        String heroCardText = "";

                                        if (jobj["ret_cod"].ToString().Equals("1000"))
                                        {
                                            heroCardText = dateText + "에 예약하신 예약번호 " + bookNumber + "를 취소하시려면 하단의 버튼을 클릭해주세요";
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9001"))
                                        {
                                            heroCardText = "자동 예약취소가 불가능한 상태입니다. 예약취소는 " + jobj["org_nam"].ToString() + " 집배점/ 전화번호 " + jobj["tel_num"].ToString() + "으로 문의하여 주시거나 모바일 고객센터로 접수바랍니다.";
                                        }
                                        else if (jobj["ret_cod"].ToString().Equals("9999"))
                                        {
                                            heroCardText = "예약번호가 존재하지 않습니다.";
                                        }
                                        else
                                        {
                                            heroCardText = "예약번호가 존재하지 않습니다.";
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
                                    //모바일 인증 체크
                                    if (authCheck.Equals("F"))
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction deliveryButton = new CardAction();
                                        deliveryButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "예. 핸드폰인증 하겠습니다",
                                            Title = "예"
                                        };
                                        cardButtons.Add(deliveryButton);

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "아니오. 핸드폰인증 취소하겠습니다",
                                            Title = "아니오"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "택배목록 확인등을 위해서 핸드폰 인증이 필요합니다. 핸드폰 인증을 하신 후에 다시 진행해 주세요<br>핸드폰 인증을 하시겠습니까?<br>또는 예약번호를 바로 입력해 주세요",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(DeliveryList);
                                        String DeliveryListJsonData = new StreamReader(stream).ReadToEnd();

                                        JArray obj = JArray.Parse(DeliveryListJsonData);
                                        int checkInt = obj.Count;

                                        if (checkInt == 0)
                                        {
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction bookButton = new CardAction();
                                            bookButton = new CardAction()
                                            {
                                                Type = "openUrl",
                                                Value = "http://www.hanjin.co.kr/Delivery_html/reserve/login1.jsp?rsr_gbn=",
                                                Title = "신규택배 예약하기"
                                            };
                                            cardButtons.Add(bookButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "예약된 정보가 없습니다.",
                                                Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                        }
                                        else
                                        {
                                            foreach (JObject jobj in obj)
                                            {
                                                List<CardList> text = new List<CardList>();
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                String tempDate = jobj["dlv_ymd"].ToString();
                                                String yearText = tempDate.Substring(0, 4);
                                                String monthText = tempDate.Substring(5, 2);
                                                String dayText = tempDate.Substring(8, 2);
                                                String dateText = yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["dlv_dy"].ToString() + "요일)";

                                                CardAction bookButton = new CardAction();
                                                bookButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = jobj["wbl_num"].ToString() + " 예약확인",
                                                    Title = "예약 내용 확인"
                                                };
                                                cardButtons.Add(bookButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = dateText + " " + jobj["god_nam"].ToString() + jobj["wrk_nam"].ToString() + "<br>" + jobj["wbl_num"].ToString(),
                                                    Buttons = cardButtons,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                        }

                                        SetActivity(apiMakerReply);
                                    }
                                }
                            }
                            else
                            {
                                //if (apiIntent.Equals("F_예약취소"))
                            }
                            

                            
                            /*****************************************************************
                             * apiIntent 택배예약방문지연
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_택배예약방문지연"))
                            {
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Contains("방문지연확인") && containNum == true)
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    JObject obj = JObject.Parse(APIDelayListData);
                                    JArray sample = (JArray)obj["예약상세내용확인"];
                                    foreach (JObject jobj in sample)
                                    {
                                        if (jobj["방문지연여부"].ToString().Equals("YES") && jobj["예약번호"].ToString().Equals(bookNumber))
                                        {
                                            List<CardList> text = new List<CardList>();
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction bookButton = new CardAction();
                                            bookButton = new CardAction()
                                            {
                                                Type = "openUrl",
                                                Value = "http://www.daum.net",
                                                Title = "고객의 말씀"
                                            };
                                            cardButtons.Add(bookButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "고객님의 예약번호는 " + bookNumber + " 입니다<br> 방문지연으로 인해 고객님께 불편드려 죄송합니다.<br> " + jobj["집배점전화번호"].ToString() + " 로 문의부탁 드립니다. ",
                                                Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                            break;
                                        }
                                        else if (jobj["방문지연여부"].ToString().Equals("NO") && jobj["예약번호"].ToString().Equals(bookNumber))
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "고객님의 예약번호는 " + bookNumber + " 입니다<br> 정상적으로 방문했습니다",
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
                                            break;
                                        }
                                        else
                                        {
                                            //NOTHING
                                        }

                                    }
                                }
                                else
                                {
                                    //모바일 인증 체크
                                    if (authCheck.Equals("F"))
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction deliveryButton = new CardAction();
                                        deliveryButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "예. 핸드폰인증 하겠습니다",
                                            Title = "예"
                                        };
                                        cardButtons.Add(deliveryButton);

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "아니오. 핸드폰인증 취소하겠습니다",
                                            Title = "아니오"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "택배목록 확인등을 위해서 핸드폰 인증이 필요합니다. 핸드폰 인증을 하신 후에 다시 진행해 주세요<br>핸드폰 인증을 하시겠습니까?",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(API4Url);
                                        String API4JsonData = new StreamReader(stream).ReadToEnd();

                                        JObject obj = JObject.Parse(API4JsonData);
                                        JArray sample = (JArray)obj["예약상세내용확인"];
                                        int checkInt = sample.Count;

                                        if (checkInt == 0)
                                        {
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "고객님! 현재 문의하신 정보에 해당하는 예약 건을 찾을 수 없습니다."
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                        }
                                        else
                                        {
                                            APIDelayListData = API4JsonData;
                                            foreach (JObject jobj in sample)
                                            {
                                                List<CardList> text = new List<CardList>();
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                CardAction bookButton = new CardAction();
                                                bookButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "예약번호 " + jobj["예약번호"].ToString() + " 방문지연확인",
                                                    Title = "방문지연여부확인"
                                                };
                                                cardButtons.Add(bookButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "고객님! 예약번호 " + jobj["예약번호"].ToString() + " 로 " + jobj["예약일자"].ToString() + " 에 " + jobj["예약종류"].ToString() + " 있습니다.<br>집배점 전화번호는 " + jobj["집배점전화번호"].ToString() + " 입니다.",
                                                    Buttons = cardButtons,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                        }

                                        SetActivity(apiMakerReply);
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
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Contains("운송장번호") || containNum == true)//직접이던 선택이던
                                {
                                    if (containNum == true) //숫자가 포함(직접이던 선택이던)
                                    {
                                        invoiceNumber = Regex.Replace(activity.Text, @"\D", "");
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(goodLocation);
                                        String goodLocationJsonData = new StreamReader(stream).ReadToEnd();

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
                                                statusText = "고객님께서 문의하신 운송장 번호 ("+ invoiceNumber + ")는 현재 상품 발송을 위해 운송장이 접수된 상태입니다.<br>터미널 입고 시점부터 배송 일정 조회가 가능하며 당일 출고한 상품은 발송일 오후나 다음날 다시 한번 배송조회 확인해 주시기 바랍니다.<br>1~2일이 경과하여도 상품 이동 내역이 없는 경우에는 주문업체(쇼핑몰) 또는 보내는 분께 상품 발송 일자와 한진택배로 발송된 상품인지 문의해 주시기 바랍니다.";
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("20"))
                                            {
                                                wrkCod = "상품발송대기중";
                                                statusText = "고객님께서 문의하신 운송장 번호(" + invoiceNumber + ")는 발송 터미널에 입고되어 상품 발송 대기 중입니다. 배송조회 하시는 다음날 다시 한번 확인해 주시기 바랍니다. 지역(현장) 사정에 따라 배송은 1~2일 소요될 수 있는 점 양해바랍니다.";
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("30"))
                                            {
                                                wrkCod = "이동중";
                                                statusText = "고객님께서 문의하신 운송장 번호 (" + invoiceNumber + ")는 상품이 발송되어, 이동 중입니다. 배송예정 시간 확인은 당일 도착 물량에 따라 변동이 될 수 있으니 다시 한번 확인 해 주시기 바랍니다. 배송지역 사정에 따라 배송은 1~2일 소요될 수 있는 점 양해바랍니다.";
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("40"))
                                            {
                                                wrkCod = "배송준비";
                                                statusText = "고객님께서 문의하신 운송장 번호 (" + invoiceNumber + ")는 현재 배송지역 터미널에 도착하여, 배송 담당자에게 인계를 위해 준비중입니다.<br>배송예정 시간 확인은 당일 도착 물량에 따라 변동이 될 수 있으며 배송은 1~2일 소요 될 수 있는 점 양해바랍니다.";
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
                                                telNum = jobj["tel_num"].ToString();
                                                empTel = jobj["emp_tel"].ToString();

                                                statusText = "고객님께서 문의하신 운송장 번호 (" + invoiceNumber + ")는 배송직원이 배송 중으로 " + dateText + " ○○~○○시 배송예정이며 배송예정 시간은 당일 도착 물량에 따라 변동이 될 수 있으며, 배송은 1~2일 소요 될 수 있는점 양해바랍니다. 자세한 사항은 " + orgNam + "집배점 전화번호 " + telNum + "또는  배송직원 전화번호 " + empTel + "로 문의하시기 바랍니다.";
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
                                                telNum = jobj["tel_num"].ToString();
                                                empTel = jobj["emp_tel"].ToString();
                                                statusText = "고객님께서 문의하신 운송장 번호 (" + invoiceNumber + ")는 " + dateText + " ○○:○○ 배송완료하였습니다.<br>자세한 사항은 " + orgNam + "집배점 전화번호 " + telNum + " 또는  배송직원 전화번호 " + empTel + " 로 문의하시기 바랍니다.";
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("70"))
                                            {
                                                wrkCod = "오도착";
                                                statusText = "고객님께서 문의하신 운송장 번호 (" + invoiceNumber + ")는 현재 배송지역이 아닌 다른 지역에 경유 중으로 배송은 1~2일 더 소요될 수 있는 점 양해부탁드립니다.<br>배송조회 하시는 다음날 다시 한번 확인해 주시기 바랍니다. ";
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("80"))
                                            {
                                                wrkCod = "미배송";
                                                orgNam = jobj["org_nam"].ToString();
                                                telNum = jobj["tel_num"].ToString();
                                                empTel = jobj["emp_tel"].ToString();
                                                statusText = "고객님께서 문의하신 운송장 번호 (" + invoiceNumber + ")는 배송지역 사정으로 배송이 1~2일 더 소요될 수 있는 점 양해부탁드립니다.<br>자세한 사항은 " + orgNam + "집배점 전화번호 " + telNum + " 또는 배송직원 전화번호 " + empTel + " 로 문의하시기 바랍니다.";
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("90"))
                                            {
                                                wrkCod = "배송오류";
                                                statusText = "고객님께서 문의하신 운송장 번호 (" + invoiceNumber + ")는 배송관련 자동안내가 어렵습니다.<br>자세한 문의 내용은 한진택배 홈페이지 고객의 말씀 또는 모바일 고객센터(m.hanjin.co.kr)로 접수 부탁드립니다.";
                                            }
                                            else if (jobj["wrk_cod"].ToString().Equals("99"))
                                            {
                                                wrkCod = "기타에러";
                                                statusText = "기타에러";
                                            }
                                            else
                                            {
                                                wrkCod = "오류";
                                                statusText = "오류";
                                            }
                                            
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = statusText,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                            SetActivity(apiMakerReply);
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
                                else
                                {
                                    //모바일 인증 체크
                                    if (authCheck.Equals("F"))
                                    {
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        CardAction deliveryButton = new CardAction();
                                        deliveryButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "예. 핸드폰인증 하겠습니다",
                                            Title = "예"
                                        };
                                        cardButtons.Add(deliveryButton);

                                        CardAction returnButton = new CardAction();
                                        returnButton = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "아니오. 핸드폰인증 취소하겠습니다",
                                            Title = "아니오"
                                        };
                                        cardButtons.Add(returnButton);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "택배목록 확인등을 위해서 핸드폰 인증이 필요합니다. 핸드폰 인증을 하신 후에 다시 진행해 주세요<br>핸드폰 인증을 하시겠습니까?",
                                            Buttons = cardButtons,
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                    else
                                    {
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(DeliveryList);
                                        String DeliveryListJsonData = new StreamReader(stream).ReadToEnd();

                                        JArray obj = JArray.Parse(DeliveryListJsonData);
                                        int checkInt = obj.Count;

                                        if (checkInt == 0)
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "고객님! 현재 문의하신 정보에 해당하는 예약 건을 찾을 수 없습니다."
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                        }
                                        else
                                        {
                                            foreach (JObject jobj in obj)
                                            {
                                                List<CardList> text = new List<CardList>();
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                String tempDate = jobj["dlv_ymd"].ToString();
                                                String yearText = tempDate.Substring(0, 4);
                                                String monthText = tempDate.Substring(5, 2);
                                                String dayText = tempDate.Substring(8, 2);
                                                String dateText = yearText + "년 " + monthText + "월 " + dayText + "일(" + jobj["dlv_dy"].ToString() + "요일)";

                                                CardAction bookButton = new CardAction();
                                                bookButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "운송장 번호 " + jobj["wbl_num"].ToString() + "",
                                                    Title = "배송일정 확인"
                                                };
                                                cardButtons.Add(bookButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = dateText + " " + jobj["god_nam"].ToString() + jobj["wrk_nam"].ToString() + "<br>" + jobj["wbl_num"].ToString(),
                                                    Buttons = cardButtons,
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
                                                apiMakerReply.Attachments.Add(plAttachment);
                                            }
                                        }

                                        SetActivity(apiMakerReply);
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
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Contains("운송장번호") && apiActiveText.Contains("연락처찾기"))
                                {
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(API4Url);
                                    String API4JsonData = new StreamReader(stream).ReadToEnd();

                                    JObject obj = JObject.Parse(API4JsonData);
                                    JArray sample = (JArray)obj["예약상세내용확인"];

                                    APIFindListData = API4JsonData;
                                    int checkInt = sample.Count;

                                    if (checkInt == 0)
                                    {
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "고객님! 현재 문의하신 정보에 해당하는 예약 건을 찾을 수 없습니다."
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                    }
                                    else
                                    {
                                        foreach (JObject jobj in sample)
                                        {
                                            List<CardList> text = new List<CardList>();
                                            List<CardAction> cardButtons = new List<CardAction>();

                                            CardAction bookButton = new CardAction();
                                            bookButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "운송장 번호 " + jobj["운송장번호"].ToString() + " 집배점/기사 연락처",
                                                Title = "집배점/기사연락처 찾기"
                                            };
                                            cardButtons.Add(bookButton);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "고객님! 요청하신 정보는 다음과 같습니다.<br>운송장번호: " + jobj["운송장번호"].ToString(),
                                                Buttons = cardButtons,
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            apiMakerReply.Attachments.Add(plAttachment);
                                        }
                                    }

                                    SetActivity(apiMakerReply);

                                }
                                else if (apiActiveText.Contains("운송장 번호") || containNum == true)//직접이던 선택이던
                                {
                                    invoiceNumber = Regex.Replace(activity.Text, @"\D", "");
                                    JObject obj = JObject.Parse(APIFindListData);
                                    JArray sample = (JArray)obj["예약상세내용확인"];
                                    foreach (JObject jobj in sample)
                                    {
                                        if (jobj["운송장번호"].ToString().Equals(invoiceNumber))
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "고객님! 연락처는 다음과 같아요.<br>담당기사 : " + jobj["담당기사이름"].ToString() + " " + jobj["담당기사연락처"].ToString() + "",
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
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "고객님의 정확한 주소를 입력해 주세요.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Equals("주소다시입력"))
                                {
                                    checkFindAddressCnt = "T";
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "고객님의 정확한 주소를 다시 입력해 주세요.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else
                                {
                                    if (checkFindAddressCnt.Equals("T")) //주소로서 데이터를 찾아야 한다
                                    {
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(findOrgInfo);
                                        String findOrgInfoJsonData = new StreamReader(stream).ReadToEnd();

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
                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "네. 고객님<br>문의하신 지역의 담당기사 연락처입니다.<br>근무 외 시간에는 통화가 어려우니 참고 해주시기 바랍니다.<br>(*근무시간: 09시~18시)<br><br>담당기사: "+ jobj["emp_tel"].ToString() + "<br>집배점: "+ jobj["org_nam"].ToString()+" "+ jobj["tel_num"].ToString() + "<br><br>고객님께 작은 도움이 되었기를 바랍니다. 추가적으로 궁금한 사항은 언제든지 문의해 주세요.",
                                                };

                                                Attachment plAttachment = plCard.ToAttachment();
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
                                            Value = "운송장번호로 집배점/기사 연락처 찾기",
                                            Title = "운송장번호로 집배점/기사 연락처 찾기"
                                        };
                                        cardButtons.Add(find1Button);

                                        CardAction find2Button = new CardAction();
                                        find2Button = new CardAction()
                                        {
                                            Type = "imBack",
                                            Value = "주소로 집배점/기사 연락처 찾기",
                                            Title = "주소로 집배점/기사 연락처 찾기"
                                        };
                                        cardButtons.Add(find2Button);

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "고객님. 문의하실 항목을 선택해 주세요",
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
                                apiOldIntent = apiIntent;
                                db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "API_OLDINTENT", apiOldIntent);
                                if (apiActiveText.Contains("예핸드폰인증"))
                                {
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
                                        Text = "고객님의 휴대폰 번호로 인증에 동의하시면 고객님의 택배목옥을 확인하실 수 있습니다<br>본 인증 절차는 고객님의 택배목록을 조회/제공하기 위한 목적으로만 활용되며, 별도 보관하지 않습니다<br><br>인증 절차에 동의하시겠습니까?",
                                        Buttons = cardButtons,
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("아니오핸드폰인증"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "핸드폰 인증 진행이 취소되었습니다.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Equals("동의"))
                                {
                                    //전화번호 넘기고 인증번호 받는 API 넣기
                                    authNumber = "123456";
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "전달받으신 인증번호 6자리(123456)를 정확히 입력해 주세요",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Equals("미동의"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "택배목록 조회등을 위해서는 휴대폰인증이 반드시 필요합니다.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (checkNum == true) //입력값이 숫자이면 인증번호라 판단한다.
                                {
                                    //인증번호 넘기고 결과 받는 api 넣기
                                    if (apiActiveText.Equals(authNumber))
                                    {
                                        authNumber = "";//기존 인증번호 삭제
                                        authCheck = "T";//인증성공
                                        db.UserCheckUpdate(activity.ChannelId, activity.Conversation.Id, "AUTH_CHECK", authCheck); //AUTH_CHECK UPDATE
                                        apiOldIntent = "";
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "인증되었습니다. 감사합니다.",
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
                                            Text = "인증에 실패되었습니다. 동의부터 다시 진행해 주세요",
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }

                                }
                                else
                                {

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
                        int dbResult = db.insertUserQuery(relationList, luisId, luisIntent, luisEntities, luisIntentScore, replyresult, orgMent);

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
                            //Title = text[i].cardTitle,
                            Text = text[i].cardText,
                            Buttons = cardButtons
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

        public static async Task<JObject> GetIntentFromBotLUIS(List<string[]> textList, string query)
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
            for (int i = 0; i < 2; i++)
            {
                //entities 0일 경우 PASS
                //if ((int)Luis_before[i]["entities"].Count() > 0)
                if (1 != 0)
                {
                    //intent None일 경우 PASS
                    if (Luis_before[i]["intents"][0]["intent"].ToString() != "None")
                    {
                        //제한점수 체크
                        if ((float)Luis_before[i]["intents"][0]["score"] > Convert.ToDouble(MessagesController.LUIS_SCORE_LIMIT))
                        {
                            if ((float)Luis_before[i]["intents"][0]["score"] > luisScoreCompare)
                            {
                                //LuisName = returnLuisName[i];
                                Luis = Luis_before[i];
                                luisScoreCompare = (float)Luis_before[i]["intents"][0]["score"];
                                //Debug.WriteLine("GetMultiLUIS() LuisName1 : " + LuisName);
                            }
                            else
                            {
                                //LuisName = returnLuisName[i];
                                //Luis = Luis_before[i];
                                //Debug.WriteLine("GetMultiLUIS() LuisName2 : " + LuisName);
                            }

                        }
                    }
                }
            }
            return Luis;
        }
    }
}