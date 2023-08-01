using System;
using System.Threading;
using System.Windows.Forms;
using TikTokLiveSharp.Client;
using TikTokLiveSharp.Events.MessageData.Messages;

namespace TikTokLiveSharpTestWinFormsApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string hostName = textBox1.Text;
            var client = new TikTokLiveClient(hostName);
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnViewerData += Client_OnViewerData;
            client.OnLiveEnded += Client_OnLiveEnded;
            client.OnJoin += Client_OnJoin;
            client.OnComment += Client_OnComment;
            client.OnFollow += Client_OnFollow;
            client.OnShare += Client_OnShare;
            client.OnSubscribe += Client_OnSubscribe;
            client.OnLike += Client_OnLike;
            client.OnGiftMessage += Client_OnGiftMessage;
            client.OnEmote += Client_OnEmote;
            client.Start(new CancellationToken());
        }

        private static void Client_OnConnected(TikTokLiveClient sender, bool e)
        {
            SetConsoleColor(ConsoleColor.White);
            Console.WriteLine($"Connected to Room! [Connected:{e}]");
        }

        private static void Client_OnDisconnected(TikTokLiveClient sender, bool e)
        {
            SetConsoleColor(ConsoleColor.White);
            Console.WriteLine($"Disconnected from Room! [Connected:{e}]");
        }

        private static void Client_OnViewerData(TikTokLiveClient sender, RoomViewerData e)
        {
            SetConsoleColor(ConsoleColor.Cyan);
            Console.WriteLine($"Viewer count is: {e.ViewerCount}");
            SetConsoleColor(ConsoleColor.White);
        }

        private static void Client_OnLiveEnded(TikTokLiveClient sender, EventArgs e)
        {
            SetConsoleColor(ConsoleColor.White);
            Console.WriteLine("Host ended Stream!");
        }

        private static void Client_OnJoin(TikTokLiveClient sender, Join e)
        {
            SetConsoleColor(ConsoleColor.Green);
            Console.WriteLine($"{e.User.UniqueId} joined!");
            SetConsoleColor(ConsoleColor.White);
        }

        private static void Client_OnComment(TikTokLiveClient sender, Comment e)
        {
            SetConsoleColor(ConsoleColor.Yellow);
            Console.WriteLine($"{e.User.UniqueId}: {e.Text}");
            SetConsoleColor(ConsoleColor.White);
        }

        private static void Client_OnFollow(TikTokLiveClient sender, Follow e)
        {
            SetConsoleColor(ConsoleColor.DarkRed);
            Console.WriteLine($"{e.NewFollower?.UniqueId} followed!");
            SetConsoleColor(ConsoleColor.White);
        }

        private static void Client_OnShare(TikTokLiveClient sender, Share e)
        {
            SetConsoleColor(ConsoleColor.Blue);
            Console.WriteLine($"{e.User?.UniqueId} shared!");
            SetConsoleColor(ConsoleColor.White);
        }

        private static void Client_OnSubscribe(TikTokLiveClient sender, Subscribe e)
        {
            SetConsoleColor(ConsoleColor.DarkCyan);
            Console.WriteLine($"{e.NewSubscriber.UniqueId} subscribed!");
            SetConsoleColor(ConsoleColor.White);
        }

        private static void Client_OnLike(TikTokLiveClient sender, Like e)
        {
            SetConsoleColor(ConsoleColor.Red);
            Console.WriteLine($"{e.Sender.UniqueId} liked!");
            SetConsoleColor(ConsoleColor.White);
        }

        private static void Client_OnGiftMessage(TikTokLiveClient sender, GiftMessage e)
        {
            SetConsoleColor(ConsoleColor.Magenta);
            Console.WriteLine($"{e.Sender.UniqueId} sent {e.Amount}x {e.Gift.Name}!");
            SetConsoleColor(ConsoleColor.White);
        }

        private static void Client_OnEmote(TikTokLiveClient sender, Emote e)
        {
            SetConsoleColor(ConsoleColor.DarkGreen);
            Console.WriteLine($"{e.User.UniqueId} sent {e.EmoteId}!");
            SetConsoleColor(ConsoleColor.White);
        }

        private static void SetConsoleColor(ConsoleColor color)
        {
            if (Console.ForegroundColor != color)
                Console.ForegroundColor = color;
        }
    }
}
