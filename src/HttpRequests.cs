using System.Net.Mime;
using System.Text;

namespace Photon.Barez;

class HttpRequests : IDisposable
{
    const string BaseUrl = "https://pos.barez.org/";
    readonly HttpClient service = new();

    public HttpRequests()
    {
        service.DefaultRequestHeaders.Add("authority", "pos.barez.org");
        service.DefaultRequestHeaders.Add("accept", "application/json, text/plain, */*");
        service.DefaultRequestHeaders.Add("accept-language", "en-US,en;q=0.8");
        service.DefaultRequestHeaders.Add("origin", "https://pos.barez.org");
        service.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"116\", \"Not)A;Brand\";v=\"24\", \"Brave\";v=\"116\"");
        service.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        service.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");
        service.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
        service.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
        service.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
        service.DefaultRequestHeaders.Add("sec-gpc", "1");
        service.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36");

        service.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<Check> Check(Person person)
    {
        var data = new
        {
            person.IdNumber,
            person.Mobile,
        };

        var content = new StringContent(
            data.SerializeJson(),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        using var response = await service.PostAsync("reserve/check", content);
        var text = await response.Content.ReadAsStringAsync();

        try
        {
            var result = text.DeserializeJson<Check.Result>();
            if (!result.Success) HandleError(text);

            return result.Customer;
        }
        catch (Exception ex)
        {
            throw new Exception(text, ex);
        }
    }

    public async Task<Person> Auth(Check check)
    {
        using var response = await service.GetAsync($"reserve/auth?code={check.Code}&id={check.IdNumber}");
        var text = await response.Content.ReadAsStringAsync();

        try
        {
            var result = text.DeserializeJson<Person.Result>();
            if (!result.Success) HandleError(text);

            return result.Customer;
        }
        catch (Exception ex)
        {
            throw new Exception(text, ex);
        }
    }

    public async Task<Person> CustomerAdd(Person person)
    {
        var data = new
        {
            person.IdNumber,
            person.Mobile,
            person.FirstName,
            person.LastName,
            person.Address,
            person.Birthday,
            person.Email,
        };

        var content = new StringContent(
            data.SerializeJson(),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        using var response = await service.PostAsync("reserve/customer/add", content);
        var text = await response.Content.ReadAsStringAsync();

        try
        {
            var result = text.DeserializeJson<Person.Ack>();
            if (!result.Success) HandleError(text);

            return result.Data;
        }
        catch (Exception ex)
        {
            throw new Exception(text, ex);
        }
    }

    public async Task<Center[]> AllCenters()
    {
        using var response = await service.GetAsync("reserve/centers/all");
        var text = await response.Content.ReadAsStringAsync();

        try
        {
            var result = text.DeserializeJson<Center.Result>();
            if (!result.Success) HandleError(text);

            return result.Data;
        }
        catch (Exception ex)
        {
            throw new Exception(text, ex);
        }
    }

    public async Task<Car[]> Cars()
    {
        using var response = await service.GetAsync("allcars");
        var text = await response.Content.ReadAsStringAsync();

        try
        {
            var result = text.DeserializeJson<Car.Result>();
            if (!result.Success) HandleError(text);

            return result.Data;
        }
        catch (Exception ex)
        {
            throw new Exception(text, ex);
        }
    }

    public async Task<Appointement[]> ReserveDates(string wc_date, int workId, int carId)
    {
        using var response = await service.GetAsync($"reserve/dates?wc_date={wc_date}&workId={workId}&carId={carId}");
        var text = await response.Content.ReadAsStringAsync();

        try
        {
            var result = text.DeserializeJson<Appointement.Result>();
            if (!result.Success) HandleError(text);

            return result.Data;
        }
        catch (Exception ex)
        {
            throw new Exception(text, ex);
        }
    }

    public async Task<Product[]> Products(string wc_date, int workId, int carId, string startAt)
    {
        using var response = await service.GetAsync($"reserve/products?wc_date={wc_date}&workId={workId}&carId={carId}&startAt={startAt}");
        var text = await response.Content.ReadAsStringAsync();

        try
        {
            var result = text.DeserializeJson<Product.Result>();
            if (!result.Success) HandleError(text);

            return result.Data;
        }
        catch (Exception ex)
        {
            throw new Exception(text, ex);
        }
    }

    public async Task<string> ProductAdd(Car car, Center center, WorkCenter work, Person person, Appointement appointement, Product product)
    {
        var data = new
        {
            CarId = car.Id.ToString(),
            CenterId = center.Id.ToString(),
            CustomerId = person.Id.ToString(),
            person.IdNumber,
            ProductId = product.Id.ToString(),
            ProductPrice = product.Price.ToString(),
            appointement.StartAt,
            Workcenter_date = work.Wc_date,
        };

        var content = new StringContent(
            data.SerializeJson(),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        using var response = await service.PostAsync("reserve/add", content);
        var text = await response.Content.ReadAsStringAsync();

        var result = text.DeserializeJson<Product.Ack>();
        if (!result.Success) HandleError(text);

        return result.Success.ToString();
    }

    public void Dispose()
    {
        service?.Dispose();
    }

    private static void HandleError(string text)
    {
        var error = text.DeserializeJson<ErrorResult>();
        throw new Exception(error.Error);
    }
}