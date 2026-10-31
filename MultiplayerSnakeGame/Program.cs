
using System.Net; //handles IP addresses and networking
using System.Net.Sockets; // provides TCP communication between computers,  lets devices and applications send data reliably across a network
using System.Text;

Console.Title = "Multiplayer Snake Game";
Console.CursorVisible = false;

const int port = 5000;
const int width = 30;
const int height = 15;

Console.WriteLine("=== MULTIPLAYER SNAKE ===");
Console.WriteLine("1. Host Game");
Console.WriteLine("2. Join Game");
Console.Write("Choose: ");

string? choice = Console.ReadLine();

if (choice == "1")
{
    await HostGame();
}
else if (choice == "2")
{
    await JoinGame();
}
else
{
    Console.WriteLine("Invalid choice.");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey(true);


// ======================
// HOST / SERVER
// ======================

//TcpListener allows player 1 to host n wait for connection
//TcpClient allows player 2 to connect to the host

async Task HostGame() //HostGame() - runs the game (controls snake movement, scores, food, and collisions)
{
    TcpListener server = new(IPAddress.Any, port);
    server.Start();

    Console.WriteLine("\nWaiting for Player 2...");

    using TcpClient client = await server.AcceptTcpClientAsync();
    using NetworkStream stream = client.GetStream();
    using StreamReader reader = new(stream, Encoding.UTF8);
    using StreamWriter writer = new(stream, Encoding.UTF8)
    {
        AutoFlush = true
    };

    Console.WriteLine("Player 2 connected!");
    await writer.WriteLineAsync("START");

    List<(int x, int y)> snake1 = new()
    {
        (5, 5), (4, 5), (3, 5)
    };

    List<(int x, int y)> snake2 = new()
    {
        (24, 10), (25, 10), (26, 10)
    };

    (int x, int y) direction1 = (1, 0);
    (int x, int y) direction2 = (-1, 0);

    Random random = new();
    (int x, int y) food = (15, 7);

    int score1 = 0;
    int score2 = 0;

    bool gameOver = false;
    string winner = "";

    // Receive Player 2 keyboard input in the background.
    _ = Task.Run(async () =>
    {
        try
        {
            while (client.Connected)
            {
                string? input = await reader.ReadLineAsync();

                if (input == null)
                    break;

                direction2 = ChangeDirection(direction2, input);
            }
        }
        catch
        {
            // Connection closed.
        }
    });


    Console.Clear();
    Console.WriteLine("PLAYER 2 CONNECTED!");
    Console.WriteLine();
    Console.WriteLine("PLAYER 1: WASD");
    Console.WriteLine("PLAYER 2: WASD");
    Console.WriteLine();
    Console.WriteLine("Press ENTER to start the game!");

    while (Console.ReadKey(true).Key != ConsoleKey.Enter)
    {
    }
    while (!gameOver)
    {
        // Player 1 keyboard input.
        while (Console.KeyAvailable)
        {
            ConsoleKey key = Console.ReadKey(true).Key;

            direction1 = ChangeDirection(
                direction1,
                key.ToString()
            );
        }

        (int x, int y) next1 =
            (snake1[0].x + direction1.x,
             snake1[0].y + direction1.y);

        (int x, int y) next2 =
            (snake2[0].x + direction2.x,
             snake2[0].y + direction2.y);

        bool eat1 = next1 == food;
        bool eat2 = next2 == food;

        bool crash1 =
            Outside(next1) ||
            snake1.Take(eat1 ? snake1.Count : snake1.Count - 1)
                  .Contains(next1) ||
            snake2.Take(eat2 ? snake2.Count : snake2.Count - 1)
                  .Contains(next1);

        bool crash2 =
            Outside(next2) || //Outside() - checks if the snake hit the wall
            snake2.Take(eat2 ? snake2.Count : snake2.Count - 1)
                  .Contains(next2) ||
            snake1.Take(eat1 ? snake1.Count : snake1.Count - 1)
                  .Contains(next2);

        if (next1 == next2 ||
            (next1 == snake2[0] && next2 == snake1[0]))
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
                do
                {
                    food = (
                        random.Next(width),
                        random.Next(height)
                    );
                }
                while (snake1.Contains(food) ||
                       snake2.Contains(food));
            }
        }

        string board = BuildBoard( //BuildBoard() - creates the game board using characters
            snake1, snake2, food,
            score1, score2, winner
        );

        Console.Clear();
        Console.Write(board);

        try
        {
            await writer.WriteLineAsync(
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(board)
                )
            );
        }
        catch
        {
            Console.WriteLine("\nPlayer 2 disconnected.");
            break;
        }

        await Task.Delay(200);
    }

    server.Stop();
}


// ======================
// JOIN / CLIENT
// ======================

async Task JoinGame() // JoinGame - sends player 2's controls to the host (displays the game as well)
{
    Console.Write("Enter Host IP address: ");
    string ip = Console.ReadLine() ?? "";

    using TcpClient client = new();

    try
    {
        await client.ConnectAsync(ip, port);
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

    string? start = await reader.ReadLineAsync();

    if (start != "START")
        return;

    // Send Player 2 keyboard input to the host.
    _ = Task.Run(async () =>  //Task.Run() - allows keyword input n network communication to run in the bg
    {
        try
        {
            while (client.Connected)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKey key = Console.ReadKey(true).Key;

                    await writer.WriteLineAsync(
                        key.ToString()
                    );
                }

                await Task.Delay(20);
            }
        }
        catch
        {
            // Connection closed.
        }
    });

    // Display the board received from the host.
    try
    {
        while (true)
        {
            string? message = await reader.ReadLineAsync();

            if (message == null)
                break;

            string board = Encoding.UTF8.GetString(
                Convert.FromBase64String(message)
            );

            Console.Clear();
            Console.Write(board);

            if (board.Contains("WINS!") ||
                board.Contains("DRAW!"))
                break;
        }
    }
    catch
    {
        Console.WriteLine("\nConnection lost.");
    }
}


// ======================
// GAME HELPERS
// ======================

bool Outside((int x, int y) point)
{
    return point.x < 0 ||
           point.x >= width ||
           point.y < 0 ||
           point.y >= height;
}

//ChangeDirection - WASD thing (arrows as well)
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

    // Prevent reversing directly into yourself.
    if (next.x == -current.x &&
        next.y == -current.y)
        return current;

    return next;
}

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
    sb.AppendLine("Controls: WASD or Arrow Keys");
    sb.AppendLine();

    sb.AppendLine("+" + new string('-', width) + "+");

    for (int y = 0; y < height; y++)
    {
        sb.Append('|');

        for (int x = 0; x < width; x++)
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

    sb.AppendLine("+" + new string('-', width) + "+");

    sb.AppendLine("O = Player 1");
    sb.AppendLine("X = Player 2");
    sb.AppendLine("* = Food");

    if (!string.IsNullOrEmpty(winner))
    {
        sb.AppendLine();
        sb.AppendLine("GAME OVER!");
        sb.AppendLine(winner);
    }

    return sb.ToString();
}
