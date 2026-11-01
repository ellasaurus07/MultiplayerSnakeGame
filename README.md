🐍 Multiplayer Snake Game

A two-player multiplayer Snake game developed using C# and .NET, featuring real-time movement, TCP networking, food collection, collision detection, scoring, and a replay voting system.

This project demonstrates how two players can communicate and play together over a network using a client-server architecture.

🎮 Game Features

* Two-Player Multiplayer: Two players can compete on separate computers connected to the same local network.
* TCP Networking: Uses TCP sockets to exchange game information between the host and client.
* Real-Time Movement: Both players control their snakes using keyboard inputs.
* Food Collection: Collect food (*) to grow your snake and increase your score.
* Collision Detection: The game detects collisions with walls, snake bodies, and other snakes.
* Winner Announcement: Displays the winner or declares a draw when both players crash.
* Replay Voting: After each round, both players can vote whether to continue.
* Snake Respawn: If both players vote Yes, their snakes respawn and the scores reset.
* Improved Performance: Faster movement and reduced screen flickering.

🛠️ Technologies Used

* C#
* .NET 10
* Visual Studio Community
* TCP/IP Socket Programming
* Console Application
* Git and GitHub

🎯 How to Play

Player 1 — Host

1. Run the application.
2. Select 1 to host the game.
3. Wait for Player 2 to connect.
4. Press Enter to start the round.
5. Control your snake using the keyboard.

Player 2 — Client

1. Run the application on another computer.
2. Select 2 to join the game.
3. Enter Player 1’s IPv4 address.
4. Wait for the host to start.
5. Control your snake using the keyboard.

⌨️ Controls

Action	Keys
Move Up	W / ↑
Move Down	S / ↓
Move Left	A / ←
Move Right	D / →
Vote Yes	Y
Vote No	N

Each player uses the keyboard on their own computer.

🌐 Multiplayer Connection

The game uses a client-server architecture.

* Player 1 acts as the host/server.
* Player 2 connects as the client.
* The game communicates using TCP port 5000.
* The host manages game logic and sends the updated game board to the client.

Playing on the Same Computer

Enter the following IP address when joining:

127.0.0.1

Playing on Different Computers

Both computers should be connected to the same local network.

On the host computer:

1. Open Command Prompt.
2. Type ipconfig.
3. Find the IPv4 address.
4. Enter that address on Player 2’s computer.

Note: Network firewall settings may need to allow the application to communicate over TCP port 5000.

🍎 Game Rules

1. Each player controls a snake that moves continuously.
2. Collect food (*) to earn points and grow longer.
3. Avoid crashing into walls, your own snake, or the opponent’s snake.
4. If one snake crashes, the other player wins.
5. If both snakes crash simultaneously, the game ends in a draw.

🔄 Replay Voting System

After a round ends, both players are asked:

PLAY AGAIN?

* Press Y to vote Yes.
* Press N to vote No.

Player 1	Player 2	Result
Yes	Yes	New round starts
Yes	No	Game ends
No	Yes	Game ends
No	No	Game ends

If both players vote Yes, the snakes return to their starting positions and scores reset. The host then starts the next round.

💻 How to Run the Project

Requirements

* Windows computer
* .NET 10 SDK
* Visual Studio Community with .NET development support

Installation

1. Clone the repository:

git clone https://github.com/ellasaurus07/MultiplayerSnakeGame.git

2. Open the project in Visual Studio.
3. Build the project.
4. Run the application.
5. Choose whether to host or join a game.

🧠 Programming Concepts Demonstrated

This project applies several programming concepts:

* Object and data management: Snake positions, movement directions, food, and scores.
* Lists and tuples: Store and update snake body coordinates.
* Loops and conditional statements: Control gameplay and determine collisions.
* Asynchronous programming: Handle network communication and keyboard input.
* TCP socket programming: Connect two players over a network.
* Client-server architecture: The host controls the game state while the client receives updates.
* Game state management: Handle gameplay, game over, replay voting, and restarting rounds.

🚀 Possible Future Improvements

* Graphical user interface
* Sound effects
* Difficulty levels
* Additional game modes
* Persistent high scores
* Improved animations

👩‍💻 Developer

GitHub: ellasaurus07

Developed as a C# programming project to explore multiplayer game development, networking, and real-time gameplay.

⸻

Built with C# and a love for learning game development. 🐍
