using GBEmulator.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GBEmulator
{
    static class Program {
        public static CPU cpe;
        public static bool start = false;
        public static bool dispose = false;
        public static int skipFarme = 0;

        private static Thread mainThread;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            //GameScreen scree = new GameScreen();
            Form form1 = new Form1();
            Thread thread = new Thread(new ThreadStart(delegate {
                //CPU cp = new CPU((ScreenIF)scree);
                CPU cp = new CPU((ScreenIF)form1);
                cpe = cp;
                //cp.memory.loadRom("D:\\Windows\\User\\Documentos\\Visual Studio 2015\\Projects\\GBEmulator\\GBEmulator\\ressources\\poke.gb");
                //Stopwatch sw = new Stopwatch();
                DateTime inicio = DateTime.Now;
                DateTime inicioSec = DateTime.Now;
                DateTime lastSecond = DateTime.Now;
                long desired = 16;
                int numFarme = 0;
                start = false;
                while (!dispose) {
                    if (start) {
                        cp.executeOneFrame();
                    }
                    numFarme += 1;
                    long elapse = (DateTime.Now - inicio).Milliseconds;
                    while (elapse < desired) {
                        Thread.Sleep((int)(desired - elapse));
                        elapse = (DateTime.Now - inicio).Milliseconds;
                    }
                    elapse = (DateTime.Now - inicioSec).Milliseconds;
                    if (elapse > 1000) {
                        if (numFarme < 60)
                        {
                            skipFarme = 60 - numFarme;
                        }
                        else {
                            numFarme = 0;
                        }
                        inicioSec = DateTime.Now;
                    }
                    inicio = DateTime.Now;
                }
            }));
            thread.Start();
            mainThread = thread;
            //scree.Run(1 / 60);
            Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(form1);
        }

    }
}
