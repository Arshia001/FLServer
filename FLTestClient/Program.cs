using Bond;
using Bond.Tag;
using FLGameLogic;
using FLGameLogicServer;
using FLGrainInterfaces;
using FLGrains;
using LightMessage.Client;
using LightMessage.Client.EndPoints;
using LightMessage.Common.Messages;
using LightMessage.Common.ProtocolMessages;
using LightMessage.Common.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Orleans.Serialization;
using OrleansBondUtils;
using OrleansCassandraUtils;
using OrleansCassandraUtils.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FLTestClient
{
    class NullGrainReferenceConverter : IGrainReferenceConverter
    {
        public GrainReference GetGrainFromKeyInfo(GrainReferenceKeyInfo keyInfo)
        {
            return null;
        }

        public GrainReference GetGrainFromKeyString(string key)
        {
            return null;
        }
    }

    [Schema]
    public class StatisticWithParameter : IEquatable<StatisticWithParameter>
    {
        [Obsolete] public StatisticWithParameter() { }

        public StatisticWithParameter(Statistics statistic, int parameter)
        {
            Statistic = statistic;
            Parameter = parameter;
        }

        [Id(0)] public Statistics Statistic { get; set; }
        [Id(1)] public int Parameter { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as StatisticWithParameter);
        }

        public bool Equals(StatisticWithParameter other)
        {
            return other != null &&
                   Statistic == other.Statistic &&
                   Parameter == other.Parameter;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Statistic, Parameter);
        }
    }

    [Schema, BondSerializationTag("#pppppp")]
    public class PlayerState
    {
        [Id(0)]
        public List<IGame> ActiveGames { get; private set; }

        [Id(1)]
        public string Name { get; set; }

        [Id(2)]
        public uint Level { get; set; }

        [Id(3)]
        public uint XP { get; set; }

        [Id(4)]
        public uint NumRoundsWonForReward { get; set; }

        [Id(5)]
        public DateTime LastRoundWinRewardTakeTime { get; set; }

        [Id(6)]
        public uint Score { get; set; }

        [Id(7)]
        public ulong Gold { get; set; }

        [Id(8)]
        public List<IGame> PastGames { get; private set; }

        [Id(9)]
        public DateTime InfinitePlayEndTime { get; set; }

        [Id(10)]
        public HashSet<string> OwnedCategoryAnswers { get; set; }

        [Id(11)]
        public Dictionary<StatisticWithParameter, ulong> StatisticsValues { get; set; }

        [Id(12)]
        public byte[] PasswordSalt { get; set; }

        [Id(13)]
        public byte[] PasswordHash { get; set; }

        [Id(14)]
        public string Email { get; set; }

        [Id(15)]
        public string Username { get; set; }
    }

    [Schema, BondSerializationTag("#gggggg")]
    public class GameGrain_State : IOnDeserializedHandler
    {
        [Id(0)]
        public SerializedGameData GameData { get; set; }

        [Id(1)]
        public Guid[] PlayerIDs { get; set; }

        [Id(2)]
        public int[] LastProcessedEndTurns { get; set; } //?? use to reprocess turn end notifications in case grain goes down

        [Id(3)]
        public int GroupChooser { get; set; } = -1;

        [Id(4)]
        public List<ushort> GroupChoices { get; set; }

        public void OnDeserialized()
        {
            if (PlayerIDs == null)
                PlayerIDs = Array.Empty<Guid>();
            if (LastProcessedEndTurns == null)
                LastProcessedEndTurns = new[] { -1, -1 };
        }
    }

    class Program
    {
        static Guid ID(int i) => new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        static void Main(string[] args)
        {
            var svc = new ServiceCollection();
            ServiceConfiguration.ConfigureGameServer(svc, "Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy");
            svc.AddSingleton<IGrainReferenceConverter, NullGrainReferenceConverter>();
            var provider = svc.BuildServiceProvider();

            BondSerializationUtil.Initialize(provider);
            var ss = new GameGrain_State();
            ss.OnDeserialized();
            ss.GameData = new GameLogicServer(1).Serialize();
            var s = BondSerializer.Serialize(ss);
            var d = (GameGrain_State)BondSerializer.Deserialize(typeof(GameGrain_State), new ArraySegmentReaderStream(s));
        }
    }
}
