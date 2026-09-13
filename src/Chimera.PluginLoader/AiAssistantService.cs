// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Text.Json;
using Chimera.Abstractions.Models;

namespace Chimera.PluginLoader;

public class AiAssistantService
{
    private readonly List<AiChatMessage> _chatHistory = new();
    private readonly PluginStoreService _storeService;
    private readonly PluginPermissionManager? _permissionManager;
    private readonly SecurityAuditLogger? _auditLogger;
    
    private const int MaxHistorySize = 50;

    public event EventHandler<AiChatResponse>? OnResponseReceived;
    public event EventHandler<string>? OnError;

    public AiAssistantService(
        PluginStoreService? storeService = null,
        PluginPermissionManager? permissionManager = null,
        SecurityAuditLogger? auditLogger = null)
    {
        _storeService = storeService ?? new PluginStoreService();
        _permissionManager = permissionManager;
        _auditLogger = auditLogger;
    }

    public async Task<AiChatResponse> SendMessageAsync(string message, AiContext? context = null)
    {
        try
        {
            // Add user message to history
            _chatHistory.Add(new AiChatMessage
            {
                Role = "user",
                Content = message,
                Timestamp = DateTime.UtcNow
            });

            // Process message and generate response
            var response = await ProcessMessageAsync(message, context);
            
            // Add assistant response to history
            _chatHistory.Add(response.Message);
            
            // Trim history if needed
            while (_chatHistory.Count > MaxHistorySize)
            {
                _chatHistory.RemoveAt(0);
            }

            OnResponseReceived?.Invoke(this, response);
            return response;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex.Message);
            throw;
        }
    }

    private async Task<AiChatResponse> ProcessMessageAsync(string message, AiContext? context)
    {
        var lowerMessage = message.ToLowerInvariant();
        var response = new AiChatResponse();

        // Simple pattern matching for demo purposes
        if (lowerMessage.Contains("帮助") || lowerMessage.Contains("help"))
        {
            response.Message = new AiChatMessage
            {
                Role = "assistant",
                Content = "我是 Chimera AI 助手，可以帮助您：\n\n" +
                         "• 安装和管理插件\n" +
                         "• 推荐适合的插件\n" +
                         "• 解决使用问题\n" +
                         "• 提供系统信息\n\n" +
                         "请告诉我您需要什么帮助？",
                Timestamp = DateTime.UtcNow
            };
            response.Suggestions.Add("推荐插件");
            response.Suggestions.Add("系统状态");
            response.Suggestions.Add("安装帮助");
        }
        else if (lowerMessage.Contains("推荐") || lowerMessage.Contains("recommend"))
        {
            var recommendations = await GetPluginRecommendationsAsync(context);
            if (recommendations.Any())
            {
                var recList = string.Join("\n", recommendations.Take(3).Select(r => 
                    $"• {r.Name} - {r.Reason} (匹配度: {r.Confidence:P0})"));
                
                response.Message = new AiChatMessage
                {
                    Role = "assistant",
                    Content = $"根据您的使用情况，推荐以下插件：\n\n{recList}",
                    Timestamp = DateTime.UtcNow
                };
                
                foreach (var rec in recommendations.Take(3))
                {
                    response.Actions.Add(new AiAction
                    {
                        Type = "install_plugin",
                        Target = rec.PluginId,
                        Parameters = new Dictionary<string, string>
                        {
                            ["name"] = rec.Name,
                            ["reason"] = rec.Reason
                        }
                    });
                }
            }
            else
            {
                response.Message = new AiChatMessage
                {
                    Role = "assistant",
                    Content = "目前没有找到适合的插件推荐。您可以尝试：\n\n" +
                             "• 浏览插件市场查看所有可用插件\n" +
                             "• 告诉我您的具体需求",
                    Timestamp = DateTime.UtcNow
                };
            }
        }
        else if (lowerMessage.Contains("状态") || lowerMessage.Contains("status"))
        {
            var statusInfo = GetSystemStatusInfo(context);
            response.Message = new AiChatMessage
            {
                Role = "assistant",
                Content = $"系统状态：\n\n{statusInfo}",
                Timestamp = DateTime.UtcNow
            };
        }
        else if (lowerMessage.Contains("安装") || lowerMessage.Contains("install"))
        {
            response.Message = new AiChatMessage
            {
                Role = "assistant",
                Content = "您可以通过以下方式安装插件：\n\n" +
                         "1. 在「插件市场」中搜索并安装\n" +
                         "2. 告诉我插件名称，我帮您查找\n" +
                         "3. 从本地文件安装 ZIP 包\n\n" +
                         "请告诉我您想安装什么插件？",
                Timestamp = DateTime.UtcNow
            };
            response.Suggestions.Add("搜索插件");
            response.Suggestions.Add("查看市场");
        }
        else if (lowerMessage.Contains("卸载") || lowerMessage.Contains("uninstall"))
        {
            response.Message = new AiChatMessage
            {
                Role = "assistant",
                Content = "要卸载插件，请：\n\n" +
                         "1. 在「已安装」页面找到要卸载的插件\n" +
                         "2. 点击卸载按钮\n" +
                         "3. 确认卸载操作\n\n" +
                         "请注意：卸载后插件数据将被删除。",
                Timestamp = DateTime.UtcNow
            };
        }
        else if (lowerMessage.Contains("问题") || lowerMessage.Contains("problem") || lowerMessage.Contains("错误"))
        {
            response.Message = new AiChatMessage
            {
                Role = "assistant",
                Content = "如果您遇到问题，可以尝试：\n\n" +
                         "1. 重启 Chimera\n" +
                         "2. 检查插件兼容性\n" +
                         "3. 查看「调试器」中的日志\n" +
                         "4. 在 GitHub 上报告问题\n\n" +
                         "请描述您遇到的具体问题，我会尽力帮助。",
                Timestamp = DateTime.UtcNow
            };
            response.Suggestions.Add("查看日志");
            response.Suggestions.Add("重启应用");
            response.Suggestions.Add("报告问题");
        }
        else
        {
            // Default response
            response.Message = new AiChatMessage
            {
                Role = "assistant",
                Content = "我理解您的需求。作为 Chimera AI 助手，我可以帮助您管理插件和使用系统。\n\n" +
                         "请告诉我您需要什么帮助，或者尝试以下命令：\n\n" +
                         "• 帮助 - 查看可用命令\n" +
                         "• 推荐 - 获取插件推荐\n" +
                         "• 状态 - 查看系统状态\n" +
                         "• 安装 - 安装插件帮助",
                Timestamp = DateTime.UtcNow
            };
            response.Suggestions.Add("帮助");
            response.Suggestions.Add("推荐");
            response.Suggestions.Add("状态");
        }

        return response;
    }

    private async Task<List<AiPluginRecommendation>> GetPluginRecommendationsAsync(AiContext? context)
    {
        var recommendations = new List<AiPluginRecommendation>();
        
        try
        {
            // Get available plugins
            var plugins = await _storeService.FetchPluginsAsync(pageSize: 50);
            
            // Simple recommendation logic based on installed plugins and categories
            var installedPlugins = context?.InstalledPlugins ?? new List<string>();
            
            foreach (var plugin in plugins)
            {
                // Skip already installed plugins
                if (installedPlugins.Contains(plugin.Id))
                    continue;

                // Simple scoring based on tags and popularity
                var score = 0.5; // Base score
                
                // Boost for popular plugins (simplified)
                if (plugin.Tags.Contains("popular"))
                    score += 0.2;
                
                // Boost for utility plugins
                if (plugin.Tags.Contains("utility"))
                    score += 0.1;
                
                // Boost for new plugins
                if (plugin.Version.StartsWith("1.") || plugin.Version.StartsWith("0."))
                    score += 0.1;

                if (score > 0.5)
                {
                    recommendations.Add(new AiPluginRecommendation
                    {
                        PluginId = plugin.Id,
                        Name = plugin.Name,
                        Reason = $"推荐理由：{string.Join(", ", plugin.Tags.Take(2))}",
                        Confidence = Math.Min(score, 1.0),
                        Category = plugin.Tags.FirstOrDefault() ?? "通用"
                    });
                }
            }
        }
        catch
        {
            // Ignore errors in recommendation
        }

        return recommendations.OrderByDescending(r => r.Confidence).ToList();
    }

    private string GetSystemStatusInfo(AiContext? context)
    {
        var info = new List<string>();
        
        // Installed plugins count
        var pluginCount = context?.InstalledPlugins.Count ?? 0;
        info.Add($"• 已安装插件：{pluginCount} 个");
        
        // System info
        if (context?.SystemInfo != null)
        {
            foreach (var kvp in context.SystemInfo.Take(5))
            {
                info.Add($"• {kvp.Key}：{kvp.Value}");
            }
        }
        
        // Recent actions
        if (context?.RecentActions != null && context.RecentActions.Any())
        {
            info.Add($"• 最近操作：{context.RecentActions.Last()}");
        }
        
        return string.Join("\n", info);
    }

    public List<AiChatMessage> GetChatHistory()
    {
        return _chatHistory.ToList();
    }

    public void ClearChatHistory()
    {
        _chatHistory.Clear();
    }

    public AiContext CreateDefaultContext()
    {
        return new AiContext
        {
            SystemInfo = new Dictionary<string, string>
            {
                ["操作系统"] = Environment.OSVersion.ToString(),
                ["版本"] = "v0.8.0-alpha",
                ["运行时间"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            }
        };
    }
}
