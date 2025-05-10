using System;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Newtonsoft.Json.Linq;

namespace StarsAICopilot.Avalonia.Pages;

public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
        DailySentence();
        GetWeather();
    }

    private async void DailySentence()
    {
        var quoteText = "无法获取每日金句。";
        var authorText = "未知";

        try
        {
            using (var client = new HttpClient())
            {
                var url = "https://api.open.aurorastudio.top/v1/daily-sentence"; 
                client.DefaultRequestHeaders.Add("type", "quotable"); 

                var response = await client.GetStringAsync(url);
                var json = JObject.Parse(response);

                if (json["error"] == null)
                {
                    quoteText = json["quote"].ToString();
                    authorText = json["author"].ToString();
                }
                else
                {
                    Console.WriteLine("Error: " + json["error"]);
                }
            }
        }
        catch
        {
        }

        QuoteTextBlock.Text = quoteText;
        AuthorTextBlock.Text = "--"+authorText;
    }
    
    private async void GetWeather()
    {
        var cityName = "未知";
        var weatherInfo = "未知";
        var temperatureInfo = "未知";

        try
        {
            using (var client = new HttpClient())
            {
                var url = "https://api.open.aurorastudio.top/v1/weather?sheng="+"guangdong"+"&shi="+"foshan"; 

                var response = await client.GetStringAsync(url);
                var json = JObject.Parse(response);

                if (json["error"] == null)
                {
                    cityName = json["city"].ToString();
                    weatherInfo = json["weather"].ToString();
                    temperatureInfo = json["temperature"].ToString();
                }
                else
                {
                    Console.WriteLine("Error: " + json["error"]);
                }
            }
        }
        catch
        {
        }

        CityName.Text = cityName;
        Weather.Text = weatherInfo;
        Temperature.Text = temperatureInfo;
    }
}