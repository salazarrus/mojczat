using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;


namespace MojCzat.komunikacja
{
    class Pingacz
    {
        Centrala centrala;

        Dictionary<string, bool> dostepnosc;

        Timer timer;

        public Pingacz(Centrala centrala, Dictionary<string, bool> dostepnosc) 
        {
            this.centrala = centrala;
            this.dostepnosc = dostepnosc;
            this.timer = new Timer();
            timer.Elapsed += timer_Elapsed;
            timer.Interval = 5000;
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var id in dostepnosc.Keys.ToList())
            {
                if (!dostepnosc[id] && centrala[id] == null) { centrala.Polacz(id); }
            }
        }

        public void Start() { timer.Start(); }

        public void Stop() { timer.Stop(); }
    }
}
