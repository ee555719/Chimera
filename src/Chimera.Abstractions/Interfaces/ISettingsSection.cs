// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Interfaces;

public interface ISettingsSection
{
    string Id { get; }
    string Title { get; }
    string? Icon { get; }
    int Order { get; }
    Type SettingsType { get; }
}
