using Newtonsoft.Json;
using RestSharp;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using WebSocketSharp;

namespace ServiceClientGameTest
{
    internal class MSG
    {
        public Accelerometer data { get; set; }
        public string sender { get; set; }
    }

    public partial class MainWindow : Window
    {
        private string _baseUrl;
        private double _left;
        private double _top;
        private DropShadowEffect _effect;
        private DispatcherTimer _timer;
        private TimeSpan _duration = TimeSpan.FromSeconds(47);
        private DateTime _start;
        private WebSocket _ws;

        public MainWindow()
        {
            InitializeComponent();
            _effect = FindResource("Shadow") as DropShadowEffect;
            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(timer_Tick);
            _timer.Interval = TimeSpan.FromMilliseconds(7);
            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        GetDir(out var dir);
            //        Dispatcher.Invoke(() =>
            //        {
            //            _left = (double)_fly.GetValue(Canvas.LeftProperty);
            //            _top = (double)_fly.GetValue(Canvas.TopProperty);
            //            Canvas.SetLeft(_fly, _left + dir.X);
            //            Canvas.SetTop(_fly, _top + dir.Y);

            //            _fly.RenderTransform = new ScaleTransform(dir.Scale, dir.Scale);
            //            _effect.ShadowDepth = dir.Scale * 7;
            //        });
            //    }
            //});

            _ws = new WebSocket("ws://greatservice.azurewebsites.net/?name=Console&room=great");
            _ws.OnMessage += (sender, e) =>
                {
                    Accelerometer acc;
                    try
                    {
                        acc = JsonConvert.DeserializeObject<Accelerometer>(e.Data);
                    }
                    catch (Exception ex)
                    {
                        return;
                    }

                    Dir dir;
                    dir.X = acc.x * -10;
                    dir.Y = acc.y * 10;
                    var coef = 0.981;
                    var scaleDefault = 1;

                    dir.Scale = ((acc.z - coef) + scaleDefault) * 1.6;
                    Dispatcher.Invoke(() =>
                    {
                        _left = (double)_fly.GetValue(Canvas.LeftProperty);
                        _top = (double)_fly.GetValue(Canvas.TopProperty);
                        Canvas.SetLeft(_fly, _left + dir.X);
                        Canvas.SetTop(_fly, _top + dir.Y);

                        _fly.RenderTransform = new ScaleTransform(dir.Scale, dir.Scale);
                        _effect.ShadowDepth = dir.Scale * 7;
                    });
                };

            _ws.Connect();
            _ws.Send("BALUS");
        }

        private void GetDir(out Dir dir)
        {
            if (string.IsNullOrEmpty(_baseUrl))
            {
                Dispatcher.Invoke(() => _baseUrl = _url.Text);
                if (string.IsNullOrEmpty(_baseUrl))
                    throw new ArgumentException("Address server not be empty");
            }

            var client = new RestClient(_baseUrl);
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response = client.Execute(request);
            dir = new Dir();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var acc = JsonConvert.DeserializeObject<Accelerometer>(response.Content);
                dir.X = acc.x * -10;
                dir.Y = acc.y * 10;
                var coef = 0.981;
                var scaleDefault = 1;

                dir.Scale = ((acc.z - coef) + scaleDefault) * 1.6;
            }
        }

        private void Canvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _start = DateTime.Now;
            _timer.Start();
            _baseUrl = _url.Text;
            TimeTextBox.FontSize = 55;
            Canvas.SetLeft(_fly, 400);
            Canvas.SetTop(_fly, 200);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            var data = _duration - (DateTime.Now - _start);
            string msg;
            if (data.TotalMilliseconds < 0)
            {
                _timer.Stop();
                msg = DateTime.Now.Millisecond % 2 == 0 ? "YOU WIN!!!\nNew game?" : "YOU LOSE!!!\nNew game?";
                TimeTextBox.FontSize = 100;
            }
            else
            {
                msg = $"{data.Minutes}:{data.Seconds.ToString().PadLeft(2, '0')}:{data.Milliseconds.ToString().PadLeft(3, '0')}";
            }
            TimeTextBox.Text = msg;
        }
    }
}