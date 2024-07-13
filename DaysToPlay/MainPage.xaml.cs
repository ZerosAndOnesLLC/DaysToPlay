using System;
using Microsoft.Maui.Controls;
using DaysToPlay.ViewModels;

namespace DaysToPlay
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            if (BindingContext is MainViewModel viewModel)
            {
                viewModel.PeriodsUpdated += GenerateCalendar;
            }
        }

        private void GenerateCalendar()
        {
            // Clear any existing children in the CalendarGrid
            CalendarGrid.Children.Clear();
            //CalendarGrid.RowDefinitions.Clear();
            //CalendarGrid.ColumnDefinitions.Clear();

            // Add day names header
            string[] dayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            for (int i = 0; i < dayNames.Length; i++)
            {
                var dayLabel = new Label
                {
                    Text = dayNames[i],
                    TextColor = Colors.White,
                    //HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold
                    //Style = (Style)Application.Current.Resources["CalendarDayLabel"]
                };

                CalendarGrid.Children.Add(dayLabel);
                Grid.SetColumn(dayLabel, i);
                Grid.SetRow(dayLabel, 0);
            }

            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            int currentRow = 1;
            int currentColumn = (int)firstDayOfMonth.DayOfWeek;

            for (DateTime day = firstDayOfMonth; day <= lastDayOfMonth; day = day.AddDays(1))
            {
                var label = new Label
                {
                    Text = day.Day.ToString(),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                // Highlight specific dates
                if (BindingContext is MainViewModel viewModel)
                {
                    if (viewModel.FertileDays.Contains(day))
                    {
                        label.BackgroundColor = Colors.Pink;
                        label.TextColor = Colors.White;
                    }
                    else if (viewModel.SafeDays.Contains(day))
                    {
                        label.BackgroundColor = Colors.LightGreen;
                        label.TextColor = Colors.White;
                    }
                    else if (viewModel.LoggedPeriods.Any(p => p.StartDate <= day && p.EndDate >= day))
                    {
                        label.BackgroundColor = Colors.Red;
                        label.TextColor = Colors.White;
                    }
                    else
                    {
                        label.TextColor = Colors.White;
                    }
                }

                CalendarGrid.Children.Add(label);
                Grid.SetRow(label, currentRow);
                Grid.SetColumn(label, currentColumn);

                currentColumn++;
                if (currentColumn > 6)
                {
                    currentColumn = 0;
                    currentRow++;
                }
            }

            // Adjust row definitions based on the number of rows required
            CalendarGrid.RowDefinitions.Clear();
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // For the day names
            for (int i = 1; i <= currentRow; i++)
            {
                CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
        }
    }
}
