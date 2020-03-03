using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace FLGrainInterfaces.Configuration
{
    public class GoldPackConfig
    {
        public string? Sku { get; private set; }
        public uint NumGold { get; private set; }
        public string? Title { get; private set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public GoldPackTag Tag { get; private set; }
    }
}
