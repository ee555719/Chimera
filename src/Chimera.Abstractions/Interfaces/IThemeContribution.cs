// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Interfaces;

public interface IThemeContribution
{
    string Id { get; }
    string Name { get; }
    bool IsDark { get; }
    IDictionary<string, string> Colors { get; }
    IDictionary<string, string> Fonts { get; }
    double CornerRadius { get; }
}
