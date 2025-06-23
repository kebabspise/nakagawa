using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace asp_nakagawa.Models
{
    public class Shift_confirm
    {
        [Key, ForeignKey("User")]
        public int user_id { get; set; }
        public DateTime work_start { get; set; }
        public DateTime work_end { get; set; }
    }
}
