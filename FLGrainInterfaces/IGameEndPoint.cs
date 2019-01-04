using FLGameLogic;
using LightMessage.OrleansUtils.GrainInterfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FLGrainInterfaces
{
    public interface IGameEndPoint : IEndPointGrain
    {
        Task SendOpponentJoined(Guid playerID, Guid gameID, PlayerInfo opponent);
        Task SendOpponentTurnEnded(Guid playerID, Guid gameID, uint roundNumber, IEnumerable<WordScorePair> wordsPlayed);
        Task SendGameEnded(Guid playerID, Guid gameID, uint myScore, uint theirScore);
    }
}
