
namespace Photon.Barez;

struct Center
{
    public int Id { get; set; }
    public string Ce_name { get; set; }
    public string StartAt { get; set; }
    public string EndAt { get; set; }
    public string HourStart { get; set; }
    public string HourEnd { get; set; }
    public WorkCenter[] Work_centers { get; set; }
    public int CouldReserved { get; set; }
    public Appointment[] Dates { get; set; }
    public int CustomerReserved { get; set; }

    public struct Result
    {
        public bool Success { get; set; }
        public Center[] Data { get; set; }
    }
}

struct Appointment
{
    public string _en { get; set; }
    public string _fa { get; set; }
    public string _day { get; set; }
}

struct WorkCenter
{
    public int Id { get; set; }
    public string Wc_date { get; set; }
    public int Reserved { get; set; }
}