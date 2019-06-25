using HanjinChatBot.Dialogs;
using HanjinChatBot.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Configuration;

namespace HanjinChatBot.DB
{
    public class DbConnect
    {
        static Configuration rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/");
        const string CONSTRINGNAME = "conString";
        string connStr = rootWebConfig.ConnectionStrings.ConnectionStrings[CONSTRINGNAME].ToString();

        public readonly string TEXTDLG = "2";
        public readonly string CARDDLG = "3";
        public readonly string MEDIADLG = "4";

        public void ConnectDb()
        {
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(connStr);
                conn.Open();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }

        }


        public List<DialogList> SelectInitDialog(String channel)
        {
            SqlDataReader rdr = null;
            List<DialogList> dialogs = new List<DialogList>();
            SqlCommand cmd = new SqlCommand();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT   				    ";
                cmd.CommandText += " 	DLG_ID,                 ";
                cmd.CommandText += " 	DLG_TYPE,               ";
                cmd.CommandText += " 	DLG_GROUP,              ";
                cmd.CommandText += " 	DLG_ORDER_NO            ";
                cmd.CommandText += " FROM TBL_DLG     ";
                cmd.CommandText += " WHERE DLG_GROUP = '1'      ";
                cmd.CommandText += " AND USE_YN = 'Y'           ";
                cmd.CommandText += " ORDER BY DLG_ORDER_NO            ";

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                DButil.HistoryLog(" db SelectInitDialog !! ");


                while (rdr.Read())
                {
                    DialogList dlg = new DialogList();
                    dlg.dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    dlg.dlgType = rdr["DLG_TYPE"] as string;
                    dlg.dlgGroup = rdr["DLG_GROUP"] as string;
                    dlg.dlgOrderNo = rdr["DLG_ORDER_NO"] as string;

                    using (SqlConnection conn2 = new SqlConnection(connStr))
                    {
                        SqlCommand cmd2 = new SqlCommand();
                        conn2.Open();
                        cmd2.Connection = conn2;
                        SqlDataReader rdr2 = null;
                        if (dlg.dlgType.Equals(TEXTDLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_TEXT FROM TBL_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y'";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);

                            while (rdr2.Read())
                            {
                                dlg.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlg.cardText = rdr2["CARD_TEXT"] as string;
                            }
                            rdr2.Close();
                        }
                        else if (dlg.dlgType.Equals(CARDDLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_SUBTITLE, CARD_TEXT, IMG_URL," +
                                    "BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT, BTN_2_TYPE, BTN_2_TITLE, BTN_2_CONTEXT, BTN_3_TYPE, BTN_3_TITLE, BTN_3_CONTEXT, BTN_4_TYPE, BTN_4_TITLE, BTN_4_CONTEXT, " +
                                    "CARD_DIVISION, CARD_VALUE, CARD_ORDER_NO " +
                                    "FROM TBL_DLG_CARD WHERE DLG_ID = @dlgID AND USE_YN = 'Y' ";

                            cmd2.CommandText += "ORDER BY CARD_ORDER_NO";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);
                            List<CardList> dialogCards = new List<CardList>();
                            while (rdr2.Read())
                            {
                                CardList dlgCard = new CardList();
                                dlgCard.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlgCard.cardSubTitle = rdr2["CARD_SUBTITLE"] as string;
                                dlgCard.cardText = rdr2["CARD_TEXT"] as string;
                                dlgCard.imgUrl = rdr2["IMG_URL"] as string;
                                dlgCard.btn1Type = rdr2["BTN_1_TYPE"] as string;
                                dlgCard.btn1Title = rdr2["BTN_1_TITLE"] as string;
                                dlgCard.btn1Context = rdr2["BTN_1_CONTEXT"] as string;
                                dlgCard.btn2Type = rdr2["BTN_2_TYPE"] as string;
                                dlgCard.btn2Title = rdr2["BTN_2_TITLE"] as string;
                                dlgCard.btn2Context = rdr2["BTN_2_CONTEXT"] as string;
                                dlgCard.btn3Type = rdr2["BTN_3_TYPE"] as string;
                                dlgCard.btn3Title = rdr2["BTN_3_TITLE"] as string;
                                dlgCard.btn3Context = rdr2["BTN_3_CONTEXT"] as string;
                                dlgCard.btn4Type = rdr2["BTN_4_TYPE"] as string;
                                dlgCard.btn4Title = rdr2["BTN_4_TITLE"] as string;
                                dlgCard.btn4Context = rdr2["BTN_4_CONTEXT"] as string;
                                dlgCard.cardDivision = rdr2["CARD_DIVISION"] as string;
                                dlgCard.cardValue = rdr2["CARD_VALUE"] as string;
                                dlgCard.card_order_no = Convert.ToInt32(rdr2["CARD_ORDER_NO"]);
                                dialogCards.Add(dlgCard);
                            }
                            dlg.dialogCard = dialogCards;
                        }
                        else if (dlg.dlgType.Equals(MEDIADLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_TEXT, MEDIA_URL," +
                                    "BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT, BTN_2_TYPE, BTN_2_TITLE, BTN_2_CONTEXT, BTN_3_TYPE, BTN_3_TITLE, BTN_3_CONTEXT, BTN_4_TYPE, BTN_4_TITLE, BTN_4_CONTEXT " +
                                    "FROM TBL_DLG_MEDIA WHERE DLG_ID = @dlgID AND USE_YN = 'Y'";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);

                            while (rdr2.Read())
                            {
                                dlg.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlg.cardText = rdr2["CARD_TEXT"] as string;
                                dlg.mediaUrl = rdr2["MEDIA_URL"] as string;
                                dlg.btn1Type = rdr2["BTN_1_TYPE"] as string;
                                dlg.btn1Title = rdr2["BTN_1_TITLE"] as string;
                                dlg.btn1Context = rdr2["BTN_1_CONTEXT"] as string;
                                dlg.btn2Type = rdr2["BTN_2_TYPE"] as string;
                                dlg.btn2Title = rdr2["BTN_2_TITLE"] as string;
                                dlg.btn2Context = rdr2["BTN_2_CONTEXT"] as string;
                                dlg.btn3Type = rdr2["BTN_3_TYPE"] as string;
                                dlg.btn3Title = rdr2["BTN_3_TITLE"] as string;
                                dlg.btn3Context = rdr2["BTN_3_CONTEXT"] as string;
                                dlg.btn4Type = rdr2["BTN_4_TYPE"] as string;
                                dlg.btn4Title = rdr2["BTN_4_TITLE"] as string;
                                dlg.btn4Context = rdr2["BTN_4_CONTEXT"] as string;
                            }
                        }

                    }
                    dialogs.Add(dlg);
                }
                rdr.Close();
            }
            return dialogs;
        }

        public DialogList SelectDialog(int dlgID, string mobilePC)
        {
            SqlDataReader rdr = null;
            DialogList dlg = new DialogList();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += " SELECT   				                    ";
                cmd.CommandText += " 	A.DLG_ID,                               ";
                cmd.CommandText += " 	A.DLG_NAME,                             ";
                cmd.CommandText += " 	A.DLG_DESCRIPTION,                      ";
                cmd.CommandText += " 	A.DLG_LANG,                             ";
                cmd.CommandText += " 	A.DLG_TYPE,                             ";
                cmd.CommandText += " 	A.DLG_ORDER_NO,                         ";
                cmd.CommandText += " 	A.DLG_GROUP,                            ";
                cmd.CommandText += " 	ISNULL(B.GESTURE,0) AS GESTURE          ";
                cmd.CommandText += " FROM TBL_DLG A, TBL_DLG_RELATION_LUIS B    ";
                cmd.CommandText += " WHERE A.DLG_ID = B.DLG_ID                  ";
                cmd.CommandText += "   AND A.DLG_ID = @dlgId                    ";
                cmd.CommandText += "   AND A.USE_YN = 'Y'                       ";
                cmd.CommandText += " ORDER BY  A.DLG_ORDER_NO                   ";

                cmd.Parameters.AddWithValue("@dlgID", dlgID);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    dlg.dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    dlg.dlgType = rdr["DLG_TYPE"] as string;
                    dlg.dlgGroup = rdr["DLG_GROUP"] as string;
                    dlg.dlgOrderNo = rdr["DLG_ORDER_NO"] as string;
                    //2018-04-25 : 제스처 추가
                    dlg.gesture = Convert.ToInt32(rdr["GESTURE"]);

                    using (SqlConnection conn2 = new SqlConnection(connStr))
                    {
                        SqlCommand cmd2 = new SqlCommand();
                        conn2.Open();
                        cmd2.Connection = conn2;
                        SqlDataReader rdr2 = null;
                        if (dlg.dlgType.Equals(TEXTDLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_TEXT FROM TBL_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y'";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);

                            while (rdr2.Read())
                            {
                                dlg.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlg.cardText = rdr2["CARD_TEXT"] as string;
                            }
                            rdr2.Close();
                        }
                        else if (dlg.dlgType.Equals(CARDDLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_SUBTITLE, CARD_TEXT, IMG_URL," +
                                    "BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT, BTN_1_CONTEXT_M, BTN_2_TYPE, BTN_2_TITLE, BTN_2_CONTEXT, BTN_2_CONTEXT_M, BTN_3_TYPE, BTN_3_TITLE, BTN_3_CONTEXT, BTN_3_CONTEXT_M, BTN_4_TYPE, BTN_4_TITLE, BTN_4_CONTEXT, BTN_4_CONTEXT_M, " +
                                    "CARD_DIVISION, CARD_VALUE, CARD_ORDER_NO " +
                                    "FROM TBL_DLG_CARD WHERE DLG_ID = @dlgID AND USE_YN = 'Y' ORDER BY CARD_ORDER_NO";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);
                            List<CardList> dialogCards = new List<CardList>();
                            while (rdr2.Read())
                            {
                                CardList dlgCard = new CardList();
                                dlgCard.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlgCard.cardSubTitle = rdr2["CARD_SUBTITLE"] as string;
                                dlgCard.cardText = rdr2["CARD_TEXT"] as string;
                                dlgCard.imgUrl = rdr2["IMG_URL"] as string;
                                dlgCard.btn1Type = rdr2["BTN_1_TYPE"] as string;
                                dlgCard.btn1Title = rdr2["BTN_1_TITLE"] as string;
                                dlgCard.btn1Context = rdr2["BTN_1_CONTEXT"] as string;
                                dlgCard.btn1ContextM = rdr2["BTN_1_CONTEXT_M"] as string;
                                //모바일 URL 적용 
                                if (mobilePC.Equals("MOBILE"))
                                {
                                    if (dlgCard.btn1Type.Equals("openUrl"))
                                    {
                                        if (!string.IsNullOrEmpty(dlgCard.btn1ContextM))
                                        {
                                            dlgCard.btn1Context = rdr2["BTN_1_CONTEXT_M"] as string;
                                        }
                                        else
                                        {
                                            dlgCard.btn1Context = "";
                                        }
                                    }
                                }
                                
                                dlgCard.btn2Type = rdr2["BTN_2_TYPE"] as string;
                                dlgCard.btn2Title = rdr2["BTN_2_TITLE"] as string;
                                dlgCard.btn2Context = rdr2["BTN_2_CONTEXT"] as string;
                                dlgCard.btn2ContextM = rdr2["BTN_2_CONTEXT_M"] as string;
                                //모바일 URL 적용 
                                if (mobilePC.Equals("MOBILE"))
                                {
                                    if (dlgCard.btn2Type.Equals("openUrl"))
                                    {
                                        if (!string.IsNullOrEmpty(dlgCard.btn2ContextM))
                                        {
                                            dlgCard.btn2Context = rdr2["BTN_2_CONTEXT_M"] as string;
                                        }
                                        else
                                        {
                                            dlgCard.btn2Context = "";
                                        }
                                    }
                                }
                                dlgCard.btn3Type = rdr2["BTN_3_TYPE"] as string;
                                dlgCard.btn3Title = rdr2["BTN_3_TITLE"] as string;
                                dlgCard.btn3Context = rdr2["BTN_3_CONTEXT"] as string;
                                dlgCard.btn3ContextM = rdr2["BTN_3_CONTEXT_M"] as string;
                                //모바일 URL 적용 
                                if (mobilePC.Equals("MOBILE"))
                                {
                                    if (dlgCard.btn3Type.Equals("openUrl"))
                                    {
                                        if (!string.IsNullOrEmpty(dlgCard.btn3ContextM))
                                        {
                                            dlgCard.btn3Context = rdr2["BTN_3_CONTEXT_M"] as string;
                                        }
                                        else
                                        {
                                            dlgCard.btn3Context = "";
                                        }
                                    }
                                }
                                dlgCard.btn4Type = rdr2["BTN_4_TYPE"] as string;
                                dlgCard.btn4Title = rdr2["BTN_4_TITLE"] as string;
                                dlgCard.btn4Context = rdr2["BTN_4_CONTEXT"] as string;
                                dlgCard.btn4ContextM = rdr2["BTN_4_CONTEXT_M"] as string;
                                //모바일 URL 적용 
                                if (mobilePC.Equals("MOBILE"))
                                {
                                    if (dlgCard.btn4Type.Equals("openUrl"))
                                    {
                                        if (!string.IsNullOrEmpty(dlgCard.btn4ContextM))
                                        {
                                            dlgCard.btn4Context = rdr2["BTN_4_CONTEXT_M"] as string;
                                        }
                                        else
                                        {
                                            dlgCard.btn4Context = "";
                                        }
                                    }
                                }
                                dlgCard.cardDivision = rdr2["CARD_DIVISION"] as string;
                                dlgCard.cardValue = rdr2["CARD_VALUE"] as string;
                                //dlgCard.card_order_no = rdr2["CARD_ORDER_NO"] as string;
                                dlgCard.card_order_no = Convert.ToInt32(rdr2["CARD_ORDER_NO"]);
                                //2018-04-25 : 제스처 추가
                                dlgCard.gesture = dlg.gesture;

                                dialogCards.Add(dlgCard);
                            }
                            dlg.dialogCard = dialogCards;
                        }
                        else if (dlg.dlgType.Equals(MEDIADLG))
                        {
                            cmd2.CommandText = "SELECT CARD_TITLE, CARD_TEXT, MEDIA_URL," +
                                    "BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT, BTN_2_TYPE, BTN_2_TITLE, BTN_2_CONTEXT, BTN_3_TYPE, BTN_3_TITLE, BTN_3_CONTEXT, BTN_4_TYPE, BTN_4_TITLE, BTN_4_CONTEXT , CARD_DIVISION, CARD_VALUE " +
                                    "FROM TBL_DLG_MEDIA WHERE DLG_ID = @dlgID AND USE_YN = 'Y'";
                            cmd2.Parameters.AddWithValue("@dlgID", dlg.dlgId);
                            rdr2 = cmd2.ExecuteReader(CommandBehavior.CloseConnection);

                            while (rdr2.Read())
                            {
                                dlg.cardTitle = rdr2["CARD_TITLE"] as string;
                                dlg.cardText = rdr2["CARD_TEXT"] as string;
                                dlg.mediaUrl = rdr2["MEDIA_URL"] as string;
                                dlg.btn1Type = rdr2["BTN_1_TYPE"] as string;
                                dlg.btn1Title = rdr2["BTN_1_TITLE"] as string;
                                dlg.btn1Context = rdr2["BTN_1_CONTEXT"] as string;
                                dlg.btn2Type = rdr2["BTN_2_TYPE"] as string;
                                dlg.btn2Title = rdr2["BTN_2_TITLE"] as string;
                                dlg.btn2Context = rdr2["BTN_2_CONTEXT"] as string;
                                dlg.btn3Type = rdr2["BTN_3_TYPE"] as string;
                                dlg.btn3Title = rdr2["BTN_3_TITLE"] as string;
                                dlg.btn3Context = rdr2["BTN_3_CONTEXT"] as string;
                                dlg.btn4Type = rdr2["BTN_4_TYPE"] as string;
                                dlg.btn4Title = rdr2["BTN_4_TITLE"] as string;
                                dlg.btn4Context = rdr2["BTN_4_CONTEXT"] as string;
                                dlg.cardDivision = rdr2["CARD_DIVISION"] as string;
                                dlg.cardValue = rdr2["CARD_VALUE"] as string;
                            }
                        }

                    }
                }
            }
            return dlg;
        }

        

        public List<TextList> SelectDialogText(int dlgID)
        {
            SqlDataReader rdr = null;
            List<TextList> dialogText = new List<TextList>();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT TEXT_DLG_ID, DLG_ID, CARD_TITLE, CARD_TEXT FROM TBL_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y' AND DLG_ID > 999";
                //cmd.CommandText = "SELECT TEXT_DLG_ID, DLG_ID, CARD_TITLE, CARD_TEXT FROM TBL_SECCS_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y' AND DLG_ID > 999";

                cmd.Parameters.AddWithValue("@dlgID", dlgID);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    int textDlgId = Convert.ToInt32(rdr["TEXT_DLG_ID"]);
                    int dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    string cardTitle = rdr["CARD_TITLE"] as string;
                    string cardText = rdr["CARD_TEXT"] as string;


                    TextList dlgText = new TextList();
                    dlgText.textDlgId = textDlgId;
                    dlgText.dlgId = dlgId;
                    dlgText.cardTitle = cardTitle;
                    dlgText.cardText = cardText;


                    dialogText.Add(dlgText);
                }
            }
            return dialogText;
        }

        public List<TextList> SelectSuggetionsDialogText(string dlgGroup)
        {
            SqlDataReader rdr = null;
            List<TextList> dialogText = new List<TextList>();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT TEXT_DLG_ID, DLG_ID, CARD_TITLE, CARD_TEXT FROM TBL_DLG_TEXT WHERE DLG_ID = (SELECT DLG_ID FROM TBL_DLG WHERE DLG_GROUP = @dlgGroup) AND USE_YN = 'Y'";
                //cmd.CommandText = "SELECT TEXT_DLG_ID, DLG_ID, CARD_TITLE, CARD_TEXT FROM TBL_SECCS_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y' AND DLG_ID > 999";

                cmd.Parameters.AddWithValue("@dlgGroup", dlgGroup);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    int textDlgId = Convert.ToInt32(rdr["TEXT_DLG_ID"]);
                    int dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    string cardTitle = rdr["CARD_TITLE"] as string;
                    string cardText = rdr["CARD_TEXT"] as string;

                    TextList dlgText = new TextList();
                    dlgText.textDlgId = textDlgId;
                    dlgText.dlgId = dlgId;
                    dlgText.cardTitle = cardTitle;
                    dlgText.cardText = cardText;


                    dialogText.Add(dlgText);
                }
            }
            return dialogText;
        }

        public List<CardList> SelectSorryDialogText(string dlgGroup)
        {
            SqlDataReader rdr = null;
            List<CardList> dialogCard = new List<CardList>();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT CARD_TEXT, BTN_1_TYPE, BTN_1_TITLE, BTN_1_CONTEXT FROM TBL_DLG_CARD WHERE DLG_ID = (SELECT DLG_ID FROM TBL_DLG WHERE DLG_GROUP = @dlgGroup) AND USE_YN = 'Y'";
                //cmd.CommandText = "SELECT TEXT_DLG_ID, DLG_ID, CARD_TITLE, CARD_TEXT FROM TBL_SECCS_DLG_TEXT WHERE DLG_ID = @dlgID AND USE_YN = 'Y' AND DLG_ID > 999";

                cmd.Parameters.AddWithValue("@dlgGroup", dlgGroup);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    //string cardTitle = rdr["CARD_TITLE"] as string;
                    string cardText = rdr["CARD_TEXT"] as string;
                    string cardBtn1type = rdr["BTN_1_TYPE"] as string;
                    string cardBtn1title = rdr["BTN_1_TITLE"] as string;
                    string cardBtn1context = rdr["BTN_1_CONTEXT"] as string;

                    CardList dlgCard = new CardList();
                    //dlgCard.cardTitle = cardTitle;
                    dlgCard.cardText = cardText;
                    dlgCard.btn1Type = cardBtn1type;
                    dlgCard.btn1Title = cardBtn1title;
                    dlgCard.btn1Context = cardBtn1context;


                    dialogCard.Add(dlgCard);
                }
            }
            return dialogCard;
        }


        //KSO START
        public CardList BannedChk(string orgMent)
        {
            SqlDataReader rdr = null;
            CardList SelectBanned = new CardList();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " SELECT";
                cmd.CommandText += " TOP 1 TD.DLG_ID, (SELECT TOP 1 BANNED_WORD FROM TBL_BANNED_WORD_LIST WHERE CHARINDEX(BANNED_WORD, @msg) > 0) AS BANNED_WORD, TDT.CARD_TITLE, TDT.CARD_TEXT";
                cmd.CommandText += " FROM TBL_DLG TD, TBL_DLG_TEXT TDT";
                cmd.CommandText += " WHERE TD.DLG_ID = TDT.DLG_ID";
                cmd.CommandText += " AND";
                cmd.CommandText += " 	TD.DLG_GROUP =";
                cmd.CommandText += " 	(";
                cmd.CommandText += " 	   SELECT CASE WHEN SUM(CASE WHEN BANNED_WORD_TYPE = 3 THEN CHARINDEX(A.BANNED_WORD, @msg) END) > 0 THEN 3";
                cmd.CommandText += " 			  WHEN SUM(CASE WHEN BANNED_WORD_TYPE = 4 THEN CHARINDEX(A.BANNED_WORD, @msg) END) > 0 THEN 4";
                cmd.CommandText += " 			 END";
                cmd.CommandText += " 	   FROM TBL_BANNED_WORD_LIST A";
                cmd.CommandText += " 	) AND TD.DLG_GROUP IN (3,4)";
                cmd.CommandText += " ORDER BY NEWID()";

                cmd.Parameters.AddWithValue("@msg", orgMent);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                while (rdr.Read())
                {
                    //answerMsg = rdr["CARD_TEXT"] + "@@" + rdr["DLG_ID"] + "@@" + rdr["CARD_TITLE"];

                    int dlg_id = Convert.ToInt32(rdr["DLG_ID"]);
                    String card_title = rdr["CARD_TITLE"] as String;
                    String card_text = rdr["CARD_TEXT"] as String;

                    SelectBanned.dlgId = dlg_id;
                    SelectBanned.cardTitle = card_title;
                    SelectBanned.cardText = card_text;
                }
            }
            return SelectBanned;
        }

        public CacheList CacheChk(string orgMent)
        {
            SqlDataReader rdr = null;
            CacheList result = new CacheList();
            Debug.WriteLine("CacheChk() parameter : " + orgMent);
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += "SELECT LUIS_ID, LUIS_INTENT, LUIS_ENTITIES, ISNULL(LUIS_INTENT_SCORE,'') AS LUIS_INTENT_SCORE FROM TBL_QUERY_ANALYSIS_RESULT WHERE LOWER(QUERY) = LOWER(@msg) AND RESULT ='H'";

                cmd.Parameters.AddWithValue("@msg", orgMent);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string luisId = rdr["LUIS_ID"] as String;
                    string intentId = rdr["LUIS_INTENT"] as String;
                    string entitiesId = rdr["LUIS_ENTITIES"] as String;
                    string luisScore = rdr["LUIS_INTENT_SCORE"] as String;


                    result.luisId = luisId;
                    result.luisIntent = intentId;
                    result.luisEntities = entitiesId;
                    result.luisScore = luisScore;
                }
            }
            return result;
        }

        public CacheList CacheDataFromIntent(string intent)
        {
            SqlDataReader rdr = null;
            CacheList result = new CacheList();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += "SELECT LUIS_ID, LUIS_INTENT, LUIS_ENTITIES, '' AS LUIS_INTENT_SCORE FROM TBL_DLG_RELATION_LUIS WHERE LUIS_INTENT=@intent";

                cmd.Parameters.AddWithValue("@intent", intent);
                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string luisId = rdr["LUIS_ID"] as String;
                    string intentId = rdr["LUIS_INTENT"] as String;
                    string entitiesId = rdr["LUIS_ENTITIES"] as String;
                    string luisScore = rdr["LUIS_INTENT_SCORE"] as String;
                    //string luisEntitiesValue = "" as String;

                    result.luisId = luisId;
                    result.luisIntent = intentId;
                    result.luisEntities = entitiesId;
                    result.luisScore = luisScore;
                    //result.luisEntitiesValue = luisEntitiesValue;


                    Debug.WriteLine("Yes rdr | intentId : " + intentId + " | entitiesId : " + entitiesId + " | luisScore : " + luisScore);
                }

            }
            return result;
        }
              
        public List<RelationList> DefineTypeChk(string luisId, string intentId, string entitiesId)
        {
            SqlDataReader rdr = null;
            List<RelationList> result = new List<RelationList>();
            Debug.WriteLine("luisId ::: " + luisId);
            Debug.WriteLine("intentId ::: " + intentId);
            Debug.WriteLine("entitiesId ::: " + entitiesId);
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += "SELECT A.LUIS_ID, A.LUIS_INTENT, A.LUIS_ENTITIES, ISNULL(A.DLG_ID,0) AS DLG_ID, A.DLG_API_DEFINE, A.API_ID ";
                cmd.CommandText += "  FROM TBL_DLG_RELATION_LUIS A, TBL_DLG B                                                    ";
                cmd.CommandText += " WHERE A.DLG_ID = B.DLG_ID                                               ";
                //cmd.CommandText += " WHERE LUIS_INTENT = @intentId                                                 ";
                cmd.CommandText += "   AND A.LUIS_ENTITIES = @entities                                                ";
                //cmd.CommandText += "   AND LUIS_ID = @luisId                                                        ";

                if (intentId != null)
                {
                    cmd.Parameters.AddWithValue("@intentId", intentId);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@intentId", DBNull.Value);
                }

                if (entitiesId != null)
                {
                    cmd.Parameters.AddWithValue("@entities", entitiesId);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@entities", DBNull.Value);
                }

                if (luisId != null)
                {
                    cmd.Parameters.AddWithValue("@luisId", luisId);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@luisId", DBNull.Value);
                }
                cmd.CommandText += "   ORDER BY B.DLG_ORDER_NO ASC                                               ";



                Debug.WriteLine("query : " + cmd.CommandText);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                while (rdr.Read())
                {
                    RelationList relationList = new RelationList();
                    relationList.luisId = rdr["LUIS_ID"] as string;
                    relationList.luisIntent = rdr["LUIS_INTENT"] as string;
                    relationList.luisEntities = rdr["LUIS_ENTITIES"] as string;
                    relationList.dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    relationList.dlgApiDefine = rdr["DLG_API_DEFINE"] as string;
                    //relationList.apiId = Convert.ToInt32(rdr["API_ID"] ?? 0);
                    relationList.apiId = rdr["API_ID"].Equals(DBNull.Value) ? 0 : Convert.ToInt32(rdr["API_ID"]);
                    //DBNull.Value
                    result.Add(relationList);
                }
            }
            return result;
        }

        public List<RelationList> DefineTypeChkSpare(string intent, string entity)
        {
            SqlDataReader rdr = null;
            List<RelationList> result = new List<RelationList>();
            entity = Regex.Replace(entity, " ", "");
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += "SELECT  LUIS_ID, LUIS_INTENT, LUIS_ENTITIES, ISNULL(DLG_ID,0) AS DLG_ID, DLG_API_DEFINE, API_ID ";
                cmd.CommandText += "  FROM  TBL_DLG_RELATION_LUIS                                                    ";
                //cmd.CommandText += " WHERE  LUIS_ENTITIES = @entities                                                ";
                cmd.CommandText += " WHERE  LUIS_INTENT = @intent                                                ";
                //cmd.CommandText += " AND  LUIS_ENTITIES = @entities                                                ";

                Debug.WriteLine("query : " + cmd.CommandText);
                Debug.WriteLine("entity : " + entity);
                Debug.WriteLine("intent : " + intent);
                cmd.Parameters.AddWithValue("@intent", intent);
                cmd.Parameters.AddWithValue("@entities", entity);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                while (rdr.Read())
                {
                    RelationList relationList = new RelationList();
                    relationList.luisId = rdr["LUIS_ID"] as string;
                    relationList.luisIntent = rdr["LUIS_INTENT"] as string;
                    relationList.luisEntities = rdr["LUIS_ENTITIES"] as string;
                    relationList.dlgId = Convert.ToInt32(rdr["DLG_ID"]);
                    relationList.dlgApiDefine = rdr["DLG_API_DEFINE"] as string;
                    //relationList.apiId = Convert.ToInt32(rdr["API_ID"] ?? 0);
                    relationList.apiId = rdr["API_ID"].Equals(DBNull.Value) ? 0 : Convert.ToInt32(rdr["API_ID"]);
                    //DBNull.Value
                    result.Add(relationList);
                }
            }
            return result;
        }


        //KSO END

        //TBL_CHATBOT_CONF 정보 가져오기
        //      LUIS_APP_ID	    - 루이스APP_ID
        //      LUIS_TIME_LIMIT - 루이스제한
        //      LUIS_SCORE_LIMIT - 스코어 제한
        //      LUIS_SUBSCRIPTION   - 루이스구독
        //      BOT_NAME        - 봇이름?
        //      BOT_APP_ID      - 봇앱아이디?
        //      BOT_APP_PASSWORD- 봇앱패스워드?
        //      QUOTE           - 견적url
        //      TESTDRIVE       - 시승url
        //      CATALOG         - 카달로그url
        //      DISCOUNT        - 할인url
        //      EVENT           - 이벤트url

        public List<ConfList> SelectConfig()
        //public List<ConfList> SelectConfig(string config_type)
        {
            SqlDataReader rdr = null;
            List<ConfList> conflist = new List<ConfList>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                DButil.HistoryLog("db conn SelectConfig !!");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = " SELECT CNF_TYPE, CNF_NM, CNF_VALUE" +
                                  " FROM TBL_CHATBOT_CONF " +
                                  //" WHERE CNF_TYPE = 'LUIS_APP_ID' " +
                                  " ORDER BY CNF_TYPE DESC, ORDER_NO ASC ";

                Debug.WriteLine("* cmd.CommandText : " + cmd.CommandText);
                //cmd.Parameters.AddWithValue("@config_type", config_type);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string cnfType = rdr["CNF_TYPE"] as string;
                    string cnfNm = rdr["CNF_NM"] as string;
                    string cnfValue = rdr["CNF_VALUE"] as string;

                    ConfList list = new ConfList();

                    list.cnfType = cnfType;
                    list.cnfNm = cnfNm;
                    list.cnfValue = cnfValue;


                    Debug.WriteLine("* cnfNm : " + cnfNm + " || cnfValue : " + cnfValue);
                    DButil.HistoryLog("* cnfNm : " + cnfNm + " || cnfValue : " + cnfValue);
                    conflist.Add(list);
                }
            }
            return conflist;
        }

        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Query Analysis
        // Insert user chat message for history and analysis
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public int insertUserQuery(List<RelationList> relationList, string luisId, string luisIntent, string luisEntities, string luisScore, string replyresult, string queryStr)
        {
            int dbResult = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                String luisID = "", intentName = "", entities = "", result = "", intentScore = "";

                int appID = 0;

                //if(MessagesController.recommendResult != "recommend")

                //if (MessagesController.relationList.Equals(null))
                if (relationList == null)
                {
                    entities = "None";
                    intentName = "None";
                    luisID = "None";
                    luisScore = "0";
                }
                else
                {

                    if (relationList.Count() > 0)
                    {
                        if (String.IsNullOrEmpty(relationList[0].luisId))
                        {
                            luisID = "None";
                        }
                        else
                        {
                            luisID = relationList[0].luisId;
                        }
                        if (String.IsNullOrEmpty(relationList[0].luisIntent))
                        {
                            intentName = "None";
                        }
                        else
                        {
                            intentName = relationList[0].luisIntent;
                        }
                        if (String.IsNullOrEmpty(relationList[0].luisEntities))
                        {
                            entities = "None";
                        }
                        else
                        {
                            entities = relationList[0].luisEntities;
                        }
                        if (String.IsNullOrEmpty(relationList[0].luisScore.ToString()))
                        {
                            intentScore = "0";
                        }
                        else
                        {
                            intentScore = relationList[0].luisScore.ToString();
                        }
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(luisId))
                        {
                            luisID = "None";
                        }
                        else
                        {
                            luisID = luisId;
                        }
                        if (String.IsNullOrEmpty(luisIntent))
                        {
                            intentName = "None";
                        }
                        else
                        {
                            intentName = luisIntent;
                        }
                        if (String.IsNullOrEmpty(luisEntities))
                        {
                            entities = "None";
                        }
                        else
                        {
                            entities = luisEntities;
                        }
                        if (String.IsNullOrEmpty(luisScore))
                        {
                            intentScore = "0";
                        }
                        else
                        {
                            intentScore = luisScore;
                        }
                    }
                }

                if (String.IsNullOrEmpty(replyresult))
                {
                    result = "D";
                }
                else
                {
                    result = replyresult;
                }
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "sp_insertusehistory4";

                cmd.CommandType = CommandType.StoredProcedure;


                //if (result.Equals("S") || result.Equals("D"))
                //{
                //    cmd.Parameters.AddWithValue("@Query", "");
                //    cmd.Parameters.AddWithValue("@intentID", "");
                //    cmd.Parameters.AddWithValue("@entitiesIDS", "");
                //    cmd.Parameters.AddWithValue("@intentScore", "");
                //    cmd.Parameters.AddWithValue("@luisID", "");
                //    cmd.Parameters.AddWithValue("@result", result);
                //    cmd.Parameters.AddWithValue("@appID", appID);
                //}
                //else
                //{
                Debug.WriteLine("DDDDDD : " + Regex.Replace(queryStr, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline).Trim().ToLower());
                cmd.Parameters.AddWithValue("@Query", Regex.Replace(queryStr, @"[^a-zA-Z0-9ㄱ-힣]", "", RegexOptions.Singleline).Trim().ToLower());
                cmd.Parameters.AddWithValue("@intentID", intentName.Trim());
                cmd.Parameters.AddWithValue("@entitiesIDS", entities.Trim().ToLower());
                if (result.Equals("D") || result.Equals("S") || result.Equals("G") || result.Equals("Q") || result.Equals("I") || result.Equals("E") || result.Equals("Z") || result.Equals("B"))
                {
                    cmd.Parameters.AddWithValue("@intentScore", "0");
                }
                else
                {
                    //if(MessagesController.relationList != null)
                    //{
                    if (relationList.Count > 0 && relationList[0].luisEntities != null)
                    {
                        cmd.Parameters.AddWithValue("@intentScore", relationList[0].luisScore);
                    }
                    //}
                    else
                    {
                        cmd.Parameters.AddWithValue("@intentScore", luisScore);
                    }
                }
                cmd.Parameters.AddWithValue("@luisID", luisID);
                cmd.Parameters.AddWithValue("@result", result);
                cmd.Parameters.AddWithValue("@appID", appID);
                //}

                dbResult = cmd.ExecuteNonQuery();



            }
            return dbResult;
        }

        public int insertUserQuery(string korQuery, string intentID, string entitiesIDS, string intentScore, String luisID, char result, int appID)
        {
            int dbResult = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "sp_insertusehistory4";

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Query", korQuery.Trim().ToLower());
                cmd.Parameters.AddWithValue("@intentID", intentID.Trim());
                cmd.Parameters.AddWithValue("@entitiesIDS", entitiesIDS.Trim().ToLower());
                cmd.Parameters.AddWithValue("@intentScore", intentScore.Trim().ToLower());
                cmd.Parameters.AddWithValue("@luisID", luisID);
                cmd.Parameters.AddWithValue("@result", result);
                cmd.Parameters.AddWithValue("@appID", appID);


                dbResult = cmd.ExecuteNonQuery();
            }
            return dbResult;
        }

        public int insertHistory(List<RelationList> relationList, string userNumber, string channel, int responseTime, string luis_intent, string luis_entities, string luis_intent_score, string dlg_id, string reply_result, string queryStr)
        {
            //SqlDataReader rdr = null;
            int appID = 0;
            int result;
            String intentName = "";

            //if (MessagesController.relationList.Equals(null))
            if (relationList == null)
            {
                intentName = "None";
            }
            else
            {
                if (relationList.Count() > 0)
                {
                    if (String.IsNullOrEmpty(relationList[0].luisIntent))
                    {
                        intentName = "None";
                    }
                    else
                    {
                        intentName = relationList[0].luisIntent;
                    }
                }
                else
                {
                    if (String.IsNullOrEmpty(luis_intent))
                    {
                        intentName = "None";
                    }
                    else
                    {
                        intentName = luis_intent;
                    }
                }
            }

            if (string.IsNullOrEmpty(luis_intent))
            {
                luis_intent = "";
            }

            if (string.IsNullOrEmpty(luis_entities))
            {
                luis_entities = "";
            }

            if (string.IsNullOrEmpty(luis_intent_score))
            {
                luis_intent_score = "";
            }

            dlg_id = dlg_id.TrimEnd(',');
            if (string.IsNullOrEmpty(dlg_id))
            {
                dlg_id = "";
            }
             
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText += " INSERT INTO TBL_HISTORY_QUERY ";
                cmd.CommandText += " (USER_NUMBER, CUSTOMER_COMMENT_KR, CHATBOT_COMMENT_CODE, CHANNEL, RESPONSE_TIME, REG_DATE, ACTIVE_FLAG, APP_ID, LUIS_INTENT, LUIS_ENTITIES, LUIS_INTENT_SCORE, DLG_ID, RESULT, USER_ID, MOBILE_YN) ";
                cmd.CommandText += " VALUES ";
                //cmd.CommandText += " (@userNumber, @customerCommentKR, @chatbotCommentCode, @channel, @responseTime, CONVERT(VARCHAR,  GETDATE(), 101) + ' ' + CONVERT(VARCHAR,  DATEADD( HH, 9, GETDATE() ), 24), 0, @appID, @luis_intent, @luis_entities, @luis_intent_score, @dlg_id, @result, (SELECT TOP 1 USER_ID FROM TBL_USERDATA WHERE CHANNELDATA=@channel AND CONVERSATIONSID = @userNumber)) ";
                cmd.CommandText += " (@userNumber, @customerCommentKR, @chatbotCommentCode, @channel, @responseTime, CONVERT(VARCHAR,  DATEADD( HH, 9, GETDATE() ), 101) + ' ' + CONVERT(VARCHAR,  DATEADD( HH, 9, GETDATE() ), 24), 0, @appID, @luis_intent, @luis_entities, @luis_intent_score, @dlg_id, @result, (SELECT TOP 1 USER_ID FROM TBL_USERDATA WHERE CHANNELDATA=@channel AND CONVERSATIONSID = @userNumber), (SELECT TOP 1 MOBILE_YN FROM TBL_USERDATA WHERE CHANNELDATA=@channel AND CONVERSATIONSID = @userNumber)) ";

                cmd.Parameters.AddWithValue("@userNumber", userNumber);
                cmd.Parameters.AddWithValue("@customerCommentKR", queryStr);

                if (reply_result.Equals("S"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "SMALLTALK");
                }
                else if (reply_result.Equals("D"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "ERROR");
                }
                else if (reply_result.Equals("E"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "ERROR");
                }
                else if (reply_result.Equals("G"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "SUGGESTION");
                }
                else if (reply_result.Equals("I"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "SAPINIT");
                }
                else if (reply_result.Equals("Q"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "SAP");
                }
                else if (reply_result.Equals("Z"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "SAP");
                }
                else if (reply_result.Equals("B"))
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", "BANNED");
                }
                else
                {
                    cmd.Parameters.AddWithValue("@chatbotCommentCode", luis_intent);
                }

                cmd.Parameters.AddWithValue("@channel", channel);
                cmd.Parameters.AddWithValue("@responseTime", responseTime);
                cmd.Parameters.AddWithValue("@appID", appID);

                cmd.Parameters.AddWithValue("@luis_intent", luis_intent);
                cmd.Parameters.AddWithValue("@luis_entities", luis_entities);
                cmd.Parameters.AddWithValue("@luis_intent_score", luis_intent_score);
                cmd.Parameters.AddWithValue("@dlg_id", dlg_id);
                cmd.Parameters.AddWithValue("@result", reply_result);

                try
                {
                    result = cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    result = '1';
                    Debug.WriteLine("ex : " + ex.Message);
                }
                //Debug.WriteLine("result : " + result);
            }
            return result;
        }        

        public String SmallTalkSentenceConfirm(string queryStr)
        {

            String query = Regex.Replace(queryStr, @"[^a-zA-Z0-9ㄱ-힣-]", "", RegexOptions.Singleline).Replace(" ", "").ToLower();
            SqlDataReader rdr = null;
            String smallTalkAnswer = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "SELECT TOP 1 MAX(A.ANSWER) AS ANSWER ";
                cmd.CommandText += "FROM ";
                cmd.CommandText += "    ( ";
                cmd.CommandText += "        SELECT  S_ANSWER AS ANSWER FROM TBL_SMALLTALK ";
                cmd.CommandText += "        WHERE  S_QUERY = @kr_query ";
                cmd.CommandText += "        AND      USE_YN = 'Y' ";
                cmd.CommandText += "    ) A ";

                cmd.Parameters.AddWithValue("@kr_query", query);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                try
                {
                    while (rdr.Read())
                    {
                        smallTalkAnswer = rdr["ANSWER"] as string;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                rdr.Close();

            }
            return smallTalkAnswer;

        }

        public String SmallTalkConfirm(string queryStr)
        {

            String query = Regex.Replace(queryStr, @"[^a-zA-Z0-9ㄱ-힣-]", "", RegexOptions.Singleline).Replace(" ", "").ToLower();
            SqlDataReader rdr = null;
            String smallTalkAnswer = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "SELECT TOP 1 MAX(A.ANSWER) AS ANSWER ";
                cmd.CommandText += "FROM ";
                cmd.CommandText += "    ( ";
                cmd.CommandText += "        SELECT  S_ANSWER AS ANSWER FROM TBL_SMALLTALK ";
                cmd.CommandText += "        WHERE  S_QUERY = @kr_query ";
                cmd.CommandText += "        AND      USE_YN = 'Y' ";
                cmd.CommandText += "        UNION ALL ";
                cmd.CommandText += "        SELECT  S_ANSWER FROM TBL_SMALLTALK ";
                cmd.CommandText += "        WHERE  CHARINDEX(ENTITY, @kr_query) > 0 ";
                cmd.CommandText += "        AND      USE_YN = 'Y' ";
                cmd.CommandText += "    ) A ";

                cmd.Parameters.AddWithValue("@kr_query", query);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                try
                {
                    while (rdr.Read())
                    {
                        smallTalkAnswer = rdr["ANSWER"] as string;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                rdr.Close();

            }
            return smallTalkAnswer;

        }

        public String getIntentFromSmallTalk(string queryStr)
        {

            String query = Regex.Replace(queryStr, @"[^a-zA-Z0-9ㄱ-힣-]", "", RegexOptions.Singleline).Replace(" ", "").ToLower();
            SqlDataReader rdr = null;
            String smallTalkIntent = "";

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "SELECT INTENT FROM TBL_SMALLTALK ";
                cmd.CommandText += "WHERE  S_QUERY = @kr_query ";
                cmd.CommandText += "AND      USE_YN = 'Y' ";

                cmd.Parameters.AddWithValue("@kr_query", query);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                try
                {
                    while (rdr.Read())
                    {
                        smallTalkIntent = rdr["INTENT"] as string;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                rdr.Close();

            }
            return smallTalkIntent;

        }


        public int UserCheckDataInsert(string channelData, string conversationsId)
        {

            SqlDataReader rdr = null;
            int result = 0;

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "INSERT INTO TBL_USERCHECK(CHANNELDATA, CONVERSATIONSID, AUTH_CHECK) ";
                cmd.CommandText += " VALUES (@channeldata, @conversationsid,'F')";

                cmd.Parameters.AddWithValue("@channeldata", channelData);
                cmd.Parameters.AddWithValue("@conversationsid", conversationsId);


                try
                {
                    result = cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

            }
            return result;
        }

        public int UserDataUpdate(string channelData, string conversationsId, int cnt, string gubun)
        {

            SqlDataReader rdr = null;
            int result = 0;           

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                if (gubun.Equals("loop"))
                {
                    cmd.CommandText += " UPDATE     TBL_USERDATA ";
                    cmd.CommandText += " SET           LOOP = @cnt ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND           CONVERSATIONSID = @conversationsid ";

                }
                else
                {
                    cmd.CommandText += " UPDATE     TBL_USERDATA ";
                    cmd.CommandText += " SET           SAP = @cnt ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                
                cmd.Parameters.AddWithValue("@channeldata", channelData);
                cmd.Parameters.AddWithValue("@conversationsid", conversationsId);
                cmd.Parameters.AddWithValue("@cnt", cnt);


                try
                {
                    result = cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            return result;
        }

        public List<UserCheck> UserDataConfirm(string channelData, string conversationsId)
        {
            SqlDataReader rdr = null;
            List<UserCheck> userdata = new List<UserCheck>();
            SqlCommand cmd = new SqlCommand();

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                cmd.Connection = conn;

                cmd.CommandText += "SELECT  TOP 1 CHANNELDATA, CONVERSATIONSID, USER_PHONE, API_INTENT, API_OLDINTENT, ISNULL(AUTH_CHECK, 'F') AS AUTH_CHECK, AUTH_NUMBER, MOBILEPC, USER_NAME, NAMECHECK, ADDRESSCHECK, AUTH_URL, API_CHECK ";
                cmd.CommandText += "FROM    TBL_USERCHECK ";
                cmd.CommandText += "WHERE  CHANNELDATA = @channeldata ";
                cmd.CommandText += "AND      CONVERSATIONSID = @conversationsId ";

                cmd.Parameters.AddWithValue("@channeldata", channelData);
                cmd.Parameters.AddWithValue("@conversationsId", conversationsId);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                try
                {
                    while (rdr.Read())
                    {
                        UserCheck userData = new UserCheck();
                        
                        userData.channelData = rdr["CHANNELDATA"] as string;
                        userData.conversationsId = rdr["CONVERSATIONSID"] as string;
                        userData.userPhone = rdr["USER_PHONE"] as string;
                        userData.apiIntent = rdr["API_INTENT"] as string;
                        userData.apiOldIntent = rdr["API_OLDINTENT"] as string;
                        userData.authCheck = rdr["AUTH_CHECK"] as string;
                        userData.authNumber = rdr["AUTH_NUMBER"] as string;
                        userData.mobilePc = rdr["MOBILEPC"] as string;
                        userData.userName = rdr["USER_NAME"] as string;
                        userData.nameCheck = rdr["NAMECHECK"] as string;
                        userData.addressCheck = rdr["ADDRESSCHECK"] as string;
                        userData.authUrl = rdr["AUTH_URL"] as string;
                        userData.apiCheck = rdr["API_CHECK"] as string;

                        userdata.Add(userData);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                rdr.Close();
            }
            return userdata;
        }

        public int UserCheckUpdate(string channelData, string conversationsId, string gubun, string val)
        {

            SqlDataReader rdr = null;
            int result = 0;

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                if (gubun.Equals("AUTH_CHECK"))
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           AUTH_CHECK = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("API_INTENT"))
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           API_INTENT = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("API_OLDINTENT"))
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           API_OLDINTENT = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("AUTH_NUMBER"))
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           AUTH_NUMBER = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("USER_PHONE"))
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           USER_PHONE = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("MOBILEPC"))//mobile, pc 여부
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           MOBILEPC = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("USER_NAME"))//인증이름
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           USER_NAME = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("NAMECHECK"))//인증이름
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           NAMECHECK = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("ADDRESSCHECK"))//인증이름
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           ADDRESSCHECK = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("AUTH_URL"))//인증이름
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           AUTH_URL = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("API_CHECK"))//인증이름
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           API_CHECK = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else if (gubun.Equals("SORRY_INTENT"))
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           SORRY_INTENT = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }
                else
                {
                    cmd.CommandText += " UPDATE     TBL_USERCHECK ";
                    cmd.CommandText += " SET           USER_ID = @val ";
                    cmd.CommandText += " WHERE      CHANNELDATA = @channeldata ";
                    cmd.CommandText += " AND          CONVERSATIONSID = @conversationsid ";
                }

                cmd.Parameters.AddWithValue("@channeldata", channelData);
                cmd.Parameters.AddWithValue("@conversationsid", conversationsId);
                cmd.Parameters.AddWithValue("@val", val);                

                try
                {
                    result = cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }
            return result;
        }

        public List<UserData> UserDataSapConfirm(string channelData, string conversationsId)
        {
            SqlDataReader rdr = null;
            List<UserData> userdata = new List<UserData>();
            SqlCommand cmd = new SqlCommand();

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                cmd.Connection = conn;

                cmd.CommandText += "SELECT  TOP 1 USER_ID, SABUN, REISSUE, OPTIONAL_1 ";
                cmd.CommandText += "FROM    TBL_USERDATA ";
                cmd.CommandText += "WHERE  CHANNELDATA = @channeldata ";
                cmd.CommandText += "AND      CONVERSATIONSID = @conversationsId ";

                cmd.Parameters.AddWithValue("@channeldata", channelData);
                cmd.Parameters.AddWithValue("@conversationsId", conversationsId);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                try
                {
                    while (rdr.Read())
                    {
                        UserData userData = new UserData();
                        userData.userId = rdr["USER_ID"] as string;
                        userData.sabun = rdr["SABUN"] as string;
                        userData.reissue = rdr["REISSUE"] as string;
                        userData.optional_1 = rdr["OPTIONAL_1"] as string;
                        
                        userdata.Add(userData);

                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                rdr.Close();
            }
            return userdata;
        }

        public int UserDataDeleteUserID(string userid)
        {

            SqlDataReader rdr = null;
            int result = 0;

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "DELETE FROM TBL_USERDATA ";
                cmd.CommandText += " WHERE USER_ID = @userid";

                cmd.Parameters.AddWithValue("@userid", userid);
                
                try
                {
                    result = cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

            }
            return result;
        }

        public int InsertError(string conversationsId, string err)
        {

            SqlDataReader rdr = null;
            int result = 0;

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "INSERT INTO TBL_ERROR(CONVERSATIONSID, ERROR, REG_DATE) ";
                cmd.CommandText += " VALUES (@conversationsid,@err,GETDATE())";

                cmd.Parameters.AddWithValue("@channeldata", conversationsId);
                cmd.Parameters.AddWithValue("@err", err);
                DButil.HistoryLog("InsertError START");

                try
                {
                    result = cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    DButil.HistoryLog("InsertError ERROR");
                }
                DButil.HistoryLog("InsertError END");
            }
            return result;
        }

        /*
         * API INTENT DATA 검색
         * */
        public String getAPITFData(string luisIntent)
        {

            String luis_intent = luisIntent;
            SqlDataReader rdr = null;
            String apiTFData = "";
            int checkCount = 0;

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "SELECT API_INTENT ";
                cmd.CommandText += "FROM TBL_DLG_RELATION_LUIS ";
                cmd.CommandText += "WHERE LUIS_INTENT = @luis_intent";

                cmd.Parameters.AddWithValue("@luis_intent", luis_intent);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                try
                {
                    while (rdr.Read())
                    {
                        apiTFData = rdr["API_INTENT"] as string;
                        checkCount++;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                rdr.Close();

                if (checkCount == 0)
                {
                    apiTFData = "F";
                }

            }
            return apiTFData;

        }

        /*
         * LUIS MIN DATA
         * */
        public String getLuisMINData(string luisIMinntent)
        {

            String luis_minintent = luisIMinntent;
            SqlDataReader rdr = null;
            String luisMinData = "";
            int checkCount = 0;

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "SELECT DLG_QUESTION ";
                cmd.CommandText += "FROM TBL_DLG_RELATION_LUIS ";
                cmd.CommandText += "WHERE LUIS_INTENT = @luis_minintent";

                cmd.Parameters.AddWithValue("@luis_minintent", luis_minintent);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                try
                {
                    while (rdr.Read())
                    {
                        luisMinData = rdr["DLG_QUESTION"] as string;
                        checkCount++;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                rdr.Close();
                
            }
            return luisMinData;

        }

        public String getSorryIntent(string conversationsId)
        {

            SqlDataReader rdr = null;
            String sorryIntent = "";
            
            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "SELECT SORRY_INTENT ";
                cmd.CommandText += "FROM TBL_USERCHECK ";
                cmd.CommandText += "WHERE CONVERSATIONSID = @conversationsId ";

                cmd.Parameters.AddWithValue("@conversationsId", conversationsId);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                try
                {
                    while (rdr.Read())
                    {
                        sorryIntent = rdr["SORRY_INTENT"] as string;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                rdr.Close();

            }
            return sorryIntent;

        }

        public List<APIConfList> SelectAPIConfig()
        //public List<ConfList> SelectConfig(string config_type)
        {
            SqlDataReader rdr = null;
            List<APIConfList> confApilist = new List<APIConfList>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                DButil.HistoryLog("db conn APIConfList !!");
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = " SELECT API_NAME, API_INTENT, API_URL, API_KORNAME" +
                                  " FROM TBL_CHATBOT_API " +
                                  " ORDER BY API_NAME DESC ";

                Debug.WriteLine("* cmd.CommandText : " + cmd.CommandText);
                //cmd.Parameters.AddWithValue("@config_type", config_type);

                rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                while (rdr.Read())
                {
                    string apiName = rdr["API_NAME"] as string;
                    string apiIntent = rdr["API_INTENT"] as string;
                    string apiUrl = rdr["API_URL"] as string;
                    string apiKorName = rdr["API_KORNAME"] as string;

                    APIConfList list = new APIConfList();

                    list.apiName = apiName;
                    list.apiIntent = apiIntent;
                    list.apiUrl = apiUrl;
                    list.apiKorName = apiKorName;

                    confApilist.Add(list);
                }
            }
            return confApilist;
        }

        public int UserReportDataInsert(string consultingDataTemp)
        {
            DButil.HistoryLog("db conn UserReportDataInsert !!");
            String consultingData = consultingDataTemp.Substring(11);
            
            SqlDataReader rdr = null;
            int result = 0;
            /*
             * DATA 정리
             * */
            String[] consultingDataDB;
            consultingDataDB = consultingData.Split('&');
            if (consultingDataDB[0] == null)
            {
                consultingDataDB[0] = "1";
            }
            int rCount = Int32.Parse(consultingDataDB[0]);
            String rComment = consultingDataDB[1];

            DButil.HistoryLog("rCount=="+rCount);
            DButil.HistoryLog("rComment==" + rComment);

            using (SqlConnection conn = new SqlConnection(connStr))
            {

                conn.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = conn;

                cmd.CommandText += "INSERT INTO TBL_CHATBOT_USEREPORT(R_COUNT, R_COMMENT, R_WDATE) ";
                cmd.CommandText += " VALUES (@rCount, @rComment,GETDATE())";


                cmd.Parameters.AddWithValue("@rCount", rCount);
                cmd.Parameters.AddWithValue("@rComment", rComment);


                try
                {
                    result = cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    DButil.HistoryLog(e.Message);
                }

            }
            return result;
        }

    }
}
