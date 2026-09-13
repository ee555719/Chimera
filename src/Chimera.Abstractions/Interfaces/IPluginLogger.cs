// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Chimera.Abstractions.Interfaces;

public interface IPluginLogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
    void Fatal(string message, Exception? exception = null);
}
