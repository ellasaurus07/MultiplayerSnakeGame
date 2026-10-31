using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

Console.Title = "Multiplayer Snake Game";
Console.CursorVisible = false;

const int PORT = 5000;
const int WIDTH = 30;
const int HEIGHT = 15;
const int SPEED = 100; // Lower number = faster movement

Console.WriteLine("=== MULTIPLAYER SNAKE ===");
Console.WriteLine("1. Host Game");
Console.WriteLine("2. Join Game");
Console.Write("Choose: ");

string? choice = Console.ReadLine();

if (choice == "1")
    await HostGame();
else if (choice == "2")
    await JoinGame();
else
    Console.WriteLine("Invalid choice.");

Console.CursorVisible = true;
Console.WriteLine("\nPress any key to exit.");
Console.ReadKey(true);


// ====================================
// PLAYER 1 - HOST / SERVER
// ====================================

async Task HostGame()
{
    TcpListener server = new(IPAddress.Any, PORT);
    server.Start();

    try
    {
        Console.WriteLine("\nWaiting for Player 2...");

        using TcpClient client = await server.AcceptTcpClientAsync();
        client.NoDelay = true;

        using NetworkStream stream = client.GetStream();
        using StreamReader reader = new(stream, Encoding.UTF8);
        using StreamWriter writer = new(stream, Encoding.UTF8)
        {
            AutoFlush = true
        };

        Console.WriteLine("Player 2 connected!");

        object sync = new();

        (int x, int y) direction2 = (-1, 0);
        TaskCompletionSource<bool>? vote2 = null;
        bool disconnected = false;

        // Listen for Player 2's movement and vote messages.
        Task receiver = Task.Run(async () =>
        {
            try
            {
                string? message;

                while ((message = await reader.ReadLineAsync()) != null)
                {
                    lock (sync)
                    {
                        if (message.StartsWith("KEY:"))
                        {
                            direction2 = ChangeDirection(
                                direction2,
                                message.Substring(4)
                            );
                        }
                        else if (message == "VOTE:Y")
                        {
                            vote2?.TrySetResult(true);
                        }
                        else if (message == "VOTE:N")
                        {
                            vote2?.TrySetResult(false);
                        }
                    }
                }
            }
            catch
            {
                // Connection lost.
            }
            finally
            {
                lock (sync)
                {
                    disconnected = true;
                    vote2?.TrySetResult(false);
                }
            }
        });

        bool keepPlaying = true;

        while (keepPlaying)
        {
            // Create fresh snakes for each round.
            List<(int x, int y)> snake1 = new()
            {
                (5, 5), (4, 5), (3, 5)
            };

            List<(int x, int y)> snake2 = new()
            {
                (24, 10), (25, 10), (26, 10)
            };

            (int x, int y) direction1 = (1, 0);

            lock (sync)
            {
                direction2 = (-1, 0);
            }

            Random random = new();
            (int x, int y) food = (15, 7);

            int score1 = 0;
            int score2 = 0;

            bool gameOver = false;
            string winner = "";

            Console.Clear();
            Console.WriteLine("=== MULTIPLAYER SNAKE ===");
            Console.WriteLine();
            Console.WriteLine("Both snakes are ready!");
            Console.WriteLine();
            Console.WriteLine("Press ENTER to start.");

            while (Console.ReadKey(true).Key != ConsoleKey.Enter)
            {
            }

            // Clear any keys pressed before the round.
            while (Console.KeyAvailable)
                Console.ReadKey(true);

            await writer.WriteLineAsync("START");

            Console.Clear();

            string board = BuildBoard(
                snake1, snake2, food,
                score1, score2, winner
            );

            DrawFrame(board);
            await SendFrame(writer, board);

            Stopwatch clock = Stopwatch.StartNew();
            long nextTick = SPEED;

            while (!gameOver)
            {
                // Read Player 1 input frequently.
                while (clock.ElapsedMilliseconds < nextTick)
                {
                    while (Console.KeyAvailable)
                    {
                        ConsoleKey key = Console.ReadKey(true).Key;

                        direction1 = ChangeDirection(
                            direction1, key.ToString()
                        );
                    }

                    await Task.Delay(5);
                }

                nextTick = clock.ElapsedMilliseconds + SPEED;

                (int x, int y) dir2;
                bool connectionLost;

                lock (sync)
                {
                    dir2 = direction2;
                    connectionLost = disconnected;
                }

                if (connectionLost)
                {
                    Console.Clear();
                    Console.WriteLine("Player 2 disconnected.");
                    return;
                }

                (int x, int y) next1 =
                (
                    snake1[0].x + direction1.x,
                    snake1[0].y + direction1.y
                );

                (int x, int y) next2 =
                (
                    snake2[0].x + dir2.x,
                    snake2[0].y + dir2.y
                );

                bool eat1 = next1 == food;
                bool eat2 = next2 == food;

                bool crash1 =
                    Outside(next1) ||
                    snake1.Take(
                        eat1 ? snake1.Count : snake1.Count - 1
                    ).Contains(next1) ||
                    snake2.Take(
                        eat2 ? snake2.Count : snake2.Count - 1
                    ).Contains(next1);

                bool crash2 =
                    Outside(next2) ||
                    snake2.Take(
                        eat2 ? snake2.Count : snake2.Count - 1
                    ).Contains(next2) ||
                    snake1.Take(
                        eat1 ? snake1.Count : snake1.Count - 1
                    ).Contains(next2);

                // Both snakes collide head-on.
                if (next1 == next2 ||
                    (next1 == snake2[0] &&
                     next2 == snake1[0]))
                {
                    crash1 = true;
                    crash2 = true;
                }

                if (crash1 || crash2)
                {
                    gameOver = true;

                    winner = crash1 && crash2
                        ? "DRAW!"
                        : crash1
                            ? "PLAYER 2 WINS!"
                            : "PLAYER 1 WINS!";
                }
                else
                {
                    snake1.Insert(0, next1);
                    snake2.Insert(0, next2);

                    if (eat1)
                        score1++;
                    else
                        snake1.RemoveAt(snake1.Count - 1);

                    if (eat2)
                        score2++;
                    else
                        snake2.RemoveAt(snake2.Count - 1);

                    if (eat1 || eat2)
                    {
                        if (snake1.Count + snake2.Count >=
                            WIDTH * HEIGHT)
                        {
                            gameOver = true;
                            winner = "DRAW!";
                        }
                        else
                        {
                            do
                            {
                                food = (
                                    random.Next(WIDTH),
                                    random.Next(HEIGHT)
                                );
                            }
                            while (snake1.Contains(food) ||
                                   snake2.Contains(food));
                        }
                    }
                }

                board = BuildBoard(
                    snake1, snake2, food,
                    score1, score2, winner
                );

                DrawFrame(board);
                await SendFrame(writer, board);
            }

            // ==========================
            // BOTH PLAYERS VOTE
            // ==========================

            TaskCompletionSource<bool> newVote =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            lock (sync)
            {
                vote2 = newVote;
            }

            await writer.WriteLineAsync("VOTE");

            Console.Clear();
            Console.WriteLine("=== GAME OVER ===");
            Console.WriteLine(winner);
            Console.WriteLine();
            Console.WriteLine("PLAY AGAIN?");
            Console.WriteLine("[Y] Yes");
            Console.WriteLine("[N] No");
            Console.WriteLine();

            ConsoleKey answer;

            do
            {
                answer = Console.ReadKey(true).Key;
            }
            while (answer != ConsoleKey.Y &&
                   answer != ConsoleKey.N);

            bool player1Yes = answer == ConsoleKey.Y;

            Console.WriteLine(
                player1Yes
                    ? "You voted YES."
                    : "You voted NO."
            );

            Console.WriteLine("Waiting for Player 2's vote...");

            bool player2Yes = await newVote.Task;

            lock (sync)
            {
                vote2 = null;
            }

            if (player1Yes && player2Yes)
            {
                Console.WriteLine("\nBoth players voted YES!");
                Console.WriteLine("Starting another round...");

                await writer.WriteLineAsync("RESTART");

                // The outer loop respawns both snakes.
            }
            else
            {
                keepPlaying = false;

                Console.WriteLine(
                    "\nAt least one player voted NO."
                );

                Console.WriteLine("Game session ended.");

                await writer.WriteLineAsync("QUIT");
            }
        }
    }
    catch (IOException)
    {
        Console.WriteLine("\nConnection lost.");
    }
    catch (SocketException)
    {
        Console.WriteLine("\nNetwork error.");
    }
    finally
    {
        server.Stop();
    }
}


// ====================================
// PLAYER 2 - CLIENT
// ====================================

async Task JoinGame()
{
    Console.Write("Enter Host IP address: ");
    string ip = Console.ReadLine() ?? "";

    using TcpClient client = new();
    client.NoDelay = true;

    try
    {
        await client.ConnectAsync(ip, PORT);
    }
    catch
    {
        Console.WriteLine("Could not connect to host.");
        return;
    }

    using NetworkStream stream = client.GetStream();
    using StreamReader reader = new(stream, Encoding.UTF8);
    using StreamWriter writer = new(stream, Encoding.UTF8)
    {
        AutoFlush = true
    };

    Console.WriteLine("Connected!");
    Console.WriteLine("Waiting for Player 1...");

    // 0 = waiting, 1 = playing,
    // 2 = voting, 3 = vote submitted.
    int phase = 0;

    using CancellationTokenSource stopInput = new();

    // Keyboard input runs independently of board updates.
    Task inputTask = Task.Run(async () =>
    {
        try
        {
            while (!stopInput.IsCancellationRequested)
            {
                while (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;
                    int currentPhase = Volatile.Read(ref phase);

                    if (currentPhase == 1)
                    {
                        await writer.WriteLineAsync(
                            "KEY:" + key
                        );
                    }
                    else if (currentPhase == 2)
                    {
                        if (key == ConsoleKey.Y ||
                            key == ConsoleKey.N)
                        {
                            // Prevent submitting more than one vote.
                            Volatile.Write(ref phase, 3);

                            bool yes = key == ConsoleKey.Y;

                            await writer.WriteLineAsync(
                                yes ? "VOTE:Y" : "VOTE:N"
                            );

                            Console.WriteLine(
                                yes
                                    ? "\nYou voted YES."
                                    : "\nYou voted NO."
                            );

                            Console.WriteLine(
                                "Waiting for Player 1..."
                            );
                        }
                    }
                }

                await Task.Delay(5, stopInput.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // Connection closed.
        }
    });

    try
    {
        while (true)
        {
            string? message = await reader.ReadLineAsync();

            if (message == null)
                break;

            if (message == "START")
            {
                Volatile.Write(ref phase, 1);
                Console.Clear();
            }
            else if (message == "VOTE")
            {
                Volatile.Write(ref phase, 2);

                Console.Clear();
                Console.WriteLine("=== GAME OVER ===");
                Console.WriteLine();
                Console.WriteLine("PLAY AGAIN?");
                Console.WriteLine("[Y] Yes");
                Console.WriteLine("[N] No");
            }
            else if (message == "RESTART")
            {
                Volatile.Write(ref phase, 0);

                Console.Clear();
                Console.WriteLine("Both players voted YES!");
                Console.WriteLine();
                Console.WriteLine("Snakes will respawn.");
                Console.WriteLine("Waiting for host to start...");
            }
            else if (message == "QUIT")
            {
                Volatile.Write(ref phase, 0);

                Console.Clear();
                Console.WriteLine("Game session ended.");
                Console.WriteLine(
                    "At least one player voted NO."
                );

                break;
            }
            else if (message.StartsWith("FRAME|"))
            {
                string encoded = message.Substring(6);

                string board = Encoding.UTF8.GetString(
                    Convert.FromBase64String(encoded)
                );

                DrawFrame(board);
            }
        }
    }
    catch
    {
        Console.WriteLine("\nConnection lost.");
    }
    finally
    {
        stopInput.Cancel();
        await inputTask;
    }
}


// ====================================
// HELPER: WALL COLLISION
// ====================================

bool Outside((int x, int y) point)
{
    return point.x < 0 ||
           point.x >= WIDTH ||
           point.y < 0 ||
           point.y >= HEIGHT;
}


// ====================================
// HELPER: KEYBOARD MOVEMENT
// ====================================

(int x, int y) ChangeDirection(
    (int x, int y) current,
    string key)
{
    (int x, int y) next = key switch
    {
        "W" or "UpArrow" => (0, -1),
        "S" or "DownArrow" => (0, 1),
        "A" or "LeftArrow" => (-1, 0),
        "D" or "RightArrow" => (1, 0),
        _ => current
    };

    // Prevent immediate reversal.
    if (next.x == -current.x &&
        next.y == -current.y)
    {
        return current;
    }

    return next;
}


// ====================================
// HELPER: CREATE THE GAME BOARD
// ====================================

string BuildBoard(
    List<(int x, int y)> snake1,
    List<(int x, int y)> snake2,
    (int x, int y) food,
    int score1,
    int score2,
    string winner)
{
    StringBuilder sb = new();

    sb.AppendLine("=== MULTIPLAYER SNAKE ===");
    sb.AppendLine($"Player 1: {score1} | Player 2: {score2}");
    sb.AppendLine("Controls: WASD / Arrow Keys");
    sb.AppendLine();

    sb.AppendLine("+" + new string('-', WIDTH) + "+");

    for (int y = 0; y < HEIGHT; y++)
    {
        sb.Append('|');

        for (int x = 0; x < WIDTH; x++)
        {
            char symbol = ' ';

            if (food == (x, y))
                symbol = '*';

            if (snake1.Contains((x, y)))
                symbol = 'O';

            if (snake2.Contains((x, y)))
                symbol = 'X';

            sb.Append(symbol);
        }

        sb.AppendLine("|");
    }

    sb.AppendLine("+" + new string('-', WIDTH) + "+");
    sb.AppendLine("O = Player 1 | X = Player 2 | * = Food");

    if (!string.IsNullOrEmpty(winner))
    {
        sb.AppendLine();
        sb.AppendLine("GAME OVER!");
        sb.AppendLine(winner);
    }

    return sb.ToString();
}


// ====================================
// HELPER: REDRAW WITHOUT FLICKERING
// ====================================

void DrawFrame(string board)
{
    try
    {
        Console.SetCursorPosition(0, 0);
        Console.Write(board);
    }
    catch (ArgumentOutOfRangeException)
    {
        Console.Clear();
        Console.Write(board);
    }
}


// ====================================
// HELPER: SEND BOARD OVER TCP
// ====================================

async Task SendFrame(StreamWriter writer, string board)
{
    string encoded = Convert.ToBase64String(
        Encoding.UTF8.GetBytes(board)
    );

    await writer.WriteLineAsync("FRAME|" + encoded);
}