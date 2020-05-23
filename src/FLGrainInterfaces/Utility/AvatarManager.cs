using Bond;
using Bond.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLGrainInterfaces.Utility
{
    [Schema]
    public class AvatarPart : IEquatable<AvatarPart?>
    {
        public AvatarPart(AvatarPartType partType, ushort id)
        {
            PartType = partType;
            ID = id;
        }

        public AvatarPart() { }

        [Id(0)] public AvatarPartType PartType { get; set; }
        [Id(1)] public ushort ID { get; set; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as AvatarPart);
        }

        public bool Equals(AvatarPart? other)
        {
            return other is object &&
                   PartType == other.PartType &&
                   ID == other.ID;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PartType, ID);
        }

        public static bool operator ==(AvatarPart left, AvatarPart right) => left.Equals(right);

        public static bool operator !=(AvatarPart left, AvatarPart right) => !(left == right);
    }

    [Schema]
    public class AvatarManagerState : ICloneable<AvatarManagerState>
    {
        [Id(0)] public HashSet<AvatarPart> OwnedAvatarParts { get; set; } = new HashSet<AvatarPart>();
        [Id(1)] public Dictionary<AvatarPartType, ushort> ActiveAvatarParts { get; set; } = new Dictionary<AvatarPartType, ushort>();

        public AvatarManagerState Clone() =>
            new AvatarManagerState
            {
                OwnedAvatarParts = new HashSet<AvatarPart>(OwnedAvatarParts),
                ActiveAvatarParts = new Dictionary<AvatarPartType, ushort>(ActiveAvatarParts)
            };
    }

    public class AvatarManager
    {
        public static AvatarManager Deserialize(AvatarManagerState state) =>
            new AvatarManager(state.Clone());

        public static AvatarManager InitializeNew() =>
            new AvatarManager(new AvatarManagerState());

        private readonly AvatarManagerState state;

        AvatarManager(AvatarManagerState state) => this.state = state;

        public AvatarManagerState Serialize() => state.Clone();

        public IEnumerable<AvatarPart> GetOwnedParts() => state.OwnedAvatarParts;

        public IEnumerable<AvatarPartDTO> GetOwnedPartsAsDTO() => state.OwnedAvatarParts.Select(a => (AvatarPartDTO)a);

        public IReadOnlyDictionary<AvatarPartType, ushort> GetActiveParts() => state.ActiveAvatarParts;

        public bool HasPart(AvatarPart part) => state.OwnedAvatarParts.Contains(part);

        public bool AddOwnedPart(AvatarPart part) => state.OwnedAvatarParts.Add(part);

        public bool ActivatePart(AvatarPart part)
        {
            if (!HasPart(part))
                return false;

            ForceActivatePart(part);
            return true;
        }

        public void ForceActivatePart(AvatarPart part) => state.ActiveAvatarParts[part.PartType] = part.ID;

        public bool DeactivatePart(AvatarPartType partType) => state.ActiveAvatarParts.Remove(partType);

        public AvatarDTO GetAvatar() =>
            new AvatarDTO(state.ActiveAvatarParts.Select(kv => new AvatarPartDTO(kv.Key, kv.Value)));
    }
}
