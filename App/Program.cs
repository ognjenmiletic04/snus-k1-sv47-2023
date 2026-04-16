using App.Configuration;
using App.Core.Models;
using App.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace App
{
    public class Program
    {
        private static readonly object fileLock = new object();

        static async Task Main(string[] args)
        {
            try
            {
                // 1. PATH setup
                string basePath = Directory
                    .GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;

                string configPath = Path.Combine(basePath, "Configuration", "SystemConfig.xml");
                string logPath = Path.Combine(basePath, "Output", "log.txt");
                string reportFolder = Path.Combine(basePath, "Output");

                Directory.CreateDirectory(reportFolder);

                // 2. Ucitavanje konfiguracije
                XmlConfigLoader configLoader = new XmlConfigLoader();

                try
                {
                    configLoader.LoadConfig(configPath);
                    Console.WriteLine(configLoader.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Config error: {ex.Message}");
                    return;
                }

                // 3. Kreiranje sistema
                ProcessingSystem system;

                try
                {
                    system = new ProcessingSystem(
                        configLoader.MaxQueueSize,
                        configLoader.WorkerCount
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"System init error: {ex.Message}");
                    return;
                }

                // 4. EVENTI (thread-safe log)
                object fileLock = new object();

                system.JobCompleted += async (id, result) =>
                {
                    string log = $"{DateTime.Now} COMPLETED {id} RESULT={result}";

                    try
                    {
                        await Task.Run(() =>
                        {
                            lock (fileLock)
                            {
                                using (var writer = new StreamWriter(logPath, true))
                                {
                                    writer.WriteLine(log);
                                }
                            }
                        });

                        Console.WriteLine(log);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Logging error: {ex.Message}");
                    }
                };

                system.JobFailed += async (id, ex) =>
                {
                    string log = $"{DateTime.Now} FAILED {id} ERROR={ex.Message}";

                    try
                    {
                        await Task.Run(() =>
                        {
                            lock (fileLock)
                            {
                                using (var writer = new StreamWriter(logPath, true))
                                {
                                    writer.WriteLine(log);
                                }
                            }
                        });

                        Console.WriteLine(log);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Logging error: {e.Message}");
                    }
                };

                // 5. SUBMIT pocetnih poslova
                List<JobHandle> handles = new List<JobHandle>();

                foreach (var job in configLoader.Jobs)
                {
                    try
                    {
                        var handle = system.Submit(job);
                        handles.Add(handle);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Submit failed: {ex.Message}");
                    }
                }

                // 6. PRODUCER niti
                Random random = new Random();

                for (int i = 0; i < configLoader.WorkerCount; i++)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            while (true)
                            {
                                try
                                {
                                    Job job;

                                    if (random.Next(2) == 0)
                                    {
                                        job = new Job(
                                            Guid.NewGuid(),
                                            JobType.Prime,
                                            $"numbers:{random.Next(5000, 20000)},threads:{random.Next(1, 5)}",
                                            random.Next(1, 5)
                                        );
                                    }
                                    else
                                    {
                                        job = new Job(
                                            Guid.NewGuid(),
                                            JobType.IO,
                                            $"delay:{random.Next(500, 3000)}",
                                            random.Next(1, 5)
                                        );
                                    }

                                    system.Submit(job);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Producer error: {ex.Message}");
                                }

                                await Task.Delay(1000);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Producer thread crashed: {ex.Message}");
                        }
                    });
                }

                // 7. REPORT GENERATOR (test: 60 sekundi)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (true)
                        {
                            await Task.Delay(60000); 

                            try
                            {
                                system.SaveReport(reportFolder);
                                Console.WriteLine("Report generated.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Report error: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Report thread crashed: {ex.Message}");
                    }
                });

                // 8. CEKANJE inicijalnih poslova
                try
                {
                    var results = await Task.WhenAll(handles.Select(h => h.Result));

                    Console.WriteLine("\n=== ALL RESULTS ===");
                    foreach (var r in results)
                    {
                        Console.WriteLine($"Result: {r}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Some jobs failed: {ex.Message}");
                }

                // 9. TEST GetTopJobs
                try
                {
                    Console.WriteLine("\n=== TOP JOBS ===");

                    var topJobs = system.GetTopJobs(3);

                    foreach (var job in topJobs)
                    {
                        Console.WriteLine($"{job.Id} | Priority: {job.Priority}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"TopJobs error: {ex.Message}");
                }

                Console.WriteLine("\nSYSTEM RUNNING... (press ENTER to exit)");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
            }
        }
    }
    }