using Serilog;
using Photon.Barez;
using Serilog.Events;

try
{
    var personPath = (args.Length > 0 ? args[0] : null) ?? "person.json";
    var carPath = (args.Length > 1 ? args[1] : null) ?? "car.json";
    if (args.Length <= 2 || !int.TryParse(args[2], out var period)) period = 15;

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(LogEventLevel.Information)
        .WriteTo.File("events-.log", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
        .CreateLogger();

    using var service = new HttpRequests();

    var person = await RegisterFirstTime(service, personPath);
    var car = await FindCar(service, carPath);

    while (true)
    {
        var center = await CheckingCenters(service, period);
        await Dates(service, car, center);

        Thread.Sleep(period * 1000);
    }
}
catch (Exception ex)
{
    Log.Logger.Error(ex, "Error");
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

    person = await service.Add(person);
    Log.Logger.Information("ADD-PERSON\t{0}\t{1}", person.Mobile, person.Id);
    Log.Logger.Debug("ADD-PERSON={0}", person.SerializeJson());

    return person;
}

static async Task<Car> FindCar(HttpRequests service, string path)
{
    var car = JsonHandler.LoadFromFile<Car>(path);
    Log.Logger.Information("LOAD-ALLCARS\t{0}\t{1}", car.Id, car.TypeId);
    Log.Logger.Debug("LOAD-CAR={0}", car.SerializeJson());

    var cars = await service.Cars();
    car = cars.Where(c => (string.IsNullOrWhiteSpace(car.Car_name) || c.Car_name.Contains(car.Car_name)) &&
                          (car.TypeId == 0 || c.TypeId == car.TypeId))
               .Select(c => (Car?)c)
               .FirstOrDefault()
               ?? throw new Exception("Car not found!");

    Log.Logger.Information("CHECK-CAR\t{0}\t{1}", car.Id, car.TypeId);
    Log.Logger.Debug("CHECK-CAR={0}", car.SerializeJson());

    return car;
}

static async Task<Center> CheckingCenters(HttpRequests service, int period)
{
    var waiting_time = period;
    while (true)
    {
        try
        {
            var centers = await service.AllCenters();
            Log.Logger.Information("ALL-CENTERS\t{0}", centers?.Length ?? 0);
            Log.Logger.Debug("ALL-CENTERS={0}", centers?.SerializeJson());

            if (centers?.Length > 0)
            {
                var tehran = centers.Where(c => c.Ce_name.Contains("تهران"))
                                    .Select(c => (Center?)c)
                                    .FirstOrDefault();
                if (tehran.HasValue)
                {
                    return tehran.Value;
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

static async Task Dates(HttpRequests service, Car car, Center center)
{
    Log.Logger.Information("CHECK-CENTER\tW:{0}\tD:{0}", center.Work_centers?.Length, center.Dates?.Length);

    if (center.Work_centers == null || center.Work_centers.Length == 0)
    {
        return;
    }

    if (center.Dates == null || center.Dates.Length == 0)
    {
        return;
    }

    var work = center.Work_centers.First();
    var date = center.Dates.First();

    var appointement = await service.ReserveDates(date._en, work.Id, car.Id);

    Log.Logger.Information("CHECK-DATES\tW:{0}\tD:{0}", appointement);
    //Log.Logger.Debug("CHECK-DATES={0}", appointement);

    appointement = await service.ReserveDates(work.Wc_date, work.Id, car.Id);

    Log.Logger.Information("CHECK-DATES\tW:{0}\tD:{0}", appointement);
    //Log.Logger.Debug("CHECK-DATES={0}", appointement);
}
