using FLGrainInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrains.ServiceInterfaces
{
    class Bot
    {
        public Bot(Guid id, string name, AvatarDTO avatar)
        {
            ID = id;
            Name = name;
            Avatar = avatar;
        }

        public Guid ID { get; }
        public string Name { get; }
        public AvatarDTO Avatar { get; }
    }

    interface IBotDatabase
    {
        Bot GetRandom();
        Bot? GetByID(Guid id);
        bool IsBotID(Guid id);
    }
}
