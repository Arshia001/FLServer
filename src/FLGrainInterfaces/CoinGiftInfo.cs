using System;
using System.Collections.Generic;
using System.Text;
using Bond;

namespace FLGrainInterfaces
{
    [Schema]
    public class CoinGiftInfo
    {
        public CoinGiftInfo(CoinGiftSubject subject, uint count, string? description, DateTime? expiryTime,
            string? extraData1, string? extraData2, string? extraData3, string? extraData4)
        {
            GiftID = Guid.NewGuid();
            Subject = subject;
            Count = count;
            Description = description;
            ExpiryTime = expiryTime;
            ExtraData1 = extraData1;
            ExtraData2 = extraData2;
            ExtraData3 = extraData3;
            ExtraData4 = extraData4;
        }

        public CoinGiftInfo() { }

        [Id(0)]
        public Guid GiftID { get; private set; }

        [Id(1)]
        public CoinGiftSubject Subject { get; private set; }

        [Id(2)]
        public uint Count { get; private set; }

        [Id(3)]
        public string? Description { get; private set; }

        [Id(4)]
        public DateTime? ExpiryTime { get; private set; }

        [Id(5)]
        public string? ExtraData1 { get; private set; }

        [Id(6)]
        public string? ExtraData2 { get; private set; }

        [Id(7)]
        public string? ExtraData3 { get; private set; }

        [Id(8)]
        public string? ExtraData4 { get; private set; }
    }
}
