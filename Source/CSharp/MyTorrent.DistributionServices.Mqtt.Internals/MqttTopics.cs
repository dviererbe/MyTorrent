using MQTTnet;
using MQTTnet.Protocol;
using System;
using System.Runtime.CompilerServices;

namespace MyTorrent.DistributionServices
{
    public static class MqttTopics
    {
        #region Tracker

        public const string TrackerHello = "tracker/hello";
        
        public const string TrackerGoodbye = "tracker/goodbye";

        #endregion

        #region Clients

        public const string ClientJoinRequested = "clients/join/requested";
        
        public const string ClientJoinAccepted = "clients/join/accepted";
        
        public const string ClientJoinDenied = "clients/join/denied";
        
        public const string ClientJoinSucceeded = "clients/join/succeeded";

        public const string ClientJoinFailed = "clients/join/failed";

        public const string ClientRegistered = "clients/registered";
        
        public const string ClientGoodbye = "clients/goodbye";

        #endregion

        #region Files

        public const string FileInfoPublished = "files/info";

        #endregion

        #region Fragments

        public const string FragmentDistributionStarted = "fragments/distribution/started";

        public const string FragmentDistributionRequested = "fragments/distribution/requested";

        public const string FragmentDistributionDelivered = "fragments/distribution/delivered";

        public const string FragmentDistributionObtained = "fragments/distribution/obtained";

        public const string FragmentDistributionFailed = "fragments/distribution/failed";

        public const string FragmentDistributionEnded = "fragments/distribution/ended";

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TopicFilter BuildTopic(string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        {
            return new TopicFilter()
            {
                Topic = topic,
                QualityOfServiceLevel = qualityOfServiceLevel
            };
        }
    }
}
