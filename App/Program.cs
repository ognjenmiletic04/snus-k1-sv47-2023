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
        static async Task Main(string[] args)
        {
            // 1. Ucitavanje konfiguracije
            XmlConfigLoader configLoader = new XmlConfigLoader();

            string path = Path.Combine(
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName,
                "Configuration",
                "SystemConfig.xml"
            );
            string outputPath = Path.Combine(
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName,
                "Output",
                "log.txt"
            );

            configLoader.LoadConfig(path);
            Console.WriteLine(configLoader.ToString());

            // 2. Kreiranje sistema
            var system = new ProcessingSystem(
                configLoader.MaxQueueSize,
                configLoader.WorkerCount
            );

            // 3. EVENTI (lambda + async log)
            system.JobCompleted += async (id, result) =>
            {
                string log = $"{DateTime.Now} COMPLETED {id} RESULT={result}";

                using (var writer = new StreamWriter(outputPath, true))
                {
                    await writer.WriteLineAsync(log);
                }

                Console.WriteLine(log);
            };

            system.JobFailed += async (id, ex) =>
            {
                string log = $"{DateTime.Now} FAILED {id} ERROR={ex.Message}";

                using (var writer = new StreamWriter(outputPath, true))
                {
                    await writer.WriteLineAsync(log);
                }

                Console.WriteLine(log);
            };

            // 4. SUBMIT poslova iz XML-a
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

            // 5. CEKANJE REZULTATA (bez Thread.Sleep)
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
                Console.WriteLine($"Some jobe aborted: {ex.Message}");
            }

            // 6. TEST GetTopJobs
            Console.WriteLine("\n=== TOP JOBS ===");
            var topJobs = system.GetTopJobs(3);

            foreach (var job in topJobs)
            {
                Console.WriteLine($"{job.Id} | Priority: {job.Priority}");
            }

            Console.WriteLine("\nEND OF TEST");
            Console.ReadLine();
        }
    }
}
