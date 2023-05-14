using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace NetworkServer
{
    enum GameStatus
    {
        MoveFirst = 0,
        MoveSecond = 1,
        Pause = 2,
        End = 3
    }

    internal class Game
    {
        protected internal GameStatus status;
        protected internal List<Client> players;

        protected internal Timer timer;
        protected internal int timeout = 30000;
        protected internal int countWarnings = 0;

        protected internal GameStatus lastStatus;
        protected internal int pauseTime = 30000;

        protected internal Battleship battleship;

        public Game(List<Client> players)
        {
            this.players = players;
            battleship = new Battleship();
            players[0].Message(battleship.getFieldsToString(battleship.fieldFirstPlayer, battleship.fieldMovesFirstPlayer));
            players[1].Message(battleship.getFieldsToString(battleship.fieldSecondPlayer, battleship.fieldMovesSecondPlayer));
            status = GameStatus.MoveFirst;
            timer = new Timer(MoveTimeout, players[0], timeout, timeout);
            Notification();
        }

        // Отсчёт времени хода игрока
        private void MoveTimeout(object obj)
        {
            Client client = (Client)obj;
            if (countWarnings == 1)
            {
                client.Message("Время вашего хода истекло, вы будете отключены от сервера");
                timer.Dispose();
                client.Close();
            }
            else
            {
                countWarnings++;
                client.Message($"Если вы не сделаете ход в течение {timeout / 1000} секунд, игра будет прервана");
                timer.Change(timeout, timeout);
            }
        }

        // Обработка хода игрока
        protected internal void Move(Client client, string action)
        {
            if (players.IndexOf(client) == (int)status)
            {
                if (Regex.IsMatch(action, "^[A-J][0-9]$", RegexOptions.IgnoreCase))
                {
                    string result;
                    if (64 < action[0] && action[0] < 75)
                    {
                        result = battleship.ProcessMove(action[0] - 65, action[1] - '0', status);
                    }
                    else
                    {
                        result = battleship.ProcessMove(action[0] - 97, action[1] - '0', status);
                    }
                    switch (result)
                    {
                        case "Промахнулся":
                            {
                                players[0].Message(battleship.getFieldsToString(battleship.fieldFirstPlayer, battleship.fieldMovesFirstPlayer));
                                players[1].Message(battleship.getFieldsToString(battleship.fieldSecondPlayer, battleship.fieldMovesSecondPlayer));
                                players.ForEach(player => player.Message($"Игрок {(int)status + 1} сделал ход {action.ToUpper()} и промахнулся"));
                                timer.Dispose();
                                countWarnings = 0;
                                if (status == GameStatus.MoveFirst)
                                {
                                    status = GameStatus.MoveSecond;
                                    timer = new Timer(MoveTimeout, players[1], timeout, timeout);
                                }
                                else
                                {
                                    status = GameStatus.MoveFirst;
                                    timer = new Timer(MoveTimeout, players[0], timeout, timeout);
                                }
                                Notification();
                                break;
                            }
                        case "Потопил":
                            {
                                players[0].Message(battleship.getFieldsToString(battleship.fieldFirstPlayer, battleship.fieldMovesFirstPlayer));
                                players[1].Message(battleship.getFieldsToString(battleship.fieldSecondPlayer, battleship.fieldMovesSecondPlayer));
                                players.ForEach(player => player.Message($"Игрок {(int)status + 1} сделал ход {action.ToUpper()} и потопил корабль"));
                                if (battleship.EndGame(status))
                                {
                                    players.ForEach(player => player.Message($"Игрок {(int)status + 1} выиграл\n"));
                                    timer.Change(Timeout.Infinite, Timeout.Infinite);
                                    status = GameStatus.End;
                                }
                                else
                                {
                                    timer.Dispose();
                                    countWarnings = 0;
                                    if (status == GameStatus.MoveFirst)
                                    {
                                        timer = new Timer(MoveTimeout, players[0], timeout, timeout);
                                    }
                                    else
                                    {
                                        timer = new Timer(MoveTimeout, players[1], timeout, timeout);
                                    }
                                    Notification();
                                }
                                break;
                            }
                        case "Попал":
                            {
                                players[0].Message(battleship.getFieldsToString(battleship.fieldFirstPlayer, battleship.fieldMovesFirstPlayer));
                                players[1].Message(battleship.getFieldsToString(battleship.fieldSecondPlayer, battleship.fieldMovesSecondPlayer));
                                players.ForEach(player => player.Message($"Игрок {(int)status + 1} сделал ход {action.ToUpper()} и попал по кораблю"));
                                timer.Dispose();
                                countWarnings = 0;
                                if (status == GameStatus.MoveFirst)
                                {
                                    timer = new Timer(MoveTimeout, players[0], timeout, timeout);
                                }
                                else
                                {
                                    timer = new Timer(MoveTimeout, players[1], timeout, timeout);
                                }
                                Notification();
                                break;
                            }
                        case "Повторный ход":
                            {
                                client.Message("Повторных ход в данную клетку; повторите попытку, сделав ход в новую клетку");
                                break;
                            }
                    }
                }
                else
                {
                    client.Message("Некорректный ход, повторите попытку");
                }
            }
            else
            {
                client.Message("Ход другого игрока");
            }
        }

        // Запуск паузы
        protected internal void PauseStart()
        {
            players.ForEach(player => player.Message($"Игрок {(int)status + 1} поставил игру на паузу на {pauseTime / 1000} секунд"));
            lastStatus = status;
            status = GameStatus.Pause;
            timer.Dispose();
            timer = new Timer(PauseStop, null, pauseTime, 0);
        }

        // Завершение паузы
        private void PauseStop(object obj)
        {
            players.ForEach(player => player.Message("Пауза закончена"));
            status = lastStatus;
            timer.Dispose();
            countWarnings = 0;
            if (status == GameStatus.MoveFirst)
            {
                timer = new Timer(MoveTimeout, players[0], timeout, timeout);
            }
            else
            {
                timer = new Timer(MoveTimeout, players[1], timeout, timeout);
            }
        }

        // Оповещение о порядке хода игроков
        private void Notification()
        {
            if (status == GameStatus.MoveFirst)
            {
                players[0].Message("Ваш ход");
                players[1].Message("Ходит другой игрок");
            }
            else
            {
                players[1].Message("Ваш ход");
                players[0].Message("Ходит другой игрок");
            }
        }
    }
}