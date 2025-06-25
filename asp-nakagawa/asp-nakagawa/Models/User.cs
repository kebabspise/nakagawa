using System.ComponentModel.DataAnnotations;


namespace asp_nakagawa.Models
{
    public class User
    {
        [Key]
        public int id { get; set; }
        public int user_id {  get; set; }
        public string name { get; set; }
        public string pass { get; set; }
        public bool admin {  get; set; }
        public int wages { get; set; }

        public ICollection<Shift_request> Shift_requests { get; set; }
        public ICollection<Shift_confirm> Shift_confirms { get; set; }
        public ICollection<Work_log> Work_logs { get; set; }
    }
}
