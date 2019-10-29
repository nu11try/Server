using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashBoardServer
{
    class FreeRAM
    {
        public void Free()
        {
            Console.WriteLine("Memory used before collection:       {0:N0}",
            GC.GetTotalMemory(false));
            
            GC.Collect();
            Console.WriteLine("Memory used after full collection:   {0:N0}",
            GC.GetTotalMemory(true));
        }
    }
}
