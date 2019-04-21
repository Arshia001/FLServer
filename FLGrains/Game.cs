using FLGameLogic;
using FLGrainInterfaces;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLGrains
{
    //?? the current scheme doesn't support immediate lookup of score details for a round
    class GameGrain_State
    {
        public string[] CategoryNames { get; set; }
        public Guid[] PlayerIDs { get; set; }
        public int[] LastProcessedEndTurns { get; set; } = new[] { -1, -1 }; //?? use to reprocess turn end notifications in case grain goes down
    }

    class Game : Grain<GameGrain_State> /*SaveStateOnDeactivateGrain<GameGrain_State>*/, IGame
    {
        class EndRoundTimerData
        {
            public int playerIndex;
            public int roundIndex;
            public IDisposable timerHandle;
        }


        readonly IDisposable[] turnTimers = new IDisposable[2]; //?? restore timers when activating
        GameLogicServer gameLogic;
        IConfigReader configReader;


        int NumJoinedPlayers => State.PlayerIDs == null || State.PlayerIDs.Length == 0 ? 0 :
            State.PlayerIDs[1] == Guid.Empty ? 1 : 2;


        public Game(IConfigReader configReader)
        {
            this.configReader = configReader;
        }

        public override Task OnActivateAsync()
        {
            //?? restore game state from grain state
            //?? register turn end timers if any
            //?? record active turn end timers in state so we can register even if turn time already passed
            if (State.CategoryNames != null && State.CategoryNames.Length > 0)
            {
                var config = configReader.Config;
                gameLogic = new GameLogicServer(State.CategoryNames.Select(n => config.CategoriesByName[n])); //?? restore game state from grain state
            }

            DelayDeactivation(TimeSpan.FromDays(20)); //?? store state in DB -.-

            return Task.CompletedTask;
        }

        int Index(Guid playerID) => State.PlayerIDs[0] == playerID ? 0 : (State.PlayerIDs[1] == playerID ? 1 : throw new Exception("Unknown player ID " + playerID.ToString()));

        public Task<byte> StartNew(Guid playerOneID)
        {
            if (GetStateInternal() != GameState.New)
                throw new Exception("Game already started");

            var config = configReader.Config;
            var randomIndices = new HashSet<int>();
            var random = new Random();

            while (randomIndices.Count < 3) //?? number of rounds as config
                randomIndices.Add(random.Next(config.Categories.Count));

            var categories = randomIndices.Select(i => config.CategoriesAsGameLogicFormat[i]);
            State.CategoryNames = categories.Select(c => c.CategoryName).ToArray();

            gameLogic = new GameLogicServer(categories);

            State.PlayerIDs = new[] { playerOneID, Guid.Empty };

            return Task.FromResult((byte)State.CategoryNames.Length);
        }

        public async Task<(Guid opponentID, byte numRounds)> AddSecondPlayer(PlayerInfo playerTwo)
        {
            if (State.PlayerIDs[0] == playerTwo.ID)
                throw new Exception("Player cannot join game with self");

            State.PlayerIDs[1] = playerTwo.ID;

            await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentJoined(State.PlayerIDs[0], this.GetPrimaryKey(), playerTwo);

            return (State.PlayerIDs[0], (byte)State.CategoryNames.Length);
        }

        public Task<(string category, TimeSpan turnTime)> StartRound(Guid id) // this could potentially be bad, timing-wise. Maybe the client should generate their own clock?
        {
            var index = Index(id);

            var turnTime = TimeSpan.FromSeconds(60); //?? config

            var result = gameLogic.StartRound(index, turnTime, out var category);

            if (!result.IsSuccess())
                throw new Exception("Failed to start round, resulting in " + result.ToString());

            var endRoundData = new EndRoundTimerData { playerIndex = index, roundIndex = gameLogic.NumTurnsTakenByIncludingCurrent(index) - 1 };
            var timerHandle = RegisterTimer(OnTurnEnded, endRoundData, turnTime, TimeSpan.MaxValue);
            endRoundData.timerHandle = timerHandle;

            return Task.FromResult((category, turnTime - TimeSpan.FromSeconds(10))); //?? config value for additional time per turn
        }

        Task OnTurnEnded(object state)
        {
            var data = (EndRoundTimerData)state;
            data.timerHandle.Dispose();

            return HandleEndTurn(data.playerIndex, data.roundIndex);
        }

        private async Task HandleEndTurn(int playerIndex, int roundIndex)
        {
            if (State.LastProcessedEndTurns[playerIndex] >= roundIndex)
                return;

            State.LastProcessedEndTurns[playerIndex] = roundIndex;

            var myID = this.GetPrimaryKey();
            await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentTurnEnded(State.PlayerIDs[1 - playerIndex], myID, (byte)roundIndex,
                gameLogic.PlayerFinishedTurn(1 - playerIndex, roundIndex) ? gameLogic.GetPlayerAnswers(playerIndex, roundIndex).Select(w => (WordScorePairDTO)w).ToList() : null);

            if (gameLogic.Finished)
            {
                var wins0 = gameLogic.GetNumRoundsWon(0);
                var wins1 = gameLogic.GetNumRoundsWon(1);
                await Task.WhenAll(GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(State.PlayerIDs[0], myID, wins0, wins1),
                    GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(State.PlayerIDs[1], myID, wins1, wins0));

                //?? rewards, etc.?

                // await ClearStateAsync(); // keep game history (separately)
                DeactivateOnIdle();
            }
        }

        public async Task<(byte wordScore, string corrected)> PlayWord(Guid id, string word)
        {
            var index = Index(id);

            var result = await gameLogic.PlayWord(index, word, GetWordScore);

            if (result.corrected != null && result.category != null && result.score > 0)
                GrainFactory.GetGrain<IWordUsageAggregationWorker>(result.category.CategoryName).AddDelta(result.corrected).Ignore();

            return (result.score, result.corrected);
        }

        Task<byte> GetWordScore(WordCategory category, string word) => 
            GrainFactory.GetGrain<IWordUsageAggregatorCache>(category.CategoryName).GetScore(word);

        public async Task<Immutable<IEnumerable<WordScorePair>>> EndRound(Guid playerID)
        {
            int index = Index(playerID);

            gameLogic.ForceEndTurn(index);

            var turnIndex = gameLogic.NumTurnsTakenBy(index) - 1;

            await HandleEndTurn(index, turnIndex);

            if (gameLogic.PlayerFinishedTurn(1 - index, turnIndex))
                return gameLogic.GetPlayerAnswers(1 - index, turnIndex).AsEnumerable().AsImmutable();
            else
                return default(IEnumerable<WordScorePair>).AsImmutable();
        }

        GameState GetStateInternal()
        {
            if (State.PlayerIDs == null || State.PlayerIDs.Length == 0)
                return GameState.New;

            if (State.PlayerIDs[1] == Guid.Empty)
                return GameState.WaitingForSecondPlayer;

            if (gameLogic.Finished)
                return GameState.Finished;

            return GameState.InProgress;
        }

        public Task<GameState> GetState() => Task.FromResult(GetStateInternal());

        public async Task<Immutable<GameInfo>> GetGameInfo(Guid playerID)
        {
            if (GetStateInternal() == GameState.New)
                throw new Exception("Game not in progress");

            int index = Index(playerID);
            var turnsTakenInclCurrent = gameLogic.NumTurnsTakenByIncludingCurrent(index);
            var turnsTaken = gameLogic.NumTurnsTakenBy(index);

            var result = new GameInfo
            {
                OtherPlayerInfo = (await PlayerInfoUtil.GetForPlayerID(GrainFactory, State.PlayerIDs[1 - index])).Value,
                NumRounds = (byte)gameLogic.Categories.Count,
                Categories = State.CategoryNames.Take(turnsTakenInclCurrent).ToList(),
                MyWordsPlayed = gameLogic.GetPlayerAnswers(index).Take(turnsTakenInclCurrent).Select(ws => ws.Select(w => (WordScorePairDTO)w).ToList()).ToList(),
                TheirWordsPlayed = gameLogic.GetPlayerAnswers(1 - index)?.Take(turnsTaken).Select(ws => ws.Select(w => (WordScorePairDTO)w).ToList()).ToList(), // don't return words for the round currently in progress
                MyTurnEndTime = gameLogic.GetTurnEndTime(index),
                MyTurnFirst = gameLogic.FirstTurn == index,
                NumTurnsTakenByOpponent = (byte)gameLogic.NumTurnsTakenByIncludingCurrent(1 - index)
            };

            return result.AsImmutable();
        }

        public async Task<Immutable<SimplifiedGameInfo>> GetSimplifiedGameInfo(Guid playerID)
        {
            int index = Index(playerID);
            var turnsTaken = gameLogic.NumTurnsTakenBy(index);

            var result = new SimplifiedGameInfo
            {
                GameID = this.GetPrimaryKey(),
                GameState = GetStateInternal(),
                OtherPlayerName = (await PlayerInfoUtil.GetForPlayerID(GrainFactory, State.PlayerIDs[1 - index])).Value.Name,
                MyTurn = gameLogic.Turn == index,
                MyScore = gameLogic.GetNumRoundsWon(index),
                TheirScore = gameLogic.GetNumRoundsWon(1 - index)
            };

            return result.AsImmutable();
        }

        public Task<bool> WasFirstTurnPlayed() //?? remove - see comment on interface 
        {
            return Task.FromResult(gameLogic.PlayerFinishedTurn(0, 0));
        }
    }
}
