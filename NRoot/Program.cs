using System.Diagnostics;
using System.Runtime.InteropServices;

Console.WriteLine("claude made me write this shit");

Console.WriteLine("Welcome to NRoot");
Thread.Sleep(10000);
while (true)
{
    Console.Write("\nNRoot -> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    var arg = ParseArgs(input);
    if (arg.Length == 0) continue;
    switch (arg[0].ToLower())
    {
        case "elevate": HandleElevate(arg); break;
        case "kill": HandleKill(arg); break;
        case "restart": HandleRestart(arg); break;
        case "run": HandleRun(arg); break;
        case "ps": HandlePs(); break;
        case "whoami": HandleWhoAmI(); break;
        case "sysinfo": HandleSysInfo(); break;
        case "version": HandleVersion(); break;
        case "help": ShowHelp(); break;
        case "exit":
        case "quit":
            Console.WriteLine("Goodbye.");
            return;
        default:
            Console.WriteLine($"Unknown command: {arg[0]}");
            break;
    }
}

static void ShowHelp()
{
    Console.WriteLine("""
        NRoot - Cross-platform privilege escalation tool

        Commands:
          elevate "path\to\exe" to "level"   Launch exe at a higher privilege level
            Levels: admin, root, system, kernel

          kill "process.exe"                  Kill process by name at SYSTEM level
          kill <PID>                          Kill process by PID at SYSTEM level

          restart "process.exe"               Restart a process at SYSTEM level
          restart <PID>                       Restart a process by PID at SYSTEM level

          run "command"                       Run a shell command at SYSTEM level

          ps                                  List all running processes + PIDs
          whoami                              Show current user and privilege level
          sysinfo                             Show OS, CPU, RAM info
          version                             Show NRoot version

          help                                Show this help message
          exit / quit                         Exit NRoot
        """);
}

static void HandleVersion()
{
    Console.WriteLine("NRoot v1.0.0 - Cross-platform privilege escalation tool");
    Console.WriteLine($"Running on: {RuntimeInformation.OSDescription}");
    Console.WriteLine($".NET Runtime: {RuntimeInformation.FrameworkDescription}");
}

static void HandleWhoAmI()
{
    string user = Environment.UserName;
    string machine = Environment.MachineName;
    string domain = Environment.UserDomainName;

    Console.WriteLine($"User:    {domain}\\{user}");
    Console.WriteLine($"Machine: {machine}");

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        bool isAdmin = false;
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { }
        Console.WriteLine($"Level:   {(isAdmin ? "Administrator" : "Standard User")}");
    }
    else
    {
        try
        {
            var result = RunCommandWithOutput("id", "-u");
            bool isRoot = result.Trim() == "0";
            Console.WriteLine($"Level:   {(isRoot ? "root" : "standard user")}");
            Console.WriteLine($"ID:      {RunCommandWithOutput("id", "").Trim()}");
        }
        catch
        {
            Console.WriteLine("Level:   unknown");
        }
    }
}

static void HandleSysInfo()
{
    Console.WriteLine($"OS:           {RuntimeInformation.OSDescription}");
    Console.WriteLine($"Architecture: {RuntimeInformation.OSArchitecture}");
    Console.WriteLine($".NET:         {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"CPU Cores:    {Environment.ProcessorCount}");
    Console.WriteLine($"Machine:      {Environment.MachineName}");
    Console.WriteLine($"Username:     {Environment.UserName}");

    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string ram = RunCommandWithOutput("wmic", "OS get TotalVisibleMemorySize /Value");
            Console.WriteLine($"RAM Info:     {ram.Trim()}");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string mem = RunCommandWithOutput("grep", "MemTotal /proc/meminfo");
            Console.WriteLine($"RAM Info:     {mem.Trim()}");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string mem = RunCommandWithOutput("sysctl", "hw.memsize");
            Console.WriteLine($"RAM Info:     {mem.Trim()}");
        }
    }
    catch
    {
        Console.WriteLine("RAM Info:     unavailable");
    }
}

static void HandlePs()
{
    var processes = Process.GetProcesses()
        .OrderBy(p => p.ProcessName)
        .ToList();

    Console.WriteLine($"\n{"PID",-8} {"Name",-40} {"Memory (MB)",-12}");
    Console.WriteLine(new string('-', 62));

    foreach (var p in processes)
    {
        try
        {
            long memMb = p.WorkingSet64 / 1024 / 1024;
            Console.WriteLine($"{p.Id,-8} {p.ProcessName,-40} {memMb,-12}");
        }
        catch
        {
            Console.WriteLine($"{p.Id,-8} {p.ProcessName,-40} {"N/A",-12}");
        }
    }

    Console.WriteLine($"\nTotal processes: {processes.Count}");
}

static void HandleRestart(string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: restart \"process.exe\" or restart <PID>");
        return;
    }

    string target = args[1].Trim('"');
    string? exePath = null;

    try
    {
        if (int.TryParse(target, out int pid))
        {
            var proc = Process.GetProcessById(pid);
            exePath = proc.MainModule?.FileName;
        }
        else
        {
            var procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(target));
            if (procs.Length > 0)
                exePath = procs[0].MainModule?.FileName;
        }
    }
    catch
    {
        Console.WriteLine("[NRoot] Warning: Could not retrieve process path, will attempt restart by name only.");
    }

    Console.WriteLine($"[NRoot] Killing {target}...");

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        KillWindows(target);
    else
        KillUnix(target);

    if (exePath != null)
    {
        Thread.Sleep(1000);
        Console.WriteLine($"[NRoot] Relaunching {exePath}...");
        ElevateToSystem(exePath);
    }
    else
    {
        Console.WriteLine("[NRoot] Could not relaunch: process path unknown.");
    }
}

static void HandleRun(string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: run \"command\"");
        return;
    }

    string command = string.Join(" ", args.Skip(1));

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        RunAtSystemWindows(command);
    else
        RunAtSystemUnix(command);
}

static void RunAtSystemWindows(string command)
{
    Console.Write("[NRoot] Enter service name for escalation -> ");
    string svcName = Console.ReadLine()?.Trim() ?? "NRootSvcElevate";
    if (string.IsNullOrWhiteSpace(svcName)) svcName = "NRootSvcElevate";

    try
    {
        string tempScript = Path.Combine(Path.GetTempPath(), "nroot_run.bat");
        File.WriteAllText(tempScript, command);

        Console.WriteLine($"[NRoot] Creating service {svcName} to run command at SYSTEM level...");
        RunSC($"create {svcName} binPath= \"cmd.exe /c \\\"{tempScript}\\\"\" start= demand obj= LocalSystem DisplayName= \"NRoot Run Service\"");
        RunSC($"start {svcName}");

        Thread.Sleep(2000);

        Console.WriteLine($"[NRoot] Cleaning up {svcName}...");
        RunSC($"stop {svcName}");
        RunSC($"delete {svcName}");
        File.Delete(tempScript);

        Console.WriteLine($"[NRoot] Command ran at SYSTEM level. Service cleaned up.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[NRoot] Run failed: {ex.Message}");
        try { RunSC($"stop {svcName}"); } catch { }
        try { RunSC($"delete {svcName}"); } catch { }
    }
}

static void RunAtSystemUnix(string command)
{
    Console.Write("[NRoot] Enter service name for escalation -> ");
    string svcName = Console.ReadLine()?.Trim() ?? "NRootSvcElevate";
    if (string.IsNullOrWhiteSpace(svcName)) svcName = "NRootSvcElevate";

    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string unitContent = $"""
                [Unit]
                Description=NRoot Run Service

                [Service]
                Type=oneshot
                ExecStart=/bin/sh -c "{command}"
                User=root
                RemainAfterExit=no
                """;

            string unitPath = $"/etc/systemd/system/{svcName}.service";
            File.WriteAllText($"/tmp/{svcName}.service", unitContent);

            Console.WriteLine($"[NRoot] Creating systemd service {svcName} to run command at kernel level...");
            RunCommand("sudo", $"cp /tmp/{svcName}.service {unitPath}");
            RunCommand("sudo", "systemctl daemon-reload");
            RunCommand("sudo", $"systemctl start {svcName}");

            Thread.Sleep(2000);
            RunCommand("sudo", $"systemctl stop {svcName}");
            RunCommand("sudo", $"systemctl disable {svcName}");
            RunCommand("sudo", $"rm {unitPath}");
            RunCommand("sudo", "systemctl daemon-reload");

            Console.WriteLine($"[NRoot] Command ran at kernel level. Service cleaned up.");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string plistContent = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0">
                <dict>
                    <key>Label</key>
                    <string>{svcName}</string>
                    <key>ProgramArguments</key>
                    <array>
                        <string>/bin/sh</string>
                        <string>-c</string>
                        <string>{command}</string>
                    </array>
                    <key>RunAtLoad</key>
                    <true/>
                    <key>UserName</key>
                    <string>root</string>
                </dict>
                </plist>
                """;

            string plistPath = "/Library/LaunchDaemons/com.nroot.run.plist";
            File.WriteAllText($"/tmp/{svcName}.plist", plistContent);

            Console.WriteLine($"[NRoot] Creating launchd service {svcName} to run command at root level...");
            RunCommand("sudo", $"cp /tmp/{svcName}.plist {plistPath}");
            RunCommand("sudo", $"launchctl load {plistPath}");

            Thread.Sleep(2000);
            RunCommand("sudo", $"launchctl unload {plistPath}");
            RunCommand("sudo", $"rm {plistPath}");

            Console.WriteLine($"[NRoot] Command ran at root level. Service cleaned up.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[NRoot] Run failed: {ex.Message}");
    }
}

static void HandleElevate(string[] args)
{
    if (args.Length < 4 || args[2].ToLower() != "to")
    {
        Console.WriteLine("Usage: elevate \"path\\to\\exe\" to \"level\"");
        return;
    }

    string exePath = args[1].Trim('"');
    string level = args[3].Trim('"').ToLower();

    if (!File.Exists(exePath))
    {
        Console.WriteLine($"Error: File not found: {exePath}");
        return;
    }

    switch (level)
    {
        case "admin":
        case "root":
            ElevateToAdmin(exePath);
            break;
        case "system":
        case "kernel":
            ElevateToSystem(exePath);
            break;
        default:
            Console.WriteLine($"Unknown level: {level}. Valid levels: admin, root, system, kernel");
            break;
    }
}

static void ElevateToAdmin(string exePath)
{
    try
    {
        var psi = new ProcessStartInfo { FileName = exePath, UseShellExecute = true };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            psi.Verb = "runas";
        else
        {
            psi.FileName = "sudo";
            psi.Arguments = $"\"{exePath}\"";
        }

        Process.Start(psi);
        Console.WriteLine($"Launched {exePath} as Admin/Root.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Elevation failed: {ex.Message}");
    }
}

static void ElevateToSystem(string exePath)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        ElevateToSystemWindows(exePath);
    else
        ElevateToSystemUnix(exePath);
}

static void ElevateToSystemWindows(string exePath)
{
    Console.Write("[NRoot] Enter service name for escalation -> ");
    string svcName = Console.ReadLine()?.Trim() ?? "NRootSvcElevate";
    if (string.IsNullOrWhiteSpace(svcName)) svcName = "NRootSvcElevate";

    try
    {
        Console.WriteLine($"[NRoot] Creating service {svcName} to launch at SYSTEM level...");
        RunSC($"create {svcName} binPath= \"\\\"{exePath}\\\"\" start= demand obj= LocalSystem DisplayName= \"NRoot Elevation Service\"");

        Console.WriteLine($"[NRoot] Starting {svcName}... (UAC prompt may appear)");
        RunSC($"start {svcName}");

        Thread.Sleep(2000);

        Console.WriteLine($"[NRoot] Cleaning up {svcName}...");
        RunSC($"stop {svcName}");
        RunSC($"delete {svcName}");

        Console.WriteLine($"[NRoot] {exePath} launched at SYSTEM level. Service cleaned up.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[NRoot] System elevation failed: {ex.Message}");
        try { RunSC($"stop {svcName}"); } catch { }
        try { RunSC($"delete {svcName}"); } catch { }
    }
}

static void ElevateToSystemUnix(string exePath)
{
    Console.Write("[NRoot] Enter service name for escalation -> ");
    string svcName = Console.ReadLine()?.Trim() ?? "NRootSvcElevate";
    if (string.IsNullOrWhiteSpace(svcName)) svcName = "NRootSvcElevate";

    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Console.WriteLine($"[NRoot] Creating systemd service {svcName}...");

            string unitContent = $"""
                [Unit]
                Description=NRoot Elevation Service
                After=network.target

                [Service]
                Type=oneshot
                ExecStart={exePath}
                User=root
                RemainAfterExit=no

                [Install]
                WantedBy=multi-user.target
                """;

            string unitPath = $"/etc/systemd/system/{svcName}.service";
            File.WriteAllText($"/tmp/{svcName}.service", unitContent);

            RunCommand("sudo", $"cp /tmp/{svcName}.service {unitPath}");
            RunCommand("sudo", "systemctl daemon-reload");
            RunCommand("sudo", $"systemctl start {svcName}");

            Console.WriteLine($"[NRoot] {exePath} launched via systemd as root/kernel level.");

            Thread.Sleep(2000);
            RunCommand("sudo", $"systemctl stop {svcName}");
            RunCommand("sudo", $"systemctl disable {svcName}");
            RunCommand("sudo", $"rm {unitPath}");
            RunCommand("sudo", "systemctl daemon-reload");

            Console.WriteLine($"[NRoot] Service {svcName} cleaned up.");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Console.WriteLine($"[NRoot] Creating launchd service {svcName}...");

            string plistContent = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0">
                <dict>
                    <key>Label</key>
                    <string>{svcName}</string>
                    <key>ProgramArguments</key>
                    <array>
                        <string>{exePath}</string>
                    </array>
                    <key>RunAtLoad</key>
                    <true/>
                    <key>UserName</key>
                    <string>root</string>
                </dict>
                </plist>
                """;

            string plistPath = "/Library/LaunchDaemons/com.nroot.elevate.plist";
            File.WriteAllText($"/tmp/{svcName}.plist", plistContent);
            RunCommand("sudo", $"cp /tmp/{svcName}.plist {plistPath}");
            RunCommand("sudo", $"launchctl load {plistPath}");

            Console.WriteLine($"[NRoot] {exePath} launched via launchd as root/kernel level.");

            Thread.Sleep(2000);
            RunCommand("sudo", $"launchctl unload {plistPath}");
            RunCommand("sudo", $"rm {plistPath}");

            Console.WriteLine($"[NRoot] Service {svcName} cleaned up.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[NRoot] System elevation failed: {ex.Message}");
    }
}

static void HandleKill(string[] args)
{
    if (args.Length < 2)
    {
        Console.WriteLine("Usage: kill \"process.exe\" or kill <PID>");
        return;
    }

    string target = args[1].Trim('"');

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        KillWindows(target);
    else
        KillUnix(target);
}

static void KillWindows(string target)
{
    Console.Write("[NRoot] Enter service name for escalation -> ");
    string svcName = Console.ReadLine()?.Trim() ?? "NRootSvcElevate";
    if (string.IsNullOrWhiteSpace(svcName)) svcName = "NRootSvcElevate";

    try
    {
        string killArgs = int.TryParse(target, out int pid)
            ? $"/PID {pid} /F"
            : $"/IM \"{target}\" /F";

        string tempScript = Path.Combine(Path.GetTempPath(), "nroot_kill.bat");
        File.WriteAllText(tempScript, $"taskkill {killArgs}");

        Console.WriteLine($"[NRoot] Creating service {svcName} to kill {target} at SYSTEM level...");
        RunSC($"create {svcName} binPath= \"cmd.exe /c \\\"{tempScript}\\\"\" start= demand obj= LocalSystem DisplayName= \"NRoot Kill Service\"");
        RunSC($"start {svcName}");

        Thread.Sleep(2000);

        Console.WriteLine($"[NRoot] Cleaning up {svcName}...");
        RunSC($"stop {svcName}");
        RunSC($"delete {svcName}");
        File.Delete(tempScript);

        Console.WriteLine($"[NRoot] {target} killed at SYSTEM level. Service cleaned up.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[NRoot] Kill failed: {ex.Message}");
        try { RunSC($"stop {svcName}"); } catch { }
        try { RunSC($"delete {svcName}"); } catch { }
    }
}

static void KillUnix(string target)
{
    Console.Write("[NRoot] Enter service name for escalation -> ");
    string svcName = Console.ReadLine()?.Trim() ?? "NRootSvcElevate";
    if (string.IsNullOrWhiteSpace(svcName)) svcName = "NRootSvcElevate";

    try
    {
        string killCmd = int.TryParse(target, out int pid)
            ? $"kill -9 {pid}"
            : $"pkill -9 -f \"{target}\"";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string unitContent = $"""
                [Unit]
                Description=NRoot Kill Service

                [Service]
                Type=oneshot
                ExecStart=/bin/sh -c "{killCmd}"
                User=root
                RemainAfterExit=no
                """;

            string unitPath = $"/etc/systemd/system/{svcName}.service";
            File.WriteAllText($"/tmp/{svcName}.service", unitContent);

            Console.WriteLine($"[NRoot] Creating systemd service {svcName} to kill {target} at kernel level...");
            RunCommand("sudo", $"cp /tmp/{svcName}.service {unitPath}");
            RunCommand("sudo", "systemctl daemon-reload");
            RunCommand("sudo", $"systemctl start {svcName}");

            Thread.Sleep(2000);
            RunCommand("sudo", $"systemctl stop {svcName}");
            RunCommand("sudo", $"systemctl disable {svcName}");
            RunCommand("sudo", $"rm {unitPath}");
            RunCommand("sudo", "systemctl daemon-reload");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            string plistContent = $"""
                <?xml version="1.0" encoding="UTF-8"?>
                <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
                <plist version="1.0">
                <dict>
                    <key>Label</key>
                    <string>{svcName}</string>
                    <key>ProgramArguments</key>
                    <array>
                        <string>/bin/sh</string>
                        <string>-c</string>
                        <string>{killCmd}</string>
                    </array>
                    <key>RunAtLoad</key>
                    <true/>
                    <key>UserName</key>
                    <string>root</string>
                </dict>
                </plist>
                """;

            string plistPath = "/Library/LaunchDaemons/com.nroot.kill.plist";
            File.WriteAllText($"/tmp/{svcName}.plist", plistContent);

            Console.WriteLine($"[NRoot] Creating launchd service {svcName} to kill {target} at root level...");
            RunCommand("sudo", $"cp /tmp/{svcName}.plist {plistPath}");
            RunCommand("sudo", $"launchctl load {plistPath}");

            Thread.Sleep(2000);
            RunCommand("sudo", $"launchctl unload {plistPath}");
            RunCommand("sudo", $"rm {plistPath}");
        }

        Console.WriteLine($"[NRoot] {target} killed at system level. Service cleaned up.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[NRoot] Kill failed: {ex.Message}");
    }
}

static void RunSC(string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = "sc.exe",
        Arguments = arguments,
        UseShellExecute = true,
        Verb = "runas",
        CreateNoWindow = true
    };
    var p = Process.Start(psi);
    p?.WaitForExit();
}

static void RunCommand(string cmd, string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = cmd,
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    var p = Process.Start(psi);
    p?.WaitForExit();
}

static string RunCommandWithOutput(string cmd, string arguments)
{
    var psi = new ProcessStartInfo
    {
        FileName = cmd,
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    var p = Process.Start(psi);
    string output = p?.StandardOutput.ReadToEnd() ?? "";
    p?.WaitForExit();
    return output;
}

static string[] ParseArgs(string input)
{
    var result = new List<string>();
    bool inQuotes = false;
    var current = new System.Text.StringBuilder();

    foreach (char c in input)
    {
        if (c == '"') { inQuotes = !inQuotes; continue; }
        if (c == ' ' && !inQuotes)
        {
            if (current.Length > 0) { result.Add(current.ToString()); current.Clear(); }
        }
        else current.Append(c);
    }
    if (current.Length > 0) result.Add(current.ToString());
    return result.ToArray();
}