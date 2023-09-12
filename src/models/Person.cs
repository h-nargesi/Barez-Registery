namespace Photon.Barez;

struct Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Birthday { get; set; }
    public string IdNumber { get; set; }
    public string Mobile { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string CreatedAt { get; set; }
    public string UpdatedAt { get; set; }

    public override readonly string ToString()
    {
        return $"{IdNumber}/{Mobile}";
    }

    public static Person Load()
    {
        var data = File.ReadAllText("person.json");
        return data.DeserializeJson<Person>();
    }

    public struct Result
    {
        public bool Success { get; set; }
        public Person Customer { get; set; }
    }

    public struct Ack
    {
        public bool Success { get; set; }
        public Person Data { get; set; }
    }
}