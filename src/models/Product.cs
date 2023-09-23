namespace Photon.Barez;

struct Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Image { get; set; }
    public long Price { get; set; }
    public string Barcode { get; set; }
    public int Items { get; set; }

    public struct Result
    {
        public bool Success { get; set; }
        public Product[] Data { get; set; }
    }

    public struct Ack
    {
        public bool Success { get; set; }
        public object Data { get; set; }
    }
}
