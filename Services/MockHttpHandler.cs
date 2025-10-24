using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Smart_Stroller_App.Services
{
    public class MockHttpHandler : HttpMessageHandler
    {
        private readonly Random _rng = new();
        private bool _brakeLocked = false;
        private double _batteryPct = 87.0;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            // Simulate slight battery drain and jitter
            _batteryPct = Math.Max(0, _batteryPct - (_rng.NextDouble() * 0.07));

            object body;
            int status = 200;

            if (request.Method == HttpMethod.Get && path == "/api/v1/battery")
            {
                // HomePage expects Percent (int)
                body = new { percent = (int)Math.Round(_batteryPct) };
            }
            else if (request.Method == HttpMethod.Get && path == "/api/v1/tof")
            {
                var distance = 120 + 90 * Math.Sin(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1200.0) + _rng.Next(-8, 9);
                body = new { distance = Math.Clamp((int)Math.Round(distance), 0, 300) };
            }
            else if (request.Method == HttpMethod.Get && path == "/api/v1/imu")
            {
                // Fake XYZ accelerometer-ish data in m/s^2 or g-units (scaled)
                var t = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 700.0;
                var x = 0.2 * Math.Sin(t) + _rng.NextDouble() * 0.05;
                var y = 0.15 * Math.Cos(t * 1.3) + _rng.NextDouble() * 0.05;
                var z = 1.0 + 0.02 * Math.Sin(t * 0.5);
                body = new { x = Math.Round(x, 3), y = Math.Round(y, 3), z = Math.Round(z, 3) };
            }
            else if (path == "/api/v1/brake" && request.Method == HttpMethod.Get)
            {
                body = new { locked = _brakeLocked };
            }
            else if (path == "/api/v1/brake/toggle" && request.Method == HttpMethod.Post)
            {
                _brakeLocked = !_brakeLocked;
                body = new { locked = _brakeLocked };
            }
            else
            {
                status = 404;
                body = new { error = "Not Found", path };
            }

            var json = JsonSerializer.Serialize(body);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
