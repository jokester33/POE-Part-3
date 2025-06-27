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
        // TASK ASSISTANT 

        private bool TryHandleTaskCommands(string input)
        {
            if (input.StartsWith("add task") || input.StartsWith("add a task") || input.StartsWith("add reminder") || input.StartsWith("remind me"))
            {
                AddChatbotMessage("Please add your task details using the 'Tasks' tab.");
                MainTabControl.SelectedIndex = 1; // Tasks tab
                AddActivity("User requested to add a task.");
                return true;
            }

            return false;
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleTextBox.Text.Trim();
            string desc = TaskDescriptionTextBox.Text.Trim();
            DateTime? reminder = TaskReminderDatePicker.SelectedDate;

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Task Title is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var task = new TaskItem
            {
                Title = title,
                Description = desc,
                Reminder = reminder,
                IsCompleted = false
            };

            tasks.Add(task);
            RefreshTaskList();
            AddActivity($"Added task: '{title}'" + (reminder.HasValue ? $" with reminder on {reminder.Value.ToShortDateString()}" : ""));

            // Clear inputs
            TaskTitleTextBox.Clear();
            TaskDescriptionTextBox.Clear();
            TaskReminderDatePicker.SelectedDate = null;

            AddChatbotMessage($"Task added: '{task.Title}'. Would you like to set a reminder for this task?");
        }

        void RefreshTaskList()
        {
            TasksListView.ItemsSource = null;
            TasksListView.ItemsSource = tasks;
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TaskItem task)
            {
                tasks.Remove(task);
                RefreshTaskList();
                AddActivity($"Deleted task: '{task.Title}'");
            }
        }

        private void TaskCompleted_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is TaskItem task)
            {
                task.IsCompleted = true;
                AddActivity($"Marked task as completed: '{task.Title}'");
            }
        }

        private void TaskCompleted_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is TaskItem task)
            {
                task.IsCompleted = false;
                AddActivity($"Marked task as incomplete: '{task.Title}'");
            }
        }

        // QUIZ LOGIC

        void InitQuizQuestions()
        {
            quizQuestions = new List<QuizQuestion>
            {
                new QuizQuestion
                {
                    Question = "What should you do if you receive an email asking for your password?",
                    Answers = new List<string> { "Reply with your password", "Delete the email", "Report the email as phishing", "Ignore it" },
                    CorrectIndex = 2,
                    IsTrueFalse = false,
                    Explanation = "Correct! Reporting phishing emails helps prevent scams."
                },
                new QuizQuestion
                {
                    Question = "True or False: You should never share your passwords with anyone.",
                    Answers = new List<string> { "True", "False" },
                    CorrectIndex = 0,
                    IsTrueFalse = true,
                    Explanation = "Correct! Passwords should always be kept private."
                },
                new QuizQuestion
                {
                    Question = "Which is the strongest password?",
                    Answers = new List<string> { "password123", "MyName2023", "G!7$w9#X", "123456" },
                    CorrectIndex = 2,
                    IsTrueFalse = false,
                    Explanation = "Correct! A strong password contains a mix of symbols, letters, and numbers."
                },
                new QuizQuestion
                {
                    Question = "What is phishing?",
                    Answers = new List<string> { "A sport", "A scam to get sensitive information", "A programming language", "A type of virus" },
                    CorrectIndex = 1,
                    IsTrueFalse = false,
                    Explanation = "Correct! Phishing is a scam to trick users into giving out personal info."
                },
                new QuizQuestion
                {
                    Question = "Using the same password for multiple accounts is:",
                    Answers = new List<string> { "Recommended", "Risky and unsafe" },
                    CorrectIndex = 1,
                    IsTrueFalse = true,
                    Explanation = "Correct! Reusing passwords puts you at risk."
                },
                new QuizQuestion
                {
                    Question = "VPN stands for:",
                    Answers = new List<string> { "Virtual Private Network", "Very Private Network" },
                    CorrectIndex = 0,
                    IsTrueFalse = true,
                    Explanation = "Correct! VPN encrypts your internet connection."
                },
                new QuizQuestion
                {
                    Question = "True or False: Public Wi-Fi is always safe to use without precautions.",
                    Answers = new List<string> { "True", "False" },
                    CorrectIndex = 1,
                    IsTrueFalse = true,
                    Explanation = "Correct! Public Wi-Fi can be insecure."
                },
                new QuizQuestion
                {
                    Question = "What is multi-factor authentication?",
                    Answers = new List<string> { "Logging in with username only", "Using multiple methods to verify identity", "Using one password for all sites" },
                    CorrectIndex = 1,
                    IsTrueFalse = false,
                    Explanation = "Correct! MFA increases account security."
                },
                new QuizQuestion
                {
                    Question = "Malware is:",
                    Answers = new List<string> { "A helpful tool", "Malicious software", "A type of hardware" },
                    CorrectIndex = 1,
                    IsTrueFalse = false,
                    Explanation = "Correct! Malware is harmful software."
                },
                new QuizQuestion
                {
                    Question = "Safe browsing means:",
                    Answers = new List<string> { "Visiting only secure https websites", "Clicking any link", "Ignoring browser warnings" },
                    CorrectIndex = 0,
                    IsTrueFalse = false,
                    Explanation = "Correct! Only visit secure sites."
                }
            };
        }
        private void StartQuizButton_Click(object sender, RoutedEventArgs e)
        {
            quizScore = 0;
            currentQuizIndex = 0;
            QuizScoreTextBlock.Visibility = Visibility.Collapsed;
            QuizFeedbackTextBlock.Text = "";
            NextQuestionButton.IsEnabled = false;
            StartQuizButton.IsEnabled = false;
            MainTabControl.SelectedIndex = 2; // Stay on Quiz tab

            ShowQuestion();
            AddActivity("Quiz started.");
        }

        void ShowQuestion()
        {
            if (currentQuizIndex >= quizQuestions.Count)
            {
                ShowQuizResult();
                return;
            }

            QuizQuestion q = quizQuestions[currentQuizIndex];
            QuizQuestionTextBlock.Text = q.Question;

            QuizAnswersPanel.Children.Clear();

            for (int i = 0; i < q.Answers.Count; i++)
            {
                var btn = new Button
                {
                    Content = q.Answers[i],
                    Tag = i,
                    Margin = new Thickness(5),
                    MinWidth = 200
                };
                btn.Click += QuizAnswerButton_Click;
                QuizAnswersPanel.Children.Add(btn);
            }
            QuizFeedbackTextBlock.Text = "";
        }
        private void QuizAnswerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int answerIndex)
            {
                QuizQuestion q = quizQuestions[currentQuizIndex];
                if (answerIndex == q.CorrectIndex)
                {
                    quizScore++;
                    QuizFeedbackTextBlock.Text = q.Explanation;
                }
                else
                {
                    QuizFeedbackTextBlock.Text = $"Wrong. {q.Explanation}";
                }

                // Disable buttons
                foreach (Button b in QuizAnswersPanel.Children)
                    b.IsEnabled = false;

                NextQuestionButton.IsEnabled = true;

                AddActivity($"Quiz question {currentQuizIndex + 1} answered.");
            }
        }

        private void NextQuestionButton_Click(object sender, RoutedEventArgs e)
        {
            currentQuizIndex++;
            NextQuestionButton.IsEnabled = false;

            if (currentQuizIndex < quizQuestions.Count)
                ShowQuestion();
            else
                ShowQuizResult();
        }

        void ShowQuizResult()
        {
            QuizQuestionTextBlock.Text = "Quiz complete!";
            QuizAnswersPanel.Children.Clear();
            QuizFeedbackTextBlock.Text = $"You scored {quizScore} out of {quizQuestions.Count}.";
            QuizScoreTextBlock.Visibility = Visibility.Visible;
            QuizScoreTextBlock.Text = quizScore switch
            {
                >= 9 => "Great job! You're a cybersecurity pro!",
                >= 6 => "Good effort! Keep learning to stay safe online!",
                _ => "Keep practicing to improve your cybersecurity knowledge."
            };
            StartQuizButton.IsEnabled = true;

            AddActivity($"Quiz completed with score {quizScore}/{quizQuestions.Count}.");
        }



