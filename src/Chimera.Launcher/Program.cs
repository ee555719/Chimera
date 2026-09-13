// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Diagnostics;
using System.IO;
using Chimera.Abstractions.Models;
using Chimera.PluginLoader;

namespace Chimera.Launcher;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("Chimera Launcher v0.3.0-alpha");
        Console.WriteLine("============================");
        Console.WriteLine();

        // Check if running as installer
        if (args.Contains("--install"))
        {
            return await RunInstaller(args);
        }

        // Check if already installed
        var installer = new ChimeraInstaller();
        
        if (!installer.IsInstalled())
        {
            Console.WriteLine("检测到首次运行，正在执行安装...");
            Console.WriteLine();

            var progress = new Progress<string>(message =>
            {
                Console.WriteLine($"  {message}");
            });

            try
            {
                await installer.CheckAndInstallAsync(progress);
                Console.WriteLine();
                Console.WriteLine("安装完成！正在启动 Chimera...");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"安装失败: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        // Start the host application
        try
        {
            var hostArgs = args.Where(a => a != "--install").ToArray();
            installer.StartHost(hostArgs);
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"启动失败: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    static async Task<int> RunInstaller(string[] args)
    {
        Console.WriteLine("Chimera 安装程序");
        Console.WriteLine("================");
        Console.WriteLine();

        var installer = new ChimeraInstaller();
        var progress = new Progress<string>(message =>
        {
            Console.WriteLine($"  {message}");
        });

        try
        {
            // Ensure directories exist
            ChimeraPaths.EnsureDirectories();
            Console.WriteLine($"安装目录: {ChimeraPaths.BaseDirectory}");
            Console.WriteLine($"插件目录: {ChimeraPaths.PluginDirectory}");
            Console.WriteLine($"Python 目录: {ChimeraPaths.PythonDirectory}");
            Console.WriteLine();

            // Run installation
            await installer.CheckAndInstallAsync(progress);
            Console.WriteLine();
            Console.WriteLine("安装完成！");
            Console.WriteLine();
            Console.WriteLine("按任意键启动 Chimera...");
            Console.ReadKey(true);

            // Start the host
            installer.StartHost(args.Where(a => a != "--install").ToArray());
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"安装失败: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey(true);
            return 1;
        }
    }
}
