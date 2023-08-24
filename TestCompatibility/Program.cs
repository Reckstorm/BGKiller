using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace TestCompatibility
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                path = args[0];
                ReadProcesses();
            }
            else Exit();

            if (_allowedProcesses == null || _allowedProcesses.Count == 0) Exit();
            CheckIfBGRunning();
            ResetTimers();
            RunCheck();
            while (true)
            {
                List<Process> _processList = Process.GetProcesses().ToList();
                foreach (Process process in _processList)
                {
                    foreach (AllowedProcess p in _allowedProcesses)
                    {
                        if (p.ProcessName.Equals(Assembly.GetExecutingAssembly().GetName().Name)) continue;
                        if (p.ProcessName.Equals(process.ProcessName) && p.AllowedTime <= p.WorkTime)
                        {
                            foreach (Process temp in Process.GetProcessesByName(p.ProcessName))
                            {
                                temp.Kill();
                            }
                            WriteProcesses();
                        }
                    }
                }
            }
        }

        private static object locker = new object();

        private static List<AllowedProcess> allowedProcesses;

        public static List<AllowedProcess> _allowedProcesses
        {
            get 
            { 
                lock(locker)
                {
                    return allowedProcesses;
                }
            }
            set 
            {
                lock (locker)
                {
                    allowedProcesses = value;
                }
            }
        }

        private static string path;

        private static void ReadProcesses()
        {
            if (File.Exists(path) && path != string.Empty)
            {
                string jsonStr = File.ReadAllText(path);
                _allowedProcesses = JsonSerializer.Deserialize<List<AllowedProcess>>(jsonStr);
            }
        }

        private static void WriteProcesses()
        {
            lock (locker)
            {
                File.WriteAllText(path, JsonSerializer.Serialize(_allowedProcesses));
            }
        }

        private static void CheckIfBGRunning()
        {
            Process temp = Process.GetProcesses().ToList().FirstOrDefault(p => p.ProcessName.Equals(Assembly.GetExecutingAssembly().GetName().Name));
            if (temp != null && temp.Id != Process.GetCurrentProcess().Id) temp.Kill();
        }

        private static void RunCheck()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    foreach (AllowedProcess p in _allowedProcesses)
                    {
                        if (Process.GetProcesses().Any(proc => proc.ProcessName.Equals(p.ProcessName))) p.WorkTime++;
                    }
                    WriteProcesses();
                    Thread.Sleep(1000);
                }
            });
        }

        private static void ResetTimers()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (DateTime.Now.ToLongTimeString().Equals("00:00:00"))
                    {
                        foreach (AllowedProcess p in _allowedProcesses)
                        {
                            p.WorkTime = 0;
                        }
                    }
                    WriteProcesses();
                    Thread.Sleep(1000);
                }
            });
        }

        private static void Exit()
        {
            string p = "ProcessAdmin 19.08.exe";
            if (!File.Exists(p)) Environment.Exit(-1);
            Process.Start(p);
            Environment.Exit(-1);
        }
    }
}
