namespace Photon.Barez;

struct Appointement
{
    public string StartAt { get; set; }
    public string EndAt { get; set; }

    public struct Result
    {
        public bool Success { get; set; }
        public Appointement[] Data { get; set; }
    }
}
