using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Core.Models
{
    public class Job
    {
        private Guid id;
        private JobType type;
        private string payload;
        private int priority;

        public Job(Guid id, JobType type, string payload, int priority)
        {
            this.id = id;
            this.type = type;
            this.payload = payload;
            this.priority = priority;
        }
        
        public Job() { }
        public Guid Id { get => id; set => id = value; }
        public JobType Type { get => type; set => type = value; }
        public string Payload { get => payload; set => payload = value; }
        public int Priority { get => priority; set => priority = value; }
    }
}
