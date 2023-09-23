using Photon.Barez;
using Serilog;
using Serilog.Events;
using System.Text.RegularExpressions;

try
{
    var personPath = (args.Length > 0 ? args[0] : null) ?? "person.json";
    var configPath = (args.Length > 1 ? args[1] : null) ?? "config.json";
    if (args.Length <= 2 || !int.TryParse(args[2], out var period)) period = 15;

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(LogEventLevel.Information)
        .WriteTo.File("events-.log", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
        .CreateLogger();

    var config = LoadConfig(configPath);

    using var service = new HttpRequests();

    var person = await RegisterFirstTime(service, personPath);
    var cars = await FindCar(service, config);

    while (true)
    {
        try
        {
            var center = await CheckingCenters(service, config, period);
            var work = GetWork(center);

            if (work == null) continue;

            foreach (var car in cars)
            {
                Log.Logger.Information("CHECK-CAR\t{0}\t{1}", car.Id, car.TypeId);

                var appointement = await Appointement(service, car, work.Value);

                if (appointement == null) continue;

                var product = await Products(service, config, car, work.Value, appointement.Value);

                if (product == null) continue;

                _ = ProductAdd(service, car, center, work.Value, person, appointement.Value, product.Value);
            }
        }
        finally
        {
            Thread.Sleep(period * 1000);
        }
    }
}
catch (Exception ex)
{
    Log.Logger.Error(ex, "Error");
}

static Config LoadConfig(string configPath)
{
    var config = JsonHandler.LoadFromFile<Config>(configPath);

    Log.Logger.Information("LOAD-CONFIG\tc={0}\tp={1}", config.CarName, config.ProductName);
    Log.Logger.Debug("LOAD-CONFIG={0}", config.SerializeJson());

    return config;
}

static async Task<Person> RegisterFirstTime(HttpRequests service, string path)
{
    var person = JsonHandler.LoadFromFile<Person>(path);
    Log.Logger.Information("LOAD-PERSON\t{0}\t{1}", person.Mobile, person.IdNumber);
    Log.Logger.Debug("LOAD-PERSON={0}", person.SerializeJson());

    if (string.IsNullOrWhiteSpace(person.Mobile) ||
        string.IsNullOrWhiteSpace(person.IdNumber))
    {
        throw new Exception("Mobile and IdNumber are neccessary.");
    }

    var check = await service.Check(person);
    Log.Logger.Information("CHECK-PERSON\t{0}\t{1}", person.Mobile, check.Code);
    Log.Logger.Debug("CHECK-PERSON={0}", check.SerializeJson());

    person = await service.Auth(check);
    Log.Logger.Information("AUTH-PERSON\t{0}\t{1}", person.Mobile, "AUTH");
    Log.Logger.Debug("AUTH-PERSON={0}", person.SerializeJson());

    person = await service.CustomerAdd(person);
    Log.Logger.Information("ADD-PERSON\t{0}\t{1}", person.Mobile, person.Id);
    Log.Logger.Debug("ADD-PERSON={0}", person.SerializeJson());

    return person;
}

static async Task<Car[]> FindCar(HttpRequests service, Config config)
{
    var cars = await service.Cars();

    var regex = string.IsNullOrWhiteSpace(config.CarName) ? null : new Regex(config.CarName);

    cars = cars.Where(c => regex == null || regex.IsMatch(c.Car_name))
               .ToArray();

    if (!cars.Any())
    {
        throw new Exception("Car not found!");
    }

    Log.Logger.Information("CHECK-CAR\tx{0}", cars.Length);
    Log.Logger.Debug("CHECK-CAR={0}", cars.SerializeJson());

    return cars;
}

static async Task<Center> CheckingCenters(HttpRequests service, Config config, int period)
{
    var waiting_time = period;
    while (true)
    {
        try
        {
            var centers = await service.AllCenters();
            Log.Logger.Information("ALL-CENTERS\tx{0}", centers?.Length ?? 0);
            Log.Logger.Debug("ALL-CENTERS={0}", centers?.SerializeJson());

            if (centers?.Length > 0)
            {
                var regex = string.IsNullOrWhiteSpace(config.CityName) ? null : new Regex(config.CityName);

                var city = centers.Where(c => regex == null || regex.IsMatch(c.Ce_name))
                                  .Select(c => (Center?)c)
                                  .FirstOrDefault();

                if (city.HasValue)
                {
                    var result = city.Value;

                    Log.Logger.Information("CHECK-CENTER\tW:{0}\tD:{1}", result.Work_centers?.Length, result.Dates?.Length);

                    return result;
                }
            }

            waiting_time = centers?.Length > 0 ? period / 4 : period;
        }
        catch (HttpRequestException ex)
        {
            Log.Logger.Error(ex, "Error");
        }

        Thread.Sleep(waiting_time * 1000);
    }
}

static WorkCenter? GetWork(Center center)
{
    if (center.Work_centers == null || center.Work_centers.Length == 0)
    {
        return null;
    }

    var result = center.Work_centers.Last();

    Log.Logger.Information("CHECK-CENTER\t{0}\t{1}", result.Reserved, result.Wc_date);

    return result;
}

static async Task<Appointement?> Appointement(HttpRequests service, Car car, WorkCenter work)
{
    var appointements = await service.ReserveDates(work.Wc_date, work.Id, car.Id);

    Log.Logger.Information("CHECK-DATES\tx{0}", appointements?.Length ?? 0);
    Log.Logger.Debug("CHECK-DATES={0}", appointements?.SerializeJson());

    var result = appointements?.Select(x => (Appointement?)x).LastOrDefault();

    Log.Logger.Information("SELECT-DATES\t{0}-{1}", result?.StartAt, result?.EndAt);

    return result;
}

static async Task<Product?> Products(HttpRequests service, Config config, Car car, WorkCenter work, Appointement appointement)
{
    var products = await service.Products(work.Wc_date, work.Id, car.Id, appointement.StartAt);

    var regex = string.IsNullOrWhiteSpace(config.ProductName) ? null : new Regex(config.ProductName);

    Log.Logger.Information("FETCH-PRODUCTS\tx{0}", products?.Length);
    Log.Logger.Debug("FETCH-PRODUCTS={0}", products?.SerializeJson());

    var result = products?.Where(x => regex == null || regex.IsMatch(x.Name))
                          .Select(x => (Product?)x)
                          .FirstOrDefault();

    if (result.HasValue)
    {
        Log.Logger.Information("SELECT-PRODUCT\t{0}", result.Value.Name);
        Log.Logger.Debug("SELECT-PRODUCT={0}", result.Value.SerializeJson());
    }

    return result;
}

static async Task ProductAdd(HttpRequests service, Car car, Center center, WorkCenter work, Person person, Appointement appointement, Product product)
{
    var result = await service.ProductAdd(car, center, work, person, appointement, product);
    Log.Logger.Information("BUY-PRODUCT\t{0}", result);
}
