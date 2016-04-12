using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace HanabiCardGame
{
    public enum GameStateID
    {
        WaitingToStart,
        Playing,
        EndSession
    }
    //Base class for all states game can be in
    public abstract class GameState
    {
        //Reference to state machine that stores and operates on the state
        protected MainGameLoopFSM InvokingFSM { get; set; }
        //This method is the "Main" method for the GameState
        public abstract void Action(GameStateArgs gmStateArgs);
    }

    public class GameStateArgs
    {
        public string StringData { get; set; }
    }

    public class WaitingToStartState : GameState
    {
        Regex _startGameCommandRegEx = new Regex(@"^Start new game with deck(\s[RGBYW][1-5]){11,}$");

        public WaitingToStartState(MainGameLoopFSM invokingFSM)
        {
            this.InvokingFSM = invokingFSM;
        }

        public override void Action(GameStateArgs gmStateArgs)
        {
            string input;

            //Check if WaitingtoStart state was invoked with input string argument
            if (gmStateArgs != null)
                input = gmStateArgs.StringData;
            else
                input = Console.ReadLine();

            if (input != null && _startGameCommandRegEx.IsMatch(input))
            {

                GameStateArgs gStateArgs = new GameStateArgs();
                gStateArgs.StringData = input;

                InvokingFSM.PerformTransitionTo(GameStateID.Playing, gStateArgs);
            }
            else
                InvokingFSM.PerformTransitionTo(GameStateID.EndSession, null);
        }
    }

    public class Card
    {
        public char Color { get; set; }
        public int Rank { get; set; }
        public bool PlayerKnowsColor { get; set; }
        public bool PlayerKnowsRank { get; set; }


        public Card(char color, int rank)
        {
            Color = color;
            Rank = rank;
            PlayerKnowsColor = false;
            PlayerKnowsRank = false;
        }

        public override string ToString()
        {
            return Color.ToString() + Rank.ToString();
        }
    }

    public class Player
    {
        public List<Card> CurrentHand { get; set; }

        public Player(IEnumerable<string> playerHand)
        {
            CurrentHand = new List<Card>();

            foreach(string card in playerHand)
            {
                CurrentHand.Add(new Card(card[0],card[1] - '0'));
            }

        }

        public void RemoveCardFromHand(int cardIndex)
        {
            CurrentHand.RemoveAt(cardIndex);
        }

        public void AddCardToHand(string card)
        {
            CurrentHand.Add(new Card(card[0],card[1] - '0'));
        }

        public bool TryRememberColorForCards(char color,IEnumerable<int> cardsIndexes)
        {
            foreach (int cardIndex in cardsIndexes)
            {
                if (CurrentHand[cardIndex].Color == color)
                    CurrentHand[cardIndex].PlayerKnowsColor = true;
                else return false;
            }
            return true;
        }

        public bool TryRememberRankForCards(int rank,IEnumerable<int> cardsIndexes)
        {
            foreach (int cardIndex in cardsIndexes.Skip(1))
            {
                if (CurrentHand[cardIndex].Rank == rank)
                    CurrentHand[cardIndex].PlayerKnowsRank = true;
                else return false;
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder playersHandSBuilder = new StringBuilder();
            foreach(Card card in CurrentHand)
            {
                playersHandSBuilder.Append(" " + card.ToString());
            }
            return playersHandSBuilder.ToString();
        }
    }

    public class PlayingState : GameState
    {
        List<string> _currentDeck;
        Dictionary<char, List<Card>> _currentTable;
        List<Card> _discard;

        Player _currentPlayer;
        Player _nextPlayer;

        int _turn = 0;
        int _score = 0;

        bool _endGame = false;

        Regex _playCardRegEx = new Regex(@"^Play card [0-4]$");
        Regex _dropCardRegEx = new Regex(@"^Drop card [0-4]$");
        Regex _tellColorRegEx = new Regex(@"^Tell color (Red|Blue|Yellow|Green|White) for cards(\s[0-4]){1,5}$");
        Regex _tellRankRegEx = new Regex(@"^Tell rank [1-5] for cards(\s[0-4]){1,5}$");

        Regex _startNewGameRegEx = new Regex(@"^Start new game with deck(\s[RGBYW][1-5]){11,}$");


        public PlayingState(MainGameLoopFSM invokingFSM)
        {
            this.InvokingFSM = invokingFSM;
        }

        public override void Action(GameStateArgs gmStateArgs)
        {
            //Extract all card signatures from input string
            string allCardsSignatures = gmStateArgs.StringData.Substring(25);

            PrepareNewGame(allCardsSignatures);
            DisplayCurrentGameInformation();
            while (!_endGame)
            {
                string input = Console.ReadLine();

                if (CheckIfInputIsValid(input))
                {
                    _turn += 1;

                    switch (input.Substring(0, 9))
                    {
                        case "Play card":
                                PlayCard(input);
                            break;
                        case "Drop card":
                                DropCard(input);
                            break;
                        case "Tell colo":
                                TellColor(input);
                            break;
                        case "Tell rank":
                                TellRank(input);
                            break;

                    }
                    SwapPlayers();
                    DisplayCurrentGameInformation();
                }
                //If input is "Start new game" command
                else if (_startNewGameRegEx.IsMatch(input))
                {
                    //We will take this "Start new game" command
                    //and pass it to WaitingToStartState
                    GameStateArgs argsForWaitingToStartState = new GameStateArgs();
                    argsForWaitingToStartState.StringData = input;
                    RestartGame(argsForWaitingToStartState);
                }
                else
                    StopGameSession();
            }
        }

        void PrepareNewGame(string allCardsSignatures)
        {

            string[] allCardsArr = allCardsSignatures.Split(' ');

            _currentPlayer = new Player(allCardsArr.Take(5));
            _nextPlayer = new Player(allCardsArr.Skip(5).Take(5));

            _currentDeck = new List<string>(allCardsArr.Skip(10));

            _currentTable = new Dictionary<char, List<Card>>
            {
                {'R',new List<Card>()},
                {'G',new List<Card>()},
                {'B',new List<Card>()},
                {'W',new List<Card>()},
                {'Y',new List<Card>()}
            };

            _discard = new List<Card>();

            _turn = 0;
            _score = 0;

            _endGame = false;
        }

        void DisplayCurrentGameInformation()
        {
            Console.WriteLine("Turn: {0}, cards on table: {1}, game is finished: {2}", _turn, _score, _endGame);
            Console.WriteLine("Current Player's hand:{0}", _currentPlayer.ToString());
            Console.WriteLine("   Next Player's hand:{0}", _nextPlayer.ToString());
            Console.WriteLine("        Current table:{0}", GetCurrentTableAsString());
        }

        string GetCurrentTableAsString()
        {
            StringBuilder currentTableSBuilder = new StringBuilder();

            foreach (char color in _currentTable.Keys)
            {
                currentTableSBuilder.Append(" " + color + _currentTable[color].Count);
            }

            return currentTableSBuilder.ToString();
        }

        bool CheckIfInputIsValid(string input)
        {
            //We are going to whitelist input, by checking here, 
            //if any of the allowed command RegEx
            //match with input, if not than input is invalid
            return _playCardRegEx.IsMatch(input) ||
                   _dropCardRegEx.IsMatch(input) ||
                   _tellColorRegEx.IsMatch(input) ||
                   _tellRankRegEx.IsMatch(input);

        }

        void PlayCard(string input)
        {
            int indexOfCardInPlayersHand = input[10] - '0';

            Card cardToPlay = _currentPlayer.CurrentHand[indexOfCardInPlayersHand];

            if (CheckIfCardCanBeAddedToTable(cardToPlay))
            {
                _currentTable[cardToPlay.Color].Add(cardToPlay);
                _score += 1;
                
                //if we've ran out of cards in the deck
                if (_currentDeck.Count == 1)
                    RestartGame();
            }
            else
                RestartGame();

            _currentPlayer.RemoveCardFromHand(indexOfCardInPlayersHand);

            TakeCardFromTheDeck();
        }

        bool CheckIfCardCanBeAddedToTable(Card card)
        {
            return
                //check if card with the same color on the table
                //has rank smaller than card being played                
                _currentTable[card.Color].Count < card.Rank
                //and the difference in rank 
                //is no more than 1
                && (card.Rank - _currentTable[card.Color].Count) == 1
                //and there are less than 25 cards currently on the table
                && _currentTable.Values.Sum((x) => x.Count) < 25;
        }

        void RestartGame()
        {
            RestartGame(null);
        }

        void RestartGame(GameStateArgs argsForWaitingToStartState)
        {
            _endGame = true;

            if (argsForWaitingToStartState == null)
            {
                InvokingFSM.PerformTransitionTo(GameStateID.WaitingToStart);
            }
            else
                InvokingFSM.PerformTransitionTo(GameStateID.WaitingToStart, argsForWaitingToStartState);

            if (_turn > 0)
                //If there were turns, we'll display infromation about last game
                Console.WriteLine("Turn: {0}, cards: {1}", _turn, _currentTable.Values.Sum((x) => x.Count));
        }

        void TakeCardFromTheDeck()
        {
            string cardToAddToPlayersHand = _currentDeck[0];
            _currentDeck.RemoveAt(0);

            _currentPlayer.AddCardToHand(cardToAddToPlayersHand);
        }

        void DropCard(string input)
        {
            int cardIndex = input[10] - '0';

            _discard.Add(_currentPlayer.CurrentHand[cardIndex]);
            _currentPlayer.CurrentHand.RemoveAt(cardIndex);

            TakeCardFromTheDeck();

            if (_currentDeck.Count < 1)
                RestartGame();
        }

        void TellColor(string input)
        {
            string colorRegExPattern = @"Red|Blue|Yellow|Green|White";
            string indexesOfCardsRegExPattern = @"\d";

            char color = Regex.Match(input, colorRegExPattern).Value[0];

            MatchCollection cardsIndexesMatches = Regex.Matches(input, indexesOfCardsRegExPattern);

            List<int> cardsIndexes = new List<int>();

            foreach(Match match in cardsIndexesMatches)
            {
                cardsIndexes.Add(Int32.Parse(match.Value));
            }

            if (!_nextPlayer.TryRememberColorForCards(color,cardsIndexes))
                RestartGame();

        }

        void TellRank(string input)
        {
            string allDigitsRegExPattern = @"\d";

            MatchCollection rankAndCardsIndexesMatches = Regex.Matches(input, allDigitsRegExPattern);

            List<int> cardsIndexes = new List<int>();

            foreach (Match match in rankAndCardsIndexesMatches)
            {
                cardsIndexes.Add(Int32.Parse(match.Value));
            }

            int rank = cardsIndexes[0];

            if (!_nextPlayer.TryRememberRankForCards(rank, cardsIndexes))
                RestartGame();
        }

        void SwapPlayers()
        {
            Player temp = _currentPlayer;
            _currentPlayer = _nextPlayer;
            _nextPlayer = temp;
        }


        void StopGameSession()
        {
            _endGame = true;
            InvokingFSM.PerformTransitionTo(GameStateID.EndSession);
        }

    }

    public class SessionEndedState : GameState
    {
        public SessionEndedState(MainGameLoopFSM invokingFSM)
        {
            this.InvokingFSM = invokingFSM;
        }

        public override void Action(GameStateArgs gmStateArgs)
        {
            InvokingFSM.Stop();
        }
    }

    public class MainGameLoopFSM
    {
        GameStateID _currentStateID;
        GameState _currentState;
        GameStateArgs _currentStateArgs;

        Dictionary<GameStateID, GameState> _allPossibleStates;

        bool _ended = false;

        public MainGameLoopFSM()
        {
            _allPossibleStates = new Dictionary<GameStateID, GameState>();
        }

        public void AddState(GameStateID stateID, GameState state)
        {
            if (_allPossibleStates.Keys.Contains(stateID))
                Console.WriteLine("GameState with ID {0} was already added",stateID);
            else
                _allPossibleStates.Add(stateID, state);
        }

        public void RemoveState(GameStateID stateID, GameState state)
        {
            if (!_allPossibleStates.Keys.Contains(stateID))
                Console.WriteLine("GameState with ID {0} was already removed", stateID);
            else
                _allPossibleStates.Remove(stateID);
        }

        public void PerformTransitionTo(GameStateID stateID)
        {
            PerformTransitionTo(stateID, null);
        }

        public void PerformTransitionTo(GameStateID stateID, GameStateArgs gmStateArgs)
        {
            if(_allPossibleStates.ContainsKey(stateID))
                _currentState = _allPossibleStates[stateID];
            else
            {
                Console.WriteLine("Invalid transition, the state has not been found");
                return;
            }

            _currentStateArgs = gmStateArgs;
            _currentStateID = stateID;
        }

        void ExecuteCurrentStateAction()
        {
            _currentState.Action(_currentStateArgs);
        }

        public void Start(GameStateID startingState)
        {
            Start(startingState,null);
        }

        public void Start(GameStateID startingState, GameStateArgs startingStateArgs)
        {
            PerformTransitionTo(startingState, startingStateArgs);

            while (!_ended)
            {
                ExecuteCurrentStateAction();
            }
        }

        public void Stop()
        {
            _ended = true;
        }

    }

    class Program
    {

        static void Main(string[] args)
        {
            MainGameLoopFSM gameFSM = new MainGameLoopFSM();

            gameFSM.AddState(GameStateID.WaitingToStart, new WaitingToStartState(gameFSM));
            gameFSM.AddState(GameStateID.Playing, new PlayingState(gameFSM));
            gameFSM.AddState(GameStateID.EndSession, new SessionEndedState(gameFSM));

            gameFSM.Start(GameStateID.WaitingToStart, null);
        }
    }
}