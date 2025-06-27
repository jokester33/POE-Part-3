using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CyberSecurityChatbotWPF
{
    public partial class MainWindow : Window
    {
        // Chatbot Data 
        delegate string TopicHandler(string input);

        readonly Dictionary<string, List<string>> topicTips = new()
        {
            { "phishing", new List<string> {
                "Be cautious of unsolicited emails or links asking for personal information.",
                "Verify the sender's address and look for spelling mistakes or urgent language.",
                "If something feels off, contact the organisation directly rather than using embedded links."
            }},
            { "password", new List<string> {
                "Use a mix of uppercase, lowercase, numbers, and symbols in your passwords.",
                "Never reuse the same password across multiple accounts.",
                "Consider using a reputable password manager to keep track of your credentials."
            }},
            { "privacy", new List<string> {
                "Review and limit app permissions on your devices.",
                "Keep your social media profiles private and think twice before sharing personal info.",
                "Enable multi-factor authentication wherever possible."
            }}
        };

        readonly Dictionary<string, TopicHandler> handlers;
        string currentTopic = null;
        string favoriteTopic = "";
        readonly Random rnd = new();

        //  Task Assistant Data 
        public class TaskItem
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime? Reminder { get; set; }
            public bool IsCompleted { get; set; }
            public string ReminderString => Reminder.HasValue ? Reminder.Value.ToShortDateString() : "";
        }
        List<TaskItem> tasks = new();

        // Activity Log Data 
        public class ActivityLogEntry
        {
            public DateTime Time { get; set; }
            public string Description { get; set; }
            public override string ToString() => $"{Time:T}: {Description}";
        }
        List<ActivityLogEntry> activityLog = new();

        // Quiz Data 
        public class QuizQuestion
        {
            public string Question { get; set; }
            public List<string> Answers { get; set; }
            public int CorrectIndex { get; set; }
            public bool IsTrueFalse { get; set; }
            public string Explanation { get; set; }
        }
        List<QuizQuestion> quizQuestions = new();
        int currentQuizIndex = -1;
        int quizScore = 0;

        public MainWindow()
        {
            InitializeComponent();

            // Init chatbot handlers
            handlers = new Dictionary<string, TopicHandler>()
            {
                { "phishing", HandlePhishing },
                { "password", HandlePassword },
                { "privacy",  HandlePrivacy  }
            };

            InitQuizQuestions();

            // Show welcome message in chatbot panel
            AddChatbotMessage("Hello! Welcome to the Cybersecurity Awareness Bot. How can I help you today?");
        }

        //  CHATBOT LOGIC 

        string HandlePhishing(string _) { currentTopic = "phishing"; return topicTips["phishing"][rnd.Next(topicTips["phishing"].Count)]; }
        string HandlePassword(string _) { currentTopic = "password"; return topicTips["password"][rnd.Next(topicTips["password"].Count)]; }
        string HandlePrivacy(string _)
        {
            currentTopic = "privacy";
            if (string.IsNullOrEmpty(favoriteTopic))
            {
                favoriteTopic = "privacy";
                AddChatbotMessage("Great! I'll remember that you're interested in privacy.");
            }
            return topicTips["privacy"][rnd.Next(topicTips["privacy"].Count)];
        }

        string DetectSentiment(string input)
        {
            if (input.Contains("worried") || input.Contains("scared") || input.Contains("anxious"))
                return "worried";
            if (input.Contains("curious") || input.Contains("interested"))
                return "curious";
            if (input.Contains("frustrated") || input.Contains("angry"))
                return "frustrated";
            return "neutral";
        }

        string GetSentimentPrefix(string sentiment) => sentiment switch
        {
            "worried" => "It's completely understandable to feel that way. ",
            "curious" => "I'm glad you're curious! ",
            "frustrated" => "I hear you—it can be overwhelming. ",
            _ => ""
        };

        bool IsMostlyNumbers(string s)
        {
            var stripped = s.Replace(" ", "");
            int digits = 0;
            foreach (var c in stripped) if (char.IsDigit(c)) digits++;
            return stripped.Length > 0 && (double)digits / stripped.Length > 0.6;
        }

        string GetDynamicResponse(string input, string userName)
        {
            var lower = input.ToLower();

            if (IsMostlyNumbers(lower))
                return "I think you entered mostly numbers—could you rephrase that in a sentence?";

            var sentiment = DetectSentiment(lower);
            var prefix = GetSentimentPrefix(sentiment);

            if ((lower.Contains("more") || lower.Contains("another") || lower.Contains("again"))
                && !string.IsNullOrEmpty(currentTopic)
                && handlers.ContainsKey(currentTopic))
            {
                var tip = handlers[currentTopic](lower);
                return prefix + tip;
            }

            foreach (var kv in handlers)
            {
                if (lower.Contains(kv.Key))
                {
                    var tip = kv.Value(lower);
                    return prefix + tip;
                }
            }

            if (lower.Contains("remember") && !string.IsNullOrEmpty(favoriteTopic))
            {
                return $"Yes, {userName}, you mentioned you're interested in {favoriteTopic}.";
            }

            return null;
        }

        string GetResponse(string input)
        {
            input = input.ToLower();

            if (input.Contains("phishing") || input.Contains("phising"))
                return "Phishing is when attackers try to trick you into giving out sensitive information. Be cautious of unsolicited emails or links.";
            if (input.Contains("hello") || input.Contains("hi") || input.Contains("hey") || input.Contains("greetings"))
                return "Hello there! How can I assist you with cybersecurity today?";
            if (input.Contains("how are you") || input.Contains("how r u"))
                return "I'm just a bunch of code but I'm running fantastic! Thanks for asking.";
            if (input.Contains("purpose") || input.Contains("what do you do") || input.Contains("why are you here") || input.Contains("goal") || input.Contains("job"))
                return "I'm here to help you stay safe online by providing valuable cybersecurity tips and answering your questions.";
            if (input.Contains("password") || input.Contains("passwords") || input.Contains("safety") || input.Contains("secure"))
                return "Always use strong, unique passwords for each account. A good password should contain a mix of letters, numbers, and symbols.";
            if (input.Contains("safe browsing") || input.Contains("browsing") || input.Contains("browsing security") || input.Contains("browser"))
                return "Make sure to only visit websites that are 'https' secure. Avoid clicking on suspicious links.";
            if (input.Contains("password manager") || input.Contains("manager"))
                return "A password manager securely stores and organizes your passwords, making it easier to use strong, unique passwords without forgetting them.";
            if (input.Contains("malware") || input.Contains("virus") || input.Contains("spyware") || input.Contains("trojan"))
                return "Malware is malicious software designed to harm your system. Make sure your antivirus software is up-to-date and avoid downloading files from untrusted sources.";
            if (input.Contains("vpn") || input.Contains("virtual private network"))
                return "A VPN (Virtual Private Network) encrypts your internet connection and hides your IP address, enhancing your privacy while browsing.";
            if (input.Contains("what can i ask you") || input.Contains("what do you know") || input.Contains("topics") || input.Contains("help") || input.Contains("info"))
                return "You can ask me about cybersecurity topics, such as password safety, phishing, malware, VPNs, safe browsing, and more!";

            return "I didn't quite understand that. Could you please rephrase? I'm here to answer your cybersecurity questions.";
        }

        // CHATBOT UI HANDLERS

        void AddChatbotMessage(string message)
        {
            var tb = new TextBlock { Text = "Bot: " + message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(5), Background = System.Windows.Media.Brushes.LightGray };
            ChatMessagesPanel.Children.Add(tb);
            ScrollChatToEnd();
        }

        void AddUserMessage(string message)
        {
            var tb = new TextBlock { Text = "You: " + message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(5), Background = System.Windows.Media.Brushes.LightBlue };
            ChatMessagesPanel.Children.Add(tb);
            ScrollChatToEnd();
        }

        void ScrollChatToEnd()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            HandleUserInput();
        }

        private void UserInputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                HandleUserInput();
                e.Handled = true;
            }
        }

        void HandleUserInput()
        {
            string input = UserInputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(input)) return;

            AddUserMessage(input);
            UserInputTextBox.Clear();

            string userName = "User"; // could make this customizable

            // NLP simulation — detect commands for task, quiz, reminders, activity log
            string lowerInput = input.ToLower();

            // Handle exit
            if (lowerInput.Contains("exit") || lowerInput.Contains("quit") || lowerInput.Contains("bye"))
            {
                AddChatbotMessage("Goodbye! Stay safe online!");
                return;
            }

            // Check task commands
            if (TryHandleTaskCommands(lowerInput))
                return;

            // Check quiz commands
            if (lowerInput.Contains("quiz"))
            {
                AddChatbotMessage("Switching to Quiz tab. Click 'Start Quiz' to begin!");
                MainTabControl.SelectedIndex = 2; // Quiz tab
                return;
            }

            // Check activity log commands
            if (lowerInput.Contains("activity log") || lowerInput.Contains("what have you done"))
            {
                MainTabControl.SelectedIndex = 3; // Activity Log tab
                ShowActivityLog();
                AddChatbotMessage("Showing recent activity log.");
                return;
            }

            // Otherwise normal chatbot reply
            string dyn = GetDynamicResponse(input, userName);
            if (!string.IsNullOrEmpty(dyn))
            {
                AddChatbotMessage(dyn);
                AddActivity($"Chatbot responded with topic '{currentTopic}'.");
            }
            else
            {
                string resp = GetResponse(input);
                AddChatbotMessage(resp);
                AddActivity("Chatbot gave a fallback response.");
            }
        }

    }
}