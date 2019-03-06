using System;
using System.Collections.Generic;
using System.Text;
using com.miracom.transceiverx;
using com.miracom.transceiverx.session;
using com.miracom.transceiverx.message;

namespace HanjinChatBot.SAP
{
    class H101EaiCall : SessionEventListener
    {
        private const String connectString = "52.2.198.170:10101";  // EAI DEV Server IP Address
        private Session ioiSession = null;
        private int sessionMode = Session_Fields.SESSION_INTER_STATION_MODE |
            Session_Fields.SESSION_PULL_DELIVERY_MODE;
        private short msgDeliveryType = DeliveryType.REQUEST;

        private void connect(String sessionName)
        {
            try
            {
                ioiSession = Transceiver.createSession(sessionName, sessionMode);
                ioiSession.addSessionEventListener(this);
                ioiSession.setAutoRecovery(true);
                ioiSession.setDefaultTTL(30000);
                ioiSession.connect(connectString);
            }
            catch (SessionException ex)
            {
                throw new TrxException(ex.ToString(), ex);
            }
        }

        public String sendMessage(String sessionName, String systemID, String interfaceID, String contents, long ttl)
        {
            String strRep = null;

            try
            {
                connect(sessionName);
                Message msg = ioiSession.createMessage();

                String msgChannel = "/" + systemID + "/" + interfaceID;
                msg.setChannel(msgChannel);
                msg.setDeliveryMode(this.msgDeliveryType);
                msg.setTTL(ttl);
                Encoding en = Encoding.UTF8;
                msg.setData(en.GetBytes(contents));

                Message reply = ioiSession.sendRequest(msg);
                if (reply == null)
                    throw new TrxException(TrxException.INVALID_MESSAGE);
                strRep = en.GetString(reply.getData());
                
            }
            catch (TrxException ex)
            {
                throw new TrxException(ex.ToString(), ex);
            }
            finally
            {
                try
                {
                    stop();
                }
                catch (Exception e)
                {
                    throw new TrxException(e.ToString(), e);
                }
            }
            return strRep;
        }

        private void stop()
        {
            ioiSession.disconnect();
            ioiSession.destroy();
        }

        #region SessionEventListener ¸â¹ö

        void SessionEventListener.onConnect(Session ss)
        {
            Console.WriteLine("Session Connected...");
        }

        void SessionEventListener.onDisconnect(Session ss)
        {
            Console.WriteLine("Session Disconnected...");
        }

        #endregion
    }
}
