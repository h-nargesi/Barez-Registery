namespace Photon.Barez;

struct Check
{
    public int Id { get; set; }
    public string IdNumber { get; set; }
    public string Mobile { get; set; }
    public string Code { get; set; }

    public struct Result
    {
        public bool Success { get; set; }
        public Check Customer { get; set; }
    }
}
