using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashBoardServer
{
    class Utils
    {
        private Logger logger = new Logger();
        /**
         * 
         *  ОЧИЩАЕТ РАБОЧУЮ ПАПКУ ОТ ФАЙЛОВ БЕЗ РАСШИРЕНИЯ (ВРЕМЕННЫХ ФАЙЛОВ ПОЛУЧЕНИЯ ЗАПРОСОВ И ОТПРАВКИ)
         * 
         */
        public void ClearWorkDir()
        {          
            string[] _filesNames = Directory.GetFiles(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
            foreach (var el in _filesNames)
            {
                if (el.IndexOf('.') == -1)
                {
                    try
                    {
                        File.Delete(el);
                    } catch (Exception ex)
                    {
                        Console.WriteLine("Utils Error = " + ex.Message);
                        logger.WriteLog("Utils Error = " + ex.Message, "ERROR");
                    }
                }
            }
        }
    }
}
