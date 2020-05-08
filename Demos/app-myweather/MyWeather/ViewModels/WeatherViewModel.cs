﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using AsyncAwaitBestPractices.MVVM;
using MyWeather.Models;
using MyWeather.Services;
using Xamarin.Essentials;

namespace MyWeather.ViewModels
{
    class WeatherViewModel : INotifyPropertyChanged
    {
        readonly WeakEventManager onPropertyChangedEventManager = new WeakEventManager();

        bool useGPS;
        string temperature = string.Empty;
        string location = Settings.City;
        bool isImperial = Settings.IsImperial;
        string condition = string.Empty;
        bool isBusy;

        WeatherForecastRoot? forecast;
        IAsyncCommand? getWeatherCommand;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => onPropertyChangedEventManager.AddEventHandler(value);
            remove => onPropertyChangedEventManager.RemoveEventHandler(value);
        }

        public IAsyncCommand GetWeatherCommand => getWeatherCommand ??= new AsyncCommand(() => ExecuteGetWeatherCommand(UseGPS, IsImperial, Location), _ => !IsBusy);

        public List<WeatherRoot> ForecastItems => Forecast?.Items ?? Enumerable.Empty<WeatherRoot>().ToList();

        public string Location
        {
            get => location;
            set => SetProperty(ref location, value, () => Settings.City = value);
        }

        public bool UseGPS
        {
            get => useGPS;
            set => SetProperty(ref useGPS, value);
        }

        public bool IsImperial
        {
            get => isImperial;
            set => SetProperty(ref isImperial, value, () => Settings.IsImperial = value);
        }

        public string Temperature
        {
            get => temperature;
            set => SetProperty(ref temperature, value);
        }

        public string Condition
        {
            get => condition;
            set => SetProperty(ref condition, value);
        }

        public bool IsBusy
        {
            get => isBusy;
            set => SetProperty(ref isBusy, value, () => MainThread.BeginInvokeOnMainThread(GetWeatherCommand.RaiseCanExecuteChanged));
        }

        WeatherForecastRoot? Forecast
        {
            get => forecast;
            set => SetProperty(ref forecast, value, () => OnPropertyChanged(nameof(ForecastItems)));
        }

        async Task ExecuteGetWeatherCommand(bool useGps, bool isImperial, string location)
        {
            IsBusy = true;
            Temperature = Condition = string.Empty;

            try
            {
                WeatherRoot weatherRoot;

                var units = isImperial ? Units.Imperial : Units.Metric;

                if (useGps)
                {
                    var gps = await Geolocation.GetLocationAsync().ConfigureAwait(false);
                    weatherRoot = await WeatherService.GetWeather(gps.Latitude, gps.Longitude, units).ConfigureAwait(false);
                }
                else
                {
                    //Get weather by city
                    weatherRoot = await WeatherService.GetWeather(location.Trim(), units).ConfigureAwait(false);
                }

                //Get forecast based on cityId
                Forecast = await WeatherService.GetForecast(weatherRoot, units).ConfigureAwait(false);

                var unit = isImperial ? "F" : "C";

                Temperature = $"Temp: {weatherRoot.MainWeather.Temperature}°{unit}";
                Condition = $"{weatherRoot.Name}: {weatherRoot.Weather.First().Description}";

                TextToSpeech.SpeakAsync(Temperature + " " + Condition).SafeFireAndForget(onException: ex => DebugServices.Report(ex));
            }
            catch (Exception e)
            {
                DebugServices.Report(e);
                Temperature = "Unable to get Weather";
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected void SetProperty<T>(ref T backingStore, in T value, in Action? onChanged = null, [CallerMemberName] in string propertyname = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return;

            backingStore = value;

            onChanged?.Invoke();

            OnPropertyChanged(propertyname);
        }

        void OnPropertyChanged([CallerMemberName] in string propertyName = "") =>
            onPropertyChangedEventManager.HandleEvent(this, new PropertyChangedEventArgs(propertyName), nameof(INotifyPropertyChanged.PropertyChanged));
    }
}
