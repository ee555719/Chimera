// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chimera.Abstractions.Models;
using Chimera.PluginLoader;

namespace Chimera.Host.Pages;

public partial class AiAssistantPage : UserControl
{
    private readonly AiAssistantService _aiService;

    public AiAssistantPage()
    {
        InitializeComponent();
        _aiService = new AiAssistantService();
        _aiService.OnResponseReceived += AiService_OnResponseReceived;
        _aiService.OnError += AiService_OnError;
        
        // Add welcome message
        AddWelcomeMessage();
        
        Loaded += AiAssistantPage_Loaded;
    }

    private void AiAssistantPage_Loaded(object sender, RoutedEventArgs e)
    {
        MessageInput.Focus();
    }

    private void AddWelcomeMessage()
    {
        var welcomeMessage = new ChatMessageDisplay
        {
            Role = "assistant",
            Content = "欢迎使用 Chimera AI 助手！\n\n我可以帮助您：\n• 管理插件\n• 提供系统信息\n• 回答使用问题\n• 推荐插件\n\n请输入您的问题或尝试输入「帮助」查看可用命令。",
            IsUserMessage = false
        };
        
        ChatMessagesList.Items.Add(welcomeMessage);
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    private async void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(MessageInput.Text))
        {
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        var message = MessageInput.Text.Trim();
        if (string.IsNullOrEmpty(message))
            return;

        // Add user message to UI
        var userMessage = new ChatMessageDisplay
        {
            Role = "user",
            Content = message,
            IsUserMessage = true
        };
        ChatMessagesList.Items.Add(userMessage);

        // Clear input
        MessageInput.Text = string.Empty;

        // Scroll to bottom
        ChatScrollViewer.ScrollToEnd();

        try
        {
            // Create context
            var context = _aiService.CreateDefaultContext();
            
            // Get response
            var response = await _aiService.SendMessageAsync(message, context);

            // Add response to UI
            var assistantMessage = new ChatMessageDisplay
            {
                Role = "assistant",
                Content = response.Message.Content,
                IsUserMessage = false
            };
            ChatMessagesList.Items.Add(assistantMessage);

            // Update suggestions
            UpdateSuggestions(response.Suggestions);

            // Scroll to bottom
            ChatScrollViewer.ScrollToEnd();
        }
        catch (Exception ex)
        {
            var errorMessage = new ChatMessageDisplay
            {
                Role = "assistant",
                Content = $"抱歉，出现错误：{ex.Message}",
                IsUserMessage = false
            };
            ChatMessagesList.Items.Add(errorMessage);
        }
    }

    private void UpdateSuggestions(List<string> suggestions)
    {
        SuggestionsList.Items.Clear();
        foreach (var suggestion in suggestions)
        {
            SuggestionsList.Items.Add(suggestion);
        }
    }

    private async void Suggestion_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Content is string suggestion)
        {
            MessageInput.Text = suggestion;
            await SendMessageAsync();
        }
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        ChatMessagesList.Items.Clear();
        _aiService.ClearChatHistory();
        AddWelcomeMessage();
        SuggestionsList.Items.Clear();
    }

    private void AiService_OnResponseReceived(object? sender, AiChatResponse e)
    {
        // Response already handled in SendMessageAsync
    }

    private void AiService_OnError(object? sender, string e)
    {
        Dispatcher.Invoke(() =>
        {
            var errorMessage = new ChatMessageDisplay
            {
                Role = "assistant",
                Content = $"错误：{e}",
                IsUserMessage = false
            };
            ChatMessagesList.Items.Add(errorMessage);
        });
    }
}

public class ChatMessageDisplay
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsUserMessage { get; set; }
    public string RoleDisplay => Role == "user" ? "您" : "AI 助手";
}
