// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Interfaces;

public interface ICommandHandler
{
    string CommandId { get; }
    Task ExecuteAsync(IDictionary<string, object> parameters);
}
