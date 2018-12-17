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
        //public List<HashSet<string>>[] PlayerAnswers { get; set; } // player no. -> turn no. -> answers //?? does bond support lists?
        //public List<uint>[] PlayerScores { get; set; }
        public Guid[] PlayerIDs { get; set; }
        public int[] LastProcessedEndTurns { get; set; } = new int[2]; //?? use to reprocess turn end notifications in case grain goes down
        //public DateTime[] TurnEndTimes { get; set; }
    }

    class Game : SaveStateOnDeactivateGrain<GameGrain_State>, IGame
    {
        class EndRoundTimerData
        {
            public int playerIndex;
            public int roundIndex;
            public IDisposable timerHandle;
        }


        readonly IDisposable[] turnTimers = new IDisposable[2]; //?? restore timers when activating
        GameLogicServer gameLogic;


        int NumJoinedPlayers => State.PlayerIDs == null || State.PlayerIDs.Length == 0 ? 0 :
            State.PlayerIDs[1] == Guid.Empty ? 1 : 2;


        public override Task OnActivateAsync()
        {
            //?? restore game state from grain state
            //?? register turn end timers if any
            //?? record active turn end timers in state so we can register even if turn time already passed
            if (State.CategoryNames != null && State.CategoryNames.Length > 0)
            {
                var categories = new List<WordCategory>();
                // foreach (var category in State.CategoryNames) //?? fetch details from said repository
                categories.Add(new WordCategory //?? fetch details from said repository
                {
                    CategoryName = State.CategoryNames[0],
                    WordsAndScores = new Dictionary<string, byte>
                    {
                        { "hello", 1 },
                        { "nice to meet you", 2 },
                        { "how are you", 3 }
                    },
                    WordCorrections = new Dictionary<string, string>
                    {
                        { "hallo", "hello" }
                    }
                });
                if (State.CategoryNames.Length > 1)
                    categories.Add(new WordCategory //?? fetch details from said repository
                    {
                        CategoryName = State.CategoryNames[1],
                        WordsAndScores = new Dictionary<string, byte>
                        {
                            { "bmw", 1 },
                            { "audi", 2 },
                            { "ikco", 3 }
                        },
                        WordCorrections = new Dictionary<string, string>
                        {
                            { "iran khodro", "ikco" }
                        }
                    });
                if (State.CategoryNames.Length > 2)
                    categories.Add(new WordCategory //?? fetch details from said repository
                    {
                        CategoryName = State.CategoryNames[2],
                        WordsAndScores = new Dictionary<string, byte>
                        {
                            { "orange", 1 },
                            { "banana", 2 },
                            { "watermelon", 3 }
                        },
                        WordCorrections = new Dictionary<string, string>
                        {
                            { "orage", "orange" }
                        }
                    });

                gameLogic = new GameLogicServer(categories); //?? restore game state from grain state
            }

            return Task.CompletedTask;
        }

        int Index(Guid playerID) => State.PlayerIDs[0] == playerID ? 0 : (State.PlayerIDs[1] == playerID ? 1 : throw new Exception("Unknown player ID " + playerID.ToString()));

        public Task<byte> StartNew(Guid playerOneID)
        {
            if (GetStateInternal() != GameState.New)
                throw new Exception("Game already started");

            State.CategoryNames = new[] { "Greetings", "Cars", "Fruits" }; //?? fetch by random from some central repository

            var categories = new List<WordCategory>();
            categories.Add(new WordCategory //?? fetch details from said repository
            {
                CategoryName = State.CategoryNames[0],
                WordsAndScores = new Dictionary<string, byte>
                {
                    { "hello", 1 },
                    { "greetings", 2 },
                    { "how are you", 3 }
                },
                WordCorrections = new Dictionary<string, string>
                {
                    { "hallo", "hello" }
                }
            });
            categories.Add(new WordCategory //?? fetch details from said repository
            {
                CategoryName = State.CategoryNames[1],
                WordsAndScores = new Dictionary<string, byte>
                {
                    { "bmw", 1 },
                    { "audi", 2 },
                    { "ikco", 3 }
                },
                WordCorrections = new Dictionary<string, string>
                {
                    { "iran khodro", "ikco" }
                }
            });
            categories.Add(new WordCategory //?? fetch details from said repository
            {
                CategoryName = State.CategoryNames[2],
                WordsAndScores = new Dictionary<string, byte>
                {
                    { "orange", 1 },
                    { "banana", 2 },
                    { "watermelon", 3 }
                },
                WordCorrections = new Dictionary<string, string>
                {
                    { "orage", "orange" }
                }
            });

            gameLogic = new GameLogicServer(categories);

            State.PlayerIDs = new[] { playerOneID, Guid.Empty };

            return Task.FromResult((byte)State.CategoryNames.Length);
        }

        public async Task<(Guid opponentID, byte numRounds)> AddSecondPlayer(Guid playerTwoID)
        {
            if (State.PlayerIDs[0] == playerTwoID)
                throw new Exception("Player cannot join game with self");

            State.PlayerIDs[1] = playerTwoID;

            await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentJoined(State.PlayerIDs[0], this.GetPrimaryKey(), playerTwoID);

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

        async Task OnTurnEnded(object state)
        {
            var data = (EndRoundTimerData)state;
            data.timerHandle.Dispose();

            State.LastProcessedEndTurns[data.playerIndex] = data.roundIndex;

            var myID = this.GetPrimaryKey();
            await GrainFactory.GetGrain<IGameEndPoint>(0).SendOpponentTurnEnded(State.PlayerIDs[1 - data.playerIndex], myID, (uint)data.roundIndex,
                gameLogic.PlayerFinishedTurn(1 - data.playerIndex, data.roundIndex) ? gameLogic.GetPlayerAnswers(1 - data.playerIndex, data.roundIndex) : Enumerable.Empty<WordScorePair>());

            if (gameLogic.Finished)
            {
                var wins0 = gameLogic.GetNumRoundsWon(0);
                var wins1 = gameLogic.GetNumRoundsWon(1);
                await Task.WhenAll(GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(State.PlayerIDs[0], myID, wins0, wins1),
                    GrainFactory.GetGrain<IGameEndPoint>(0).SendGameEnded(State.PlayerIDs[1], myID, wins1, wins0));

                await ClearStateAsync();
                DeactivateOnIdle();
            }
        }

        public Task<(byte wordScore, string corrected)> PlayWord(Guid id, string word)
        {
            var index = Index(id);

            gameLogic.PlayWord(index, word, out var wordScore, out var corrected);

            return Task.FromResult((wordScore, corrected));
        }

        public Task<IEnumerable<WordScorePair>> EndRound(Guid playerID)
        {
            int index = Index(playerID);

            gameLogic.ForceEndTurn(index);

            var turnIndex = gameLogic.NumTurnsTakenBy(index) - 1;

            if (gameLogic.PlayerFinishedTurn(1 - index, turnIndex))
                return Task.FromResult(gameLogic.GetPlayerAnswers(1 - index, turnIndex).AsEnumerable());
            else
                return Task.FromResult(Enumerable.Empty<WordScorePair>());
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

        public Task<GameInfo> GetGameInfo(Guid playerID)
        {
            if (GetStateInternal() == GameState.New)
                throw new Exception("Game not in progress");

            int index = Index(playerID);
            var turnsTakenInclCurrent = gameLogic.NumTurnsTakenByIncludingCurrent(index);
            var turnsTaken = gameLogic.NumTurnsTakenBy(index);

            var result = new GameInfo
            {
                OtherPlayerID = State.PlayerIDs[1 - index],
                NumRounds = (byte)gameLogic.Categories.Count,
                Categories = State.CategoryNames.Take(turnsTakenInclCurrent).ToList(),
                MyWordsPlayed = gameLogic.GetPlayerAnswers(index).Take(turnsTakenInclCurrent).ToList(),
                TheirWordsPlayed = gameLogic.GetPlayerAnswers(1 - index)?.Take(turnsTaken).ToList(), // don't return words for the round currently in progress
                MyTurnEndTime = gameLogic.GetTurnEndTime(index)
            };

            return Task.FromResult(result);
        }

        public Task<SimplifiedGameInfo> GetSimplifiedGameInfo(Guid playerID)
        {
            int index = Index(playerID);
            var turnsTaken = gameLogic.NumTurnsTakenBy(index);

            var result = new SimplifiedGameInfo
            {
                GameID = this.GetPrimaryKey(),
                GameState = GetStateInternal(),
                OtherPlayerID = State.PlayerIDs[1 - index],
                MyTurn = gameLogic.Turn == index,
                MyScore = gameLogic.GetNumRoundsWon(index),
                TheirScore = gameLogic.GetNumRoundsWon(1 - index)
            };

            return Task.FromResult(result);
        }
    }
}
