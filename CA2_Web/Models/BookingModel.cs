namespace CA2_Web.Models
{
    public class Booking
    {
        public string Id { get; set; }
        public string RoomId { get; set; }

        public string BookedById { get; set; }
        public string BookedByName { get; set; }
        public string SupervisedById { get; set; }
        public string SupervisedByName { get; set; }

        public int Status { get; set; }
        public string BookDate { get; set; }
        public int TimeSlotId { get; set; }
        public string TimeSlot { get; set; }
    }
}
