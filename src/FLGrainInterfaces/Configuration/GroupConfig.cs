namespace FLGrainInterfaces.Configuration
{
    public class GroupConfig
    {
        public ushort ID { get; }
        public string Name { get; }

        public GroupConfig(ushort id, string name)
        {
            ID = id;
            Name = name;
        }
    }
}
