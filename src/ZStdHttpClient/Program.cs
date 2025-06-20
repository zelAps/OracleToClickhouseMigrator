// Пример использования клиента со сжатием

using ZstdHttpClient;

var base_address = "http://10.0.0.138:8123/?user=ddddd&password=11111111&database=default&enable_http_compression=1&query=select%201";

using (var httpClient = new HttpClient())
{
    using (var response = await httpClient.GetAsync(base_address, CompressionType.Zstd))
    {
        string responseData = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseData);
    }
}

Console.Read();
