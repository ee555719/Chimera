// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Interfaces;

public interface IContributionRegistry
{
    void RegisterStartupTask(IStartupTask task);
    void RegisterMenuContribution(IMenuContribution contribution);
    void RegisterToolbarContribution(IToolbarContribution contribution);
    void RegisterPageContribution(IPageContribution contribution);
    void RegisterWidgetContribution(IWidgetContribution contribution);
    void RegisterSettingsSection(ISettingsSection section);
    void RegisterThemeContribution(IThemeContribution theme);
    void RegisterCommandHandler(ICommandHandler handler);
    void RegisterLifecycleHook(ILifecycleHook hook);

    IReadOnlyList<IStartupTask> GetStartupTasks();
    IReadOnlyList<IMenuContribution> GetMenuContributions();
    IReadOnlyList<IToolbarContribution> GetToolbarContributions();
    IReadOnlyList<IPageContribution> GetPageContributions();
    IReadOnlyList<IWidgetContribution> GetWidgetContributions();
    IReadOnlyList<ISettingsSection> GetSettingsSections();
    IReadOnlyList<IThemeContribution> GetThemeContributions();
    ICommandHandler? GetCommandHandler(string commandId);
    IReadOnlyList<ILifecycleHook> GetLifecycleHooks();
}
