using System;
using System.Collections.Generic;

namespace DaysToPlay.Models
{
    public class MenstrualCycle
    {
        public List<Period> Periods { get; set; } = new List<Period>();
        public DateTime StartDate { get; set; }
        public int CycleLength { get; set; }
    }

    public class Period
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Length => ((EndDate - StartDate).Days + 1).ToString() + " Day(s)";
    }
}
