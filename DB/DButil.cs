using HanjinChatBot.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using Newtonsoft.Json;

namespace HanjinChatBot.DB
{
    public class DButil
    {
        //DbConnect db = new DbConnect();
        //재시도 횟수 설정
        //private static int retryCount = 3;

        public static void HistoryLog(String strMsg)
        {
            try
            {
                //Debug.WriteLine("AppDomain.CurrentDomain.BaseDirectory : " + AppDomain.CurrentDomain.BaseDirectory);
                string m_strLogPrefix = AppDomain.CurrentDomain.BaseDirectory + @"LOG\";
                string m_strLogExt = @".LOG";
                DateTime dtNow = DateTime.Now;
                string strDate = dtNow.ToString("yyyy-MM-dd");
                string strPath = String.Format("{0}{1}{2}", m_strLogPrefix, strDate, m_strLogExt);
                string strDir = Path.GetDirectoryName(strPath);
                DirectoryInfo diDir = new DirectoryInfo(strDir);

                if (!diDir.Exists)
                {
                    diDir.Create();
                    diDir = new DirectoryInfo(strDir);
                }

                if (diDir.Exists)
                {
                    System.IO.StreamWriter swStream = File.AppendText(strPath);
                    string strLog = String.Format("{0}: {1}", dtNow.ToString("MM/dd/yyyy hh:mm:ss.fff"), strMsg);
                    swStream.WriteLine(strLog);
                    swStream.Close(); ;
                }
            }
            catch (System.Exception e)
            {
                HistoryLog(e.Message);
            }
        }
        
        public Attachment getAttachmentFromDialog(DialogList dlg, Activity activity)
        {
            Attachment returnAttachment = new Attachment();
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            if (dlg.dlgType.Equals(MessagesController.TEXTDLG))
            {

                if (!activity.ChannelId.Equals("facebook"))
                {
                    UserHeroCard plCard = new UserHeroCard()
                    {
                        Title = dlg.cardTitle,
                        Text = dlg.cardText,
                        Gesture = dlg.gesture
                    };
                    returnAttachment = plCard.ToAttachment();
                }

                
            }
            else if (dlg.dlgType.Equals(MessagesController.MEDIADLG))
            {

                string cardDiv = "";
                string cardVal = "";

                List<CardImage> cardImages = new List<CardImage>();
                List<CardAction> cardButtons = new List<CardAction>();

                HistoryLog("CARD IMG START");
                if (dlg.mediaUrl != null)
                {
                    HistoryLog("FB CARD IMG " + dlg.mediaUrl);
                    cardImages.Add(new CardImage(url: dlg.mediaUrl));
                }


                HistoryLog("CARD BTN1 START");
                if (activity.ChannelId.Equals("facebook") && dlg.btn1Type == null && !string.IsNullOrEmpty(dlg.cardDivision) && dlg.cardDivision.Equals("play") && !string.IsNullOrEmpty(dlg.cardValue))
                {
                    CardAction plButton = new CardAction();
                    plButton = new CardAction()
                    {
                        Value = dlg.cardValue,
                        Type = "openUrl",
                        Title = "영상보기"
                    };
                    cardButtons.Add(plButton);
                }
                else if (dlg.btn1Type != null)
                {
                    CardAction plButton = new CardAction();
                    plButton = new CardAction()
                    {
                        Value = dlg.btn1Context,
                        Type = dlg.btn1Type,
                        Title = dlg.btn1Title
                    };
                    cardButtons.Add(plButton);
                }

                if (dlg.btn2Type != null)
                {
                    if (!(activity.ChannelId == "facebook" && dlg.btn2Title == "나에게 맞는 모델 추천"))
                    {
                        CardAction plButton = new CardAction();
                        HistoryLog("CARD BTN2 START");
                        plButton = new CardAction()
                        {
                            Value = dlg.btn2Context,
                            Type = dlg.btn2Type,
                            Title = dlg.btn2Title
                        };
                        cardButtons.Add(plButton);
                    }
                }

                if (dlg.btn3Type != null )
                {
                    
                    CardAction plButton = new CardAction();

                    HistoryLog("CARD BTN3 START");
                    plButton = new CardAction()
                    {
                        Value = dlg.btn3Context,
                        Type = dlg.btn3Type,
                        Title = dlg.btn3Title
                    };
                    cardButtons.Add(plButton);
                    
                }

                if (dlg.btn4Type != null)
                {
                    CardAction plButton = new CardAction();
                    HistoryLog("CARD BTN4 START");
                    plButton = new CardAction()
                    {
                        Value = dlg.btn4Context,
                        Type = dlg.btn4Type,
                        Title = dlg.btn4Title
                    };
                    cardButtons.Add(plButton);
                }

                if (!string.IsNullOrEmpty(dlg.cardDivision))
                {
                    cardDiv = dlg.cardDivision;
                }

                if (!string.IsNullOrEmpty(dlg.cardValue))
                {
                    //cardVal = priceMediaDlgList[i].cardValue.Replace();
                    cardVal = dlg.cardValue;
                }
                //HistoryLog("!!!!!FB CARD BTN1 START channelID.Equals(facebook) && cardButtons.Count < 1 && cardImages.Count < 1");
                HeroCard plCard = new UserHeroCard();
                if (activity.ChannelId == "facebook" && string.IsNullOrEmpty(dlg.cardTitle))
                {
                    //HistoryLog("FB CARD BTN1 START channelID.Equals(facebook) && string.IsNullOrEmpty(card.cardTitle)");
                    plCard = new UserHeroCard()
                    {
                        Title = "선택해 주세요",
                        Text = dlg.cardText,
                        Images = cardImages,
                        Buttons = cardButtons,
                        Card_division = cardDiv,
                        Card_value = cardVal
                    };
                    returnAttachment = plCard.ToAttachment();
                }
                else if (activity.ChannelId == "facebook" && string.IsNullOrEmpty(dlg.cardValue))
                {
                    //HistoryLog("FB CARD BTN1 START channelID.Equals(facebook) && string.IsNullOrEmpty(card.cardValue)");
                    plCard = new UserHeroCard()
                    {
                        Title = dlg.cardTitle,
                        Images = cardImages,
                        Buttons = cardButtons
                    };
                    returnAttachment = plCard.ToAttachment();
                }
                else
                {
                    //HistoryLog("!!!!!!!!FB CARD BTN1 START channelID.Equals(facebook) && string.IsNullOrEmpty(card.cardTitle)");
                    plCard = new UserHeroCard()
                    {
                        Title = dlg.cardTitle,
                        Text = dlg.cardText,
                        Images = cardImages,
                        Buttons = cardButtons,
                        Card_division = cardDiv,
                        Card_value = cardVal
                    };
                    returnAttachment = plCard.ToAttachment();
                }
            }
            else
            {
                Debug.WriteLine("Dialog Type Error : " + dlg.dlgType);
            }
            return returnAttachment;
        }


        public Attachment getAttachmentFromDialog(CardList card, Activity activity, string userSSO)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            Attachment returnAttachment = new Attachment();

            string cardDiv = "";
            string cardVal = "";

            List<CardImage> cardImages = new List<CardImage>();
            List<CardAction> cardButtons = new List<CardAction>();
            //HistoryLog("CARD IMG START");
            if (card.imgUrl != null)
            {
                HistoryLog("FB CARD IMG " + card.imgUrl);
                cardImages.Add(new CardImage(url: card.imgUrl));
            }


            //HistoryLog("CARD BTN1 START");
            /*
            if (!userSSO.Equals("INIT"))
            {
                card = chkOpenUrlDlg(card, userSSO);
            }
            */
            if (activity.ChannelId.Equals("facebook") && card.btn1Type == null && !string.IsNullOrEmpty(card.cardDivision) && card.cardDivision.Equals("play") && !string.IsNullOrEmpty(card.cardValue))
            {
                CardAction plButton = new CardAction();
                plButton = new CardAction()
                {
                    Value = card.cardValue,
                    Type = "openUrl",
                    Title = "영상보기"
                };
                cardButtons.Add(plButton);
            }
            else if (card.btn1Type != null)
            {
                CardAction plButton = new CardAction();
                plButton = new CardAction()
                {
                    Value = card.btn1Context,
                    Type = card.btn1Type,
                    Title = card.btn1Title
                };
                cardButtons.Add(plButton);
            }

            if (card.btn2Type != null)
            {
                CardAction plButton = new CardAction();
                //HistoryLog("CARD BTN2 START");
                plButton = new CardAction()
                {
                    Value = card.btn2Context,
                    Type = card.btn2Type,
                    Title = card.btn2Title
                };
                cardButtons.Add(plButton);
            }

            if (card.btn3Type != null)
            {
                CardAction plButton = new CardAction();

                //HistoryLog("CARD BTN3 START");
                plButton = new CardAction()
                {
                    Value = card.btn3Context,
                    Type = card.btn3Type,
                    Title = card.btn3Title
                };
                cardButtons.Add(plButton);
            }

            if (card.btn4Type != null)
            {
                CardAction plButton = new CardAction();
                //HistoryLog("CARD BTN4 START");
                plButton = new CardAction()
                {
                    Value = card.btn4Context,
                    Type = card.btn4Type,
                    Title = card.btn4Title
                };
                cardButtons.Add(plButton);
            }



            if (!string.IsNullOrEmpty(card.cardDivision))
            {
                cardDiv = card.cardDivision;
            }

            if (!string.IsNullOrEmpty(card.cardValue))
            {
                //cardVal = priceMediaDlgList[i].cardValue.Replace();
                cardVal = card.cardValue;
            }


            if(activity.ChannelId.Equals("facebook") && cardButtons.Count < 1 && cardImages.Count < 1)
            {
                //HistoryLog("FB CARD BTN1 START channelID.Equals(facebook) && cardButtons.Count < 1 && cardImages.Count < 1");
                Activity reply_facebook = activity.CreateReply();
                reply_facebook.Recipient = activity.From;
                reply_facebook.Type = "message";
                //HistoryLog("facebook  card Text : " + card.cardText);
                reply_facebook.Text = card.cardText;
                var reply_ment_facebook = connector.Conversations.SendToConversationAsync(reply_facebook);
            }
            else
            {
                //HistoryLog("!!!!!FB CARD BTN1 START channelID.Equals(facebook) && cardButtons.Count < 1 && cardImages.Count < 1");
                HeroCard plCard = new UserHeroCard();
                if (activity.ChannelId == "facebook" && string.IsNullOrEmpty(card.cardValue))
                {
                    //HistoryLog("FB CARD BTN1 START channelID.Equals(facebook) && string.IsNullOrEmpty(card.cardValue)");
                    plCard = new UserHeroCard()
                    {
                        Title = card.cardTitle,
                        Images = cardImages,
                        Buttons = cardButtons,
                        Gesture = card.gesture //2018-04-24 : 제스처 추가
                    };
                    returnAttachment = plCard.ToAttachment();
                }
                else
                {
                    //HistoryLog("!!!!!!!FB CARD BTN1 START channelID.Equals(facebook) && string.IsNullOrEmpty(card.cardValue)");
                    if (activity.ChannelId == "facebook" && string.IsNullOrEmpty(card.cardTitle))
                    {
                        //HistoryLog("FB CARD BTN1 START channelID.Equals(facebook) && string.IsNullOrEmpty(card.cardTitle)");
                        plCard = new UserHeroCard()
                        {
                            Title = "선택해 주세요",
                            Text = card.cardText,
                            Images = cardImages,
                            Buttons = cardButtons,
                            Card_division = cardDiv,
                            Card_value = cardVal,
                            Gesture = card.gesture //2018-04-24 : 제스처 추가
                        };
                        returnAttachment = plCard.ToAttachment();
                    }
                    else
                    {
                        //HistoryLog("!!!!!!!!FB CARD BTN1 START channelID.Equals(facebook) && string.IsNullOrEmpty(card.cardTitle)");
                        plCard = new UserHeroCard()
                        {
                            Title = card.cardTitle,
                            Text = card.cardText,
                            Images = cardImages,
                            Buttons = cardButtons,
                            Card_division = cardDiv,
                            Card_value = cardVal,
                            Gesture = card.gesture //2018-04-24 : 제스처 추가
                        };
                        returnAttachment = plCard.ToAttachment();
                    }
                    
                }
            }

            return returnAttachment;
        }


        public static Attachment GetHeroCard(string title, string subtitle, string text, CardImage cardImage, /*CardAction cardAction*/ List<CardAction> buttons)
        {
            var heroCard = new UserHeroCard
            {
                Title = title,
                Subtitle = subtitle,
                Text = text,
                Images = new List<CardImage>() { cardImage },
                Buttons = buttons,
            };

            return heroCard.ToAttachment();
        }
        public Attachment GetHeroCard(string title, string subtitle, string text, CardImage cardImage, /*CardAction cardAction*/ List<CardAction> buttons, string cardDivision, string cardValue)
        {
            var heroCard = new UserHeroCard
            {
                Title = title,
                Subtitle = subtitle,
                Text = text,
                Images = new List<CardImage>() { cardImage },
                Buttons = buttons,
                Card_division = cardDivision,
                Card_value = cardValue,

            };

            return heroCard.ToAttachment();
        }

        
        //태그 제거
        public string StripHtml(string Txt)
        {
            return Regex.Replace(Txt, "<(.|\\n)*?>", string.Empty);
        }
    }
}