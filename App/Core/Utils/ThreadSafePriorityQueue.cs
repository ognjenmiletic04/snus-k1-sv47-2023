using App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Core.Utils
{
    public class ThreadSafePriorityQueue
    {
        private readonly object locker = new object();

        // manji broj = VECI prioritet -> SortedDictionary (uzima najmanji prvi)
        private readonly SortedDictionary<int, Queue<Job>> storage = new SortedDictionary<int, Queue<Job>>();
        public void Enqueue(Job job)
        {
            lock (locker)
            {
                if (!storage.ContainsKey(job.Priority))
                    storage[job.Priority] = new Queue<Job>();

                storage[job.Priority].Enqueue(job);
            }
        }

        public Job Dequeue()
        {
            lock (locker)
            {
                foreach (var key in storage.Keys.ToList())
                {
                    if (storage[key].Count > 0)
                    {
                        var job = storage[key].Dequeue();

                        if (storage[key].Count == 0)
                            storage.Remove(key);

                        return job;
                    }
                }
                return null;
            }
        }

        public int Count
        {
            get
            {
                lock (locker)
                {
                    return storage.Sum(x => x.Value.Count);
                }
            }
        }

        public List<Job> PeekTop(int n)
        {
            lock (locker)
            {
                return storage
                    .OrderBy(x => x.Key)
                    .SelectMany(x => x.Value)
                    .Take(n)
                    .ToList();
            }
        }
    }


}

