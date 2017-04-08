﻿using System;
using System.Net;
using Lidgren.Network;
using System.Collections.Generic;
using System.Linq;

namespace TankGameTestFramework
{
    public class FakeNetConnection : INetConnection
    {
        public FakeNetPeer EndPoint { get; set; }

        public List<FakeNetIncomingMessage> MessagesInTransit { get; private set; } = new List<FakeNetIncomingMessage>();

        public float AverageRoundtripTime { get; set; }

        #region Not Implemented
        public int CurrentMTU
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public NetOutgoingMessage LocalHailMessage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public NetPeer Peer
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public NetIncomingMessage RemoteHailMessage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public float RemoteTimeOffset
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public long RemoteUniqueIdentifier
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public NetConnectionStatistics Statistics
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public NetConnectionStatus Status
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object Tag
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        INetOutgoingMessage INetConnection.LocalHailMessage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Approve()
        {
            throw new NotImplementedException();
        }

        public void Approve(INetOutgoingMessage localHail)
        {
            throw new NotImplementedException();
        }

        public void Approve(NetOutgoingMessage localHail)
        {
            throw new NotImplementedException();
        }

        public bool CanSendImmediately(NetDeliveryMethod method, int sequenceChannel)
        {
            throw new NotImplementedException();
        }

        public void Deny()
        {
            throw new NotImplementedException();
        }

        public void Deny(string reason)
        {
            throw new NotImplementedException();
        }

        public void Disconnect(string byeMessage)
        {
            throw new NotImplementedException();
        }
        public void GetSendQueueInfo(NetDeliveryMethod method, int sequenceChannel, out int windowSize, out int freeWindowSlots)
        {
            throw new NotImplementedException();
        }
        #endregion

        public double GetLocalTime(double remoteTimestamp)
        {
            return remoteTimestamp;
        }

        public double GetRemoteTime(double localTimestamp)
        {
            return localTimestamp;
        }

        public NetSendResult SendMessage(INetOutgoingMessage msg, NetDeliveryMethod method, int sequenceChannel)
        {
            var _msg = (FakeNetOutgoingMessage)msg;
            _msg.SendTime = NetTime.Now;
            if (Latency > 0)
            {
                MessagesInTransit.Add(_msg.ToIncomingMessage(Latency));
            }
            else
            {
                EndPoint.Messages.Enqueue(_msg.ToIncomingMessage(Latency));
            }
            return NetSendResult.Sent;
        }

        public void SetTime(double time)
        {
            var arrivals = MessagesInTransit
                .FindAll(item => item.ReceiveTime <= time)
                .OrderBy(item => item.ReceiveTime)
                .ToList();
            MessagesInTransit = MessagesInTransit.Except(arrivals).ToList();
            arrivals.ForEach(EndPoint.Messages.Enqueue);
        }

        public double Latency { get; set; }
    }
}