using App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace App.Configuration
{
    public class XmlConfigLoader
    {
        private int workerCount { get; set; }
        private int maxQueueSize { get; set; }
        private List<Job> jobs { get; set; }
        public XmlConfigLoader() { }

        public int WorkerCount { get => workerCount; set => workerCount = value; }
        public int MaxQueueSize { get => maxQueueSize; set => maxQueueSize = value; }
        public List<Job> Jobs { get => jobs; set => jobs = value; }

        public void LoadConfig(string filePath)
        {
            var document = XDocument.Load(filePath);

            var root = document.Root;
            if (root == null)
                throw new Exception("Invalid config file: missing root element.");

            WorkerCount = int.Parse(root.Element("WorkerCount")?.Value
                ?? throw new Exception("WorkerCount missing"));

            MaxQueueSize = int.Parse(root.Element("MaxQueueSize")?.Value
                ?? throw new Exception("MaxQueueSize missing"));

            var jobsElement = root.Element("Jobs");
            if (jobsElement == null)
                throw new Exception("Jobs section missing");

            Jobs = jobsElement
                .Elements("Job")
                .Select(job =>
                {
                    var idValue =
                        job.Attribute("Id")?.Value; 

                    Guid id = Guid.TryParse(idValue, out var parsedId)
                        ? parsedId
                        : Guid.NewGuid(); 

                    return new Job(
                        id,
                        (JobType)Enum.Parse(typeof(JobType), job.Attribute("Type")?.Value
                            ?? throw new Exception("Job Type missing")),
                        job.Attribute("Payload")?.Value
                            ?? throw new Exception("Job Payload missing"),
                        int.Parse(job.Attribute("Priority")?.Value
                            ?? throw new Exception("Job Priority missing"))
                    );
                })
                .ToList();
        }

        public override string ToString()
        {
            var jobsString = string.Join("\n", Jobs.Select(j => $"Id: {j.Id}, Type: {j.Type}, Payload: {j.Payload}, Priority: {j.Priority}"));
            return $"WorkerCount: {WorkerCount}\nMaxQueueSize: {MaxQueueSize}\nJobs:\n{jobsString}";
        }
    }
}
