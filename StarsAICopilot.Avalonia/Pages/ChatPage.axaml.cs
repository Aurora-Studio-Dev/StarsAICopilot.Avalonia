using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using Newtonsoft.Json;
using MarkdownAIRender.Controls.MarkdownRender;
using StarsAICopilot.Avalonia.Helper;

namespace StarsAICopilot.Avalonia.Pages;

public partial class ChatPage : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    private static readonly HttpClient HttpClient = new();

    private bool _isProcessingCurrentSession = false;

    public class ChatMessage
    {
        public string Content { get; set; }
        public bool IsReceived { get; set; }
    }

    public class ChatSession : INotifyPropertyChanged
    {
        private string _title = "新对话";
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<ChatMessage> Messages { get; } = new();
        public string Id { get; } = Guid.NewGuid().ToString();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public ObservableCollection<ChatSession> Sessions { get; } = new();
    
    private ChatSession _selectedSession;
    public ChatSession SelectedSession
    {
        get => _selectedSession;
        set
        {
            if (SetField(ref _selectedSession, value))
            {
                _isProcessingCurrentSession = false;
                MsgShow.Items.Clear();
                if (value != null)
                {
                    foreach (var msg in value.Messages)
                    {
                        AddMessageBubble(msg.Content, msg.IsReceived);
                    }
                }
                OnPropertyChanged(nameof(Messages));  // 触发关联属性更新
            }
        }
    }


    public ObservableCollection<ChatMessage> Messages => SelectedSession?.Messages ?? new ObservableCollection<ChatMessage>();

    private string _apiKey = ConfigHelper.CurrentConfig.ApiKey;
    private string _apiUrl = ConfigHelper.CurrentConfig.ApiUrl;
    private string _mod = ConfigHelper.CurrentConfig.Mod;
    
    private static readonly string _storagePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "StarsAICopilot", 
        "sessions.json");
    public ChatPage()
    {
        InitializeComponent();
        DataContext = this;
    
        LoadSessions();
    
        if (!Sessions.Any())
        {
            var session = new ChatSession();
            Sessions.Add(session);
            SelectedSession = session;
        }
    
        SessionList.SelectedItem = SelectedSession;
    }

    public void CreateNewSession(object? sender, RoutedEventArgs e)
    {
        var session = new ChatSession { Title = $"对话 {Sessions.Count + 1}" };
        Sessions.Add(session);
        SessionList.SelectedItem = session;
    }

    private async void Send(object? sender, RoutedEventArgs e)
    {
        if (_isProcessingCurrentSession) return;
        _isProcessingCurrentSession = true;
        try
        {
            var userInput = UserInput.Text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            AddMessage(userInput, false);
            UserInput.Text = string.Empty;

            var response = await GetGptResponseAsync();
            AddMessage(response, true); 
        }
        catch (Exception ex)
        {
            AddMessage($"⚠️ 发生错误: {ex.Message}", true);
        }
        finally
        {
            _isProcessingCurrentSession = false;
        }
    }

    private async Task<string> GetGptResponseAsync()
    {
        try
        {
            using var request = CreateApiRequest();
            using var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return $"⚠️ 请求失败({response.StatusCode}): {error}";
            }

            var json = await response.Content.ReadAsStringAsync();
            return ParseApiResponse(json) ?? "⚠️ 无法解析响应内容";
        }
        catch (Exception ex)
        {
            return $"⚠️ 网络请求异常: {ex.Message}";
        }
    }

    private HttpRequestMessage CreateApiRequest()
    {
        var messages = SelectedSession.Messages.Select(msg => new
        {
            role = !msg.IsReceived ? "user" : "assistant",
            content = msg.Content
        });

        var requestBody = new
        {
            model = _mod,
            messages,
            temperature = 0.7
        };

        var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
        {
            Content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return request;
    }

    private static string ParseApiResponse(string json)
    {
        try
        {
            dynamic obj = JsonConvert.DeserializeObject(json);
            return obj?.choices?[0]?.message?.content?.ToString();
        }
        catch
        {
            return null;
        }
    }
    private void AddMessage(string content, bool isReceived)
    {
        var message = new ChatMessage
        {
            Content = content,
            IsReceived = isReceived
        };

        SelectedSession.Messages.Add(message);
        AddMessageBubble(content, isReceived);
        SaveSessions();
    }

    private void AddMessageBubble(string content, bool isReceived)
    {
        var bubble = new Border
        {
            Background = isReceived ? Brushes.White : Brushes.DodgerBlue,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8),
            Margin = new Thickness(5),
            HorizontalAlignment = isReceived ? HorizontalAlignment.Left : HorizontalAlignment.Right,
            MaxWidth = 500,
            Child = new MarkdownRender 
            { 
                Value = content,
                Foreground = isReceived ? Brushes.Black : Brushes.White
            }
        };

        MsgShow.Items.Add(bubble);
    }

    public void DeleteSession(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ChatSession session })
        {
            Sessions.Remove(session);
            SaveSessions();
            if (Sessions.Count == 0)
            {
                var newSession = new ChatSession();
                Sessions.Add(newSession);
            }
            SelectedSession = Sessions.Last();
        }
    }

    private void SaveSessions()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_storagePath));
            var sessionsSnapshot = Sessions.Select(s => new 
            {
                s.Id,
                s.Title,
                Messages = new List<ChatMessage>(s.Messages)
            }).ToList();
            File.WriteAllText(_storagePath, JsonConvert.SerializeObject(sessionsSnapshot));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存会话失败: {ex.Message}");
        }
    }
    
    private void LoadSessions()
    {
        try
        {
            if (File.Exists(_storagePath))
            {
                var content = File.ReadAllText(_storagePath);
                var sessions = JsonConvert.DeserializeObject<List<dynamic>>(content);
                
                foreach (var s in sessions)
                {
                    var session = new ChatSession { 
                        Title = s.Title
                    };
                    foreach (var msg in s.Messages)
                    {
                        session.Messages.Add(new ChatMessage 
                        { 
                            Content = msg.Content, 
                            IsReceived = msg.IsReceived 
                        });
                    }
                    Sessions.Add(session);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载会话失败: {ex.Message}");
        }
        SessionList.SelectedIndex = 0;
    }
    
    public void RenameSession(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && textBox.DataContext is ChatSession session)
        {
            session.Title = textBox.Text.Trim();
            SaveSessions();  
        }
    }

    private void OnTitleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
        {
            RenameSession(textBox, null);
            var next = KeyboardNavigationHandler.GetNext(textBox, NavigationDirection.Next);
            next?.Focus();
        }
    }
}