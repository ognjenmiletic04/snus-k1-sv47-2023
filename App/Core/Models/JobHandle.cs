using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Core.Models
{
    public class JobHandle
    {
        private Guid id;
        private Task<int> result;
        public JobHandle(Guid id, Task<int> result)
        {
            this.id = id;
            this.result = result;
        }
        public JobHandle() { }

        public Guid Id { get => id; set => id = value; }
        public Task<int> Result { get => result; set => result = value; }

    }
}
