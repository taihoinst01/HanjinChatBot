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
//using SSODecodeCJW;
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
        static public string[] LUIS_NM = new string[5];        //루이스 이름
        static public string[] LUIS_APP_ID = new string[5];    //루이스 app_id
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

            //건의사항용 userData 선언
            List<UserData> userData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);

            if (userData.Count() == 0)
            {
                int userDataResult = db.UserDataInsert(activity.ChannelId, activity.Conversation.Id);
            }
            DButil.HistoryLog("userData insert end ");
            if (activity.Type == ActivityTypes.ConversationUpdate && activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
            {
                startTime = DateTime.Now;

                //파라메터 호출
                if (LUIS_NM.Count(s => s != null) > 0)
                {
                    //string[] LUIS_NM = new string[10];
                    Array.Clear(LUIS_NM, 0, LUIS_NM.Length);
                }

                if (LUIS_APP_ID.Count(s => s != null) > 0)
                {
                    //string[] LUIS_APP_ID = new string[10];
                    Array.Clear(LUIS_APP_ID, 0, LUIS_APP_ID.Length);
                }
                //Array.Clear(LUIS_APP_ID, 0, 10);
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
            else if (activity.Type == ActivityTypes.Message && activity.Text.Contains("sso:"))
            {
                //사용자ID
                string userID = "";
                DButil.HistoryLog("start sso : ");
                String ssoMessage = activity.Text;
                DButil.HistoryLog("ssoMessage : " + ssoMessage);
                userID = dbutil.GetSSO(ssoMessage);
                //기존 계정 삭제
                db.UserDataDeleteUserID(userID);
                //ID 입력
                db.UserDataUpdateUserID(activity.ChannelId, activity.Conversation.Id, "userid" ,userID);
                //모바일 여부 입력
                String mobileyn = activity.Text.Substring(0, 1);
                db.UserDataUpdateUserID(activity.ChannelId, activity.Conversation.Id, "mobileyn", mobileyn);
                //sso입력
                db.UserDataUpdateUserID(activity.ChannelId, activity.Conversation.Id, "sso", ssoMessage.Replace("Msso:", "").Replace("Psso:", ""));

                DButil.HistoryLog("sso : " + userID);
            }
            else if (activity.Type == ActivityTypes.Message && !activity.Text.Contains("sso:"))
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

                    //건의사항 및 sap 초기화 시나리오가 있으면 bannedMsg.cardText null 처리
                    if (userData[0].sap != 0 || userData[0].loop != 0)
                    {
                        bannedMsg.cardText = null;
                    }

                    //금칙어 처리
                    if (bannedMsg.cardText != null )
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

                        //SAP 처리                        
                        if (orgMent.Contains("SAP#"))
                        {
                            DButil.HistoryLog("SAP 처리 시작");
                            //SAP 용어 확인
                            string qnAMakerAnswer = dbutil.GetQnAMaker(orgMent.Replace("SAP#",""));

                            if (!qnAMakerAnswer.Contains("No good match"))
                            {
                                Activity qnAMakerReply = activity.CreateReply();

                                qnAMakerReply.Recipient = activity.From;
                                qnAMakerReply.Type = "message";
                                qnAMakerReply.Attachments = new List<Attachment>();

                                List<CardList> text = new List<CardList>();

                                UserHeroCard plCard = new UserHeroCard()
                                {
                                    Title = "용어 사전",
                                    Text = qnAMakerAnswer
                                };

                                Attachment plAttachment = plCard.ToAttachment();
                                qnAMakerReply.Attachments.Add(plAttachment);

                                SetActivity(qnAMakerReply);

                                db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "loop");
                                db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "sap");
                                replyresult = "Q";
                                luisIntent = "SAP";
                                luisTypeEntities = "SAP";
                            }
                            else
                            {
                                DButil.HistoryLog("SAP 답변없을때");
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

                                db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "loop");
                                db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "sap");

                                SetActivity(sorryReply);
                                replyresult = "Z";

                            }

                            DateTime endTime = DateTime.Now;

                            //analysis table insert
                            int dbResult = db.insertUserQuery(relationList, luisId, luisIntent, luisEntities, luisIntentScore, replyresult, orgMent);

                            //history table insert
                            //db.insertHistory(activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), "", "", "", "", replyresult);
                            db.insertHistory(null, activity.Conversation.Id, activity.ChannelId, ((endTime - MessagesController.startTime).Milliseconds), luisIntent, luisEntities, luisIntentScore, dlgId, replyresult, orgMent);
                            replyresult = "";
                            luisIntent = "";
                            luisTypeEntities = "";
                        }
                        //SAP 이외 처리
                        else
                        {
                            CacheList cacheList = new CacheList();
                            //정규화
                            luisQuery = orgMent;
                            orgMent = Regex.Replace(orgMent, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline);
                            orgMent = orgMent.Replace(" ", "").ToLower();
                            queryStr = orgMent;
                            cacheList = db.CacheChk(orgMent.Replace(" ", ""));                     // 캐시 체크 (TBL_QUERY_ANALYSIS_RESULT 조회..)
                            //cacheList.luisIntent 초기화
                            //cacheList.luisIntent = null;

                            //userData 예외처리
                            DButil.HistoryLog("userData.Count() : " + userData.Count());
                            if (userData.Count() == 0)
                            {
                                DButil.HistoryLog("userData.Count()일때 다시 입력 START");
                                userData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);
                                DButil.HistoryLog("userData.Count()일때 다시 입력 END");
                            }

                            //SAP 비밀번호 
                            DButil.HistoryLog("SAP 비밀번호 체크");
                            if (orgMent.Equals("sap비밀번호초기화신청접수"))
                            {
                                if (userData[0].conversationsId == activity.Conversation.Id)
                                {
                                    db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 1, "sap");
                                    userData[0].sap = 1;
                                }
                            }

                            //건의사항
                            DButil.HistoryLog("건의사항 체크");
                            if ((orgMent.Contains("건의사항") || orgMent.Contains("건의 사항")) && userData[0].loop != 2)
                            {
                                if (userData[0].conversationsId == activity.Conversation.Id)
                                {
                                    db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 1, "loop");
                                    userData[0].loop = 1;
                                }
                            }

                            //smalltalk 문자 확인  
                            DButil.HistoryLog("smalltalk 체크");
                            String smallTalkSentenceConfirm = db.SmallTalkSentenceConfirm(orgMent);
                            if ((userData[0].sap == 1 || userData[0].sap == 2 || userData[0].sap == 3 || userData[0].sap == 4) || (userData[0].loop == 1 || userData[0].loop == 2))
                            {
                                smallTalkSentenceConfirm = "";
                            }                 
                            
                            //smalltalk 답변이 있을경우
                            if (!string.IsNullOrEmpty(smallTalkSentenceConfirm))
                            {
                                DButil.HistoryLog("smalltalk 답변이 있을경우");
                                luisId = "";
                            }
                            else if (userData[0].sap == 1 || userData[0].sap == 2 || userData[0].sap == 3 || userData[0].sap == 4)
                            {
                                DButil.HistoryLog("SAP 비밀번호 초기화가 있을경우");
                                luisId = "";
                            }
                            else if (userData[0].loop == 1 || userData[0].loop == 2)
                            {
                                DButil.HistoryLog("건의사항이 있을경우");
                                luisId = "";
                            }
                            //luis 호출
                            else if (cacheList.luisIntent == null || cacheList.luisEntities == null)
                            {
                                DButil.HistoryLog("cache none : " + orgMent);
                                Debug.WriteLine("cache none : " + orgMent);

                                List<string[]> textList = new List<string[]>(5);

                                for (int i = 0; i < 5; i++)
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
                                Task<JObject> t1 = Task<JObject>.Run(async () => await GetIntentFromBotLUIS(textList, luisQuery));

                                //결과값 받기
                                await Task.Delay(1000);
                                t1.Wait();
                                Luis = t1.Result;

                                //Debug.WriteLine("Luis : " + Luis); 
                                //entities 갯수가 0일겨우 intent를 None으로 처리

                                //if (Luis != null || Luis.Count > 0)
                                if (Luis.Count != 0)
                                {
                                    if ((int)Luis["entities"].Count() != 0)
                                    {
                                        float luisScore = (float)Luis["intents"][0]["score"];
                                        int luisEntityCount = (int)Luis["entities"].Count();

                                        luisIntent = Luis["topScoringIntent"]["intent"].ToString();//add
                                        luisScore = luisScoreCompare;
                                        Debug.WriteLine("GetMultiLUIS() LUIS luisIntent : " + luisIntent);

                                        //통근버스
                                        if (luisIntent.Equals("총무통근버스_통근버스노선안내"))
                                        {
                                            for (int i = 0; i < (int)Luis["entities"].Count(); i++)
                                            {
                                                if ((string)Luis["entities"][i]["type"] == "L>통근버스노선")
                                                {
                                                    luisTypeEntities = Regex.Replace((string)Luis["entities"][i]["entity"], " ", "");
                                                }
                                            }
                                        }
                                        Debug.WriteLine("통근버스노선" + luisTypeEntities);
                                    }
                                    else
                                    {
                                        luisIntent = "None";
                                    }
                                    
                                }else
                                {
                                    luisIntent = "None";
                                }

                                Debug.WriteLine("cacheList.luisIntent : " + cacheList.luisIntent);
                                cacheList = db.CacheDataFromIntent(luisIntent);

                                luisId = cacheList.luisId;
                                luisIntent = cacheList.luisIntent;
                                luisEntities = cacheList.luisEntities;
                                luisIntentScore = cacheList.luisScore;

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

                            string smallTalkConfirm = "";

                            if (!string.IsNullOrEmpty(luisIntent))
                            {
                                relationList = db.DefineTypeChkSpare(cacheList.luisIntent, cacheList.luisEntities);
                            }
                            else
                            {
                                relationList = null;
                                //smalltalk 답변가져오기
                                
                                if (orgMent.Length < 11)
                                {
                                    if((userData[0].sap == 1 || userData[0].sap == 2 || userData[0].sap == 3 || userData[0].sap == 4) || (userData[0].loop == 1 || userData[0].loop == 2))
                                    {
                                        smallTalkConfirm = "";
                                    }
                                    else
                                    {
                                        smallTalkConfirm = db.SmallTalkConfirm(orgMent);
                                    }
                                    
                                }
                                else
                                {
                                    smallTalkConfirm = "";
                                }

                            }

                            //if (apiFlag.Equals("COMMON") && relationList != null)
                            //if (relationList != null && string.IsNullOrEmpty(userData.GetProperty<string>("suggestion")))
                            if (relationList != null)
                            {
                                dlgId = "";
                                //List<UserData> userData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);

                                for (int m = 0; m < relationList.Count; m++)
                                {
                                    DialogList dlg = db.SelectDialog(relationList[m].dlgId, userData[0].mobileYN);
                                    dlgId += Convert.ToString(dlg.dlgId) + ",";
                                    Activity commonReply = activity.CreateReply();
                                    Attachment tempAttachment = new Attachment();
                                    DButil.HistoryLog("dlg.dlgType : " + dlg.dlgType);

                                    string userSSO = "NONE";
                                    List<UserData> uData = new List<UserData>();
                                    uData = db.UserDataConfirm(activity.ChannelId, activity.Conversation.Id);

                                    if (uData[0].sso != null)
                                    {
                                        userSSO = uData[0].sso;
                                    }

                                    if (dlg.dlgType.Equals(CARDDLG))
                                    {
                                        foreach (CardList tempcard in dlg.dialogCard)
                                        {
                                            //주차신청 get방식 userid 추가
                                            if (!string.IsNullOrEmpty(tempcard.btn1Context) && tempcard.btn1Context.Contains("http://116.121.31.148/visitor2/menu1.asp?emailAlias="))
                                            {
                                                //DButil.HistoryLog("btn1Context ==1" + tempcard.btn1Context);
                                                tempcard.btn1Context += uData[0].userId;
                                                //DButil.HistoryLog("btn1Context ==2" + tempcard.btn1Context);
                                            }

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
                            //else if (!string.IsNullOrEmpty(smallTalkConfirm) && string.IsNullOrEmpty(userData.GetProperty<string>("suggestion")))
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
                            //건의사항
                            else if ((userData[0].conversationsId == activity.Conversation.Id && (userData[0].loop == 1 || userData[0].loop == 2)))
                            {
                                Debug.WriteLine("suggestions dialogue-------------");

                                Activity suggestionsReply = activity.CreateReply();
                                suggestionsReply.Recipient = activity.From;
                                suggestionsReply.Type = "message";
                                suggestionsReply.Attachments = new List<Attachment>();

                                List<TextList> text = new List<TextList>();

                                if (userData[0].loop == 1)
                                {
                                    text = db.SelectSuggetionsDialogText("6");
                                    db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 2, "loop");
                                    replyresult = "G";
                                }
                                else
                                {
                                    text = db.SelectSuggetionsDialogText("7");
                                    db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "loop");
                                    replyresult = "G";
                                }

                                for (int i = 0; i < text.Count; i++)
                                {
                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Title = text[i].cardTitle,
                                        Text = text[i].cardText
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    suggestionsReply.Attachments.Add(plAttachment);
                                }

                                SetActivity(suggestionsReply);
                            }

                            //sap 비밀번호 초기화
                            else if ((userData[0].conversationsId == activity.Conversation.Id && (userData[0].sap == 1 || userData[0].sap == 2 || userData[0].sap == 3 || userData[0].sap == 4)))
                            {
                                Debug.WriteLine("sapInit dialogue-------------");

                                luisIntent = "SAPINIT";
                                luisTypeEntities = "SAPINIT";

                                Activity sapInitReply = activity.CreateReply();
                                sapInitReply.Recipient = activity.From;
                                sapInitReply.Type = "message";
                                sapInitReply.Attachments = new List<Attachment>();

                                List<TextList> text = new List<TextList>();
                                List<CardAction> cardButtons = new List<CardAction>();

                                if (userData[0].sap == 1)
                                {
                                    CardAction plButton1 = new CardAction();
                                    plButton1 = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "전사 ERP(PRD)",
                                        Title = "전사 ERP(PRD)"
                                    };
                                    CardAction plButton2 = new CardAction();
                                    plButton2 = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "전사 BI(BIP)",
                                        Title = "전사 BI(BIP)"
                                    };
                                    CardAction plButton3 = new CardAction();
                                    plButton3 = new CardAction()
                                    {
                                        Type = "imBack",
                                        Value = "해외 BI(BW1)",
                                        Title = "해외 BI(BW1)"
                                    };
                                    cardButtons.Add(plButton1);
                                    cardButtons.Add(plButton2);
                                    cardButtons.Add(plButton3);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Text = "선택",
                                        Buttons = cardButtons
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    sapInitReply.Attachments.Add(plAttachment);
                                    db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 2, "sap");
                                }
                                else if (userData[0].sap == 2)
                                {
                                    string optional_1 = "";

                                    switch (luisQuery)
                                    {
                                        case "전사 ERP(PRD)":
                                            optional_1 = "CJ_SAP";
                                            break;
                                        case "전사 BI(BIP)":
                                            optional_1 = "CJ_BI";
                                            break;
                                        default:
                                            optional_1 = "CJG_BI";
                                            break;
                                    }
                                     
                                    db.UserDataUpdateUserID(activity.ChannelId, activity.Conversation.Id, "OPTIONAL_1", optional_1);

                                    UserHeroCard plCard = new UserHeroCard()
                                    {
                                        Text = "초기화 안내 /n [" + userData[0].userId + "] 님 SAP 비밀번호 초기화를 위해 계정의 사원번호가 필요합니다. 사원번호 를 입력해주세요./n/n취소를 원하시면 '취소'라고 입력해주세요."
                                    };

                                    Attachment plAttachment = plCard.ToAttachment();
                                    sapInitReply.Attachments.Add(plAttachment);
                                    db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 3, "sap");
                                }
                                else if (userData[0].sap == 3)
                                {
                                    //sap 비밀번호 초기화 탈출
                                    if (orgMent.Contains("취소"))
                                    {
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Text = "SAP 비밀번호 초기화가 취소되었습니다. 다른 문의 사항을 말씀해주세요."
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        sapInitReply.Attachments.Add(plAttachment);
                                        
                                        db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "sap");
                                        userData[0].sap = 0;
                                    }
                                    else
                                    {
                                        int val;
                                        Debug.WriteLine(int.TryParse(orgMent, out val));
                                        if (!int.TryParse(orgMent, out val) || orgMent.Length != 6)
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Text = "정확한 사번을 입력해주세요."
                                            };
                                            Attachment plAttachment = plCard.ToAttachment();
                                            sapInitReply.Attachments.Add(plAttachment);
                                            db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 3, "sap");
                                        }
                                        else
                                        {
                                            String sabun = "";
                                            sabun = orgMent;
                                            db.UserDataUpdateUserID(activity.ChannelId, activity.Conversation.Id, "sabun", sabun);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Text = "재발급사유 를 입력해주세요. (5자 이상)/n/n취소를 원하시면 '취소'라고 입력해주세요."
                                            };
                                            Attachment plAttachment = plCard.ToAttachment();
                                            sapInitReply.Attachments.Add(plAttachment);
                                            db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 4, "sap");
                                        }
                                    }
                                    
                                }
                                else if (userData[0].sap == 4)
                                {
                                    //sap 비밀번호 초기화 탈출
                                    if (orgMent.Contains("취소"))
                                    {
                                        UserHeroCard plCard = new UserHeroCard()
                                        {
                                            Text = "SAP 비밀번호 초기화가 취소되었습니다. 다른 문의 사항을 말씀해주세요."
                                        };
                                        Attachment plAttachment = plCard.ToAttachment();
                                        sapInitReply.Attachments.Add(plAttachment);

                                        db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "sap");
                                        userData[0].sap = 0;
                                    }
                                    else
                                    {
                                        if (orgMent.Length < 6)
                                        {
                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Text = "재발급사유 를 입력해주세요. (5자 이상)."
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            sapInitReply.Attachments.Add(plAttachment);

                                            db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 4, "sap");
                                        }
                                        else
                                        {
                                            String reissue = "";
                                            reissue = orgMent;
                                            db.UserDataUpdateUserID(activity.ChannelId, activity.Conversation.Id, "reissue", reissue);

                                            UserHeroCard plCard = new UserHeroCard()
                                            {
                                                Text = "SAP 비밀번호 초기화를 진행중입니다."
                                            };

                                            Attachment plAttachment = plCard.ToAttachment();
                                            sapInitReply.Attachments.Add(plAttachment);

                                            //SAP 초기화 리스트
                                            string urlParameter = "";
                                            List<UserData> uData = new List<UserData>();
                                            uData = db.UserDataSapConfirm(activity.ChannelId, activity.Conversation.Id);
                                            urlParameter = "&userid=" + uData[0].userId + "&sabun=" + uData[0].sabun + "&reissue=" + uData[0].reissue + "&optional_1=" + uData[0].optional_1;

                                            //SAP 초기화 작업
                                            string sapInit = dbutil.GetSapInit(urlParameter);

                                            if (!string.IsNullOrEmpty(sapInit))
                                            {
                                                UserHeroCard plCard1 = new UserHeroCard()
                                                {
                                                    //Text = "[" + userID + "]님의 SAP 비밀번호가 초기화 되었습니다. 초기화된 임시패스워드가 메일로 발송되었습니다. (5분이내수신)"
                                                    Text = dbutil.StripHtml(sapInit)
                                                };
                                                Attachment plAttachment1 = plCard1.ToAttachment();
                                                sapInitReply.Attachments.Add(plAttachment1);
                                                db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "sap");
                                            }
                                            else
                                            {
                                                UserHeroCard plCard1 = new UserHeroCard()
                                                {
                                                    Text = "[" + userData[0].userId + "] 님의 SAP 비밀번호가 초기화가 실패되었습니다. 사원번호 및 사유를 재확인 부탁드립니다."
                                                };
                                                Attachment plAttachment1 = plCard1.ToAttachment();
                                                sapInitReply.Attachments.Add(plAttachment1);
                                                db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "sap");
                                            }
                                        }
                                    }
                                    
                                } 
                                replyresult = "I";
                                SetActivity(sapInitReply);

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

                                db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "loop");
                                db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "sap");

                                SetActivity(sorryReply);
                                replyresult = "D";

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
                }
                catch (Exception e)
                {
                    Debug.Print(e.StackTrace);
                    DButil.HistoryLog("ERROR==="+e.Message);

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

                    db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "loop");
                    db.UserDataUpdate(activity.ChannelId, activity.Conversation.Id, 0, "sap");
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

            JObject[] Luis_before = new JObject[5];
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
            for (int i = 0; i < 5; i++)
            {
                //entities 0일 경우 PASS
                if ((int)Luis_before[i]["entities"].Count() > 0)
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