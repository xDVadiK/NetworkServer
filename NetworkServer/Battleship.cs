using System;
using System.Linq;
using System.Text;

namespace NetworkServer
{
    internal class Battleship
    {
        protected internal string[,] fieldFirstPlayer = new string[11, 11];
        protected internal string[,] fieldMovesFirstPlayer = new string[11, 11];
        protected internal string[,] fieldSecondPlayer = new string[11, 11];
        protected internal string[,] fieldMovesSecondPlayer = new string[11, 11];

        private int[] shipLengths = { 4, 3, 2, 1 }; // Длина кораблей
        private int[] shipCounts = { 1, 2, 3, 4 }; // Количество кораблей
        private int shipCountsFirstPlayer;
        private int shipCountsSecondPlayer;

        public Battleship()
        {
            InitialFillingField(fieldFirstPlayer);
            InitialFillingField(fieldMovesFirstPlayer);
            InitialFillingField(fieldSecondPlayer);
            InitialFillingField(fieldMovesSecondPlayer);
            PlaceShips(fieldFirstPlayer);
            showFields(fieldFirstPlayer, fieldMovesFirstPlayer);
            PlaceShips(fieldSecondPlayer);
            showFields(fieldSecondPlayer, fieldMovesSecondPlayer);
            shipCountsFirstPlayer = shipCounts.Sum();
            shipCountsSecondPlayer = shipCounts.Sum();
        }

        // Первичное заполнение игровых полей
        private void InitialFillingField(string[,] field)
        {
            char letter = 'A';
            for (int i = 0; i < 11; i++)
            {
                if (i == 0)
                {
                    field[0, i] = " ";
                }
                else
                {
                    field[0, i] = Char.ToString(letter++);
                }
            }
            for (int i = 0; i < 11; i++)
            {
                if (i == 0)
                {
                    field[i, 0] = " ";
                }
                else
                {
                    field[i, 0] = Convert.ToString(i - 1);
                }
            }
            for (int i = 1; i < 11; i++)
            {
                for (int j = 1; j < 11; j++)
                {
                    field[i, j] = ".";
                }
            }
        }

        // Автоматическая расстановка кораблей на игровом поле
        private void PlaceShips(string[,] field)
        {
            Random random = new Random();
            for (int i = 0; i < shipLengths.Length; i++)
            {
                for (int j = 0; j < shipCounts[i]; j++)
                {
                    int shipLength = shipLengths[i];
                    int x, y;
                    int rotation;
                    do
                    {
                        x = random.Next(1, 10);
                        y = random.Next(1, 10);
                        rotation = random.Next(1, 2);
                    } while (!CanPlaceShip(field, x, y, rotation, shipLength));
                    for (int k = 0; k < shipLength; k++)
                    {
                        if (rotation == 1)
                        {
                            field[y + k, x] = "#";
                        }
                        else
                        {
                            field[y, x + k] = "#";
                        }
                    }
                }
            }
        }

        // Проверка возможности размещения корабля
        private bool CanPlaceShip(string[,] field, int x, int y, int rotation, int length)
        {
            int x2 = (rotation == 1) ? x : x + length - 1;
            int y2 = (rotation == 1) ? y + length - 1 : y;
            if (x2 > 10 || y2 > 10)
            {
                return false;
            }
            x = (x > 1) ? x - 1 : x;
            y = (y > 1) ? y - 1 : y;
            x2 = (x2 < 10) ? x2 + 1 : x2;
            y2 = (y2 < 10) ? y2 + 1 : y2;
            for (int i = y; i <= y2; i++)
            {
                for (int j = x; j <= x2; j++)
                {
                    if (!field[i, j].Equals("."))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Отображение игровых полей в консоли
        protected internal void showFields(string[,] shipsField, string[,] movesField)
        {
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 11; j++)
                {
                    Console.Write(shipsField[i, j] + " ");
                    if (j == 10)
                    {
                        Console.Write("  ");
                    }
                }
                for (int j = 0; j < 11; j++)
                {
                    Console.Write(movesField[i, j] + " ");
                    if (j == 10)
                    {
                        Console.WriteLine();
                    }
                }
            }
        }

        // Преобразование игровых полей в строку
        protected internal string getFieldsToString(string[,] shipsField, string[,] movesField)
        {
            StringBuilder stringBuilder = new StringBuilder("\n");
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 11; j++)
                {
                    stringBuilder.Append(shipsField[i, j]).Append(" ");
                    if (j == 10)
                    {
                        stringBuilder.Append("  ");
                    }
                }
                for (int j = 0; j < 11; j++)
                {
                    stringBuilder.Append(movesField[i, j]).Append(" ");
                    if (j == 10)
                    {
                        stringBuilder.Append("\n");
                    }
                }
            }
            return stringBuilder.ToString();
        }

        // Обработка хода игрока
        protected internal string ProcessMove(int y, int x, GameStatus status)
        {
            x = x + 1; y = y + 1;
            if (status == GameStatus.MoveFirst)
            {
                if (!fieldSecondPlayer[x, y].Equals("#"))
                {
                    if (fieldSecondPlayer[x, y].Equals("X") || fieldSecondPlayer[x, y].Equals("O"))
                    {
                        return "Повторный ход";
                    }
                    else
                    {
                        fieldSecondPlayer[x, y] = "O";
                        fieldMovesFirstPlayer[x, y] = "O";
                        return "Промахнулся";
                    }
                }
                else
                {
                    fieldSecondPlayer[x, y] = "X";
                    fieldMovesFirstPlayer[x, y] = "X";
                    if (CheckShip(x, y, fieldSecondPlayer))
                    {
                        shipCountsSecondPlayer--;
                        return "Потопил";
                    }
                    else
                    {
                        return "Попал";
                    }
                }
            }
            else
            {
                if (!fieldFirstPlayer[x, y].Equals("#"))
                {
                    fieldFirstPlayer[x, y] = "O";
                    fieldMovesSecondPlayer[x, y] = "O";
                    return "Промахнулся";
                }
                else
                {
                    fieldFirstPlayer[x, y] = "X";
                    fieldMovesSecondPlayer[x, y] = "X";
                    if (CheckShip(x, y, fieldFirstPlayer))
                    {
                        shipCountsFirstPlayer--;
                        return "Потопил";
                    }
                    else
                    {
                        return "Попал";
                    }
                }
            }
        }

        // Проверка состояния корабля 
        private bool CheckShip(int x, int y, string[,] array)
        {
            int x1 = x;
            int y1 = y;
            while (x1 < array.Length && !array[x1, y].Equals("."))
            {
                if (array[x1, y].Equals("#"))
                {
                    return false;
                }
                x1++;
            }
            while (x1 > 0 && !array[x1, y].Equals("."))
            {
                if (array[x1, y].Equals("#"))
                {
                    return false;
                }
                x1--;
            }
            while (y1 < array.Length && !array[x, y1].Equals("."))
            {
                if (array[x, y1].Equals("#"))
                {
                    return false;
                }
                y1++;
            }
            while (y1 > 0 && !array[x, y1].Equals("."))
            {
                if (array[x1, y].Equals("#"))
                {
                    return false;
                }
                y1--;
            }
            return true;
        }

        // Проверка окончания игры
        protected internal bool EndGame(GameStatus status)
        {
            return (status == GameStatus.MoveFirst) ? shipCountsSecondPlayer == 0 : shipCountsFirstPlayer == 0;
        }
    }
}