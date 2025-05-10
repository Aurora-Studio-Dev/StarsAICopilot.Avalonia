using System;

private class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object parameter) => _execute(parameter);

    // 使用 WeakEventManager 替代 CommandManager 实现事件管理
    private static readonly WeakEventManager<RelayCommand, EventArgs> CanExecuteChangedEventManager 
        = new WeakEventManager<RelayCommand, EventArgs>();

    public event EventHandler CanExecuteChanged
    {
        add => CanExecuteChangedEventManager.AddEventHandler(this, value);
        remove => CanExecuteChangedEventManager.RemoveEventHandler(this, value);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChangedEventManager.HandleEvent(this, EventArgs.Empty, "CanExecuteChanged");
    }
}

public ChatPage()
{
    InitializeComponent();
    DataContext = new ChatViewModel(); // 确保 DataContext 正确设置
}

public class ChatViewModel : INotifyPropertyChanged
{
    private ObservableCollection<Session> _sessions;
    private Session _selectedSession;

    public ObservableCollection<Session> Sessions
    {
        get => _sessions;
        set
        {
            _sessions = value;
            OnPropertyChanged(nameof(Sessions));
        }
    }

    public Session SelectedSession
    {
        get => _selectedSession;
        set
        {
            _selectedSession = value;
            OnPropertyChanged(nameof(SelectedSession));
        }
    }

    public ChatViewModel()
    {
        // 初始化 Sessions 数据源
        Sessions = new ObservableCollection<Session>
        {
            new Session { Title = "默认会话" },
            new Session { Title = "会话 2" }
        };

        // 默认选中第一个会话
        SelectedSession = Sessions.FirstOrDefault();

        // 修改 SendMessage 方法签名以匹配 RelayCommand 的要求
        SendCommand = new RelayCommand(_ => SendMessage(), () => !string.IsNullOrWhiteSpace(UserInput));
    }

    public ICommand SendCommand { get }

    private void SendMessage()
    {
        // 检查 Messages 集合是否为空
        if (Messages == null || Messages.Count == 0)
        {
            // 如果为空，直接添加用户消息和AI回复
            Messages = new ObservableCollection<Message>();
            Messages.Add(new Message { Sender = "用户", Message = UserInput });
            Messages.Add(new Message { Sender = "AI", Message = $"您说的 \"{UserInput}\" 非常有趣！" });
        }
        else
        {
            // 如果不为空，将AI回复添加到集合末尾
            Messages.Add(new Message { Sender = "用户", Message = UserInput });
            Messages.Add(new Message { Sender = "AI", Message = $"您说的 \"{UserInput}\" 非常有趣！" });
        }

        // 清空输入框并保持滚动到底部
        UserInput = string.Empty;
        // 在实际项目中需要添加滚动到底部的逻辑
    }

}

// 定义 Session 类
public class Session
{
    public string Title { get; set; }
}

private record Message
{
    public string Sender { get; init; } = string.Empty; // 将 Role 改为 Sender
    public string Message { get; init; } = string.Empty; // 将 Content 改为 Message
}
