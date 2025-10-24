namespace Smart_Stroller_App.Services;

public static class ApiClient
{
    // Set your ESP32 or server IP when UseMockApi=false
    private const string BaseUrl = "http://192.168.4.1"; // example ESP32 AP default

    public static readonly HttpClient Http = BuildClient();

    private static HttpClient BuildClient()
    {
        if (BuildConfig.UseMockApi)
        {
            return new HttpClient(new MockHttpHandler()) { BaseAddress = new Uri("http://mock") };
        }
        else
        {
            return new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }
    }
}
