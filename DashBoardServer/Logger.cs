using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DashBoardServer
{
    class Logger
    {
        public void WriteLog(string msg, string flag = "LOG")
        {
            using (FileStream fstream = new FileStream(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\log.txt", FileMode.Append))
            {
                // преобразуем строку в байты
                byte[] array = System.Text.Encoding.Default.GetBytes(DateTime.Now + " [" + flag + "] -- " +  msg + "\n");
                // запись массива байтов в файл
                fstream.Write(array, 0, array.Length);
            }
        }
    }
}
