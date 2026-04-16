using App.Core.Models;
using App.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace App.Services
{
    public class ProcessingSystem
    {
        private readonly ThreadSafePriorityQueue queue = new ThreadSafePriorityQueue();
        private readonly List<JobExecutionInfo> executionLog = new List<JobExecutionInfo>();
        private readonly HashSet<Guid> processed = new HashSet<Guid>();
        private readonly Dictionary<Guid, TaskCompletionSource<int>> jobTasks =
            new Dictionary<Guid, TaskCompletionSource<int>>();

        private readonly object locker = new object();
        private readonly SemaphoreSlim signal = new SemaphoreSlim(0);

        private readonly List<Task> workers = new List<Task>();
        private readonly int maxSize;
        private int reportIndex = 0;

        // EVENTI
        public event Func<Guid, int, Task> JobCompleted;
        public event Func<Guid, Exception, Task> JobFailed;

        public ProcessingSystem(int maxSize, int workerCount)
        {
            this.maxSize = maxSize;

            for (int i = 0; i < workerCount; i++)
            {
                workers.Add(Task.Run(() => WorkerLoop()));
            }
        }

        // SUBMIT
        public JobHandle Submit(Job job)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            lock (locker)
            {
                if (processed.Contains(job.Id))
                    throw new Exception("Job already exists!");

                if (queue.Count >= maxSize)
                    throw new Exception("Queue is full!");

                processed.Add(job.Id);
                jobTasks[job.Id] = tcs;

                queue.Enqueue(job);
            }

            signal.Release();

            return new JobHandle(job.Id, tcs.Task);
        }

        // WORKER LOOP
        private async Task WorkerLoop()
        {
            while (true)
            {
                await signal.WaitAsync();

                Job job = queue.Dequeue();
                if (job == null) continue;

                await HandleJob(job);
            }
        }

        // GLAVNA LOGIKA SA RETRY + TIMEOUT
        private async Task HandleJob(Job job)
        {
            var tcs = jobTasks[job.Id];
            var start = DateTime.Now;

            try
            {
                int result = await ExecuteWithRetry(job);

                var duration = (long)(DateTime.Now - start).TotalMilliseconds;

                lock (executionLog)
                {
                    executionLog.Add(new JobExecutionInfo
                    {
                        Type = job.Type,
                        Success = true,
                        ExecutionTimeMs = duration
                    });
                }

                tcs.SetResult(result);

                if (JobCompleted != null)
                    await JobCompleted.Invoke(job.Id, result);
            }
            catch (Exception ex)
            {
                var duration = (long)(DateTime.Now - start).TotalMilliseconds;

                lock (executionLog)
                {
                    executionLog.Add(new JobExecutionInfo
                    {
                        Type = job.Type,
                        Success = false,
                        ExecutionTimeMs = duration
                    });
                }

                tcs.SetException(ex);

                if (JobFailed != null)
                    await JobFailed.Invoke(job.Id, ex);
            }
        }

        private async Task<int> ExecuteWithRetry(Job job)
        {
            int attempts = 0;

            while (attempts < 3)
            {
                try
                {
                    var task = ProcessJobInternal(job);
                    var completed = await Task.WhenAny(task, Task.Delay(2000));

                    if (completed == task)
                        return await task;

                    attempts++;
                }
                catch
                {
                    attempts++;
                }
            }

            throw new Exception("ABORT");
        }

        private Task<int> ProcessJobInternal(Job job)
        {
            switch (job.Type)
            {
                case JobType.Prime:
                    return ProcessPrime(job.Payload);
                case JobType.IO:
                    return ProcessIO(job.Payload);
                default:
                    throw new Exception("Unknown job type");
            }
        }

        // PRIME
        private Task<int> ProcessPrime(string payload)
        {
            // numbers:10_000,threads:3

            var parts = payload.Split(',');

            int n = int.Parse(parts[0].Split(':')[1].Replace("_", ""));
            int threads = int.Parse(parts[1].Split(':')[1]);

            if (threads < 1)
                threads = 1;
            else if (threads > 8)
                threads = 8;

            return Task.Run(() =>
            {
                int count = 0;
                object lockObj = new object();

                Parallel.For(2, n, new ParallelOptions
                {
                    MaxDegreeOfParallelism = threads
                },
                i =>
                {
                    if (IsPrime(i))
                    {
                        lock (lockObj)
                            count++;
                    }
                });

                return count;
            });
        }

        private bool IsPrime(int n)
        {
            if (n < 2) return false;

            for (int i = 2; i <= Math.Sqrt(n); i++)
                if (n % i == 0)
                    return false;

            return true;
        }

        // IO
        private async Task<int> ProcessIO(string payload)
        {
            // delay:1_000

            int delay = int.Parse(payload.Split(':')[1].Replace("_", ""));

            await Task.Delay(delay);

            return new Random().Next(0, 100);
        }

        // DODATNE METODE
        public IEnumerable<Job> GetTopJobs(int n)
        {
            return queue.PeekTop(n);
        }

        public Job GetJob(Guid id)
        {
            lock (locker)
            {
                return queue
                    .PeekTop(int.MaxValue)
                    .FirstOrDefault(j => j.Id == id);
            }
        }

        //LINQ IZVESTAJI
        public XDocument GenerateReport()
        {
            List<JobExecutionInfo> snapshot;

            lock (executionLog)
            {
                snapshot = executionLog.ToList();
            }

            var byType = snapshot.GroupBy(x => x.Type);

            var report = new XElement("Report",
                byType.Select(group =>
                    new XElement("JobType",
                        new XAttribute("Type", group.Key),

                        new XElement("TotalExecuted", group.Count(x => x.Success)),

                        new XElement("AverageTime",
                            group.Where(x => x.Success)
                                 .DefaultIfEmpty()
                                 .Average(x => x == null ? 0 : x.ExecutionTimeMs)
                        ),

                        new XElement("Failed",
                            group.Count(x => !x.Success)
                        )
                    )
                )
            );

            return new XDocument(report);
        }
        public void SaveReport(string folderPath)
        {
            var report = GenerateReport();

            string fileName = $"report_{reportIndex % 10}.xml";
            string fullPath = Path.Combine(folderPath, fileName);

            report.Save(fullPath);

            reportIndex++;
        }
    }
}
