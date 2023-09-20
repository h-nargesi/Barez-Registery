namespace Photon.Barez;

struct Car
{
    public int Id { get; set; }
    public int TypeId { get; set; }
    public string Car_name { get; set; }

    public struct Result
    {
        public bool Success { get; set; }
        public Car[] Data { get; set; }
    }
}
