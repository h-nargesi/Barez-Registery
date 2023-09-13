using Serilog;
using Photon.Barez;
using Serilog.Events;

try
{
    var path = (args.Length > 1 ? args[0] : null) ?? "person.json";
    if (args.Length < 2 || !int.TryParse(args[1], out var period)) period = 15;

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(LogEventLevel.Information)
        .WriteTo.File("events-.log", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
        .CreateLogger();

    using var service = new HttpRequests();

    var person = await RegisterFirstTime(service, path);
    await CheckingCenters(service, period);
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

static async Task CheckingCenters(HttpRequests service, int period)
{
    while (true)
    {
        var centers = await service.All();
        Log.Logger.Information("ALL-CENTERS\t{0}", centers?.Length ?? 0);
        Log.Logger.Debug("ALL-CENTERS={0}", centers?.SerializeJson());
        Thread.Sleep(period * 1000);
    }
}