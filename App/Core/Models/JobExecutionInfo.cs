using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Core.Models
{
    public class JobExecutionInfo
    {
        private JobType type;
        private bool success;
        private long executionTimeMs;

        public JobExecutionInfo(JobType type, bool success, long executionTimeMs)
        {
            this.type = type;
            this.success = success;
            this.executionTimeMs = executionTimeMs;
        }
        public JobExecutionInfo()
        {
        }
        public override string ToString()
        {
            return $"Type={this.type} Success={this.success} ExecutionTimeMs={this.executionTimeMs}";
        }

        public JobType Type { get => type; set => type = value; }
        public bool Success { get => success; set => success = value; }
        public long ExecutionTimeMs { get => executionTimeMs; set => executionTimeMs = value; }

    }
}
