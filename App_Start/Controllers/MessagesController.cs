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
        static public string API1Url = "http://www.jobible.co.kr/json.data";                 //반품예약, 반품예약확인
        static public string API2Url = "http://www.jobible.co.kr/json1.data";                 //반품예약취소
        static public string API3Url = "http://www.jobible.co.kr/json2.data";                 //집하예정일확인
        static public string API4Url = "http://www.jobible.co.kr/json3.data";                 //예약번호확인
        static public string API5Url = "http://www.jobible.co.kr/json4.data";                 //택배예약내용확인
        static public string API6Url = "http://www.jobible.co.kr/json5.data";                 //택배예약취소
        static public string API7Url = "http://www.jobible.co.kr/json6.data";                 //배송일정상세조회
        static public string APIDeliverListData = "";                 //택배반품리스트 json 데이터
        static public string APIReturnBookListData = "";                 //반품예약리스트 json 데이터
        static public string APIDelayListData = "";                 //택배방문지연 json 데이터
        static public string APIFindListData = "";                 //집배점기사찾기 json 데이터
        static public string apiIntent = "";                 //api 용 intent
        static public string apiOldIntent = "None";                 //api 용 intent(old)
        static public string invoiceNumber = "";                 //운송장 번호
        static public string bookNumber = "";                 //예약 번호
        static public string APIResult = "";                 //api 결과-쏘리메세지 안 나오게 하기 위해서.
        static public string APILuisIntent = null;                 //API 용 루이스 INTENT
        static public string authCheck = "F";                 //인증 체크-리스트 추출용(T/F)
        static public string authNumber = "";                 //인증 번호

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
            else if (activity.Type == ActivityTypes.Message)
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

                            if (containNum == true) //숫자가 포함되어 있으면 대화셋의 데이터는 나오지 않는다. 나중에 숫자 길이까지 체크(운송장, 예약번호, 전화번호)
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
                        if (luisIntent == ""||luisIntent.Equals("None"))
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

                        }
                        Debug.WriteLine("apiIntentapiIntent : " + apiIntent);
                        
                        
                        //////////////////////////////////////////////

                        string smallTalkConfirm = "";

                        if (!string.IsNullOrEmpty(luisIntent))
                        {
                            relationList = db.DefineTypeChkSpare(luisIntent, luisEntities);
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
                        if (relationList == null&& (apiIntent == null||apiIntent.Equals("None")))
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

                            Activity apiMakerReply = activity.CreateReply();

                            apiMakerReply.Recipient = activity.From;
                            apiMakerReply.Type = "message";
                            apiMakerReply.Attachments = new List<Attachment>();

                            Regex r = new Regex("[0-9]");
                            bool checkNum = Regex.IsMatch(activity.Text, @"^\d+$"); //입력값이 숫자인지 파악.
                            bool containNum = r.IsMatch(activity.Text); //숫자여부 확인
                            String apiActiveText = Regex.Replace(activity.Text, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);//공백 및 특수문자 제거
                            String onlyNumber = Regex.Replace(activity.Text, @"\D","");
                            /*
                             * [APIINTENT]::글자
                             * */
                            if (activity.Text.Contains("[")&& activity.Text.Contains("]"))
                            {
                                int apiIntentS = activity.Text.IndexOf("[");
                                int apiIntentE = activity.Text.IndexOf("]");
                                apiIntent = activity.Text.Substring(apiIntentS + 1, (apiIntentE - 1) - apiIntentS);
                                Debug.WriteLine("apiIntent[]-------------" + apiIntent);
                            }
                            Debug.WriteLine("apiIntent2-------------" + apiIntent);

                            /*
                             * old api intent 초기화 시키기
                             * */
                            if (apiIntent.Equals("None"))
                            {

                            }
                            else
                            {
                                apiOldIntent = "None";
                            }
                            
                            if (apiOldIntent.Equals("None"))
                            {

                            }
                            else
                            {
                                apiIntent = apiOldIntent;
                            }
                            Debug.WriteLine("apiIntent3-------------" + apiIntent);

                            /*****************************************************************
                            * apiIntent F_예약
                            * 
                            ************************************************************** */
                            if (apiIntent.Equals("F_예약"))
                            {
                                apiOldIntent = apiIntent;
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
                                    List<CardList> text = new List<CardList>();
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    JObject obj = JObject.Parse(APIDeliverListData);
                                    JArray sample = (JArray)obj["반품예약"];
                                    String returnYN = "";//반품가능여부

                                    foreach (JObject jobj in sample)
                                    {
                                        if (onlyNumber.Equals(jobj["운송장번호"].ToString()))
                                        {
                                            returnYN = jobj["반품처리가능"].ToString();
                                            break;
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
                                            Text = "운송장 번호 " + onlyNumber + "를 반품처리할수 없습니다.",
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
                                        Stream stream = webClient.OpenRead(API1Url);
                                        String API1JsonData = new StreamReader(stream).ReadToEnd();

                                        JObject obj = JObject.Parse(API1JsonData);
                                        JArray sample = (JArray)obj["반품예약"];

                                        APIDeliverListData = API1JsonData;

                                        apiIntent = luisIntent;
                                        List<CardList> text = new List<CardList>();
                                        List<CardAction> cardButtons = new List<CardAction>();

                                        int i = 1;
                                        foreach (JObject jobj in sample)
                                        {
                                            CardAction plButton = new CardAction();
                                            plButton = new CardAction()
                                            {
                                                Type = "imBack",
                                                Value = "운송장번호 " + jobj["운송장번호"].ToString() + " 반품택배예약",
                                                Title = "운송장번호" + i + ""
                                            };
                                            cardButtons.Add(plButton);
                                            i++;
                                        }

                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            //Text = "네~ 고객님. 문의하신 정보에 해당하는 운송장 목록(" + apiIntent + ")입니다.",
                                            Text = "반품 접수를 원하시는 택배를 선택해 주세요",
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
                                        Stream stream = webClient.OpenRead(API3Url);
                                        String API3JsonData = new StreamReader(stream).ReadToEnd();

                                        JObject obj = JObject.Parse(API3JsonData);
                                        JArray sample = (JArray)obj["집하예정일확인"];
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
                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "고객님! " + jobj["집하날짜"].ToString() + " 집하 예정입니다.<br> 집배점 전화번호는 " + jobj["집배점전화번호"].ToString() + " 입니다."
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
                                            foreach (JObject jobj in sample)
                                            {
                                                List<CardList> text = new List<CardList>();
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                CardAction bookButton = new CardAction();
                                                bookButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = jobj["예약번호"].ToString() + " 예약 내용 확인",
                                                    Title = "예약 내용 확인"
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
                                else if (apiActiveText.Contains("예약내용확인"))
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(API5Url);
                                    String API5JsonData = new StreamReader(stream).ReadToEnd();

                                    JObject obj = JObject.Parse(API5JsonData);
                                    JArray sample = (JArray)obj["택배예약내용확인"];

                                    foreach (JObject jobj in sample)
                                    {
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "고객님께서 입력하신 예약번호 " + jobj["예약번호"].ToString() + " 는 " + jobj["예약일자"].ToString() + " 정상 예약접수 되어 있습니다.<br>방문일정 또는 예약변경은 " + jobj["집배점이름"].ToString() + " 집배점 전화번호" + jobj["집배점전화번호"].ToString() + " 로 문의 부탁드립니다.",
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
                                if (apiActiveText.Contains("반품예약취소선택"))
                                {
                                    invoiceNumber = Regex.Replace(activity.Text, @"\D", "");
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction cancel1Button = new CardAction();
                                    cancel1Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "운송장 번호 " + invoiceNumber + " <br>취소사유: aaaaaaaa<br>반품예약 취소진행",
                                        Title = "취소사유 1"
                                    };
                                    cardButtons.Add(cancel1Button);
                                    CardAction cancel2Button = new CardAction();
                                    cancel2Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "운송장 번호 " + invoiceNumber + " <br>취소사유: aaaaaaaa<br>반품예약 취소진행",
                                        Title = "취소사유 2"
                                    };
                                    cardButtons.Add(cancel2Button);
                                    CardAction cancel3Button = new CardAction();
                                    cancel3Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "운송장 번호 " + invoiceNumber + " <br>취소사유: aaaaaaaa<br>반품예약 취소진행",
                                        Title = "취소사유 3"
                                    };
                                    cardButtons.Add(cancel3Button);

                                    CardAction noButton = new CardAction();
                                    noButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "아니오. " + invoiceNumber + " 반품예약 취소하지 않습니다.",
                                        Title = "아니오"
                                    };
                                    cardButtons.Add(noButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "운송장 번호 " + invoiceNumber + " 반품예약을 취소하시겠습니까?",
                                        Buttons = cardButtons,
                                    };

                                    invoiceNumber = activity.Text;
                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("택배예약취소선택"))
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction cancel1Button = new CardAction();
                                    cancel1Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "예약 번호 " + bookNumber + " <br>취소사유: aaaaaaaa<br>택배예약 취소진행",
                                        Title = "취소사유 1"
                                    };
                                    cardButtons.Add(cancel1Button);
                                    CardAction cancel2Button = new CardAction();
                                    cancel2Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "예약 번호 " + bookNumber + " <br>취소사유: aaaaaaaa<br>택배예약 취소진행",
                                        Title = "취소사유 2"
                                    };
                                    cardButtons.Add(cancel2Button);
                                    CardAction cancel3Button = new CardAction();
                                    cancel3Button = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "예약 번호 " + bookNumber + " <br>취소사유: aaaaaaaa<br>택배예약 취소진행",
                                        Title = "취소사유 3"
                                    };
                                    cardButtons.Add(cancel3Button);

                                    CardAction noButton = new CardAction();
                                    noButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "아니오. " + bookNumber + " 택배예약 취소하지 않습니다.",
                                        Title = "아니오"
                                    };
                                    cardButtons.Add(noButton);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "예약 번호 " + bookNumber + " 택배예약을 취소하시겠습니까?",
                                        Buttons = cardButtons,
                                    };

                                    invoiceNumber = activity.Text;
                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("반품예약취소진행"))
                                {
                                    invoiceNumber = Regex.Replace(activity.Text, @"\D", "");
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(API6Url);
                                    String API6JsonData = new StreamReader(stream).ReadToEnd();

                                    JObject obj = JObject.Parse(API6JsonData);
                                    JArray sample = (JArray)obj["택배예약취소"];

                                    foreach (JObject jobj in sample)
                                    {
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "운송장번호 " + jobj["운송장번호"].ToString() + " 반품예약 취소처리가 완료되었습니다.<br>취소사유: aaaaaaaaaaaaaaa",
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                }
                                else if (apiActiveText.Contains("택배예약취소진행"))
                                {
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(API6Url);
                                    String API6JsonData = new StreamReader(stream).ReadToEnd();

                                    JObject obj = JObject.Parse(API6JsonData);
                                    JArray sample = (JArray)obj["택배예약취소"];

                                    foreach (JObject jobj in sample)
                                    {
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Title = "",
                                            Text = "예약번호 " + jobj["예약번호"].ToString() + " 택배예약 취소처리가 완료되었습니다.<br>취소사유: bbbbbbbbbbb",
                                        };

                                        Attachment plAttachment = plCard.ToAttachment();
                                        apiMakerReply.Attachments.Add(plAttachment);
                                        SetActivity(apiMakerReply);
                                    }
                                }
                                else if (apiActiveText.Contains("아니오") && apiActiveText.Contains("반품예약취소"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "운송장번호 " + invoiceNumber + " 반품 예약처리 취소작업이 종료되었습니다.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Contains("아니오") && apiActiveText.Contains("택배예약취소"))
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = "",
                                        Text = "예약번호 " + bookNumber + " 택배 예약처리 취소작업이 종료되었습니다.",
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    apiMakerReply.Attachments.Add(plAttachment);
                                    SetActivity(apiMakerReply);
                                }
                                else if (apiActiveText.Equals("반품예약취소") || apiActiveText.Equals("택배예약취소"))
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
                                        String cancelType = "cancel";
                                        if (apiActiveText.Equals("반품예약취소"))
                                        {
                                            cancelType = "returnCancel";
                                        }
                                        else if (apiActiveText.Equals("택배예약취소"))
                                        {
                                            cancelType = "deliveryCancel";
                                        }
                                        else
                                        {

                                        }
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(API4Url);
                                        String API4JsonData = new StreamReader(stream).ReadToEnd();

                                        JObject obj = JObject.Parse(API4JsonData);
                                        JArray sample = (JArray)obj["예약상세내용확인"];
                                        int checkInt = 0;

                                        if (cancelType.Equals("deliveryCancel"))
                                        {
                                            // 택배예약 취소
                                            foreach (JObject jobj in sample)//예약리스트에서 반품예약인 것만.
                                            {
                                                if (jobj["예약종류"].ToString().Equals("택배예약"))
                                                {
                                                    checkInt++;
                                                }
                                            }

                                            if (checkInt == 0)
                                            {
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                CardAction bookButton = new CardAction();
                                                bookButton = new CardAction()
                                                {
                                                    Type = "openUrl",
                                                    Value = "http://www,daum.net",
                                                    Title = "신규택배 예약하기"
                                                };
                                                cardButtons.Add(bookButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "택배예약된 정보가 없습니다.",
                                                    Buttons = cardButtons,
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

                                                    if (jobj["예약종류"].ToString().Equals("택배예약"))
                                                    {
                                                        CardAction bookButton = new CardAction();
                                                        bookButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "예약번호 " + jobj["예약번호"].ToString() + " 택배예약 확인",
                                                            Title = "예약 내용 확인"
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
                                                    else
                                                    {
                                                        //nothing
                                                    }

                                                }
                                            }

                                            SetActivity(apiMakerReply);

                                        }
                                        else if (cancelType.Equals("returnCancel"))
                                        {
                                            //반품예약 취소
                                            foreach (JObject jobj in sample)//예약리스트에서 반품예약인 것만.
                                            {
                                                if (jobj["예약종류"].ToString().Equals("반품예약"))
                                                {
                                                    checkInt++;
                                                }
                                            }

                                            if (checkInt == 0)
                                            {
                                                List<CardAction> cardButtons = new List<CardAction>();

                                                CardAction bookButton = new CardAction();
                                                bookButton = new CardAction()
                                                {
                                                    Type = "openUrl",
                                                    Value = "http://www,daum.net",
                                                    Title = "반품 예약하기"
                                                };
                                                cardButtons.Add(bookButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "반품예약된 정보가 없습니다.",
                                                    Buttons = cardButtons,
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

                                                    if (jobj["예약종류"].ToString().Equals("반품예약"))
                                                    {
                                                        CardAction bookButton = new CardAction();
                                                        bookButton = new CardAction()
                                                        {
                                                            Type = "imBack",
                                                            Value = "운송장번호 " + jobj["운송장번호"].ToString() + " 반품예약확인",
                                                            Title = "예약 내용 확인"
                                                        };
                                                        cardButtons.Add(bookButton);

                                                        UserHeroCard plCard = new UserHeroCard()
                                                        {
                                                            Title = "",
                                                            Text = "고객님! 운송장번호 " + jobj["운송장번호"].ToString() + " 로 " + jobj["예약일자"].ToString() + " 에 " + jobj["예약종류"].ToString() + " 있습니다.<br>집배점 전화번호는 " + jobj["집배점전화번호"].ToString() + " 입니다.",
                                                            Buttons = cardButtons,
                                                        };

                                                        Attachment plAttachment = plCard.ToAttachment();
                                                        apiMakerReply.Attachments.Add(plAttachment);
                                                    }
                                                    else
                                                    {
                                                        //nothing
                                                    }
                                                }
                                            }

                                            SetActivity(apiMakerReply);
                                        }
                                        else
                                        {
                                            //error
                                        }
                                    }
    
                                }
                                else if (apiActiveText.Contains("반품예약확인") || apiActiveText.Contains("택배예약확인"))
                                {
                                    invoiceNumber = Regex.Replace(activity.Text, @"\D", "");
                                    bookNumber = Regex.Replace(activity.Text, @"\D", "");
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(API4Url);
                                    String API4JsonData = new StreamReader(stream).ReadToEnd();

                                    JObject obj = JObject.Parse(API4JsonData);
                                    JArray sample = (JArray)obj["예약상세내용확인"];

                                    String bookType = "book";
                                    if (apiActiveText.Contains("반품예약확인"))
                                    {
                                        bookType = "returnBook";
                                    }
                                    else if (apiActiveText.Contains("택배예약확인"))
                                    {
                                        bookType = "deliveryBook";
                                    }
                                    else
                                    {

                                    }

                                    if (bookType.Equals("returnBook"))
                                    {
                                        foreach (JObject jobj in sample)
                                        {
                                            if (jobj["운송장번호"].ToString().Equals(invoiceNumber))
                                            {
                                                List<CardAction> cardButtons = new List<CardAction>();
                                                CardAction bookButton = new CardAction();
                                                bookButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "운송장번호 " + jobj["운송장번호"].ToString() + " 반품예약 취소선택",
                                                    Title = "반품 예약 취소"
                                                };
                                                cardButtons.Add(bookButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "운송장번호 " + jobj["운송장번호"].ToString() + " , \"" + jobj["상품명"].ToString() + "\" 는 반품예약 접수되어 있습니다.",
                                                    Buttons = cardButtons,
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
                                    else if (bookType.Equals("deliveryBook"))
                                    {
                                        foreach (JObject jobj in sample)
                                        {
                                            if (jobj["예약번호"].ToString().Equals(bookNumber))
                                            {
                                                List<CardAction> cardButtons = new List<CardAction>();
                                                CardAction bookButton = new CardAction();
                                                bookButton = new CardAction()
                                                {
                                                    Type = "imBack",
                                                    Value = "운송장번호 " + jobj["예약번호"].ToString() + " 택배예약 취소선택",
                                                    Title = "택배 예약 취소"
                                                };
                                                cardButtons.Add(bookButton);

                                                UserHeroCard plCard = new UserHeroCard()
                                                {
                                                    Title = "",
                                                    Text = "고객님께서 입력하신 예약번호 " + jobj["예약번호"].ToString() + " 는 " + jobj["예약일자"].ToString() + " 정상 예약접수 되어 있습니다.<br>방문일정 또는 예약변경은 집배점 전화번호" + jobj["집배점전화번호"].ToString() + " 로 문의 부탁드립니다.",
                                                    Buttons = cardButtons,
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

                                    }

                                }
                                else
                                {
                                    List<CardAction> cardButtons = new List<CardAction>();

                                    CardAction deliveryButton = new CardAction();
                                    deliveryButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "반품예약취소",
                                        Title = "반품예약 취소하기"
                                    };
                                    cardButtons.Add(deliveryButton);

                                    CardAction returnButton = new CardAction();
                                    returnButton = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "택배예약취소",
                                        Title = "택배예약 취소하기"
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
                             * apiIntent 택배예약방문지연
                             * 
                             ************************************************************** */
                            if (apiIntent.Equals("F_택배예약방문지연"))
                            {
                                apiOldIntent = apiIntent;
                                if (apiActiveText.Equals("택배예약방문지연"))
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
                                else if (apiActiveText.Contains("방문지연확인") && containNum == true)
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
                                if (apiActiveText.Contains("운송장번호") || containNum == true)//직접이던 선택이던
                                {
                                    if (containNum == true) //숫자가 포함(직접이던 선택이던)
                                    {
                                        invoiceNumber = Regex.Replace(activity.Text, @"\D", "");
                                        WebClient webClient = new WebClient();
                                        Stream stream = webClient.OpenRead(API7Url);
                                        String API4JsonData = new StreamReader(stream).ReadToEnd();

                                        JObject obj = JObject.Parse(API4JsonData);
                                        JArray sample = (JArray)obj["배송일정상세조회"];
                                        foreach (JObject jobj in sample)
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Title = "",
                                                Text = "고객님의 문의하신 운송장 번호( " + invoiceNumber + ")는 현재 " + jobj["배송상태"].ToString() + " 중이며 " + jobj["현재위치"].ToString() + " 중입니다.<br>자세한 문의는 1588-0011로 문의부탁드립니다",
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
                                    WebClient webClient = new WebClient();
                                    Stream stream = webClient.OpenRead(API4Url);
                                    String API4JsonData = new StreamReader(stream).ReadToEnd();

                                    JObject obj = JObject.Parse(API4JsonData);
                                    JArray sample = (JArray)obj["예약상세내용확인"];
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
                                                Value = "운송장 번호 " + jobj["운송장번호"].ToString() + "",
                                                Title = "배송일정 확인"
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
                                                Text = "고객님! 요청하신 정보는 다음과 같습니다.<br>운송장번호: " + jobj["운송장번호"].ToString() ,
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