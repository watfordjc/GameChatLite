using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace uk.JohnCook.dotnet.GameChatLite.Model
{
    public class DiscordGateway
    {
        public static readonly int Version = 9;

        public class Payload
        {
            [JsonPropertyName("op")]
            public OpCode OpCode { get; set; }
            [JsonPropertyName("d")]
            public object? Data { get; set; }
            [JsonPropertyName("s")]
            public int? Seq { get; set; }
            [JsonPropertyName("t")]
            public string? Name { get; set; }
        }

        public enum OpCode
        {
            Dispatch = 0,
            Heartbeat = 1,
            Identify = 2,
            PresenceUpdate = 3,
            VoiceStateUpdate = 4,
            Resume = 6,
            Reconnect = 7,
            RequestGuildMembers = 8,
            InvalidSession = 9,
            Hello = 10,
            HeartbeatAck = 11
        }

        public enum CloseEventCode
        {
            UnknownError = 4000,
            UnknownOpCode = 4001,
            DecodeError = 4002,
            NotAuthenticated = 4003,
            AuthenticationFailed = 4004,
            AlreadyAuthenticated = 4005,
            InvalidSeq = 4007,
            RateLimited = 4008,
            SessionTimedOut = 4009,
            InvalidShared = 4010,
            ShardingRequired = 4011,
            InvalidApiVersion = 4012,
            InvalidIntent = 4013,
            DisallowedIntent = 4014
        }
    }

    public class DiscordVoice
    {
        public enum OpCode
        {
            Identify = 0,
            SelectProtocol = 1,
            Ready = 2,
            Heartbeat = 3,
            SessionDescription = 4,
            Speaking = 5,
            HeartbeatAck = 6,
            Resume = 7,
            Hello = 8,
            Resumed = 9,
            ClientDisconnect = 10
        }

        public enum CloseEventCode
        {
            UnknownOpCode = 4001,
            FailedToDecodePayload = 4002,
            NotAuthenticated = 4003,
            AuthenticationFailed = 4004,
            AlreadyAuthenticated = 4005,
            SessionNoLongerValid = 4006,
            SessionTimedOut = 4009,
            ServerNotFound = 4011,
            UnknownProtocol = 4012,
            Disconnected = 4014,
            VoiceServerCrashed = 4015,
            UnknownEncryptionMode = 4016
        }
    }
}
