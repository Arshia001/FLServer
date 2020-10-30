﻿using FLGrainInterfaces.Configuration;
using FLGrains;
using Microsoft.Extensions.DependencyInjection;
using Orleans.CodeGeneration;
using Orleans.Runtime;
using Orleans.Serialization;
using OrleansBondUtils;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FLTestClient
{
    class NullGrainRuntime : IGrainReferenceRuntime
    {
        public TGrainInterface Convert<TGrainInterface>(IAddressable grain)
        {
            return
                (TGrainInterface)
                typeof(TGrainInterface)
                .Assembly
                .GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(TGrainInterface)) && t.BaseType == typeof(GrainReference))
                .First()
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, new[] { typeof(GrainReference) }, Array.Empty<ParameterModifier>())
                .Invoke(new object[] { grain });
        }

        public object Convert(IAddressable grain, Type interfaceType)
        {
            return grain;
        }

        public Task<T> InvokeMethodAsync<T>(GrainReference reference, int methodId, object[] arguments, InvokeMethodOptions options, SiloAddress silo)
        {
            throw new NotImplementedException();
        }

        public void InvokeOneWayMethod(GrainReference reference, int methodId, object[] arguments, InvokeMethodOptions options, SiloAddress silo)
        {
            throw new NotImplementedException();
        }
    }

    class NullGrainReferenceConverter : IGrainReferenceConverter
    {
        IGrainReferenceRuntime runtime = new NullGrainRuntime();

        public GrainReference GetGrainFromKeyInfo(GrainReferenceKeyInfo keyInfo)
        {
            return (GrainReference)typeof(GrainReference).GetMethod("FromKeyInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { keyInfo, runtime });
        }

        public GrainReference GetGrainFromKeyString(string key)
        {
            return (GrainReference)typeof(GrainReference).GetMethod("FromKeyString", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { key, runtime });
        }
    }

    //    [Schema]
    //    public class StatisticWithParameter : IEquatable<StatisticWithParameter>
    //    {
    //        [Obsolete] public StatisticWithParameter() { }

    //        public StatisticWithParameter(Statistics statistic, int parameter)
    //        {
    //            Statistic = statistic;
    //            Parameter = parameter;
    //        }

    //        [Id(0)] public Statistics Statistic { get; set; }
    //        [Id(1)] public int Parameter { get; set; }

    //        public override bool Equals(object obj)
    //        {
    //            return Equals(obj as StatisticWithParameter);
    //        }

    //        public bool Equals(StatisticWithParameter other)
    //        {
    //            return other != null &&
    //                   Statistic == other.Statistic &&
    //                   Parameter == other.Parameter;
    //        }

    //        public override int GetHashCode()
    //        {
    //            return HashCode.Combine(Statistic, Parameter);
    //        }
    //    }

    //    [Schema, BondSerializationTag("#pppppp")]
    //    public class PlayerState
    //    {
    //        [Id(0)]
    //        public List<IGame> ActiveGames { get; private set; }

    //        [Id(1)]
    //        public string Name { get; set; }

    //        [Id(2)]
    //        public uint Level { get; set; }

    //        [Id(3)]
    //        public uint XP { get; set; }

    //        [Id(4)]
    //        public uint NumRoundsWonForReward { get; set; }

    //        [Id(5)]
    //        public DateTime LastRoundWinRewardTakeTime { get; set; }

    //        [Id(6)]
    //        public uint Score { get; set; }

    //        [Id(7)]
    //        public ulong Gold { get; set; }

    //        [Id(8)]
    //        public List<IGame> PastGames { get; private set; }

    //        [Id(9)]
    //        public DateTime InfinitePlayEndTime { get; set; }

    //        [Id(10)]
    //        public HashSet<string> OwnedCategoryAnswers { get; set; }

    //        [Id(11)]
    //        public Dictionary<StatisticWithParameter, ulong> StatisticsValues { get; set; }

    //        [Id(12)]
    //        public byte[] PasswordSalt { get; set; }

    //        [Id(13)]
    //        public byte[] PasswordHash { get; set; }

    //        [Id(14)]
    //        public string Email { get; set; }

    //        [Id(15)]
    //        public string Username { get; set; }
    //    }

    //    [Schema, BondSerializationTag("#gggggg")]
    //    public class GameGrain_State : IOnDeserializedHandler
    //    {
    //        [Id(0)]
    //        public SerializedGameData GameData { get; set; }

    //        [Id(1)]
    //        public Guid[] PlayerIDs { get; set; }

    //        [Id(2)]
    //        public int[] LastProcessedEndTurns { get; set; }

    //        [Id(3)]
    //        public int GroupChooser { get; set; } = -1;

    //        [Id(4)]
    //        public List<ushort> GroupChoices { get; set; }

    //        public void OnDeserialized()
    //        {
    //            if (PlayerIDs == null)
    //                PlayerIDs = Array.Empty<Guid>();
    //            if (LastProcessedEndTurns == null)
    //                LastProcessedEndTurns = new[] { -1, -1 };
    //        }
    //    }

    //    [Schema]
    //    class MatchMakingEntry
    //    {
    //        [Id(0)]
    //        public IGame? Game { get; private set; }
    //        [Id(1)]
    //        public uint Score { get; private set; }
    //        [Id(2)]
    //        public uint Level { get; private set; }
    //        [Id(3)]
    //        public Guid FirstPlayerID { get; private set; }

    //        [Obsolete("For deserialization only")] public MatchMakingEntry() { }

    //        public MatchMakingEntry(IGame game, uint score, uint level, Guid firstPlayerID)
    //        {
    //            Game = game;
    //            Score = score;
    //            Level = level;
    //            FirstPlayerID = firstPlayerID;
    //        }
    //    }

    //    [Schema, BondSerializationTag("#mmmmmmmm")]
    //    class MatchMakingGrainState
    //    {
    //        [Id(0)]
    //        public List<MatchMakingEntry>? Entries { get; private set; }
    //    }

    class Program
    {
        //        static Guid ID(int i) => new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        static void Main(string[] args)
        {
            var svc = new ServiceCollection();
            ServiceConfiguration.ConfigureGameServer(svc, new SystemSettings(@"{""ConnectionString"":""Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy""}", ""));
            svc.AddSingleton<IGrainReferenceConverter, NullGrainReferenceConverter>();
            var provider = svc.BuildServiceProvider();

            BondSerializationUtil.Initialize(provider);

            var data = StringToByteArray(
                //File.ReadAllText(@"C:\Users\Arshia\source\repos\fl\mmstate.txt")
                "0b0a010b0b05120112a906340646062f0647060c202a063106cc06460620002d06cc0648062706460627062a0612001200120012002b0b020b010a000b004b0b0205010005006b1102aca6e890cacebcd81100b18080cea6a532002b0b020e10c852e6bb1e3d4c41928cfff707c49fc40e10000000000000000000000000000000004b10020101ab10020000d00602cb0710020000cb081002000000"
                );

            //var session = CassandraSessionFactory.CreateSession("Contact Point=localhost;KeySpace=fl_server_dev;Compression=Snappy").Result;
            //var statement = session.Prepare("update storage set data = :? where grain_type = '#mm' and grain_id = 0x00; ");
            //var x = session.Execute(statement.Bind(data));
            //foreach (var xx in x)
            //{
            //    Console.WriteLine(xx);
            //}

            var obj = (GameGrainState)BondSerializer.Deserialize(typeof(GameGrainState), new MemoryStream(data));

            //var grouped = obj.Entries.GroupBy(e => e.Game.GetPrimaryKey().ToString() + e.FirstPlayerID.ToString());
            //var grouped2 = obj.Entries.GroupBy(e => e.Game.GetPrimaryKey().ToString());

            Console.WriteLine("Done");
        }
    }
}
