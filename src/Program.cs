
using Photon.Barez;

try
{
    var person = Person.Load();
    Console.WriteLine("\nload person={0}", person.SerializeJson());

    if (string.IsNullOrWhiteSpace(person.Mobile) ||
        string.IsNullOrWhiteSpace(person.IdNumber))
    {
        throw new Exception("Mobile and IdNumber are neccessary.");
    }

    using var service = new HttpRequests();

    var check = await service.Check(person);
    Console.WriteLine("\ncheck person={0}", check.SerializeJson());

    person = await service.Auth(check);
    Console.WriteLine("\nauth person={0}", person.SerializeJson());

    person = await service.Add(person);
    Console.WriteLine("\nadd person={0}", person.SerializeJson());

    var centers = await service.All();
    Console.WriteLine("\nall centers={0}", centers.SerializeJson());
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(ex.Message);
}
