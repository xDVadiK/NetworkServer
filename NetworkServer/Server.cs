using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkServer
{
    internal class Server
    {
        protected internal TcpListener server;
        protected internal List<Client> players = new List<Client>();
        protected internal List<Client> clients = new List<Client>();
        protected internal List<Client> waitingPlayers = new List<Client>();

        bool START_GAME = false;
        Game game;

        public Server(TcpListener tcpListener)
        {
            server = tcpListener;
        }

        // Завершение работы сервера
        protected internal void Disconnect()
        {
            clients.ForEach(client =>
            {
                client.Message("Сервер завершил свою работу");
                client.Close();
            });
            clients.Clear();
            waitingPlayers.ForEach(client =>
            {
                client.Message("Сервер завершил свою работу");
                client.Close();
            });
            waitingPlayers.Clear();
            players.ForEach(player =>
            {
                player.Message("Сервер завершил свою работу");
                player.Close();
            });
            players.Clear();
            server.Stop();
        }

        // Запуск сервера
        protected internal void Start()
        {
            try
            {
                server.Start();
                Console.WriteLine("Сервер запущен. Ожидается подключение клиентов");
                Task.Run(WaitingStartGame);
                while (true)
                {
                    TcpClient player = server.AcceptTcpClient();
                    Client client = new Client(player);
                    clients.Add(client);
                    Console.WriteLine($"К серверу подключен клиент {client.id}");
                    Task.Run(() => СlientListening(client));
                }
            }
            catch
            {
                Console.WriteLine("Сервер завершил свою работу");
                Disconnect();
            }
        }

        // Обработка запросов клиента
        protected internal void СlientListening(Client client)
        {
            client.Message("Вы подключены к серверу. Для просмотра списка запросов введите '\\requests'");
            bool process = true;
            string request;
            while (process)
            {
                try
                {
                    request = client.Reader.ReadLine();
                    switch (request.ToLower())
                    {
                        case "\\play":
                            {
                                switch (client.status)
                                {
                                    case Status.Connected:
                                        {
                                            clients.Remove(client);
                                            waitingPlayers.Add(client);
                                            client.status = Status.Waiting;
                                            client.Message("Вы добавлены в очередь");
                                            QueuePosition(client);
                                            waitingPlayers.ForEach(clients =>
                                            {
                                                if (clients.chat && clients.id != client.id)
                                                {
                                                    clients.Message($"Пользователь {client.id} подключился к очереди");
                                                }
                                            });
                                            break;
                                        }
                                    case Status.Waiting:
                                        {
                                            client.Message("Вы уже ожидаете начала игры");
                                            break;
                                        }
                                    case Status.Playing:
                                        {
                                            client.Message("Вы уже в игре");
                                            break;
                                        }
                                }
                                break;
                            }
                        case "\\chat\\mute":
                            {
                                if (client.status != Status.Connected)
                                {
                                    if (client.chat)
                                    {
                                        client.chat = false;
                                        ChatNotification(client);
                                        client.Message("Чат выключен");
                                    }
                                    else
                                    {
                                        client.Message("Чат уже выключен, для включения введите '\\unmute'");
                                    }
                                }
                                else
                                {
                                    client.Message("Чтобы пользоваться чатом, сначала необходимо отправить запрос '\\play'");
                                }
                                break;
                            }
                        case "\\chat\\unmute":
                            {
                                if (client.status != Status.Connected)
                                {
                                    if (!client.chat)
                                    {
                                        client.chat = true;
                                        ChatNotification(client);
                                        client.Message("Чат включен");
                                    }
                                    else
                                    {
                                        client.Message("Чат уже включен, для выключения введите '\\mute'");
                                    }

                                }
                                else
                                {
                                    client.Message("Чтобы пользоваться чатом, сначала необходимо отправить запрос '\\play'");
                                }
                                break;
                            }
                        case "\\requests":
                            {
                                string command = $"Ваш статус: {client.status}\n\n" +
                                    $"Запросы для статуса '{Status.Connected}':\n" +
                                    "'\\play' - подключение к игре;\n" +
                                    "'\\exit' - выход с сервера;\n" +
                                    "'\\requests' - список запросов.\n\n" +
                                    $"Запросы для статуса '{Status.Waiting}':\n" +
                                    "'\\chat\\{сообщение}' - отправка сообщения в чат;\n" +
                                    "'\\chat\\mute' - отключение чата;\n" +
                                    "'\\chat\\unmute' - включение чата;\n" +
                                    "'\\exit' - выход с сервера;\n" +
                                    "'\\requests' - список запросов.\n\n" +
                                    $"Запросы для статуса '{Status.Playing}':\n" +
                                    "'\\pause' - перевод игры в режим паузы;\n" +
                                    "'\\move\\{действие}' - ход игрока;\n" +
                                    "'\\chat\\{сообщение}' - отправка сообщения в чат;\n" +
                                    "'\\chat\\mute' - отключение чата;\n" +
                                    "'\\chat\\unmute' - включение чата;\n" +
                                    "'\\exit' - выход с сервера;\n" +
                                    "'\\requests' - список запросов.\n\n";
                                client.Message(command);
                                break;
                            }
                        case "\\pause":
                            {
                                switch (client.status)
                                {
                                    case Status.Connected:
                                        {
                                            Console.WriteLine("Пауза доступна только в ходе игры");
                                            break;
                                        }
                                    case Status.Waiting:
                                        {
                                            Console.WriteLine("Пауза доступна только в ходе игры");
                                            break;
                                        }
                                    case Status.Playing:
                                        {
                                            if (game.status != GameStatus.Pause)
                                            {
                                                game.PauseStart();
                                            }
                                            else
                                            {
                                                client.Message("Игра уже поставлена на паузу");
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                        case "\\exit":
                            {
                                bool exit = false;
                                while (!exit)
                                {
                                    client.Message("Вы точно хотите отключиться от сервера? (yes/no)");
                                    request = client.Reader.ReadLine();
                                    if (request.ToLower().Equals("yes"))
                                    {
                                        Console.WriteLine($"Клиент {client.id} покинул сервер");
                                        switch (client.status)
                                        {
                                            case Status.Connected:
                                                {
                                                    clients.Remove(client);
                                                    client.Close();
                                                    process = false;
                                                    exit = true;
                                                    break;
                                                }
                                            case Status.Waiting:
                                                {
                                                    waitingPlayers.Remove(client);
                                                    waitingPlayers.ForEach(clients =>
                                                    {
                                                        if (clients.chat && clients.id != client.id)
                                                        {
                                                            clients.Message($"Пользователь {client.id} покинул очередь");
                                                        }
                                                        QueuePosition(clients);
                                                    });
                                                    client.Close();
                                                    process = false;
                                                    exit = true;
                                                    break;
                                                }
                                            case Status.Playing:
                                                {
                                                    PrematurePlayerExit(client);
                                                    client.Close();
                                                    process = false;
                                                    exit = true;
                                                    break;
                                                }
                                        }
                                    }
                                    else if (!request.ToLower().Equals("no"))
                                    {
                                        client.Message("Некорректный ответ, повторите попытку");
                                    }
                                    else
                                    {
                                        exit = true;
                                    }
                                }
                                break;
                            }
                        default:
                            {
                                if (Regex.IsMatch(request, @"\\move\\.*", RegexOptions.IgnoreCase))
                                {
                                    switch (client.status)
                                    {
                                        case Status.Playing:
                                            {
                                                if (game.status != GameStatus.Pause)
                                                {
                                                    request = Regex.Replace(request, @"\\move\\", "", RegexOptions.IgnoreCase);
                                                    game.Move(client, request);
                                                    CheckEndGame();
                                                }
                                                else
                                                {
                                                    client.Message("Ход невозможен, игра поставлена на паузу");
                                                }
                                                break;
                                            }
                                        case Status.Connected:
                                            {
                                                client.Message("Вы не подключены к игре\nОтправьте запрос '\\play' для начала игры");
                                                break;
                                            }
                                        case Status.Waiting:
                                            {
                                                client.Message("Вы не подключены к игре\nОжидайте своей очереди");
                                                break;
                                            }
                                    }
                                }
                                else if (Regex.IsMatch(request, @"\\chat\\.*", RegexOptions.IgnoreCase))
                                {
                                    request = Regex.Replace(request, @"\\chat\\", "", RegexOptions.IgnoreCase);
                                    switch (client.status)
                                    {
                                        case Status.Connected:
                                            {
                                                client.Message("Чтобы пользоваться чатом, сначала необходимо отправить запрос '\\play'");
                                                break;
                                            }
                                        case Status.Waiting:
                                            {
                                                QueueChat(client, request);
                                                break;
                                            }
                                        case Status.Playing:
                                            {
                                                GameChat(client, request);
                                                break;
                                            }
                                    }
                                }
                                else
                                {
                                    client.Message("Неизвестный запрос. Для просмотра списка запросов введите '\\requests'");
                                }
                                break;
                            }
                    }
                }
                catch
                {
                    Console.WriteLine($"Клиент {client.id} покинул сервер");
                    switch (client.status)
                    {
                        case Status.Connected:
                            {
                                clients.Remove(client);
                                break;
                            }
                        case Status.Waiting:
                            {
                                waitingPlayers.Remove(client);
                                waitingPlayers.ForEach(clients =>
                                {
                                    if (clients.chat && clients.id != client.id)
                                    {
                                        clients.Message($"Пользователь {client.id} покинул очередь");
                                    }
                                    QueuePosition(clients);
                                });
                                break;
                            }
                        case Status.Playing:
                            {
                                PrematurePlayerExit(client);
                                break;
                            }
                    }
                    client.Close();
                    break;
                }
            }
        }

        // Оповещение чата о изменении числа участников
        private void ChatNotification(Client speaker)
        {
            string message;
            if (speaker.status == Status.Waiting)
            {
                waitingPlayers.ForEach(client =>
                {
                    if (client.chat && client.id != speaker.id)
                    {
                        message = $"Пользователь {speaker.id} " + (!speaker.chat ? "выключил чат" : "включил чат");
                        client.Message(message);
                    }
                }
                );
            }
            else
            {
                players.ForEach((player) =>
                {
                    if (player.chat && player.id != speaker.id)
                    {
                        message = $"Игрок {players.IndexOf(speaker) + 1} " + (!speaker.chat ? "выключил чат" : "включил чат");
                        player.Message(message);
                    }
                }
);
            }
        }

        // Преждевременное завершение игры
        private void PrematurePlayerExit(Client client)
        {
            if (game.status != GameStatus.End)
            {
                players.Remove(client);
                players.ForEach(players => players.Message("Соперник вышел из игры"));
                game.status = GameStatus.End;
                CheckEndGame();
            }
        }

        // Проверка окончания игры
        private void CheckEndGame()
        {
            if (game.status == GameStatus.End)
            {
                players.ForEach(player =>
                {
                    player.Message("Игра окончена\nЧтобы играть снова введите команду '\\play'");
                    player.status = Status.Connected;
                    player.chat = true;
                });
                clients.AddRange(players);
                players.Clear();
                game.timer.Dispose();
                game = null;
                START_GAME = false;
            }
        }

        // Оповещение клиентов об изменении их позиции в очереди
        protected internal void QueuePosition(Client client)
        {
            var number = waitingPlayers.IndexOf(client) + 1;
            client.Writer.WriteLine($"Ваша позиция в очереди: {number}");
            client.Writer.Flush();
        }

        // Ожидание игроков и запуск игры
        protected internal void WaitingStartGame()
        {
            while (true)
            {
                while (waitingPlayers.Count < 2 || START_GAME)
                {
                    Thread.Sleep(5000);
                }
                players.Add(waitingPlayers[0]);
                players.Add(waitingPlayers[1]);
                waitingPlayers.RemoveAll(player => players.Contains(player));
                players.ForEach(player =>
                {
                    player.Message("Вы подключены к игре");
                    player.status = Status.Playing;
                    player.chat = true;
                }
                );
                waitingPlayers.ForEach(clients => QueuePosition(clients));
                START_GAME = true;
                game = new Game(players);
            }
        }

        // Сообщение в чат пользователей, ожидающих начало игры
        protected internal void QueueChat(Client speaker, string message)
        {
            waitingPlayers.ForEach(client =>
            {
                if (client.chat)
                {
                    client.Message($"Пользователь {speaker.id} пишет: " + message);
                }
            }
            );
        }

        // Сообщение в чат игроков
        protected internal void GameChat(Client speaker, string message)
        {
            players.ForEach((player) =>
            {
                if (player.chat)
                {
                    player.Message($"Игрок {players.IndexOf(speaker) + 1} пишет: " + message);
                }
            }
            );
        }
    }
}