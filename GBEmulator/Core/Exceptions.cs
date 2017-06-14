using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Core {
    class IllegalAccessMemory : Exception{

        public IllegalAccessMemory() { }

        public IllegalAccessMemory(string message) : base(message) { }
    }

    class IllegalOperationCPU : Exception {

        public IllegalOperationCPU() { }

        public IllegalOperationCPU(string message) : base(message) { }
    }
}
