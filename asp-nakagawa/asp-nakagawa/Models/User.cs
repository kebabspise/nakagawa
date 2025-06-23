using System.ComponentModel.DataAnnotations;


namespace asp_nakagawa.Models
{
    public class User
    {
        [Key]
        public int user_id { get; set; }
        public string name { get; set; }
        public string pass { get; set; }
        public bool admin {  get; set; }
        public int wages { get; set; }
    }
}
