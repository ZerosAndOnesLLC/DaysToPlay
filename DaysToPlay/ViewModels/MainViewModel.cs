using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using DaysToPlay.Models;
using Microsoft.Maui.Controls;

namespace DaysToPlay.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event Action PeriodsUpdated;

        private MenstrualCycle _menstrualCycle;
        public MenstrualCycle MenstrualCycle
        {
            get => _menstrualCycle;
            set
            {
                _menstrualCycle = value;
                OnPropertyChanged(nameof(MenstrualCycle));
            }
        }

        public Period CurrentPeriod { get; set; }
        public ICommand LogPeriodCommand { get; }

        public DateTime OvulationDate { get; private set; }
        public DateTime FertileStartDate { get; private set; }
        public DateTime FertileEndDate { get; private set; }
        public double AveragePeriodLength { get; private set; }
        public DateTime NextPeriodStartDate { get; private set; }

        public ObservableCollection<DateTime> FertileDays { get; private set; }
        public ObservableCollection<DateTime> SafeDays { get; private set; }
        public ObservableCollection<Period> LoggedPeriods { get; private set; }

        public MainViewModel()
        {
            MenstrualCycle = new MenstrualCycle();
            CurrentPeriod = new Period { StartDate = DateTime.Today, EndDate = DateTime.Today };
            LogPeriodCommand = new Command(OnLogPeriod);
            LoggedPeriods = new ObservableCollection<Period>();
            FertileDays = new ObservableCollection<DateTime>();
            SafeDays = new ObservableCollection<DateTime>();
        }

        private void OnLogPeriod()
        {
            Debug.WriteLine("Logging period: Start - " + CurrentPeriod.StartDate + ", End - " + CurrentPeriod.EndDate);
            MenstrualCycle.Periods.Add(new Period { StartDate = CurrentPeriod.StartDate, EndDate = CurrentPeriod.EndDate });
            CalculateAverageCycleLength();
            CalculateAveragePeriodLength();
            CalculateForecasts();
            UpdateLoggedPeriods();
            OnPropertyChanged(nameof(MenstrualCycle));
            PeriodsUpdated?.Invoke();
        }

        private void CalculateAverageCycleLength()
        {
            if (MenstrualCycle.Periods.Count > 1)
            {
                var cycleLengths = MenstrualCycle.Periods
                    .OrderBy(p => p.StartDate)
                    .Zip(MenstrualCycle.Periods.Skip(1), (a, b) => (b.StartDate - a.StartDate).Days)
                    .ToList();

                MenstrualCycle.CycleLength = (int)cycleLengths.Average();
                Debug.WriteLine("Calculated average cycle length: " + MenstrualCycle.CycleLength);
                OnPropertyChanged(nameof(MenstrualCycle.CycleLength));
            }
        }

        private void CalculateAveragePeriodLength()
        {
            if (MenstrualCycle.Periods.Count > 0)
            {
                var periodLengths = MenstrualCycle.Periods
                    .Select(p => (p.EndDate - p.StartDate).Days + 1)
                    .ToList();

                AveragePeriodLength = periodLengths.Average();
                Debug.WriteLine("Calculated average period length: " + AveragePeriodLength);
                OnPropertyChanged(nameof(AveragePeriodLength));
            }
        }

        private void CalculateForecasts()
        {
            if (MenstrualCycle.Periods.Any() && MenstrualCycle.CycleLength > 0)
            {
                try
                {
                    DateTime lastPeriodStart = MenstrualCycle.Periods.Last().StartDate;
                    OvulationDate = CalculateOvulation(lastPeriodStart, MenstrualCycle.CycleLength);
                    (FertileStartDate, FertileEndDate) = CalculateFertileWindow(OvulationDate);
                    CalculateFertileDays(FertileStartDate, FertileEndDate);
                    CalculateSafeDays(FertileStartDate, FertileEndDate, lastPeriodStart, MenstrualCycle.CycleLength);
                    CalculateNextPeriodStartDate(lastPeriodStart);

                    Debug.WriteLine("Calculated ovulation date: " + OvulationDate);
                    Debug.WriteLine("Fertile window: " + FertileStartDate + " - " + FertileEndDate);
                    Debug.WriteLine("Next period start date: " + NextPeriodStartDate);

                    OnPropertyChanged(nameof(OvulationDate));
                    OnPropertyChanged(nameof(FertileStartDate));
                    OnPropertyChanged(nameof(FertileEndDate));
                    OnPropertyChanged(nameof(FertileDays));
                    OnPropertyChanged(nameof(SafeDays));
                    OnPropertyChanged(nameof(NextPeriodStartDate));
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    // Handle the exception appropriately (e.g., log it, show a message to the user, etc.)
                    Debug.WriteLine($"Error calculating dates: {ex.Message}");
                }
            }
        }

        private DateTime CalculateOvulation(DateTime startDate, int cycleLength)
        {
            DateTime ovulationDate = startDate.AddDays(cycleLength - 14);
            if (ovulationDate < DateTime.MinValue || ovulationDate > DateTime.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(ovulationDate), "The calculated ovulation date is out of range.");

            return ovulationDate;
        }

        private (DateTime, DateTime) CalculateFertileWindow(DateTime ovulationDate)
        {
            DateTime fertileStart = ovulationDate.AddDays(-5);
            DateTime fertileEnd = ovulationDate.AddDays(1);

            if (fertileStart < DateTime.MinValue || fertileStart > DateTime.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(fertileStart), "The calculated fertile start date is out of range.");
            if (fertileEnd < DateTime.MinValue || fertileEnd > DateTime.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(fertileEnd), "The calculated fertile end date is out of range.");

            return (fertileStart, fertileEnd);
        }

        private void CalculateFertileDays(DateTime fertileStart, DateTime fertileEnd)
        {
            FertileDays.Clear();
            for (DateTime day = fertileStart; day <= fertileEnd; day = day.AddDays(1))
            {
                FertileDays.Add(day);
            }
            Debug.WriteLine("Calculated fertile days: " + string.Join(", ", FertileDays));
        }

        private void CalculateSafeDays(DateTime fertileStart, DateTime fertileEnd, DateTime lastPeriodStart, int cycleLength)
        {
            SafeDays.Clear();
            DateTime cycleEnd = lastPeriodStart.AddDays(cycleLength - 1);
            for (DateTime day = lastPeriodStart.AddDays(AveragePeriodLength + 1); day < fertileStart; day = day.AddDays(1))
            {
                SafeDays.Add(day);
            }
            for (DateTime day = fertileEnd.AddDays(1); day <= cycleEnd; day = day.AddDays(1))
            {
                SafeDays.Add(day);
            }
            Debug.WriteLine("Calculated safe days: " + string.Join(", ", SafeDays));
        }

        private void CalculateNextPeriodStartDate(DateTime lastPeriodStart)
        {
            if (MenstrualCycle.CycleLength > 0)
            {
                NextPeriodStartDate = lastPeriodStart.AddDays(MenstrualCycle.CycleLength);
                Debug.WriteLine("Calculated next period start date: " + NextPeriodStartDate);
            }
        }

        private void UpdateLoggedPeriods()
        {
            LoggedPeriods.Clear();
            foreach (var period in MenstrualCycle.Periods)
            {
                LoggedPeriods.Add(period);
            }
            OnPropertyChanged(nameof(LoggedPeriods));
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
