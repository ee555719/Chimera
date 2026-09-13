// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Windows.Input;

namespace Chimera.Abstractions.Interfaces;

public interface IMenuContribution
{
    string Id { get; }
    string Header { get; }
    string? Icon { get; }
    int Order { get; }
    ICommand Command { get; }
}
