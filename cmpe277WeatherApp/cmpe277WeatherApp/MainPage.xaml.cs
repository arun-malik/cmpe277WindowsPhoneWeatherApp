using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.WindowsAzure.MobileServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using cmpe277WeatherApp.Resources;
using Windows.Devices.Geolocation;
using System.IO.IsolatedStorage;
using System.IO;

namespace cmpe277WeatherApp
{
    public class city
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "cityURL")]
        public string CityURL { get; set; }

        [JsonProperty(PropertyName = "current")]
        public bool Current { get; set; }
    }

    public class Registrations
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "handle")]
        public string Handle { get; set; }
    }

    public partial class MainPage : PhoneApplicationPage
    {

        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private IMobileServiceTable<city> cityTable = App.MobileService.GetTable<city>();

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            if (IsolatedStorageSettings.ApplicationSettings.Contains("LocationConsent"))
            {
                getLocation();
                return;
            }
            else
            {
                MessageBoxResult result =
                    MessageBox.Show("This app accesses your phone's location. Is that ok?",
                    "Location",
                    MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = true;
                }
                else
                {
                    IsolatedStorageSettings.ApplicationSettings["LocationConsent"] = false;
                }

                IsolatedStorageSettings.ApplicationSettings.Save();
                getLocation();
            }

            //string cityName = this.NavigationContext.QueryString["City"];
            //txtblkCityName.Text = null != cityName ? cityName : "Error";

            ////new WeatherAPI().GetWeatherForecast(cityName);
            //  string url = "http://api.openweathermap.org/data/2.5/weather?q=" + cityName;
            //  GetWeatherForecast(url);

        }

        public void GetWeatherForecast(String Url)
        {
            UriBuilder restApiUri = new UriBuilder(Url);

            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(restApiUri.Uri);
            WeatherAsyncState reqState = new WeatherAsyncState();
            reqState.AsyncRequest = httpRequest;
            httpRequest.BeginGetResponse(new AsyncCallback(AsyncCallBackResponse), reqState);

        }

        public void AsyncCallBackResponse(IAsyncResult asyncResult)
        {

            String jsonString = null;
            WeatherAsyncState responseState = (WeatherAsyncState)asyncResult.AsyncState;
            HttpWebRequest httpRequest = (HttpWebRequest)responseState.AsyncRequest;

            responseState.AsyncResponse = (HttpWebResponse)httpRequest.EndGetResponse(asyncResult);

            using (Stream streamResult = responseState.AsyncResponse.GetResponseStream())
            {
                using (TextReader textReader = new StreamReader(streamResult, true))
                {
                    jsonString = textReader.ReadToEnd();
                }
            }

            dynamic jsonRes = JsonConvert.DeserializeObject(jsonString);
            string City = jsonRes.name;
            string country = jsonRes.sys.country;
            string mainTemp = jsonRes.main.temp;
            double degreeCelcius = Convert.ToDouble(mainTemp) - 273.15;
            string desc = jsonRes.weather[0].description;
            string airPressure = jsonRes.main.pressure;
            string humidity = jsonRes.main.humidity;
            string windspeed = jsonRes.wind.speed;


            var cityRecord = new city { CityURL = "Temp : "+degreeCelcius };
            InsertCityRecord(cityRecord);


            System.Text.StringBuilder result = new System.Text.StringBuilder("\n City: " + City);
                result.Append("\n Temperature: " + degreeCelcius + " Celcius.");
                result.Append("\n Description: " + desc);
                result.Append("\n AirPressure: " + airPressure);
                result.Append("\n Humidity: " + humidity);
                result.Append("\n Wind Speed: " + windspeed);
                result.Append("\n Country: " + country);

            
           

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                txtResult.Text = result.ToString();
            });

        }

        private async void getLocation()
        {

            if ((bool)IsolatedStorageSettings.ApplicationSettings["LocationConsent"] != true)
            {
                // The user has opted out of Location.
                MessageBox.Show("Geo Error\n Location  is disabled in phone settings.");
                return;
            }

            Geolocator geolocator = new Geolocator();
            geolocator.DesiredAccuracyInMeters = 50;

            try
            {
                Geoposition geoposition = await geolocator.GetGeopositionAsync(
                    maximumAge: TimeSpan.FromSeconds(5),
                    timeout: TimeSpan.FromSeconds(5)
                    );

                string latitude = geoposition.Coordinate.Latitude.ToString("0.0000");
                string longitude = geoposition.Coordinate.Longitude.ToString("0.0000");

                //MessageBox.Show("Geo Details\n Lat:" + latitude + ", Long: " + longitude);

                string url = "http://api.openweathermap.org/data/2.5/weather?lat=" + latitude + "&lon=" + longitude;

                

                GetWeatherForecast(url);

                //ImageBrush imageBrush = new ImageBrush();
                //imageBrush.ImageSource = new BitmapImage(new Uri("url", UriKind.Relative));
                //cnvBackground.Background = imageBrush;



            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == 0x80004004)
                {
                    // the application does not have the right capability or the location master switch is off
                    MessageBox.Show("Geo Error\n Report Issue and Grab a drink, till i fix this issue ;) ");

                }
                //else
                //{
                //     something else happened acquring the location
                //}
            }
        }

        private async void InsertCityRecord(city cityRecord)
        {
            await cityTable.InsertAsync(cityRecord);

        }

        public class WeatherAsyncState
        {
            public HttpWebRequest AsyncRequest { get; set; }
            public HttpWebResponse AsyncResponse { get; set; }
        }
    }
}