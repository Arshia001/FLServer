using FLGrainInterfaces;
using FLGrainInterfaces.Configuration;
using FLGrains.ServiceInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGrains.Services
{
    class BotDatabase : IBotDatabase
    {
        static readonly byte[] guidZeroBytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };

        readonly IConfigReader configReader;

        int? lastObservedConfigVersion;
        readonly Dictionary<Guid, Bot> bots = new Dictionary<Guid, Bot>();

        public BotDatabase(IConfigReader configReader) => this.configReader = configReader;

        void RefreshBotsIfNeeded()
        {
            var config = configReader.Config;
            if (lastObservedConfigVersion < 0 || lastObservedConfigVersion != config.Version)
            {
                lock (bots)
                {
                    bots.Clear();
                    foreach (var botConfig in config.Bots)
                    {
                        var id = new Guid(botConfig.ID, 0, 0, guidZeroBytes);

                        var parts = new List<AvatarPartDTO>(5)
                        {
                            new AvatarPartDTO(AvatarPartType.HeadShape, botConfig.AvatarHeadShape),
                            new AvatarPartDTO(AvatarPartType.Eyes, botConfig.AvatarEyes),
                            new AvatarPartDTO(AvatarPartType.Mouth, botConfig.AvatarMouth)
                        };
                        if (botConfig.AvatarHair.HasValue)
                            parts.Add(new AvatarPartDTO(AvatarPartType.Hair, botConfig.AvatarHair.Value));
                        if (botConfig.AvatarGlasses.HasValue)
                            parts.Add(new AvatarPartDTO(AvatarPartType.Glasses, botConfig.AvatarGlasses.Value));

                        bots[id] = new Bot(id, botConfig.Name, new AvatarDTO(parts));
                    }

                    lastObservedConfigVersion = config.Version;
                }
            }
        }

        public Bot? GetByID(Guid id)
        {
            RefreshBotsIfNeeded();
            return bots.TryGetValue(id, out var result) ? result : null;
        }

        public Bot GetRandom()
        {
            RefreshBotsIfNeeded();
            return bots.Values.Skip(RandomHelper.GetInt32(bots.Count)).First();
        }

        public bool IsBotID(Guid id)
        {
            RefreshBotsIfNeeded();
            return bots.ContainsKey(id);
        }
    }
}
