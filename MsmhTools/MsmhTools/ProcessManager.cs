﻿using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;
using System.Security;

namespace MsmhTools
{
    public static class ProcessManager
    {
        //-----------------------------------------------------------------------------------

        /// <summary>
        /// Get CPU Usage By Process
        /// </summary>
        /// <param name="process">Process</param>
        /// <param name="delay">Delay to calculate usage (ms)</param>
        /// <returns>Returns -1 if fail</returns>
        public static async Task<float> GetCpuUsage(Process process, int delay)
        {
            string processName = process.ProcessName;
            return await GetCpuUsage(processName, delay);
        }

        /// <summary>
        /// Get CPU Usage By Process ID
        /// </summary>
        /// <param name="pid">PID</param>
        /// <param name="delay">Delay to calculate usage (ms)</param>
        /// <returns>Returns -1 if fail</returns>
        public static async Task<float> GetCpuUsage(int pid, int delay)
        {
            string processName = GetProcessNameByPID(pid);
            if (!string.IsNullOrEmpty(processName))
                return await GetCpuUsage(processName, delay);
            return -1;
        }

        /// <summary>
        /// Get CPU Usage By Process Name
        /// </summary>
        /// <param name="processName">Process Name</param>
        /// <param name="delay">Delay to calculate usage (ms)</param>
        /// <returns>Returns -1 if fail</returns>
        public static async Task<float> GetCpuUsage(string processName, int delay)
        {
            // To Get CPU Total Usage:
            // new PerformanceCounter("Processor", "% Processor Time", "_Total");
            float result = -1;

            await Task.Run(async () =>
            {
                try
                {

                    using PerformanceCounter performanceCounter = new("Process", "% Processor Time", processName, true);
                    performanceCounter.NextValue(); // Returns 0
                    await Task.Delay(delay); // Needs time to calculate
                    result = performanceCounter.NextValue() / Environment.ProcessorCount;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Get CPU Usage: {ex.Message}");
                }
            });
            
            return result;
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Send Command to a Process and Get Result
        /// </summary>
        /// <param name="process">Process</param>
        /// <param name="command">Commands</param>
        /// <returns>Returns True if success</returns>
        public static async Task<bool> SendCommandAsync(Process process, string command)
        {
            try
            {
                await process.StandardInput.WriteLineAsync(command);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Send Command to a Process and Get Result
        /// </summary>
        /// <param name="process">Process</param>
        /// <param name="command">Commands</param>
        /// <returns>Returns True if success</returns>
        public static bool SendCommand(Process process, string command)
        {
            try
            {
                process.StandardInput.WriteLine(command);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Send Command to a Process and Get Result
        /// </summary>
        /// <param name="process">Process</param>
        /// <param name="command">Commands</param>
        /// <param name="alsoRedirectError">Also Redirect Error Output</param>
        /// <returns>Returns Stdout and Stderr if success</returns>
        public static async Task<string> SendCommandAndGetAnswerAsync(Process process, string command, bool alsoRedirectError)
        {
            string? result;

            try
            {
                process.StandardOutput.DiscardBufferedData();
                await process.StandardInput.WriteLineAsync(command);
                result = await process.StandardOutput.ReadLineAsync();
                if (alsoRedirectError)
                {
                    result += Environment.NewLine;
                    result += await process.StandardError.ReadLineAsync();
                }
            }
            catch (Exception)
            {
                result = null;
            }

            return result ?? string.Empty;
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Send Command to a Process and Get Result
        /// </summary>
        /// <param name="process">Process</param>
        /// <param name="command">Commands</param>
        /// <param name="alsoRedirectError">Also Redirect Error Output</param>
        /// <returns>Returns Stdout and Stderr if success</returns>
        public static string SendCommandAndGetAnswer(Process process, string command, bool alsoRedirectError)
        {
            string? result;

            try
            {
                process.StandardOutput.DiscardBufferedData();
                process.StandardInput.WriteLine(command);
                result = process.StandardOutput.ReadLine();
                if (alsoRedirectError)
                {
                    result += Environment.NewLine;
                    result += process.StandardError.ReadLine();
                }
            }
            catch (Exception)
            {
                result = null;
            }

            return result ?? string.Empty;
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Returns stdout or Stderr after process finished. Set waitForExit to false to get out Process.
        /// </summary>
        public static string Execute(out Process process, string processName, string? args = null, bool hideWindow = true, bool runAsAdmin = false, string? workingDirectory = null, ProcessPriorityClass processPriorityClass = ProcessPriorityClass.Normal, bool waitForExit = true)
        {
            // Create process
            Process process0 = new();
            process = process0;
            process0.StartInfo.FileName = processName;
            
            if (args != null)
                process0.StartInfo.Arguments = args;

            if (hideWindow)
            {
                process0.StartInfo.CreateNoWindow = true;
                process0.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }
            else
            {
                process0.StartInfo.CreateNoWindow = false;
                process0.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            }

            if (runAsAdmin)
            {
                process0.StartInfo.Verb = "runas";
            }
            else
            {
                process0.StartInfo.Verb = "";
            }

            // Redirect input output to get ability of sending and reading process output
            process0.StartInfo.UseShellExecute = false;
            process0.StartInfo.RedirectStandardInput = true;
            process0.StartInfo.RedirectStandardOutput = true;
            process0.StartInfo.RedirectStandardError = true;

            if (workingDirectory != null)
                process0.StartInfo.WorkingDirectory = workingDirectory;

            try
            {
                process0.Start();

                // Set process priority
                process0.PriorityClass = processPriorityClass;

                string stdout = process0.StandardOutput.ReadToEnd().ReplaceLineEndings(Environment.NewLine);
                string errout = process0.StandardError.ReadToEnd().ReplaceLineEndings(Environment.NewLine);
                //string output = stdout + Environment.NewLine + errout;

                // Wait for process to finish
                if (waitForExit)
                    process0.WaitForExit();

                if (process0.ExitCode == 0)
                {
                    return stdout;
                }
                else
                {
                    return errout;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Execute and returns PID, if faild returns -1
        /// </summary>
        public static int ExecuteOnly(out Process process, string processName, string? args = null, bool hideWindow = true, bool runAsAdmin = false, string? workingDirectory = null, ProcessPriorityClass processPriorityClass = ProcessPriorityClass.Normal)
        {
            int pid;
            // Create process
            Process process0 = new();
            process0.StartInfo.FileName = processName;

            if (args != null)
                process0.StartInfo.Arguments = args;

            if (hideWindow)
            {
                process0.StartInfo.CreateNoWindow = true;
                process0.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            }
            else
            {
                process0.StartInfo.CreateNoWindow = false;
                process0.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            }

            if (runAsAdmin)
            {
                process0.StartInfo.Verb = "runas";
            }
            else
            {
                process0.StartInfo.Verb = "";
            }

            // Redirect input output to get ability of sending and reading process output
            process0.StartInfo.UseShellExecute = false;
            process0.StartInfo.RedirectStandardInput = true;
            process0.StartInfo.RedirectStandardOutput = true;
            process0.StartInfo.RedirectStandardError = true;

            if (workingDirectory != null)
                process0.StartInfo.WorkingDirectory = workingDirectory;

            try
            {
                process0.Start();

                // Set process priority
                process0.PriorityClass = processPriorityClass;
                pid = process0.Id;
            }
            catch (Exception ex)
            {
                pid = -1;
                Debug.WriteLine($"ExecuteOnly: {ex.Message}");
            }

            process = process0;
            return pid;
        }
        //-----------------------------------------------------------------------------------
        public static bool FindProcessByName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        //-----------------------------------------------------------------------------------
        public static bool FindProcessByPID(int pid)
        {
            bool result = false;
            Process[] processes = Process.GetProcesses();
            for (int n = 0; n < processes.Length; n++)
            {
                if (processes[n].Id == pid)
                {
                    result = true;
                    break;
                }
            }
            return result;
        }
        //-----------------------------------------------------------------------------------
        public static void KillProcessByName(string processName, bool killEntireProcessTree = false)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            for (int n = 0; n < processes.Length; n++)
                processes[n].Kill(killEntireProcessTree);
        }
        //-----------------------------------------------------------------------------------
        public static void KillProcessByPID(int pid, bool killEntireProcessTree = false)
        {
            try
            {
                if (FindProcessByPID(pid))
                {
                    Process process = Process.GetProcessById(pid);
                    process.Kill(killEntireProcessTree);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Returns first PID, if faild returns -1
        /// </summary>
        public static int GetFirstPidByName(string processName)
        {
            int pid = -1;
            Process[] processes = Process.GetProcessesByName(processName);
            for (int n = processes.Length - 1; n >= 0; n--)
                pid = processes[n].Id;
            return pid;
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Returns process PID, if faild returns -1
        /// </summary>
        public static int GetProcessPidByListeningPort(int port)
        {
            string netstatArgs = "-a -n -o";
            string? stdout = Execute(out Process _, "netstat", netstatArgs);
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                List<string> lines = stdout.SplitToLines();
                for (int n = 0; n < lines.Count; n++)
                {
                    string line = lines[n];
                    if (!string.IsNullOrWhiteSpace(line) && line.Contains("LISTENING") && line.Contains($":{port} "))
                    {
                        string[] split1 = line.Split("LISTENING", StringSplitOptions.TrimEntries);
                        bool isBool = int.TryParse(split1[1], out int pid);
                        if (isBool)
                        {
                            return pid;
                        }
                    }
                }
            }
            return -1;
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Returns process name, if failed returns string.empty
        /// </summary>
        public static string GetProcessNameByListeningPort(int port)
        {
            int pid = GetProcessPidByListeningPort(port);
            if (pid != -1)
            {
                try
                {
                    return Process.GetProcessById(pid).ProcessName;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Get Process Name By Listening Port:");
                    Debug.WriteLine(ex.Message);
                }
            }
            return string.Empty;
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Get Process By PID
        /// </summary>
        /// <param name="pid">PID</param>
        /// <returns>Returns null if not exist.</returns>
        public static Process? GetProcessByPID(int pid)
        {
            Process? result = null;
            Process[] processes = Process.GetProcesses();
            for (int n = 0; n < processes.Length; n++)
            {
                if (processes[n].Id == pid)
                {
                    result = processes[n];
                    break;
                }
            }
            return result;
        }
        //-----------------------------------------------------------------------------------
        /// <summary>
        /// Get Process Name By PID
        /// </summary>
        /// <param name="pid">PID</param>
        /// <returns>Returns string.Empty if not exist.</returns>
        public static string GetProcessNameByPID(int pid)
        {
            string result = string.Empty;
            Process? process = GetProcessByPID(pid);
            if (process != null) result = process.ProcessName;
            return result;
        }
        //-----------------------------------------------------------------------------------
        public static string GetArguments(this Process process)
        {
            using ManagementObjectSearcher searcher = new("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id);
            using ManagementObjectCollection objects = searcher.Get();
            return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString() ?? string.Empty;

        }
        //-----------------------------------------------------------------------------------
        public static void SetProcessPriority(ProcessPriorityClass processPriorityClass)
        {
            Process.GetCurrentProcess().PriorityClass = processPriorityClass;
        }
        //-----------------------------------------------------------------------------------
        [Flags]
        private enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern int ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        private static extern int CloseHandle(IntPtr hThread);

        public static void ThrottleProcess(int processId, double limitPercent)
        {
            var process = Process.GetProcessById(processId);
            var processName = process.ProcessName;
            var p = new PerformanceCounter("Process", "% Processor Time", processName);
            Task.Run(async () =>
            {
                while (true)
                {
                    var interval = 1000;
                    p.NextValue();
                    await Task.Delay(interval);
                    var currentUsage = p.NextValue() / Environment.ProcessorCount;
                    Debug.WriteLine(currentUsage);
                    if (currentUsage < limitPercent) continue;
                    SuspendProcess(processId);
                    await Task.Delay(interval);
                    ResumeProcess(processId);
                }
            });
        }
        public static void SuspendProcess(int pId)
        {
            var process = Process.GetProcessById(pId);
            SuspendProcess(process);
        }
        public static void SuspendProcess(Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                _ = SuspendThread(pOpenThread);
            }
        }
        public static void ResumeProcess(int pId)
        {
            var process = Process.GetProcessById(pId);
            ResumeProcess(process);
        }
        public static void ResumeProcess(Process process)
        {
            foreach (ProcessThread thread in process.Threads)
            {
                var pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }
                _ = ResumeThread(pOpenThread);
            }
        }
        //-----------------------------------------------------------------------------------
    }
}
