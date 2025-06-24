using System.ComponentModel.DataAnnotations;


namespace asp_nakagawa.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Pass { get; set; }
        public bool Admin {  get; set; }
        public int Wages { get; set; }

        public ICollection<Shift_request> Shift_requests { get; set; }
        public ICollection<Shift_confirm> Shift_confirms { get; set; }
        public ICollection<Work_log> Work_logs { get; set; }
    }
}
