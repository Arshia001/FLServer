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
    class GameState
    {
        public string[] CategoryNames { get; set; }
        //public List<HashSet<string>>[] PlayerAnswers { get; set; } // player no. -> turn no. -> answers //?? does bond support lists?
        //public List<uint>[] PlayerScores { get; set; }
        public Guid[] PlayerIDs { get; set; }
        //public DateTime[] TurnEndTimes { get; set; }
    }

    class Game : SaveStateOnDeactivateGrain<GameState>, IGame
    {
        class EndTurnTimerData
        {
            public int playerIndex;
            public IDisposable timerHandle;
        }


        readonly IDisposable[] turnTimers = new IDisposable[2]; //?? restore timers when activating
        GameLogic gameLogic;


        int NumJoinedPlayers => State.PlayerIDs == null || State.PlayerIDs.Length == 0 ? 0 :
            State.PlayerIDs[1] == Guid.Empty ? 1 : 2;


        public override Task OnActivateAsync()
        {
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

                gameLogic = new GameLogic(categories); //?? restore game state from grain state
            }

            return Task.CompletedTask;
        }

        int Index(Guid playerID) => State.PlayerIDs[0] == playerID ? 0 : (State.PlayerIDs[1] == playerID ? 1 : throw new Exception("Unknown player ID " + playerID.ToString()));

        public Task<byte> StartNew(Guid playerOneID)
        {
            if (GetStateInternal() != EGameState.New)
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

            gameLogic = new GameLogic(categories);

            State.PlayerIDs = new[] { playerOneID, Guid.Empty };

            return Task.FromResult((byte)State.CategoryNames.Length);
        }

        public Task<(Guid opponentID, byte numRounds)> AddSecondPlayer(Guid playerTwoID)
        {
            if (State.PlayerIDs[0] == playerTwoID)
                throw new Exception("Player cannot join game with self");

            State.PlayerIDs[1] = playerTwoID;

            return Task.FromResult((State.PlayerIDs[0], (byte)State.CategoryNames.Length));
        }

        public Task<(string category, TimeSpan turnTime)> StartRound(Guid id) // this could potentially be bad, timing-wise. Maybe the client should generate their own clock?
        {
            var index = Index(id);

            var turnTime = TimeSpan.FromSeconds(60); //?? config

            var result = gameLogic.StartRound(index, turnTime, out var category);

            if (!result.IsSuccess())
                throw new Exception("Failed to start round, resulting in " + result.ToString());

            var endTurnData = new EndTurnTimerData { playerIndex = index };
            var timerHandle = RegisterTimer(OnTurnEnded, endTurnData, turnTime, TimeSpan.MaxValue);
            endTurnData.timerHandle = timerHandle;

            return Task.FromResult((category, turnTime - TimeSpan.FromSeconds(10))); //?? config value for additional time per turn
        }

        Task OnTurnEnded(object state)
        {
            //?? notify other player about their next turn
            //?? no need to notify for start of round, if they see it, fine, if not, so what? they'll get it in a minute.

            var data = (EndTurnTimerData)state;
            data.timerHandle.Dispose();

            if (gameLogic.Finished)
            {
                //?? handle match result
            }

            return Task.CompletedTask;
        }

        public Task<(uint totalScore, sbyte thisWordScore, string corrected)> PlayWord(Guid id, string word)
        {
            var index = Index(id);

            gameLogic.PlayWord(index, word, out var total, out var thisWord, out var corrected);

            return Task.FromResult((total, thisWord, corrected));
        }

        EGameState GetStateInternal()
        {
            if (State.PlayerIDs == null || State.PlayerIDs.Length == 0)
                return EGameState.New;

            if (State.PlayerIDs[1] == Guid.Empty)
                return EGameState.WaitingForSecondPlayer;

            if (gameLogic.Finished)
                return EGameState.Finished;

            return EGameState.InProgress;
        }

        public Task<EGameState> GetState() => Task.FromResult(GetStateInternal());

        public Task<GameInfo> GetGameInfo(Guid playerID)
        {
            int index = Index(playerID);
            var turnsTaken = gameLogic.NumTurnsTakenBy(index);

            var result = new GameInfo
            {
                GameState = GetStateInternal(),
                OtherPlayerID = State.PlayerIDs[1 - index],
                NumRounds = (byte)gameLogic.Categories.Count,
                Categories = State.CategoryNames.Take(turnsTaken).ToList(),
                MyWordsPlayed = gameLogic.GetPlayerAnswers(index).Take(turnsTaken).Select(w => w.Select(ww => (ww.Item1, ww.Item2)).ToList()).ToList(),
                TheirWordsPlayed = gameLogic.GetPlayerAnswers(1 - index)?.Take(turnsTaken).Select(w => w.Select(ww => (ww.Item1, ww.Item2)).ToList()).ToList(), // don't return words for the round currently in progress
                MyTurn = gameLogic.Turn == index
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
